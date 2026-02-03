# UI Animation Patterns

> **Coroutines by default** - No external dependencies. DOTween optional (Asset Store).

## DOTween Menu Transitions

```csharp
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuTransitions : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private Ease easeType = Ease.OutQuart;

    [Header("Optional Vignette (URP Post Processing)")]
    [SerializeField] private float vignetteIntensity = 0.3f;

    private Sequence currentSequence;

    public void OpenMenu(CanvasGroup menu, RectTransform panel)
    {
        // Kill any running sequence to prevent conflicts
        currentSequence?.Kill();

        menu.gameObject.SetActive(true);
        menu.alpha = 0f;
        menu.interactable = false;
        menu.blocksRaycasts = false;
        panel.anchoredPosition = new Vector2(0, -50f);

        currentSequence = DOTween.Sequence();
        currentSequence.Append(menu.DOFade(1f, fadeDuration));
        currentSequence.Join(panel.DOAnchorPosY(0f, slideDuration).SetEase(easeType));
        currentSequence.OnComplete(() =>
        {
            menu.interactable = true;
            menu.blocksRaycasts = true;
        });
    }

    public void CloseMenu(CanvasGroup menu, RectTransform panel, System.Action onComplete = null)
    {
        currentSequence?.Kill();

        menu.interactable = false;
        menu.blocksRaycasts = false;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(menu.DOFade(0f, fadeDuration));
        currentSequence.Join(panel.DOAnchorPosY(-50f, slideDuration).SetEase(Ease.InQuart));
        currentSequence.OnComplete(() =>
        {
            menu.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    // Soul Reaver spectral shimmer
    public void SpectralPulse(Image element)
    {
        element.DOColor(new Color(0.25f, 0.88f, 0.82f, 0.8f), 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true); // Ignores Time.timeScale for paused menus
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }
}
```

## Animator-Based UI States

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimatedButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Animator animator;

    // Cache hash IDs for performance (Unity 6 best practice)
    private static readonly int NormalHash = Animator.StringToHash("Normal");
    private static readonly int HighlightedHash = Animator.StringToHash("Highlighted");
    private static readonly int SelectedHash = Animator.StringToHash("Selected");
    private static readonly int PressedHash = Animator.StringToHash("Pressed");

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip confirmSound;

    [Header("Audio Source (optional - uses AudioManager if null)")]
    [SerializeField] private AudioSource audioSource;

    public void OnSelect(BaseEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(SelectedHash);
        PlaySound(selectSound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(NormalHash);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(HighlightedHash);
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animator != null)
            animator.SetTrigger(NormalHash);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        // else: Use your AudioManager singleton if available
    }
}
```
