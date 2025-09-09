using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class Launcher : MonoBehaviourPunCallbacks
{
    void Start()
    {
        //Debug.Log("🚀 Launcher sahnesi yüklendi.");

        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("🔌 Photon bağlantısı başlatılıyor...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("📡 Zaten bağlı, Lobby'ye geçiliyor...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✔ Connected to Master – Master Server'a bağlandı.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✔ Joined Lobby – Lobby'ye giriş yapıldı.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"❌ Disconnected: {cause}");
    }
}
