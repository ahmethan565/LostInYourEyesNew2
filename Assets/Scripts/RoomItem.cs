using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class RoomItem : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_Text roomNameText;
    public TMP_Text roomStatusText;
    public TMP_Text playerCountText;
    [SerializeField] private Image passImage;
    [SerializeField] private Image backgroundImage; // Oda background'ı için
    [SerializeField] private Image fullRoomDisableImage; // Dolu odalarda devre dışı bırakılacak image
    [SerializeField] private TabController tabController; // Tab controller referansı
    
    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = Color.white;
    [SerializeField] private Color fullRoomBackgroundColor = Color.gray;
    
    private Button _button;
    private RoomInfo _roomInfo;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
        
        // Eğer TabController referansı atanmamışsa, bu objede arayalım
        if (tabController == null)
        {
            tabController = GetComponent<TabController>();
        }
    }

    public void Setup(RoomInfo info)
    {
        _roomInfo = info;
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        bool isRoomFull = info.PlayerCount == info.MaxPlayers;

        // Oda dolu mu kontrolü
        if (isRoomFull)
        {
            // Dolu oda ayarları
            roomStatusText.text = "Full!";
            _button.interactable = false; // Butonu devre dışı bırak
            passImage.gameObject.SetActive(false); // Şifre işaretini gizle
            
            // Background'i gri yap
            if (backgroundImage != null)
            {
                backgroundImage.color = fullRoomBackgroundColor;
            }
            
            // Belirlediğiniz image'ı devre dışı bırak
            if (fullRoomDisableImage != null)
            {
                fullRoomDisableImage.gameObject.SetActive(false);
            }
            
            // TabController'da hover animasyonunu devre dışı bırak
            if (tabController != null)
            {
                tabController.enableHoverAnimation = false;
            }
        }
        else
        {
            // Normal oda ayarları
            roomStatusText.text = "Join!";
            _button.interactable = true; // Butonu aktif et
            
            // Background'i normal renge getir
            if (backgroundImage != null)
            {
                backgroundImage.color = normalBackgroundColor;
            }
            
            // Belirlediğiniz image'ı tekrar aktif et
            if (fullRoomDisableImage != null)
            {
                fullRoomDisableImage.gameObject.SetActive(true);
            }

            // TabController'da hover animasyonunu tekrar aktif et
            if (tabController != null)
            {
                tabController.enableHoverAnimation = true;

            }
            
            // Şifre kontrolü (sadece dolu olmayan odalar için)
            if (info.CustomProperties.ContainsKey("pwd") && info.CustomProperties["pwd"] is string pwd && !string.IsNullOrEmpty(pwd))
            {
                passImage.gameObject.SetActive(true); // Şifre varsa simgeyi göster
            }
            else
            {
                passImage.gameObject.SetActive(false); // Şifre yoksa gizle
            }
        }
    }


    private void OnClick()
    {
        if (_roomInfo == null)
        {
            Debug.LogError("[RoomItem] RoomInfo null!");
            return;
        }

        // Dolu odalara tıklanamaz (buton zaten devre dışı ama ekstra kontrol)
        if (_roomInfo.PlayerCount == _roomInfo.MaxPlayers)
        {
            Debug.Log("[RoomItem] Oda dolu, giriş yapılamaz!");
            return;
        }

        if (PasswordPopup.Instance != null)
        {
            PasswordPopup.Instance.Show(_roomInfo);
        }
        else
        {
            Debug.LogError("[RoomItem] PasswordPopup.Instance yok! Sahnede PasswordPopup objesi aktif mi?");
        }
    }
}
