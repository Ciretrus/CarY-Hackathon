using Unity.Cinemachine;
using UnityEngine;

namespace Noir.Player
{
    [RequireComponent(typeof(CinemachineInputAxisController))]
    public class PlayerLook : MonoBehaviour
    {
        const string SensitivityKey = "MouseSensitivity";

        [SerializeField] float sensitivity = 0.1f;

        CinemachineInputAxisController axisController;

        public float Sensitivity
        {
            get => sensitivity;
            set
            {
                sensitivity = value;
                PlayerPrefs.SetFloat(SensitivityKey, value);
                ApplySensitivity();
            }
        }

        void Awake()
        {
            axisController = GetComponent<CinemachineInputAxisController>();
            sensitivity = PlayerPrefs.GetFloat(SensitivityKey, sensitivity);
            ApplySensitivity();
        }

        void OnEnable()
        {
            axisController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            if (axisController != null) axisController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void ApplySensitivity()
        {
            foreach (var c in axisController.Controllers)
                c.Input.Gain = Mathf.Sign(c.Input.Gain) * sensitivity;
        }
    }
}
