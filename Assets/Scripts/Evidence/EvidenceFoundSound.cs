using UnityEngine;

namespace Noir.Interaction
{
    public class EvidenceFoundSound : MonoBehaviour
    {
        [SerializeField] private EvidenceRegistry _registry;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _clip;

        private void OnEnable() => _registry.FoundChanged += OnFound;

        private void OnDisable()
        {
            if (_registry != null)
                _registry.FoundChanged -= OnFound;
        }

        private void OnFound(BoardEvidence evidence)
        {
            if (_clip != null)
                _audio.PlayOneShot(_clip);
        }
    }
}
