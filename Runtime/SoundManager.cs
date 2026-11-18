using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Yasin.AudioSystem {
    public class SoundManager : PersistentSingleton<SoundManager> {
        IObjectPool<SoundEmitter> soundEmitterPool;
        readonly List<SoundEmitter> activeSoundEmitters = new();
        public readonly LinkedList<SoundEmitter> FrequentSoundEmitters = new();

        [SerializeField] SoundEmitter soundEmitterPrefab;
        [SerializeField] bool collectionCheck = true;
        [SerializeField] int defaultCapacity = 10;
        [SerializeField] int maxPoolSize = 100;
        [SerializeField] int maxSoundInstances = 30;

        void Start() {
            if (soundEmitterPrefab == null)
                soundEmitterPrefab = Resources.Load<SoundEmitter>("Audio/SoundEmitter");

            if (soundEmitterPrefab == null) {
                Debug.LogError("[AudioSystem] SoundEmitter prefab atanmadı ve Resources/Audio/SoundEmitter bulunamadı.");
                return;
            }

            InitializePool();
        }

        public SoundBuilder CreateSoundBuilder() => new SoundBuilder(this);

        /// <summary>
        /// AudioResource (AudioClip veya AudioRandomContainer) doğrudan çalar.
        /// </summary>
        public void Play(AudioResource resource, Vector3 position, float volume = 1f, float spatialBlend = 1f, bool randomPitch = false)
        {
            if (resource == null) return;
            var builder = CreateSoundBuilder().WithPosition(position);
            if (randomPitch) builder.WithRandomPitch();
            builder.Play(resource, volume, spatialBlend);
        }

        /// <summary>
        /// 2D ses veya UI sesi çalar (uzamsal blend = 0).
        /// </summary>
        public void PlayUI(AudioResource resource, float volume = 1f, bool randomPitch = false)
        {
            if (resource == null) return;
            Play(resource, Vector3.zero, volume, spatialBlend: 0f, randomPitch);
        }

        public bool CanPlaySound(SoundData data) {
            if (!data.frequentSound) return true;

            if (FrequentSoundEmitters.Count >= maxSoundInstances) {
                try {
                    FrequentSoundEmitters.First.Value.Stop();
                    return true;
                } catch {
                    Debug.Log("SoundEmitter is already released");
                }
                return false;
            }
            return true;
        }

        public SoundEmitter Get() {
            if (soundEmitterPool == null) {
                if (soundEmitterPrefab == null)
                    soundEmitterPrefab = Resources.Load<SoundEmitter>("Audio/SoundEmitter");

                if (soundEmitterPrefab != null)
                    InitializePool();
            }

            return soundEmitterPool != null ? soundEmitterPool.Get() : null;
        }

        public void ReturnToPool(SoundEmitter soundEmitter) {
            soundEmitterPool.Release(soundEmitter);
        }

        public void StopAll() {
			LinkedList<SoundEmitter> tempList = new LinkedList<SoundEmitter>(activeSoundEmitters);

			foreach (var soundEmitter in tempList)
            {
                soundEmitter.Stop();
            }

            FrequentSoundEmitters.Clear();
        }

        void InitializePool() {
            soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize);
        }

        SoundEmitter CreateSoundEmitter() {
            var soundEmitter = Instantiate(soundEmitterPrefab);
            soundEmitter.gameObject.SetActive(false);
            return soundEmitter;
        }

        void OnTakeFromPool(SoundEmitter soundEmitter) {
            soundEmitter.gameObject.SetActive(true);
            activeSoundEmitters.Add(soundEmitter);
        }

        void OnReturnedToPool(SoundEmitter soundEmitter) {
            if (soundEmitter.Node != null) {
                FrequentSoundEmitters.Remove(soundEmitter.Node);
                soundEmitter.Node = null;
            }
            soundEmitter.gameObject.SetActive(false);
            activeSoundEmitters.Remove(soundEmitter);
        }

        void OnDestroyPoolObject(SoundEmitter soundEmitter) {
            Destroy(soundEmitter.gameObject);
        }
    }
}