using UnityEngine;
using Photon.Pun;
using DG.Tweening;
using System.Collections;

public enum ObstacleMovementType
{
    LinearBack,     // İleri geri doğrusal hareket
    Circular,       // Dairesel hareket
    PathMovement,   // Nokta nokta hareket
    SineWave,       // Sinüs dalgası hareketi
    Oscillate,      // Salınım hareketi
    RandomPosition  // Rastgele pozisyon değiştirme
}

[System.Serializable]
public class ObstacleMovementSettings
{
    [Header("Hareket Tipi")]
    public ObstacleMovementType movementType = ObstacleMovementType.LinearBack;
    
    [Header("Temel Ayarlar")]
    public float movementSpeed = 2f;
    public float movementDistance = 5f;
    public bool useLocalPosition = true;
    
    [Header("Hareket Yönü Ayarları")]
    [Tooltip("X, Y, Z değerleri ile hareket yönünü belirleyin. Örnek: (1,0,0) = sağa, (0,1,0) = yukarı, (0,0,1) = ileri")]
    public Vector3 movementDirection = Vector3.forward;
    public bool normalizeDirection = true;
    
    [Header("Delay Ayarları")]
    public float startDelay = 0f;
    public bool useRandomDelay = false;
    [Range(0f, 10f)] public float randomDelayMin = 0f;
    [Range(0f, 10f)] public float randomDelayMax = 3f;
    
    [Header("Döngü Ayarları")]
    public bool loopMovement = true;
    public LoopType loopType = LoopType.Yoyo;
    
    [Header("Easing")]
    public Ease easeType = Ease.InOutSine;
    
    [Header("Rotasyon")]
    public bool enableRotation = false;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f;
    
    [Header("Özel Yol (Path Movement için)")]
    public Transform[] pathPoints;
    public PathType pathType = PathType.Linear;
    
    [Header("Sinüs Hareketi Ayarları")]
    public float amplitude = 2f;
    public float frequency = 1f;
    public Vector3 sineDirection = Vector3.right;
    
    [Header("Dairesel Hareket Ayarları")]
    public Vector3 circularPlane = Vector3.up; // Y ekseni etrafında döner (XZ düzlemi)
    
    [Header("Oscillate Hareket Ayarları")]
    public Vector3 oscillateDirection = Vector3.up;
    
    /// <summary>
    /// Hareket yönünü döndürür
    /// </summary>
    public Vector3 GetMovementDirection()
    {
        return normalizeDirection ? movementDirection.normalized : movementDirection;
    }
}

public class ObstacleController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Obstacle Settings")]
    public ObstacleMovementSettings movementSettings = new ObstacleMovementSettings();
    
    [Header("Photon Network Settings")]
    public bool syncMovement = true;
    public bool onlyMasterControls = true;
    
    [Header("Güvenlik")]
    public bool pauseOnPlayerCollision = false;
    public float pauseDuration = 1f;
    public LayerMask playerLayerMask = -1;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Color gizmoColor = Color.red;
    
    // Private değişkenler
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Tween currentTween;
    private Tween rotationTween;
    private bool isMoving = false;
    private bool isPaused = false;
    private float actualDelay;
    
    // Network senkronizasyonu için
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool networkIsMoving;
    
    // Sinüs hareketi için
    private float sineTime = 0f;
    private Vector3 sineStartPos;
    
    void Start()
    {
        InitializeObstacle();
    }
    
    void InitializeObstacle()
    {
        // Başlangıç pozisyonunu kaydet
        startPosition = movementSettings.useLocalPosition ? transform.localPosition : transform.position;
        startRotation = transform.rotation;
        sineStartPos = startPosition;
        
        // Network pozisyonunu başlat
        networkPosition = startPosition;
        networkRotation = startRotation;
        
        // Delay hesapla
        CalculateDelay();
        
        // Sadece master client veya tüm clientlar hareket edecek
        if (!onlyMasterControls || PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            StartMovementWithDelay();
        }
    }
    
    void CalculateDelay()
    {
        if (movementSettings.useRandomDelay)
        {
            actualDelay = Random.Range(movementSettings.randomDelayMin, movementSettings.randomDelayMax);
        }
        else
        {
            actualDelay = movementSettings.startDelay;
        }
    }
    
    void StartMovementWithDelay()
    {
        if (actualDelay > 0f)
        {
            StartCoroutine(DelayedStart());
        }
        else
        {
            StartMovement();
        }
    }
    
    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(actualDelay);
        StartMovement();
    }
    
    void StartMovement()
    {
        if (isPaused) return;
        
        isMoving = true;
        
        // Rotasyon başlat
        if (movementSettings.enableRotation)
        {
            StartRotation();
        }
        
        // Hareket tipine göre hareketi başlat
        switch (movementSettings.movementType)
        {
            case ObstacleMovementType.LinearBack:
                StartLinearMovement();
                break;
            case ObstacleMovementType.Circular:
                StartCircularMovement();
                break;
            case ObstacleMovementType.PathMovement:
                StartPathMovement();
                break;
            case ObstacleMovementType.SineWave:
                StartSineMovement();
                break;
            case ObstacleMovementType.Oscillate:
                StartOscillateMovement();
                break;
            case ObstacleMovementType.RandomPosition:
                StartRandomMovement();
                break;
        }
    }
    
    void StartLinearMovement()
    {
        Vector3 direction = movementSettings.GetMovementDirection();
        Vector3 targetPos = startPosition + direction * movementSettings.movementDistance;
        
        if (movementSettings.useLocalPosition)
        {
            currentTween = transform.DOLocalMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, movementSettings.loopType);
        }
        else
        {
            currentTween = transform.DOMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, movementSettings.loopType);
        }
    }
    
    void StartCircularMovement()
    {
        Vector3[] path = new Vector3[8];
        float radius = movementSettings.movementDistance;
        
        // Dairesel düzlemi belirle
        Vector3 circularNormal = movementSettings.circularPlane.normalized;
        Vector3 right, forward;
        
        // Düzlem vektörlerini oluştur
        if (Mathf.Abs(Vector3.Dot(circularNormal, Vector3.up)) > 0.9f)
        {
            right = Vector3.Cross(circularNormal, Vector3.forward).normalized;
        }
        else
        {
            right = Vector3.Cross(circularNormal, Vector3.up).normalized;
        }
        forward = Vector3.Cross(right, circularNormal).normalized;
        
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
            path[i] = startPosition + offset;
        }
        
        if (movementSettings.useLocalPosition)
        {
            currentTween = transform.DOLocalPath(path, movementSettings.movementSpeed, PathType.CatmullRom)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, LoopType.Restart);
        }
        else
        {
            currentTween = transform.DOPath(path, movementSettings.movementSpeed, PathType.CatmullRom)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, LoopType.Restart);
        }
    }
    
    void StartPathMovement()
    {
        if (movementSettings.pathPoints == null || movementSettings.pathPoints.Length < 2)
        {
            Debug.LogWarning("Path hareketi için en az 2 nokta gerekli!");
            return;
        }
        
        Vector3[] path = new Vector3[movementSettings.pathPoints.Length];
        for (int i = 0; i < movementSettings.pathPoints.Length; i++)
        {
            if (movementSettings.pathPoints[i] != null)
            {
                path[i] = movementSettings.useLocalPosition ? 
                    movementSettings.pathPoints[i].localPosition : 
                    movementSettings.pathPoints[i].position;
            }
        }
        
        if (movementSettings.useLocalPosition)
        {
            currentTween = transform.DOLocalPath(path, movementSettings.movementSpeed, movementSettings.pathType)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, movementSettings.loopType);
        }
        else
        {
            currentTween = transform.DOPath(path, movementSettings.movementSpeed, movementSettings.pathType)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, movementSettings.loopType);
        }
    }
    
    void StartSineMovement()
    {
        // Sinüs hareketi Update'te hesaplanacak
        sineTime = 0f;
    }
    
    void StartOscillateMovement()
    {
        Vector3 direction = movementSettings.oscillateDirection.normalized;
        Vector3 targetPos = startPosition + direction * movementSettings.movementDistance;
        
        if (movementSettings.useLocalPosition)
        {
            currentTween = transform.DOLocalMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, LoopType.Yoyo);
        }
        else
        {
            currentTween = transform.DOMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .SetLoops(movementSettings.loopMovement ? -1 : 1, LoopType.Yoyo);
        }
    }
    
    void StartRandomMovement()
    {
        MoveToRandomPosition();
    }
    
    void MoveToRandomPosition()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-movementSettings.movementDistance, movementSettings.movementDistance),
            Random.Range(-movementSettings.movementDistance * 0.5f, movementSettings.movementDistance * 0.5f),
            Random.Range(-movementSettings.movementDistance, movementSettings.movementDistance)
        );
        
        Vector3 targetPos = startPosition + randomOffset;
        
        if (movementSettings.useLocalPosition)
        {
            currentTween = transform.DOLocalMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .OnComplete(() => {
                    if (movementSettings.loopMovement && isMoving)
                    {
                        StartCoroutine(DelayedRandomMove());
                    }
                });
        }
        else
        {
            currentTween = transform.DOMove(targetPos, movementSettings.movementSpeed)
                .SetEase(movementSettings.easeType)
                .OnComplete(() => {
                    if (movementSettings.loopMovement && isMoving)
                    {
                        StartCoroutine(DelayedRandomMove());
                    }
                });
        }
    }
    
    IEnumerator DelayedRandomMove()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 2f));
        if (isMoving) MoveToRandomPosition();
    }
    
    void StartRotation()
    {
        rotationTween = transform.DORotate(
            movementSettings.rotationAxis * movementSettings.rotationSpeed, 
            1f, 
            RotateMode.LocalAxisAdd
        ).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
    }
    
    void FixedUpdate()
    {
        // Sadece aktif olan obstacle için
        if (!isMoving) return;
        
        // Sinüs hareketi FixedUpdate'te hesaplanır
        if (movementSettings.movementType == ObstacleMovementType.SineWave && 
            (!onlyMasterControls || PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected))
        {
            UpdateSineMovement();
        }
        
        // Network senkronizasyonu - diğer clientlar için
        if (syncMovement && PhotonNetwork.IsConnected && 
            (!photonView.IsMine || !onlyMasterControls))
        {
            SyncNetworkMovement();
        }
    }
    
    void UpdateSineMovement()
    {
        sineTime += Time.fixedDeltaTime * movementSettings.frequency;
        
        Vector3 sineDirection = movementSettings.sineDirection.normalized;
        Vector3 sineOffset = sineDirection * Mathf.Sin(sineTime) * movementSettings.amplitude;
        Vector3 newPos = sineStartPos + sineOffset;
        
        if (movementSettings.useLocalPosition)
        {
            transform.localPosition = newPos;
        }
        else
        {
            transform.position = newPos;
        }
    }
    
    void SyncNetworkMovement()
    {
        // Network pozisyonuna doğru yumuşak geçiş
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.fixedDeltaTime * 10f);
    }
    
    #region Collision Detection
    void OnTriggerEnter(Collider other)
    {
        if (pauseOnPlayerCollision && IsPlayerLayer(other.gameObject.layer))
        {
            PauseMovement();
        }
    }
    
    bool IsPlayerLayer(int layer)
    {
        return (playerLayerMask.value & (1 << layer)) > 0;
    }
    
    public void PauseMovement()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        if (currentTween != null) currentTween.Pause();
        if (rotationTween != null) rotationTween.Pause();
        
        StartCoroutine(ResumeAfterDelay());
    }
    
    IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(pauseDuration);
        ResumeMovement();
    }
    
    public void ResumeMovement()
    {
        isPaused = false;
        
        if (currentTween != null) currentTween.Play();
        if (rotationTween != null) rotationTween.Play();
    }
    #endregion
    
    #region Public Control Methods
    public void StopMovement()
    {
        isMoving = false;
        
        if (currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }
        
        if (rotationTween != null)
        {
            rotationTween.Kill();
            rotationTween = null;
        }
    }
    
    public void RestartMovement()
    {
        StopMovement();
        CalculateDelay();
        StartMovementWithDelay();
    }
    
    public void ResetToStartPosition()
    {
        StopMovement();
        
        if (movementSettings.useLocalPosition)
        {
            transform.localPosition = startPosition;
        }
        else
        {
            transform.position = startPosition;
        }
        
        transform.rotation = startRotation;
    }
    
    /// <summary>
    /// Çalışma zamanında hareket yönünü değiştirir
    /// </summary>
    public void ChangeMovementDirection(Vector3 newDirection)
    {
        movementSettings.movementDirection = newDirection;
        if (isMoving && movementSettings.movementType == ObstacleMovementType.LinearBack)
        {
            RestartMovement();
        }
    }
    #endregion
    
    #region Photon Network Methods
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!syncMovement) return;
        
        if (stream.IsWriting)
        {
            // Pozisyon ve durumu gönder
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(isMoving);
        }
        else
        {
            // Pozisyon ve durumu al
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkIsMoving = (bool)stream.ReceiveNext();
            
            // Eğer master client değilse, network durumunu kullan
            if (onlyMasterControls && !PhotonNetwork.IsMasterClient)
            {
                isMoving = networkIsMoving;
            }
        }
    }
    
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        // Master client değiştiğinde hareket kontrolünü yeni master'a geçir
        if (onlyMasterControls && PhotonNetwork.IsMasterClient)
        {
            RestartMovement();
        }
    }
    #endregion
    
    #region Debug and Gizmos
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = gizmoColor;
        Vector3 currentPos = movementSettings.useLocalPosition ? transform.localPosition : transform.position;
        
        switch (movementSettings.movementType)
        {
            case ObstacleMovementType.LinearBack:
                Vector3 direction = Application.isPlaying ? 
                    movementSettings.GetMovementDirection() : 
                    (movementSettings.normalizeDirection ? movementSettings.movementDirection.normalized : movementSettings.movementDirection);
                Gizmos.DrawLine(currentPos, currentPos + direction * movementSettings.movementDistance);
                
                // Yön okunu göster
                Gizmos.color = Color.cyan;
                Vector3 endPoint = currentPos + direction * movementSettings.movementDistance;
                Vector3 arrowHead1 = endPoint - direction * 0.5f + Vector3.Cross(direction, Vector3.up).normalized * 0.2f;
                Vector3 arrowHead2 = endPoint - direction * 0.5f - Vector3.Cross(direction, Vector3.up).normalized * 0.2f;
                Gizmos.DrawLine(endPoint, arrowHead1);
                Gizmos.DrawLine(endPoint, arrowHead2);
                Gizmos.color = gizmoColor;
                break;
                
            case ObstacleMovementType.Circular:
                Gizmos.DrawWireSphere(currentPos, movementSettings.movementDistance);
                // Dairesel düzlem gösterimi
                Vector3 circularNormal = movementSettings.circularPlane.normalized;
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(currentPos, circularNormal * 2f);
                Gizmos.color = gizmoColor;
                break;
                
            case ObstacleMovementType.PathMovement:
                if (movementSettings.pathPoints != null && movementSettings.pathPoints.Length > 1)
                {
                    for (int i = 0; i < movementSettings.pathPoints.Length - 1; i++)
                    {
                        if (movementSettings.pathPoints[i] != null && movementSettings.pathPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(
                                movementSettings.pathPoints[i].position,
                                movementSettings.pathPoints[i + 1].position
                            );
                        }
                    }
                    
                    // Path noktalarını göster
                    Gizmos.color = Color.green;
                    for (int i = 0; i < movementSettings.pathPoints.Length; i++)
                    {
                        if (movementSettings.pathPoints[i] != null)
                        {
                            Gizmos.DrawWireSphere(movementSettings.pathPoints[i].position, 0.3f);
                        }
                    }
                    Gizmos.color = gizmoColor;
                }
                break;
                
            case ObstacleMovementType.SineWave:
                Vector3 startPos = currentPos;
                Vector3 sineDir = movementSettings.sineDirection.normalized;
                
                // Sinüs dalgası çizimi
                for (int i = 0; i < 50; i++)
                {
                    float t = i / 49f * 4f; // 4 tam dalga
                    float sineValue = Mathf.Sin(t * movementSettings.frequency * Mathf.PI) * movementSettings.amplitude;
                    Vector3 point = startPos + sineDir * sineValue + Vector3.forward * t;
                    
                    if (i > 0)
                    {
                        float prevT = (i - 1) / 49f * 4f;
                        float prevSineValue = Mathf.Sin(prevT * movementSettings.frequency * Mathf.PI) * movementSettings.amplitude;
                        Vector3 prevPoint = startPos + sineDir * prevSineValue + Vector3.forward * prevT;
                        Gizmos.DrawLine(prevPoint, point);
                    }
                }
                
                // Sinüs yönü oku
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(currentPos, sineDir * movementSettings.amplitude);
                Gizmos.color = gizmoColor;
                break;
                
            case ObstacleMovementType.Oscillate:
                Vector3 oscillateDir = movementSettings.oscillateDirection.normalized;
                Gizmos.DrawLine(currentPos, currentPos + oscillateDir * movementSettings.movementDistance);
                
                // Oscillate yönü oku
                Gizmos.color = Color.blue;
                Vector3 oscillateEnd = currentPos + oscillateDir * movementSettings.movementDistance;
                Gizmos.DrawLine(oscillateEnd, oscillateEnd - oscillateDir * 0.3f + Vector3.right * 0.1f);
                Gizmos.DrawLine(oscillateEnd, oscillateEnd - oscillateDir * 0.3f - Vector3.right * 0.1f);
                Gizmos.color = gizmoColor;
                break;
                
            case ObstacleMovementType.RandomPosition:
                Gizmos.DrawWireCube(currentPos, Vector3.one * movementSettings.movementDistance);
                break;
        }
        
        // Başlangıç pozisyonunu göster
        Gizmos.color = Color.green;
        Vector3 startPosGizmo = Application.isPlaying ? startPosition : currentPos;
        Gizmos.DrawWireSphere(startPosGizmo, 0.2f);
    }
    #endregion
}
