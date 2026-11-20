using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
#if UNITY_6000_0_OR_NEWER
using AudioAsset = UnityEngine.Audio.AudioResource;
#else
using AudioAsset = UnityEngine.AudioClip;
#endif

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki ses kimliklerini AudioClip veya Unity 6 AudioResource varlıklarına eşleyen ScriptableObject.
    /// GUID tabanlı kopmayan referanslar, varsayılan mikser yönlendirmesi ve spam/örnek sayısı sınırları sunar.
    /// </summary>
    [CreateAssetMenu(menuName = "AudioSystem/Audio Catalog", fileName = "NewAudioCatalog", order = 10)]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Kırılmaz ses kimliği (GUID). Ses ID'si değişse bile AudioKey referansları bozulmaz.")]
            public string guid;

            [Tooltip("Sesin okunabilir benzersiz kimliği (Örn: player_jump, ui_click).")]
            public string id;

            [Tooltip("Oynatılacak ses varlığı (Unity 2021/2022'de AudioClip, Unity 6'da AudioResource/AudioRandomContainer).")]
            public AudioAsset resource;

            [Tooltip("Bu ses için özel AudioMixerGroup (atanmazsa kataloğun varsayılan mikser grubu kullanılır).")]
            public AudioMixerGroup mixerGroup;

            [Header("Oynatma Ayarları")]
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            [Range(0f, 0.5f)] public float pitchVariance = 0f;
            [Range(0f, 1f)] public float spatialBlend = 1f;
            [Min(0.01f)] public float minDistance = 1f;
            [Min(0.1f)] public float maxDistance = 50f;
            public bool loop = false;

            [Header("Spam & Eşzamanlılık Koruması")]
            [Tooltip("Aynı sesin art arda tetiklenmesi arasındaki minimum süre (saniye). 0 = sınırsız.")]
            [Min(0f)] public float cooldown = 0f;

            [Tooltip("Aynı anda çalabilecek maksimum kopya sayısı. 0 = sınırsız.")]
            [Min(0)] public int maxConcurrentInstances = 0;

            public AudioAsset ResolveResource() => resource;
        }

        [Header("Mikser Yönlendirmesi")]
        [Tooltip("Bu katalogdaki seslerin varsayılan olarak yönlendirileceği AudioMixerGroup.")]
        [SerializeField] private AudioMixerGroup defaultMixerGroup;

        [Header("Ses Listesi")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [NonSerialized] private readonly Dictionary<string, Entry> _byId = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        [NonSerialized] private readonly Dictionary<string, Entry> _byGuid = new Dictionary<string, Entry>(StringComparer.Ordinal);
        [NonSerialized] private bool _lookupBuilt;

        public AudioMixerGroup DefaultMixerGroup => defaultMixerGroup;
        public IReadOnlyList<Entry> Entries => entries;

        public AudioMixerGroup ResolveMixerGroup(Entry entry, AudioMixerGroup fallback = null)
        {
            if (entry != null && entry.mixerGroup != null)
                return entry.mixerGroup;
            if (defaultMixerGroup != null)
                return defaultMixerGroup;
            return fallback;
        }

        public bool TryGet(string id, out Entry entry)
        {
            EnsureLookups();
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && _byId.TryGetValue(id.Trim(), out entry);
        }

        public bool TryGetByGuid(string guid, out Entry entry)
        {
            EnsureLookups();
            entry = null;
            return !string.IsNullOrWhiteSpace(guid) && _byGuid.TryGetValue(guid.Trim(), out entry);
        }

        public void EnsureLookups()
        {
            if (!_lookupBuilt)
            {
                RebuildLookups();
            }
        }

        public void RebuildLookups()
        {
            _byId.Clear();
            _byGuid.Clear();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    if (entry == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(entry.guid))
                    {
                        string g = entry.guid.Trim();
                        if (!_byGuid.ContainsKey(g))
                        {
                            _byGuid[g] = entry;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(entry.id))
                    {
                        string id = entry.id.Trim();
                        if (!_byId.ContainsKey(id))
                        {
                            _byId[id] = entry;
                        }
                    }
                }
            }

            _lookupBuilt = true;
        }

        public void SanitizeEntries()
        {
            if (entries == null)
            {
                entries = new List<Entry>();
            }

            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null)
                {
                    entry = new Entry();
                    entries[i] = entry;
                }

                bool wasNewOrUninitialized = string.IsNullOrWhiteSpace(entry.guid);
                if (wasNewOrUninitialized || !seenGuids.Add(entry.guid))
                {
                    entry.guid = Guid.NewGuid().ToString("N");
                    seenGuids.Add(entry.guid);
                }

                entry.id = entry.id != null ? entry.id.Trim() : string.Empty;

                if (wasNewOrUninitialized && entry.volume <= 0f && entry.pitch <= 0f)
                {
                    entry.volume = 1f;
                    entry.spatialBlend = 1f;
                }

                entry.volume = Mathf.Clamp01(entry.volume);
                if (entry.pitch <= 0f) entry.pitch = 1f;
                entry.pitch = Mathf.Clamp(entry.pitch, 0.1f, 3f);
                entry.pitchVariance = Mathf.Clamp(entry.pitchVariance, 0f, 0.5f);
                entry.spatialBlend = Mathf.Clamp01(entry.spatialBlend);
                if (entry.minDistance <= 0f) entry.minDistance = 1f;
                if (entry.maxDistance <= 0f) entry.maxDistance = 50f;
                if (entry.maxDistance < entry.minDistance) entry.maxDistance = entry.minDistance;
                if (entry.cooldown < 0f) entry.cooldown = 0f;
                if (entry.maxConcurrentInstances < 0) entry.maxConcurrentInstances = 0;
            }
        }

        private void OnEnable()
        {
            RebuildLookups();
        }

        private void OnDisable()
        {
            _lookupBuilt = false;
        }

        private void OnValidate()
        {
            SanitizeEntries();
            RebuildLookups();
            AudioDatabase.InvalidateLookup();
        }
    }
}
