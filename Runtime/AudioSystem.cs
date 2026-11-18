using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yasin.AudioSystem
{
    /// <summary>
    /// AudioSystemManager için kısa ve pratik takma ad (alias).
    /// Kodda doğrudan AudioSystem.Play(...) şeklinde çağrılabilir.
    /// </summary>
    public static class AudioSystem
    {
        public static IReadOnlyList<AudioCatalog> Catalogs => AudioSystemManager.Catalogs;
        public static bool HasSound(string soundId) => AudioSystemManager.HasSound(soundId);
        public static bool HasSound(AudioKey key) => AudioSystemManager.HasSound(key);

        public static SoundEmitter Play(string soundId, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
            => AudioSystemManager.Play(soundId, position, volume, spatialBlend, pitch);

        public static SoundEmitter Play(AudioKey key, Vector3 position, float volume = 1f, float spatialBlend = 1f, float pitch = 1f)
            => AudioSystemManager.Play(key, position, volume, spatialBlend, pitch);

        public static SoundEmitter PlayUI(string soundId, float volume = 1f, float pitch = 1f)
            => AudioSystemManager.PlayUI(soundId, volume, pitch);

        public static SoundEmitter PlayUI(AudioKey key, float volume = 1f, float pitch = 1f)
            => AudioSystemManager.PlayUI(key, volume, pitch);

        public static SoundEmitter Play(AudioKey key)
            => AudioSystemManager.Play(key);

        public static void PlayMusic(AudioClip musicClip, float fadeDuration = 1f, bool loop = true)
            => AudioSystemManager.PlayMusic(musicClip, fadeDuration, loop);

        public static void StopMusic(float fadeDuration = 1f)
            => AudioSystemManager.StopMusic(fadeDuration);
    }
}
