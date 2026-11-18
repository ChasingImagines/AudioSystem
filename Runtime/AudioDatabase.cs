using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki tüm AudioCatalog dosyalarını tek bir merkezde toplayan ve yöneten ana ScriptableObject.
    /// Resources klasörü gereksinimini ortadan kaldırır.
    /// </summary>
    [CreateAssetMenu(menuName = "AudioSystem/Audio Database", fileName = "AudioDatabase", order = 0)]
    public sealed class AudioDatabase : ScriptableObject
    {
        private static AudioDatabase _instance;
        public static AudioDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindDatabaseInstance();
                }
                return _instance;
            }
            internal set => _instance = value;
        }

        [Header("Mikser Ayarları")]
        [Tooltip("Oyunun ana AudioMixer dosyası.")]
        [SerializeField] private AudioMixer masterMixer;

        [Header("Kataloglar")]
        [Tooltip("Oyunda kullanılacak tüm AudioCatalog varlıkları bu listeye eklenir.")]
        [SerializeField] private List<AudioCatalog> catalogs = new List<AudioCatalog>();

        [NonSerialized]
        private readonly Dictionary<string, AudioCatalog.Entry> _compositeLookup = new Dictionary<string, AudioCatalog.Entry>(StringComparer.OrdinalIgnoreCase);
        [NonSerialized]
        private bool _lookupBuilt;

        public AudioMixer MasterMixer => masterMixer;
        public IReadOnlyList<AudioCatalog> Catalogs => catalogs;

        public bool TryGetEntry(string soundId, out AudioCatalog.Entry entry)
        {
            EnsureCompositeLookup();
            entry = null;
            return !string.IsNullOrWhiteSpace(soundId) && _compositeLookup.TryGetValue(soundId.Trim(), out entry);
        }

        public void EnsureCompositeLookup()
        {
            if (_lookupBuilt) return;

            _compositeLookup.Clear();
            for (int i = 0; i < catalogs.Count; i++)
            {
                AudioCatalog cat = catalogs[i];
                if (cat == null) continue;

                cat.EnsureLookup();
                IReadOnlyList<AudioCatalog.Entry> entries = cat.Entries;
                for (int j = 0; j < entries.Count; j++)
                {
                    AudioCatalog.Entry e = entries[j];
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

            _lookupBuilt = true;
        }

        public void InvalidateLookup()
        {
            _lookupBuilt = false;
        }

        private static AudioDatabase FindDatabaseInstance()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioDatabase>(path);
            }
#endif
            return null;
        }

        private void OnEnable() => _lookupBuilt = false;
        private void OnValidate() => _lookupBuilt = false;
    }
}
