using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki ses kimliklerini AudioClip veya Unity 6 AudioResource'a (AudioRandomContainer vb.) eşleyen ScriptableObject.
    /// Tüm sesler bu kataloglar içinde toplanır.
    /// </summary>
    [CreateAssetMenu(menuName = "AudioSystem/Audio Catalog", fileName = "NewAudioCatalog", order = 10)]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Sesin benzersiz kimliği (Örn: player_jump, ui_click).")]
            public string id;

            [Tooltip("Unity 6 AudioResource (AudioClip veya AudioRandomContainer doğrudan buraya atanır).")]
            public AudioResource resource;

            [Tooltip("Bu ses için özel AudioMixerGroup (atanmazsa kataloğun varsayılan mikser grubu kullanılır).")]
            public AudioMixerGroup mixerGroup;

            public AudioResource ResolveResource() => resource;
        }

        [Header("Mikser Yönlendirmesi")]
        [Tooltip("Bu katalogdaki seslerin varsayılan olarak yönlendirileceği AudioMixerGroup.")]
        [SerializeField] private AudioMixerGroup defaultMixerGroup;

        [Header("Ses Listesi")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, Entry> _lookup;

        public AudioMixerGroup DefaultMixerGroup => defaultMixerGroup;
        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string id, out Entry entry)
        {
            EnsureLookup();
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && _lookup.TryGetValue(id.Trim(), out entry);
        }

        public void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                _lookup[entry.id.Trim()] = entry;
            }
        }

        private void OnEnable() => _lookup = null;
        private void OnDisable() => _lookup = null;
        private void OnValidate() => _lookup = null;
    }
}
