using UnityEngine;
using UnityEngine.Audio;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Manages background music using a zero-GC dual-AudioSource ping-pong architecture with logarithmic crossfades.
    /// </summary>
    public class MusicManager : PersistentSingleton<MusicManager>
    {
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _activeSource;
        private AudioSource _fadingOutSource;

        private bool _isFading;
        private bool _isStopping;
        private bool _isPaused;
        private float _fadeElapsed;
        private float _fadeDuration;
        private float _activeStartVol;
        private float _activeTargetVol = 1f;
        private float _baseTargetVol = 1f;
        private float _fadingOutStartVol;
        private float _masterMusicVolume = 1f;

        public bool IsPlaying => (_activeSource != null && _activeSource.isPlaying) ||
                                 (_fadingOutSource != null && _fadingOutSource.isPlaying);

        internal AudioSource ActiveSource => _activeSource;
        internal AudioSource FadingOutSource => _fadingOutSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            ClearStaticInstance();
        }

        protected override void Awake()
        {
            base.Awake();
            if (instance == this)
            {
                EnsureSources();
            }
        }

        private void Update()
        {
            TickFade(Time.unscaledDeltaTime);
        }

        private void EnsureSources()
        {
            if (_sourceA == null)
            {
                _sourceA = gameObject.AddComponent<AudioSource>();
                ConfigureMusicSource(_sourceA);
            }

            if (_sourceB == null)
            {
                _sourceB = gameObject.AddComponent<AudioSource>();
                ConfigureMusicSource(_sourceB);
            }
        }

        private static void ConfigureMusicSource(AudioSource src)
        {
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = true;
            src.volume = 0f;
        }

        public void Play(
            AudioAsset resource,
            float fadeDuration = 1.0f,
            bool loop = true,
            float targetVolume = 1.0f,
            AudioMixerGroup mixerGroup = null)
        {
            if (resource == null)
                return;

            EnsureSources();

            _baseTargetVol = Mathf.Clamp01(targetVolume);
            float resolvedTargetVol = _baseTargetVol * _masterMusicVolume;
            AudioMixerGroup resolvedMixer = mixerGroup != null
                ? mixerGroup
                : (AudioRegistry.Active != null && AudioRegistry.Active.DefaultMusicMixerGroup != null
                    ? AudioRegistry.Active.DefaultMusicMixerGroup
                    : musicMixerGroup);

            // Continue on the active source if the exact same track is already playing or paused
            if (_activeSource != null &&
                _activeSource.GetAssignedAsset() == resource &&
                (_activeSource.isPlaying || _isPaused))
            {
                _activeSource.loop = loop;
                _activeSource.outputAudioMixerGroup = resolvedMixer;
                _isStopping = false;

                if (fadeDuration <= 0f)
                {
                    _activeSource.volume = resolvedTargetVol;
                    _isFading = false;
                    StopAndClearSource(_fadingOutSource);
                    _fadingOutSource = null;
                }
                else
                {
                    _activeStartVol = _activeSource.volume;
                    _activeTargetVol = resolvedTargetVol;
                    _fadingOutStartVol = _fadingOutSource != null ? _fadingOutSource.volume : 0f;
                    _fadeDuration = fadeDuration;
                    _fadeElapsed = 0f;
                    _isFading = true;
                }
                return;
            }

            // Immediately stop any lingering fading-out source during rapid transitions to prevent zombie audio
            StopAndClearSource(_fadingOutSource);

            _fadingOutSource = _activeSource;
            _activeSource = ReferenceEquals(_fadingOutSource, _sourceA) ? _sourceB : _sourceA;

            _activeSource.AssignAsset(resource);
            _activeSource.outputAudioMixerGroup = resolvedMixer;
            _activeSource.loop = loop;
            _activeSource.spatialBlend = 0f;
            _activeSource.pitch = 1f;

            _isPaused = false;
            _isStopping = false;

            if (fadeDuration <= 0f)
            {
                StopAndClearSource(_fadingOutSource);
                _fadingOutSource = null;
                _activeSource.volume = resolvedTargetVol;
                _activeSource.Play();
                _isFading = false;
            }
            else
            {
                _fadingOutStartVol = _fadingOutSource != null ? _fadingOutSource.volume : 0f;
                _activeStartVol = 0f;
                _activeTargetVol = resolvedTargetVol;
                _fadeDuration = fadeDuration;
                _fadeElapsed = 0f;
                _isFading = true;

                _activeSource.volume = 0f;
                _activeSource.Play();
            }
        }

        public void Stop(float fadeDuration = 1.0f)
        {
            bool activePlaying = _activeSource != null && (_activeSource.isPlaying || _isPaused);
            bool fadingPlaying = _fadingOutSource != null && (_fadingOutSource.isPlaying || _isPaused);

            if (!activePlaying && !fadingPlaying)
            {
                StopAndClearSource(_activeSource);
                StopAndClearSource(_fadingOutSource);
                _activeSource = null;
                _fadingOutSource = null;
                _isFading = false;
                _isStopping = false;
                _isPaused = false;
                return;
            }

            if (fadeDuration <= 0f)
            {
                StopAndClearSource(_activeSource);
                StopAndClearSource(_fadingOutSource);
                _activeSource = null;
                _fadingOutSource = null;
                _isFading = false;
                _isStopping = false;
                _isPaused = false;
                return;
            }

            _activeStartVol = _activeSource != null ? _activeSource.volume : 0f;
            _activeTargetVol = 0f;
            _fadingOutStartVol = _fadingOutSource != null ? _fadingOutSource.volume : 0f;
            _fadeDuration = fadeDuration;
            _fadeElapsed = 0f;
            _isStopping = true;
            _isFading = true;
        }

        public void Pause()
        {
            _isPaused = true;
            if (_activeSource != null && _activeSource.isPlaying)
                _activeSource.Pause();
            if (_fadingOutSource != null && _fadingOutSource.isPlaying)
                _fadingOutSource.Pause();
        }

        public void Resume()
        {
            _isPaused = false;
            if (_activeSource != null && _activeSource.GetAssignedAsset() != null)
                _activeSource.UnPause();
            if (_fadingOutSource != null && _fadingOutSource.GetAssignedAsset() != null)
                _fadingOutSource.UnPause();
        }

        public void SetVolume(float volume)
        {
            _masterMusicVolume = Mathf.Clamp01(volume);
            if (_isFading)
            {
                if (!_isStopping)
                {
                    _activeTargetVol = _baseTargetVol * _masterMusicVolume;
                }
            }
            else if (_activeSource != null && (_activeSource.isPlaying || _isPaused))
            {
                _activeSource.volume = _baseTargetVol * _masterMusicVolume;
            }
        }

        internal void TickFade(float deltaTime)
        {
            if (!_isFading || _isPaused)
                return;

            _fadeElapsed += Mathf.Max(0f, deltaTime);
            float linearT = _fadeDuration > 0f ? Mathf.Clamp01(_fadeElapsed / _fadeDuration) : 1f;
            float fadeInScale = linearT.ToLogarithmicFraction();
            float fadeOutScale = (1f - linearT).ToLogarithmicFraction();

            if (_isStopping)
            {
                if (_activeSource != null)
                    _activeSource.volume = _activeStartVol * fadeOutScale;
                if (_fadingOutSource != null)
                    _fadingOutSource.volume = _fadingOutStartVol * fadeOutScale;
            }
            else
            {
                if (_activeSource != null)
                    _activeSource.volume = Mathf.Lerp(_activeStartVol, _activeTargetVol, fadeInScale);
                if (_fadingOutSource != null)
                    _fadingOutSource.volume = _fadingOutStartVol * fadeOutScale;
            }

            if (linearT >= 1f)
            {
                _isFading = false;
                if (_isStopping)
                {
                    StopAndClearSource(_activeSource);
                    StopAndClearSource(_fadingOutSource);
                    _activeSource = null;
                    _fadingOutSource = null;
                    _isStopping = false;
                    _isPaused = false;
                }
                else
                {
                    if (_activeSource != null)
                        _activeSource.volume = _activeTargetVol;
                    StopAndClearSource(_fadingOutSource);
                    _fadingOutSource = null;
                }
            }
        }

        private static void StopAndClearSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.volume = 0f;
            source.AssignAsset(null);
            source.outputAudioMixerGroup = null;
        }
    }
}
