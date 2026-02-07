using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns a tall glowing beam from the player on level-up.
/// Uses sprite-based rendering for a solid beam-of-light look.
/// Attach to the Player GameObject (requires LevelSystem).
/// </summary>
public class LevelUpVFXController : MonoBehaviour
{
    [Header("Beam Dimensions")]
    [SerializeField] private float beamWidth = 6f;
    [SerializeField] private float beamHeight = 25f;

    [Header("Beam Color")]
    [SerializeField] private Color beamColor = new Color(0f, 0.808f, 0.820f, 0.85f);
    [SerializeField] private Color coreColor = new Color(0.3f, 0.85f, 1f, 0.95f);

    [Header("Timing")]
    [SerializeField] private float riseTime = 0.15f;
    [SerializeField] private float holdTime = 0.6f;
    [SerializeField] private float fadeTime = 0.5f;

    [Header("Screen Flash")]
    [SerializeField] private float flashAlpha = 0.2f;
    [SerializeField] private float flashDuration = 0.2f;

    private LevelSystem levelSystem;

    private void Start()
    {
        levelSystem = GetComponent<LevelSystem>();
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        StartCoroutine(PlayBeamSequence());

        if (ScreenFlash.Instance != null)
        {
            ScreenFlash.Instance.Flash(new Color(beamColor.r, beamColor.g, beamColor.b, flashAlpha), flashDuration);
        }
    }

    private IEnumerator PlayBeamSequence()
    {
        GameObject vfxRoot = new GameObject("LevelUpBeamVFX");
        vfxRoot.transform.SetParent(transform, false);
        vfxRoot.transform.localPosition = Vector3.zero;

        // Outer glow beam
        SpriteRenderer outerBeam = CreateBeamSprite(vfxRoot.transform, "OuterGlow",
            beamWidth, beamHeight, beamColor, 14);

        // Inner bright core (narrower, brighter)
        SpriteRenderer innerCore = CreateBeamSprite(vfxRoot.transform, "InnerCore",
            beamWidth * 0.6f, beamHeight, coreColor, 15);

        // Base glow at feet
        SpriteRenderer baseGlow = CreateBaseGlow(vfxRoot.transform, 16);

        // === Rise phase: beam scales up from zero width ===
        float elapsed = 0f;
        while (elapsed < riseTime)
        {
            float t = elapsed / riseTime;
            // Smooth ease-out curve
            float widthScale = 1f - (1f - t) * (1f - t);

            SetBeamWidth(outerBeam, widthScale);
            SetBeamWidth(innerCore, widthScale);
            SetBaseGlowAlpha(baseGlow, beamColor, widthScale);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetBeamWidth(outerBeam, 1f);
        SetBeamWidth(innerCore, 1f);
        SetBaseGlowAlpha(baseGlow, beamColor, 1f);

        // === Hold phase: beam pulses gently ===
        elapsed = 0f;
        while (elapsed < holdTime)
        {
            float pulse = 1f + Mathf.Sin(elapsed * 12f) * 0.08f;
            SetBeamWidth(outerBeam, pulse);
            SetBeamWidth(innerCore, pulse * 1.05f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // === Fade phase: beam fades and narrows ===
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            float t = elapsed / fadeTime;
            // Ease-in curve for fade
            float fade = 1f - t * t;

            Color outerC = outerBeam.color;
            outerC.a = beamColor.a * fade;
            outerBeam.color = outerC;

            Color innerC = innerCore.color;
            innerC.a = coreColor.a * fade;
            innerCore.color = innerC;

            // Narrow slightly as it fades
            float widthFade = Mathf.Lerp(1f, 0.3f, t);
            SetBeamWidth(outerBeam, widthFade);
            SetBeamWidth(innerCore, widthFade);

            SetBaseGlowAlpha(baseGlow, beamColor, fade * 0.8f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(vfxRoot);
    }

    private SpriteRenderer CreateBeamSprite(Transform parent, string name,
        float width, float height, Color color, int sortOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        // Offset upward so beam rises from player's center
        obj.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSoftRectSprite(32, 128);
        sr.color = color;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = sortOrder;
        sr.drawMode = SpriteDrawMode.Simple;

        // Scale to desired world dimensions
        // Sprite is 32x128 pixels at 32 PPU = 1x4 world units base
        obj.transform.localScale = new Vector3(width, height / 4f, 1f);

        // Use additive-like material for glow
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            sr.material = new Material(shader);
        }

        return sr;
    }

    private SpriteRenderer CreateBaseGlow(Transform parent, int sortOrder)
    {
        GameObject obj = new GameObject("BaseGlow");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = new Vector3(beamWidth * 1.5f, beamWidth * 0.8f, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSoftCircleSprite(32);
        sr.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.5f);
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = sortOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            sr.material = new Material(shader);
        }

        return sr;
    }

    private void SetBeamWidth(SpriteRenderer sr, float scale)
    {
        Vector3 s = sr.transform.localScale;
        float baseWidth = sr.name == "InnerCore" ? beamWidth * 0.6f : beamWidth;
        s.x = baseWidth * scale;
        sr.transform.localScale = s;
    }

    private void SetBaseGlowAlpha(SpriteRenderer sr, Color baseColor, float alpha)
    {
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f * alpha);
    }

    /// <summary>
    /// Creates a soft-edged rectangle texture for the beam.
    /// Bright center column with smooth falloff on all edges.
    /// </summary>
    private Sprite CreateSoftRectSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[width * height];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Horizontal falloff (wide bright center, soft edges)
                float dx = Mathf.Abs(x - halfW) / halfW;
                float alphaX = 1f - Mathf.Clamp01(dx);
                alphaX = Mathf.Pow(alphaX, 0.8f); // Gentle falloff, stays bright across most of the width

                // Vertical falloff (fade at top, slight fade at bottom)
                float dy = (float)y / height;
                float alphaTop = 1f - Mathf.Pow(Mathf.Clamp01((dy - 0.7f) / 0.3f), 1.5f);
                float alphaBot = Mathf.Clamp01(dy / 0.05f); // Quick fade-in at bottom

                float alpha = alphaX * alphaTop * alphaBot;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>
    /// Creates a soft circle texture for the base glow.
    /// </summary>
    private Sprite CreateSoftCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.Pow(alpha, 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }
}
