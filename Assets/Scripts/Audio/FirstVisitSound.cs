using Noir.Player;
using UnityEngine;

namespace Noir.Audio
{
    [RequireComponent(typeof(BoxCollider))]
    public class FirstVisitSound : MonoBehaviour
    {
        [SerializeField] private AudioSource m_Source;
        [SerializeField] private AudioClip m_FirstVisitClip;

        private bool m_Visited;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_Visited || m_Source == null || m_FirstVisitClip == null) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return;

            m_Visited = true;
            m_Source.PlayOneShot(m_FirstVisitClip);
        }
    }
}
