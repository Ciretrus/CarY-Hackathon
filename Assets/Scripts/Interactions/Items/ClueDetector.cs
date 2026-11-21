using System;
using System.Collections.Generic;
using UnityEngine;

namespace Noir.Interaction
{
    public class ClueDetector : MonoBehaviour
    {
        [SerializeField] private EvidenceRegistry _registry;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _foundSound;
        [SerializeField] private float _showAngle = 25f;
        [SerializeField] private float _hideAngle = 35f;

        private readonly List<ClueSpot> _spots = new();
        private readonly HashSet<ClueSpot> _announced = new();
        private Transform _view;

        public ClueSpot Current { get; private set; }
        public bool HasRemaining => _spots.Count > 0;
        public event Action<ClueSpot> VisibleChanged;

        private void Awake()
        {
            _view = Camera.main.transform;
            if (_registry == null)
                _registry = FindFirstObjectByType<EvidenceRegistry>(FindObjectsInactive.Include);
        }

        public void Begin(Transform item)
        {
            _spots.Clear();
            _announced.Clear();

            foreach (var spot in item.GetComponentsInChildren<ClueSpot>())
            {
                if (string.IsNullOrWhiteSpace(spot.EvidenceId))
                {
                    Debug.LogWarning($"ClueSpot '{spot.name}': не задан evidenceId", spot);
                    continue;
                }

                if (!_registry.IsFound(spot.EvidenceId))
                    _spots.Add(spot);
            }
        }

        public void Tick() => SetCurrent(FindVisible());

        public bool TryCollect()
        {
            if (Current == null)
                return false;

            var line = Current.FoundLine;
            _registry.Collect(Current.EvidenceId);
            _spots.Remove(Current);
            SetCurrent(null);
            if (line != null && _audio != null)
                _audio.PlayOneShot(line);
            return true;
        }

        public void End()
        {
            _spots.Clear();
            SetCurrent(null);
        }

        private ClueSpot FindVisible()
        {
            if (Current != null && IsFacing(Current, _hideAngle))
                return Current;

            foreach (var spot in _spots)
            {
                if (IsFacing(spot, _showAngle))
                    return spot;
            }

            return null;
        }

        private bool IsFacing(ClueSpot spot, float maxAngle) =>
            Vector3.Angle(spot.transform.forward, _view.position - spot.transform.position) <= maxAngle;

        private void SetCurrent(ClueSpot spot)
        {
            if (spot == Current)
                return;

            Current = spot;
            if (spot != null && _announced.Add(spot))
                PlayFoundCue();

            VisibleChanged?.Invoke(spot);
        }

        public float PlayFoundCue()
        {
            if (_foundSound == null || _audio == null || !_audio.isActiveAndEnabled)
                return 0f;

            _audio.PlayOneShot(_foundSound);
            float pitch = Mathf.Abs(_audio.pitch);
            return pitch > 0f ? _foundSound.length / pitch : 0f;
        }
    }
}
