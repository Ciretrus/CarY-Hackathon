using UnityEngine;
using UnityEngine.Audio;

namespace Noir.Audio
{
    [RequireComponent(typeof(Collider))]
    public class AudioZone : MonoBehaviour
    {
        [SerializeField] private AudioMixerSnapshot _snapshot;
        [SerializeField] private int _priority;

        public AudioMixerSnapshot Snapshot => _snapshot;
        public int Priority => _priority;
        public float Volume { get; private set; }

        private void Awake()
        {
            Vector3 size = GetComponent<Collider>().bounds.size;
            Volume = size.x * size.y * size.z;
        }
    }
}