using DG.Tweening;
using Noir.Player;
using Noir.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Interaction
{
    public class LocationExit : Interactable
    {
        [Header("Teleport")]
        [SerializeField] private FlashbackScreen _flashback;
        [SerializeField] private Transform _officeSpawn;
        [SerializeField] private PlayerMovement _movement;

        [Header("Highlight")]
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField] private bool _useLeftClick = true;

        private Tween _tween;
        private bool _isHovered;
        private CinemachineBrain _brain;
        private PlayerModes _modes;
        private float? _savedBlend;

        void Awake()
        {
            if (_movement == null)
                _movement = FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            _modes = FindFirstObjectByType<PlayerModes>(FindObjectsInactive.Include);

            var cam = Camera.main;
            if (cam != null)
                _brain = cam.GetComponent<CinemachineBrain>();

            SetAlpha(0f);
        }

        void OnDisable()
        {
            _tween?.Kill();
            _isHovered = false;
            SetAlpha(0f);
        }

        public override void HoverEnter()
        {
            base.HoverEnter();
            _isHovered = true;
            Refresh();
        }

        public override void HoverExit()
        {
            base.HoverExit();
            _isHovered = false;
            Refresh();
        }

        public override void Interact(Interactor interactor)
        {
            base.Interact(interactor);
            TryExit();
        }

        void Update()
        {
            if (!_useLeftClick || !_isHovered)
                return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            TryExit();
        }

        void TryExit()
        {
            if (_modes != null && _modes.Current != null)
                return;
            if (_flashback == null || _flashback.IsPlaying)
                return;

            if (_officeSpawn == null || _movement == null || _brain == null)
            {
                Debug.LogError($"LocationExit '{name}': не назначены spawn / movement / brain", this);
                return;
            }

            _flashback.Play(OnWhite, OnFlashbackDone);
        }

        void OnWhite()
        {
            var blend = _brain.DefaultBlend;
            _savedBlend = blend.Time;
            blend.Time = 0f;
            _brain.DefaultBlend = blend;

            _movement.Warp(_officeSpawn.position);
        }

        void OnFlashbackDone()
        {
            if (_savedBlend is not float time)
                return;

            var blend = _brain.DefaultBlend;
            blend.Time = time;
            _brain.DefaultBlend = blend;
            _savedBlend = null;
        }

        void Refresh()
        {
            _tween?.Kill();
            if (_glow == null)
                return;
            _tween = _glow.DOFade(_isHovered ? _hoverAlpha : 0f, _duration)
                          .SetEase(_ease).SetLink(gameObject);
        }

        void SetAlpha(float alpha)
        {
            if (_glow == null)
                return;
            var c = _glow.color;
            c.a = alpha;
            _glow.color = c;
        }
    }
}