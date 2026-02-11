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
            case "magic_shield":
            case "evasion":
                ExecuteBuff(skillInstance);
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
                ExecuteProjectile(skillInstance, 15f, 5f);
                break;
            case "ice_bolt":
                ExecuteProjectile(skillInstance, 12f, 5f, slowPercent: 0.5f, slowDuration: 2f);
                break;
            case "meteor":
                ExecuteMeteor(skillInstance);
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
    }

    private void CreateMeleeHitbox(Vector3 position, Vector2 size, float facing,
        float damage, DamageType damageType, bool isCrit, float lifetime)
    {
        var go = new GameObject("SkillHitbox");
        go.transform.position = position;

        int layer = LayerMask.NameToLayer("PlayerAttack");
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

            if (i < 2) // No wait after last hit
                yield return new WaitForSeconds(0.12f);
        }
    }

    // ===========================
    // Projectile (fireball, ice_bolt)
    // ===========================

    private void ExecuteProjectile(SkillInstance skill, float speed, float lifetime,
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

        var projectile = SkillProjectile.Create(spawnPos, direction);
        projectile.Initialize(finalDamage, skill.skillData.damageType, isCrit,
            speed, lifetime, direction, gameObject, enemyLayers,
            slowPercent, slowDuration);
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
    // Meteor (mage) — AoE ahead of player
    // ===========================

    private void ExecuteMeteor(SkillInstance skill)
    {
        float facing = GetFacingDirection();
        Vector3 center = transform.position + new Vector3(facing * 6f, 0f, 0f);
        ExecuteAoE(skill, center, 4f);
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
        float dashSpeed = 25f;
        float dashDuration = 0.25f;

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

        // Spawn damage number
        var spawner = DamageNumberSpawner.GetOrCreate();
        if (spawner != null)
        {
            Vector3 pos = other.bounds.center + Vector3.up * other.bounds.extents.y;
            spawner.SpawnDamageWithType(pos, damage, damageType, isCrit);
        }
    }
}
