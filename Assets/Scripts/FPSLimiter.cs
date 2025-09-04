#pragma warning disable CS0414 // Kullan�lmayan alanlar i�in uyar�y� kapat

using UnityEngine;

/// <summary>
/// T�m sahnelerde ge�erli, profesyonel FPS sabitleme sistemi.
/// Edit�rde ve build s�ras�nda farkl� limitler tan�mlanabilir.
/// </summary>
public class FPSLimiter : MonoBehaviour
{
    public static FPSLimiter Instance { get; private set; }

    [Header("FPS Ayarlar�")]
    [SerializeField] private int editorTargetFPS = 60;
    [SerializeField] private int buildTargetFPS = 60;
    [SerializeField] private bool limitWhenInBackground = true;
    [SerializeField] private int backgroundTargetFPS = 15;

    private void Awake()
    {
        // Singleton kontrol�
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyFPSLimit();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (limitWhenInBackground)
            ApplyFPSLimit();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (limitWhenInBackground)
            ApplyFPSLimit();
    }

    private void ApplyFPSLimit()
    {
        if (!Application.isPlaying) return;

#if UNITY_EDITOR
        Application.targetFrameRate = editorTargetFPS;
#else
        if (Application.isFocused)
            Application.targetFrameRate = buildTargetFPS;
        else
            Application.targetFrameRate = limitWhenInBackground ? backgroundTargetFPS : buildTargetFPS;
#endif

        if (QualitySettings.vSyncCount != 0)
        {
            Debug.LogWarning("VSync aktif. FPS limiti ge�erli olmayabilir. VSync ayar�n� kontrol edin.");
        }
    }
}

#pragma warning restore CS0414 // Uyar�y� tekrar a�
