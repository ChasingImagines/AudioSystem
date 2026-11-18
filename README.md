# AudioSystem for Unity

**AudioSystem**, Unity projeleri için katalog güdümlü (Catalog-Driven), otomatik katalog keşifli, tip güvenli (`AudioKey`) ve havuzlamalı (Pooling) yüksek performanslı bir ses yönetim motorudur.

---

## 🎯 Temel Mimarisi ve Felsefesi

1. **Sıfır Kurulum Zahmeti & Tam Otomasyon:**
   - Projede elle veritabanı dosyası oluşturmak veya katalogları bir listeye tek tek sürükleyip bırakmak **yoktur**.
   - `Resources` klasörü açma zorunluluğu **yoktur**.
   - Projenin neresinde olursa olsun oluşturduğunuz tüm `AudioCatalog` dosyaları arka planda otomatik keşfedilir ve statik olarak kullanıma sunulur.
2. **Katalog Dışı Kaçak Ses Çalma Yasaktır:**
   - Sahnelerdeki scriptlerin rastgele, kontrolsüz ses çalması engellenir; her ses merkezi bir katalog kaydına bağlı olmak zorundadır.
3. **Tip Güvenli `AudioKey` (String Yazmaya Son):**
   - Kodda `"explosion"` gibi elle string yazmak yerine **`AudioKey`** kullanılır. Yazım hatası (typo) riski sıfıra iner.
   - Inspector'da otomatik arama çubuklu (fuzzy search) şık bir açılır menü açılır.
4. **Tek Giriş Kapısı: `AudioSystem.`:**
   - İster `AudioSystem.Play(key)`, ister doğrudan `key.Play()` diyerek tek noktadan tüm ses işlemlerini yürütürsünüz.
5. **Otomatik Havuzlama (Zero-GC Pooling):**
   - Çalınan her ses için yeni nesne yaratılmaz; arka planda dinamik bir `SoundEmitter` havuzu çalışır.

---

## 📦 Kurulum (Unity Package Manager)

1. Unity'de **Window > Package Manager** penceresini açın.
2. Sol üstteki **`+`** butonuna tıklayın ve **"Add package from git URL..."** seçin.
3. Git URL'sini yapıştırıp **Add** butonuna basın:
   ```text
   https://github.com/ChasingImagines/AudioSystem.git
   ```

---

## 🚀 Hızlı Başlangıç Rehberi

### Adım 1: Ses Kataloğu Oluşturun

Project penceresinde istediğiniz bir klasörde sağ tıklayın:
> **`Create > AudioSystem > Audio Catalog`**

Örn: `PlayerSounds.asset`, `UISounds.asset` adını verin ve içine sesleri ekleyin:
- **ID:** `jump`, `attack`, `click`
- **Resource:** Unity 6 `AudioClip` veya `AudioRandomContainer` dosyasını atayın.
- **Mixer Group:** İsteğe bağlı olarak ilgili ses mikserini bağlayın.

*(Hepsi bu kadar! Veritabanına sürükle-bırak yapmanıza gerek yoktur, sistem yeni kataloğu anında tanır.)*

---

### Adım 2: Inspector'da Ses Tanımlama (`AudioKey`)

Bileşenlerinizde elle string yazmak yerine doğrudan `AudioKey` kullanın:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    // Inspector'da tüm katalogları listeleyen arama çubuklu açılır menü çıkar!
    [SerializeField] private AudioKey attackSound;
    [SerializeField] private AudioKey hurtSound;

    public void Attack()
    {
        // 1. Yol: Doğrudan key üzerinden çalma:
        attackSound.Play(transform.position);

        // 2. Yol: AudioSystem üzerinden çalma:
        AudioSystem.Play(attackSound, transform.position);
    }
}
```

---

### Adım 3: C# Kodundan Ses Yönetimi (`AudioSystem.`)

Kod yazarken sadece `AudioSystem.` yazıp nokta koymanız yeterlidir:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class GameAudioExample : MonoBehaviour
{
    [SerializeField] private AudioKey uiClickKey;
    [SerializeField] private AudioKey explosionKey;
    [SerializeField] private AudioClip backgroundMusic;

    void Start()
    {
        // 1. 2D / UI Sesi Çalma:
        AudioSystem.PlayUI(uiClickKey);
        // veya:
        uiClickKey.PlayUI();

        // 2. 3D Uzamsal Ses Çalma (Konum, Ses Seviyesi, Pitch):
        AudioSystem.Play(explosionKey, transform.position, volume: 0.9f, pitch: 1.1f);

        // 3. Arka Plan Müziği (Crossfade geçişli):
        AudioSystem.PlayMusic(backgroundMusic, fadeDuration: 2f, loop: true);

        // 4. Müziği Durdurma:
        AudioSystem.StopMusic(fadeDuration: 1f);

        // 5. Ses Sorgulama:
        if (AudioSystem.HasSound(explosionKey))
        {
            Debug.Log("Patlama sesi katalogda mevcut.");
        }
    }
}
```

---

## 📄 Lisans
MIT License - Yasin Öztürk
