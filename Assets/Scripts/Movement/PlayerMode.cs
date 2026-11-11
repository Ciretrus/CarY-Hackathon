using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Player
{
    public abstract class PlayerMode : MonoBehaviour
    {
        [SerializeField] bool showCursor;
        [SerializeField] bool closeOnEscape = true;
        [SerializeField] GameObject modeCamera;

        int enterFrame;

        protected PlayerModes Modes { get; private set; }
        protected bool JustEntered => Time.frameCount == enterFrame;

        protected virtual void Awake()
        {
            Modes = GetComponentInParent<PlayerModes>();
            enabled = false;
        }

        public bool Open() => Modes.Enter(this);

        protected void Close() => Modes.Exit();

        public void Activate()
        {
            enterFrame = Time.frameCount;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showCursor;
            if (modeCamera != null) modeCamera.SetActive(true);
            enabled = true;
            OnEnter();
        }

        public void Deactivate()
        {
            enabled = false;
            if (modeCamera != null) modeCamera.SetActive(false);
            OnExit();
        }

        void Update()
        {
            if (JustEntered) return;

            if (closeOnEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            Tick();
        }

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void Tick() { }
    }
}
