using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// Projedeki tüm ses motorunun merkezi statik yönetim noktası.
    /// AudioSystemManager. (veya AudioSystem.) yazarak tüm çalma, durdurma ve müzik fonksiyonlarına tek noktadan erişilir.
    /// </summary>
    public static class AudioSystemManager
    {
        public static IReadOnlyList<AudioCatalog> Catalogs => AudioDatabase.Catalogs;

        /// <summary>
        /// Bir sesin kayıtlı olup olmadığını kontrol eder.
        /// </summary>
        public static bool HasSound(string soundId)
        {
            if (string.IsNullOrWhiteSpace(soundId)) return false;
            return AudioDatabase.TryGetEntry(soundId, out _);
        }

        public static bool HasSound(AudioKey key) => HasSound(key.SoundId);

        /// <summary>
        /// 2D / Arayüz (UI) sesi çalar.
        /// </summary>
        public static SoundEmitter PlayUI(string soundId, float volume = 1f, float pitch = 1f)
        {
            return Play(soundId, Vector3.zero, volume, spatialBlend: 0f, pitch);
        }

        public static SoundEmitter PlayUI(AudioKey key, float volume = 1f, float pitch = 1f)
        {
            return PlayUI(key.SoundId, volume, pitch);
        }

        /// <summary>
        /// 3D uzamsal ses çalar.
        /// </summary>
        public static SoundEmitter Play(string soundId, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return null;

            if (!AudioDatabase.TryGetEntry(soundId, out AudioCatalog.Entry entry))
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' ID'li ses hiçbir katalogda bulunamadı!");
                return null;
            }

            AudioResource resource = entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' sesinin Resource/AudioClip ataması boş!");
                return null;
            }

            SoundBuilder builder = SoundManager.Instance.CreateSound()
                .WithPosition(position)
                .WithVolume(volume)
                .WithPitch(pitch)
                .WithSpatialBlend(spatialBlend);

            AudioMixerGroup mixer = entry.mixerGroup != null ? entry.mixerGroup : null;
            if (mixer != null)
            {
                builder.WithAudioMixerGroup(mixer);
            }

            return builder.Play(resource);
        }

        public static SoundEmitter Play(AudioKey key, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
        {
            return Play(key.SoundId, position, volume, spatialBlend, pitch);
        }

        public static SoundEmitter Play(AudioKey key)
        {
            return PlayUI(key);
        }

        /// <summary>
        /// Arka plan müziği başlatır (Crossfade geçişli).
        /// </summary>
        public static void PlayMusic(AudioClip musicClip, float fadeDuration = 1f, bool loop = true)
        {
            if (musicClip == null) return;
            MusicManager.Instance.Play(musicClip, fadeDuration, loop);
        }

        /// <summary>
        /// Çalan müziği yumuşakça durdurur.
        /// </summary>
        public static void StopMusic(float fadeDuration = 1f)
        {
            MusicManager.Instance.Stop(fadeDuration);
        }
    }
}
