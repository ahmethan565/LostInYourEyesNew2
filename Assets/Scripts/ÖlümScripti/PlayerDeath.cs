using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 3f;

    [Header("FX (Everyone)")]
    [SerializeField] private GameObject deathVfxPrefab;      // Ölüm VFX (herkeste)
    [SerializeField] private GameObject respawnVfxPrefab;    // Respawn VFX (herkeste)

    [Header("SFX (Everyone, 3D)")]
    [SerializeField] private AudioClip deathSfx;             // Ölüm SFX (herkeste)
    [SerializeField] private AudioClip respawnSfx;           // Respawn SFX (herkeste)
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private float sfxMaxDistance = 30f;

    [Header("Disable While Dead")]
    [SerializeField] private Behaviour[] componentsToDisable; // hareket/kamera scriptlerin

    // ---------- LOCAL UI (owner’da runtime oluşturulacak) Fade bugı buradan düzeldi ----------
    [Header("Local Fade UI")]
    [Tooltip("Owner’da otomatik Overlay Canvas + FadeImage + RespawnText oluşturulsun.")]
    [SerializeField] private bool autoCreateLocalUI = true;

    [Tooltip("Fade hızları")]
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float fadeOutTime = 0.35f;

    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Bu ikisini kod oluşturacak (owner’da):
    [SerializeField] private Image fadeImage;   // FULLSCREEN siyah Image
    [SerializeField] private Text respawnText; // geri sayım yazısı

    // ---------- YÖN AYARLARI (SADECE ÖLÜM VFX'İNE UYGULANIR) aptal vfx yön değiştiriyor yoksa----------
    private enum Axis { PosX, NegX, PosY, NegY, PosZ, NegZ }

    [Header("Death VFX Orientation")]
    [Tooltip("Death VFX prefabı parçacığı hangi LOKAL eksenden fırlatıyor? (çoğu prefab PosZ)")]
    [SerializeField] private Axis deathVfxLocalDirection = Axis.PosZ;

    [Tooltip("True: zeminin normaline hizala. False: her zaman dünya +Y'ye hizala.")]
    [SerializeField] private bool deathAlignToSurfaceNormal = false;

    [Tooltip("Gerekirse küçük açı düzeltmesi (derece).")]
    [SerializeField] private Vector3 deathExtraEuler = Vector3.zero;

    [Header("Respawn VFX Orientation")]
    [Tooltip("Respawn VFX prefab rotasyonunu KORU (önerilen).")]
    [SerializeField] private bool keepRespawnPrefabRotation = true;

    [Tooltip("Prefab rotasyonuna sadece küçük ince ayar gerekiyorsa kullan.")]
    [SerializeField] private Vector3 respawnExtraEuler = Vector3.zero;

    // ---------- INTERNAL ----------
    private PhotonView pv;
    private CharacterController cc;
    private Renderer[] rends;
    private AudioSource aud;
    private bool isDead;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        cc = GetComponent<CharacterController>();
        rends = GetComponentsInChildren<Renderer>(true);

        aud = GetComponent<AudioSource>();
        if (!aud) aud = gameObject.AddComponent<AudioSource>();
        aud.playOnAwake = false;
        aud.spatialBlend = 0f; // yerel UI sesleri için; global sesler RPC ile 3D çalınır
    }

    void Start()
    {
        if (pv.IsMine)
        {
            // Owner: Local UI yoksa oluştur
            if (autoCreateLocalUI && (fadeImage == null || respawnText == null))
                CreateLocalOverlayUI();

            // Güvenlik: alpha=0, text kapalı
            if (fadeImage)
            {
                var c = fadeImage.color; c.a = 0f; fadeImage.color = c;
                fadeImage.raycastTarget = false;
            }
            if (respawnText) respawnText.gameObject.SetActive(false);
        }
        else
        {
            // Remote kopyada kesinlikle UI oluşturma/gösterme
            // (Bu sınıfta zaten runtime oluşturmadığımız için görünmez)
            // Eğer sahne/prefab’da yanlışlıkla atandıysa kapatalım:
            // Kodu çalanın amına korum
            if (fadeImage && fadeImage.canvas) fadeImage.canvas.enabled = false;
            if (respawnText) respawnText.gameObject.SetActive(false);
        }
    }

    public void Kill(Transform respawnPoint)
    {
        if (!pv.IsMine || isDead) return;
        Vector3 spawnPos = respawnPoint ? respawnPoint.position : transform.position + Vector3.up;
        StartCoroutine(DieAndRespawn(spawnPos));
    }

    private IEnumerator DieAndRespawn(Vector3 spawnPos)
    {
        isDead = true;

        // --- Ölüm anı: VFX & SFX (HERKESTE) ---
        Vector3 deathPos = transform.position;
        pv.RPC(nameof(RPC_SpawnVFX), RpcTarget.All, deathPos, false); // false = death
        pv.RPC(nameof(RPC_PlaySFX3D), RpcTarget.All, deathPos, 0, sfxVolume, sfxMaxDistance);

        // --- Fade in (yalnızca owner) ---
        yield return AlphaTo(1f, fadeInTime);

        // --- Kontrolleri kapat, görünürlüğü herkeste kapat ---
        SetEnable(false);
        SetVisible(false);                                 // owner’da
        pv.RPC(nameof(RPC_SetBodyVisible), RpcTarget.Others, false); // diğerlerinde
        if (cc) cc.enabled = false;

        // --- Geri sayım (owner) ---
        if (respawnText != null)
        {
            respawnText.gameObject.SetActive(true);
            float t = respawnTime;
            while (t > 0f)
            {
                respawnText.text = $"Respawn in {Mathf.CeilToInt(t)}...";
                yield return new WaitForSeconds(1f);
                t -= 1f;
            }
            respawnText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(respawnTime);
        }

        // --- Respawn ---
        if (cc) cc.enabled = false;
        transform.position = spawnPos;
        if (cc) cc.enabled = true;

        SetVisible(true);                                  // owner’da
        pv.RPC(nameof(RPC_SetBodyVisible), RpcTarget.Others, true);
        SetEnable(true);

        // Respawn VFX & SFX (HERKESTE)
        pv.RPC(nameof(RPC_SpawnVFX), RpcTarget.All, spawnPos, true); // true = respawn
        pv.RPC(nameof(RPC_PlaySFX3D), RpcTarget.All, spawnPos, 1, sfxVolume, sfxMaxDistance);

        // --- Fade out (owner) ---
        yield return AlphaTo(0f, fadeOutTime);

        isDead = false;
    }

    private void SetEnable(bool on)
    {
        if (componentsToDisable == null) return;
        foreach (var b in componentsToDisable) if (b) b.enabled = on;
    }

    private void SetVisible(bool on)
    {
        if (rends == null) return;
        foreach (var r in rends) if (r) r.enabled = on;
    }

    private IEnumerator AlphaTo(float target, float duration)
    {
        if (!fadeImage) yield break; // owner değilse/null ise sessizce çık
        float start = fadeImage.color.a;
        if (Mathf.Approximately(start, target) || duration <= 0f)
        {
            var c0 = fadeImage.color; c0.a = target; fadeImage.color = c0;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = fadeCurve.Evaluate(k);
            float a = Mathf.Lerp(start, target, eased);
            var c = fadeImage.color; c.a = a; fadeImage.color = c;
            yield return null;
        }
        var cEnd = fadeImage.color; cEnd.a = target; fadeImage.color = cEnd;
    }

    // ---------- Local Overlay UI oluştur ----------
    private void CreateLocalOverlayUI()
    {
        // Root
        GameObject root = new GameObject("LocalUIRoot (Owner)");
        root.layer = LayerMask.NameToLayer("UI"); // varsa
        root.transform.SetParent(null, false);    // world root’a koy (Overlay’de olduğu için fark etmez)
        // Root'u bu oyuncu yok olunca temizlemek istersen parent'ı player yapabilirsin:
        root.transform.SetParent(this.transform, false);

        // Canvas
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<GraphicRaycaster>();

        // CanvasScaler (opsiyonel)
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Fade Image
        var fadeGO = new GameObject("FadeImage");
        fadeGO.transform.SetParent(root.transform, false);
        var img = fadeGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // başta görünmez
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        fadeImage = img;

        // Respawn Text
        var textGO = new GameObject("RespawnText");
        textGO.transform.SetParent(root.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = "";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = 42;
        txt.color = Color.white;
        txt.raycastTarget = false;
        // Varsayılan font:
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var trt = txt.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(800, 120);

        txt.gameObject.SetActive(false);
        respawnText = txt;
    }

    // ---------- Yön hesapları ----------
    private Vector3 AxisToVector(Axis a)
    {
        switch (a)
        {
            case Axis.PosX: return Vector3.right;
            case Axis.NegX: return Vector3.left;
            case Axis.PosY: return Vector3.up;
            case Axis.NegY: return Vector3.down;
            case Axis.PosZ: return Vector3.forward;
            case Axis.NegZ: return Vector3.back;
        }
        return Vector3.forward;
    }

    private Vector3 GetSurfaceNormal(Vector3 origin)
    {
        if (Physics.Raycast(origin + Vector3.up * 0.2f, Vector3.down, out var hit, 5f))
            return hit.normal;
        return Vector3.up;
    }

    private Quaternion ComputeDeathRotation(Vector3 pos)
    {
        Vector3 localEmitAxis = AxisToVector(deathVfxLocalDirection);
        Vector3 targetDir = deathAlignToSurfaceNormal ? GetSurfaceNormal(pos) : Vector3.up; // dünya +Y
        Quaternion baseRot = Quaternion.FromToRotation(localEmitAxis, targetDir);
        return baseRot * Quaternion.Euler(deathExtraEuler);
    }

    private Quaternion ComputeRespawnRotation(GameObject prefab, Vector3 pos)
    {
        if (keepRespawnPrefabRotation)
            return prefab.transform.rotation * Quaternion.Euler(respawnExtraEuler);
        return Quaternion.Euler(respawnExtraEuler);
    }

    // ---------- RPC'ler ----------
    [PunRPC]
    private void RPC_SetBodyVisible(bool on)
    {
        if (rends == null) return;
        foreach (var r in rends) if (r) r.enabled = on;
    }

    // isRespawn: false = death, true = respawn
    [PunRPC]
    private void RPC_SpawnVFX(Vector3 pos, bool isRespawn)
    {
        GameObject prefab = isRespawn ? respawnVfxPrefab : deathVfxPrefab;
        if (!prefab) return;

        Quaternion rot = isRespawn
            ? ComputeRespawnRotation(prefab, pos)   // Respawn: prefab rotasyonu korunur
            : ComputeDeathRotation(pos);            // Death: lokal -> dünya +Y (veya yüzey normaline)

        var go = Instantiate(prefab, pos, rot);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
        }
        Destroy(go, 5f);
    }

    // which: 0=death, 1=respawn
    [PunRPC]
    private void RPC_PlaySFX3D(Vector3 pos, int which, float volume, float maxDist)
    {
        AudioClip clip = (which == 0) ? deathSfx : respawnSfx;
        if (!clip) return;

        GameObject go = new GameObject("OneShotSFX");
        go.transform.position = pos;

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f; // 3D
        source.minDistance = 1f;
        source.maxDistance = Mathf.Max(5f, maxDist);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.playOnAwake = false;
        source.volume = Mathf.Clamp01(volume);

        source.Play();
        Destroy(go, clip.length + 0.2f);
    }
}
