using UnityEngine;

/// <summary>
/// Tracks permanent passive skill bonuses and applies them to player systems.
/// Recalculates when skills are learned or leveled up.
/// </summary>
public class PassiveSkillTracker : MonoBehaviour
{
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;

    // Aggregate passive bonuses
    public float PassiveCritChanceBonus { get; private set; }
    public float PassiveDefenseBonus { get; private set; }
    public float PassiveManaRegenBonus { get; private set; }
    public float PassiveCritDamageBonus { get; private set; }

    private float baseRegenRate;
    private bool baseRegenCaptured;

    private void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        manaSystem = GetComponent<ManaSystem>();

        // Capture base regen rate before we modify it
        if (manaSystem != null)
        {
            baseRegenRate = manaSystem.RegenRate;
            baseRegenCaptured = true;
        }

        // Subscribe to skill events
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillLearned += HandleSkillLearned;
            SkillManager.Instance.OnSkillLevelChanged += HandleSkillLevelChanged;
        }

        // Calculate initial passives (in case skills were loaded from save)
        RecalculateAllPassives();
    }

    private void OnDestroy()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.OnSkillLearned -= HandleSkillLearned;
            SkillManager.Instance.OnSkillLevelChanged -= HandleSkillLevelChanged;
        }
    }

    private void HandleSkillLearned(SkillInstance skill)
    {
        if (skill.SkillType == SkillType.Passive)
        {
            RecalculateAllPassives();
            SpawnPassiveVFX(skill);
        }
    }

    private void HandleSkillLevelChanged(SkillInstance skill, int oldLevel, int newLevel)
    {
        if (skill.SkillType == SkillType.Passive)
        {
            RecalculateAllPassives();
            SpawnPassiveVFX(skill);
        }
    }

    private void SpawnPassiveVFX(SkillInstance skill)
    {
        if (skill.SkillId == "mana_mastery")
            MageSkillVFX.SpawnManaMasteryEffect(transform.position);
    }

    /// <summary>
    /// Reads all learned passive skills and recomputes aggregate bonuses.
    /// </summary>
    public void RecalculateAllPassives()
    {
        PassiveCritChanceBonus = 0f;
        PassiveDefenseBonus = 0f;
        PassiveManaRegenBonus = 0f;
        PassiveCritDamageBonus = 0f;

        if (SkillManager.Instance == null) return;

        var passives = SkillManager.Instance.GetSkillsByType(SkillType.Passive);
        foreach (var skill in passives)
        {
            float value = skill.GetDamage(); // baseDamage field repurposed as passive value

            switch (skill.SkillId)
            {
                case "critical_eye":
                    // 2% at lv1, +1%/level (value = 2 at lv1, grows by damagePerLevel)
                    PassiveCritChanceBonus += value / 100f;
                    break;

                case "iron_skin":
                    // 3% at lv1, +1%/level
                    PassiveDefenseBonus += value / 100f;
                    break;

                case "mana_mastery":
                    // 5% at lv1, +2%/level
                    PassiveManaRegenBonus += value / 100f;
                    break;

                case "critical_mastery":
                    // 10% at lv1, +5%/level
                    PassiveCritDamageBonus += value / 100f;
                    break;
            }
        }

        ApplyPassivesToSystems();
    }

    private void ApplyPassivesToSystems()
    {
        // Apply defense bonus (additive on top of 1.0 base)
        if (healthSystem != null && PassiveDefenseBonus > 0f)
        {
            // Only apply passive defense if no active buffs are overriding
            // ActiveBuffTracker handles its own defense multiplier
            // We add passive on top: base 1.0 + passive bonus
            var buffTracker = GetComponent<ActiveBuffTracker>();
            float buffDefense = buffTracker != null ? buffTracker.TotalDefenseMultiplier : 1f;
            healthSystem.SetDefenseMultiplier(buffDefense + PassiveDefenseBonus);
        }

        // Apply mana regen bonus
        if (manaSystem != null && baseRegenCaptured)
        {
            float boostedRegen = baseRegenRate * (1f + PassiveManaRegenBonus);
            manaSystem.SetRegenRate(boostedRegen);
        }
    }
}
