using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        public LinkedListNode<SoundEmitter> Node { get; set; }

        private AudioSource _audioSource;
        private Coroutine _playingCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }

        public void Play(AudioResource resource, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f, AudioMixerGroup mixerGroup = null, bool loop = false)
        {
            transform.position = position;
            _audioSource.resource = resource;
            _audioSource.volume = Mathf.Clamp01(volume);
            _audioSource.spatialBlend = Mathf.Clamp01(spatialBlend);
            _audioSource.pitch = pitch;
            _audioSource.outputAudioMixerGroup = mixerGroup;
            _audioSource.loop = loop;

            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
            }

            _audioSource.Play();

            if (!loop)
            {
                _playingCoroutine = StartCoroutine(WaitForSoundToEnd());
            }
        }

        private IEnumerator WaitForSoundToEnd()
        {
            yield return null;
            yield return new WaitWhile(() => _audioSource.isPlaying);
            Stop();
        }

        public void Stop()
        {
            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            SoundManager.Instance.ReturnToPool(this);
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            if (_audioSource != null)
            {
                _audioSource.pitch += Random.Range(min, max);
            }
        }
    }
}
