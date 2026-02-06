using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen color flash overlay. Attach to a Canvas with a full-screen Image.
/// Call Flash() from anywhere via the singleton Instance.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    private Image flashImage;
    private float flashTimer;
    private float flashDuration;
    private Color flashColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Find or create the flash image
        flashImage = GetComponentInChildren<Image>();
        if (flashImage == null)
        {
            // Create overlay canvas + image at runtime
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            GameObject imgObj = new GameObject("FlashOverlay");
            imgObj.transform.SetParent(transform, false);
            flashImage = imgObj.AddComponent<Image>();

            RectTransform rt = flashImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(flashTimer / flashDuration);
        flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * alpha);
    }

    /// <summary>
    /// Triggers a brief full-screen color flash.
    /// </summary>
    public void Flash(Color color, float duration)
    {
        if (duration <= 0f)
            return;

        flashColor = color;
        flashDuration = duration;
        flashTimer = duration;
        flashImage.color = color;
    }
}
