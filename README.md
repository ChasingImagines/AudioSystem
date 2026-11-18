# AudioSystem for Unity

**AudioSystem**, Unity projeleri için katalog güdümlü (Catalog-Driven), merkezi ScriptableObject veritabanına bağlı, tip güvenli (`AudioKey`) ve havuzlamalı (Pooling) yüksek performanslı bir ses yönetim motorudur.

---

## 🎯 Temel Mimarisi ve Felsefesi

1. **Resources Yok (Merkezi AudioDatabase):**
   - Projenizde `Resources` klasörü açmak ve `Resources.Load` arayışlarına girmek zorunda değilsiniz.
   - Tüm kataloglar ve mikser ayarları tek bir merkezi **`AudioDatabase`** ScriptableObject içinde toplanır.
2. **Katalog Dışı Kaçak Ses Çalma Yasaktır:**
   - Sahnelerdeki scriptlerin kafasına göre kontrolsüz veya kaçak ses çalması engellenir; projedeki her ses tek bir merkezi katalog kaydına bağlı olmak zorundadır.
3. **Tip Güvenli `AudioKey` (String Yazmaya Son):**
   - Kodda `"explosion"` gibi elle string yazmak yerine **`AudioKey`** kullanılır. Yazım hatası (typo) riski sıfıra iner.
   - Inspector'da otomatik arama çubuklu (fuzzy search) şık bir açılır menü açılır.
4. **Tek Giriş Kapısı: `AudioSystem.`:**
   - İster `AudioSystem.Play(key)`, ister doğrudan `key.Play()` diyerek tek noktadan tüm ses işlemlerini yürütürsünüz.
5. **Otomatik Havuzlama (Zero-GC Pooling):**
   - Çalınan her ses için yeni `GameObject` yaratılmaz; arka planda dinamik bir `SoundEmitter` havuzu çalışır.

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

### Adım 1: Katalog ve Veritabanı Oluşturma

1. **Katalog Oluşturun:**
   - Project penceresinde sağ tıklayın:
     > **`Create > AudioSystem > Audio Catalog`**
   - Örn: `CombatAudioCatalog.asset` adını verin ve içine sesleri ekleyin:
     - `player_jump`
     - `enemy_hit`
     - `explosion`
   - Her sesin karşısına Unity 6 `AudioClip` veya `AudioRandomContainer` dosyasını atayın.

2. **Merkezi Veritabanını Oluşturun (Projede 1 Kere):**
   - Project penceresinde sağ tıklayın:
     > **`Create > AudioSystem > Audio Database`**
   - Açılan dosyada oluşturduğunuz katalogları **Catalogs** listesine sürükleyip bırakın.

---

### Adım 2: Inspector'da Ses Tanımlama (`AudioKey`)

Scriptlerinizde elle string yazmak yerine doğrudan `AudioKey` tanımlayın:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    // Inspector'da arama çubuklu açılır menü çıkar!
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
