using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectName.UI;

/// <summary>
/// Canvas overlay for cutscene dialogue display.
/// Handles typewriter text effect, semi-transparent background, and skip prompt.
/// </summary>
public class CutsceneUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private Image borderFrame;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text skipPromptText;

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color borderColor = new Color(0.812f, 0.710f, 0.231f, 1f);   // agedGold
    [SerializeField] private Color textColor = new Color(0.961f, 0.961f, 0.863f, 1f);      // boneWhite

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 30f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Coroutine typewriterCoroutine;
    private bool typewriterComplete;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Apply style guide colors if available
        if (UIManager.Instance != null && UIManager.Instance.StyleGuide != null)
        {
            UIStyleGuide style = UIManager.Instance.StyleGuide;
            borderColor = style.agedGold;
            textColor = style.boneWhite;
        }

        ApplyColors();
        Hide();
    }

    private void ApplyColors()
    {
        if (backgroundPanel != null)
            backgroundPanel.color = backgroundColor;
        if (borderFrame != null)
            borderFrame.color = borderColor;
        if (dialogueText != null)
        {
            dialogueText.color = textColor;
            FontManager.EnsureFont(dialogueText);
        }
        if (skipPromptText != null)
        {
            skipPromptText.color = new Color(textColor.r, textColor.g, textColor.b, 0.5f);
            FontManager.EnsureFont(skipPromptText);
        }
    }

    /// <summary>
    /// Shows the cutscene UI with a fade-in.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));
        }
    }

    /// <summary>
    /// Hides the cutscene UI with a fade-out.
    /// </summary>
    public void Hide()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Fades out the UI, then disables it.
    /// </summary>
    public IEnumerator FadeOutAndHide()
    {
        if (canvasGroup != null)
        {
            yield return FadeCanvasGroup(canvasGroup.alpha, 0f, fadeOutDuration);
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays text with a typewriter effect. Call from a coroutine.
    /// Completes when all characters are shown.
    /// </summary>
    public IEnumerator ShowDialogue(string text)
    {
        if (dialogueText == null)
            yield break;

        typewriterComplete = false;
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        int totalChars = text.Length;
        float elapsed = 0f;

        while (dialogueText.maxVisibleCharacters < totalChars)
        {
            elapsed += Time.deltaTime;
            int visibleChars = Mathf.FloorToInt(elapsed * charactersPerSecond);
            dialogueText.maxVisibleCharacters = Mathf.Min(visibleChars, totalChars);
            yield return null;
        }

        dialogueText.maxVisibleCharacters = totalChars;
        typewriterComplete = true;
    }

    /// <summary>
    /// Immediately reveals all dialogue text, skipping the typewriter effect.
    /// </summary>
    public void CompleteTypewriter()
    {
        if (dialogueText != null)
        {
            dialogueText.maxVisibleCharacters = dialogueText.text.Length;
        }
        typewriterComplete = true;
    }

    /// <summary>
    /// Whether the typewriter effect has finished showing all characters.
    /// </summary>
    public bool IsTypewriterComplete => typewriterComplete;

    /// <summary>
    /// Shows or hides the skip prompt text.
    /// </summary>
    public void SetSkipPromptVisible(bool visible)
    {
        if (skipPromptText != null)
        {
            skipPromptText.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Updates the skip prompt text (e.g., "Hold Space to skip...").
    /// </summary>
    public void SetSkipPromptText(string text)
    {
        if (skipPromptText != null)
        {
            skipPromptText.text = text;
        }
    }

    /// <summary>
    /// Clears the dialogue text.
    /// </summary>
    public void ClearDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 0;
        }
        typewriterComplete = false;
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
