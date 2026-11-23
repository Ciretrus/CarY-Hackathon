using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Noir.UI
{
    public enum HintSlideFrom { Left, Right, Top, Bottom }

    [Serializable]
    public class TutorialHint
    {
        [Tooltip("Карточка (Image с PNG). Ставится в Canvas у края экрана, там где она должна лежать")]
        public RectTransform card;
        [Tooltip("Если пусто, добавится автоматически")]
        public CanvasGroup group;
        [Tooltip("С какого края карточка выезжает и куда уезжает")]
        public HintSlideFrom slideFrom = HintSlideFrom.Left;
        [Tooltip("Наклон карточки в покое, градусы (как положенная на стол бумага)")]
        public float restTilt = -3f;
        public AudioClip showSound;
    }

    public class TutorialHintsController : MonoBehaviour
    {
        enum State { Idle, Showing, Waiting, Hiding, Finished }

        [Header("Подсказки")]
        [SerializeField] private List<TutorialHint> _hints = new();
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private float _startDelay = 0.8f;
        [Tooltip("Показывать только при первом запуске игры")]
        [SerializeField] private bool _showOnlyOnce = false;
        [SerializeField] private string _prefsKey = "NoirTutorialDone";

        [Header("Управление")]
        [SerializeField] private Key _nextKey = Key.E;
        [Tooltip("Сколько ждать после появления карточки, прежде чем принять нажатие")]
        [SerializeField] private float _minShowTime = 0.5f;

        [Header("Затемнение и значок клавиши")]
        [Tooltip("Чёрный Image на весь экран + CanvasGroup. Raycast Target включить, чтобы клики не уходили в UI под ним")]
        [SerializeField] private CanvasGroup _dim;
        [SerializeField, Range(0f, 1f)] private float _dimAlpha = 0.55f;
        [Tooltip("Значок «E - далее» + CanvasGroup")]
        [SerializeField] private CanvasGroup _keyPrompt;

        [Header("Анимация")]
        [SerializeField] private float _showDuration = 0.55f;
        [SerializeField] private float _hideDuration = 0.3f;
        [SerializeField] private float _slideMargin = 120f;
        [Tooltip("Дополнительный наклон в момент въезда, градусы")]
        [SerializeField] private float _enterTilt = 9f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InCubic;
        [Tooltip("Лёгкое покачивание карточки в покое, пиксели. 0 = выключено")]
        [SerializeField] private float _idleFloat = 6f;

        [Header("Звук")]
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _hideSound;
        [SerializeField] private AudioClip _finishSound;
        [SerializeField, Range(0f, 0.2f)] private float _pitchJitter = 0.05f;

        [Header("События")]
        public UnityEvent onStarted;
        public UnityEvent onFinished;
        public event Action Started;
        public event Action Finished;

        public bool IsRunning => _state is State.Showing or State.Waiting or State.Hiding;

        private State _state = State.Idle;
        private int _index;
        private float _canAdvanceAt;
        private Vector2[] _rest;
        private Sequence _sequence;
        private Tween _idle;

        void Awake()
        {
            _rest = new Vector2[_hints.Count];
            for (int i = 0; i < _hints.Count; i++)
            {
                var h = _hints[i];
                if (h.card == null)
                {
                    Debug.LogError($"TutorialHintsController: у подсказки {i} не назначена карточка", this);
                    enabled = false;
                    return;
                }

                if (h.group == null)
                {
                    h.group = h.card.GetComponent<CanvasGroup>();
                    if (h.group == null)
                        h.group = h.card.gameObject.AddComponent<CanvasGroup>();
                }

                h.group.blocksRaycasts = false;
                h.group.interactable = false;
                h.group.alpha = 0f;

                _rest[i] = h.card.anchoredPosition;
                h.card.gameObject.SetActive(false);
            }

            if (_dim != null)
            {
                _dim.alpha = 0f;
                _dim.blocksRaycasts = false;
            }
            if (_keyPrompt != null)
                _keyPrompt.alpha = 0f;
        }

        void Start()
        {
            if (!_playOnStart)
                return;
            if (_showOnlyOnce && PlayerPrefs.GetInt(_prefsKey, 0) == 1)
                return;

            DOVirtual.DelayedCall(_startDelay, Begin).SetLink(gameObject);
        }

        void OnDisable()
        {
            _sequence?.Kill();
            _idle?.Kill();
        }

        void Update()
        {
            if (_state != State.Waiting || Time.unscaledTime < _canAdvanceAt)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_nextKey].wasPressedThisFrame)
                Next();
        }

        public void Begin()
        {
            if (!enabled || IsRunning || _hints.Count == 0)
                return;

            _index = 0;
            onStarted?.Invoke();
            Started?.Invoke();

            if (_dim != null)
            {
                _dim.blocksRaycasts = true;
                _dim.DOKill();
                _dim.DOFade(_dimAlpha, 0.4f).SetUpdate(true).SetLink(_dim.gameObject);
            }

            if (_keyPrompt != null)
                _keyPrompt.transform.localScale = Vector3.one;

            ShowHint(0);
        }

        void ShowHint(int index)
        {
            _index = index;
            _state = State.Showing;

            var h = _hints[index];
            var rt = h.card;
            var rest = _rest[index];

            rt.DOKill();
            h.group.DOKill();
            rt.gameObject.SetActive(true);
            rt.SetAsLastSibling();
            rt.anchoredPosition = rest + SlideOffset(h);
            rt.localRotation = Quaternion.Euler(0f, 0f, h.restTilt + TiltSign(h) * _enterTilt);
            rt.localScale = Vector3.one * 0.92f;
            h.group.alpha = 0f;

            Play(h.showSound);

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(rt.DOAnchorPos(rest, _showDuration).SetEase(_showEase));
            _sequence.Join(rt.DOLocalRotate(new Vector3(0f, 0f, h.restTilt), _showDuration).SetEase(Ease.OutCubic));
            _sequence.Join(rt.DOScale(1f, _showDuration).SetEase(Ease.OutCubic));
            _sequence.Join(h.group.DOFade(1f, _showDuration * 0.5f));
            _sequence.OnComplete(() => OnShown(rt, rest));
        }

        void OnShown(RectTransform rt, Vector2 rest)
        {
            _state = State.Waiting;
            _canAdvanceAt = Time.unscaledTime + _minShowTime;

            if (_keyPrompt != null)
            {
                _keyPrompt.DOKill();
                _keyPrompt.DOFade(1f, 0.25f).SetUpdate(true).SetLink(_keyPrompt.gameObject);
            }

            if (_idleFloat > 0f)
            {
                _idle?.Kill();
                _idle = rt.DOAnchorPosY(rest.y + _idleFloat, 1.6f)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true).SetLink(rt.gameObject);
            }
        }

        void Next()
        {
            _state = State.Hiding;
            _idle?.Kill();

            if (_keyPrompt != null)
            {
                _keyPrompt.DOKill();
                _keyPrompt.DOFade(0f, 0.15f).SetUpdate(true).SetLink(_keyPrompt.gameObject);
            }

            var h = _hints[_index];
            var rt = h.card;
            Vector2 target = _rest[_index] + SlideOffset(h);

            Play(_hideSound);

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(rt.DOAnchorPos(target, _hideDuration).SetEase(_hideEase));
            _sequence.Join(rt.DOLocalRotate(new Vector3(0f, 0f, h.restTilt - TiltSign(h) * _enterTilt * 0.5f), _hideDuration));
            _sequence.Join(h.group.DOFade(0f, _hideDuration * 0.8f));
            _sequence.OnComplete(() =>
            {
                rt.gameObject.SetActive(false);
                if (_index + 1 < _hints.Count)
                    ShowHint(_index + 1);
                else
                    Finish();
            });
        }

        void Finish()
        {
            _state = State.Finished;
            Play(_finishSound);

            if (_dim != null)
            {
                _dim.DOKill();
                _dim.DOFade(0f, 0.4f).SetUpdate(true).SetLink(_dim.gameObject);
            }

            if (_showOnlyOnce)
            {
                PlayerPrefs.SetInt(_prefsKey, 1);
                PlayerPrefs.Save();
            }

            DOVirtual.DelayedCall(0.15f, () =>
            {
                if (_dim != null)
                    _dim.blocksRaycasts = false;
                onFinished?.Invoke();
                Finished?.Invoke();
            }).SetUpdate(true).SetLink(gameObject);
        }

        Vector2 SlideOffset(TutorialHint h)
        {
            Vector2 size = h.card.rect.size;
            return h.slideFrom switch
            {
                HintSlideFrom.Left => new Vector2(-(size.x + _slideMargin), 0f),
                HintSlideFrom.Right => new Vector2(size.x + _slideMargin, 0f),
                HintSlideFrom.Top => new Vector2(0f, size.y + _slideMargin),
                _ => new Vector2(0f, -(size.y + _slideMargin)),
            };
        }

        static float TiltSign(TutorialHint h) =>
            h.slideFrom is HintSlideFrom.Left or HintSlideFrom.Top ? 1f : -1f;

        void Play(AudioClip clip)
        {
            if (_audio == null || clip == null)
                return;
            _audio.pitch = 1f + UnityEngine.Random.Range(-_pitchJitter, _pitchJitter);
            _audio.PlayOneShot(clip);
        }

        [ContextMenu("Reset saved flag")]
        void ResetSavedFlag() => PlayerPrefs.DeleteKey(_prefsKey);
    }
}