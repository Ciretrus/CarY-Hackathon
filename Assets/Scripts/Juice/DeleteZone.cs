using UnityEngine;

public class DeleteZone : MonoBehaviour
{
    [SerializeField] private LayerMask carLayer;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == carLayer)
        {
            Destroy(other.gameObject);
        }
     }
}
