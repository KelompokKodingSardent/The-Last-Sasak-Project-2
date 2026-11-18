using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class UIDialogueManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private GameObject speakerNameBox;
    [SerializeField] private TMP_Text dialogueText;

    [Header("VN Sprites Slots")]
    [SerializeField] private Image slotLeft;
    [SerializeField] private Image slotCenter;
    [SerializeField] private Image slotRight;

    [Header("Settings & Speed")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float punctuationPauseMultiplier = 3.5f;
    [SerializeField] private float transitionDuration = 0.25f;

    [Header("Cinematic Styling")]
    [SerializeField] private Color colorTalking = Color.white;
    [SerializeField] private Color colorIdle = new Color(0.4f, 0.4f, 0.45f, 0.85f);
    [SerializeField] private Vector3 talkingScale = new Vector3(1.05f, 1.05f, 1f);
    [SerializeField] private Vector3 idleScale = Vector3.one;

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    public static Action OnDialogueFinished;

    private Coroutine typingCoroutine;
    private Coroutine fadeCoroutine;
    private Dictionary<Image, Coroutine> activeSpriteCoroutines = new Dictionary<Image, Coroutine>();

    private bool isTyping = false;
    private string currentFullText = "";
    private int totalVisibleCharacters = 0;

    private void Awake()
    {
        if (dialogueCanvasGroup == null && dialogueCanvas != null)
        {
            dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
                dialogueCanvasGroup = dialogueCanvas.AddComponent<CanvasGroup>();
        }

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            dialogueCanvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (!dialogueCanvas.activeSelf) return;

        bool isNextPressed = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            isNextPressed = true;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            isNextPressed = true;

        if (isNextPressed)
        {
            if (isTyping)
            {
                CompleteDialogueText();
            }
            else
            {
                OnDialogueFinished?.Invoke();
            }
        }
    }

    public void PlayDialogue(CutsceneAction actionData)
    {
        if (!dialogueCanvas.activeSelf)
        {
            dialogueCanvas.SetActive(true);
            FadeCanvasGroup(1f);
        }

        // Setup Speaker Name
        if (string.IsNullOrEmpty(actionData.speakerName))
        {
            if (speakerNameText != null) speakerNameText.text = "";
            if (speakerNameBox != null) speakerNameBox.SetActive(false);
        }
        else
        {
            if (speakerNameBox != null) speakerNameBox.SetActive(true);
            if (speakerNameText != null) speakerNameText.text = actionData.speakerName;
        }

        // Update Characters Sprites & Focus
        UpdateVNSprites(actionData.actorCommands);

        // Typewriter Effect
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        currentFullText = actionData.dialogueText;
        typingCoroutine = StartCoroutine(TypeTextCinematic(currentFullText));
    }

    public void HideDialogue()
    {
        FadeCanvasGroup(0f, () =>
        {
            dialogueCanvas.SetActive(false);
        });
    }

private void UpdateVNSprites(List<VNActorCommand> actors)
{
    HashSet<Image> usedSlots = new HashSet<Image>();

    if (actors != null)
    {
        foreach (var actor in actors)
        {
            Image targetSlot = GetSlotByEnum(actor.slot);
            if (targetSlot == null) continue;

            usedSlots.Add(targetSlot);

            if (actor.expression != null)
            {
                targetSlot.sprite = actor.expression;
                
                // --- TAMBAHKAN BARIS INI ---
                targetSlot.SetNativeSize(); // Mengatur ukuran Image sesuai resolusi asli Sprite
                // ---------------------------

                if (!targetSlot.gameObject.activeSelf)
                {
                    targetSlot.gameObject.SetActive(true);
                    targetSlot.color = new Color(colorIdle.r, colorIdle.g, colorIdle.b, 0f);
                }

                Color targetColor = actor.isTalking ? colorTalking : colorIdle;
                Vector3 targetScale = actor.isTalking ? talkingScale : idleScale;

                AnimateSpriteSlot(targetSlot, targetColor, targetScale, transitionDuration);
            }
            else
            {
                ResetSlotSmooth(targetSlot);
            }
        }
    }

    if (slotLeft != null && !usedSlots.Contains(slotLeft)) ResetSlotSmooth(slotLeft);
    if (slotCenter != null && !usedSlots.Contains(slotCenter)) ResetSlotSmooth(slotCenter);
    if (slotRight != null && !usedSlots.Contains(slotRight)) ResetSlotSmooth(slotRight);
}
    private Image GetSlotByEnum(VNSlot slot)
    {
        switch (slot)
        {
            case VNSlot.Left: return slotLeft;
            case VNSlot.Center: return slotCenter;
            case VNSlot.Right: return slotRight;
            default: return null;
        }
    }

    private IEnumerator TypeTextCinematic(string textToType)
    {
        isTyping = true;
        dialogueText.text = textToType;
        dialogueText.maxVisibleCharacters = 0;

        // Force render TMP untuk mendapatkan info karakter lengkap (termasuk RichText)
        dialogueText.ForceMeshUpdate();
        TMP_TextInfo textInfo = dialogueText.textInfo;
        totalVisibleCharacters = textInfo.characterCount;

        for (int visibleCount = 0; visibleCount <= totalVisibleCharacters; visibleCount++)
        {
            dialogueText.maxVisibleCharacters = visibleCount;

            if (visibleCount > 0 && visibleCount <= totalVisibleCharacters)
            {
                char lastChar = textInfo.characterInfo[visibleCount - 1].character;

                // Play Audio SFX jika ada
                if (audioSource != null && typingSound != null && char.IsLetterOrDigit(lastChar))
                {
                    audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(typingSound);
                }

                // Dynamic Pause untuk Tanda Baca
                if (lastChar == '.' || lastChar == '!' || lastChar == '?')
                {
                    yield return new WaitForSeconds(typingSpeed * punctuationPauseMultiplier);
                }
                else if (lastChar == ',' || lastChar == ';' || lastChar == ':')
                {
                    yield return new WaitForSeconds(typingSpeed * (punctuationPauseMultiplier * 0.5f));
                }
                else
                {
                    yield return new WaitForSeconds(typingSpeed);
                }
            }
        }

        isTyping = false;
    }

    private void CompleteDialogueText()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.maxVisibleCharacters = totalVisibleCharacters;
        isTyping = false;
    }

    private void AnimateSpriteSlot(Image slot, Color targetColor, Vector3 targetScale, float duration)
    {
        if (activeSpriteCoroutines.ContainsKey(slot) && activeSpriteCoroutines[slot] != null)
        {
            StopCoroutine(activeSpriteCoroutines[slot]);
        }

        activeSpriteCoroutines[slot] = StartCoroutine(AnimateSpriteRoutine(slot, targetColor, targetScale, duration));
    }

    private IEnumerator AnimateSpriteRoutine(Image slot, Color targetColor, Vector3 targetScale, float duration)
    {
        Color startColor = slot.color;
        Vector3 startScale = slot.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            slot.color = Color.Lerp(startColor, targetColor, t);
            slot.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        slot.color = targetColor;
        slot.transform.localScale = targetScale;
    }

    private void ResetSlotSmooth(Image slot)
    {
        if (slot == null || !slot.gameObject.activeSelf) return;

        AnimateSpriteSlot(slot, new Color(slot.color.r, slot.color.g, slot.color.b, 0f), idleScale, transitionDuration);
        StartCoroutine(DisableAfterDelay(slot.gameObject, transitionDuration));
    }

    private IEnumerator DisableAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    private void FadeCanvasGroup(float targetAlpha, Action onComplete = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, transitionDuration, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        float startAlpha = dialogueCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        dialogueCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}