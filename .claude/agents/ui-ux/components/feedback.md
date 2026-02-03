# Feedback Systems

> **Unity 6** - Use ScriptableObjects for audio banks, AudioSource.PlayOneShot for UI sounds.

## Audio Cues

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "UISoundBank", menuName = "Audio/UI Sound Bank")]
public class UISoundBank : ScriptableObject
{
    [Header("Navigation")]
    [Tooltip("Subtle tick when moving between options")]
    public AudioClip navigate;
    [Tooltip("Deeper confirmation when selecting")]
    public AudioClip select;
    [Tooltip("Soft whoosh when canceling")]
    public AudioClip cancel;
    [Tooltip("Page turn / stone slide for tab switching")]
    public AudioClip tabSwitch;

    [Header("Feedback")]
    public AudioClip confirm;           // Satisfying click
    public AudioClip error;             // Low buzz
    public AudioClip itemPickup;        // Mystical chime
    public AudioClip menuOpen;          // Stone door / book open
    public AudioClip menuClose;         // Reverse of open

    [Header("Gothic Ambience")]
    public AudioClip backgroundDrone;   // Low cathedral reverb
    public AudioClip candleFlicker;     // Subtle fire crackle

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float navigationVolume = 0.5f;
    [Range(0f, 1f)] public float feedbackVolume = 0.7f;
    [Range(0f, 1f)] public float ambienceVolume = 0.3f;
}
```

## Visual Feedback

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.15f;

    [Header("Glow Effect")]
    [SerializeField] private Image glowImage;
    [SerializeField] private Color glowColor = new Color(0.81f, 0.71f, 0.23f, 0.5f);
    [SerializeField] private Color normalColor = new Color(0.81f, 0.71f, 0.23f, 0f);

    [Header("Click Effect")]
    [SerializeField] private ParticleSystem clickParticles;

    private Vector3 originalScale;
    private Tweener currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (glowImage != null)
            glowImage.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * hoverScale, hoverDuration)
            .SetUpdate(true);

        if (glowImage != null)
            glowImage.DOColor(glowColor, hoverDuration).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, hoverDuration)
            .SetUpdate(true);

        if (glowImage != null)
            glowImage.DOColor(normalColor, hoverDuration).SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 2, 0.5f)
            .SetUpdate(true);

        if (clickParticles != null)
            clickParticles.Play();
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}
```

## Tooltips

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class TooltipSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(20f, -20f);
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private float fadeDuration = 0.15f;

    private Coroutine showCoroutine;
    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Show(string header, string description, Vector2 screenPosition)
    {
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowAfterDelay(header, description, screenPosition));
    }

    public void Hide()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => tooltipPanel.gameObject.SetActive(false));
        }
    }

    private IEnumerator ShowAfterDelay(string header, string description, Vector2 screenPosition)
    {
        yield return new WaitForSecondsRealtime(showDelay); // RealTime for paused menus

        if (headerText != null) headerText.text = header;
        if (descriptionText != null) descriptionText.text = description;

        // Position tooltip, keeping it on screen
        Vector2 position = screenPosition + offset;
        position = ClampToScreen(position);
        tooltipPanel.position = position;

        tooltipPanel.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
    }

    private Vector2 ClampToScreen(Vector2 position)
    {
        Vector2 size = tooltipPanel.sizeDelta;
        position.x = Mathf.Clamp(position.x, size.x * 0.5f, Screen.width - size.x * 0.5f);
        position.y = Mathf.Clamp(position.y, size.y * 0.5f, Screen.height - size.y * 0.5f);
        return position;
    }
}
```
