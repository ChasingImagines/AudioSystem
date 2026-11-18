# AudioSystem for Unity (Türkçe Dokümantasyon)

**AudioSystem**, Unity projelerinde sesleri mantıksal kataloglar halinde düzenleyen, tip güvenli (`AudioKey`) erişim sağlayan ve arka planda AudioSource havuzu (pooling) ile sesleri oynatan hafif bir ses yönetim paketidir.

---

## 🎯 Ne İşe Yarar? (Temel Amacı)

Büyük projelerde ses kliplerini scriptlerin veya prefab'ların içine dağınık şekilde bağlamak yönetimi zorlaştırır. AudioSystem bu süreci şu şekilde çözer:

1. **Kataloglama:** Sesleri kategorilerine göre ScriptableObject kataloglarında toplarsınız (örn: `PlayerAudioCatalog`, `UIAudioCatalog`). Bir sesi veya mikser grubunu değiştirmek istediğinizde prefab gezmez, doğrudan katalogdan değiştirirsiniz.
2. **Tip Güvenli `AudioKey`:** Kod tarafında string ile `"jump"` yazmak yerine `AudioKey` kullanılır. Bu sayede yazım hatası (typo) riski olmaz. Inspector'da tüm katalogları tarayan arama kutulu bir açılır liste çıkar.
3. **Otomatik Havuzlama (Zero-GC Pooling):** Çalınan her ses için yeni `GameObject` oluşturulup yok edilmez; arka planda dinamik bir `SoundEmitter` havuzu çalışır.
4. **Resources veya Sürükle-Bırak Gerektirmez:** Katalogları bir veritabanına elle eklemeniz veya `Resources` klasörü açmanız gerekmez. Editör projedeki tüm `AudioCatalog` dosyalarını otomatik olarak tanır.

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

### 1. Ses Kataloğu (AudioCatalog) Oluşturma
Project penceresinde istediğiniz bir klasörde sağ tıklayın:
> **`Create > AudioSystem > Audio Catalog`**

Örn: `PlayerSounds.asset` adını verin ve içine sesleri ekleyin:
- **ID:** `jump`, `footstep`, `attack`
- **Resource:** Unity 6 `AudioClip` veya `AudioRandomContainer` dosyasını atayın.
- **Mixer Group:** İsteğe bağlı olarak ilgili ses mikserini bağlayın.

---

### 2. Inspector'da Ses Tanımlama (`AudioKey`)

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerCombat : MonoBehaviour
{
    // Inspector'da tüm kataloglardaki sesleri listeleyen arama kutulu açılır menü çıkar
    [SerializeField] private AudioKey attackSound;

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

### 3. C# Kodundan Ses Çalma (`AudioSystem.`)

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
        // 1. 2D / UI Sesi Çalma:
        AudioSystem.PlayUI(uiClickSound);

        // 2. 3D Uzamsal Ses Çalma (Konum, Ses Seviyesi, Pitch):
        AudioSystem.Play(explosionSound, transform.position, volume: 0.9f, pitch: 1.1f);

        // 3. Arka Plan Müziği (Crossfade geçişi ile):
        AudioSystem.PlayMusic(backgroundMusic, fadeDuration: 1.5f, loop: true);

        // 4. Müziği Durdurma:
        AudioSystem.StopMusic(fadeDuration: 1f);

        // 5. Tüm Çalan Sesleri Durdurma:
        AudioSystem.StopAllSounds();
    }
}
```

---

## 📄 Lisans
Bu proje **MIT Lisansı** altında lisanslanmıştır. Yasin Öztürk tarafından geliştirilmiştir.
