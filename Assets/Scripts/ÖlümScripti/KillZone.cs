using UnityEngine;

public class KillZone : MonoBehaviour
{
    [Tooltip("Bu kill zondan sonra spawn edilecek nokta")]
    public Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        var death = other.GetComponentInParent<PlayerDeath>();
        if (death != null) death.Kill(respawnPoint);
    }
}
