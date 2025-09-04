using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Lever2Script : MonoBehaviourPunCallbacks
{
    [Header("Smoke Settings")]
    public GameObject smokePrefab;
    public Transform smokeCenterObject; // Smoke'ların etrafında oluşacağı merkez obje
    public int smokeCount = 8; // Kaç tane smoke oluşacak
    public float smokeRadius = 3f; // Merkez objeden ne kadar uzaklıkta oluşacak
    public float smokeDuration = 2f;

    private List<GameObject> activeSmokes = new List<GameObject>(); // Aktif smoke'ları takip etmek için

    [Header("Mazgal Settings")]
    public GameObject MazgalObject1;
    public GameObject MazgalObject2;
    public float mazgalMoveDistance = 2f; // Mazgalların ne kadar hareket edeceği

    private Vector3 mazgal1OriginalPosition;
    private Vector3 mazgal2OriginalPosition;



    bool playerInside = false;
    public KeyCode leverKey = KeyCode.E;

    public GameObject lever;

    public Vector3 targetAngle = new Vector3(0, 0, 0);
    public float duration = 2f;

    private bool allPlayersLever = false;

    public GameObject desks;

    public Vector3 desksTargetPosition = new Vector3(0, 0, 0);

    public bool leverWorked = false;

    public GameObject runes;
    public GameObject runesTemporary;

    [Header ("Camera Shake Sets")]
    public float shakeDuration;
    public float shakeMagnitude;

    [Header ("sound")]
    private AudioSource audioSource;

    public AudioClip leverSound;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D ses için
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(leverKey) && playerInside)
        {
            sendDeskOpenInfo();
            if (audioSource != null && leverSound != null)
            {
                audioSource.PlayOneShot(leverSound);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if ((other.GetComponent<clientPlayerU>() != null) || (other.GetComponent<hostPlayerU>() != null))
        {
            playerInside = true;
        }
    }

    void OTriggerExit(Collider other)
    {
        if ((other.GetComponent<clientPlayerU>() != null) || (other.GetComponent<hostPlayerU>() != null))
        {
            playerInside = false;
        }
    }

    void sendDeskOpenInfo()
    {
        //Debug.Log("start");
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { "leverWorked", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        // Debug.Log("finish");
        StartCoroutine(rotateLever());

    }

    IEnumerator rotateLever()
    {
        Quaternion startRotation = lever.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(targetAngle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            lever.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lever.transform.rotation = endRotation;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("leverWorked") && !leverWorked)
        {
            CheckIfBothPlayersLever();
        }
    }

    public void CheckIfBothPlayersLever()
    {
        allPlayersLever = true;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue("leverWorked", out object value) && !(value is bool levered && levered))
            {
                allPlayersLever = false;
            }
        }

        if (allPlayersLever)
        {
            Debug.Log("LeveredCompeletely");
            StartCoroutine(MazgalSequence());
            leverWorked = true;
        }
    }
    public bool test1 = false;
    void Start()
    {
        // Mazgalların orijinal pozisyonlarını kaydet
        if (MazgalObject1 != null)
            mazgal1OriginalPosition = MazgalObject1.transform.position;
        if (MazgalObject2 != null)
            mazgal2OriginalPosition = MazgalObject2.transform.position;

        if (test1)
        {
            StartCoroutine(MazgalSequence());
        }
    }

    IEnumerator MazgalPositionChange(GameObject mazgalObject, Vector3 targetPosition)
    {
        Vector3 startPos = mazgalObject.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            mazgalObject.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mazgalObject.transform.position = targetPosition; // Hedefe tam sabitle
    }

    IEnumerator MazgalSequence()
    {
        // 1. Adım: Merkez objenin etrafında daire şeklinde smoke efektleri oluştur ve mazgalları hareket ettir
        activeSmokes.Clear(); // Önceki smoke'ları temizle
        
        if (smokePrefab != null && smokeCenterObject != null && smokeCount > 0)
        {
            for (int i = 0; i < smokeCount; i++)
            {
                // Daire üzerinde eşit aralıklarla pozisyonlar hesapla
                float angle = (360f / smokeCount) * i * Mathf.Deg2Rad; // Radyan cinsinden açı
                
                // X ve Z koordinatlarını hesapla (Y sabit kalıyor)
                float x = smokeCenterObject.position.x + Mathf.Cos(angle) * smokeRadius;
                float z = smokeCenterObject.position.z + Mathf.Sin(angle) * smokeRadius;
                
                Vector3 smokePosition = new Vector3(x, smokeCenterObject.position.y, z);
                
                // Smoke'u oluştur ve listeye ekle
                GameObject smoke = Instantiate(smokePrefab, smokePosition, Quaternion.Euler(-90, 0, 0));
                activeSmokes.Add(smoke);
            }
        }
        
        // İki mazgalı otomatik olarak hareket ettir
        Vector3 mazgal1Target = mazgal1OriginalPosition + Vector3.left * mazgalMoveDistance; // Sola
        Vector3 mazgal2Target = mazgal2OriginalPosition + Vector3.right * mazgalMoveDistance; // Sağa
        
        Coroutine mazgal1Movement = StartCoroutine(MazgalPositionChange(MazgalObject1, mazgal1Target));
        Coroutine mazgal2Movement = StartCoroutine(MazgalPositionChange(MazgalObject2, mazgal2Target));
        
        // Her iki mazgal hareketi tamamlanana kadar bekle
        yield return mazgal1Movement;
        yield return mazgal2Movement;
        
        // 2. Adım: Desk'i yukarı çıkar (smoke'lar hala devam ediyor)
        yield return StartCoroutine(deskTransformChange());
        
        // 3. Adım: Desk yukarı çıktıktan sonra tüm smoke'ları yavaş yavaş söndür
        foreach (GameObject smoke in activeSmokes)
        {
            if (smoke != null)
            {
                // Particle System'in emission'ını kapat
                ParticleSystem particleSystem = smoke.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var emission = particleSystem.emission;
                    emission.enabled = false;
                    
                    // Mevcut parçacıklar sönene kadar bekle, sonra objeyi yok et
                    StartCoroutine(DestroyAfterParticles(smoke, particleSystem));
                }
                else
                {
                    // Particle System yoksa direkt yok et
                    Destroy(smoke);
                }
            }
        }
        activeSmokes.Clear();
        
        // 4. Adım: Mazgalları tekrar kapatma (orijinal pozisyonlarına döndür)
        Coroutine mazgal1Close = StartCoroutine(MazgalPositionChange(MazgalObject1, mazgal1OriginalPosition));
        Coroutine mazgal2Close = StartCoroutine(MazgalPositionChange(MazgalObject2, mazgal2OriginalPosition));
        
        yield return mazgal1Close;
        yield return mazgal2Close;
    }

    IEnumerator deskTransformChange()
    {
        Vector3 startPos = desks.transform.position;
        float elapsed = 0f;

        cameraShake.instance.photonView.RPC("RpcTriggerShake", RpcTarget.All, shakeDuration, shakeMagnitude);

        while (elapsed < duration)
        {
            desks.transform.position = Vector3.Lerp(startPos, desksTargetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        desks.transform.position = desksTargetPosition; // Hedefe tam sabitle

        runes.SetActive(true);
        runesTemporary.SetActive(false);

    }
// //     IEnumerator deskTransformChange()
// // {
// //     Rigidbody rb = desks.GetComponent<Rigidbody>();
// //     if (rb == null)
// //     {
// //         Debug.LogError("Desks objesinde Rigidbody yok!");
// //         yield break;
// //     }

// //     Vector3 startPos = rb.position;
// //     float elapsed = 0f;

// //     while (elapsed < duration)
// //     {
// //         float t = elapsed / duration;
// //         Vector3 newPos = Vector3.Lerp(startPos, desksTargetPosition, t);
// //         rb.MovePosition(newPos);
// //         elapsed += Time.fixedDeltaTime;
// //         yield return new WaitForFixedUpdate();
// //     }

//     rb.MovePosition(desksTargetPosition); // Tam hedefe git
// }

    IEnumerator DestroyAfterParticles(GameObject smokeObject, ParticleSystem particleSystem)
    {
        // Particle System'in hala parçacık üretip üretmediğini ve mevcut parçacıkları kontrol et
        while (particleSystem != null && (particleSystem.IsAlive() || particleSystem.particleCount > 0))
        {
            yield return new WaitForSeconds(0.1f); // Kısa aralıklarla kontrol et
        }
        
        // Tüm parçacıklar sönünce objeyi yok et
        if (smokeObject != null)
        {
            Destroy(smokeObject);
        }
    }
}
