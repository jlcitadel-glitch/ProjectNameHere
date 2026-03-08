using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and spawns sprite-based VFX for mage skills.
/// Sprite sheets are loaded from Resources/VFX/MageSkills/ and sliced at runtime.
/// </summary>
public static class MageSkillVFX
{
    // Frame counts for each sprite sheet (must match the actual sheets)
    private static readonly Dictionary<string, int> FrameCounts = new Dictionary<string, int>
    {
        { "fireball_ball", 3 },
        { "fireball_cast", 9 },
        { "fireball_hit", 7 },
        { "icebolt_ball", 3 },
        { "icebolt_cast", 7 },
        { "magicshield_activate", 12 },
        { "magicshield_loop", 7 },
        { "manamastery_effect", 15 },
        { "meteor_prepare", 10 },
        { "meteor_explosion", 7 },
        { "meteor_hit1", 6 },
        { "meteor_hit2", 13 },
    };

    // Cached sprite arrays per sheet name
    private static readonly Dictionary<string, Sprite[]> SpriteCache = new Dictionary<string, Sprite[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCache()
    {
        SpriteCache.Clear();
    }

    /// <summary>
    /// Loads and caches sprite frames for a given sheet name.
    /// </summary>
    private static Sprite[] GetFrames(string sheetName)
    {
        if (SpriteCache.TryGetValue(sheetName, out var cached))
            return cached;

        var texture = Resources.Load<Texture2D>($"VFX/MageSkills/{sheetName}");
        if (texture == null)
        {
            Debug.LogWarning($"[MageSkillVFX] Missing sprite sheet: VFX/MageSkills/{sheetName}");
            return null;
        }

        if (!FrameCounts.TryGetValue(sheetName, out int frameCount))
        {
            Debug.LogWarning($"[MageSkillVFX] No frame count for: {sheetName}");
            return null;
        }

        var frames = SkillSpriteAnimator.SliceSpriteSheet(texture, frameCount);
        SpriteCache[sheetName] = frames;
        return frames;
    }

    // ===========================
    // Fireball
    // ===========================

    /// <summary>
    /// Spawns the fireball cast effect at the caster's position.
    /// </summary>
    public static void SpawnFireballCast(Vector3 position, bool facingRight)
    {
        var frames = GetFrames("fireball_cast");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 18f, loop: false, scale: 1f, flipX: !facingRight);
    }

    /// <summary>
    /// Attaches the animated fireball sprite to a projectile, replacing the default blank sprite.
    /// </summary>
    public static void AttachFireballSprite(GameObject projectile)
    {
        var frames = GetFrames("fireball_ball");
        if (frames == null || projectile == null) return;

        var animator = projectile.AddComponent<SkillSpriteAnimator>();
        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projectile.AddComponent<SpriteRenderer>();

        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        animator.Initialize(frames, 12f, loop: true);
    }

    /// <summary>
    /// Spawns the fireball hit/impact effect.
    /// </summary>
    public static void SpawnFireballHit(Vector3 position)
    {
        var frames = GetFrames("fireball_hit");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 20f, loop: false, scale: 1.2f);
    }

    // ===========================
    // Ice Bolt
    // ===========================

    /// <summary>
    /// Spawns the ice bolt cast effect at the caster's position.
    /// </summary>
    public static void SpawnIceBoltCast(Vector3 position, bool facingRight)
    {
        var frames = GetFrames("icebolt_cast");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 18f, loop: false, scale: 1f, flipX: !facingRight);
    }

    /// <summary>
    /// Attaches the animated ice bolt sprite to a projectile.
    /// </summary>
    public static void AttachIceBoltSprite(GameObject projectile)
    {
        var frames = GetFrames("icebolt_ball");
        if (frames == null || projectile == null) return;

        var animator = projectile.AddComponent<SkillSpriteAnimator>();
        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projectile.AddComponent<SpriteRenderer>();

        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        animator.Initialize(frames, 12f, loop: true);
    }

    // ===========================
    // Magic Shield
    // ===========================

    /// <summary>
    /// Spawns the shield activation burst, then returns the looping aura animator
    /// so it can be parented to the player.
    /// </summary>
    public static SkillSpriteAnimator SpawnMagicShieldActivate(Vector3 position)
    {
        var activateFrames = GetFrames("magicshield_activate");
        if (activateFrames != null)
            SkillSpriteAnimator.Spawn(position, activateFrames, 20f, loop: false, scale: 1.2f);

        // Spawn the looping shield aura
        var loopFrames = GetFrames("magicshield_loop");
        if (loopFrames == null) return null;

        return SkillSpriteAnimator.Spawn(position, loopFrames, 10f, loop: true, scale: 1f);
    }

    // ===========================
    // Mana Mastery
    // ===========================

    /// <summary>
    /// Spawns the mana mastery activation effect (plays once on skill learn/level-up).
    /// </summary>
    public static void SpawnManaMasteryEffect(Vector3 position)
    {
        var frames = GetFrames("manamastery_effect");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 15f, loop: false, scale: 1.5f);
    }

    // ===========================
    // Meteor
    // ===========================

    /// <summary>
    /// Spawns the meteor charge-up effect at the target location.
    /// </summary>
    public static void SpawnMeteorPrepare(Vector3 position)
    {
        var frames = GetFrames("meteor_prepare");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 8f, loop: false, scale: 1.5f);
    }

    /// <summary>
    /// Spawns the meteor explosion effect at the impact location.
    /// </summary>
    public static void SpawnMeteorExplosion(Vector3 position)
    {
        var frames = GetFrames("meteor_explosion");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 14f, loop: false, scale: 2f);
    }

    /// <summary>
    /// Spawns the meteor hit effect on a damaged target.
    /// </summary>
    public static void SpawnMeteorHit(Vector3 position)
    {
        var frames = GetFrames("meteor_hit1");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 18f, loop: false, scale: 1.2f);

        // Also spawn the secondary hit effect slightly offset
        var frames2 = GetFrames("meteor_hit2");
        if (frames2 != null)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0f);
            SkillSpriteAnimator.Spawn(position + offset, frames2, 20f, loop: false, scale: 1f);
        }
    }
}
