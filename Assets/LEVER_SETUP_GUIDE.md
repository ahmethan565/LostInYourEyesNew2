# Lever Sistemi Kurulum Rehberi (Multi-Lever)

## Sistem Nasıl Çalışır?

1. **Rune Puzzle Tamamlanması**: Tüm rünler doğru yerleştirildiğinde
2. **Tüm Leverlar Aktif Olur**: Şalterler artık kullanılabilir hale gelir (görsel feedback ile)
3. **Belirlenen Sayıda Lever ile Etkileşim**: Oyuncu gerekli sayıda şalter ile etkileşime geçer
4. **Kapılar Açılır**: Sadece gerekli sayıda lever çekildikten sonra kapılar açılır

## Unity'de Kurulum:

### 1. Lever Objeleri Oluşturma:
```
1. Her lever için boş GameObject oluştur (Lever1, Lever2, etc.)
2. Her birine LeverController script'ini ekle
3. Her lever için handle child object oluştur
4. Her lever için activation indicator child object oluştur (küp/küre)
```

### 2. LeverController Inspector Settings (Her lever için):
```
- Lever Handle: Şalterin hareket eden kolu
- Activated Rotation: Aktif olduğunda hangi açıya döneceği (ör: -45, 0, 0)
- Tween Duration: Animasyon süresi
- Lever Activation Sound: Ses dosyası
- Activation Indicator: Durumu gösteren obje
- Active Color: Aktif renk (yeşil)
- Inactive Color: İnaktif renk (kırmızı)
```

### 3. RunePuzzleController Settings:
```
- Levers: Oluşturduğunuz tüm lever objelerini array'e sürükleyin
- Required Levers Count: Kaç lever çekilmesi gerektiği (ör: 2, 3, vb.)
- (0 veya boş bırakırsanız tüm leverlar gerekir)
```

## Test Etme:
1. Rünleri yanlış yerleştirin → Tüm leverlar kırmızı olmalı
2. Rünleri doğru yerleştirin → Tüm leverlar sarı olmalı (aktive edilebilir)
3. İlk lever'ı çekin → Progress mesajı görünmeli (ör: "1/3 completed")
4. Gerekli sayıda lever çekin → Kapılar açılmalı

## Örnekler:
- **3 Lever, 2 Gerekli**: 3 lever var, herhangi 2'sini çekmek yeterli
- **5 Lever, 5 Gerekli**: Tüm 5 lever'ı çekmek gerekir
- **4 Lever, 1 Gerekli**: Herhangi 1 lever'ı çekmek yeterli

## Debug Mesajları:
- "Activate X out of Y levers to open the doors"
- "Lever activated! Progress: X/Y"
- "Need X more lever(s) to open the doors"
- "All required levers activated! Opening doors..."

## Ek Özellikler:
- Network senkronizasyonu (Photon PUN2)
- Görsel feedback (renk değişimi)
- Ses efektleri
- Smooth animasyonlar (DOTween)
- Hata kontrolü ve debug mesajları
- **Multi-lever support**: İstediğiniz sayıda lever
- **Flexible requirements**: Kaç lever gerektiğini ayarlayabilme
- **Progress tracking**: Kaç lever çekildiğini takip etme
- **Reset functionality**: Lever'ları sıfırlama

## Ek Metodlar (Script'ten çağırabilirsiniz):
```csharp
// Progress bilgisi al
string progress = puzzleController.GetLeverProgress(); // "2/3 levers activated"

// Tüm leverlar tamamlandı mı?
bool completed = puzzleController.AreAllRequiredLeversActivated();

// Kaç lever daha gerekiyor?
int remaining = puzzleController.GetRemainingLeversCount();

// Tüm lever'ları sıfırla
puzzleController.ResetAllLevers();
```
