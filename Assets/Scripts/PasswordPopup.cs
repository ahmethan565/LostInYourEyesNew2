// PasswordPopup.cs
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Unity.VisualScripting; // Button için

public class PasswordPopup : MonoBehaviourPunCallbacks
{
    public static PasswordPopup Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button joinButton; // Inspector'dan atayın
    [SerializeField] private TextMeshProUGUI errorMessageText; // Hata mesajını gösterecek TMP Text
    [SerializeField] private GameObject loadingIndicator; // Yükleniyor animasyonu/metni
    
    // Yeni hata popup elemanları
    [SerializeField] private GameObject errorPopup; // Hata popup paneli
    [SerializeField] private TextMeshProUGUI errorPopupText; // Hata popup metni
    [SerializeField] private Button tryAgainButton; // Try Again butonu

    private RoomInfo currentRoom;
    private bool waitingToJoinRoom = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
            panel.SetActive(false);
        else
            Debug.LogError("[PasswordPopup] Panel objesi bağlanmamış!");

        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false); // Başlangıçta hata mesajını gizle
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false); // Başlangıçta yükleme göstergesini gizle
        if (errorPopup != null)
            errorPopup.SetActive(false); // Başlangıçta hata popup'ını gizle
            
        // Try Again butonuna event listener ekle
        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(OnTryAgainButton);
    }

    public void Show(RoomInfo room)
    {
        if (room == null)
        {
            Debug.LogError("[PasswordPopup] RoomInfo null geldi!");
            return;
        }

        if (panel == null || passwordInput == null || joinButton == null || errorMessageText == null)
        {
            Debug.LogError("[PasswordPopup] Panel, InputField, JoinButton veya ErrorMessageText atanmadı!");
            return;
        }

        Debug.Log("[PasswordPopup] Show() çağrıldı. Room: " + room.Name);
        currentRoom = room;
        passwordInput.text = "";
        errorMessageText.gameObject.SetActive(false); // Her gösterimde önceki hata mesajını temizle
        if (errorPopup != null) errorPopup.SetActive(false); // Hata popup'ını da gizle
        loadingIndicator.SetActive(false); // Yükleme göstergesini kapat
        joinButton.interactable = true; // Butonu aktif et
        panel.SetActive(true);
        
        // Eğer odada şifre yoksa direkt gir, varsa kullanıcının şifre girmesini bekle
        string roomPassword = currentRoom.CustomProperties["pwd"] as string;
        if (string.IsNullOrEmpty(roomPassword))
        {
            // Şifre yok, direkt odaya gir
            Debug.Log("[PasswordPopup] Odada şifre yok, direkt giriliyor...");
            OnJoinButton();
        }
        else
        {
            // Şifre var, kullanıcının girmesini bekle
            Debug.Log("[PasswordPopup] Odada şifre var, kullanıcı girişini bekleniyor...");
            // Input field'a focus ver ki kullanıcı hemen yazmaya başlayabilsin
            if (passwordInput != null)
                passwordInput.Select();
        }

    }

    public void OnJoinButton()
    {
        if (currentRoom == null)
        {
            Debug.LogError("[PasswordPopup] currentRoom null, giriş yapılamaz.");
            DisplayErrorMessage("Oda bilgisi eksik. Lütfen tekrar deneyin.");
            return;
        }

        string enteredPassword = passwordInput.text.Trim();
        string correctPassword = currentRoom.CustomProperties["pwd"] as string;

        if (enteredPassword == correctPassword)
        {
            Debug.Log("[PasswordPopup] Şifre doğru, odaya giriliyor...");

            // UI'ı bağlantı sürecini göstermek için ayarla
            joinButton.interactable = false; // Butonu devre dışı bırak
            loadingIndicator.SetActive(true); // Yükleme göstergesini aç
            errorMessageText.gameObject.SetActive(false); // Hata mesajını gizle

            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[PasswordPopup] Önce mevcut odadan çıkılıyor...");
                PhotonNetwork.LeaveRoom();
            }
            else if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[PasswordPopup] Lobby'de değiliz, JoinLobby çağrılıyor...");
                PhotonNetwork.JoinLobby();
                waitingToJoinRoom = true;
            }
            else
            {
                PhotonNetwork.JoinRoom(currentRoom.Name);
                // Panel kapanışı OnJoinedRoom veya OnJoinRoomFailed'a taşındı
            }
        }
        else
        {
            Debug.LogWarning("[PasswordPopup] Wrong Password!");
            ShowWrongPasswordPopup(); // Güzel hata popup'ını göster
        }
    }

    // Yeni hata mesajı gösterme metodu
    private void DisplayErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
        }
        // Hata durumunda loading göstergesini kapat ve butonu tekrar aktif et
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (joinButton != null) joinButton.interactable = true;
    }

    // Yanlış şifre için güzel popup göster
    private void ShowWrongPasswordPopup()
    {
        if (errorPopup != null && errorPopupText != null)
        {
            errorPopupText.text = "Wrong Password!\nPlease check your password and try again.";
            errorPopup.SetActive(true);
        }
        
        // Loading göstergesini kapat ve join butonunu aktif et
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (joinButton != null) joinButton.interactable = true;
    }

    // Try Again butonuna basıldığında çağrılır
    public void OnTryAgainButton()
    {
        if (errorPopup != null)
            errorPopup.SetActive(false);
            
        // Input field'ı temizle ki kullanıcı yeni şifre girebilsin
        if (passwordInput != null)
            passwordInput.text = "";
            
        // Focus'u input field'a ver
        if (passwordInput != null)
            passwordInput.Select();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PasswordPopup] Odadan çıkıldı. Şimdi lobby'e giriliyor...");
        PhotonNetwork.JoinLobby();
        waitingToJoinRoom = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PasswordPopup] Lobby'e girildi.");

        if (waitingToJoinRoom && currentRoom != null)
        {
            Debug.Log("[PasswordPopup] Odaya yeniden bağlanılıyor: " + currentRoom.Name);
            PhotonNetwork.JoinRoom(currentRoom.Name);
            // Panel kapanışı OnJoinedRoom veya OnJoinRoomFailed'a taşındı
            waitingToJoinRoom = false; // İşlem başladı, bayrağı sıfırla
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("🎉 Odaya başarıyla katıldım: " + PhotonNetwork.CurrentRoom.Name);
        panel.SetActive(false); // Başarılı katılımda paneli kapat
        loadingIndicator.SetActive(false); // Yükleme göstergesini kapat
        joinButton.interactable = true; // Butonu eski haline getir
        PhotonNetwork.LoadLevel("LobbyRoom");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PasswordPopup] Odaya katılım başarısız: {message} (Code: {returnCode})");
        DisplayErrorMessage($"Odaya katılamadı: {message}"); // Kullanıcıya hata mesajını göster
        loadingIndicator.SetActive(false); // Yükleme göstergesini kapat
        joinButton.interactable = true; // Butonu tekrar aktif et
        // Popup'ı açık tut ki kullanıcı tekrar denesin veya iptal etsin
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PasswordPopup] Photon bağlantısı kesildi: {cause}");
        if (panel.activeSelf) // Eğer popup açıkken bağlantı kesilirse
        {
            DisplayErrorMessage($"Bağlantı kesildi: {cause}. Lütfen tekrar deneyin.");
            loadingIndicator.SetActive(false);
            joinButton.interactable = true;
            waitingToJoinRoom = false; // Bekleyen bir işlem varsa iptal et
        }
    }

    public void OnCancelButton()
    {
        if (panel != null)
            panel.SetActive(false);

        // İptal edildiğinde tüm durumları sıfırla
        loadingIndicator.SetActive(false);
        joinButton.interactable = true;
        errorMessageText.gameObject.SetActive(false);
        if (errorPopup != null) errorPopup.SetActive(false); // Hata popup'ını da kapat
        waitingToJoinRoom = false; // Çok önemli: eğer bekleyen bir odaya katılma işlemi varsa iptal et.
    }
}