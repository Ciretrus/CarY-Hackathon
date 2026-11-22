using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneDebugTrigger : MonoBehaviour
{
    [SerializeField] private DoorCutsceneSequence cutsceneSequence;
    [SerializeField] private Key triggerKey = Key.L;
    [SerializeField] private bool triggerOnce = true;

    private bool _hasTriggered = false;

    private void Update()
    {
        if (triggerOnce && _hasTriggered) return;

        if (Keyboard.current != null && Keyboard.current[triggerKey].wasPressedThisFrame)
        {
            if (cutsceneSequence != null)
            {
                _hasTriggered = true;
                cutsceneSequence.PlayCutscene();
            }
        }
    }
}