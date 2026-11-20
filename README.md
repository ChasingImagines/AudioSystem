# AudioSystem for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3+-blue.svg)](https://unity.com/)
[![Language](https://img.shields.io/badge/Language-English%20%7C%20Turkish-informational)](#-documentation-languages)

[🇹🇷 Türkçe Dokümantasyon için tıklayın (README.tr.md)](README.tr.md)

**AudioSystem** is a catalog-driven, zero-GC, high-performance sound and music management engine for Unity (supporting Unity 2021.3 through Unity 6) featuring `AudioRegistry` PreloadedAssets discovery, dynamic `AudioSource` pooling with generation-safe `SoundHandle`s, ping-pong BGM crossfades, and GUID-safe `AudioKey` inspector dropdowns.

---

## 🎯 Key Features

- **Zero Scene Setup & Build-Safe Discovery:** Place `AudioCatalog` assets anywhere under `Assets/`. `AudioRegistry` automatically synchronizes catalogs via `PreloadedAssets` and verifies everything before builds via `IPreprocessBuildWithReport`—no `Resources` folder or scene manager setup required.
- **Unity 2021.3 – Unity 6 Compatible:** Uses `AudioClip` on Unity 2021.3/2022.3 and `AudioResource` / `AudioRandomContainer` on Unity 6 (`UNITY_6000_0_OR_NEWER`).
- **GUID-Safe `AudioKey` & `[AudioId]` Filtering:** `AudioKey` binds by immutable GUID (falling back to case-insensitive string ID), so renaming a sound in a catalog never breaks scene or prefab references. Use `[AudioId("ui_")]` on `string` or `AudioKey` fields to filter dropdown options by prefix.
- **True Zero-GC Pooling & `SoundHandle`:** Active emitters are ticked centrally without per-play coroutines or `WaitWhile` allocations, never kill sounds on `AudioListener.pause`, and return generation-safe `SoundHandle`s (`Stop`, `Pause`, `Resume`, `SetVolume`, `SetPitch`, `Follow`).
- **Per-Entry Mixing, Variance, Cooldown & Concurrency Caps:** Configure `volume`, `pitch`, `pitchVariance`, `spatialBlend`, `minDistance`/`maxDistance`, `loop`, `cooldown`, and `maxConcurrentInstances` per sound entry, with clean mixer fallback (`Entry.mixerGroup` → `Catalog.defaultMixerGroup` → `AudioRegistry.defaultSfxMixerGroup`).
- **2-Source Ping-Pong `MusicManager`:** Plays music from either an `AudioKey`, string ID, or raw `AudioClip`/`AudioResource` using two permanent `AudioSource`s and logarithmic crossfades—rapid track changes or `StopMusic` mid-fade never leak zombie audio.
- **Mixer Volume Helpers & Edit Mode Preview:** Includes `SetMasterVolume`, `SetSFXVolume`, `SetMusicVolume` (`ToLogarithmicVolume`), and one-click `▶` preview buttons directly in Inspectors.

---

## 📦 Installation (Unity Package Manager)

1. Open **Window > Package Manager** in Unity.
2. Click the **`+`** icon in the top-left corner and select **"Add package from git URL..."**.
3. Paste the Git URL:
   ```text
   https://github.com/ChasingImagines/AudioSystem.git
   ```

---

## 🚀 Quick Start Guide

### 1. Create an Audio Catalog
Right-click anywhere under `Assets/`:
> **`Create > AudioSystem > Audio Catalog`**

`AudioRegistry` automatically registers the catalog and ensures it is included in `PlayerSettings > Preloaded Assets`. Add entries (`player_jump`, `ui_click`, `bgm_boss`) with their mixer groups, cooldowns, and concurrency limits.

---

### 2. Inspector Reference (`AudioKey` & `[AudioId]`)

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private AudioKey attackSound;

    // Optional prefix filtering on either AudioKey or string fields:
    [AudioId("ui_"), SerializeField] private AudioKey uiConfirmSound;

    public void Attack()
    {
        SoundHandle handle = attackSound.Play(transform.position);
        handle.Follow(transform);
    }
}
```

---

### 3. Playback & Mixer Controls (`AudioSystem`)

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class AudioManagerExample : MonoBehaviour
{
    [SerializeField] private AudioKey uiClickSound;
    [SerializeField] private AudioKey explosionSound;
    [SerializeField] private AudioKey battleTheme;

    void Start()
    {
        // 1. 2D / UI Sound:
        AudioSystem.PlayUI(uiClickSound);

        // 2. 3D Spatial Sound with handle control:
        SoundHandle handle = AudioSystem.Play(explosionSound, transform.position, volumeScale: 0.9f, pitchScale: 1.05f);

        // 3. Background Music via AudioKey or AudioClip/AudioResource:
        AudioSystem.PlayMusic(battleTheme, fadeDuration: 1.5f, loop: true);

        // 4. Stop / Pause / Mixer Volume Controls:
        AudioSystem.SetMasterVolume(0.8f);
        AudioSystem.StopMusic(fadeDuration: 1f);
        AudioSystem.StopAllSounds();
    }
}
```

---

## 👥 Credits & Acknowledgments
- **Author:** [Yasin Öztürk](https://github.com/ChasingImagines)
- **AI Pair-Programmer:** Antigravity (Google DeepMind)

## 📄 License
This project is licensed under the [MIT License](LICENSE.md) - created by Yasin Öztürk.
