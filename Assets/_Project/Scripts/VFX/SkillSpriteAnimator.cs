using UnityEngine;

/// <summary>
/// Plays a sprite sheet animation by slicing a horizontal strip texture at runtime.
/// Attach to a GameObject with a SpriteRenderer. Self-destructs when done (unless looping).
/// </summary>
public class SkillSpriteAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameDuration;
    private bool loop;
    private float timer;
    private int currentFrame;
    private bool finished;

    /// <summary>
    /// Initializes the animator with pre-sliced frames.
    /// </summary>
    public void Initialize(Sprite[] sprites, float fps, bool loop = false)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        frames = sprites;
        frameDuration = 1f / fps;
        this.loop = loop;

        spriteRenderer.sprite = frames[0];
        spriteRenderer.sortingLayerName = "Foreground";
        spriteRenderer.sortingOrder = 15;
    }

    private void Update()
    {
        if (finished || frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer < frameDuration) return;

        timer -= frameDuration;
        currentFrame++;

        if (currentFrame >= frames.Length)
        {
            if (loop)
            {
                currentFrame = 0;
            }
            else
            {
                finished = true;
                Destroy(gameObject);
                return;
            }
        }

        spriteRenderer.sprite = frames[currentFrame];
    }

    /// <summary>
    /// Slices a horizontal strip texture into individual sprite frames.
    /// </summary>
    public static Sprite[] SliceSpriteSheet(Texture2D texture, int frameCount)
    {
        int cellWidth = texture.width / frameCount;
        int cellHeight = texture.height;
        float ppu = 100f; // pixels per unit

        var sprites = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            var rect = new Rect(i * cellWidth, 0, cellWidth, cellHeight);
            var pivot = new Vector2(0.5f, 0.5f);
            sprites[i] = Sprite.Create(texture, rect, pivot, ppu);
        }

        return sprites;
    }

    /// <summary>
    /// Creates a new animated sprite VFX at the given position.
    /// </summary>
    public static SkillSpriteAnimator Spawn(Vector3 position, Sprite[] frames, float fps,
        bool loop = false, float scale = 1f, bool flipX = false)
    {
        var go = new GameObject("SkillSpriteVFX");
        go.transform.position = position;
        go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, scale);

        var animator = go.AddComponent<SkillSpriteAnimator>();
        animator.Initialize(frames, fps, loop);

        return animator;
    }
}
