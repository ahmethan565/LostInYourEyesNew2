using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ResolutionSelector : MonoBehaviour
{
    public TextMeshProUGUI resolutionText;
    public Button previousResolutionButton;
    public Button nextResolutionButton;

    private List<Resolution> uniqueResolutions; // Ba�lang��ta null olarak kalabilir
    private int pendingResolutionIndex = 0;
    public const string RESOLUTION_PREF_KEY = "ResolutionIndex";

    [Header("Resolution Filtering")]
    [Tooltip("Hedeflenen en boy oran� (�rn. 16:9 i�in 1.777f, 16:10 i�in 1.6f)")]
    public float targetAspectRatio = 16f / 9f;
    [Tooltip("En boy oran� i�in kabul edilebilir sapma (�rn. 0.01f)")]
    public float aspectRatioTolerance = 0.01f;
    [Tooltip("Minimum geni�lik ��z�n�rl���")]
    public int minWidth = 1280;
    [Tooltip("Minimum y�kseklik ��z�n�rl���")]
    public int minHeight = 720;

    [Header("Custom Resolutions (Bypass Filtering)")]
    [Tooltip("Filtrelemeye tabi tutulmadan eklenecek �zel ��z�n�rl�kler (geni�lik x y�kseklik)")]
    public List<Vector2Int> customResolutions = new List<Vector2Int>();


    void Awake()
    {
        if (resolutionText == null) { Debug.LogError("Resolution Text is not assigned in ResolutionSelector!", this); return; }
        if (previousResolutionButton == null) { Debug.LogError("Previous Resolution Button is not assigned in ResolutionSelector!", this); return; }
        if (nextResolutionButton == null) { Debug.LogError("Next Resolution Button is not assigned in ResolutionSelector!", this); return; }

        // PopulateResolutionList(); // Awake'den kald�rd�k, ��nk� LoadResolution/GetPendingResolution i�inde kontrol edece�iz
        // Ama yine de bu scriptin UI'� g�stermesi i�in ba�lat�lmas� laz�m.
        // E�er bu objenin her zaman aktif olaca��n� biliyorsan�z, Awake'de kalmas� sorun de�il.
        // G�venli olmas� i�in, ilk ihtiya� duyuldu�unda doldurulmas�n� sa�layaca��z.
    }

    void OnEnable()
    {
        previousResolutionButton.onClick.AddListener(PreviousResolution);
        nextResolutionButton.onClick.AddListener(NextResolution);

        // Buradan LoadResolution �a�r�l�yor, o y�zden LoadResolution i�inde doldurma kontrol� �nemli
        // LoadResolution(); // SettingsManager'dan �a�r�ld���nda �al��acak, bu k�sm� SettingsManager y�netiyor.
        // UpdateResolutionUI(); // UI'� ba�lang��ta g�ncel tutmak i�in yine de �a��rabiliriz, ama LoadResolution da bunu yap�yor.
    }

    void OnDisable()
    {
        previousResolutionButton.onClick.RemoveListener(PreviousResolution);
        nextResolutionButton.onClick.RemoveListener(NextResolution);
    }

    private void PopulateResolutionList()
    {
        uniqueResolutions = Screen.resolutions
            .Where(res => {
                float currentAspectRatio = (float)res.width / res.height;
                bool meetsAspectRatio = Mathf.Abs(currentAspectRatio - targetAspectRatio) < aspectRatioTolerance;
                bool meetsMinDimensions = res.width >= minWidth && res.height >= minHeight;
                return meetsAspectRatio && meetsMinDimensions;
            })
            .GroupBy(res => new { res.width, res.height })
            .Select(group => group.OrderByDescending(res => res.refreshRateRatio).First())
            .OrderBy(res => res.width * res.height)
            .ToList();

        // Custom resolution'lar� ekle (filtrelemeden)
        foreach (var vec in customResolutions)
        {
            if (!uniqueResolutions.Any(r => r.width == vec.x && r.height == vec.y))
            {
                Resolution custom = new Resolution
                {
                    width = vec.x,
                    height = vec.y,
                    refreshRate = 60 // varsay�lan bir de�er, de�i�tirilebilir
                };
                uniqueResolutions.Add(custom);
            }
        }

        // Listeyi yeniden s�rala
        uniqueResolutions = uniqueResolutions
            .OrderBy(res => res.width * res.height)
            .ToList();

        if (uniqueResolutions.Count == 0)
        {
            Debug.LogError("No unique resolutions found after filtering! Check your filter settings. Adding current screen resolution as fallback.", this);
            uniqueResolutions.Add(Screen.currentResolution);
        }
    }

    private void UpdateResolutionUI()
    {
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            resolutionText.text = "N/A"; // ��z�n�rl�k bulunamazsa
            previousResolutionButton.interactable = false;
            nextResolutionButton.interactable = false;
            return;
        }

        Resolution selectedRes = uniqueResolutions[pendingResolutionIndex];
        resolutionText.text = $"{selectedRes.width}x{selectedRes.height}";

        previousResolutionButton.interactable = pendingResolutionIndex > 0;
        nextResolutionButton.interactable = pendingResolutionIndex < uniqueResolutions.Count - 1;
    }

    public void PreviousResolution()
    {
        // uniqueResolutions'�n dolu oldu�undan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) { PopulateResolutionList(); }
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) return; // Hala bo�sa i�lem yapma

        if (pendingResolutionIndex > 0)
        {
            pendingResolutionIndex--;
            UpdateResolutionUI();
            Debug.Log($"Resolution pending: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
        }
    }

    public void NextResolution()
    {
        // uniqueResolutions'�n dolu oldu�undan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) { PopulateResolutionList(); }
        if (uniqueResolutions == null || uniqueResolutions.Count == 0) return; // Hala bo�sa i�lem yapma

        if (pendingResolutionIndex < uniqueResolutions.Count - 1)
        {
            pendingResolutionIndex++;
            UpdateResolutionUI();
            Debug.Log($"Resolution pending: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
        }
    }

    // SettingsManager taraf�ndan �a�r�lacak: Se�ilen ��z�n�rl��� d�nd�r�r.
    public Resolution GetPendingResolution()
    {
        // uniqueResolutions'�n dolu oldu�undan emin ol
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            PopulateResolutionList();
            if (uniqueResolutions == null || uniqueResolutions.Count == 0)
            {
                Debug.LogWarning("Resolution list is empty even after attempting to populate. Returning current screen resolution.", this);
                return Screen.currentResolution;
            }
        }

        if (pendingResolutionIndex >= 0 && pendingResolutionIndex < uniqueResolutions.Count)
        {
            return uniqueResolutions[pendingResolutionIndex];
        }
        Debug.LogWarning("Pending resolution index out of bounds. Returning current screen resolution.", this);
        return Screen.currentResolution;
    }

    // --- Kal�c�l�k ve Uygulama Metotlar� ---

    public void LoadResolution()
    {
        // uniqueResolutions'�n dolu oldu�undan emin ol <-- BURADAK� DE����KL�K KR�T�K
        if (uniqueResolutions == null || uniqueResolutions.Count == 0)
        {
            PopulateResolutionList();
            if (uniqueResolutions == null || uniqueResolutions.Count == 0)
            {
                Debug.LogError("Failed to populate uniqueResolutions list. Cannot load resolution.", this);
                return; // Listeyi dolduramazsak ilerleyemeyiz.
            }
        }

        // Kaydedilmi� ��z�n�rl�k indeksini y�kle. Yoksa varsay�lan olarak mevcut ekran ��z�n�rl���n� bul.
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, -1);
        // Sat�r 117'deki hata, uniqueResolutions.Count'a eri�meden �nce uniqueResolutions'�n null olmamas�n� garanti etmedi�imiz i�indi.
        if (savedIndex != -1 && savedIndex < uniqueResolutions.Count)
        {
            pendingResolutionIndex = savedIndex;
        }
        else
        {
            // Kay�tl� ayar yoksa, mevcut sistem ��z�n�rl���n�n indeksini bul
            pendingResolutionIndex = uniqueResolutions.FindIndex(res =>
                res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height);
            if (pendingResolutionIndex == -1) pendingResolutionIndex = 0;
        }
        UpdateResolutionUI();
        Debug.Log($"Resolution Loaded: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
    }

    public void ApplyResolution()
    {
        Debug.Log("ResolutionSelector.ApplyResolution() called. Actual resolution change coordinated by SettingsManager.");
    }

    public void SaveResolution()
    {
        PlayerPrefs.SetInt(RESOLUTION_PREF_KEY, pendingResolutionIndex);
        Debug.Log($"Resolution Saved: {uniqueResolutions[pendingResolutionIndex].width}x{uniqueResolutions[pendingResolutionIndex].height}");
    }
}