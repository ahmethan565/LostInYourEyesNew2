using UnityEngine;
using System.Collections;
using Photon.Pun;

public class DoorOnlyLift : MonoBehaviourPun
{
    public float liftHeight = 3f;
    public float liftSpeed = 2f;

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isOpen = false;
    private bool isMoving = false;

    public Transform[] smokePoints;

    [Header ("Camera Shake Sets")]
    public float shakeDuration;
    public float shakeMagnitude;

    [Header("sound sets")]
    private AudioSource audioSource;
    public AudioClip doorSound;

    void Awake()
    {
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
        targetPosition = initialPosition + new Vector3(0, liftHeight, 0);
    }

    public void ToggleDoor()
    {
        if (!isMoving)
        {
            StartCoroutine(MoveDoor(isOpen ? targetPosition : initialPosition, isOpen ? initialPosition : targetPosition));
        }
    }

    private IEnumerator MoveDoor(Vector3 fromPos, Vector3 toPos)
    {
        isMoving = true;
        float elapsed = 0f;

        for (int i = 0; i < smokePoints.Length; i++)
        {
            PhotonNetwork.Instantiate("doorSmokePrefab", smokePoints[i].position, Quaternion.identity);
        }

        cameraShake.instance.photonView.RPC("RpcTriggerShake", RpcTarget.All, shakeDuration, shakeMagnitude);

        if(audioSource != null && doorSound != null)
        {
            audioSource.PlayOneShot(doorSound);
        }

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * liftSpeed;
            transform.position = Vector3.Lerp(fromPos, toPos, elapsed);
            yield return null;
        }

        transform.position = toPos;
        isMoving = false;
    }
}
