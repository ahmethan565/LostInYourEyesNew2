using Unity.VisualScripting;
using UnityEngine;

public class doorCheckerGreen : MonoBehaviour
{

    public DoorLift doorToOpen;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<clientPlayerU>() != null)
        {
            Debug.Log("BLUEPLAYER ENTERED");
            doorToOpen.ToggleDoor();
        }
        else
        {
            Debug.Log("Yanlış oyuncu");
        }


        // if (other.GetComponent<FPSPlayerControllerSingle>() != null)
        // {
        //     Debug.Log("BLUEPLAYER ENTERED");
        //     doorToOpen.ToggleDoor();
        // }
        // else
        // {
        //     Debug.Log("Yanlış oyuncu");
        // }
    }
}
