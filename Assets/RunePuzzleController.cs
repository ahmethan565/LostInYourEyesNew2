using UnityEngine;
using Photon.Pun;

public class RunePuzzleController : MonoBehaviourPun, IPunObservable
{
    public RuneSlot[] runeSlots;
    public KeyDoorExample[] doorsToOpen; // Açılacak kapı objeleri
    
    [Header("Lever System")]
    public LeverController[] levers; // Çekilmesi gereken şalterler
    public int requiredLeversCount = 1; // Kaç lever çekilmesi gerektiği
    public bool skipLeversIfAllRunesCompleted = false; // Tüm rünler doğru yerleştirildiğinde lever olmadan kapı açılsın mı?
    
    private bool allRunesCompleted = false;
    private int activatedLeversCount = 0;

    private void Start()
    {
        // Otomatik bulmak istiyorsan
        if (runeSlots == null || runeSlots.Length == 0)
            runeSlots = GetComponentsInChildren<RuneSlot>();

        foreach (var slot in runeSlots)
        {
            slot.puzzleController = this;
        }
        
        // Kapı referanslarını kontrol et
        if (doorsToOpen != null && doorsToOpen.Length > 0)
        {
            for (int i = 0; i < doorsToOpen.Length; i++)
            {
                if (doorsToOpen[i] == null)
                {
                    Debug.LogError($"Door at index {i} in doorsToOpen array is null! Please assign all doors in the Inspector.");
                }
                else
                {
                    // Test door validity by checking required components or properties
                    // Eğer door.OpenDoor() metodu çağrıldığında gerekli olan componentleri kontrol et
                    if (doorsToOpen[i].gameObject.activeSelf == false)
                    {
                        Debug.LogWarning($"Door at index {i} is inactive and might not work correctly!");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No doors assigned to this puzzle controller. The puzzle will not open any doors when completed.");
        }
        
        // Lever referanslarını otomatik olarak bul
        if (levers == null || levers.Length == 0)
        {
            levers = FindObjectsByType<LeverController>(FindObjectsSortMode.None);
            if (levers.Length == 0)
            {
                Debug.LogWarning("No levers found! Doors will open immediately after runes are completed.");
            }
        }
        
        // Required levers count kontrolü
        if (requiredLeversCount <= 0)
            requiredLeversCount = levers.Length;
        
        if (requiredLeversCount > levers.Length)
        {
            Debug.LogWarning($"Required levers count ({requiredLeversCount}) is greater than available levers ({levers.Length}). Setting to available count.");
            requiredLeversCount = levers.Length;
        }
    }

    public void SetLeverReference(LeverController leverRef)
    {
        // Bu metod eski uyumluluk için kalabilir, yeni sistemde gerek yok
        Debug.Log("SetLeverReference is deprecated in multi-lever system. Use levers array instead.");
    }

    public void NotifyRunePlaced()
    {
        // Bu metod, bir rün yerleştirildiğinde RuneSlot tarafından çağrılır.
        // RPC ile tüm clientlarda kontrol edilsin
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("CheckRunePuzzleCompletion", RpcTarget.All);
        }
    }

    [PunRPC]
    private void CheckRunePuzzleCompletion()
    {
        // Hepsi doğru rünlerle tamamlandı mı?
        bool allCorrectRunesPlaced = true;
        foreach (var slot in runeSlots)
        {
            // Artık slot.isCompleted yerine slot.IsCorrectRunePlaced kontrolü yapıyoruz.
            // Bu, slota bir rün konulsa bile doğru rün değilse puzzle'ı tamamlamayacak.
            if (!slot.IsCorrectRunePlaced)
            {
                allCorrectRunesPlaced = false;
                break;
            }
        }

        if (allCorrectRunesPlaced && !allRunesCompleted)
        {
            allRunesCompleted = true;
            Debug.Log("All correct runes placed!");
            
            // Eğer lever atlanacaksa direkt kapıları aç
            if (skipLeversIfAllRunesCompleted)
            {
                Debug.Log("Skip levers enabled - opening doors immediately!");
                photonView.RPC("OpenDoorsRPC", RpcTarget.All);
            }
            // Lever sistemi aktifse
            else if (levers != null && levers.Length > 0)
            {
                photonView.RPC("ActivateAllLevers", RpcTarget.All);
                Debug.Log($"Activate {requiredLeversCount} out of {levers.Length} levers to open the doors.");
            }
            // Lever yoksa eski sistem gibi direkt aç
            else
            {
                Debug.LogWarning("No levers found! Doors will open immediately.");
                photonView.RPC("OpenDoorsRPC", RpcTarget.All);
            }
        }
        else if (!allCorrectRunesPlaced && allRunesCompleted)
        {
            // Rünlerden biri çıkarıldı, sistemi resetle
            allRunesCompleted = false;
            activatedLeversCount = 0;
            
            if (levers != null && levers.Length > 0)
            {
                photonView.RPC("DeactivateAllLevers", RpcTarget.All);
            }
            Debug.Log("Rune puzzle incomplete. All levers deactivated.");
        }
    }

    [PunRPC]
    private void ActivateAllLevers()
    {
        if (levers != null && levers.Length > 0)
        {
            foreach (var lever in levers)
            {
                if (lever != null)
                    lever.SetCanBeActivated(true);
            }
        }
    }

    [PunRPC]
    private void DeactivateAllLevers()
    {
        if (levers != null && levers.Length > 0)
        {
            foreach (var lever in levers)
            {
                if (lever != null)
                    lever.SetCanBeActivated(false);
            }
        }
    }

    public void OnLeverActivated()
    {
        // Sadece MasterClient sayacı artırsın
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("OnLeverActivatedRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void OnLeverActivatedRPC()
    {
        // Lever aktive edildiğinde bu metod çağrılır
        if (!allRunesCompleted)
        {
            Debug.Log("Runes are not completed yet!");
            return;
        }
        
        activatedLeversCount++;
        Debug.Log($"Lever activated! Progress: {activatedLeversCount}/{requiredLeversCount}");
        
        if (activatedLeversCount >= requiredLeversCount)
        {
            Debug.Log("All required levers activated! Opening doors...");
            photonView.RPC("OpenDoorsRPC", RpcTarget.All);
        }
        else
        {
            int remaining = requiredLeversCount - activatedLeversCount;
            Debug.Log($"Need {remaining} more lever(s) to open the doors.");
        }
    }

    [PunRPC]
    private void OpenDoorsRPC()
    {
        OpenDoors();
    }

    private void OpenDoors()
    {
        // Tüm kapıları açıyoruz - artık sahiplik kontrolü yok çünkü RPC ile tüm clientlarda çalışacak
        if (doorsToOpen != null && doorsToOpen.Length > 0)
        {
            bool anyDoorOpened = false;
            foreach (var door in doorsToOpen)
            {
                if (door != null)
                {
                    try
                    {
                        // Kapının PhotonView'ının sahibi olduğumuzdan emin ol
                        if (door.photonView != null && !door.photonView.IsMine)
                        {
                            door.photonView.RequestOwnership();
                        }
                        
                        door.OpenDoor(); // Kapıyı aç
                        anyDoorOpened = true;
                    }
                    catch (System.NullReferenceException e)
                    {
                        Debug.LogError($"Error opening door: {e.Message}\nThis door object may be missing components required by KeyDoorExample.OpenDoor()");
                    }
                }
                else
                {
                    Debug.LogWarning("Null door reference in doorsToOpen array!");
                }
            }
            
            if (!anyDoorOpened)
            {
                Debug.LogError("Failed to open any doors. Check door references and components!");
            }
        }
        else
        {
            Debug.LogWarning("No doors assigned to open!");
        }
    }

    // Ek faydalı metodlar
    public void ResetAllLevers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ResetAllLeversRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void ResetAllLeversRPC()
    {
        activatedLeversCount = 0;
        if (levers != null)
        {
            foreach (var lever in levers)
            {
                if (lever != null)
                {
                    lever.ResetLever(); // LeverController'da bu metodu da ekleyeceğiz
                }
            }
        }
        Debug.Log("All levers reset!");
    }

    public string GetLeverProgress()
    {
        return $"{activatedLeversCount}/{requiredLeversCount} levers activated";
    }

    public bool AreAllRequiredLeversActivated()
    {
        return activatedLeversCount >= requiredLeversCount;
    }

    public int GetRemainingLeversCount()
    {
        return Mathf.Max(0, requiredLeversCount - activatedLeversCount);
    }

    // Debug metodu - aktivasyon durumunu kontrol et
    [ContextMenu("Debug Puzzle State")]
    public void DebugPuzzleState()
    {
        Debug.Log($"=== PUZZLE DEBUG INFO ===");
        Debug.Log($"All Runes Completed: {allRunesCompleted}");
        Debug.Log($"Skip Levers If All Runes Completed: {skipLeversIfAllRunesCompleted}");
        Debug.Log($"Activated Levers Count: {activatedLeversCount}");
        Debug.Log($"Required Levers Count: {requiredLeversCount}");
        Debug.Log($"Is Master Client: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"Photon View ID: {photonView?.ViewID}");
        
        if (levers != null)
        {
            Debug.Log($"Total Levers Found: {levers.Length}");
            for (int i = 0; i < levers.Length; i++)
            {
                if (levers[i] != null)
                {
                    Debug.Log($"Lever {i}: Activated={levers[i].IsActivated}, CanActivate={levers[i].CanBeActivated}");
                }
                else
                {
                    Debug.Log($"Lever {i}: NULL");
                }
            }
        }
        else
        {
            Debug.Log("Levers array is NULL");
        }
        
        if (runeSlots != null)
        {
            Debug.Log($"Total Rune Slots: {runeSlots.Length}");
            for (int i = 0; i < runeSlots.Length; i++)
            {
                if (runeSlots[i] != null)
                {
                    Debug.Log($"Rune Slot {i}: Correct Rune Placed={runeSlots[i].IsCorrectRunePlaced}");
                }
                else
                {
                    Debug.Log($"Rune Slot {i}: NULL");
                }
            }
        }
        else
        {
            Debug.Log("Rune Slots array is NULL");
        }
        Debug.Log($"========================");
    }

    // IPunObservable implementation - durumları ağ üzerinden senkronize et
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Sadece MasterClient veri göndersin
            if (PhotonNetwork.IsMasterClient)
            {
                stream.SendNext(allRunesCompleted);
                stream.SendNext(activatedLeversCount);
            }
        }
        else
        {
            // Diğer clientlar veri alsın
            if (!PhotonNetwork.IsMasterClient)
            {
                allRunesCompleted = (bool)stream.ReceiveNext();
                activatedLeversCount = (int)stream.ReceiveNext();
            }
        }
    }
}