using System;
using Noir.Interaction;
using Noir.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Player
{
    public class BoardMode : PlayerMode
    {
        private enum State { Idle, Entering, Browsing, Transitioning, Exiting }

        [SerializeField] private LayerMask _pinMask;
        [SerializeField] private float _rayDistance = 50f;
        [SerializeField] private CinemachineCamera _boardCamera;
        [SerializeField] private FlashbackScreen _flashback;
        [SerializeField] private GameObject _boardHud;

        private State _state;
        private Board _board;
        private BoardLocation _hovered;
        private BoardLocation _selected;
        private Camera _cam;
        private CinemachineBrain _brain;
        private PlayerMovement _movement;
        private float? _savedBlendTime;

        public event Action<BoardLocation> SelectionChanged;

        protected override void Awake()
        {
            base.Awake();
            _cam = Camera.main;
            _brain = _cam.GetComponent<CinemachineBrain>();
            _movement = GetComponentInParent<PlayerMovement>();
            if (_movement == null) _movement = FindFirstObjectByType<PlayerMovement>();
        }

        public bool Open(Board target)
        {
            if (Modes.Current != null || _flashback.IsPlaying)
                return false;

            _board = target;
            return Open();
        }

        protected override void OnEnter()
        {
            var anchor = _board.ViewAnchor;
            _boardCamera.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            _boardHud.SetActive(true);
            _state = State.Entering;
        }

        protected override void Tick()
        {
            if (_state == State.Entering)
            {
                if (!_brain.IsBlending) _state = State.Browsing;
                return;
            }

            if (_state != State.Browsing)
                return;

            UpdateHover();
            ReadButtons();
        }
        protected override void OnExit()
        {
            ClearSelection();
            _boardHud.SetActive(false);
            _state = State.Idle;
            _board = null;
        }

        void UpdateHover()
        {
            BoardLocation found = null;
            var ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _pinMask))
            {
                found = hit.collider.GetComponentInParent<BoardLocation>();
                if (found != null && !found.isActiveAndEnabled)
                    found = null;
            }
            SetHovered(found);
        }

        void SetHovered(BoardLocation next)
        {
            if (next == _hovered)
                return;

            if (_hovered != null) 
                _hovered.HoverExit();

            _hovered = next;
            if (_hovered != null)
                _hovered.HoverEnter();
        }

        void ReadButtons()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) 
                return;

            if (mouse.rightButton.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                BeginExit();
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && _hovered != null)
                Select(_hovered);

            if (keyboard.eKey.wasPressedThisFrame && _selected != null)
                BeginFlashback();
        }

        void Select(BoardLocation location)
        {
            if (location == _selected)
                return;

            if (_selected != null) 
                _selected.SetSelected(false);

            _selected = location;
            if (_selected != null) 
                _selected.SetSelected(true);

            SelectionChanged?.Invoke(_selected);
        }

        void ClearSelection()
        {
            SetHovered(null);
            Select(null);
        }

        void BeginExit()
        {
            _state = State.Exiting;
            ClearSelection();
            Close();
        }

        void BeginFlashback()
        {
            if (_flashback.IsPlaying) 
                return;

            if (_selected?.Spawn == null)
            {
                Debug.LogError($"BoardLocation '{_selected?.name}': spawnPoint не назначен", _selected);
                return;
            }

            _state = State.Transitioning;
            _flashback.Play(OnWhite, OnFlashbackDone);
        }
        void OnWhite()
        {
            if (Modes.Current != this) 
                return;

            var spawn = _selected != null ? _selected.Spawn : null;
            if (spawn == null)
            {
                _state = State.Browsing;
                return;
            }

            ClearSelection();
            var blend = _brain.DefaultBlend;
            _savedBlendTime = blend.Time;
            blend.Time = 0f;
            _brain.DefaultBlend = blend;
            _movement.Warp(spawn.position);
            Close();
        }

        void OnFlashbackDone()
        {
            if (_savedBlendTime is not float time) 
                return;

            var blend = _brain.DefaultBlend;
            blend.Time = time;
            _brain.DefaultBlend = blend;
            _savedBlendTime = null;
        }
    }
}