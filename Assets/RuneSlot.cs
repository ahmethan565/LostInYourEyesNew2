// RuneSlot.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Player sınıfı için gerekli
using DG.Tweening; // DoTween için gerekli

public class RuneSlot : MonoBehaviourPun, IInteractable, IPunObservable
{
    public string requiredRuneID;
    public Transform runePlacementPoint; // Rün'ün yerleştirileceği pozisyon

    private bool _isAnyRunePlaced = false;
    private bool _isCorrectRunePlaced = false;

    public bool IsCorrectRunePlaced
    {
        get { return _isCorrectRunePlaced; }
        private set
        {
            _isCorrectRunePlaced = value;
            UpdateVisualState(); // Rün doğru yerleştirildiğinde veya alındığında görsel durumu günceller
        }
    }

    public RunePuzzleController puzzleController;
    private GameObject currentPlacedRune = null; // Yerleştirilen rün GameObject'i

    [Header("Sound Settings")]
    public AudioClip wrongRuneSound;
    public AudioClip correctRuneSound; // Doğru rün yerleştirildiğinde çalacak ses
    public AudioClip runePlacementSound; // Her rün yerleştirildiğinde çalacak genel ses
    public AudioClip guidanceSpawnSound; // Trail efekti spawn olduğunda çalacak ses
    private AudioSource audioSource;

    [Header("Visual Effect Objects")]
    public GameObject correctRuneIndicator; // Tik işareti objesi
    public GameObject wrongRuneIndicator; // Çarpı işareti objesi
    
    [Header("Effect Settings")]
    public float effectDuration = 0.5f; // Efekt süresi
    public float pulseScale = 1.3f; // Pulse efekti için ölçek
    public int pulseCount = 3; // Kaç kez pulse yapacak
    
    // YENİ: Trail Efekti ve Lever Yönlendirme
    [Header("Lever Guidance Settings")]
    public GameObject trailEffectPrefab; // Trail efektli prefab (opsiyonel)
    public Transform targetLever; // Bu slotun bağlı olduğu lever
    
    [Header("Guidance Animation Settings")]
    public float guidanceDelay = 1f; // Doğru rün yerleştirildikten ne kadar sonra rehber obje yaratılacak
    public float guidanceSpeed = 5f; // Rehber objenin hareket hızı
    public int repeatCount = 3; // Kaç kez tekrarlansın (0 = sonsuz)
    public float repeatDelay = 2f; // Tekrarlar arası bekleme süresi
    public bool useNoiseMovement = false; // Sallantılı hareket kullanılsın mı?
    public float noiseAmplitude = 0.5f; // Sallantı genliği
    public float noiseFrequency = 3f; // Sallantı frekansı
    public Ease movementEase = Ease.OutQuad; // Hareket easing
    
    [Header("Guidance Visual Settings")]
    public bool createFallbackObject = true; // Trail prefab yoksa basit obje yaratılsın mı?
    public Color fallbackObjectColor = Color.yellow; // Fallback obje rengi
    public float fallbackObjectSize = 0.2f; // Fallback obje boyutu
    
    // Orijinal scale'leri sakla
    private Vector3 correctIndicatorOriginalScale;
    private Vector3 wrongIndicatorOriginalScale;
    
    // Performance için guidance tracking
    private Coroutine currentGuidanceCoroutine;
    private GameObject currentGuidanceObject;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D ses için
        }
        
        // Visual effect objelerini başlangıçta gizle
        if (correctRuneIndicator != null)
        {
            correctIndicatorOriginalScale = correctRuneIndicator.transform.localScale; // Orijinal scale'i kaydet
            correctRuneIndicator.SetActive(false);
            Debug.Log($"Correct indicator found and hidden: {correctRuneIndicator.name}, Original Scale: {correctIndicatorOriginalScale}");
        }
        else
        {
            Debug.LogError("Correct Rune Indicator is not assigned in inspector!");
        }
        
        if (wrongRuneIndicator != null)
        {
            wrongIndicatorOriginalScale = wrongRuneIndicator.transform.localScale; // Orijinal scale'i kaydet
            wrongRuneIndicator.SetActive(false);
            Debug.Log($"Wrong indicator found and hidden: {wrongRuneIndicator.name}, Original Scale: {wrongIndicatorOriginalScale}");
        }
        else
        {
            Debug.LogError("Wrong Rune Indicator is not assigned in inspector!");
        }
            
        UpdateVisualState(); // Başlangıçta slotun rengini ayarlar
    }

    private void OnDestroy()
    {
        // DoTween'leri temizle
        if (correctRuneIndicator != null)
            correctRuneIndicator.transform.DOKill();
        if (wrongRuneIndicator != null)
            wrongRuneIndicator.transform.DOKill();
            
        // Guidance coroutine'ini durdur
        if (currentGuidanceCoroutine != null)
        {
            StopCoroutine(currentGuidanceCoroutine);
            currentGuidanceCoroutine = null;
        }
        
        // Guidance objesini temizle
        if (currentGuidanceObject != null)
        {
            currentGuidanceObject.transform.DOKill();
            Destroy(currentGuidanceObject);
            currentGuidanceObject = null;
        }
    }

    private void UpdateVisualState()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer)
        {
            if (_isAnyRunePlaced)
            {
                // Rün yerleştirilmişse, doğru olup olmadığına göre renk ayarlar
                renderer.material.color = _isCorrectRunePlaced ? Color.green : Color.red;
            }
            else
            {
                // Rün yerleştirilmemişse gri renk
                renderer.material.color = Color.gray;
                // Slot boşsa tüm efektleri kapat
                HideAllEffects();
            }
        }
    }

    // Doğru rün efektini göster
    private void ShowCorrectRuneEffect()
    {
        Debug.Log($"ShowCorrectRuneEffect called! correctRuneIndicator is null: {correctRuneIndicator == null}");
        
        HideAllEffects(); // Önce diğer efektleri kapat
        
        if (correctRuneIndicator != null)
        {
            Debug.Log($"Activating correct rune indicator: {correctRuneIndicator.name}");
            correctRuneIndicator.SetActive(true);
            
            // Başlangıçta küçük yap (orijinal scale'in %10'u)
            correctRuneIndicator.transform.localScale = correctIndicatorOriginalScale * 0.1f;
            
            // DoTween ile pulse efekti - orijinal scale'e göre
            Sequence sequence = DOTween.Sequence();
            sequence.Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale * pulseScale, effectDuration * 0.3f).SetEase(Ease.OutBack))
                   .Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale, effectDuration * 0.2f).SetEase(Ease.InOutSine))
                   .AppendInterval(0.1f)
                   .Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale * pulseScale * 0.8f, effectDuration * 0.15f).SetEase(Ease.OutSine))
                   .Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale, effectDuration * 0.15f).SetEase(Ease.InOutSine))
                   .AppendInterval(0.1f)
                   .Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale * pulseScale * 0.6f, effectDuration * 0.1f).SetEase(Ease.OutSine))
                   .Append(correctRuneIndicator.transform.DOScale(correctIndicatorOriginalScale, effectDuration * 0.1f).SetEase(Ease.InOutSine));
                   
            sequence.Play();
        }
    }

    // Yanlış rün efektini göster
    private void ShowWrongRuneEffect()
    {
        Debug.Log($"ShowWrongRuneEffect called! wrongRuneIndicator is null: {wrongRuneIndicator == null}");
        
        HideAllEffects(); // Önce diğer efektleri kapat
        
        if (wrongRuneIndicator != null)
        {
            Debug.Log($"Activating wrong rune indicator: {wrongRuneIndicator.name}");
            wrongRuneIndicator.SetActive(true);
            
            // Başlangıçta küçük yap (orijinal scale'in %10'u)
            wrongRuneIndicator.transform.localScale = wrongIndicatorOriginalScale * 0.1f;
            
            // DoTween ile sarsıntı + pulse efekti - orijinal scale'e göre
            Sequence sequence = DOTween.Sequence();
            sequence.Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale * pulseScale, effectDuration * 0.3f).SetEase(Ease.OutBack))
                   .Join(wrongRuneIndicator.transform.DOShakeRotation(effectDuration * 0.3f, new Vector3(0, 0, 10), 10, 90))
                   .Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale, effectDuration * 0.2f).SetEase(Ease.InOutSine))
                   .AppendInterval(0.1f)
                   .Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale * pulseScale * 0.8f, effectDuration * 0.15f).SetEase(Ease.OutSine))
                   .Join(wrongRuneIndicator.transform.DOShakeRotation(effectDuration * 0.15f, new Vector3(0, 0, 5), 5, 90))
                   .Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale, effectDuration * 0.15f).SetEase(Ease.InOutSine))
                   .AppendInterval(0.1f)
                   .Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale * pulseScale * 0.6f, effectDuration * 0.1f).SetEase(Ease.OutSine))
                   .Append(wrongRuneIndicator.transform.DOScale(wrongIndicatorOriginalScale, effectDuration * 0.1f).SetEase(Ease.InOutSine));
                   
            sequence.Play();
        }
    }

    // Tüm efektleri gizle
    private void HideAllEffects()
    {
        Debug.Log("HideAllEffects called!");
        
        if (correctRuneIndicator != null)
        {
            correctRuneIndicator.transform.DOKill(); // Çalışan tween'leri durdur
            correctRuneIndicator.transform.localScale = correctIndicatorOriginalScale; // Orijinal scale'e dön
            correctRuneIndicator.SetActive(false);
            Debug.Log($"Hiding correct indicator: {correctRuneIndicator.name}");
        }
        
        if (wrongRuneIndicator != null)
        {
            wrongRuneIndicator.transform.DOKill(); // Çalışan tween'leri durdur
            wrongRuneIndicator.transform.localScale = wrongIndicatorOriginalScale; // Orijinal scale'e dön
            wrongRuneIndicator.SetActive(false);
            Debug.Log($"Hiding wrong indicator: {wrongRuneIndicator.name}");
        }
    }

    public string GetInteractText()
    {
        // Eğer rün yerleştirilmişse
        if (_isAnyRunePlaced)
        {
            // Eğer doğru rün yerleştirilmişse, geri alma seçeneği gösterme
            if (IsCorrectRunePlaced)
            {
                return $"Rune placed: {requiredRuneID}"; // Sadece bilgi göster
            }
            else
            {
                return "Take back Rune (E)"; // Yanlış rün yerleşmişse geri alma seçeneği
            }
        }
        else
        {
            return $"Place {requiredRuneID} (E)"; // Slot boşsa yerleştirme seçeneği
        }
    }

    public void Interact()
    {
        // Eğer slot doluysa (herhangi bir rünle), geri alma işlemini başlat
        if (_isAnyRunePlaced)
        {
            // Doğru rün yerleştirilmişse geri almayı engelle
            if (IsCorrectRunePlaced)
            {
                Debug.Log("Correct rune is placed, cannot take it back.");
                return; // Geri alma işlemini durdur
            }

            // Envanter dolu mu kontrolü: Eğer envanter doluysa geri almayı engelle
            if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingItem())
            {
                Debug.Log("Inventory is full. Cannot take back rune.");
                // Oyuncuya bir UI mesajı gösterebilirsiniz.
                return;
            }

            // Envanter boşsa ve yanlış rün varsa, rünü geri alma isteğini gönder
            RequestTakeBackRune(PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            Debug.Log("Slot is empty, cannot take back a rune.");
        }
    }

    public void InteractWithItem(GameObject heldItemGO)
    {
        // Slot zaten doluysa, elde item yoksa veya doğru rün yerleştirilmişse işlem yapma.
        if (_isAnyRunePlaced || heldItemGO == null || IsCorrectRunePlaced) return;

        ItemPickup pickup = heldItemGO.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            Debug.LogError("Held item does not have an ItemPickup component.");
            return;
        }

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("Held item does not have a PhotonView. Cannot place rune.");
            return;
        }

        // Slotun sahibi değilsek, RPC ile sahibine bildir.
        if (!photonView.IsMine)
        {
            photonView.RPC("RPC_RequestRunePlacement", photonView.Owner, heldItemPV.ViewID, pickup.itemID, PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }

        // Slotun sahibiysek, işlemi doğrudan başlat.
        ProcessRunePlacement(heldItemGO, pickup.itemID, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_RequestRunePlacement(int heldItemViewID, string heldItemItemID, int placingPlayerActorNumber, PhotonMessageInfo info)
    {
        GameObject heldItemGO = PhotonView.Find(heldItemViewID)?.gameObject;
        if (heldItemGO == null)
        {
            Debug.LogError($"RPC_RequestRunePlacement: Could not find held item with ViewID {heldItemViewID}");
            return;
        }
        ProcessRunePlacement(heldItemGO, heldItemItemID, placingPlayerActorNumber);
    }

    void ProcessRunePlacement(GameObject heldItemGO, string heldItemItemID, int placingPlayerActorNumber)
    {
        // Slot zaten doluysa veya doğru rün yerleştirildiyse tekrar işlem yapma.
        if (_isAnyRunePlaced || IsCorrectRunePlaced || heldItemGO == null) return;

        PhotonView heldItemPV = heldItemGO.GetComponent<PhotonView>();
        if (heldItemPV == null)
        {
            Debug.LogError("The item being placed does not have a PhotonView. It cannot be synchronized.");
            return;
        }

        // Rünü tüm istemcilerde görsel olarak slota yerleştir
        photonView.RPC("RPC_PlaceRuneVisuals", RpcTarget.AllBuffered, heldItemPV.ViewID);

        // Her rün yerleştirildiğinde genel placement sesi çal
        if (audioSource != null && runePlacementSound != null)
        {
            audioSource.PlayOneShot(runePlacementSound);
        }

        // Rünün doğru olup olmadığını kontrol et
        if (heldItemItemID == requiredRuneID)
        {
            IsCorrectRunePlaced = true; // Setter UpdateVisualState'i çağırır
            Debug.Log($"Correct rune '{requiredRuneID}' placed in slot {gameObject.name}.");
            
            // Doğru rün sesi çal
            if (audioSource != null && correctRuneSound != null)
            {
                audioSource.PlayOneShot(correctRuneSound);
            }
            
            // Doğru rün efektini göster (tüm istemcilerde)
            photonView.RPC("RPC_ShowCorrectEffect", RpcTarget.AllBuffered);
            
            // YENİ: Trail efektli rehber objeyi yaratıp levere gönder
            if (targetLever != null && (trailEffectPrefab != null || createFallbackObject))
            {
                photonView.RPC("RPC_CreateGuidanceObject", RpcTarget.AllBuffered);
            }
            
            // Tüm istemcilerde renk güncelleme
            photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, true, true);
        }
        else
        {
            IsCorrectRunePlaced = false; // Setter UpdateVisualState'i çağırır
            
            // Yanlış rün sesi çal
            if (audioSource != null && wrongRuneSound != null)
            {
                audioSource.PlayOneShot(wrongRuneSound);
            }
            
            Debug.LogWarning($"Incorrect rune '{heldItemItemID}' placed in slot {gameObject.name}. Expected '{requiredRuneID}'.");
            
            // Yanlış rün efektini göster (tüm istemcilerde)
            photonView.RPC("RPC_ShowWrongEffect", RpcTarget.AllBuffered);
            
            // Tüm istemcilerde renk güncelleme
            photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, true, false);
        }

        _isAnyRunePlaced = true;
        currentPlacedRune = heldItemGO; // Yerleştirilen rünü kaydet

        // Rünü yerleştiren oyuncunun envanterinden item'ı düşürmesini iste
        Player placingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(placingPlayerActorNumber);
        if (placingPlayer != null)
        {
            photonView.RPC("RPC_TriggerForceDrop", placingPlayer);
        }

        // PuzzleController'a rün yerleştirildiğini bildir (puzzle durumunu kontrol etmesi için)
        if (puzzleController != null)
            puzzleController.NotifyRunePlaced();
    }

    [PunRPC]
    void RPC_PlaceRuneVisuals(int runeViewID)
    {
        GameObject runeGO = PhotonView.Find(runeViewID)?.gameObject;
        if (runeGO == null)
        {
            Debug.LogError($"RPC_PlaceRuneVisuals: Could not find rune with ViewID {runeViewID}");
            return;
        }

        Vector3 originalScale = runeGO.transform.lossyScale; // Orijinal dünya ölçeğini koru
        runeGO.transform.SetParent(runePlacementPoint, false); // Slotun altına parent yap
        SetWorldScale(runeGO.transform, originalScale); // Dünya ölçeğini tekrar ayarla
        runeGO.transform.localPosition = Vector3.zero; // Lokal pozisyonu sıfırla
        runeGO.transform.localRotation = Quaternion.identity; // Lokal rotasyonu sıfırla

        // Rün slota yerleşince collider ve rigidbody'yi kapat
        Collider col = runeGO.GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = runeGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        _isAnyRunePlaced = true;
        currentPlacedRune = runeGO; // Yerleştirilen rünü senkronize et

        Debug.Log($"Rune {runeGO.name} (ID: {runeViewID}) visually placed in slot {gameObject.name} on all clients.");
    }

    [PunRPC]
    void RPC_TriggerForceDrop(PhotonMessageInfo info)
    {
        // Rünü yerleştiren oyuncunun envanterinden rünü "düşürmesini" tetikle
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ForceDropItem();
        }
        else
        {
            Debug.LogError("InventorySystem.Instance is not found on this client. Cannot force drop item.");
        }
    }

    // Objelerin dünya ölçeğini koruyarak parent değiştirmesini sağlayan yardımcı metot
    void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t.parent == null)
        {
            t.localScale = worldScale;
        }
        else
        {
            Vector3 parentScale = t.parent.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Durumları senkronize et
            stream.SendNext(_isAnyRunePlaced);
            stream.SendNext(_isCorrectRunePlaced);
            stream.SendNext(currentPlacedRune != null ? currentPlacedRune.GetComponent<PhotonView>().ViewID : -1);
        }
        else
        {
            // Durumları al ve güncelle
            bool receivedAnyRunePlaced = (bool)stream.ReceiveNext();
            bool receivedCorrectRunePlaced = (bool)stream.ReceiveNext();
            int receivedRuneViewID = (int)stream.ReceiveNext();

            if (_isAnyRunePlaced != receivedAnyRunePlaced || _isCorrectRunePlaced != receivedCorrectRunePlaced)
            {
                _isAnyRunePlaced = receivedAnyRunePlaced;
                _isCorrectRunePlaced = receivedCorrectRunePlaced;
                UpdateVisualState();
            }

            if (receivedAnyRunePlaced && receivedRuneViewID != -1)
            {
                GameObject receivedRuneGO = PhotonView.Find(receivedRuneViewID)?.gameObject;
                if (receivedRuneGO != null && receivedRuneGO.transform.parent != runePlacementPoint)
                {
                    // Eğer rün henüz slota parent yapılmamışsa, görselini ayarla
                    Vector3 originalScale = receivedRuneGO.transform.lossyScale;
                    receivedRuneGO.transform.SetParent(runePlacementPoint, false);
                    SetWorldScale(receivedRuneGO.transform, originalScale);
                    receivedRuneGO.transform.localPosition = Vector3.zero;
                    receivedRuneGO.transform.localRotation = Quaternion.identity;

                    Collider col = receivedRuneGO.GetComponent<Collider>();
                    if (col) col.enabled = false;
                    Rigidbody rb = receivedRuneGO.GetComponent<Rigidbody>();
                    if (rb) { rb.isKinematic = true; rb.useGravity = false; }
                }
                currentPlacedRune = receivedRuneGO;
            }
            else if (!receivedAnyRunePlaced && currentPlacedRune != null)
            {
                // Slot boşaldıysa referansı temizle
                currentPlacedRune = null;
            }
        }
    }

    // Rünü geri alma isteğini başlatır (slotun sahibinden)
    private void RequestTakeBackRune(int requestingPlayerActorNumber)
    {
        if (currentPlacedRune == null)
        {
            Debug.LogWarning("No rune to take back from this slot.");
            return;
        }

        // Doğru rün yerleştirilmişse geri almayı engelle
        if (IsCorrectRunePlaced)
        {
            Debug.Log("Correct rune is placed, cannot take it back.");
            return; // Geri alma işlemini durdur
        }

        // Slotun sahibi değilsek, RPC ile sahibine bildir.
        if (!photonView.IsMine)
        {
            photonView.RPC("RPC_RequestRuneTakeBack", photonView.Owner, requestingPlayerActorNumber);
            return;
        }

        // Slotun sahibiyiz, işlemi doğrudan başlat.
        ProcessRuneTakeBack(requestingPlayerActorNumber);
    }

    // Yeni RPC: Rünü geri alma isteğini slotun sahibine iletir.
    [PunRPC]
    private void RPC_RequestRuneTakeBack(int requestingPlayerActorNumber, PhotonMessageInfo info)
    {
        // Sadece slotun sahibi olan istemcide çalışır.
        ProcessRuneTakeBack(requestingPlayerActorNumber);
    }

    // Değiştirilmiş metod: Rünü geri alma mantığını işler - artık envantere almaz, yere düşürür
    private void ProcessRuneTakeBack(int requestingPlayerActorNumber)
    {
        if (currentPlacedRune == null) return; // Zaten boşsa işlem yapma

        // Slotu tüm istemcilerde görsel olarak boşalt ve durumunu sıfırla.
        photonView.RPC("RPC_ClearRuneSlotVisuals", RpcTarget.AllBuffered);

        // PuzzleController'a bildir (rün geri alındığında puzzle durumu değiştiği için)
        if (puzzleController != null)
        {
            puzzleController.NotifyRunePlaced(); // Puzzle durumu tekrar kontrol edilecek
        }
    }

    // Değiştirilmiş RPC: Rün slotunu tüm istemcilerde boşaltır ve rünü yere düşürür
    [PunRPC]
    private void RPC_ClearRuneSlotVisuals()
    {
        if (currentPlacedRune != null)
        {
            // Rünü parent'ından ayır ve dünyaya yerleştir
            currentPlacedRune.transform.SetParent(null);
            
            // Rünün pozisyonunu slot pozisyonuna göre ayarla (öne ve yukarıda başlasın ki düşsün)
            Vector3 dropPosition = runePlacementPoint.position + runePlacementPoint.forward * -1 + Vector3.up * 1;
            currentPlacedRune.transform.position = dropPosition;
            
            // Rünün fiziksel durumunu eski haline getir
            Collider col = currentPlacedRune.GetComponent<Collider>();
            if (col) col.enabled = true;
            
            Rigidbody rb = currentPlacedRune.GetComponent<Rigidbody>();
            if (rb) 
            { 
                rb.isKinematic = false; 
                rb.useGravity = true; 
            }
            
            // ItemPickup durumunu güncelle
            ItemPickup pickup = currentPlacedRune.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.IsHeld = false;
            }
            
            // Eğer bu istemci rünün sahibiyse, düşme simülasyonunu başlat
            PhotonView runePhotonView = currentPlacedRune.GetComponent<PhotonView>();
            if (runePhotonView != null && runePhotonView.IsMine)
            {
                // RPC ile rünün düşme durumunu tüm istemcilere bildir
                runePhotonView.RPC("RPC_SetItemState", RpcTarget.AllBuffered, false);
            }
        }

        _isAnyRunePlaced = false;
        IsCorrectRunePlaced = false; // Setter UpdateVisualState'i çağırır (slotu griye çevirir)
        currentPlacedRune = null; // Yerleştirilen rün referansını temizle

        // Tüm efektleri gizle
        HideAllEffects();

        // Guidance objesini durdur
        StopGuidanceObject();

        // Tüm istemcilerde slot rengini güncelle (gri yap)
        photonView.RPC("RPC_UpdateSlotColor", RpcTarget.AllBuffered, false, false);
        
        // Tüm istemcilerde efektleri gizle
        photonView.RPC("RPC_HideAllEffects", RpcTarget.AllBuffered);

        Debug.Log($"Rune slot {gameObject.name} cleared and rune dropped to ground on all clients.");
    }

    // Yeni RPC: Slot rengini tüm istemcilerde günceller
    [PunRPC]
    private void RPC_UpdateSlotColor(bool isAnyRunePlaced, bool isCorrectRunePlaced)
    {
        _isAnyRunePlaced = isAnyRunePlaced;
        _isCorrectRunePlaced = isCorrectRunePlaced;
        UpdateVisualState();
    }

    // Yeni RPC: Doğru rün efektini tüm istemcilerde göster
    [PunRPC]
    private void RPC_ShowCorrectEffect()
    {
        ShowCorrectRuneEffect();
    }

    // Yeni RPC: Yanlış rün efektini tüm istemcilerde göster
    [PunRPC]
    private void RPC_ShowWrongEffect()
    {
        ShowWrongRuneEffect();
    }

    // Yeni RPC: Tüm efektleri tüm istemcilerde gizle
    [PunRPC]
    private void RPC_HideAllEffects()
    {
        HideAllEffects();
    }

    // YENİ: Rehber objeyi yaratıp levere gönderen RPC
    [PunRPC]
    private void RPC_CreateGuidanceObject()
    {
        // Eğer zaten bir guidance coroutine çalışıyorsa durdur
        if (currentGuidanceCoroutine != null)
        {
            StopCoroutine(currentGuidanceCoroutine);
            currentGuidanceCoroutine = null;
        }
        
        // Eski guidance objesini temizle
        if (currentGuidanceObject != null)
        {
            currentGuidanceObject.transform.DOKill();
            Destroy(currentGuidanceObject);
            currentGuidanceObject = null;
        }
        
        currentGuidanceCoroutine = StartCoroutine(CreateGuidanceObjectCoroutine());
    }

    // YENİ: Rehber obje yaratma ve hareket ettirme coroutine'i
    private System.Collections.IEnumerator CreateGuidanceObjectCoroutine()
    {
        // Belirtilen süre kadar bekle
        yield return new WaitForSeconds(guidanceDelay);
        
        if (targetLever == null)
        {
            Debug.LogWarning($"Target lever not assigned for slot {gameObject.name}");
            yield break;
        }

        // Guidance objesini yaratmaya çalış
        currentGuidanceObject = CreateGuidanceObject();
        if (currentGuidanceObject == null)
        {
            Debug.LogWarning($"Could not create guidance object for slot {gameObject.name}");
            currentGuidanceCoroutine = null;
            yield break;
        }

        // Trail efekti spawn sesi çal
        if (audioSource != null && guidanceSpawnSound != null)
        {
            audioSource.PlayOneShot(guidanceSpawnSound);
        }

        // Tekrar sayısını kontrol et
        int currentRepeat = 0;
        bool infiniteLoop = (repeatCount == 0);

        while (infiniteLoop || currentRepeat < repeatCount)
        {
            // Objeyi levere doğru hareket ettir
            yield return StartCoroutine(MoveGuidanceObjectToLever(currentGuidanceObject));
            
            currentRepeat++;
            
            // Sonsuz döngü değilse ve tekrar sayısına ulaştıysak çık
            if (!infiniteLoop && currentRepeat >= repeatCount)
                break;
                
            // Tekrar bekleme süresi
            yield return new WaitForSeconds(repeatDelay);
            
            // Objeyi başlangıç pozisyonuna geri getir
            if (currentGuidanceObject != null)
            {
                currentGuidanceObject.transform.position = runePlacementPoint.position + Vector3.up * 0.5f;
            }
        }
        
        // Hedefe ulaştıktan sonra objeyi yok et
        if (currentGuidanceObject != null)
        {
            // DoTween ile küçük bir pulse efekti yapıp sonra yok et
            currentGuidanceObject.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    if (currentGuidanceObject != null)
                    {
                        Destroy(currentGuidanceObject);
                        currentGuidanceObject = null;
                    }
                });
        }
        
        currentGuidanceCoroutine = null;
    }

    // YENİ: Guidance objesi yaratma (trail prefab veya fallback)
    private GameObject CreateGuidanceObject()
    {
        GameObject guidanceObject = null;
        Vector3 startPosition = runePlacementPoint.position + Vector3.up * 0.5f;

        // Önce trail prefab'ı dene
        if (trailEffectPrefab != null)
        {
            guidanceObject = Instantiate(trailEffectPrefab, startPosition, Quaternion.identity);
        }
        // Trail prefab yoksa fallback obje yarat
        else if (createFallbackObject)
        {
            guidanceObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            guidanceObject.transform.position = startPosition;
            guidanceObject.transform.localScale = Vector3.one * fallbackObjectSize;
            
            // Rengi ayarla
            Renderer renderer = guidanceObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = fallbackObjectColor;
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Glossiness", 0.8f);
                renderer.material = mat;
            }
            
            // Collider'ı kaldır
            Collider col = guidanceObject.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }

        return guidanceObject;
    }

    // YENİ: Rehber objeyi levere doğru hareket ettiren coroutine (DoTween ile)
    private System.Collections.IEnumerator MoveGuidanceObjectToLever(GameObject guidanceObject)
    {
        if (guidanceObject == null || targetLever == null) yield break;

        Vector3 startPosition = guidanceObject.transform.position;
        Vector3 targetPosition = targetLever.position + Vector3.up * 1f; // Lever'ın biraz üstünde hedefle
        
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float journeyTime = journeyLength / guidanceSpeed;

        // DoTween ile hareket animasyonu
        Sequence moveSequence = DOTween.Sequence();
        
        if (useNoiseMovement)
        {
            // Sallantılı hareket için waypoint'ler oluştur
            Vector3[] waypoints = CreateNoisyPath(startPosition, targetPosition, 10);
            moveSequence.Append(guidanceObject.transform.DOPath(waypoints, journeyTime, PathType.CatmullRom)
                .SetEase(movementEase));
        }
        else
        {
            // Düz hareket
            moveSequence.Append(guidanceObject.transform.DOMove(targetPosition, journeyTime)
                .SetEase(movementEase));
        }

        // Hareket yönüne doğru dönme animasyonu
        Vector3 direction = (targetPosition - startPosition).normalized;
        if (direction != Vector3.zero)
        {
            moveSequence.Join(guidanceObject.transform.DOLookAt(targetPosition, 0.5f));
        }

        // Animasyonun bitmesini bekle
        bool animationComplete = false;
        moveSequence.OnComplete(() => animationComplete = true);
        
        while (!animationComplete && guidanceObject != null)
        {
            yield return null;
        }
    }

    // YENİ: Sallantılı hareket için waypoint'ler oluştur
    private Vector3[] CreateNoisyPath(Vector3 start, Vector3 end, int segmentCount)
    {
        Vector3[] waypoints = new Vector3[segmentCount + 2];
        waypoints[0] = start;
        waypoints[waypoints.Length - 1] = end;

        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular == Vector3.zero) // Eğer yukarı doğru gidiyorsa
            perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

        for (int i = 1; i < waypoints.Length - 1; i++)
        {
            float t = (float)i / (segmentCount + 1);
            Vector3 basePosition = Vector3.Lerp(start, end, t);
            
            // Sinüzoidal sallantı ekle
            float noiseOffset = Mathf.Sin(t * noiseFrequency * Mathf.PI * 2) * noiseAmplitude;
            Vector3 noiseVector = perpendicular * noiseOffset;
            
            // Y ekseninde de hafif sallantı
            noiseVector += Vector3.up * (Mathf.Sin(t * noiseFrequency * Mathf.PI * 4) * noiseAmplitude * 0.3f);
            
            waypoints[i] = basePosition + noiseVector;
        }

        return waypoints;
    }

    // YENİ: Guidance objesini durduran metot
    private void StopGuidanceObject()
    {
        if (currentGuidanceCoroutine != null)
        {
            StopCoroutine(currentGuidanceCoroutine);
            currentGuidanceCoroutine = null;
        }
        
        if (currentGuidanceObject != null)
        {
            currentGuidanceObject.transform.DOKill();
            Destroy(currentGuidanceObject);
            currentGuidanceObject = null;
        }
    }
}