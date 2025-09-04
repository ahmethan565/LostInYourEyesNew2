using Photon.Pun;
using System.Diagnostics;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class TableReceiver : MonoBehaviour, IInteractable
{
    public static TableReceiver Instance;

    public MeshRenderer tableRenderer;
    public Transform[] symbolSlots;
    private Texture[] placedSymbols = new Texture[3];
    private GameObject[] placedSymbolObjects = new GameObject[3];
    private int currentSlotIndex = 0;

    public GameObject symbolVisualPrefab;

    public GameObject solvedRunPrefab;

    public Transform runPosition;

    public float openSpeed = 1f;

    public catacombPuzzleManager puzzleManager;

    private bool isOpening = false;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private Vector3 selectedTableTransform;
    public GameObject selectedTableRun;

    private TableDisplay selectedDisplay;

    public DoorOnlyLift door1;
    public DoorOnlyLift door2;

    public GameObject tableDoor;

    public float duration = 2f;
    public Vector3 targetAngle = new Vector3(0,0,0);
    public Vector3 localOffset = new Vector3(0,0,0);
    public Quaternion selectedTableRotation;

    private bool catacombSolved = false;
    private AudioSource audioSource;

    [Header("sound sets")]
    public AudioClip catacomPuzzleSound;
    public AudioClip runePlaceSound;


    void Awake()
    {
        Instance = this;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D ses için
        }
    }

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + new Vector3(0f, 6f, 0f);
    }

    void Update()
    {
        if (isOpening)
        {
            StartCoroutine(rotateTableDoor());
        }
    }

    public void ShowSelectedTable(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        catacombPuzzleChecker.Instance.SetCorrectSymbols(data.correctTextures);
        // selectedTableTransform = data.tableTransform.position + data.tableTransform.forward * 1.2f;
        // selectedTableTransform -= new Vector3(0f, 6f, 0f);
        selectedTableTransform = puzzleManager.spawnedTableTransforms[puzzleManager.selectedTableIndex].transform.TransformPoint(localOffset);
        selectedTableTransform += new Vector3(0f, -5.4f, 0f);
        selectedTableRotation = puzzleManager.spawnedTableTransforms[puzzleManager.selectedTableIndex].transform.rotation;
        selectedTableRotation *= Quaternion.Euler(0,180,0);

        TableDisplay[] displays = FindObjectsByType<TableDisplay>(FindObjectsSortMode.None);
        foreach (var display in displays)
            if (display.GetTableData() == data)
            {
                selectedDisplay = display;
                break;
            }
    }

    public bool TryPlaceSymbol(Texture symbolTexture)
    {
        if (currentSlotIndex >= symbolSlots.Length)
        {
            UnityEngine.Debug.Log("Tüm yuvalar dolu gardeşim");
            return false;
        }

        Transform parentTransform = symbolSlots[currentSlotIndex]; // Ya da direkt tablo objesi
        GameObject placed = Instantiate(symbolVisualPrefab, parentTransform.position, parentTransform.rotation, parentTransform);

        placed.GetComponentInChildren<MeshRenderer>().material.mainTexture = symbolTexture;

        if (audioSource != null && runePlaceSound != null)
        {
            audioSource.PlayOneShot(runePlaceSound);
        }

        placedSymbols[currentSlotIndex] = symbolTexture;
        placedSymbolObjects[currentSlotIndex] = placed;
        currentSlotIndex++;

        if (currentSlotIndex == symbolSlots.Length)
        {
            bool result = catacombPuzzleChecker.Instance.Check(placedSymbols);
            UnityEngine.Debug.Log(result ? "doğru yerleştirdin" : "yanlış yerleştirdin");

            if (result && !catacombSolved)
            {
                PhotonNetwork.Instantiate("HostSolvedRunPrefab", runPosition.position, UnityEngine.Quaternion.identity);
                isOpening = true;

                PhotonNetwork.Instantiate("ClientSolvedRunPrefab", selectedTableTransform, selectedTableRotation);
                PhotonNetwork.Instantiate("tableDoorSound", selectedTableTransform, UnityEngine.Quaternion.identity);
                UnityEngine.Debug.Log("Seçilen tablonun arkasına obje yerleştiridli");
                if (selectedDisplay != null)
                {
                    selectedDisplay.TriggerMoveUp();
                }

                door1.ToggleDoor();
                door2.ToggleDoor();

                catacombSolved = true;

                if (audioSource != null && catacomPuzzleSound != null)
                {
                    audioSource.PlayOneShot(catacomPuzzleSound);
                }
            }
        }

        return true;
    }

    public bool CanUndo()
    {
        return currentSlotIndex > 0;
    }

    public Texture UndoLastPlacement()
    {
        if (currentSlotIndex <= 0) return null;

        currentSlotIndex--;
        Destroy(placedSymbolObjects[currentSlotIndex]);
        Texture symbol = placedSymbols[currentSlotIndex];
        placedSymbols[currentSlotIndex] = null;
        placedSymbolObjects[currentSlotIndex] = null;

        return symbol;
    }

    // IInteractable Implementation
    public void Interact()
    {
        // Handle basic interaction with the table
        // For symbol placement, players should use C key (handled in InteractionManager)
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingSymbol())
        {
            // If holding a symbol, suggest using C key
            UnityEngine.Debug.Log("Use C key to place symbol on table");
        }
        else if (CanUndo())
        {
            // If can undo, suggest using R key
            UnityEngine.Debug.Log("Use R key to retrieve last placed symbol");
        }
    }

    public void InteractWithItem(GameObject heldItemGameObject)
    {
        // Check if the held item is a symbol
        SymbolObject symbolObject = heldItemGameObject.GetComponent<SymbolObject>();
        if (symbolObject != null)
        {
            // Try to place the symbol
            Texture symbolTexture = symbolObject.GetTexture();
            if (symbolTexture != null)
            {
                bool placed = TryPlaceSymbol(symbolTexture);
                if (placed)
                {
                    // Consume the symbol from inventory
                    InventorySystem.Instance.ConsumeHeldItem();
                    UnityEngine.Debug.Log("Symbol placed on table via item interaction");
                }
            }
        }
    }

    public string GetInteractText()
    {
        // Check current state and provide appropriate text
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsHoldingSymbol())
        {
            if (currentSlotIndex >= symbolSlots.Length)
            {
                return "Table Full";
            }
            return "Place Symbol (C key)";
        }
        else if (CanUndo())
        {
            return "Retrieve Symbol (R key)";
        }
        else if (currentSlotIndex == 0)
        {
            return "Place symbols here";
        }
        else if (currentSlotIndex >= symbolSlots.Length)
        {
            return "Puzzle Complete";
        }
        else
        {
            return $"Symbols placed: {currentSlotIndex}/{symbolSlots.Length}";
        }
    }
    
    IEnumerator rotateTableDoor()
    {
        Quaternion startRotation = tableDoor.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(targetAngle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            tableDoor.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tableDoor.transform.rotation = endRotation;
    }

    // public bool Check()
    // {
    //     Texture[] userInput = new Texture[symbolSlots.Length];
    //     for (int i = 0; i < symbolSlots.Length; i++)
    //     {
    //         userInput[i] = symbolSlots[i].GetSymbol();
    //     }

    //     return catacombPuzzleChecker.Instance.Check(userInput);
    // }        
}
