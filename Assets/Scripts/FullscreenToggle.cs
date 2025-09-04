using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kullan�yorsan�z. E�er Text kullan�yorsan�z UnityEngine.UI.Text olarak de�i�tirin.
using System.Collections.Generic;

public class FullscreenToggle : MonoBehaviour
{
    public TextMeshProUGUI fullscreenModeText; // Tam ekran modunu g�sterecek metin alan�
    public Button leftButton; // �nceki moda git butonu
    public Button rightButton; // Sonraki moda git butonu

    private List<FullScreenMode> availableModes;
    private int currentIndex = 0; // Kullan�c�n�n se�ti�i ama hen�z uygulamad��� mod indeksi
    public const string FULLSCREEN_MODE_PREF_KEY = "FullscreenModeIndex"; // PlayerPrefs anahtar�

    void Awake()
    {
        // Null referans kontrolleri
        if (fullscreenModeText == null) { Debug.LogError("Fullscreen Mode Text is not assigned in FullscreenToggle!", this); return; }
        if (leftButton == null) { Debug.LogError("Left Button is not assigned in FullscreenToggle!", this); return; }
        if (rightButton == null) { Debug.LogError("Right Button is not assigned in FullscreenToggle!", this); return; }

        PopulateModesList(); // Mevcut mod se�eneklerini doldur
    }

    void OnEnable()
    {
        // Butonlara listener ekle
        leftButton.onClick.AddListener(PreviousMode);
        rightButton.onClick.AddListener(NextMode);

        //LoadFullscreenMode(); // Script etkinle�ti�inde ayar� y�kle ve UI'y� g�ncelle
        UpdateFullscreenUI(); // UI'y� ba�lang��ta g�ncel tut
    }

    void OnDisable()
    {
        // Script devre d��� b�rak�ld���nda listener'lar� kald�r
        leftButton.onClick.RemoveListener(PreviousMode);
        rightButton.onClick.RemoveListener(NextMode);
    }

    private void PopulateModesList()
    {
        availableModes = new List<FullScreenMode>
        {
            FullScreenMode.ExclusiveFullScreen, // �zel Tam Ekran (genellikle eski oyunlarda veya y�ksek performans istenen yerlerde)
            FullScreenMode.FullScreenWindow,    // Penceresiz Tam Ekran (Borderless Window - modern oyunlarda yayg�n)
            FullScreenMode.Windowed             // Pencereli Mod
        };

        // E�er sistem mevcut FullScreenMode'u desteklemiyorsa bu listeyi geni�letilebilir
        // �rne�in, sadece FullScreenWindow ve Windowed modlar� yeterliyse:
        // availableModes = new List<FullScreenMode> { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
    }

    private void UpdateFullscreenUI()
    {
        if (availableModes == null || availableModes.Count == 0) return;

        // Ge�erli se�ili modu metin alan�na yaz
        string modeName = GetModeName(availableModes[currentIndex]);
        fullscreenModeText.text = modeName;

        // Butonlar� etkin/devre d��� b�rakarak s�n�rlar� g�ster
        leftButton.interactable = currentIndex > 0;
        rightButton.interactable = currentIndex < availableModes.Count - 1;
    }

    private string GetModeName(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return "Fullscreen"; // Ya da "�zel Tam Ekran"
            case FullScreenMode.FullScreenWindow:
                return "Windowed Fullscreen"; // Ya da "Borderless"
            case FullScreenMode.Windowed:
                return "Windowed";
            default:
                return "Null??";
        }
    }

    public void PreviousMode()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateFullscreenUI();
            Debug.Log($"Fullscreen mode pending: {GetModeName(availableModes[currentIndex])}");
        }
    }

    public void NextMode()
    {
        if (currentIndex < availableModes.Count - 1)
        {
            currentIndex++;
            UpdateFullscreenUI();
            Debug.Log($"Fullscreen mode pending: {GetModeName(availableModes[currentIndex])}");
        }
    }

    // SettingsManager taraf�ndan �a�r�lacak: Se�ilen tam ekran modunu d�nd�r�r.
    public FullScreenMode GetPendingFullScreenMode()
    {
        if (availableModes == null || availableModes.Count == 0)
        {
            Debug.LogWarning("Fullscreen mode list is empty. Returning current full screen mode.", this);
            return Screen.fullScreenMode;
        }

        if (currentIndex >= 0 && currentIndex < availableModes.Count)
        {
            return availableModes[currentIndex];
        }
        Debug.LogWarning("Pending fullscreen mode index out of bounds. Returning current full screen mode.", this);
        return Screen.fullScreenMode;
    }

    // --- Kal�c�l�k ve Uygulama Metotlar� ---

    public void LoadFullscreenMode()
    {
        if (availableModes == null || availableModes.Count == 0)
        {
            PopulateModesList(); // Liste hen�z yoksa olu�tur
        }

        int savedIndex = PlayerPrefs.GetInt(FULLSCREEN_MODE_PREF_KEY, -1);
        if (savedIndex != -1 && savedIndex < availableModes.Count)
        {
            currentIndex = savedIndex;
        }
        else
        {
            currentIndex = availableModes.FindIndex(mode => mode == Screen.fullScreenMode);
            if (currentIndex == -1) currentIndex = 0;
        }

        UpdateFullscreenUI();
        Debug.Log($"Fullscreen Mode Loaded: {GetModeName(availableModes[currentIndex])}");
    }


    public void ApplyFullscreen()
    {
        // Ger�ek tam ekran de�i�ikli�i SettingsManager taraf�ndan ��z�n�rl�k ile birlikte koordine edilecek.
        // Bu metod FullscreenToggle'�n "uygulama" faz�nda oldu�unu belirtir.
        Debug.Log("FullscreenToggle.ApplyFullscreen() called. Actual fullscreen change coordinated by SettingsManager.");
    }

    public void SaveFullscreen()
    {
        PlayerPrefs.SetInt(FULLSCREEN_MODE_PREF_KEY, currentIndex);
        Debug.Log($"Fullscreen Mode Saved: {GetModeName(availableModes[currentIndex])}");
    }
}