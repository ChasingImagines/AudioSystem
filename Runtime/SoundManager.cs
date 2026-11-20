using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Manages the AudioSource pool (SoundEmitter pool), zero-GC playback lifecycle, cooldowns, and concurrency limits.
    /// </summary>
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        private IObjectPool<SoundEmitter> _pool;
        private readonly List<SoundEmitter> _activeEmitters = new List<SoundEmitter>(32);
        private readonly Dictionary<string, float> _lastPlayedTime = new Dictionary<string, float>(32, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _activeInstanceCounts = new Dictionary<string, int>(32, StringComparer.OrdinalIgnoreCase);

        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maxPoolSize = 64;

        private bool _isPaused;
        private bool _appPaused;

        public int ActiveEmitterCount => _activeEmitters.Count;

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
                InitializePool();
            }
        }

        private void Update()
        {
            TickEmitters();
        }

        internal void TickEmitters()
        {
            bool globalPaused = _isPaused || _appPaused;
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                SoundEmitter emitter = _activeEmitters[i];
                if (emitter == null)
                {
                    _activeEmitters.RemoveAt(i);
                    continue;
                }

                if (!emitter.Tick(globalPaused))
                {
                    ReturnToPool(emitter);
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _appPaused = pauseStatus;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!Application.runInBackground)
            {
                _appPaused = !hasFocus;
            }
        }

        private void InitializePool()
        {
            if (_pool != null)
                return;

            AudioRegistry reg = AudioRegistry.Active;
            int cap = reg != null ? reg.DefaultPoolCapacity : Mathf.Max(1, defaultCapacity);
            int max = reg != null ? reg.MaxPoolSize : Mathf.Max(cap, maxPoolSize);

#if UNITY_EDITOR
            const bool checkCollection = true;
#else
            const bool checkCollection = false;
#endif

            _pool = new ObjectPool<SoundEmitter>(
                createFunc: CreateSoundEmitter,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: checkCollection,
                defaultCapacity: cap,
                maxSize: max
            );
        }

        private SoundEmitter CreateSoundEmitter()
        {
            GameObject go = new GameObject("SoundEmitter");
            go.transform.SetParent(transform);
            go.SetActive(false);
            return go.AddComponent<SoundEmitter>();
        }

        private void OnTakeFromPool(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(true);
            _activeEmitters.Add(emitter);
        }

        private void OnReturnedToPool(SoundEmitter emitter)
        {
            if (emitter != null)
            {
                emitter.gameObject.SetActive(false);
                _activeEmitters.Remove(emitter);
            }
        }

        private void OnDestroyPoolObject(SoundEmitter emitter)
        {
            if (emitter != null)
            {
                Destroy(emitter.gameObject);
            }
        }

        public SoundEmitter Get()
        {
            InitializePool();
            return _pool != null ? _pool.Get() : null;
        }

        public bool CanPlay(string trackingKey, float cooldown, int maxConcurrentInstances)
        {
            if (string.IsNullOrEmpty(trackingKey))
                return true;

            if (cooldown > 0f && _lastPlayedTime.TryGetValue(trackingKey, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < cooldown)
                    return false;
            }

            if (maxConcurrentInstances > 0 && _activeInstanceCounts.TryGetValue(trackingKey, out int activeCount))
            {
                if (activeCount >= maxConcurrentInstances)
                    return false;
            }

            return true;
        }

        internal SoundHandle PlayResolved(
            AudioDatabase.ResolvedSound resolved,
            Vector3 position,
            float volumeScale = 1f,
            float pitchScale = 1f,
            float? spatialBlendOverride = null,
            bool? loopOverride = null)
        {
            AudioCatalog.Entry entry = resolved.Entry;
            if (entry == null)
                return SoundHandle.Invalid;

            AudioAsset resource = entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] Sound '{entry.id}' has no audio asset assigned!");
                return SoundHandle.Invalid;
            }

            string trackingKey = resolved.TrackingKey;
            if (!CanPlay(trackingKey, entry.cooldown, entry.maxConcurrentInstances))
                return SoundHandle.Invalid;

            SoundEmitter emitter = Get();
            if (emitter == null)
                return SoundHandle.Invalid;

            float finalVolume = Mathf.Clamp01(entry.volume * volumeScale);
            float basePitch = entry.pitch > 0f ? entry.pitch : 1f;
            float variance = entry.pitchVariance > 0f ? UnityEngine.Random.Range(-entry.pitchVariance, entry.pitchVariance) : 0f;
            float finalPitch = basePitch * pitchScale + variance;
            float finalSpatial = spatialBlendOverride.HasValue ? Mathf.Clamp01(spatialBlendOverride.Value) : entry.spatialBlend;
            bool finalLoop = loopOverride.HasValue ? loopOverride.Value : entry.loop;
            AudioMixerGroup mixer = resolved.ResolveMixerGroup(isMusic: false);

            if (!string.IsNullOrEmpty(trackingKey))
            {
                _lastPlayedTime[trackingKey] = Time.unscaledTime;
                _activeInstanceCounts.TryGetValue(trackingKey, out int activeCount);
                _activeInstanceCounts[trackingKey] = activeCount + 1;
            }

            return emitter.Play(
                resource,
                position,
                finalVolume,
                finalSpatial,
                finalPitch,
                mixer,
                finalLoop,
                entry.minDistance,
                entry.maxDistance,
                trackingKey);
        }

        public void ReturnToPool(SoundEmitter emitter)
        {
            if (emitter == null || emitter.IsInPool)
                return;

            string trackingKey = emitter.TrackingKey;
            if (!string.IsNullOrEmpty(trackingKey) && _activeInstanceCounts.TryGetValue(trackingKey, out int activeCount))
            {
                if (activeCount <= 1)
                {
                    _activeInstanceCounts.Remove(trackingKey);
                }
                else
                {
                    _activeInstanceCounts[trackingKey] = activeCount - 1;
                }
            }

            emitter.OnReturnedToPoolCleanup();

            if (_pool != null)
            {
                _pool.Release(emitter);
            }
            else
            {
                _activeEmitters.Remove(emitter);
            }
        }

        public void StopAll()
        {
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                if (i < _activeEmitters.Count && _activeEmitters[i] != null)
                {
                    _activeEmitters[i].Stop();
                }
            }
        }

        public void PauseAll()
        {
            _isPaused = true;
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                if (i < _activeEmitters.Count && _activeEmitters[i] != null)
                {
                    _activeEmitters[i].Pause();
                }
            }
        }

        public void ResumeAll()
        {
            _isPaused = false;
            for (int i = _activeEmitters.Count - 1; i >= 0; i--)
            {
                if (i < _activeEmitters.Count && _activeEmitters[i] != null)
                {
                    _activeEmitters[i].Resume();
                }
            }
        }
    }
}
