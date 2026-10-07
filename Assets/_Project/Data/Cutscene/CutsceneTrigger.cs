using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CutsceneTrigger : MonoBehaviour
{
    [Tooltip("Masukkan file data cutscene (ScriptableObject) ke sini")]
    public CutsceneSequence cutsceneToPlay;
    
    [Tooltip("Apakah cutscene ini hanya bisa dimainkan sekali?")]
    public bool playOnlyOnce = true;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && playOnlyOnce) return;

        // Cek apakah yang menabrak adalah Player
        if (other.CompareTag("Player"))
        {
            if (cutsceneToPlay != null)
            {
                hasTriggered = true;
                SequenceManager.Instance.StartCutscene(cutsceneToPlay);
            }
            else
            {
                Debug.LogWarning("Naskah cutscene belum dimasukkan ke Trigger!");
            }
        }
    }
}