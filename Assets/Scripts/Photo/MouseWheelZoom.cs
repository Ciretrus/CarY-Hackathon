using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Player
{
    [System.Serializable]
    public sealed class MouseWheelZoom
    {
        [SerializeField, Min(1f)] private float maxMagnification = 3f;
        [SerializeField, Min(0.01f)] private float step = 0.25f;

        private Camera view;
        private CinemachineBrain brain;
        private CinemachineCamera live;
        private bool applied;
        private float initialFov;
        private float initialSize;
        private float magnification;

        public void Begin(Camera camera)
        {
            End();
            if (camera == null) return;

            view = camera;
            brain = camera.GetComponent<CinemachineBrain>();
            magnification = 1f;
        }

        public bool Tick(Mouse mouse)
        {
            if (view == null || mouse == null) return false;
            float scroll = mouse.scroll.ReadValue().y;
            var next = brain != null && brain.isActiveAndEnabled
                ? brain.ActiveVirtualCamera as CinemachineCamera : null;
            bool changed = next != live;
            if (changed)
            {
                Restore();
                live = next;
            }

            if (live == null && brain != null && brain.isActiveAndEnabled && brain.ActiveVirtualCamera != null)
                return false;

            float previous = magnification;
            if (scroll != 0f)
                magnification = Mathf.Clamp(magnification + Mathf.Sign(scroll) * step, 1f, maxMagnification);
            bool stepped = magnification != previous;
            if (!stepped && (!changed || magnification == 1f))
                return false;

            if (!applied)
            {
                initialFov = live != null ? live.Lens.FieldOfView : view.fieldOfView;
                initialSize = live != null ? live.Lens.OrthographicSize : view.orthographicSize;
                applied = true;
            }

            float halfAngle = initialFov * Mathf.Deg2Rad * 0.5f;
            float fov = 2f * Mathf.Atan(Mathf.Tan(halfAngle) / magnification) * Mathf.Rad2Deg;
            SetLens(fov, initialSize / magnification);
            return stepped;
        }

        private void SetLens(float fov, float size)
        {
            if (live != null)
            {
                live.Lens.FieldOfView = fov;
                live.Lens.OrthographicSize = size;
            }
            else if (view != null)
            {
                if (view.orthographic) view.orthographicSize = size;
                else view.fieldOfView = fov;
            }
        }

        private void Restore()
        {
            if (applied) SetLens(initialFov, initialSize);
            applied = false;
        }

        public void End()
        {
            Restore();
            live = null;
            brain = null;
            view = null;
        }
    }
}
