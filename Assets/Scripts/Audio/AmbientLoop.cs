using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace Noir.Audio
{
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField] private float _crossfade = 3f;
        [SerializeField] private float _fadeIn = 2f;
        [SerializeField] private bool _randomStart = true;

        private AudioSource[] _sources;
        private int _current;

        private void Awake() => _sources = new[] { CreateSource(), CreateSource() };

        private void OnEnable() => PlayNext(_fadeIn);

        private void OnDisable()
        {
            foreach (var source in _sources)
            {
                source.DOKill();
                source.Stop();
            }
        }

        private void Update()
        {
            var source = _sources[_current];
            if (source.isPlaying && source.time >= _clip.length - _crossfade)
                PlayNext(_crossfade);
        }

        private void PlayNext(float fade)
        {
            var previous = _sources[_current];
            _current = 1 - _current;
            var next = _sources[_current];

            next.DOKill();
            next.volume = 0f;
            next.Play();
            next.time = _randomStart ? Random.Range(0f, Mathf.Max(0f, _clip.length - _crossfade * 2f)) : 0f;
            next.DOFade(_volume, fade);

            previous.DOKill();
            previous.DOFade(0f, fade).OnComplete(previous.Stop);
        }

        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = _clip;
            source.outputAudioMixerGroup = _output;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
