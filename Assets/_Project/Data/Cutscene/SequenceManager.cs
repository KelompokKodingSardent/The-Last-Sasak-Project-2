using System.Collections;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance;

    [Header("Managers Reference")]
    public UIDialogueManager dialogueManager;
    public PlayerMovement playerMovement; // Masukkan referensi player ke sini

    private bool isWaitingForDialogue = false;

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Mendengarkan sinyal dari UIDialogueManager saat player klik Next
        UIDialogueManager.OnDialogueFinished += ResumeFromDialogue;
    }

    private void OnDisable()
    {
        UIDialogueManager.OnDialogueFinished -= ResumeFromDialogue;
    }

    private void ResumeFromDialogue()
    {
        isWaitingForDialogue = false;
    }

    // Dipanggil oleh objek Trigger di dalam game
    public void StartCutscene(CutsceneSequence sequence)
    {
        StartCoroutine(RunSequence(sequence));
    }

    private IEnumerator RunSequence(CutsceneSequence sequence)
    {
        // 1. Kunci Pergerakan Player saat cutscene mulai
        if (playerMovement != null) playerMovement.SetMovementState(false);

        // 2. Baca naskah baris per baris (Looping step)
        foreach (CutsceneAction step in sequence.sequenceSteps)
        {
            switch (step.actionType)
            {
                case CutsceneActionType.SwitchCamera:
                    CameraEventManager.TriggerSwitchCamera(step.cameraID);
                    if (step.waitForCompletion)
                    {
                        yield return new WaitForSeconds(step.waitTime);
                    }
                    break;

                case CutsceneActionType.PlayDialogue:
                    isWaitingForDialogue = true;
                    dialogueManager.PlayDialogue(step);
                    
                    if (step.waitForCompletion)
                    {
                        // Tunggu (Pause urutan) sampai player menekan spasi/klik
                        yield return new WaitUntil(() => !isWaitingForDialogue);
                    }
                    break;

                case CutsceneActionType.WaitDuration:
                    if (step.waitForCompletion)
                    {
                        yield return new WaitForSeconds(step.waitTime);
                    }
                    break;

                case CutsceneActionType.EndCutscene:
                    goto EndSequence; // Lompat ke bagian akhir
            }
        }

        EndSequence:
        // 3. Selesai! Kembalikan semuanya ke kondisi normal
        dialogueManager.HideDialogue();
        CameraEventManager.TriggerSwitchCamera("Player"); // Otomatis balik ke kamera player

        if (playerMovement != null) playerMovement.SetMovementState(true);
    }
}