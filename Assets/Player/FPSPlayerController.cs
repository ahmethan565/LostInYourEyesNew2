using UnityEngine;
using Photon.Pun;

public class FPSPlayerController : MonoBehaviourPun, IPunObservable
{

    public string currentState;
    public bool isMovementFrozen = false;

    [Header("Speed Settings")]
    public float baseWalkSpeed = 5f;
    public float baseSprintSpeed = 8f;

    public float currentSpeed { get; private set; }

    [Header("Hareket Ayarları")]
    public bool isSprinting = false;
    public float jumpForce = 8f;
    // public float gravityValue = -20f; // Artık bir çarpanla kullanılacak
    [Tooltip("Yüksekliği bu değerden daha az olan eğimlerde karakter yukarı doğru hareket edebilir.")]
    public float maxSlopeAngle = 45f; // Karakterin çıkabileceği maksimum eğim açısı
    [Tooltip("Havada hareket ederken inputun ne kadar etkili olacağını belirler. 0 = hiç kontrol yok, 1 = tam kontrol.")]
    [Range(0f, 1f)]
    public float airControlFactor = 0.5f; // Havada hareket kontrolü çarpanı
    [Tooltip("Karakterin düşebileceği maksimum hız. Mutlak bir değerdir, pozitif girilir.")]
    public float maxFallSpeed = 30f; // Limit hız (Terminal Velocity) için eklendi

    [Header("Yer Çekimi Ayarları")]
    [Tooltip("Yer çekimi kuvvetinin temel değeri (genellikle negatif).")]
    public float baseGravity = -20f; // Yer çekiminin temel değeri
    [Tooltip("Yer çekimi kuvvetine uygulanacak çarpan. 1.0 varsayılan, daha büyük değerler daha hızlı ivmelenme sağlar.")]
    public float gravityMultiplier = 1.0f; // Yer çekimi ivmesi çarpanı için eklendi

    [Header("Çömelme Ayarları")]
    public float crouchHeight = 1.0f; // Çömelme yüksekliği
    public float crouchSpeedMultiplier = 0.5f; // Çömelirken hız çarpanı
    public float crouchCameraOffset = -0.5f; // Kameranın ne kadar aşağı ineceği

    [Header("Sprint Ayarları")]
    public float sprintDuration = 0f;
    public float slideTriggerTime = 1.0f; // Örn: 1 saniye sonra slide açılabilir

    [Header("Kamera Ayarları")]
    public float mouseSensitivity = 100f;
    public Transform cameraRoot;
    public bool clampVerticalRotation = true;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Animasyon Ayarları")]
    public Animator animator; // Karakter animatörü
    
    [Header("Ağ Ayarları")]
    public float remoteSmoothFactor = 15f;

    public CharacterController controller;
    public Vector3 playerVelocity; // Hem input kaynaklı hem yer çekimi kaynaklı dikey hızı içerir
    private float xRotation = 0f;

    // FSM ile ilgili eklemeler
    private PlayerFSM playerFSM;
    public bool isGrounded; // Yerel oyuncu için yere değme durumu
    public bool isSlidingSlope { get; private set; } // Dik eğimde kayma bayrağı, FSM'ye açık

    // Ağ üzerinden gelen veriler
    private float network_xRotation;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private string networkAnimationState = "idle"; // Network üzerinden gelen animasyon durumu
    private JumpPhase networkJumpPhase = JumpPhase.None; // Network üzerinden gelen jump phase

    // Slope Projection için
    private Vector3 _hitNormal; // Son çarpma normali

    // Animasyon parametreleri (hash değerleri performans için)
    private int animIdleHash;
    private int animWalkingHash;
    private int animRunningHash;
    private int animJumpingHash;
    private int animJumpTakeoffHash;
    private int animJumpInAirHash;
    private int animJumpLandingHash;
    private int animCrouchingHash;
    private int animSlidingHash;
    private int animVelocityHash;
    private int animGroundedHash;
    
    // Jumping aşamaları için enum
    public enum JumpPhase
    {
        None,
        Takeoff,
        InAir,
        Landing
    }
    
    public JumpPhase currentJumpPhase = JumpPhase.None;
    public float jumpPhaseTimer = 0f;
    
    [Header("Jump Phase Settings")]
    [Tooltip("Takeoff aşamasının süresi (saniye)")]
    public float takeoffDuration = 0.2f;
    [Tooltip("Landing aşamasının minimum süresi (saniye)")]
    public float landingDuration = 0.3f;
    [Tooltip("Landing için minimum yere temas süresi")]
    public float minGroundedTimeForLanding = 0.1f;
    [Tooltip("Yere değdikten sonra tekrar zıplamak için beklenmesi gereken süre")]
    public float jumpCooldownTime = 0.2f;
    private float groundedTimer = 0f;
    private float lastLandingTime = 0f; // Son yere değme zamanı

    [Header("Organ Played Controller")]
    public bool organPlayed = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // controller.stepOffset = 0.3f; // İhtiyacınıza göre bu değeri ayarlayın

        // Animator'ü al ve hash değerlerini initialize et
        if (animator == null)
            animator = GetComponent<Animator>();

        InitializeAnimationHashes();

        if (!photonView.IsMine)
        {
            if (controller != null)
                controller.enabled = false;
            SetupRemotePlayerCamera();
        }
    }

    void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 20;

        if (photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetupLocalPlayerCamera();

            // FSM'yi başlat
            playerFSM = new PlayerFSM();
            playerFSM.AddState(new PlayerIdleState(this, playerFSM));
            playerFSM.AddState(new PlayerWalkingState(this, playerFSM));
            playerFSM.AddState(new PlayerJumpingState(this, playerFSM));
            playerFSM.AddState(new PlayerRunningState(this, playerFSM));
            playerFSM.AddState(new PlayerCrouchingState(this, playerFSM));
            playerFSM.AddState(new PlayerSlidingState(this, playerFSM));

            playerFSM.ChangeState(typeof(PlayerIdleState)); // Başlangıç durumu
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            bool wasGrounded = isGrounded; // Önceki frame'deki durum
            isGrounded = controller.isGrounded;
            
            // Yere ilk kez değdiğimizde landing zamanını kaydet
            if (isGrounded && !wasGrounded)
            {
                lastLandingTime = Time.time;
            }
            
            // Grounded timer'ı güncelle
            if (isGrounded)
                groundedTimer += Time.deltaTime;
            else
                groundedTimer = 0f;
                
            currentState = playerFSM.GetCurrentState().ToString();

            bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

            if (!menuIsOpen)
            {
                playerFSM.Update();
                HandleLocalPlayerMouseLook();
            }
            else
            {
                ApplyGravity();
                MoveCharacter();
            }

            UpdateMoveSpeed();
            UpdateAnimationParameters(); // Animasyon parametrelerini güncelle
        }
        else
        {
            SmoothRemotePlayerData();
        }
    }

    private void UpdateMoveSpeed()
    {
        float targetSpeed = baseWalkSpeed;

        if (isSprinting)
        {
            targetSpeed = baseSprintSpeed;
        }

        if (currentState.Contains("Crouching"))
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        currentSpeed = targetSpeed;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        _hitNormal = hit.normal;
        if (isGrounded)
        {
            float angle = Vector3.Angle(Vector3.up, _hitNormal);
            isSlidingSlope = angle > maxSlopeAngle;
        }
        else
        {
            isSlidingSlope = false;
        }
    }

    // --- Animasyon Metotları ---
    private void InitializeAnimationHashes()
    {
        if (animator != null)
        {
            animIdleHash = Animator.StringToHash("IsIdle");
            animWalkingHash = Animator.StringToHash("IsWalking");
            animRunningHash = Animator.StringToHash("IsRunning");
            animJumpingHash = Animator.StringToHash("IsJumping");
            animJumpTakeoffHash = Animator.StringToHash("IsJumpTakeoff");
            animJumpInAirHash = Animator.StringToHash("IsJumpInAir");
            animJumpLandingHash = Animator.StringToHash("IsJumpLanding");
            animCrouchingHash = Animator.StringToHash("IsCrouching");
            animSlidingHash = Animator.StringToHash("IsSliding");
            animVelocityHash = Animator.StringToHash("Velocity");
            animGroundedHash = Animator.StringToHash("IsGrounded");
        }
    }

    public void SetAnimationState(string stateName)
    {
        if (animator == null) return;

        // Tüm state boolean'larını false yap
        animator.SetBool(animIdleHash, false);
        animator.SetBool(animWalkingHash, false);
        animator.SetBool(animRunningHash, false);
        animator.SetBool(animJumpingHash, false);
        animator.SetBool(animJumpTakeoffHash, false);
        animator.SetBool(animJumpInAirHash, false);
        animator.SetBool(animJumpLandingHash, false);
        animator.SetBool(animCrouchingHash, false);
        animator.SetBool(animSlidingHash, false);

        // İstenen state'i true yap
        switch (stateName.ToLower())
        {
            case "idle":
                animator.SetBool(animIdleHash, true);
                break;
            case "walking":
                animator.SetBool(animWalkingHash, true);
                break;
            case "running":
                animator.SetBool(animRunningHash, true);
                break;
            case "jumping":
                animator.SetBool(animJumpingHash, true);
                break;
            case "jumptakeoff":
                animator.SetBool(animJumpTakeoffHash, true);
                break;
            case "jumpinair":
                animator.SetBool(animJumpInAirHash, true);
                break;
            case "jumplanding":
                animator.SetBool(animJumpLandingHash, true);
                break;
            case "crouching":
                animator.SetBool(animCrouchingHash, true);
                break;
            case "sliding":
                animator.SetBool(animSlidingHash, true);
                break;
        }
    }

    public void UpdateAnimationParameters()
    {
        if (animator == null) return;

        // Hareket hızını animator'e gönder
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;
        animator.SetFloat(animVelocityHash, speed);

        // Zemin durumunu güncelle
        animator.SetBool(animGroundedHash, isGrounded);
    }

    private string GetCurrentAnimationState()
    {
        // CurrentState string'ini basitleştirip sadece state ismini döndür
        if (currentState.Contains("Idle")) return "idle";
        if (currentState.Contains("Walking")) return "walking";
        if (currentState.Contains("Running")) return "running";
        if (currentState.Contains("Jumping")) 
        {
            // Jump phase'e göre döndür
            switch (currentJumpPhase)
            {
                case JumpPhase.Takeoff:
                    return "jumptakeoff";
                case JumpPhase.InAir:
                    return "jumpinair";
                case JumpPhase.Landing:
                    return "jumplanding";
                default:
                    return "jumping";
            }
        }
        if (currentState.Contains("Crouching")) return "crouching";
        if (currentState.Contains("Sliding")) return "sliding";
        return "idle"; // Varsayılan
    }

    // --- FSM Tarafından Erişilecek Yardımcı Metotlar ---

    public Vector3 GetInputMoveVector()
    {
        bool menuIsOpen = PauseMenuManager.Instance != null && PauseMenuManager.Instance.isMenuOpen;

        if (menuIsOpen || isMovementFrozen)
        {
            return Vector3.zero; // Menü veya puzzle ekranı açıksa input yok
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v);
        if (move.magnitude > 1f) move.Normalize();

        if (!isGrounded)
            return move * airControlFactor;

        return move;
    }

    public void HandleJumpInput()
    {
        // Yerde olmalı VE son landing'den beri yeterli süre geçmiş olmalı
        if (isGrounded && (Time.time - lastLandingTime) >= jumpCooldownTime)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * (baseGravity * gravityMultiplier)); // Yer çekimi çarpanı kullanıldı
            StartJumpPhase(JumpPhase.Takeoff);
        }
    }
    
    // Jump phase yönetimi için yeni metotlar
    public void StartJumpPhase(JumpPhase phase)
    {
        currentJumpPhase = phase;
        jumpPhaseTimer = 0f;
        
        switch (phase)
        {
            case JumpPhase.Takeoff:
                SetAnimationState("jumptakeoff");
                break;
            case JumpPhase.InAir:
                SetAnimationState("jumpinair");
                break;
            case JumpPhase.Landing:
                SetAnimationState("jumplanding");
                break;
        }
    }
    
    public void UpdateJumpPhase()
    {
        if (currentJumpPhase == JumpPhase.None) return;
        
        jumpPhaseTimer += Time.deltaTime;
        
        switch (currentJumpPhase)
        {
            case JumpPhase.Takeoff:
                // Takeoff süresi doldu veya havaya kalktık
                if (jumpPhaseTimer >= takeoffDuration || !isGrounded)
                {
                    StartJumpPhase(JumpPhase.InAir);
                }
                break;
                
            case JumpPhase.InAir:
                // Yere değdik ve minimum süre geçti
                if (isGrounded && groundedTimer >= minGroundedTimeForLanding)
                {
                    StartJumpPhase(JumpPhase.Landing);
                }
                break;
                
            case JumpPhase.Landing:
                // Landing süresi doldu
                if (jumpPhaseTimer >= landingDuration)
                {
                    EndJumpPhase();
                }
                break;
        }
    }
    
    public void EndJumpPhase()
    {
        currentJumpPhase = JumpPhase.None;
        jumpPhaseTimer = 0f;
    }
    
    public bool IsInJumpPhase()
    {
        return currentJumpPhase != JumpPhase.None;
    }

    public void ApplyGravity()
    {
        // Yere yapışma mantığı
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -0.5f;
        }

        // Yer çekimi ivmesini uygula
        playerVelocity.y += (baseGravity * gravityMultiplier) * Time.deltaTime; // Yer çekimi çarpanı kullanıldı

        // Limit hızı uygula: Karakterin y-hızı, belirlenen maksimum düşüş hızını geçmemeli
        // maxFallSpeed pozitif bir değer olduğu için, playerVelocity.y negatif olduğundan
        // Math.Max fonksiyonu daha az negatif (yani daha büyük) olan değeri seçecektir.
        playerVelocity.y = Mathf.Max(playerVelocity.y, -maxFallSpeed); 
    }

    public void ApplyHorizontalMovement(Vector3 horizontalMove)
    {
        // Bu metot artık doğrudan MoveCharacter tarafından ele alınıyor,
        // ancak farklı FSM durumları özel yatay hareket uygulamak isterse kullanılabilir.
    }

    public void MoveCharacter()
    {
        Vector3 horizontalMovement = GetInputMoveVector() * currentSpeed;
        Vector3 totalMove = horizontalMovement;

        if (isGrounded && !isSlidingSlope)
        {
            totalMove = ProjectMoveOnSlope(horizontalMovement, _hitNormal);
        }

        totalMove += new Vector3(0, playerVelocity.y, 0);

        if (controller != null && controller.enabled)
        {
            controller.Move(totalMove * Time.deltaTime);
        }
    }

    private Vector3 ProjectMoveOnSlope(Vector3 moveVector, Vector3 slopeNormal)
    {
        return Vector3.ProjectOnPlane(moveVector, slopeNormal);
    }

    // --- Mevcut Diğer Metotlar (Aynı Kalır) ---
    void SetupLocalPlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al == null) al = cam.gameObject.AddComponent<AudioListener>();
                al.enabled = true;
            }
            else Debug.LogWarning("cameraRoot altında kamera bulunamadı! Yerel kamera ayarlanamadı.");
        }
        else Debug.LogWarning("cameraRoot atanmamış! Yerel kamera ayarlanamadı.");
    }

    void SetupRemotePlayerCamera()
    {
        if (cameraRoot != null)
        {
            Camera cam = cameraRoot.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.gameObject.SetActive(false);
            AudioListener al = cameraRoot.GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;
        }
        else Debug.LogWarning("cameraRoot atanmamış! Remote kamera ayarlanamadı.");
    }

    void HandleLocalPlayerMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;

        if (clampVerticalRotation)
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        else Debug.LogWarning("cameraRoot atanmamış! Dikey bakış yapılamadı.");
    }

    void SmoothRemotePlayerData()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * remoteSmoothFactor);
        transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * remoteSmoothFactor);

        if (cameraRoot != null)
        {
            Quaternion currentRot = cameraRoot.localRotation;
            Quaternion targetRot = Quaternion.Euler(network_xRotation, 0f, 0f);
            cameraRoot.localRotation = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * remoteSmoothFactor);
        }

        // Remote oyuncu için jump phase'ini güncelle
        currentJumpPhase = networkJumpPhase;
        
        // Remote oyuncu için animasyon state'ini güncelle
        SetAnimationState(networkAnimationState);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(xRotation);
            stream.SendNext(isSlidingSlope);
            stream.SendNext(playerVelocity.y); // PlayerVelocity.y'yi de senkronize edelim
            stream.SendNext(GetCurrentAnimationState()); // Animasyon state'ini gönder
            stream.SendNext((int)currentJumpPhase); // Jump phase'i int olarak gönder
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            network_xRotation = (float)stream.ReceiveNext();
            isSlidingSlope = (bool)stream.ReceiveNext();
            playerVelocity.y = (float)stream.ReceiveNext(); // Uzak oyuncular için y hızını da al
            networkAnimationState = (string)stream.ReceiveNext(); // Animasyon state'ini al
            networkJumpPhase = (JumpPhase)stream.ReceiveNext(); // Jump phase'i al
        }
    }
}