using UnityEngine;
using UnityEngine.Audio;

namespace Noir.Audio
{
    public class AudioZoneTracker : MonoBehaviour
    {
        [SerializeField] private AudioMixerSnapshot _outdoor;
        [SerializeField] private LayerMask _zoneLayers = 1 << 2;
        [SerializeField] private Vector3 _offset = new(0f, 1f, 0f);
        [SerializeField] private float _transition = 1f;

        private readonly Collider[] _hits = new Collider[16];
        private AudioMixerSnapshot _current;

        private void Update()
        {
            var zone = FindBest();
            var snapshot = zone != null ? zone.Snapshot : _outdoor;
            if (snapshot == _current)
                return;

            snapshot.TransitionTo(_current == null ? 0f : _transition);
            _current = snapshot;
        }

        private AudioZone FindBest()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position + _offset, 0.1f, _hits, _zoneLayers, QueryTriggerInteraction.Collide);

            AudioZone best = null;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].TryGetComponent(out AudioZone zone) && (best == null || IsBetter(zone, best)))
                    best = zone;
            }

            return best;
        }

        private static bool IsBetter(AudioZone zone, AudioZone other) =>
            zone.Priority != other.Priority ? zone.Priority > other.Priority : zone.Volume < other.Volume;
    }
}