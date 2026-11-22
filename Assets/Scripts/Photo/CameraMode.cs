using System.Collections;
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
        [SerializeField, HideInInspector] private AudioClip _noEvidenceSound;
        [SerializeField] private AudioClip[] _noEvidenceSounds;
        [SerializeField, HideInInspector] private AudioClip _alreadyInDossierSound;
        [SerializeField] private AudioClip[] _alreadyInDossierSounds;
        [SerializeField] private AudioClip _zoomSound;
        [SerializeField, Range(0f, 1f)] private float _zoomVolume = 0.55f;
        [SerializeField] private LayerMask _occluders = ~0;

        [SerializeField] private MouseWheelZoom _zoom = new();

        private ClueDetector _detector;
        private State _state;
        private bool _lowerRequested;

        protected override void Awake()
        {
            base.Awake();
            _detector = GetComponent<ClueDetector>();

            if (_registry == null)
                _registry = FindFirstObjectByType<EvidenceRegistry>(FindObjectsInactive.Include);
        }

        protected override void OnEnter()
        {
            _lowerRequested = false;
            _zoom.Begin(_capture.Camera);
            _state = State.Raising;
            _hud.Raise(_capture.Begin).OnComplete(() => _state = State.Shooting);
        }

        protected override void Tick()
        {
            if (_state == State.Lowering)
                return;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if ((keyboard != null && keyboard.rKey.wasPressedThisFrame) ||
                (mouse != null && mouse.rightButton.wasPressedThisFrame))
                _lowerRequested = true;

            if (_state != State.Shooting)
                return;

            if (_lowerRequested)
            {
                _state = State.Lowering;
                _zoom.End();
                _hud.Lower(_capture.End).OnComplete(Close);
                return;
            }

            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
                Shoot();
            else if (_zoom.Tick(mouse) && _zoomSound != null && _audio != null)
                _audio.PlayOneShot(_zoomSound, _zoomVolume);
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
            var clue = PhotoClue.FindInShot(_capture.Camera, _capture.SquareViewport, _registry, _occluders, out bool foundKnownClue);

            if (_shutterSound != null && _audio != null)
                _audio.PlayOneShot(_shutterSound);

            bool isEvidence = clue != null && _registry.Collect(clue.EvidenceId, photo);
            if (isEvidence)
            {
                float delay = _detector != null ? _detector.PlayFoundCue() : 0f;
                if (clue.FoundLine != null && _audio != null)
                    StartCoroutine(PlayPhotoLine(clue.FoundLine, delay));
            }

            if (!isEvidence && _audio != null && _registry != null)
            {
                AudioClip resultSound = foundKnownClue
                    ? PickClip(_alreadyInDossierSounds, _alreadyInDossierSound)
                    : PickClip(_noEvidenceSounds, _noEvidenceSound);
                if (resultSound != null)
                    _audio.PlayOneShot(resultSound);
            }

            _hud.Shot(photo).OnComplete(() =>
            {
                if (!isEvidence)
                    Destroy(photo);
                _state = State.Shooting;
            });
        }

        private static AudioClip PickClip(AudioClip[] clips, AudioClip previousClip)
        {
            AudioClip selected = previousClip;
            int count = 0;
            if (clips == null) return selected;

            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;
                count++;
                if (Random.Range(0, count) == 0)
                    selected = clip;
            }
            return selected;
        }

        private IEnumerator PlayPhotoLine(AudioClip line, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            if (_audio != null && _audio.isActiveAndEnabled)
                _audio.PlayOneShot(line);
        }
    }
}
