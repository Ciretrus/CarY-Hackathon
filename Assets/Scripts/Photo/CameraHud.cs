using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Noir.Photo
{
    public class CameraHud : MonoBehaviour
    {
        [Header("Hands")]
        [SerializeField] private RectTransform _hands;
        [SerializeField] private Vector2 _handsHidden = new(0f, -700f);
        [SerializeField] private Vector2 _handsRaised = new(0f, -150f);
        [SerializeField] private float _handsZoom = 4f;
        [SerializeField] private float _raiseTime = 0.35f;
        [SerializeField] private float _zoomTime = 0.25f;

        [Header("Screen")]
        [SerializeField] private CanvasGroup _blackout;
        [SerializeField] private CanvasGroup _viewfinder;
        [SerializeField] private Volume _cameraVolume;
        [SerializeField] private float _fadeTime = 0.15f;

        [Header("Shot")]
        [SerializeField] private CanvasGroup _flash;
        [SerializeField] private RectTransform _card;
        [SerializeField] private CanvasGroup _cardGroup;
        [SerializeField] private RawImage _cardImage;
        [SerializeField] private RectTransform _cardCorner;
        [SerializeField] private Volume _blurVolume;
        [SerializeField] private float _flashTime = 0.2f;
        [SerializeField] private float _cardHold = 3f;
        [SerializeField] private float _cardOutTime = 0.2f;

        private Canvas _canvas;
        private Vector2 _cardCenter;
        private Sequence _sequence;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _cardCenter = _card.anchoredPosition;
            ResetAll();
        }

        public Sequence Raise(TweenCallback onDark)
        {
            Restart();
            _canvas.enabled = true;
            _cameraVolume.enabled = true;
            _sequence
                .Append(_hands.DOAnchorPos(_handsRaised, _raiseTime).SetEase(Ease.OutQuad))
                .Append(_hands.DOScale(_handsZoom, _zoomTime).SetEase(Ease.InQuad))
                .Append(_blackout.DOFade(1f, _fadeTime))
                .AppendCallback(() =>
                {
                    HideHands();
                    _viewfinder.alpha = 1f;
                    _cameraVolume.weight = 1f;
                    onDark?.Invoke();
                })
                .Append(_blackout.DOFade(0f, _fadeTime));
            return _sequence;
        }

        public Sequence Lower(TweenCallback onDark)
        {
            Restart();
            _sequence
                .Append(_blackout.DOFade(1f, _fadeTime))
                .AppendCallback(() =>
                {
                    _viewfinder.alpha = 0f;
                    _cameraVolume.weight = 0f;
                    _hands.anchoredPosition = _handsRaised;
                    _hands.localScale = Vector3.one * _handsZoom;
                    onDark?.Invoke();
                })
                .Append(_blackout.DOFade(0f, _fadeTime))
                .Join(_hands.DOScale(1f, _zoomTime).SetEase(Ease.OutQuad))
                .Append(_hands.DOAnchorPos(_handsHidden, _raiseTime).SetEase(Ease.InQuad));
            return _sequence;
        }

        public Sequence Shot(Texture photo)
        {
            Restart();
            _cardImage.texture = photo;
            _card.anchoredPosition = _cardCenter;
            _card.localScale = Vector3.one * 0.85f;
            _blurVolume.enabled = true;

            _sequence
                .Append(_flash.DOFade(1f, 0.03f))
                .Append(_flash.DOFade(0f, _flashTime))
                .Join(_cardGroup.DOFade(1f, _flashTime))
                .Join(_card.DOScale(1f, _flashTime).SetEase(Ease.OutBack))
                .Join(FadeVolume(_blurVolume, 1f, _flashTime))
                .AppendInterval(_cardHold)
                .Append(_card.DOMove(_cardCorner.position, _cardOutTime).SetEase(Ease.InBack))
                .Join(_card.DOScale(0.25f, _cardOutTime))
                .Join(FadeVolume(_blurVolume, 0f, _cardOutTime))
                .Append(_cardGroup.DOFade(0f, 0.1f));
            return _sequence;
        }

        public void ResetAll()
        {
            _sequence?.Kill();
            HideHands();
            _blackout.alpha = 0f;
            _viewfinder.alpha = 0f;
            _flash.alpha = 0f;
            _cardGroup.alpha = 0f;
            _cameraVolume.weight = 0f;
            _blurVolume.weight = 0f;
            _cameraVolume.enabled = false;
            _blurVolume.enabled = false;
            _canvas.enabled = false;
        }

        private void Restart()
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetLink(gameObject);
        }

        private void HideHands()
        {
            _hands.anchoredPosition = _handsHidden;
            _hands.localScale = Vector3.one;
        }

        private static Tween FadeVolume(Volume volume, float target, float duration) =>
            DOTween.To(() => volume.weight, w => volume.weight = w, target, duration);
    }
}