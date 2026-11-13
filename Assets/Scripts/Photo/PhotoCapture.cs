using UnityEngine;
using UnityEngine.UI;

namespace Noir.Photo
{
    public class PhotoCapture : MonoBehaviour
    {
        [SerializeField] private RawImage _screen;
        [SerializeField] private Camera _screenCamera;
        [SerializeField] private int _height = 240;

        private Camera _camera;
        private RenderTexture _target;

        public Camera Camera => _camera;

        public Rect SquareViewport
        {
            get
            {
                float width = (float)_target.height / _target.width;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
        }

        private void Awake()
        {
            _camera = Camera.main;
            if (_screenCamera == null)
                _screenCamera = CreateScreenCamera();
            _screenCamera.enabled = false;
        }

        private Camera CreateScreenCamera()
        {
            var screenCamera = new GameObject("ScreenCamera").AddComponent<Camera>();
            screenCamera.transform.SetParent(transform, false);
            screenCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCamera.backgroundColor = Color.black;
            screenCamera.cullingMask = 0;
            screenCamera.depth = _camera.depth + 10f;
            return screenCamera;
        }

        private void OnDestroy()
        {
            if (_target != null)
                _target.Release();
        }

        public void Begin()
        {
            if (_target == null)
            {
                int width = Mathf.RoundToInt(_height * (float)Screen.width / Screen.height);
                _target = new RenderTexture(width, _height, 24) { filterMode = FilterMode.Point };
            }

            _camera.targetTexture = _target;
            _screen.texture = _target;
            _screenCamera.enabled = true;
        }

        public void End()
        {
            _camera.targetTexture = null;
            _screenCamera.enabled = false;
        }

        public Texture2D Capture()
        {
            int size = _target.height;
            int x = (_target.width - size) / 2;

            var previous = RenderTexture.active;
            RenderTexture.active = _target;

            var photo = new Texture2D(size, size, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
            photo.ReadPixels(new Rect(x, 0, size, size), 0, 0);
            photo.Apply();

            RenderTexture.active = previous;
            return photo;
        }
    }
}