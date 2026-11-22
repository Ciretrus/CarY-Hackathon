using Noir.Interaction;
using Noir.Player;
using UnityEngine;

namespace Noir.Audio
{
    [RequireComponent(typeof(BoxCollider))]
    public class OfficeEvidenceSound : MonoBehaviour
    {
        private const int RequiredClues = 9;

        [SerializeField] private EvidenceRegistry m_Registry;
        [SerializeField] private AudioSource m_Source;
        [SerializeField] private AudioClip m_OfficeLine;

        private bool m_Played;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            if (m_Registry == null)
                m_Registry = FindFirstObjectByType<EvidenceRegistry>();

            if (m_Registry == null || m_Source == null || m_OfficeLine == null)
                Debug.LogWarning("OfficeEvidenceSound: проверь Registry, Source и Office Line.", this);
        }

        private void OnTriggerEnter(Collider other) => TryPlay(other);

        private void OnTriggerStay(Collider other) => TryPlay(other);

        private void TryPlay(Collider other)
        {
            if (!isActiveAndEnabled || m_Played || m_Registry == null ||
                !m_Registry.isActiveAndEnabled || m_Source == null ||
                !m_Source.isActiveAndEnabled || m_OfficeLine == null)
                return;

            if (other.GetComponentInParent<PlayerMovement>() == null ||
                m_Registry.FoundCount < RequiredClues)
                return;

            m_Played = true;
            m_Source.PlayOneShot(m_OfficeLine);
        }
    }
}
