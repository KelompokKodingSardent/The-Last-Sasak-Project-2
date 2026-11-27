using System.Collections;
using TMPro;
using UnityEngine;

namespace TheLastSasak.UI.Inventory
{
    public class InventoryFeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        [Header("Colors & Timing")]
        [SerializeField] private Color infoColor = new Color(0.92f, 0.90f, 0.84f, 1f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.42f, 0.35f, 1f);
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 0.35f;

        private Coroutine _activeRoutine;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void ShowInfo(string message)
        {
            ShowMessage(message, infoColor);
        }

        public void ShowWarning(string message)
        {
            ShowMessage(message, warningColor);
        }

        private void ShowMessage(string message, Color color)
        {
            if (messageText == null || canvasGroup == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
            }

            messageText.text = message;
            messageText.color = color;
            _activeRoutine = StartCoroutine(AnimateToast());
        }

        private IEnumerator AnimateToast()
        {
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(displayDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            _activeRoutine = null;
        }
    }
}