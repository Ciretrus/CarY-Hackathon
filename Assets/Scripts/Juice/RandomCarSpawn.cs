using System.Collections;
using UnityEngine;

public class RandomCarSpawn : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float minSpawnDelay = 2f;
    [SerializeField] private float maxSpawnDelay = 5f;
    [SerializeField] private float timeToDestroy = 15f;

    private void Start()
    {
            StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(waitTime);

            var car = Instantiate(prefabToSpawn, transform.position, transform.rotation);
            Destroy(car, timeToDestroy);
        }
    }
}
