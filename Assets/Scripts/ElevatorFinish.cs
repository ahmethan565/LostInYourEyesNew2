using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class ElevatorFinish : MonoBehaviourPunCallbacks
{
    [Header("Elevator Settings")]
    [SerializeField] private string nextSceneName = "SecondMap"; // Sonraki sahne adı
    [SerializeField] private float doorCloseDelay = 1f; // Kapıların kapanma gecikmesi (kısaltıldı)
    [SerializeField] private float elevatorMoveDelay = 1f; // Asansör hareket gecikmesi
    [SerializeField] private float fadeDelay = 2f; // Fade başlama gecikmesi
    [SerializeField] private float lockDuration = 0.5f; // Oyuncuları kilitlme süresi
    
    [Header("Door References")]
    [SerializeField] private KeyDoorExample[] elevatorDoors; // Asansör kapıları
    
    [Header("Screen Effects")]
    [SerializeField] private Image fadePanel; // Fade için siyah panel
    [SerializeField] private float fadeDuration = 2f; // Fade süresi
    
    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 3f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip elevatorMoveSound;
    [SerializeField] private AudioClip doorCloseSound;
    private AudioSource audioSource;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Player tracking - GÜVENLİK ARTTIRILDI
    private HashSet<int> playersInElevator = new HashSet<int>();
    private HashSet<int> lockedPlayers = new HashSet<int>(); // Kilitli oyuncular
    private bool elevatorTriggered = false;
    private bool elevatorSequenceStarted = false; // Sequence başladı mı?
    private float lastPlayerEntryTime = 0f; // Son oyuncu giriş zamanı
    private const float ENTRY_GRACE_PERIOD = 0.2f; // Girişler arası minimum süre
    
    // Collider referansları
    private Collider elevatorCollider;
    private List<Collider> playerCollidersInside = new List<Collider>();

    void Awake()
    {
        // AudioSource setup
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D sound
        }
        
        // Fade panel setup
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 0); // Başlangıçta şeffaf
            fadePanel.gameObject.SetActive(true);
        }
        
        // Elevator collider reference
        elevatorCollider = GetComponent<Collider>();
        if (elevatorCollider == null || !elevatorCollider.isTrigger)
        {
            Debug.LogError("[ElevatorFinish] Collider missing or not set as trigger!");
        }
    }

    void Start()
    {
        // Asansör kapılarını kontrol et
        if (elevatorDoors == null || elevatorDoors.Length == 0)
        {
            Debug.LogWarning("[ElevatorFinish] No elevator doors assigned!");
        }
        
        if (fadePanel == null)
        {
            Debug.LogError("[ElevatorFinish] Fade panel not assigned! Elevator won't work properly.");
        }
        
        if (debugMode)
        {
            Debug.Log($"[ElevatorFinish] Initialized. Next scene: {nextSceneName}");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Sequence başladıysa yeni girişleri engelle
        if (elevatorSequenceStarted)
        {
            if (debugMode)
            {
                Debug.Log("[ElevatorFinish] Elevator sequence already started, blocking new entries!");
            }
            return;
        }
        
        // Sadece oyuncu karakterlerini kontrol et
        if (other.CompareTag("Player"))
        {
            PhotonView playerPhotonView = other.GetComponent<PhotonView>();
            if (playerPhotonView != null && playerPhotonView.IsMine)
            {
                int playerID = PhotonNetwork.LocalPlayer.ActorNumber;
                
                // Rate limiting - çok hızlı girişleri engelle
                if (Time.time - lastPlayerEntryTime < ENTRY_GRACE_PERIOD)
                {
                    if (debugMode)
                    {
                        Debug.Log("[ElevatorFinish] Entry too fast, ignoring...");
                    }
                    return;
                }
                
                lastPlayerEntryTime = Time.time;
                
                if (!playersInElevator.Contains(playerID))
                {
                    // Collider'ı kaydet (çıkış kontrolü için)
                    if (!playerCollidersInside.Contains(other))
                    {
                        playerCollidersInside.Add(other);
                    }
                    
                    // RPC ile tüm oyunculara bildir
                    photonView.RPC("PlayerEnteredElevator", RpcTarget.All, playerID);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ElevatorFinish] Player {playerID} entered elevator");
                    }
                }
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Sequence başladıysa çıkışları engelle
        if (elevatorSequenceStarted)
        {
            if (debugMode)
            {
                Debug.Log("[ElevatorFinish] Elevator sequence started, preventing player exit!");
            }
            
            // Oyuncuyu geri inside listesine ekle
            if (!playerCollidersInside.Contains(other))
            {
                playerCollidersInside.Add(other);
            }
            return;
        }
        
        // Oyuncu asansörden çıktı
        if (other.CompareTag("Player"))
        {
            PhotonView playerPhotonView = other.GetComponent<PhotonView>();
            if (playerPhotonView != null && playerPhotonView.IsMine)
            {
                int playerID = PhotonNetwork.LocalPlayer.ActorNumber;
                
                // Kilitli oyuncular çıkamaz
                if (lockedPlayers.Contains(playerID))
                {
                    if (debugMode)
                    {
                        Debug.Log($"[ElevatorFinish] Player {playerID} is locked, cannot exit!");
                    }
                    return;
                }
                
                if (playersInElevator.Contains(playerID))
                {
                    // Collider'ı listeden çıkar
                    playerCollidersInside.Remove(other);
                    
                    // RPC ile tüm oyunculara bildir
                    photonView.RPC("PlayerExitedElevator", RpcTarget.All, playerID);
                    
                    if (debugMode)
                    {
                        Debug.Log($"[ElevatorFinish] Player {playerID} exited elevator");
                    }
                }
            }
        }
    }
    
    [PunRPC]
    void PlayerEnteredElevator(int playerID)
    {
        playersInElevator.Add(playerID);
        
        if (debugMode)
        {
            Debug.Log($"[ElevatorFinish] Players in elevator: {playersInElevator.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
        }
        
        CheckAllPlayersInElevator();
    }
    
    [PunRPC]
    void PlayerExitedElevator(int playerID)
    {
        playersInElevator.Remove(playerID);
        lockedPlayers.Remove(playerID); // Kilidi de kaldır
        
        if (debugMode)
        {
            Debug.Log($"[ElevatorFinish] Players in elevator: {playersInElevator.Count}/{PhotonNetwork.CurrentRoom.PlayerCount}");
        }
    }
    
    void CheckAllPlayersInElevator()
    {
        // Tüm oyuncular asansörde mi kontrol et
        if (!elevatorTriggered && !elevatorSequenceStarted && playersInElevator.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            if (debugMode)
            {
                Debug.Log("[ElevatorFinish] All players in elevator! Starting lock sequence...");
            }
            
            elevatorTriggered = true;
            
            // Sadece Master Client sequence'i başlatsın
            if (PhotonNetwork.IsMasterClient)
            {
                // ÖNCE OYUNCULARI KİLİTLE
                photonView.RPC("LockAllPlayers", RpcTarget.All);
            }
        }
    }
    
    [PunRPC]
    void LockAllPlayers()
    {
        // Tüm asansördeki oyuncuları kilitle
        lockedPlayers.Clear();
        foreach (int playerID in playersInElevator)
        {
            lockedPlayers.Add(playerID);
        }
        
        if (debugMode)
        {
            Debug.Log("[ElevatorFinish] All players locked in elevator!");
        }
        
        // Kısa bir süre bekle, sonra sequence'i başlat
        StartCoroutine(DelayedSequenceStart());
    }
    
    IEnumerator DelayedSequenceStart()
    {
        yield return new WaitForSeconds(lockDuration);
        
        // Sequence başlat
        elevatorSequenceStarted = true;
        
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartElevatorSequence", RpcTarget.All);
        }
    }
    
    [PunRPC]
    void StartElevatorSequence()
    {
        StartCoroutine(ElevatorSequence());
    }
    
    IEnumerator ElevatorSequence()
    {
        if (debugMode)
        {
            Debug.Log("[ElevatorFinish] Starting elevator sequence");
        }
        
        // 1. Adım: Kısa bekleme (artık daha kısa)
        yield return new WaitForSeconds(doorCloseDelay);
        
        // 2. Adım: Kapıları kapat
        CloseDoors();
        yield return new WaitForSeconds(elevatorMoveDelay);
        
        // 3. Adım: Asansör hareketi (Camera shake + ses)
        StartElevatorMovement();
        yield return new WaitForSeconds(fadeDelay);
        
        // 4. Adım: Ekranı karart
        yield return StartCoroutine(FadeToBlack());
        
        // 5. Adım: Sonraki sahneye geç
        LoadNextScene();
    }
    
    void CloseDoors()
    {
        if (debugMode)
        {
            Debug.Log("[ElevatorFinish] Closing elevator doors");
        }
        
        // Kapı kapanma sesi
        if (audioSource != null && doorCloseSound != null)
        {
            audioSource.PlayOneShot(doorCloseSound);
        }
        
        // Tüm asansör kapılarını kapat
        if (elevatorDoors != null)
        {
            foreach (var door in elevatorDoors)
            {
                if (door != null)
                {
                    door.CloseDoor();
                }
            }
        }
    }
    
    void StartElevatorMovement()
    {
        if (debugMode)
        {
            Debug.Log("[ElevatorFinish] Starting elevator movement");
        }
        
        // Asansör hareketi sesi
        if (audioSource != null && elevatorMoveSound != null)
        {
            audioSource.PlayOneShot(elevatorMoveSound);
        }
        
        // Camera shake efekti
        if (cameraShake.instance != null)
        {
            cameraShake.instance.photonView.RPC("RpcTriggerShake", RpcTarget.All, shakeDuration, shakeMagnitude);
        }
        else
        {
            Debug.LogWarning("[ElevatorFinish] cameraShake instance not found!");
        }
    }
    
    IEnumerator FadeToBlack()
    {
        if (debugMode)
        {
            Debug.Log("[ElevatorFinish] Starting fade to black");
        }
        
        if (fadePanel != null)
        {
            // DOTween ile fade efekti
            yield return fadePanel.DOFade(1f, fadeDuration).WaitForCompletion();
        }
        else
        {
            // Fallback: DOTween yoksa normal bekle
            yield return new WaitForSeconds(fadeDuration);
        }
    }
    
    void LoadNextScene()
    {
        if (debugMode)
        {
            Debug.Log($"[ElevatorFinish] Loading next scene: {nextSceneName}");
        }
        
        // Sadece Master Client sahne değişimini yapar
        if (PhotonNetwork.IsMasterClient)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                PhotonNetwork.LoadLevel(nextSceneName);
            }
            else
            {
                Debug.LogError("[ElevatorFinish] Next scene name is empty!");
            }
        }
    }
    
    // GÜVENLIK: Oyuncu bağlantısı kesilirse
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        int playerID = otherPlayer.ActorNumber;
        
        if (playersInElevator.Contains(playerID))
        {
            playersInElevator.Remove(playerID);
            lockedPlayers.Remove(playerID);
            
            if (debugMode)
            {
                Debug.Log($"[ElevatorFinish] Player {playerID} disconnected, removed from elevator");
            }
        }
    }
    
    // Debug metodları
    [ContextMenu("Test Elevator Sequence")]
    public void TestElevatorSequence()
    {
        if (Application.isPlaying && debugMode)
        {
            Debug.Log("[ElevatorFinish] Manual elevator test started");
            elevatorTriggered = true;
            elevatorSequenceStarted = true;
            StartCoroutine(ElevatorSequence());
        }
    }
    
    [ContextMenu("Force All Players In Elevator")]
    public void ForceAllPlayersInElevator()
    {
        if (Application.isPlaying && debugMode)
        {
            // Test için tüm oyuncuları asansörde say
            playersInElevator.Clear();
            for (int i = 1; i <= PhotonNetwork.CurrentRoom.PlayerCount; i++)
            {
                playersInElevator.Add(i);
            }
            CheckAllPlayersInElevator();
        }
    }
    
    // Inspector'da durum bilgisi göster
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = elevatorSequenceStarted ? Color.red : (elevatorTriggered ? Color.yellow : Color.green);
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
            
            // Locked players info
            if (lockedPlayers.Count > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2, 1f);
            }
        }
    }
}
