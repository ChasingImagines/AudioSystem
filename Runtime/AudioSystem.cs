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
    /// Central static facade for playing catalog SFX and background music via the zero-GC SoundManager pool and MusicManager.
    /// </summary>
    public static class AudioSystem
    {
        public static IReadOnlyList<AudioCatalog> Catalogs => AudioDatabase.Catalogs;
        public static AudioRegistry Registry => AudioDatabase.Registry;

        public static void RegisterCatalog(AudioCatalog catalog) => AudioDatabase.RegisterCatalog(catalog);
        public static void UnregisterCatalog(AudioCatalog catalog) => AudioDatabase.UnregisterCatalog(catalog);

        /// <summary>
        /// Checks whether a sound ID exists in any registered AudioCatalog.
        /// </summary>
        public static bool HasSound(string soundId)
        {
            return !string.IsNullOrWhiteSpace(soundId) && AudioDatabase.TryGetById(soundId, out _);
        }

        public static bool HasSound(AudioKey key)
        {
            return key.IsValid && AudioDatabase.TryResolve(key, out _);
        }

        /// <summary>
        /// Plays a 2D / UI sound by string ID.
        /// </summary>
        public static SoundHandle PlayUI(string soundId, float volumeScale = 1f, float pitchScale = 1f)
        {
            return Play(soundId, Vector3.zero, volumeScale, pitchScale, spatialBlendOverride: 0f);
        }

        public static SoundHandle PlayUI(AudioKey key, float volumeScale = 1f, float pitchScale = 1f)
        {
            return Play(key, Vector3.zero, volumeScale, pitchScale, spatialBlendOverride: 0f);
        }

        /// <summary>
        /// Plays a 3D spatial sound by string ID.
        /// </summary>
        public static SoundHandle Play(
            string soundId,
            Vector3 position,
            float volumeScale = 1f,
            float pitchScale = 1f,
            float? spatialBlendOverride = null,
            bool? loopOverride = null)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return SoundHandle.Invalid;

            if (!AudioDatabase.TryGetById(soundId, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] Sound ID '{soundId}' was not found in any AudioCatalog!");
                return SoundHandle.Invalid;
            }

            SoundManager manager = SoundManager.Instance;
            if (manager == null)
                return SoundHandle.Invalid;

            return manager.PlayResolved(resolved, position, volumeScale, pitchScale, spatialBlendOverride, loopOverride);
        }

        /// <summary>
        /// Plays a 3D spatial sound via AudioKey (GUID/ID).
        /// </summary>
        public static SoundHandle Play(
            AudioKey key,
            Vector3 position,
            float volumeScale = 1f,
            float pitchScale = 1f,
            float? spatialBlendOverride = null,
            bool? loopOverride = null)
        {
            if (!key.IsValid)
                return SoundHandle.Invalid;

            if (!AudioDatabase.TryResolve(key, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] Sound key '{key}' was not found in any AudioCatalog!");
                return SoundHandle.Invalid;
            }

            SoundManager manager = SoundManager.Instance;
            if (manager == null)
                return SoundHandle.Invalid;

            return manager.PlayResolved(resolved, position, volumeScale, pitchScale, spatialBlendOverride, loopOverride);
        }

        public static SoundHandle Play(AudioKey key)
        {
            return PlayUI(key);
        }

        /// <summary>
        /// Plays background music from an AudioKey with a logarithmic crossfade.
        /// </summary>
        public static void PlayMusic(AudioKey key, float fadeDuration = 1f, bool loop = true, float volumeScale = 1f)
        {
            if (!key.IsValid)
                return;

            if (!AudioDatabase.TryResolve(key, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] Music key '{key}' was not found in any AudioCatalog!");
                return;
            }

            AudioAsset resource = resolved.Entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] Music entry '{key}' has no audio asset assigned!");
                return;
            }

            float finalVol = Mathf.Clamp01(resolved.Entry.volume * volumeScale);
            AudioMixerGroup mixer = resolved.ResolveMixerGroup(isMusic: true);
            PlayMusic(resource, fadeDuration, loop, finalVol, mixer);
        }

        /// <summary>
        /// Plays background music from a string ID with a logarithmic crossfade.
        /// </summary>
        public static void PlayMusic(string soundId, float fadeDuration = 1f, bool loop = true, float volumeScale = 1f)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return;

            if (!AudioDatabase.TryGetById(soundId, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] Music ID '{soundId}' was not found in any AudioCatalog!");
                return;
            }

            AudioAsset resource = resolved.Entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] Music entry '{soundId}' has no audio asset assigned!");
                return;
            }

            float finalVol = Mathf.Clamp01(resolved.Entry.volume * volumeScale);
            AudioMixerGroup mixer = resolved.ResolveMixerGroup(isMusic: true);
            PlayMusic(resource, fadeDuration, loop, finalVol, mixer);
        }

        /// <summary>
        /// Plays background music directly from an AudioClip or Unity 6 AudioResource.
        /// </summary>
        public static void PlayMusic(
            AudioAsset musicResource,
            float fadeDuration = 1f,
            bool loop = true,
            float volume = 1f,
            AudioMixerGroup mixerGroup = null)
        {
            if (musicResource == null)
                return;

            MusicManager manager = MusicManager.Instance;
            if (manager != null)
            {
                manager.Play(musicResource, fadeDuration, loop, volume, mixerGroup);
            }
        }

        /// <summary>
        /// Smoothly fades out and stops the active background music.
        /// </summary>
        public static void StopMusic(float fadeDuration = 1f)
        {
            MusicManager manager = MusicManager.TryGetInstance();
            if (manager != null)
            {
                manager.Stop(fadeDuration);
            }
        }

        public static void PauseMusic()
        {
            MusicManager.TryGetInstance()?.Pause();
        }

        public static void ResumeMusic()
        {
            MusicManager.TryGetInstance()?.Resume();
        }

        /// <summary>
        /// Stops all currently playing sound effects immediately.
        /// </summary>
        public static void StopAllSounds()
        {
            SoundManager.TryGetInstance()?.StopAll();
        }

        public static void PauseAllSounds()
        {
            SoundManager.TryGetInstance()?.PauseAll();
        }

        public static void ResumeAllSounds()
        {
            SoundManager.TryGetInstance()?.ResumeAll();
        }

        public static bool SetMasterVolume(float normalized01)
        {
            AudioRegistry reg = AudioRegistry.Active ?? AudioDatabase.Registry;
            return reg != null && SetMixerVolume(reg.MasterVolumeParam, normalized01);
        }

        public static bool SetSFXVolume(float normalized01)
        {
            AudioRegistry reg = AudioRegistry.Active ?? AudioDatabase.Registry;
            return reg != null && SetMixerVolume(reg.SfxVolumeParam, normalized01);
        }

        public static bool SetMusicVolume(float normalized01)
        {
            AudioRegistry reg = AudioRegistry.Active ?? AudioDatabase.Registry;
            return reg != null && SetMixerVolume(reg.MusicVolumeParam, normalized01);
        }

        public static bool SetMixerVolume(string exposedParam, float normalized01)
        {
            AudioRegistry reg = AudioRegistry.Active ?? AudioDatabase.Registry;
            if (reg == null || reg.MasterMixer == null || string.IsNullOrWhiteSpace(exposedParam))
                return false;

            return reg.MasterMixer.SetFloat(exposedParam, normalized01.ToLogarithmicVolume());
        }
    }
}
