using System.Collections;
using UnityEngine;
using Photon.Pun;

public class TableDisplay : MonoBehaviourPun
{
    public MeshRenderer tableRenderer;
    public MeshRenderer[] symbolRenderers;

    private Vector3 targetPos;
    private bool moveUp = false;
    public float moveSpeed = 2f;

    public GameObject tableDoor;

    private TableData tableData;

    public Vector3 targetAngle = new Vector3(0,0,0);

    public float duration = 2f;
    void Update()
    {
        if (moveUp)
        {
            // transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            StartCoroutine(rotateTableDoor());
        }
    }

    public void Setup(TableData data)
    {
        tableRenderer.material.mainTexture = data.tableTexture;
        for (int i = 0; i < symbolRenderers.Length; i++)
        {
            symbolRenderers[i].material.mainTexture = data.symbolTextures[i];
        }

        tableData = data;
    }

    [PunRPC]
    public void RPC_SetupTable(int tableIndex)
    {
        TableData data = Resources.LoadAll<TableData>("Tables")[tableIndex];
        Setup(data);
    }

    public void TriggerMoveUp(float height = 6f)
    {
        // targetPos = transform.position + Vector3.up * height;
        moveUp = true;
    }

    public TableData GetTableData()
    {
        return tableData;
    }

    IEnumerator rotateTableDoor()
    {
        Quaternion startRotation = tableDoor.transform.localRotation;
        Quaternion endRotation = Quaternion.Euler(targetAngle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            tableDoor.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tableDoor.transform.localRotation = endRotation;
    }
}
