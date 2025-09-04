// Assets/Scripts/LobbyUIManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject createRoomPanel;   // CreateRoomPanel objesi
    [SerializeField] private GameObject refreshButton;     // Refresh butonu
    [SerializeField] private GameObject backButton;        // Back butonu

    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.Escape))
            OnBackPressed();
        */
    }

    public void OnCreateLobbyClicked()
    {
        createRoomPanel.SetActive(true);
    }

    public void OnRefreshClicked()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.NetworkClientState == ClientState.JoinedLobby)
        {
            Debug.Log("? Lobby?deyiz, oda listesini yeniliyorum.");
            PhotonNetwork.GetCustomRoomList(TypedLobby.Default, "");
        }
        else
        {
            Debug.LogWarning($"?? Hen�z lobby?de de�iliz (State: {PhotonNetwork.NetworkClientState}), bekle OnJoinedLobby?yu.");
        }
    }
    public void OnBackPressed()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("? Oday� terk ediyorum?");
            PhotonNetwork.LeaveRoom();
            return; // Sahne ge�i�i callback'te olacak
        }

        SceneManager.LoadScene("MainMenu");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("? OnLeftRoom ? Lobby?ye d�n�l�yor.");
        PhotonNetwork.JoinLobby();
        SceneManager.LoadScene("MainMenu"); // Buraya al
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("? ConnectedToMaster ? Lobby?ye kat�l�yorum.");
        PhotonNetwork.JoinLobby();
    }
}
