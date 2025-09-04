using UnityEngine;
using Photon.Pun;

public class cataSolvedSound : MonoBehaviourPun
{
    [SerializeField] private float volume = 1f;

    public AudioClip tableDoorSound;

    void Start()
    {
        if (photonView.IsMine)
        {
            // Sadece sahibi RPC gönderir
            photonView.RPC("RpcPlaySound", RpcTarget.All, volume);
        }
    }

    [PunRPC]
    void RpcPlaySound(float vol)
    {
        AudioSource.PlayClipAtPoint(tableDoorSound, transform.position, vol);
        
        // Ses çalındı, artık bu obje gerekmez
        Destroy(gameObject);
    }
}