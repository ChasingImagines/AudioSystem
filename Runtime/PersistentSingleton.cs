using UnityEngine;

namespace Yasin.AudioSystem
{
    internal static class AudioSingletonLifecycle
    {
        public static bool IsQuitting { get; internal set; }

        static AudioSingletonLifecycle()
        {
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting()
        {
            IsQuitting = true;
        }
    }

    public class PersistentSingleton<T> : MonoBehaviour where T : Component
    {
        public bool AutoUnparentOnAwake = true;

        protected static T instance;

        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance
        {
            get
            {
                if (AudioSingletonLifecycle.IsQuitting || !Application.isPlaying)
                    return null;

                if (instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name + " Auto-Generated");
                        instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }

        protected static void ClearStaticInstance()
        {
            instance = null;
        }

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            AudioSingletonLifecycle.IsQuitting = true;
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
                return;

            if (AutoUnparentOnAwake)
            {
                transform.SetParent(null);
            }

            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
