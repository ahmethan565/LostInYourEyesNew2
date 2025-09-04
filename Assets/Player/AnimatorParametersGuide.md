# Animator Controller Parametreleri

Bu dosya, FPSPlayerController için Animator Controller'da kullanılması gereken parametreleri listeler.

## Boolean Parametreler

- **IsIdle** (Bool) - Karakter durgun durumdayken true
- **IsWalking** (Bool) - Karakter yürürken true  
- **IsRunning** (Bool) - Karakter koşarken true
- **IsJumping** (Bool) - Karakter zıplarken true
- **IsCrouching** (Bool) - Karakter çömelirken true
- **IsSliding** (Bool) - Karakter kayarken true
- **IsGrounded** (Bool) - Karakter yerdeyken true

## Float Parametreler

- **Velocity** (Float) - Karakterin yatay hareket hızı (0-8 arası değer alır)

## Animator Controller Kurulumu

1. Character modelinize bir Animator component ekleyin
2. Animator Controller asset'i oluşturun
3. Yukarıdaki parametreleri Animator Controller'a ekleyin
4. State'ler arası geçişleri (transitions) bu parametrelere göre ayarlayın

## Örnek Transition Koşulları

- Idle → Walking: IsWalking == true
- Walking → Running: IsRunning == true
- Any State → Jumping: IsJumping == true
- Jumping → Idle: IsGrounded == true && IsIdle == true

## Walking Animasyonu İçin Öneriler

- Walking döngüsü animasyonu loop olmalı
- Velocity parametresini kullanarak blend tree oluşturabilirsiniz:
  - 0 = Idle animasyon
  - 1-5 = Walk animasyon  
  - 5-8 = Run animasyon
