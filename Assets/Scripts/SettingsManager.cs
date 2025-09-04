using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    // Inspector'dan atayaca��n�z ayar scriptlerinin referanslar�.
    public ResolutionSelector resolutionSelector;
    public FullscreenToggle fullscreenToggle; // << BURASI DE���MED� >>
    //public GraphicsQualitySelector graphicsQualitySelector;

    public Button applyButton; // UI'daki "Apply" butonu

    void Awake()
    {
        // �nemli: Scriptlerin atan�p atanmad���n� kontrol et.
        if (resolutionSelector == null) Debug.LogError("ResolutionSelector not assigned in SettingsManager!", this);
        if (fullscreenToggle == null) Debug.LogError("FullscreenToggle not assigned in SettingsManager!", this);
        //if (graphicsQualitySelector == null) Debug.LogError("GraphicsQualitySelector not assigned in SettingsManager!", this);
    }

    void OnEnable()
    {
        // Apply butonuna listener ekle. Birden fazla eklenmemesi i�in OnEnable/OnDisable kullan�l�yor.
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyAllSettings);
        }
        else
        {
            Debug.LogError("Apply Button is not assigned in SettingsManager! Please assign it in the Inspector.", this);
        }

        // Ayar men�s� a��ld���nda (script etkinle�ti�inde) t�m ayarlar� y�kle
        LoadAllSettings();
    }

    void OnDisable()
    {
        // Script devre d��� b�rak�ld���nda listener'� kald�r
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyAllSettings);
        }
    }

    void OnApplicationQuit()
    {
        // Uygulama kapan�rken olas� son de�i�ikliklerin kaydedildi�inden emin ol
        PlayerPrefs.Save();
    }

    // T�m ayarlar� PlayerPrefs'ten y�kler ve ilgili UI elemanlar�n� g�nceller.
    public void LoadAllSettings()
    {
        Debug.Log("Loading all settings...");
        if (resolutionSelector != null) resolutionSelector.LoadResolution();
        if (fullscreenToggle != null) fullscreenToggle.LoadFullscreenMode(); // << BURASI DE���T� >>
        //if (graphicsQualitySelector != null) graphicsQualitySelector.LoadQuality();
        Debug.Log("All settings loaded!");
    }

    // T�m bekleyen ayarlar� oyuna uygular.
    public void ApplyAllSettings()
    {
        Debug.Log("Applying all settings...");

        // --- G�r�nt� Ayarlar�n� Uygula (��z�n�rl�k ve Tam Ekran Birlikte) ---
        if (resolutionSelector != null && fullscreenToggle != null)
        {
            Resolution pendingRes = resolutionSelector.GetPendingResolution();
            FullScreenMode pendingMode = fullscreenToggle.GetPendingFullScreenMode(); // << BURASI DE���T� >>

            Screen.SetResolution(pendingRes.width, pendingRes.height, pendingMode); // << BURASI DE���T� >>
            Debug.Log($"Applied Screen Resolution: {pendingRes.width}x{pendingRes.height} in {pendingMode} mode.");
        }
        else
        {
            Debug.LogWarning("ResolutionSelector or FullscreenToggle not assigned. Screen resolution might not be applied correctly.", this);
            // Fallback: Yaln�zca biri atanm��sa veya ikisi de atanmam��sa, mevcut ayarlar� koruyarak uygula.
            if (resolutionSelector != null) resolutionSelector.ApplyResolution(); // ApplyResolution �imdilik bo�, ama belki gelecekte ek lojik olur.
            if (fullscreenToggle != null) fullscreenToggle.ApplyFullscreen(); // Ayn� �ekilde.
        }
        /*
        // --- Grafik Kalitesi Ayarlar�n� Uygula ---
        if (graphicsQualitySelector != null)
        {
            graphicsQualitySelector.ApplyQuality();
        }
        */
        // T�m ayarlar uyguland�ktan sonra kaydet
        SaveAllSettings();

        Debug.Log("All settings applied successfully!");
    }

    // T�m ayarlar� PlayerPrefs'e kaydeder.
    public void SaveAllSettings()
    {
        Debug.Log("Saving all settings...");
        if (resolutionSelector != null) resolutionSelector.SaveResolution();
        if (fullscreenToggle != null) fullscreenToggle.SaveFullscreen(); // << BURASI DE���MED� >>
        //if (graphicsQualitySelector != null) graphicsQualitySelector.SaveQuality();

        PlayerPrefs.Save(); // T�m PlayerPrefs de�i�ikliklerini diske yaz
        Debug.Log("All settings saved successfully!");
    }

    // �ste�e ba�l�: Ayarlar� varsay�lanlara s�f�rlar.
    public void ResetToDefaults()
    {
        Debug.Log("Resetting all settings to defaults...");

        // PlayerPrefs'ten ilgili anahtarlar� sil
        PlayerPrefs.DeleteKey(ResolutionSelector.RESOLUTION_PREF_KEY);
        PlayerPrefs.DeleteKey(FullscreenToggle.FULLSCREEN_MODE_PREF_KEY); // << BURASI DE���T� >>
        //PlayerPrefs.DeleteKey(GraphicsQualitySelector.QUALITY_PREF_KEY);
        PlayerPrefs.Save(); // De�i�iklikleri hemen diske yaz

        // Ayarlar� yeniden y�kle (bu, UI'y� varsay�lanlara geri d�nd�r�r)
        LoadAllSettings();

        // �ste�e ba�l�: Varsay�lanlar� hemen oyuna uygula
        // ApplyAllSettings(); // E�er s�f�rlama sonras� hemen uygulans�n istenirse etkinle�tirin.

        Debug.Log("All settings reset to defaults!");
    }
}