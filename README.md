# AudioSystem for Unity

**AudioSystem**, Unity projeleri için katalog güdümlü (Catalog-Driven), yüksek performanslı, havuzlamalı (Pooling) ve Inspector dostu bir ses yönetim motorudur.

---

## 🎯 Temel Felsefe: "Katalog Tek Gerçektir (Single Source of Truth)"

Klasik Unity projelerinde ses yönetimi genellikle kaosa dönüşür:
- Sahnelerdeki scriptler kafalarına göre `AudioSource.PlayClipAtPoint` çağırır veya doğrudan `AudioClip` referansları taşır.
- Seslerin mikser grupları, ses seviyeleri ve optimizasyonları kontrol edilemez hale gelir.

**AudioSystem bu yan yolları tamamen kapatır:**
1. **Kaçak Ses Çalma Yasaktır:** Projede çalınacak her ses mutlaka bir **AudioCatalog (ScriptableObject)** kütüphanesine kayıtlı olmalıdır.
2. **Yalnızca Kimlik (ID) ile Çağrı:** Kod yazanlar sadece sesin ID'sini bilir (`Audio.Play("ui_click")`), arka plandaki `AudioClip`, `AudioRandomContainer` veya mikser detaylarıyla uğraşmaz.
3. **Arama Çubuğu Olan Inspector Seçicisi:** `[AudioId]` özniteliği sayesinde Inspector'da string yazmak yerine tüm katalogdaki sesleri arayarak tek tıkla seçersiniz.
4. **Sıfır Bellek Tahsisi (AudioSource Pooling):** Çalınan her ses için yeni nesne yaratılmaz; arka planda dinamik bir `SoundEmitter` havuzu çalışır.

---

## 📦 Kurulum (Unity Package Manager)

1. Unity'de **Window > Package Manager** penceresini açın.
2. Sol üstteki **`+`** butonuna tıklayın ve **"Add package from git URL..."** seçin.
3. Git URL'sini yapıştırıp **Add** butonuna basın:
   ```text
   https://github.com/ChasingImagines/AudioSystem.git
   ```

---

## 🚀 Hızlı Başlangıç (3 Adımda)

### Adım 1: Ses Kataloğu (AudioCatalog) Oluşturma
Proje penceresinde `Assets/Resources/Audio/Catalogs/` klasörüne gidin (yoksa oluşturun):
> Sağ Tık: **`Create > AudioSystem > Audio Catalog`**

Örneğin `SFXCatalog.asset` veya `UICatalog.asset` adını verin ve içine sesleri ekleyin:
- **ID:** `player_jump`
- **Resource:** Unity 6 `AudioClip` veya `AudioRandomContainer` varlığını sürükleyin.
- **Mixer Group:** İsteğe bağlı olarak bu sesin gideceği `AudioMixerGroup`'u atayın.

---

### Adım 2: Inspector'da Ses Seçme (`[AudioId]`)

Bileşenlerinizde string alanlara `[AudioId]` özniteliğini ekleyin:

```csharp
using UnityEngine;
using Yasin.AudioSystem;

public class PlayerMovement : MonoBehaviour
{
    // Inspector'da arama destekli açılır liste çıkar!
    [SerializeField, AudioId] private string jumpSoundId;

    public void Jump()
    {
        // 3D uzamsal ses çalar:
        Audio.Play(jumpSoundId, transform.position);
    }
}
```

---

### Adım 3: C# Kodundan Ses Çalma

Tüm ses işlemleri tek bir statik sınıf üzerinden yürütülür:
```csharp
using Yasin.AudioSystem;
```

#### 1. 2D / Arayüz (UI) Sesi Çalma:
```csharp
Audio.PlayUI("button_click");
// veya kısa hali:
Audio.Play("button_click");
```

#### 2. 3D Uzamsal (Dünya Koordinatlarında) Ses Çalma:
```csharp
Audio.Play("explosion", enemyTransform.position);
```

#### 3. Ses Seviyesi ve Perde (Pitch) Ayarlama:
```csharp
// volume: 0.8f, pitch: 1.2f
Audio.PlayUI("coin_pickup", volume: 0.8f, pitch: 1.2f);
```

#### 4. Arka Plan Müziği (Crossfade Desteğiyle):
```csharp
[SerializeField] private AudioClip levelMusic;

void Start()
{
    // 2 saniyelik yumuşak geçişle (crossfade) müziği başlatır:
    Audio.PlayMusic(levelMusic, fadeDuration: 2f, loop: true);
}
```

---

## 🔍 Neden Bu Sistemi Kullanmalısınız?

- **Unity 6 Uyumlu:** Hem klasik `AudioClip`'leri hem de Unity 6'nın yeni `AudioResource` / `AudioRandomContainer` yapılarını tam destekler.
- **Güvenli Fallback:** Yanlış yazılan veya katalogda bulunamayan bir ID olursa oyun çökmez; konsola açıklayıcı bir uyarı basar.
- **Merkezi Ses Değişimi:** Bir ses efektini değiştirmek istediğinizde 50 farklı prefab gezmezsiniz; sadece katalogdaki ilgili ID'nin klibini değiştirmeniz yeterlidir.

---

## 📄 Lisans
MIT License - Yasin Öztürk
