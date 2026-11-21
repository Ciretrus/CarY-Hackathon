using DG.Tweening;
using Noir.Interaction;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Noir.Photo
{
    public class CameraMode : PlayerMode
    {
        private enum State { Raising, Shooting, Reviewing, Lowering }

        [SerializeField] private CameraHud _hud;
        [SerializeField] private PhotoCapture _capture;
        [SerializeField] private EvidenceRegistry _registry;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _shutterSound;
        [SerializeField] private LayerMask _occluders = ~0;

        [SerializeField] private MouseWheelZoom _zoom = new();

        private State _state;

        protected override void Awake()
        {
            base.Awake();

            if (_registry == null)
                _registry = FindFirstObjectByType<EvidenceRegistry>(FindObjectsInactive.Include);
        }

        protected override void OnEnter()
        {
            _zoom.Begin(_capture.Camera);
            _state = State.Raising;
            _hud.Raise(_capture.Begin).OnComplete(() => _state = State.Shooting);
        }

        protected override void Tick()
        {
            if (_state != State.Shooting)
                return;

            var mouse = Mouse.current;

            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _state = State.Lowering;
                _hud.Lower(_capture.End).OnComplete(Close);
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
                Shoot();
            else
                _zoom.Tick(mouse);
        }

        protected override void OnExit()
        {
            _zoom.End();
            _capture.End();
            _hud.ResetAll();
        }

        private void Shoot()
        {
            _state = State.Reviewing;

            var photo = _capture.Capture();
            var clue = PhotoClue.FindInShot(_capture.Camera, _capture.SquareViewport, _registry, _occluders);

            if (_shutterSound != null)
                _audio.PlayOneShot(_shutterSound);

            bool isEvidence = clue != null && _registry.Collect(clue.EvidenceId, photo);
            if (isEvidence && clue.FoundLine != null)
                _audio.PlayOneShot(clue.FoundLine);

            _hud.Shot(photo).OnComplete(() =>
            {
                if (!isEvidence)
                    Destroy(photo);
                _state = State.Shooting;
            });
        }
    }
}