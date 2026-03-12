using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectName.UI;

/// <summary>
/// Central dispatcher that executes all 19 class skills.
/// Attached to the Player. Called by PlayerSkillController.ExecuteSkill().
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private string attackLayerName = "PlayerAttack";

    [Header("Debug")]
    [SerializeField] private bool logExecution = true;

    // Cached references
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private StatSystem statSystem;
    private ActiveBuffTracker buffTracker;
    private PassiveSkillTracker passiveTracker;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        manaSystem = GetComponent<ManaSystem>();
        statSystem = GetComponent<StatSystem>();
        buffTracker = GetComponent<ActiveBuffTracker>();
        passiveTracker = GetComponent<PassiveSkillTracker>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Add buff/passive trackers if missing
        if (buffTracker == null)
            buffTracker = gameObject.AddComponent<ActiveBuffTracker>();
        if (passiveTracker == null)
            passiveTracker = gameObject.AddComponent<PassiveSkillTracker>();

        // Add buff visual indicator
        if (GetComponent<BuffAuraVFX>() == null)
            gameObject.AddComponent<BuffAuraVFX>();
    }

    /// <summary>
    /// Main entry point — dispatches skill execution by skillId.
    /// </summary>
    public void Execute(SkillInstance skillInstance)
    {
        if (skillInstance?.skillData == null) return;

        string skillId = skillInstance.SkillId;

        if (logExecution)
            Debug.Log($"[SkillExecutor] Executing: {skillId} (Lv.{skillInstance.currentLevel})");

        switch (skillId)
        {
            // --- Beginner ---
            case "power_strike":
                ExecuteMeleeHitbox(skillInstance, new Vector2(2f, 1.5f), 0.15f);
                break;
            case "recovery":
                ExecuteHeal(skillInstance);
                break;
            case "guard":
            case "berserk":
            case "war_cry":
            case "evasion":
                ExecuteBuff(skillInstance);
                break;
            case "magic_shield":
                ExecuteBuff(skillInstance);
                float shieldDuration = skillInstance.GetDuration();
                if (shieldDuration <= 0f) shieldDuration = skillInstance.skillData.baseDuration;
                MageSkillVFX.SpawnMagicShield(transform, shieldDuration);
                break;
            case "critical_eye":
            case "iron_skin":
            case "mana_mastery":
            case "critical_mastery":
                // Passives are handled by PassiveSkillTracker on learn/level-up.
                // No active execution needed.
                break;

            // --- Warrior ---
            case "triple_slash":
                StartCoroutine(ExecuteTripleSlash(skillInstance));
                break;
            case "ground_slam":
                ExecuteAoE(skillInstance, transform.position, 3f);
                break;

            // --- Mage ---
            case "fireball":
                ExecuteFireball(skillInstance);
                break;
            case "ice_bolt":
                ExecuteIceBolt(skillInstance);
                break;
            case "meteor":
                StartCoroutine(ExecuteMeteorSequence(skillInstance));
                break;
            case "charge_shot":
                ExecuteChargeShot(skillInstance);
                break;
            case "chain_lightning":
                ExecuteChainLightning(skillInstance);
                break;
            case "arcane_ward":
                ExecuteBuff(skillInstance);
                float wardDuration = skillInstance.GetDuration();
                if (wardDuration <= 0f) wardDuration = skillInstance.skillData.baseDuration;
                MageSkillVFX.SpawnArcaneWard(transform, wardDuration);
                break;

            // --- Rogue ---
            case "quick_strike":
                ExecuteMeleeHitbox(skillInstance, new Vector2(2f, 1.5f), 0.15f, extraCritBonus: 0.15f);
                break;
            case "shadow_strike":
                StartCoroutine(ExecuteShadowStrike(skillInstance));
                break;
            case "poison_blade":
                ExecutePoisonBlade(skillInstance);
                break;

            default:
                if (logExecution)
                    Debug.LogWarning($"[SkillExecutor] No execution handler for skill: {skillId}");
                break;
        }
    }

    // ===========================
    // Melee Hitbox (power_strike, quick_strike)
    // ===========================

    private void ExecuteMeleeHitbox(SkillInstance skill, Vector2 hitboxSize, float lifetime,
        float extraCritBonus = 0f, float damageMultiplierOverride = 0f)
    {
        float facing = GetFacingDirection();
        Vector3 spawnPos = transform.position + new Vector3(facing * hitboxSize.x * 0.5f, 0f, 0f);

        float baseDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float finalDamage = baseDamage * statMult * buffMult;

        if (damageMultiplierOverride > 0f)
            finalDamage *= damageMultiplierOverride;

        // Crit roll
        bool isCrit = RollCrit(extraCritBonus);
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        if (isCrit)
            finalDamage *= (baseCritMult * critDamageMult);

        DamageType dmgType = skill.skillData.damageType;

        CreateMeleeHitbox(spawnPos, hitboxSize, facing, finalDamage, dmgType, isCrit, lifetime);
        SkillVFXFactory.SpawnMeleeSweep(spawnPos, facing, dmgType);
    }

    private void CreateMeleeHitbox(Vector3 position, Vector2 size, float facing,
        float damage, DamageType damageType, bool isCrit, float lifetime)
    {
        var go = new GameObject("SkillHitbox");
        go.transform.position = position;

        int layer = LayerMask.NameToLayer(attackLayerName);
        if (layer != -1)
            go.layer = layer;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = size;

        var handler = go.AddComponent<SkillHitboxHandler>();
        handler.Initialize(damage, damageType, isCrit, gameObject, enemyLayers);

        Destroy(go, lifetime);
    }

    // ===========================
    // Triple Slash (warrior)
    // ===========================

    private IEnumerator ExecuteTripleSlash(SkillInstance skill)
    {
        float totalDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float perHitDamage = (totalDamage * statMult * buffMult) / 3f;
        DamageType dmgType = skill.skillData.damageType;

        for (int i = 0; i < 3; i++)
        {
            float facing = GetFacingDirection();
            Vector3 spawnPos = transform.position + new Vector3(facing * 1f, 0f, 0f);

            bool isCrit = RollCrit();
            float hitDamage = perHitDamage;
            float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
            float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
            if (isCrit)
                hitDamage *= (baseCritMult * critDamageMult);

            CreateMeleeHitbox(spawnPos, new Vector2(2f, 1.5f), facing, hitDamage, dmgType, isCrit, 0.1f);
            SkillVFXFactory.SpawnMeleeSweep(spawnPos, facing, dmgType);

            if (i < 2) // No wait after last hit
                yield return new WaitForSeconds(0.22f);
        }
    }

    // ===========================
    // AoE (ground_slam, meteor)
    // ===========================

    private void ExecuteAoE(SkillInstance skill, Vector3 center, float radius)
    {
        float baseDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float finalDamage = baseDamage * statMult * buffMult;
        DamageType dmgType = skill.skillData.damageType;

        bool isCrit = RollCrit();
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        if (isCrit)
            finalDamage *= (baseCritMult * critDamageMult);

        SkillVFXFactory.SpawnAoECircle(center, radius, dmgType);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);

        if (hits.Length == 0)
        {
            // Fallback: find all IDamageable in radius if no layer match
            hits = Physics2D.OverlapCircleAll(center, radius);
        }

        HashSet<Transform> hitRoots = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            // Prevent double-hitting the same entity
            Transform root = hit.transform.root;
            if (!hitRoots.Add(root)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>()
                ?? hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(finalDamage);
                SpawnDamageNumber(hit, finalDamage, dmgType, isCrit);
                continue;
            }

            HealthSystem hs = hit.GetComponent<HealthSystem>()
                ?? hit.GetComponentInParent<HealthSystem>();

            if (hs != null)
            {
                hs.TakeDamage(finalDamage);
                SpawnDamageNumber(hit, finalDamage, dmgType, isCrit);
            }
        }
    }

    // ===========================
    // Heal (recovery)
    // ===========================

    private void ExecuteHeal(SkillInstance skill)
    {
        if (healthSystem == null) return;

        float healAmount = skill.GetDamage(); // baseDamage repurposed as heal value
        float actualHeal = healthSystem.Heal(healAmount);

        // Show heal number
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner != null && actualHeal > 0f)
        {
            Vector3 pos = transform.position + Vector3.up * 1f;
            spawner.SpawnDamage(pos, actualHeal, DamageNumberType.Heal);
        }

        if (logExecution)
            Debug.Log($"[SkillExecutor] Recovery healed {actualHeal}");
    }

    // ===========================
    // Buff (guard, berserk, war_cry, magic_shield, evasion)
    // ===========================

    private void ExecuteBuff(SkillInstance skill)
    {
        if (buffTracker == null) return;

        float duration = skill.GetDuration();
        if (duration <= 0f)
        {
            // Fallback: use baseDuration from skill data
            duration = skill.skillData.baseDuration;
        }

        var buff = ActiveBuffTracker.CreateBuffForSkill(skill.SkillId, duration);
        buffTracker.AddBuff(buff);

        // Show buff text
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner != null)
        {
            Vector3 pos = transform.position + Vector3.up * 1.2f;
            Color buffColor = new Color(0.81f, 0.71f, 0.23f, 1f); // Aged gold
            spawner.SpawnText(pos, skill.skillData.skillName, buffColor);
        }

        if (logExecution)
            Debug.Log($"[SkillExecutor] Buff {skill.SkillId} applied for {duration}s");
    }

    // ===========================
    // Shadow Strike (rogue) — dash + wide hitbox + auto-crit
    // ===========================

    private IEnumerator ExecuteShadowStrike(SkillInstance skill)
    {
        float facing = GetFacingDirection();
        float dashSpeed = 18f;
        float dashDuration = 0.35f;

        float baseDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        float finalDamage = baseDamage * statMult * buffMult * baseCritMult * critDamageMult; // Auto-crit

        DamageType dmgType = skill.skillData.damageType;

        // Grant invulnerability during dash
        if (healthSystem != null)
            healthSystem.GrantInvulnerability(dashDuration + 0.1f);

        // Create wide hitbox covering the dash path
        Vector3 hitboxPos = transform.position + new Vector3(facing * 3f, 0f, 0f);
        CreateMeleeHitbox(hitboxPos, new Vector2(6f, 1.5f), facing, finalDamage, dmgType, true, dashDuration);
        SkillVFXFactory.SpawnMeleeSweep(hitboxPos, facing, dmgType);

        // Dash movement
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Stop dash momentum
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ===========================
    // Poison Blade (rogue) — melee hit + DoT
    // ===========================

    private void ExecutePoisonBlade(SkillInstance skill)
    {
        float facing = GetFacingDirection();
        Vector3 spawnPos = transform.position + new Vector3(facing * 1f, 0f, 0f);

        float totalDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float scaledDamage = totalDamage * statMult * buffMult;

        // 50% upfront, 50% over duration as DoT
        float hitDamage = scaledDamage * 0.5f;
        float dotTotal = scaledDamage * 0.5f;
        float dotDuration = skill.GetDuration();
        if (dotDuration <= 0f) dotDuration = skill.skillData.baseDuration;
        if (dotDuration <= 0f) dotDuration = 5f; // Absolute fallback

        DamageType dmgType = skill.skillData.damageType;

        bool isCrit = RollCrit();
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        if (isCrit)
        {
            hitDamage *= (baseCritMult * critDamageMult);
            dotTotal *= (baseCritMult * critDamageMult);
        }

        // Collect enemies hit by initial melee
        Vector2 hitboxSize = new Vector2(2f, 1.5f);
        Vector2 hitboxCenter = new Vector2(spawnPos.x, spawnPos.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, 0f, enemyLayers);
        if (hits.Length == 0)
        {
            // Fallback: broader search
            hits = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, 0f);
        }

        HashSet<Transform> hitRoots = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            Transform root = hit.transform.root;
            if (!hitRoots.Add(root)) continue;

            // Apply initial hit damage
            IDamageable damageable = hit.GetComponent<IDamageable>()
                ?? hit.GetComponentInParent<IDamageable>();
            HealthSystem hs = hit.GetComponent<HealthSystem>()
                ?? hit.GetComponentInParent<HealthSystem>();

            if (damageable != null)
            {
                damageable.TakeDamage(hitDamage);
                SpawnDamageNumber(hit, hitDamage, dmgType, isCrit);
            }
            else if (hs != null)
            {
                hs.TakeDamage(hitDamage);
                SpawnDamageNumber(hit, hitDamage, dmgType, isCrit);
            }

            // Start DoT coroutine on each target
            if (damageable != null || hs != null)
            {
                StartCoroutine(ApplyPoisonDoT(hit, damageable, hs, dotTotal, dotDuration, dmgType));
            }
        }
    }

    private IEnumerator ApplyPoisonDoT(Collider2D target, IDamageable damageable,
        HealthSystem hs, float totalDotDamage, float duration, DamageType dmgType)
    {
        float tickInterval = 0.5f;
        int totalTicks = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
        float damagePerTick = totalDotDamage / totalTicks;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            // Check if target still exists
            if (target == null) yield break;

            if (damageable != null)
            {
                damageable.TakeDamage(damagePerTick);
            }
            else if (hs != null)
            {
                hs.TakeDamage(damagePerTick);
            }

            SpawnDamageNumber(target, damagePerTick, dmgType, false);
        }
    }

    // ===========================
    // Fireball (mage) — projectile with sprite VFX
    // ===========================

    private void ExecuteFireball(SkillInstance skill)
    {
        float facing = GetFacingDirection();

        // Cast effect at player
        MageSkillVFX.SpawnFireballCast(transform.position + new Vector3(facing * 0.5f, 0.2f, 0f), facing > 0);

        // Create projectile with animated fireball sprite
        var projectile = CreateSkillProjectile(skill, 10f, 5f);
        MageSkillVFX.AttachFireballSprite(projectile.gameObject);
        SkillVFXFactory.AttachProjectileTrail(projectile.gameObject, skill.skillData.damageType);
    }

    // ===========================
    // Ice Bolt (mage) — projectile with sprite VFX
    // ===========================

    private void ExecuteIceBolt(SkillInstance skill)
    {
        float facing = GetFacingDirection();

        // Cast effect at player
        MageSkillVFX.SpawnIceBoltCast(transform.position + new Vector3(facing * 0.5f, 0.2f, 0f), facing > 0);

        // Create projectile with animated ice bolt sprite
        var projectile = CreateSkillProjectile(skill, 8f, 5f, slowPercent: 0.5f, slowDuration: 2f);
        MageSkillVFX.AttachIceBoltSprite(projectile.gameObject);
        SkillVFXFactory.AttachProjectileTrail(projectile.gameObject, skill.skillData.damageType);
    }

    // ===========================
    // Meteor (mage) — prepare → explosion → hit
    // ===========================

    private IEnumerator ExecuteMeteorSequence(SkillInstance skill)
    {
        float facing = GetFacingDirection();
        Vector3 center = transform.position + new Vector3(facing * 6f, 0f, 0f);

        // Charge-up effect at target location
        MageSkillVFX.SpawnMeteorPrepare(center + Vector3.up * 2f);

        // Wait for the prepare animation (10 frames at 5fps = 2.0s)
        yield return new WaitForSeconds(2.0f);

        // Explosion effect
        MageSkillVFX.SpawnMeteorExplosion(center);

        // Execute damage AoE (also spawns particle VFX)
        ExecuteAoE(skill, center, 4f);

        // Hit effects on each damaged target position
        MageSkillVFX.SpawnMeteorHit(center);
    }

    /// <summary>
    /// Creates a projectile with damage calculation (shared by mage projectile skills).
    /// </summary>
    private SkillProjectile CreateSkillProjectile(SkillInstance skill, float speed, float lifetime,
        float slowPercent = 0f, float slowDuration = 0f)
    {
        float facing = GetFacingDirection();
        Vector2 direction = new Vector2(facing, 0f);
        Vector3 spawnPos = transform.position + new Vector3(facing * 1f, 0.2f, 0f);

        float baseDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float finalDamage = baseDamage * statMult * buffMult;

        bool isCrit = RollCrit();
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        if (isCrit)
            finalDamage *= (baseCritMult * critDamageMult);

        var projectile = SkillProjectile.Create(spawnPos, direction, attackLayerName);
        projectile.Initialize(finalDamage, skill.skillData.damageType, isCrit,
            speed, lifetime, direction, gameObject, enemyLayers,
            slowPercent, slowDuration);

        return projectile;
    }

    // ===========================
    // Charge Shot (mage) — projectile with sprite VFX
    // ===========================

    private void ExecuteChargeShot(SkillInstance skill)
    {
        float facing = GetFacingDirection();

        // Cast effect at player
        MageSkillVFX.SpawnChargeShotCast(transform.position + new Vector3(facing * 0.5f, 0.2f, 0f), facing > 0);

        // Create projectile with animated charge shot sprite
        var projectile = CreateSkillProjectile(skill, 10f, 4f);
        MageSkillVFX.AttachChargeShotSprite(projectile.gameObject);
        SkillVFXFactory.AttachProjectileTrail(projectile.gameObject, skill.skillData.damageType);
    }

    // ===========================
    // Chain Lightning (mage) — chain-targeting AoE
    // ===========================

    private void ExecuteChainLightning(SkillInstance skill)
    {
        float facing = GetFacingDirection();
        bool facingRight = facing > 0;

        // Cast VFX at player
        MageSkillVFX.SpawnChainLightningCast(transform.position + new Vector3(facing * 0.5f, 0.2f, 0f), facingRight);

        // Damage calculation
        float baseDamage = skill.GetDamage();
        float statMult = GetStatMultiplier(skill);
        float buffMult = buffTracker != null ? buffTracker.TotalAttackMultiplier : 1f;
        float finalDamage = baseDamage * statMult * buffMult;
        DamageType dmgType = skill.skillData.damageType;

        // Single crit roll for entire chain
        bool isCrit = RollCrit();
        float critDamageMult = 1f + (passiveTracker != null ? passiveTracker.PassiveCritDamageBonus : 0f);
        float baseCritMult = statSystem != null ? statSystem.CritDamageMultiplier : 2f;
        if (isCrit)
            finalDamage *= (baseCritMult * critDamageMult);

        // Find closest enemy in range (8 units)
        float primaryRange = 8f;
        Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, primaryRange, enemyLayers);
        if (candidates.Length == 0)
            candidates = Physics2D.OverlapCircleAll(transform.position, primaryRange);

        Collider2D primaryTarget = null;
        float closestDist = float.MaxValue;

        foreach (var c in candidates)
        {
            if (c.isTrigger) continue;
            if (c.transform.IsChildOf(transform)) continue;

            IDamageable dmg = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            HealthSystem hs = c.GetComponent<HealthSystem>() ?? c.GetComponentInParent<HealthSystem>();
            if (dmg == null && hs == null) continue;

            float dist = Vector2.Distance(transform.position, c.bounds.center);
            if (dist < closestDist)
            {
                closestDist = dist;
                primaryTarget = c;
            }
        }

        if (primaryTarget == null) return;

        // Damage primary target (full damage)
        HashSet<Transform> hitRoots = new HashSet<Transform>();
        hitRoots.Add(primaryTarget.transform.root);

        ApplyChainDamage(primaryTarget, finalDamage, dmgType, isCrit);
        SkillVFXFactory.SpawnImpactBurst(primaryTarget.bounds.center, dmgType);

        // Chain to up to 2 secondary targets within 4 units of primary
        float chainRange = 4f;
        float chainDamage = finalDamage * 0.5f;
        int chainsRemaining = 2;

        Collider2D[] chainCandidates = Physics2D.OverlapCircleAll(primaryTarget.bounds.center, chainRange, enemyLayers);
        if (chainCandidates.Length == 0)
            chainCandidates = Physics2D.OverlapCircleAll(primaryTarget.bounds.center, chainRange);

        foreach (var c in chainCandidates)
        {
            if (chainsRemaining <= 0) break;
            if (c.isTrigger) continue;
            if (c.transform.IsChildOf(transform)) continue;

            Transform root = c.transform.root;
            if (!hitRoots.Add(root)) continue;

            IDamageable dmg = c.GetComponent<IDamageable>() ?? c.GetComponentInParent<IDamageable>();
            HealthSystem hs = c.GetComponent<HealthSystem>() ?? c.GetComponentInParent<HealthSystem>();
            if (dmg == null && hs == null) continue;

            ApplyChainDamage(c, chainDamage, dmgType, isCrit);
            SkillVFXFactory.SpawnImpactBurst(c.bounds.center, dmgType);
            chainsRemaining--;
        }
    }

    private void ApplyChainDamage(Collider2D target, float damage, DamageType dmgType, bool isCrit)
    {
        IDamageable damageable = target.GetComponent<IDamageable>()
            ?? target.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        else
        {
            HealthSystem hs = target.GetComponent<HealthSystem>()
                ?? target.GetComponentInParent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(damage);
        }

        SpawnDamageNumber(target, damage, dmgType, isCrit);
    }

    // ===========================
    // Helpers
    // ===========================

    private float GetFacingDirection()
    {
        return Mathf.Sign(transform.localScale.x);
    }

    private float GetStatMultiplier(SkillInstance skill)
    {
        if (statSystem == null) return 1f;

        // Physical damage scales with STR, everything else with INT
        if (skill.skillData.damageType == DamageType.Physical)
            return statSystem.MeleeDamageMultiplier;

        return statSystem.SkillDamageMultiplier;
    }

    private bool RollCrit(float extraBonus = 0f)
    {
        if (statSystem == null) return false;

        float passiveBonus = passiveTracker != null ? passiveTracker.PassiveCritChanceBonus : 0f;
        float buffBonus = buffTracker != null ? buffTracker.TotalCritChanceBonus : 0f;
        float totalCrit = statSystem.GetTotalCritChance(passiveBonus + extraBonus, buffBonus);

        return Random.value < totalCrit;
    }

    private void SpawnDamageNumber(Collider2D target, float damage, DamageType dmgType, bool isCrit)
    {
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner == null || target == null) return;

        Vector3 pos = target.bounds.center + Vector3.up * target.bounds.extents.y;
        spawner.SpawnDamageWithType(pos, damage, dmgType, isCrit);
    }
}

/// <summary>
/// Temporary MonoBehaviour attached to runtime skill hitboxes.
/// Handles damage application on trigger enter, similar to AttackHitbox.
/// </summary>
public class SkillHitboxHandler : MonoBehaviour
{
    private float damage;
    private DamageType damageType;
    private bool isCrit;
    private GameObject caster;
    private LayerMask targetLayers;
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    public void Initialize(float damage, DamageType damageType, bool isCrit,
        GameObject caster, LayerMask targetLayers)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.isCrit = isCrit;
        this.caster = caster;
        this.targetLayers = targetLayers;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitTargets.Contains(other)) return;
        if (other.isTrigger) return;
        if (caster != null && other.transform.IsChildOf(caster.transform)) return;

        // Layer check with IDamageable fallback
        if (targetLayers != 0)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((targetLayers & otherLayer) == 0)
            {
                IDamageable fallback = other.GetComponent<IDamageable>()
                    ?? other.GetComponentInParent<IDamageable>();
                if (fallback == null) return;
            }
        }

        hitTargets.Add(other);

        // Apply damage
        IDamageable damageable = other.GetComponent<IDamageable>()
            ?? other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        else
        {
            HealthSystem hs = other.GetComponent<HealthSystem>()
                ?? other.GetComponentInParent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(damage);
        }

        // Spawn damage number + impact VFX
        Vector3 hitPos = other.bounds.center;
        SkillVFXFactory.SpawnImpactBurst(hitPos, damageType);

        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner != null)
        {
            Vector3 pos = hitPos + Vector3.up * other.bounds.extents.y;
            spawner.SpawnDamageWithType(pos, damage, damageType, isCrit);
        }
    }
}
