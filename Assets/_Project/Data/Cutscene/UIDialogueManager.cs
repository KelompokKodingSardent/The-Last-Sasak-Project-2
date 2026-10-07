using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // <-- Tambahan Wajib untuk New Input System

public class UIDialogueManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject dialogueCanvas;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;

    [Header("VN Sprites Slots")]
    public Image slotLeft;
    public Image slotCenter;
    public Image slotRight;

    [Header("Settings")]
    public float typingSpeed = 0.02f;
    public Color colorTalking = Color.white;
    public Color colorIdle = new Color(0.5f, 0.5f, 0.5f, 1f);

    public static Action OnDialogueFinished;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentFullText = "";

    private void Awake()
    {
        dialogueCanvas.SetActive(false);
    }

    private void Update()
    {
        // Deteksi Input menggunakan NEW INPUT SYSTEM
        bool isNextPressed = false;

        // Cek Spasi
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isNextPressed = true;
        }
        // Cek Klik Kiri Mouse
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isNextPressed = true;
        }

        // Jika tombol ditekan dan Canvas sedang aktif
        if (dialogueCanvas.activeSelf && isNextPressed)
        {
            if (isTyping)
            {
                CompleteDialogueText();
            }
            else
            {
                OnDialogueFinished?.Invoke(); // Lanjut ke dialog/step berikutnya
            }
        }
    }

    public void PlayDialogue(CutsceneAction actionData)
    {
        dialogueCanvas.SetActive(true);

        if (string.IsNullOrEmpty(actionData.speakerName))
        {
            speakerNameText.text = "";
            speakerNameText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            speakerNameText.transform.parent.gameObject.SetActive(true);
            speakerNameText.text = actionData.speakerName;
        }

        UpdateVNSprites(actionData.actorCommands);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        currentFullText = actionData.dialogueText;
        typingCoroutine = StartCoroutine(TypeText(currentFullText));
    }

    public void HideDialogue()
    {
        dialogueCanvas.SetActive(false);
    }

    private void UpdateVNSprites(List<VNActorCommand> actors)
    {
        ResetSlot(slotLeft);
        ResetSlot(slotCenter);
        ResetSlot(slotRight);

        foreach (var actor in actors)
        {
            Image targetSlot = null;
            switch (actor.slot)
            {
                case VNSlot.Left: targetSlot = slotLeft; break;
                case VNSlot.Center: targetSlot = slotCenter; break;
                case VNSlot.Right: targetSlot = slotRight; break;
            }

            if (targetSlot != null && actor.expression != null)
            {
                targetSlot.sprite = actor.expression;
                targetSlot.color = actor.isTalking ? colorTalking : colorIdle;
                targetSlot.gameObject.SetActive(true);
            }
        }
    }

    private void ResetSlot(Image slot)
    {
        slot.sprite = null;
        slot.gameObject.SetActive(false);
    }

    private IEnumerator TypeText(string textToType)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in textToType.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteDialogueText()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.text = currentFullText;
        isTyping = false;
    }
}