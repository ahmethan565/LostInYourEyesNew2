using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.InputSystem;
using System.Collections;

public class Lever1Script : MonoBehaviourPunCallbacks
{
    bool playerInside = false;
    public KeyCode leverKey = KeyCode.E;

    public GameObject lever;

    public Vector3 targetAngle = new Vector3(0,0,0);
    public float duration = 2f;

    private AudioSource audioSource;
    public AudioClip leverSound;

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
    void Update()
    {
        if (Input.GetKeyDown(leverKey) && playerInside)
        {
            sendDeskOpenInfo();
            if(audioSource != null && leverSound != null)
            {
                audioSource.PlayOneShot(leverSound);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if ((other.GetComponent<clientPlayerU>() != null) || (other.GetComponent<hostPlayerU>() != null))
        {
            playerInside = true;
        }
    }

    void OTriggerExit(Collider other)
    {
        if ((other.GetComponent<clientPlayerU>() != null) || (other.GetComponent<hostPlayerU>() != null))
        {
            playerInside = false;
        }
    }

    void sendDeskOpenInfo()
    {
        //Debug.Log("start");
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { "leverWorked", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        // Debug.Log("finish");
        StartCoroutine(rotateLever());

    }

    IEnumerator rotateLever()
    {
        Quaternion startRotation = lever.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(targetAngle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            lever.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lever.transform.rotation = endRotation;
    }
}
