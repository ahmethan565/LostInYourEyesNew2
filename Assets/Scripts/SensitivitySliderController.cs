using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class SensitivitySliderController : MonoBehaviour
{
    // Inspector'da atanacak bile�enler
    [Header("UI References")]
    [Tooltip("Bu slider'�n ba�l� oldu�u UI Slider bile�eni.")]
    [SerializeField] private Slider uiSlider;
    [Tooltip("Hassasiyet de�erini g�steren TextMeshPro metni.")]
    [SerializeField] private TextMeshProUGUI valueText;
    [Tooltip("Dolu k�sm�n g�rseli (Image bile�enine ihtiyac�n�z var).")]
    [SerializeField] private Image fillImage; // *** YEN� EKLEND� ***

    [Header("Slider Settings")]
    [Tooltip("Slider'�n g�sterece�i minimum hassasiyet de�eri.")]
    [SerializeField] private float minSensitivity = 1f;
    [Tooltip("Slider'�n g�sterece�i maksimum hassasiyet de�eri.")]
    [SerializeField] private float maxSensitivity = 10f;
    [Tooltip("Varsay�lan hassasiyet de�eri.")]
    [SerializeField] private float defaultSensitivity = 5f;

    [Header("Persistence")]
    [Tooltip("Hassasiyet de�erini kaydetmek ve y�klemek i�in PlayerPrefs anahtar�.")]
    [SerializeField] private string playerPrefsKey = "MasterSensitivity";

    [Header("Debugging")]
    [Tooltip("Bu, slider'�n g�ncel de�erini Inspector'da g�rmenizi sa�lar.")]
    [SerializeField]
    private float _currentSliderValue;

    // Hassasiyet de�eri de�i�ti�inde di�er scriptleri bilgilendirmek i�in Event
    public UnityEvent<float> OnSensitivityChanged;

    // Start metodu burada bo� b�rak�labilir veya ba�lang��ta bir i�lem yap�yorsa kullan�labilir.
    // Ancak, LoadSensitivity zaten OnEnable'da �a�r�ld��� i�in burada bir �eye gerek yok.
    private void Start()
    {
        // UpdateSliderVisual(defaultSensitivity); // Bu sat�r LoadSensitivity i�inde zaten hallediliyor
    }

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

        // FillImage referans�n� da Awake'de kontrol et ve otomatik bulmaya �al��
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


        uiSlider.minValue = minSensitivity;
        uiSlider.maxValue = maxSensitivity;

        if (OnSensitivityChanged == null)
        {
            OnSensitivityChanged = new UnityEvent<float>();
        }
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

        LoadSensitivity();
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
        SaveSensitivity(newValue);
        OnSensitivityChanged.Invoke(newValue);
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
            // Slider'�n de�erini (minSensitivity-maxSensitivity aral���nda) 0-1 aral���na d�n��t�r
            float fillAmount = Mathf.InverseLerp(minSensitivity, maxSensitivity, currentValue);
            fillImage.fillAmount = fillAmount; // G�r�nt�n�n ne kadar�n�n dolu olaca��n� ayarla
        }
        else
        {
            Debug.LogWarning($"UpdateSliderVisual: {gameObject.name} - fillImage is NULL. Fill visual won't work.");
        }
    }

    // --- Kaydetme/Y�kleme Metotlar� ---
    private void LoadSensitivity()
    {
        float loadedValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultSensitivity);

        if (uiSlider != null)
        {
            // Y�klenen de�eri slider'a ayarla.
            // uiSlider.value'yu set etmek OnSliderValueChanged'� tetikleyecektir.
            // Bu, _currentSliderValue'nun ve g�rselin de g�ncellenmesini sa�lar.
            uiSlider.value = loadedValue;
        }
        else
        {
            Debug.LogError($"LoadSensitivity: {gameObject.name} - UI Slider is NULL, cannot set value during load.");
        }
    }

    private void SaveSensitivity(float valueToSave)
    {
        PlayerPrefs.SetFloat(playerPrefsKey, valueToSave);
        PlayerPrefs.Save();
    }
}