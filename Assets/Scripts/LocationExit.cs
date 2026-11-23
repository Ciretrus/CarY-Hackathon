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
        [SerializeField] private BoardHighlight _highlight;
        [SerializeField] private bool _useLeftClick = true;

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
        }

        void OnDisable()
        {
            _isHovered = false;
        }

        public override void HoverEnter()
        {
            base.HoverEnter();
            _isHovered = true;
            if (_highlight) _highlight.SetHovered(true);
        }

        public override void HoverExit()
        {
            base.HoverExit();
            _isHovered = false;
            if (_highlight) _highlight.SetHovered(false);
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
    }
}