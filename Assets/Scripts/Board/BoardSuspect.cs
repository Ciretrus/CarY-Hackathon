using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Noir.Interaction
{
    public class BoardSuspect : Interactable
    {
        [SerializeField] private string _suspectName;
        [SerializeField] private string[] _requiredIds = new string[5];
        [SerializeField] private AudioClip _solvedLine;
        [SerializeField] private UnityEvent onSolved = new();

        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private float _hoverAlpha = 0.3f;
        [SerializeField] private float _selectedAlpha = 1f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        private BoardHighlight _highlight;
        private Tween _tween;
        private bool _isHovered, _isSelected;

        public AudioClip SolvedLine => _solvedLine;
        public string SuspectName => _suspectName;
        public string[] RequiredIds => _requiredIds;

        void Awake() => _highlight = GetComponent<BoardHighlight>();

        void OnEnable() { _isHovered = false; _isSelected = false; SetAlpha(0f); }
        void OnDisable()
        {
            _tween?.Kill();
            _isHovered = _isSelected = false;
            if (_highlight != null && _highlight.isActiveAndEnabled)
            {
                _highlight.SetHovered(false);
                _highlight.SetSelected(false);
            }
            SetAlpha(0f);
        }

        public override void HoverEnter() { base.HoverEnter(); _isHovered = true; Refresh(); }
        public override void HoverExit() { base.HoverExit(); _isHovered = false; Refresh(); }

        public void SetSelected(bool value)
        {
            if (_isSelected == value) return;
            _isSelected = value;
            Refresh();
        }

        public void TriggerSolved(AudioSource audio)
        {
            if (audio != null && _solvedLine != null) audio.PlayOneShot(_solvedLine);
            onSolved?.Invoke();
        }

        void Refresh()
        {
            _tween?.Kill();
            if (_highlight != null)
            {
                _highlight.SetHovered(_isHovered);
                _highlight.SetSelected(_isSelected);
            }
            if (_glow == null) return;

            float target = _isSelected ? _selectedAlpha : _isHovered ? _hoverAlpha : 0f;
            _tween = _glow.DOFade(target, _duration).SetEase(_ease).SetLink(gameObject);
        }

        void SetAlpha(float a)
        {
            if (_glow == null) return;
            var c = _glow.color;
            c.a = a;
            _glow.color = c;
        }

        void OnValidate()
        {
            if (_requiredIds == null) return;
            if (_requiredIds.Length != 5)
                Debug.LogWarning($"Suspect '{_suspectName}': нужно 5 id, сейчас {_requiredIds.Length}", this);

            var seen = new HashSet<string>();
            foreach (var id in _requiredIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    Debug.LogWarning($"Suspect '{_suspectName}': пустой id", this);
                else if (!seen.Add(id))
                    Debug.LogWarning($"Suspect '{_suspectName}': id '{id}' повторяется", this);
            }
        }
    }
}