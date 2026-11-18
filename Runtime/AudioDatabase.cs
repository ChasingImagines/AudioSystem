using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki tüm AudioCatalog dosyalarını otomatik toplayan ve statik olarak eriştiren merkezi veritabanı.
    /// Elle katalog sürükleyip bırakma veya Resources klasörü zorunluluğunu tamamen ortadan kaldırır.
    /// </summary>
    public static class AudioDatabase
    {
        private static readonly List<AudioCatalog> _catalogs = new List<AudioCatalog>();
        private static readonly Dictionary<string, AudioCatalog.Entry> _compositeLookup = new Dictionary<string, AudioCatalog.Entry>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        public static IReadOnlyList<AudioCatalog> Catalogs
        {
            get
            {
                EnsureInitialized();
                return _catalogs;
            }
        }

        public static bool TryGetEntry(string soundId, out AudioCatalog.Entry entry)
        {
            EnsureInitialized();
            entry = null;
            return !string.IsNullOrWhiteSpace(soundId) && _compositeLookup.TryGetValue(soundId.Trim(), out entry);
        }

        public static void EnsureInitialized(bool forceRefresh = false)
        {
            if (_initialized && !forceRefresh) return;

            _catalogs.Clear();
            _compositeLookup.Clear();

#if UNITY_EDITOR
            // Unity Editöründe projenin neresinde olursa olsun tüm katalogları otomatik keşfeder
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioCatalog");
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                    AudioCatalog cat = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                    if (cat != null && !_catalogs.Contains(cat))
                    {
                        RegisterCatalog(cat);
                    }
                }
            }
#endif
            _initialized = true;
        }

        public static void RegisterCatalog(AudioCatalog catalog)
        {
            if (catalog == null) return;

            if (!_catalogs.Contains(catalog))
            {
                _catalogs.Add(catalog);
            }

            catalog.EnsureLookup();
            IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AudioCatalog.Entry e = entries[i];
                if (e != null && !string.IsNullOrWhiteSpace(e.id))
                {
                    string id = e.id.Trim();
                    if (!_compositeLookup.ContainsKey(id))
                    {
                        _compositeLookup[id] = e;
                    }
                }
            }
        }

        public static void InvalidateLookup()
        {
            _initialized = false;
        }
    }
}
