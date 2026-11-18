using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Oyun olaylarında kullanılan ses kimliklerini AudioClip veya AudioResource'a (AudioRandomContainer vb.) eşler.
    /// Gerçek sesler eklenene kadar demo sesi fallback olarak kullanılabilir.
    /// </summary>
    [CreateAssetMenu(menuName = "AudioSystem/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            [Tooltip("Unity 6 AudioResource (AudioClip veya AudioRandomContainer doğrudan buraya atanır).")]
            public AudioResource resource;
            public AudioMixerGroup defaultMixerGroup;

            public AudioResource ResolveResource()
            {
                return resource;
            }
        }

        [Header("Mikser Yönlendirmesi (Grup Bazlı)")]
        [Tooltip("Bu katalogdaki tüm seslerin varsayılan olarak yönlendirileceği AudioMixerGroup.")]
        [SerializeField] private AudioMixerGroup defaultMixerGroup;

        [Header("Girdiler")]
        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private bool useDemoFallback = true;
        [SerializeField] private AudioClip demoFallbackClip;

        [NonSerialized] private Dictionary<string, Entry> _lookup;

        public AudioMixerGroup DefaultMixerGroup => defaultMixerGroup;
        public AudioClip DemoFallbackClip => useDemoFallback ? demoFallbackClip : null;
        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string id, out Entry entry)
        {
            EnsureLookup();
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && _lookup.TryGetValue(id.Trim(), out entry);
        }

        private void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                if (entry.ResolveResource() == null)
                    continue;

                _lookup[entry.id.Trim()] = entry;
            }
        }

        private void OnEnable() => _lookup = null;
        private void OnDisable() => _lookup = null;
        private void OnValidate() => _lookup = null;
    }
}
