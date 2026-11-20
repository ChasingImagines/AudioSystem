using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Central project registry holding all AudioCatalog assets and default mixer/pool settings,
    /// automatically loaded at startup via Unity's PreloadedAssets mechanism.
    /// </summary>
    [CreateAssetMenu(menuName = "AudioSystem/Audio Registry (Project Singleton)", fileName = "AudioRegistry", order = 0)]
    public sealed class AudioRegistry : ScriptableObject
    {
        public const string DefaultRegistryAssetPath = "Assets/AudioSystem/AudioRegistry.asset";
        public const string DefaultCatalogFolderPath = "Assets/AudioSystem/Catalogs";

        [Header("Catalogs")]
        [SerializeField] private string defaultCatalogFolder = DefaultCatalogFolderPath;
        [SerializeField] private List<AudioCatalog> catalogs = new List<AudioCatalog>();

        [Header("Mixer & Volume Parameters")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup defaultSfxMixerGroup;
        [SerializeField] private AudioMixerGroup defaultMusicMixerGroup;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";

        [Header("Pool Settings")]
        [Min(1)] [SerializeField] private int defaultPoolCapacity = 16;
        [Min(1)] [SerializeField] private int maxPoolSize = 64;

        private static AudioRegistry _active;

        public static AudioRegistry Active => _active;
        public IReadOnlyList<AudioCatalog> Catalogs => catalogs;
        public string DefaultCatalogFolder => string.IsNullOrWhiteSpace(defaultCatalogFolder)
            ? DefaultCatalogFolderPath
            : defaultCatalogFolder.Trim().TrimEnd('/');

        public AudioMixer MasterMixer => masterMixer;
        public AudioMixerGroup DefaultSfxMixerGroup => defaultSfxMixerGroup;
        public AudioMixerGroup DefaultMusicMixerGroup => defaultMusicMixerGroup;
        public string MasterVolumeParam => masterVolumeParam;
        public string SfxVolumeParam => sfxVolumeParam;
        public string MusicVolumeParam => musicVolumeParam;
        public int DefaultPoolCapacity => Mathf.Max(1, defaultPoolCapacity);
        public int MaxPoolSize => Mathf.Max(DefaultPoolCapacity, maxPoolSize);

        private void OnEnable()
        {
            _active = this;
            AudioDatabase.SetRegistry(this);
        }

        private void OnDestroy()
        {
            if (_active == this)
            {
                _active = null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (catalogs == null)
            {
                catalogs = new List<AudioCatalog>();
            }
            else
            {
                for (int i = catalogs.Count - 1; i >= 0; i--)
                {
                    if (catalogs[i] == null)
                    {
                        catalogs.RemoveAt(i);
                    }
                }
            }

            _active = this;
            AudioDatabase.SetRegistry(this);
        }

        public bool SyncCatalogs(IReadOnlyList<AudioCatalog> discovered)
        {
            if (catalogs == null)
            {
                catalogs = new List<AudioCatalog>();
            }

            List<AudioCatalog> normalized = new List<AudioCatalog>(discovered != null ? discovered.Count : 0);
            HashSet<AudioCatalog> seen = new HashSet<AudioCatalog>();

            if (discovered != null)
            {
                for (int i = 0; i < discovered.Count; i++)
                {
                    AudioCatalog c = discovered[i];
                    if (c != null && seen.Add(c))
                    {
                        normalized.Add(c);
                    }
                }
            }

            normalized.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            bool changed = catalogs.Count != normalized.Count;
            if (!changed)
            {
                for (int i = 0; i < normalized.Count; i++)
                {
                    if (catalogs[i] != normalized[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
                return false;

            catalogs.Clear();
            catalogs.AddRange(normalized);
            return true;
        }
#endif
    }
}
