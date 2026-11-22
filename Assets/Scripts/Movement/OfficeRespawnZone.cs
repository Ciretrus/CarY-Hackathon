using UnityEngine;

namespace Noir.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class OfficeRespawnZone : MonoBehaviour
    {
        [SerializeField] private Transform m_OfficeSpawn;

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            if (m_OfficeSpawn == null)
                Debug.LogWarning("OfficeRespawnZone: назначь точку возврата в офис.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_OfficeSpawn == null) return;

            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null)
                player.Warp(m_OfficeSpawn.position);
        }
    }
}
