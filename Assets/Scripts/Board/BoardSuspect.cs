using System.Collections.Generic;
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

        [SerializeField] private BoardHighlight _highlight;

        public string SuspectName => _suspectName;
        public string[] RequiredIds => _requiredIds;

        public override void HoverEnter()
        {
            base.HoverEnter();
            if (_highlight)
                _highlight.SetHovered(true);
        }

        public override void HoverExit()
        {
            base.HoverExit();
            if (_highlight)
                _highlight.SetHovered(false);
        }

        public void SetSelected(bool value)
        {
            if (_highlight)
                _highlight.SetSelected(value);
        }

        public void TriggerSolved(AudioSource audio)
        {
            if (audio != null && _solvedLine != null)
                audio.PlayOneShot(_solvedLine);
            onSolved?.Invoke();
        }

        void OnValidate()
        {
            if (_requiredIds == null)
                return;
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