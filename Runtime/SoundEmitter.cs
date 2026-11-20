using UnityEngine;
using UnityEngine.Audio;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        private AudioSource _audioSource;
        private uint _generation;
        private bool _isInPool = true;
        private bool _isLooping;
        private bool _isPaused;
        private bool _hasStartedPlaying;
        private string _trackingKey;
        private Transform _followTarget;

        public bool IsPlaying => !_isInPool && _audioSource != null && _audioSource.isPlaying;
        internal string TrackingKey => _trackingKey;
        internal bool IsInPool => _isInPool;

        internal bool IsGenerationActive(uint generation) => !_isInPool && generation != 0 && _generation == generation;

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null)
                return;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }

        internal SoundHandle Play(
            AudioAsset resource,
            Vector3 position,
            float volume = 1f,
            float spatialBlend = 1f,
            float pitch = 1f,
            AudioMixerGroup mixerGroup = null,
            bool loop = false,
            float minDistance = 1f,
            float maxDistance = 50f,
            string trackingKey = null)
        {
            EnsureAudioSource();

            _isInPool = false;
            unchecked
            {
                _generation++;
                if (_generation == 0)
                    _generation = 1;
            }

            float safeMinDist = Mathf.Max(0.01f, minDistance);
            float safeMaxDist = Mathf.Max(safeMinDist, maxDistance);

            transform.position = position;
            _audioSource.AssignAsset(resource);
            _audioSource.volume = Mathf.Clamp01(volume);
            _audioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
            _audioSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
            _audioSource.minDistance = safeMinDist;
            _audioSource.maxDistance = safeMaxDist;
            _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _audioSource.panStereo = 0f;
            _audioSource.dopplerLevel = 1f;
            _audioSource.priority = 128;
            _audioSource.outputAudioMixerGroup = mixerGroup;
            _audioSource.loop = loop;

            _isPaused = false;
            _followTarget = null;
            _isLooping = loop;
            _trackingKey = trackingKey;

            _audioSource.Play();
            _hasStartedPlaying = true;

            return new SoundHandle(this, _generation);
        }

        internal bool Tick(bool globalPaused)
        {
            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }

            if (_isLooping || _isPaused || globalPaused || AudioListener.pause)
                return true;

            if (_hasStartedPlaying && _audioSource != null && !_audioSource.isPlaying)
                return false;

            return true;
        }

        public void Stop()
        {
            if (_isInPool)
                return;

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            SoundManager manager = SoundManager.TryGetInstance();
            if (manager != null)
            {
                manager.ReturnToPool(this);
            }
            else
            {
                OnReturnedToPoolCleanup();
            }
        }

        public void Pause()
        {
            if (_isInPool || _audioSource == null)
                return;

            _isPaused = true;
            _audioSource.Pause();
        }

        public void Resume()
        {
            if (_isInPool || _audioSource == null)
                return;

            _isPaused = false;
            _audioSource.UnPause();
        }

        public void SetVolume(float volume)
        {
            if (!_isInPool && _audioSource != null)
            {
                _audioSource.volume = Mathf.Clamp01(volume);
            }
        }

        public void SetPitch(float pitch)
        {
            if (!_isInPool && _audioSource != null)
            {
                _audioSource.pitch = Mathf.Clamp(pitch, -3f, 3f);
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (!_isInPool)
            {
                transform.position = position;
            }
        }

        public void Follow(Transform target)
        {
            if (!_isInPool)
            {
                _followTarget = target;
            }
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            if (!_isInPool && _audioSource != null)
            {
                _audioSource.pitch = Mathf.Clamp(_audioSource.pitch + UnityEngine.Random.Range(min, max), -3f, 3f);
            }
        }

        internal void OnReturnedToPoolCleanup()
        {
            unchecked
            {
                _generation++;
            }

            _isInPool = true;
            _hasStartedPlaying = false;
            _isLooping = false;
            _isPaused = false;
            _followTarget = null;
            _trackingKey = null;

            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.AssignAsset(null);
                _audioSource.outputAudioMixerGroup = null;
            }
        }
    }
}
