using Unity.VisualScripting;
using UnityEngine;

public class doorChecker : MonoBehaviour
{
    public DoorLift doorToOpen;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<clientPlayerU>() != null || other.GetComponent<hostPlayerU>() != null)
        {
            doorToOpen.ToggleDoor();
        }
    }
}
