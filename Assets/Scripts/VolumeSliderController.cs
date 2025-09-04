using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class VolumeSliderController : MonoBehaviour
{
    // Inspector'da atanacak bile�enler
    [Header("UI References")]
    [Tooltip("Bu slider'�n ba�l� oldu�u UI Slider bile�eni.")]
    [SerializeField] private Slider uiSlider;
    [Tooltip("Ses seviyesini g�steren TextMeshPro metni.")]
    [SerializeField] private TextMeshProUGUI valueText;

    // Fill g�rselinin RectTransform'u yerine, Image bile�enine ihtiyac�m�z var.
    [Tooltip("Dolu k�sm�n g�rseli (fillImageRect yerine Image bile�enine ihtiyac�n�z var).")]
    [SerializeField] private Image fillImage; // *** BURASI DE���T� ***

    [Header("Audio References")]
    [Tooltip("Bu slider'�n kontrol edece�i AudioSource.")]
    [SerializeField] private AudioSource targetAudioSource;
    [Tooltip("Bu slider'�n kontrol edece�i AudioMixerGroup parametresi.")]
    [SerializeField] private AudioMixerGroup targetAudioMixerGroup;
    [Tooltip("AudioMixer'daki ses parametresinin ad� (�rne�in 'MasterVolume').")]
    [SerializeField] private string mixerParameterName = "MasterVolume";

    [Header("Slider Settings")]
    [Tooltip("Slider'�n g�sterece�i minimum ses de�eri (genellikle 0).")]
    [SerializeField] private float minVolume = 0f;
    [Tooltip("Slider'�n g�sterece�i maksimum ses de�eri (genellikle 100).")]
    [SerializeField] private float maxVolume = 100f;

    // maxFillWidth art�k gerekli de�il, ��nk� Image.fillAmount kullanaca��z.
    // [Tooltip("G�rsel dolgunun maksimum geni�li�i (varsay�lan fillImage'in boyutu kullan�labilir).")]
    // [SerializeField] private float maxFillWidth = 300f; 

    [Header("Persistence")]
    [Tooltip("Ses seviyesi de�erini kaydetmek ve y�klemek i�in PlayerPrefs anahtar�.")]
    [SerializeField] private string playerPrefsKey = "MasterVolume";
    [Tooltip("Varsay�lan ses seviyesi de�eri.")]
    [SerializeField] private float defaultVolume = 75f;

    [Header("Debugging")]
    [Tooltip("Bu, slider'�n g�ncel de�erini Inspector'da g�rmenizi sa�lar.")]
    [SerializeField]
    private float _currentSliderValue;

    // --- Ba�lang�� ve Olay Atamalar� ---
    private void Awake()
    {
        if (uiSlider == null)
        {
            uiSlider = GetComponent<Slider>();
            if (uiSlider == null)
            {
                Debug.LogError($"Awake: {gameObject.name} - UI Slider not found on this GameObject or not assigned in Inspector! Please assign the Slider component.");
                return;
            }
        }

        // FillImage referans�n� da Awake'de kontrol edebiliriz
        if (fillImage == null)
        {
            // Genellikle slider'�n alt�ndaki "Fill" objesinde Image bile�eni bulunur
            Transform fillTransform = uiSlider.fillRect;
            if (fillTransform != null)
            {
                fillImage = fillTransform.GetComponent<Image>();
            }
            if (fillImage == null)
            {
                Debug.LogWarning($"Awake: {gameObject.name} - Fill Image component not found or assigned! Visual fill won't work correctly.");
            }
        }


        uiSlider.minValue = minVolume;
        uiSlider.maxValue = maxVolume;
    }

    private void OnEnable()
    {
        if (uiSlider != null)
        {
            uiSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        else
        {
            Debug.LogError($"OnEnable: {gameObject.name} - UI Slider is NULL, cannot add listener!");
            return;
        }

        LoadVolume();
    }

    private void OnDisable()
    {
        if (uiSlider != null)
        {
            uiSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    // --- Olay Dinleyici Metotlar� ---
    private void OnSliderValueChanged(float newValue)
    {
        _currentSliderValue = newValue;

        UpdateSliderVisual(newValue);
        SaveVolume(newValue); // Ses seviyesini kaydet

        float audioVolume = newValue / maxVolume;

        if (targetAudioSource != null)
        {
            targetAudioSource.volume = audioVolume;
        }
        else if (targetAudioMixerGroup != null)
        {
            float dbValue = Mathf.Log10(audioVolume) * 20;
            if (audioVolume <= 0.0001f)
            {
                dbValue = -80f;
            }
            targetAudioMixerGroup.audioMixer.SetFloat(mixerParameterName, dbValue);
        }
        else
        {
            Debug.LogWarning($"OnSliderValueChanged: {gameObject.name} - No AudioSource or AudioMixerGroup assigned. Slider only updates visuals and saves preference.");
        }
    }

    // --- G�rsel G�ncelleme Metodu ---
    private void UpdateSliderVisual(float currentValue)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(currentValue).ToString();
        }
        else
        {
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - valueText is NULL. Cannot update text.");
        }

        // *** BURASI DE���T�: fillImage'in fillAmount'unu kullan�yoruz ***
        if (fillImage != null)
        {
            // Slider'�n 0-100 de�erini 0-1 aral���na d�n��t�r
            float fillAmount = Mathf.Clamp01(currentValue / maxVolume);
            fillImage.fillAmount = fillAmount; // G�r�nt�n�n ne kadar�n�n dolu olaca��n� ayarla
        }
        else
        {
            // fillImage atand���ndan emin olmak i�in bir uyar� ekleyebiliriz
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - fillImage is NULL. Fill visual won't work.");
        }
    }

    // --- Kaydetme/Y�kleme Metotlar� ---
    private void LoadVolume()
    {
        float loadedValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultVolume);

        if (targetAudioSource != null)
        {
            if (!PlayerPrefs.HasKey(playerPrefsKey))
            {
                loadedValue = targetAudioSource.volume * maxVolume;
            }
        }
        else if (targetAudioMixerGroup != null)
        {
            float mixerDbValue;
            if (targetAudioMixerGroup.audioMixer.GetFloat(mixerParameterName, out mixerDbValue))
            {
                if (!PlayerPrefs.HasKey(playerPrefsKey))
                {
                    float normalizedValue = Mathf.Pow(10, mixerDbValue / 20);
                    loadedValue = normalizedValue * maxVolume;
                    if (mixerDbValue <= -79f)
                    {
                        loadedValue = minVolume;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"LoadVolume: {gameObject.name} - AudioMixer parameter '{mixerParameterName}' not found. Using PlayerPrefs or default.");
            }
        }
        else
        {
            // Hi�bir ses kayna�� ba�l� de�ilse, sadece PlayerPrefs veya varsay�lan de�eri kullan
        }

        if (uiSlider != null)
        {
            uiSlider.value = loadedValue;
        }
        else
        {
            Debug.LogError($"LoadVolume: {gameObject.name} - UI Slider is NULL, cannot set value during load.");
        }
    }

    private void SaveVolume(float valueToSave)
    {
        PlayerPrefs.SetFloat(playerPrefsKey, valueToSave);
        PlayerPrefs.Save();
    }
}