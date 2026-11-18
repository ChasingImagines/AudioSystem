using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki ses işlemlerinin tek ve zorunlu giriş noktası.
    /// Yan yollardan ses çalmayı engeller; her sesin bir AudioCatalog içinde tanımlı olmasını şart koşar.
    /// </summary>
    public static class Audio
    {
        private const string CatalogsResourceFolder = "Audio/Catalogs";
        private const string DefaultCatalogResourcePath = "Audio/AudioCatalog";

        private static readonly Dictionary<string, AudioCatalog.Entry> _catalogEntries = new Dictionary<string, AudioCatalog.Entry>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        /// <summary>
        /// Resources/Audio altındaki tüm AudioCatalog varlıklarını hafızaya yükler.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            _catalogEntries.Clear();

            // 1. Audio/Catalogs altındaki tüm katalogları yükle
            AudioCatalog[] catalogs = Resources.LoadAll<AudioCatalog>(CatalogsResourceFolder);
            if (catalogs != null)
            {
                for (int i = 0; i < catalogs.Length; i++)
                {
                    RegisterCatalog(catalogs[i]);
                }
            }

            // 2. Audio/AudioCatalog tekil kataloğunu yükle (varsa)
            AudioCatalog singleCatalog = Resources.Load<AudioCatalog>(DefaultCatalogResourcePath);
            if (singleCatalog != null)
            {
                RegisterCatalog(singleCatalog);
            }

            _initialized = true;
        }

        public static void RegisterCatalog(AudioCatalog catalog)
        {
            if (catalog == null) return;

            IReadOnlyList<AudioCatalog.Entry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                AudioCatalog.Entry entry = entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                {
                    _catalogEntries[entry.id.Trim()] = entry;
                }
            }
        }

        /// <summary>
        /// 2D Arayüz (UI) veya genel duyurulacak bir sesi çalar.
        /// </summary>
        /// <param name="soundId">AudioCatalog içinde kayıtlı ses ID'si.</param>
        /// <param name="volume">Ses seviyesi çarpanı (0..1).</param>
        /// <param name="pitch">Perde çarpanı.</param>
        public static SoundEmitter PlayUI(string soundId, float volume = 1f, float pitch = 1f)
        {
            return Play(soundId, Vector3.zero, volume, spatialBlend: 0f, pitch);
        }

        /// <summary>
        /// 3D uzamsal (dünya koordinatlarında) bir sesi çalar.
        /// </summary>
        /// <param name="soundId">AudioCatalog içinde kayıtlı ses ID'si.</param>
        /// <param name="position">Sesin çalınacağı dünya pozisyonu.</param>
        /// <param name="volume">Ses seviyesi çarpanı (0..1).</param>
        /// <param name="spatialBlend">0 = Tamamen 2D, 1 = Tamamen 3D uzamsal.</param>
        /// <param name="pitch">Perde çarpanı.</param>
        public static SoundEmitter Play(string soundId, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return null;

            if (!_initialized)
            {
                Initialize();
            }

            if (!_catalogEntries.TryGetValue(soundId.Trim(), out AudioCatalog.Entry entry))
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' ID'li ses hiçbir AudioCatalog içinde bulunamadı! Lütfen kataloğa ekleyin.");
                return null;
            }

            AudioResource resource = entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' ID'li sesin AudioResource/AudioClip ataması boş!");
                return null;
            }

            SoundBuilder builder = SoundManager.Instance.CreateSound()
                .WithPosition(position)
                .WithVolume(volume)
                .WithPitch(pitch)
                .WithSpatialBlend(spatialBlend);

            if (entry.defaultMixerGroup != null)
            {
                builder.WithAudioMixerGroup(entry.defaultMixerGroup);
            }

            return builder.Play(resource);
        }

        /// <summary>
        /// Konum belirtilmediğinde varsayılan olarak 2D UI sesi çalar.
        /// </summary>
        public static SoundEmitter Play(string soundId)
        {
            return PlayUI(soundId);
        }

        /// <summary>
        /// Arka plan müziği başlatır.
        /// </summary>
        public static void PlayMusic(AudioClip musicClip, float fadeDuration = 1f, bool loop = true)
        {
            if (musicClip == null) return;
            MusicManager.Instance.Play(musicClip, fadeDuration, loop);
        }

        /// <summary>
        /// Çalan arka plan müziğini durdurur.
        /// </summary>
        public static void StopMusic(float fadeDuration = 1f)
        {
            MusicManager.Instance.Stop(fadeDuration);
        }
    }
}
