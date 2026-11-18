# AudioSystem for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3+-blue.svg)](https://unity.com/)
[![Language](https://img.shields.io/badge/Language-English%20%7C%20Turkish-informational)](#-documentation-languages)

[🇹🇷 Türkçe Dokümantasyon için tıklayın (README.tr.md)](README.tr.md)

**AudioSystem** is a catalog-driven, high-performance sound and music management engine for Unity featuring dynamic AudioSource pooling, spatial blending, and strongly-typed `AudioKey` inspector dropdowns.

---

## 🎯 Key Features

- **Catalog-Driven Architecture:** Organize sound clips, random containers, and volume/mixer routing inside modular `AudioCatalog` ScriptableObjects instead of scattering clip references across prefabs.
- **Type-Safe `AudioKey`:** Replace fragile magic strings with `AudioKey` structs in your components. Enjoy searchable, categorized inspector dropdowns with instant validation.
- **Zero-GC AudioSource Pooling:** Audio playback never instantiates or destroys GameObjects during gameplay; an internal `SoundEmitter` pool handles 2D/3D playback efficiently.
- **No Resources Setup Required:** Catalogs are automatically discovered across the project at runtime and in editor without needing manual database drag-and-drop or `Resources` folders.
- **BGM Crossfade & Transitions:** Built-in `MusicManager` supports smooth asynchronous volume fades and crossfades.
- **Unity 6 AudioResource Ready:** Fully supports both legacy `AudioClip` and new Unity 6 `AudioResource` / `AudioRandomContainer` assets.

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

Right-click anywhere in your Project window:
> **`Create > AudioSystem > Audio Catalog`**

Name it (e.g. `CombatSounds.asset`) and add your audio entries:
- **ID:** `jump`, `footstep`, `attack`
- **Resource:** Assign an `AudioClip` or `AudioRandomContainer`.
- **Mixer Group:** (Optional) Select an `AudioMixerGroup` for routing.

---

### 2. Inspector Reference (`AudioKey`)

Avoid typing raw strings—declare an `AudioKey` field in your C# components:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    // Searchable dropdown menu listing all sound IDs from all catalogs
    [SerializeField] private AudioKey attackSound;

    public void Attack()
    {
        // Option 1: Play directly through the key:
        attackSound.Play(transform.position);

        // Option 2: Play through central AudioSystem API:
        AudioSystem.Play(attackSound, transform.position);
    }
}
```

---

### 3. Audio Playback API (`AudioSystem.`)

Control all 2D/3D SFX and background music via a single static interface:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class AudioManagerExample : MonoBehaviour
{
    [SerializeField] private AudioKey uiClickSound;
    [SerializeField] private AudioKey explosionSound;
    [SerializeField] private AudioClip backgroundMusic;

    void Start()
    {
        // 1. 2D / UI Sound (spatialBlend = 0):
        AudioSystem.PlayUI(uiClickSound);
        // or directly:
        uiClickSound.PlayUI();

        // 2. 3D Spatial Sound (Position, Volume, Pitch):
        AudioSystem.Play(explosionSound, transform.position, volume: 0.9f, pitch: 1.1f);

        // 3. Background Music (Smooth crossfade):
        AudioSystem.PlayMusic(backgroundMusic, fadeDuration: 1.5f, loop: true);

        // 4. Stop Music:
        AudioSystem.StopMusic(fadeDuration: 1f);

        // 5. Stop All Active SFX:
        AudioSystem.StopAllSounds();
    }
}
```

---


---

## 👥 Credits & Acknowledgments
- **Author:** [Yasin Öztürk](https://github.com/ChasingImagines)
- **AI Pair-Programmer:** Antigravity (Google DeepMind)

## 📄 License
This project is licensed under the [MIT License](LICENSE.md) - created by Yasin Öztürk.
