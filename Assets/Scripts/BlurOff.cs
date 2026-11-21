using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurOff : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    private void OnTriggerEnter(Collider other)
    {
        var trigger = other.tag;
        if (trigger == "BlurTrigger")
        {
            _volume.profile.TryGet<DepthOfField>(out var depthOfField);
            if (depthOfField != null)
            {
                depthOfField.active = false;
            }
            Debug.Log("Blur effect disabled.");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        var trigger = other.tag;
        if (trigger == "BlurTrigger")
        {
            _volume.profile.TryGet<DepthOfField>(out var depthOfField);
            if (depthOfField != null)
            {
                depthOfField.active = true;
            }
            Debug.Log("Blur effect enabled.");
        }
    }
}
