using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Player
{
    public class ModeHotkey : MonoBehaviour
    {
        [SerializeField] private InputActionReference _action;
        [SerializeField] private PlayerMode _mode;

        private void OnEnable() => _action.action.Enable();

        private void Update()
        {
            if (_action.action.WasPressedThisFrame())
                _mode.Open();
        }
    }
}
