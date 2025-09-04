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

            // RPC'yi t�m oyunculara g�nder
            photonView.RPC("ActivateTorchesRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void ActivateTorchesRPC()
    {
        Debug.Log("TORCHLER A�ILIYOR!");

        foreach (GameObject torch in torchesToActivate)
        {
            if (torch != null)
                torch.SetActive(true);
        }
    }
}
