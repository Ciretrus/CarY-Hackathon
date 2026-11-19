using UnityEngine;

public class CarMove : MonoBehaviour
{
    [SerializeField] private Vector3 speed = new Vector3(5,0,0);
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(speed * Time.deltaTime);
    }
}
