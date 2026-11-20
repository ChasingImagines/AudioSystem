using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Central audio database indexing all AudioCatalog assets via AudioRegistry (PreloadedAssets)
    /// and optional dynamic registrations, supporting both GUID and case-insensitive ID lookups.
    /// </summary>
    public static class AudioDatabase
    {
        public readonly struct ResolvedSound
        {
            public readonly AudioCatalog Catalog;
            public readonly AudioCatalog.Entry Entry;

            public ResolvedSound(AudioCatalog catalog, AudioCatalog.Entry entry)
            {
                Catalog = catalog;
                Entry = entry;
            }

            public bool IsValid => Catalog != null && Entry != null;

            public string TrackingKey => Entry != null
                ? (!string.IsNullOrEmpty(Entry.guid) ? Entry.guid : (Entry.id ?? string.Empty))
                : string.Empty;

            public AudioMixerGroup ResolveMixerGroup(bool isMusic = false)
            {
                AudioRegistry reg = AudioRegistry.Active;
                AudioMixerGroup registryFallback = reg != null
                    ? (isMusic ? reg.DefaultMusicMixerGroup : reg.DefaultSfxMixerGroup)
                    : null;

                if (Catalog != null)
                    return Catalog.ResolveMixerGroup(Entry, registryFallback);

                return Entry != null && Entry.mixerGroup != null ? Entry.mixerGroup : registryFallback;
            }
        }

        private static AudioRegistry _registry;
        private static readonly List<AudioCatalog> _dynamicCatalogs = new List<AudioCatalog>(4);
        private static readonly List<AudioCatalog> _catalogs = new List<AudioCatalog>(8);
        private static readonly Dictionary<string, ResolvedSound> _byGuid = new Dictionary<string, ResolvedSound>(StringComparer.Ordinal);
        private static readonly Dictionary<string, ResolvedSound> _byId = new Dictionary<string, ResolvedSound>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        public static AudioRegistry Registry => _registry;

        public static IReadOnlyList<AudioCatalog> Catalogs
        {
            get
            {
                EnsureInitialized();
                return _catalogs;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            _dynamicCatalogs.Clear();
            _catalogs.Clear();
            _byGuid.Clear();
            _byId.Clear();
            _initialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            EnsureInitialized();
        }

        public static void SetRegistry(AudioRegistry registry)
        {
            _registry = registry;
            _initialized = false;
        }

        public static bool TryGetByGuid(string guid, out ResolvedSound resolved)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(guid))
            {
                resolved = default;
                return false;
            }
            return _byGuid.TryGetValue(guid.Trim(), out resolved);
        }

        public static bool TryGetById(string soundId, out ResolvedSound resolved)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(soundId))
            {
                resolved = default;
                return false;
            }
            return _byId.TryGetValue(soundId.Trim(), out resolved);
        }

        public static bool TryResolve(AudioKey key, out ResolvedSound resolved)
        {
            EnsureInitialized();
            if (!string.IsNullOrWhiteSpace(key.Guid) && _byGuid.TryGetValue(key.Guid.Trim(), out resolved))
                return true;

            if (!string.IsNullOrWhiteSpace(key.SoundId) && _byId.TryGetValue(key.SoundId.Trim(), out resolved))
                return true;

            resolved = default;
            return false;
        }

        public static void EnsureInitialized(bool forceRefresh = false)
        {
            if (_initialized && !forceRefresh)
                return;

            _catalogs.Clear();
            _byGuid.Clear();
            _byId.Clear();
            _initialized = true;

#if UNITY_EDITOR
            if (_registry == null)
            {
                _registry = FindRegistryInEditor();
            }
#endif

            if (_registry != null && _registry.Catalogs != null)
            {
                IReadOnlyList<AudioCatalog> regCatalogs = _registry.Catalogs;
                for (int i = 0; i < regCatalogs.Count; i++)
                {
                    IndexCatalog(regCatalogs[i]);
                }
            }
#if UNITY_EDITOR
            else
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioCatalog");
                if (guids != null)
                {
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                        AudioCatalog cat = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                        if (cat != null)
                        {
                            IndexCatalog(cat);
                        }
                    }
                }
            }
#endif

            for (int i = 0; i < _dynamicCatalogs.Count; i++)
            {
                IndexCatalog(_dynamicCatalogs[i]);
            }
        }

        /// <summary>
        /// Dynamically registers an extra AudioCatalog loaded at runtime (e.g., via DLC or Addressables).
        /// </summary>
        public static void RegisterCatalog(AudioCatalog catalog)
        {
            if (catalog == null)
                return;

            if (!_dynamicCatalogs.Contains(catalog))
            {
                _dynamicCatalogs.Add(catalog);
            }

            if (_initialized)
            {
                IndexCatalog(catalog);
            }
        }

        public static void UnregisterCatalog(AudioCatalog catalog)
        {
            if (catalog == null)
                return;

            _dynamicCatalogs.Remove(catalog);
            _initialized = false;
        }

        private static void IndexCatalog(AudioCatalog catalog)
        {
            if (catalog == null)
                return;

            if (!_catalogs.Contains(catalog))
            {
                _catalogs.Add(catalog);
            }

            catalog.EnsureLookups();
            IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                AudioCatalog.Entry e = entries[i];
                if (e == null)
                    continue;

                ResolvedSound resolved = new ResolvedSound(catalog, e);

                if (!string.IsNullOrWhiteSpace(e.guid))
                {
                    string g = e.guid.Trim();
                    if (!_byGuid.ContainsKey(g))
                    {
                        _byGuid[g] = resolved;
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioSystem] Duplicate GUID detected: '{g}' ({catalog.name}/{e.id}). Keeping first catalog entry.", catalog);
                    }
                }

                if (!string.IsNullOrWhiteSpace(e.id))
                {
                    string id = e.id.Trim();
                    if (!_byId.ContainsKey(id))
                    {
                        _byId[id] = resolved;
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioSystem] Duplicate sound ID detected: '{id}' ({catalog.name}). Keeping first catalog entry.", catalog);
                    }
                }
            }
        }

        public static void InvalidateLookup()
        {
            _initialized = false;
        }

#if UNITY_EDITOR
        private static AudioRegistry FindRegistryInEditor()
        {
            UnityEngine.Object[] preloaded = UnityEditor.PlayerSettings.GetPreloadedAssets();
            if (preloaded != null)
            {
                for (int i = 0; i < preloaded.Length; i++)
                {
                    if (preloaded[i] is AudioRegistry reg && reg != null)
                        return reg;
                }
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioRegistry");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioRegistry reg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioRegistry>(path);
                    if (reg != null)
                        return reg;
                }
            }

            return null;
        }
#endif
    }
}
