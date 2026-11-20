# AudioSystem for Unity (Türkçe Dokümantasyon)

**AudioSystem**, Unity projelerinde (Unity 2021.3 – Unity 6 arası tüm sürümler) sesleri modüler kataloglar halinde düzenleyen, `PreloadedAssets` tabanlı `AudioRegistry` ile Build ortamında otomatik çalışan, sıfır GC (Zero-GC) havuzlama ve jenerasyon güvenli `SoundHandle` sunan ses ve müzik motorudur.

---

## 🎯 Temel Özellikler (v2.0)

1. **Sıfır Sahne Kurulumu & Build Garantisi:** `AudioCatalog` dosyalarınızı `Assets/` altında istediğiniz herhangi bir klasöre koyabilirsiniz. `Resources` klasörü gerekmez; `AudioRegistry` katalogları otomatik toplar, `PlayerSettings > Preloaded Assets` listesine ekler ve build öncesi `IPreprocessBuildWithReport` ile doğrular.
2. **Unity 2021.3 – Unity 6 Uyumlu:** Unity 2021.3/2022.3'te `AudioClip`, Unity 6'da (`UNITY_6000_0_OR_NEWER`) ise `AudioResource` / `AudioRandomContainer` ile tam uyumlu çalışır.
3. **GUID Korumalı `AudioKey` & `[AudioId("...")]` Filtreleme:** `AudioKey` sesleri değiştirilemez GUID ile tutar (isim değişse bile sahne/prefab bağlantısı kopmaz; büyük/küçük harf duyarsızdır). `[AudioId("ui_")]` özniteliği hem `string` hem `AudioKey` alanlarında açılır listeyi prefix ile filtreler.
4. **Gerçek Zero-GC Havuzlama & `SoundHandle`:** Coroutine/`WaitWhile` tahsisi yoktur; `AudioListener.pause` / odak kaybında sesleri kesmez; jenerasyon korumalı `SoundHandle` ile `Stop()`, `Pause()`, `Resume()`, `SetVolume()`, `Follow()` kontrolü verir.
5. **Giriş Bazlı Ayarlar, Cooldown & Örnek Limiti:** Her ses için `volume`, `pitch`, `pitchVariance`, `spatialBlend`, `minDistance`/`maxDistance`, `loop`, `cooldown` ve `maxConcurrentInstances` tanımlanabilir. Mikser yönlendirmesi otomatik zincir izler (`Entry.mixerGroup` → `Catalog.defaultMixerGroup` → `AudioRegistry.defaultSfxMixerGroup`).
6. **2 Kaynaklı Ping-Pong `MusicManager`:** Sabit 2 `AudioSource` ile sıfır bileşen sızıntısı ve logaritmik crossfade sunar; hem `AudioKey` hem `AudioClip`/`AudioResource` çalabilir.
7. **Mikser Ses Kontrolü & Editör Önizleme:** `SetMasterVolume`, `SetSFXVolume`, `SetMusicVolume` (`ToLogarithmicVolume`) ve Inspector içinden tek tıkla `▶` ses önizleme butonu içerir.

---

## 📦 Kurulum (Unity Package Manager)

1. Unity'de **Window > Package Manager** penceresini açın.
2. Sol üstteki **`+`** butonuna tıklayın ve **"Add package from git URL..."** seçin.
3. Git URL'sini yapıştırıp **Add** butonuna basın:
   ```text
   https://github.com/ChasingImagines/AudioSystem.git
   ```

---

## 🚀 Kullanım Rehberi

### 1. Ses Kataloğu (`AudioCatalog`) Oluşturma
Project penceresinde `Assets/` altında istediğiniz herhangi bir klasörde sağ tıklayın:
> **`Create > AudioSystem > Audio Catalog`**

`AudioRegistry` otomatik olarak kataloğu kaydedip Build ayarlarına bağlar. İçine seslerinizi (`player_jump`, `ui_click`, `bgm_boss`) ve mikser/cooldown ayarlarını ekleyin.

### 2. Inspector'da Ses Tanımlama (`AudioKey` ve `[AudioId]`)

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private AudioKey attackSound;

    // İsteğe bağlı önek filtreleme (AudioKey veya string üzerinde çalışır):
    [AudioId("ui_"), SerializeField] private AudioKey uiConfirmSound;

    public void Attack()
    {
        SoundHandle handle = attackSound.Play(transform.position);
        handle.Follow(transform);
    }
}
```

### 3. Oynatma & Mikser Kontrolü (`AudioSystem`)

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
        // 1. 2D / UI Sesi:
        AudioSystem.PlayUI(uiClickSound);

        // 2. 3D Uzamsal Ses:
        SoundHandle handle = AudioSystem.Play(explosionSound, transform.position, volumeScale: 0.9f, pitchScale: 1.05f);

        // 3. Arka Plan Müziği (AudioKey veya AudioClip/AudioResource ile logaritmik crossfade):
        AudioSystem.PlayMusic(battleTheme, fadeDuration: 1.5f, loop: true);

        // 4. Mikser Ses Seviyeleri ve Durdurma:
        AudioSystem.SetMasterVolume(0.8f);
        AudioSystem.StopMusic(fadeDuration: 1f);
        AudioSystem.StopAllSounds();
    }
}
```

---

## 👥 Emeği Geçenler (Credits)
- **Geliştirici:** [Yasin Öztürk](https://github.com/ChasingImagines)
- **AI Pair-Programmer:** Antigravity (Google DeepMind)

## 📄 Lisans
Bu proje **MIT Lisansı** altında lisanslanmıştır. Yasin Öztürk tarafından geliştirilmiştir.
