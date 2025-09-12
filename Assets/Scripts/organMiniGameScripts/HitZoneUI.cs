using System;
using System.Linq;
using UnityEngine;

public class HitZoneUI : MonoBehaviour
{
    public string key;

    [Header("addPoint")]
    public int addPoint = 10;

    public NoteSpawnerUI noteSpawner;

    void Update()
    {
        if (Input.anyKeyDown)
        {
            foreach (Transform child in transform.parent)
            {
                var note = child.GetComponent<Note>();
                if (note == null) continue;

                // Nota zaten işlenmiş mi kontrol et (animasyon sırasında veya miss durumunda)
                if (note.HasBeenProcessed()) continue;

                if (WasCorrectKeyPressed(note.assignedKey))
                {
                    float distance = Mathf.Abs(child.localPosition.y - transform.localPosition.y);
                    if (distance < 20f)
                    {
                        NoteSpawnerUI.Instance.AddPoints(addPoint);
                        FeedbackUIController.Instance?.ShowFeedback(Color.green, note.assignedKey);
                        note.HandleHitEffect();  // Animasyonlu hit efekti
                        if (noteSpawner.audioStart == false)
                        {
                            noteSpawner.organAudioSource.Play();
                            noteSpawner.audioStart = true;
                        }
                        break;
                    }
                }
            }
        }
    }


    bool WasCorrectKeyPressed(KeyType key)
    {
        return key switch
        {
            KeyType.W => Input.GetKeyDown(KeyCode.W),
            KeyType.A => Input.GetKeyDown(KeyCode.A),
            KeyType.S => Input.GetKeyDown(KeyCode.S),
            KeyType.D => Input.GetKeyDown(KeyCode.D),
            KeyType.Left => Input.GetKeyDown(KeyCode.LeftArrow),
            KeyType.Up => Input.GetKeyDown(KeyCode.UpArrow),
            KeyType.Right => Input.GetKeyDown(KeyCode.RightArrow),
            KeyType.Down => Input.GetKeyDown(KeyCode.DownArrow),

            _ => false
        };
    }
}