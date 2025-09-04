using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Play butonuna bas�l�nca Lobby sahnesini y�kleyecek

    [Header("Panels")]
    [SerializeField] private GameObject optionsMenuPanel;

    [Header("Tab Panels")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    private GameObject currentActivePanel;

    [Header("System Infos")]
    [SerializeField] private TextMeshProUGUI displayAdapterText;
    [SerializeField] private TextMeshProUGUI monitorText;

    void Start()
    {
        optionsMenuPanel.SetActive(false);

        // Sistem bilgilerini g�ster
        if (displayAdapterText != null)
            displayAdapterText.text = SystemInfo.graphicsDeviceName;

        Resolution res = Screen.currentResolution;
        double hz = res.refreshRateRatio.value;
        if (monitorText != null)
            monitorText.text = $"{res.width}x{res.height} @ {(int)hz}Hz";
    }

    private void Update()
    {
        // �ste�e ba�l�: ESC'ye bas�nca kapans�n m�?
        if (Input.GetKeyDown(KeyCode.Escape) && optionsMenuPanel.activeSelf)
        {
            CloseOptions();
        }
    }

    // Ayarlar panelini a�
    public void OpenOptions()
    {
        optionsMenuPanel.SetActive(true);
        ShowPanel(displayPanel); // Varsay�lan olarak Display paneli a��ls�n
        Debug.Log("Ana men�den Ayarlar A��ld�");
    }

    // Ayarlar panelini kapat
    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        Debug.Log("Ana men�den Ayarlar Kapat�ld�");
    }

    // Sekmeler (tab'lar)
    public void OnDisplayButton() => ShowPanel(displayPanel);
    public void OnGraphicsButton() => ShowPanel(graphicsPanel);
    public void OnAudioButton() => ShowPanel(audioPanel);
    public void OnControlsButton() => ShowPanel(controlsPanel);

    private void ShowPanel(GameObject panelToShow)
    {
        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        panelToShow.SetActive(true);
        currentActivePanel = panelToShow;
    }


    public void OnPlayButton()
    {
        SceneManager.LoadScene("Lobby");
    }

    // Quit butonuna bas�l�nca oyundan ��k
    public void OnQuitButton()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
