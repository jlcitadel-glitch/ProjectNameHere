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
        { "charge_shot_cast", 5 },
        { "shield_cast_activate", 6 },
        { "lightning_chain_cast", 5 },
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

        SkillSpriteAnimator.Spawn(position, frames, 10f, loop: false, scale: 1f, flipX: !facingRight);
    }

    /// <summary>
    /// Attaches the animated fireball sprite to a projectile, replacing the default blank sprite.
    /// </summary>
    public static void AttachFireballSprite(GameObject projectile)
    {
        var frames = GetFrames("fireball_ball");
        if (frames == null || projectile == null) return;

        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projectile.AddComponent<SpriteRenderer>();

        // Reset color — sprite art already has correct colors, damage-type tint would double-color
        sr.color = Color.white;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        var animator = projectile.AddComponent<SkillSpriteAnimator>();
        animator.Initialize(frames, 8f, loop: true);
    }

    /// <summary>
    /// Spawns the fireball hit/impact effect.
    /// </summary>
    public static void SpawnFireballHit(Vector3 position)
    {
        var frames = GetFrames("fireball_hit");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 12f, loop: false, scale: 1.2f);
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

        SkillSpriteAnimator.Spawn(position, frames, 10f, loop: false, scale: 1f, flipX: !facingRight);
    }

    /// <summary>
    /// Attaches the animated ice bolt sprite to a projectile.
    /// </summary>
    public static void AttachIceBoltSprite(GameObject projectile)
    {
        var frames = GetFrames("icebolt_ball");
        if (frames == null || projectile == null) return;

        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projectile.AddComponent<SpriteRenderer>();

        // Reset color — sprite art already has correct colors
        sr.color = Color.white;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        var animator = projectile.AddComponent<SkillSpriteAnimator>();
        animator.Initialize(frames, 8f, loop: true);
    }

    // ===========================
    // Magic Shield
    // ===========================

    /// <summary>
    /// Spawns the shield activation burst and a looping aura parented to the caster.
    /// The looping aura self-destructs after the given duration.
    /// </summary>
    public static void SpawnMagicShield(Transform caster, float duration)
    {
        Vector3 position = caster.position;

        // One-shot activation burst
        var activateFrames = GetFrames("magicshield_activate");
        if (activateFrames != null)
            SkillSpriteAnimator.Spawn(position, activateFrames, 12f, loop: false, scale: 1.2f);

        // Looping shield aura — parent to caster so it follows the player
        var loopFrames = GetFrames("magicshield_loop");
        if (loopFrames == null) return;

        var loopAura = SkillSpriteAnimator.Spawn(position, loopFrames, 7f, loop: true, scale: 1f);
        loopAura.transform.SetParent(caster, true);
        loopAura.transform.localPosition = Vector3.zero;

        // Destroy the loop when the buff expires
        Object.Destroy(loopAura.gameObject, duration);
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

        SkillSpriteAnimator.Spawn(position, frames, 10f, loop: false, scale: 1.5f);
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

        SkillSpriteAnimator.Spawn(position, frames, 5f, loop: false, scale: 1.5f);
    }

    /// <summary>
    /// Spawns the meteor explosion effect at the impact location.
    /// </summary>
    public static void SpawnMeteorExplosion(Vector3 position)
    {
        var frames = GetFrames("meteor_explosion");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 8f, loop: false, scale: 2f);
    }

    /// <summary>
    /// Spawns the meteor hit effect on a damaged target.
    /// </summary>
    public static void SpawnMeteorHit(Vector3 position)
    {
        var frames = GetFrames("meteor_hit1");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 10f, loop: false, scale: 1.2f);

        // Also spawn the secondary hit effect slightly offset
        var frames2 = GetFrames("meteor_hit2");
        if (frames2 != null)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0f);
            SkillSpriteAnimator.Spawn(position + offset, frames2, 12f, loop: false, scale: 1f);
        }
    }

    // ===========================
    // Charge Shot
    // ===========================

    /// <summary>
    /// Spawns the charge shot cast effect at the caster's position.
    /// </summary>
    public static void SpawnChargeShotCast(Vector3 position, bool facingRight)
    {
        var frames = GetFrames("charge_shot_cast");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 10f, loop: false, scale: 1f, flipX: !facingRight);
    }

    /// <summary>
    /// Attaches the animated charge shot sprite to a projectile, looping the cast frames.
    /// </summary>
    public static void AttachChargeShotSprite(GameObject projectile)
    {
        var frames = GetFrames("charge_shot_cast");
        if (frames == null || projectile == null) return;

        var sr = projectile.GetComponent<SpriteRenderer>();
        if (sr == null) sr = projectile.AddComponent<SpriteRenderer>();

        sr.color = Color.white;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;

        var animator = projectile.AddComponent<SkillSpriteAnimator>();
        animator.Initialize(frames, 8f, loop: true);
    }

    // ===========================
    // Chain Lightning
    // ===========================

    /// <summary>
    /// Spawns the chain lightning cast effect at the caster's position.
    /// </summary>
    public static void SpawnChainLightningCast(Vector3 position, bool facingRight)
    {
        var frames = GetFrames("lightning_chain_cast");
        if (frames == null) return;

        SkillSpriteAnimator.Spawn(position, frames, 12f, loop: false, scale: 1.2f, flipX: !facingRight);
    }

    // ===========================
    // Arcane Ward
    // ===========================

    /// <summary>
    /// Spawns the arcane ward activation burst and a looping aura parented to the caster.
    /// Same pattern as SpawnMagicShield.
    /// </summary>
    public static void SpawnArcaneWard(Transform caster, float duration)
    {
        Vector3 position = caster.position;

        // One-shot activation burst
        var activateFrames = GetFrames("shield_cast_activate");
        if (activateFrames != null)
            SkillSpriteAnimator.Spawn(position, activateFrames, 10f, loop: false, scale: 1.3f);

        // Looping aura — reuse the last few frames of the activation for the sustained effect
        if (activateFrames == null || activateFrames.Length < 2) return;

        // Use frames 3+ (the formed shield) as the loop
        int loopStart = Mathf.Min(3, activateFrames.Length - 1);
        var loopFrames = new Sprite[activateFrames.Length - loopStart];
        System.Array.Copy(activateFrames, loopStart, loopFrames, 0, loopFrames.Length);

        var loopAura = SkillSpriteAnimator.Spawn(position, loopFrames, 5f, loop: true, scale: 1.1f);
        loopAura.transform.SetParent(caster, true);
        loopAura.transform.localPosition = Vector3.zero;

        Object.Destroy(loopAura.gameObject, duration);
    }
}
