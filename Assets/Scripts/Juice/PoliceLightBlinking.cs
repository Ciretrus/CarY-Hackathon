using System.Collections;
using UnityEngine;

public class PoliceLightBlinking : MonoBehaviour
{
    [SerializeField] private GameObject blue;
    [SerializeField] private GameObject red;
    [SerializeField] private float timeToBlink = 1.5f;
    [SerializeField] private float startDelay = 1f;

    private void Start()
    {
            StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        while (true)
        {
            yield return new WaitForSeconds(timeToBlink);
            blue.SetActive(true);
            red.SetActive(false);
            yield return new WaitForSeconds(timeToBlink);
            blue.SetActive(false);
            red.SetActive(true);
        }
    }
}
