using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// AudioSource havuzunu (SoundEmitter Pool) yöneten ana motor.
    /// Prefab veya Resources bağımlılığı olmadan çalışma anında saf C# ile havuz oluşturur.
    /// </summary>
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        private IObjectPool<SoundEmitter> _pool;
        private readonly List<SoundEmitter> _activeEmitters = new List<SoundEmitter>();

        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maxPoolSize = 64;

        protected override void Awake()
        {
            base.Awake();
            InitializePool();
        }

        private void InitializePool()
        {
            if (_pool != null) return;

            _pool = new ObjectPool<SoundEmitter>(
                createFunc: CreateSoundEmitter,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        private SoundEmitter CreateSoundEmitter()
        {
            var go = new GameObject("SoundEmitter");
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
            emitter.gameObject.SetActive(false);
            _activeEmitters.Remove(emitter);
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

        public void ReturnToPool(SoundEmitter emitter)
        {
            if (_pool != null && emitter != null)
            {
                _pool.Release(emitter);
            }
        }

        public void StopAll()
        {
            var list = new List<SoundEmitter>(_activeEmitters);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    list[i].Stop();
                }
            }
        }
    }
}
