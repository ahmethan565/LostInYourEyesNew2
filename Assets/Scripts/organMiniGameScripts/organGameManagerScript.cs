using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;
using DG.Tweening;

public class organGameManagerScript : MonoBehaviourPunCallbacks
{
    public static organGameManagerScript Instance;
    Vector3 position = new Vector3(-28, 3, 148);

    [Header("Puzzle Reward Chests - Runtime References")]
    private Transform chest1Lid; // İlk sandığın kapağı
    private Transform chest2Lid; // İkinci sandığın kapağı
    private GameObject rune1; // İlk rün
    private GameObject rune2; // İkinci rün

    [Header("Object Names/Tags for Finding")]
    public string chest1LidName = "Chest1_Lid"; // Sandık kapağı 1'in adı
    public string chest2LidName = "Chest2_Lid"; // Sandık kapağı 2'nin adı
    public string rune1Name = "Rune1"; // Rün 1'in adı
    public string rune2Name = "Rune2"; // Rün 2'nin adı

    [Header("Alternative: Use Tags Instead of Names")]
    public bool useTagsInsteadOfNames = false;
    public string chest1LidTag = "Chest1Lid";
    public string chest2LidTag = "Chest2Lid";
    public string rune1Tag = "Rune1";
    public string rune2Tag = "Rune2";

    [Header("Animation Settings")]
    public float chestOpenDuration = 1f;
    public float runeRiseDuration = 2f;
    public float runeRotationSpeed = 180f;

    public organCapsuleTriggerScript[] organCapsuleTriggers;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Runtime'da referansları bul
        FindChestAndRuneReferences();
        // photonView.RPC("PuzzleSolved", RpcTarget.All); test için
        organCapsuleTriggers = FindObjectsByType<organCapsuleTriggerScript>(FindObjectsSortMode.None);
    }

    private void FindChestAndRuneReferences()
    {
        if (useTagsInsteadOfNames)
        {
            // Tag ile bul
            FindObjectsByTags();
        }
        else
        {
            // İsim ile bul
            FindObjectsByNames();
        }
    }

    private void FindObjectsByNames()
    {
        // Sandık kapaklarını bul
        GameObject chest1LidObj = GameObject.Find(chest1LidName);
        if (chest1LidObj != null)
            chest1Lid = chest1LidObj.transform;
        else
            Debug.LogWarning($"Chest1 Lid with name '{chest1LidName}' not found!");

        GameObject chest2LidObj = GameObject.Find(chest2LidName);
        if (chest2LidObj != null)
            chest2Lid = chest2LidObj.transform;
        else
            Debug.LogWarning($"Chest2 Lid with name '{chest2LidName}' not found!");

        // Rünleri bul
        rune1 = GameObject.Find(rune1Name);
        if (rune1 == null)
            Debug.LogWarning($"Rune1 with name '{rune1Name}' not found!");

        rune2 = GameObject.Find(rune2Name);
        if (rune2 == null)
            Debug.LogWarning($"Rune2 with name '{rune2Name}' not found!");
    }

    private void FindObjectsByTags()
    {
        // Sandık kapaklarını bul
        GameObject chest1LidObj = GameObject.FindWithTag(chest1LidTag);
        if (chest1LidObj != null)
            chest1Lid = chest1LidObj.transform;
        else
            Debug.LogWarning($"Chest1 Lid with tag '{chest1LidTag}' not found!");

        GameObject chest2LidObj = GameObject.FindWithTag(chest2LidTag);
        if (chest2LidObj != null)
            chest2Lid = chest2LidObj.transform;
        else
            Debug.LogWarning($"Chest2 Lid with tag '{chest2LidTag}' not found!");

        // Rünleri bul
        rune1 = GameObject.FindWithTag(rune1Tag);
        if (rune1 == null)
            Debug.LogWarning($"Rune1 with tag '{rune1Tag}' not found!");

        rune2 = GameObject.FindWithTag(rune2Tag);
        if (rune2 == null)
            Debug.LogWarning($"Rune2 with tag '{rune2Tag}' not found!");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Reached400"))
        {
            CheckIfBothPlayersReached400();
        }
    }

    private void CheckIfBothPlayersReached400()
    {
        bool allReached = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            //if (!player.CustomProperties.TryGetValue("Reached400", out object value) || !(bool)value)
            if (!player.CustomProperties.TryGetValue("Reached400", out object value) || !(value is bool reached && reached))
            {
                allReached = false;
                break;
            }
        }

        if (allReached)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("PuzzleSolved", RpcTarget.All);
                foreach (organCapsuleTriggerScript organCapsuleTrigger in organCapsuleTriggers)
                {
                    PhotonNetwork.Destroy(organCapsuleTrigger.gameObject);
                }
            }
        }
    }

    [PunRPC]
    void PuzzleSolved()
    {
        Debug.Log("Both two players reached 400 points. puzzle solved.");
        // PhotonNetwork.Instantiate("createPoint", position, Quaternion.identity);

        // Sandıkları aç ve rünleri çıkar
        OpenChestsAndRevealRunes();
        
    }

    private void OpenChestsAndRevealRunes()
    {
        // Eğer referanslar null ise tekrar bulmaya çalış
        if (chest1Lid == null || chest2Lid == null || rune1 == null || rune2 == null)
        {
            Debug.Log("Some references are null, trying to find them again...");
            FindChestAndRuneReferences();
        }

        // Sandık kapaklarını açma animasyonu (90 derece döndür)
        if (chest1Lid != null)
        {
            chest1Lid.DOLocalRotate(new Vector3(-180, 0, 0), chestOpenDuration)
                .SetEase(Ease.OutBounce);
        }
        else
        {
            Debug.LogError("Chest1 Lid is still null! Cannot open chest.");
        }

        if (chest2Lid != null)
        {
            chest2Lid.DOLocalRotate(new Vector3(-180, 0, 0), chestOpenDuration)
                .SetEase(Ease.OutBounce);
        }
        else
        {
            Debug.LogError("Chest2 Lid is still null! Cannot open chest.");
        }

        // Sandık açıldıktan sonra rünleri çıkar
        DOVirtual.DelayedCall(chestOpenDuration * 0.5f, () =>
        {
            RevealRune(rune1);
            RevealRune(rune2);
        });
    }

    private void RevealRune(GameObject rune)
    {
        if (rune == null) return;

        // Rünü aktif et
        rune.transform.GetChild(0).gameObject.SetActive(true);

        // Başlangıç pozisyonunu kaydet
        Vector3 startPos = rune.transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.5f; // 0.5 birim yukarı çık

        // Başlangıçta rünü biraz aşağı yerleştir
        rune.transform.position = startPos - Vector3.up * 0.5f;

        // Rün çıkma animasyonu
        DG.Tweening.Sequence runeSequence = DOTween.Sequence();

        // Yukarı çıkma animasyonu
        runeSequence.Append(rune.transform.DOMoveY(endPos.y, runeRiseDuration)
            .SetEase(Ease.OutQuart));

        // Döndürme animasyonu (sürekli)
        rune.transform.DORotate(new Vector3(0, 360, 0), runeRotationSpeed / 60f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);

        // Parıltı efekti (scale animasyonu)
        runeSequence.Join(rune.transform.DOScale(Vector3.one * 0.5f, 0.5f)
            .SetLoops(6, LoopType.Yoyo)
            .SetEase(Ease.InOutSine));
    }
}
