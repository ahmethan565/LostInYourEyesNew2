using UnityEngine;
using Photon.Pun;

public class TorchTrigger_Photon : MonoBehaviourPun
{
    public GameObject[] torchesToActivate;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            Debug.Log("Trigger tetiklendi: " + other.name);

            // RPC'yi tüm oyunculara gönder
            photonView.RPC("ActivateTorchesRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void ActivateTorchesRPC()
    {
        Debug.Log("TORCHLER AÇILIYOR!");

        foreach (GameObject torch in torchesToActivate)
        {
            if (torch != null)
                torch.SetActive(true);
        }
    }
}
