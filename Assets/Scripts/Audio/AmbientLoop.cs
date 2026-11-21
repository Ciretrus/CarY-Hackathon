using UnityEngine;
using UnityEngine.Audio;

namespace Noir.Audio
{
    [DisallowMultipleComponent]
    public class AmbientLoop : MonoBehaviour
    {
        [Header("Ambient")]
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField, Min(0f)] private float _fadeIn = 2f;
        [SerializeField] private bool _randomStart = true;

        [Header("2D Distance Zone")]
        [Tooltip("Включает 2D-звук в радиусе Max Distance у AudioSource.")]
        [SerializeField] private bool _use2DDistanceZone = true;

        [Tooltip("Время полного затухания при выходе и появления при возвращении.")]
        [SerializeField, Min(0f)] private float _zoneFadeDuration = .5f;

        private AudioSource _source;
        private AudioListener _listener;

        private float _baseVolume;
        private float _originalSpatialBlend;
        private float _startupGain;
        private float _zoneGain;
        private float _nextListenerSearch;
        private bool _zoneModeApplied;

        private void Awake()
        {
            if (!TryGetComponent(out _source))
            {                
                _source = gameObject.AddComponent<AudioSource>();                
            }

            if (_source.clip == null)
                _source.clip = _clip;

            if (_source.outputAudioMixerGroup == null)
                _source.outputAudioMixerGroup = _output;

            _source.playOnAwake = false;
            _source.loop = true;
            _source.volume = _volume;

            _baseVolume = _source.volume;
            _originalSpatialBlend = _source.spatialBlend;

            FindListener();
        }

        private void OnEnable()
        {
            if (_source.clip == null)
            {
                Debug.LogWarning("AmbientLoop: назначь клип в AudioSource или AmbientLoop.", this);
                return;
            }

            ApplyZoneMode();
            _startupGain = _fadeIn > 0f ? 0f : 1f;
            _zoneGain = GetTargetZoneGain();

            _source.Stop();
            _source.volume = _baseVolume * _startupGain * _zoneGain;
            _source.timeSamples = _randomStart
                ? Random.Range(0, _source.clip.samples)
                : 0;

            _source.Play();
        }

        private void LateUpdate()
        {
            if (_zoneModeApplied != _use2DDistanceZone)
                ApplyZoneMode();

            float delta = Time.unscaledDeltaTime;

            _startupGain = _fadeIn > 0f
                ? Mathf.MoveTowards(_startupGain, 1f, delta / _fadeIn)
                : 1f;

            float target = GetTargetZoneGain();
            _zoneGain = _use2DDistanceZone && _zoneFadeDuration > 0f
                ? Mathf.MoveTowards(_zoneGain, target, delta / _zoneFadeDuration)
                : target;

            _source.volume = _baseVolume * _startupGain * _zoneGain;
        }

        private void ApplyZoneMode()
        {
            _zoneModeApplied = _use2DDistanceZone;
            _source.spatialBlend = _use2DDistanceZone ? 0f : _originalSpatialBlend;
        }

        private float GetTargetZoneGain()
        {
            if (!_use2DDistanceZone)
                return 1f;

            if (_listener == null || !_listener.isActiveAndEnabled)
            {
                if (Time.unscaledTime >= _nextListenerSearch)
                    FindListener();

                if (_listener == null || !_listener.isActiveAndEnabled)
                    return 0f;
            }

            Vector3 offset = _listener.transform.position - _source.transform.position;
            float radius = _source.maxDistance;

            return offset.sqrMagnitude <= radius * radius ? 1f : 0f;
        }

        private void FindListener()
        {
            _nextListenerSearch = Time.unscaledTime + 1f;

            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener.isActiveAndEnabled)
                {
                    _listener = listener;
                    return;
                }
            }

            _listener = null;
        }

        private void OnDisable()
        {
            _source.Stop();
            _source.volume = _baseVolume;
            _source.spatialBlend = _originalSpatialBlend;
        }
    }
}