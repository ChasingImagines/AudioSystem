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
    /// Projedeki seslerin ve müziklerin merkezi statik yönetim API'si.
    /// Katalogdaki sesleri AudioKey (GUID/ID) veya string ID üzerinden sıfır GC SoundManager havuzu ile oynatır.
    /// </summary>
    public static class AudioSystem
    {
        public static IReadOnlyList<AudioCatalog> Catalogs => AudioDatabase.Catalogs;
        public static AudioRegistry Registry => AudioDatabase.Registry;

        public static void RegisterCatalog(AudioCatalog catalog) => AudioDatabase.RegisterCatalog(catalog);
        public static void UnregisterCatalog(AudioCatalog catalog) => AudioDatabase.UnregisterCatalog(catalog);

        /// <summary>
        /// Bir sesin herhangi bir katalogda tanımlı olup olmadığını kontrol eder.
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
        /// 2D / Arayüz (UI) sesi çalar.
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
        /// 3D uzamsal ses çalar (ID üzerinden).
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
                Debug.LogWarning($"[AudioSystem] '{soundId}' ID'li ses katalogda bulunamadı!");
                return SoundHandle.Invalid;
            }

            SoundManager manager = SoundManager.Instance;
            if (manager == null)
                return SoundHandle.Invalid;

            return manager.PlayResolved(resolved, position, volumeScale, pitchScale, spatialBlendOverride, loopOverride);
        }

        /// <summary>
        /// 3D uzamsal ses çalar (AudioKey GUID/ID üzerinden).
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
                Debug.LogWarning($"[AudioSystem] '{key}' anahtarına karşılık gelen ses katalogda bulunamadı!");
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
        /// Katalogdaki bir AudioKey üzerinden arka plan müziği başlatır (Logaritmik Crossfade geçişli).
        /// </summary>
        public static void PlayMusic(AudioKey key, float fadeDuration = 1f, bool loop = true, float volumeScale = 1f)
        {
            if (!key.IsValid)
                return;

            if (!AudioDatabase.TryResolve(key, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] '{key}' müzik anahtarı katalogda bulunamadı!");
                return;
            }

            AudioAsset resource = resolved.Entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] '{key}' müzik girişinin Resource/AudioClip ataması boş!");
                return;
            }

            float finalVol = Mathf.Clamp01(resolved.Entry.volume * volumeScale);
            AudioMixerGroup mixer = resolved.ResolveMixerGroup(isMusic: true);
            PlayMusic(resource, fadeDuration, loop, finalVol, mixer);
        }

        /// <summary>
        /// Katalogdaki bir ID üzerinden arka plan müziği başlatır (Logaritmik Crossfade geçişli).
        /// </summary>
        public static void PlayMusic(string soundId, float fadeDuration = 1f, bool loop = true, float volumeScale = 1f)
        {
            if (string.IsNullOrWhiteSpace(soundId))
                return;

            if (!AudioDatabase.TryGetById(soundId, out AudioDatabase.ResolvedSound resolved))
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' ID'li müzik katalogda bulunamadı!");
                return;
            }

            AudioAsset resource = resolved.Entry.ResolveResource();
            if (resource == null)
            {
                Debug.LogWarning($"[AudioSystem] '{soundId}' müzik girişinin Resource/AudioClip ataması boş!");
                return;
            }

            float finalVol = Mathf.Clamp01(resolved.Entry.volume * volumeScale);
            AudioMixerGroup mixer = resolved.ResolveMixerGroup(isMusic: true);
            PlayMusic(resource, fadeDuration, loop, finalVol, mixer);
        }

        /// <summary>
        /// Doğrudan AudioClip / Unity 6 AudioResource üzerinden arka plan müziği başlatır.
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
        /// Çalan arka plan müziğini yumuşakça durdurur.
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
        /// O an çalan tüm ses efektlerini durdurur.
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
