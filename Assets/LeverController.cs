using UnityEngine;
using Photon.Pun;
using DG.Tweening;

public class LeverController : MonoBehaviourPun, IInteractable, IPunObservable
{
    [Header("Lever Settings")]
    public Transform leverHandle; // Şalterin kolu
    public Vector3 activatedRotation = new Vector3(-45, 0, 0); // Aktif pozisyonda rotasyon
    public float tweenDuration = 0.5f;
    
    [Header("Audio")]
    public AudioClip leverActivationSound;
    private AudioSource audioSource;
    
    [Header("Visual Feedback")]
    public GameObject activationIndicator; // Aktif olduğunda görünecek indicator
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;
    private Renderer indicatorRenderer;
    
    private bool isActivated = false;
    private bool canBeActivated = false; // Rünler tamamlandıktan sonra true olacak
    private Vector3 originalRotation;
    public RunePuzzleController puzzleController;
    
    public bool IsActivated => isActivated;
    public bool CanBeActivated => canBeActivated;

    private void Start()
    {
        // Audio source'u al veya oluştur
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Orijinal rotasyonu kaydet
        if (leverHandle != null)
            originalRotation = leverHandle.localEulerAngles;
        else
            originalRotation = transform.localEulerAngles;
            
        // Indicator renderer'ı al
        if (activationIndicator != null)
        {
            indicatorRenderer = activationIndicator.GetComponent<Renderer>();
            UpdateIndicatorColor();
        }
    }

    public void SetCanBeActivated(bool canActivate)
    {
        canBeActivated = canActivate;
        UpdateIndicatorColor();
        
        if (canActivate)
        {
            Debug.Log("Lever can now be activated! All runes are correctly placed.");
        }
    }

    // IInteractable interface implementations
    public void Interact()
    {
        if (!canBeActivated)
        {
            Debug.Log("This lever cannot be activated yet. Complete the rune puzzle first!");
            return;
        }
        
        if (isActivated)
        {
            Debug.Log("This lever has already been activated.");
            return;
        }
        
        // Herhangi bir oyuncu lever'ı aktive edebilir
        photonView.RPC("ActivateLever", RpcTarget.All);
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        // Bu lever eşya gerektirmez, normal etkileşim yeterli
        Interact();
    }

    public string GetInteractText()
    {
        if (!canBeActivated)
            return "Complete the rune puzzle first";
        
        if (isActivated)
            return "Already activated";
            
        return "Activate Lever";
    }

    [PunRPC]
    private void ActivateLever()
    {
        if (isActivated) return;
        
        isActivated = true;
        
        // Ses çal
        if (leverActivationSound != null && audioSource != null)
            audioSource.PlayOneShot(leverActivationSound);
            
        // Lever animasyonu
        Transform targetTransform = leverHandle != null ? leverHandle : transform;
        targetTransform.DOLocalRotate(originalRotation + activatedRotation, tweenDuration)
            .SetEase(Ease.OutBounce);
            
        // Indicator güncelle
        UpdateIndicatorColor();
        
        // Puzzle controller'a bildir - sadece local metod çağır
        if (puzzleController != null)
            puzzleController.OnLeverActivated();
            
        Debug.Log("Lever activated! Opening doors...");
    }

    private void UpdateIndicatorColor()
    {
        if (indicatorRenderer == null) return;
        
        Color targetColor = inactiveColor;
        
        if (isActivated)
            targetColor = activeColor;
        else if (canBeActivated)
            targetColor = Color.yellow; // Aktive edilebilir durumda sarı
            
        indicatorRenderer.material.color = targetColor;
    }

    public void ResetLever()
    {
        photonView.RPC("ResetLeverRPC", RpcTarget.All);
    }

    [PunRPC]
    private void ResetLeverRPC()
    {
        if (!isActivated) return;
        
        isActivated = false;
        canBeActivated = false;
        
        // Lever pozisyonunu sıfırla
        Transform targetTransform = leverHandle != null ? leverHandle : transform;
        targetTransform.DOLocalRotate(originalRotation, tweenDuration);
        
        // Indicator'ı güncelle
        UpdateIndicatorColor();
        
        Debug.Log("Lever reset to initial state");
    }

    // IPunObservable implementation - lever durumlarını senkronize et
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Veri gönder
            stream.SendNext(isActivated);
            stream.SendNext(canBeActivated);
        }
        else
        {
            // Veri al
            bool newIsActivated = (bool)stream.ReceiveNext();
            bool newCanBeActivated = (bool)stream.ReceiveNext();
            
            // Durumları güncelle
            if (isActivated != newIsActivated || canBeActivated != newCanBeActivated)
            {
                isActivated = newIsActivated;
                canBeActivated = newCanBeActivated;
                UpdateIndicatorColor();
                
                // Lever pozisyonunu güncelle
                if (leverHandle != null || transform != null)
                {
                    Transform targetTransform = leverHandle != null ? leverHandle : transform;
                    Vector3 targetRotation = isActivated ? (originalRotation + activatedRotation) : originalRotation;
                    targetTransform.localEulerAngles = targetRotation;
                }
            }
        }
    }
}
