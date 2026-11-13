using UnityEngine;

namespace Noir.Interaction
{
    public class ClueSpot : MonoBehaviour
    {
        [SerializeField] private string _evidenceId;
        [SerializeField] private AudioClip _foundLine;

        public string EvidenceId => _evidenceId;
        public AudioClip FoundLine => _foundLine;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.01f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.1f);
        }
    }
}
