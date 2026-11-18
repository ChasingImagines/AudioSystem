using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Arka plan müziklerini crossfade geçişleriyle yöneten motor.
    /// </summary>
    public class MusicManager : PersistentSingleton<MusicManager>
    {
        private AudioSource _currentSource;
        private AudioSource _previousSource;
        private Coroutine _fadeCoroutine;

        [SerializeField] private AudioMixerGroup musicMixerGroup;

        public void Play(AudioClip clip, float fadeDuration = 1.0f, bool loop = true)
        {
            if (clip == null) return;
            if (_currentSource != null && _currentSource.clip == clip && _currentSource.isPlaying) return;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(CrossFadeRoutine(clip, fadeDuration, loop));
        }

        public void Stop(float fadeDuration = 1.0f)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
        }

        private IEnumerator CrossFadeRoutine(AudioClip newClip, float duration, bool loop)
        {
            _previousSource = _currentSource;

            _currentSource = gameObject.AddComponent<AudioSource>();
            _currentSource.clip = newClip;
            _currentSource.outputAudioMixerGroup = musicMixerGroup;
            _currentSource.loop = loop;
            _currentSource.volume = 0f;
            _currentSource.spatialBlend = 0f;
            _currentSource.Play();

            float elapsed = 0f;
            float initialPrevVolume = _previousSource != null ? _previousSource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _currentSource.volume = t;
                if (_previousSource != null)
                {
                    _previousSource.volume = Mathf.Lerp(initialPrevVolume, 0f, t);
                }
                yield return null;
            }

            _currentSource.volume = 1f;
            if (_previousSource != null)
            {
                Destroy(_previousSource);
                _previousSource = null;
            }
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            if (_currentSource == null) yield break;

            float elapsed = 0f;
            float startVol = _currentSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _currentSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            Destroy(_currentSource);
            _currentSource = null;
            _fadeCoroutine = null;
        }
    }
}
