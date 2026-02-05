using System;
using UnityEngine;

/// <summary>
/// Skill effect that applies temporary stat modifiers.
/// </summary>
public class BuffSkillEffect : BaseSkillEffect
{
    [Header("Buff Settings")]
    [Tooltip("Apply buff to caster (self-buff)")]
    [SerializeField] private bool applyToSelf = true;

    [Tooltip("Apply buff to allies in range")]
    [SerializeField] private bool applyToAllies = false;

    [Tooltip("Radius for ally buffs")]
    [SerializeField] private float buffRadius = 5f;

    [Tooltip("Layer mask for allies")]
    [SerializeField] private LayerMask allyLayers;

    [Header("Visual")]
    [Tooltip("Attach visual effect to buffed targets")]
    [SerializeField] private GameObject buffVisualPrefab;

    [Tooltip("Follow the buffed target")]
    [SerializeField] private bool followTarget = true;

    // Runtime
    private SkillEffectData buffData;
    private GameObject buffVisual;
    private Transform followTransform;

    protected override void OnInitialized()
    {
        buffData = GetEffectDataByType(SkillEffectData.EffectType.Buff);

        if (buffData == null && skillInstance?.skillData?.effects?.Length > 0)
        {
            // Use first effect if no specific buff data
            buffData = skillInstance.skillData.effects[0];
        }

        if (applyToSelf && caster != null)
        {
            ApplyBuffToTarget(caster);
            followTransform = caster.transform;
        }

        if (applyToAllies)
        {
            ApplyBuffToAllies();
        }

        // Spawn visual on caster
        if (buffVisualPrefab != null && caster != null)
        {
            buffVisual = Instantiate(buffVisualPrefab, caster.transform.position, Quaternion.identity);
            if (followTarget)
            {
                buffVisual.transform.SetParent(caster.transform);
            }
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Follow target if needed
        if (followTarget && followTransform != null)
        {
            transform.position = followTransform.position;
        }
    }

    private void ApplyBuffToTarget(GameObject target)
    {
        if (target == null || buffData == null) return;

        // In a full implementation, this would interface with a stat system
        // For now, we'll apply specific modifiers we can handle

        // Apply speed modifier if applicable
        var playerController = target.GetComponent<PlayerControllerScript>();
        if (playerController != null && buffData.speedModifier != 1f)
        {
            // Would need to add a method to PlayerControllerScript to handle speed modifiers
            Debug.Log($"[BuffSkillEffect] Applied speed modifier: {buffData.speedModifier}x for {duration}s");
        }

        Debug.Log($"[BuffSkillEffect] Applied buff to {target.name} for {duration}s");
    }

    private void ApplyBuffToAllies()
    {
        if (buffRadius <= 0) return;

        Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, buffRadius, allyLayers);

        foreach (var ally in allies)
        {
            if (ally != null && ally.gameObject != caster)
            {
                ApplyBuffToTarget(ally.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up buff visual if it exists and wasn't parented
        if (buffVisual != null && !followTarget)
        {
            Destroy(buffVisual);
        }

        // Remove buff effects would happen here
        Debug.Log("[BuffSkillEffect] Buff expired");
    }

    private void OnDrawGizmosSelected()
    {
        if (applyToAllies && buffRadius > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, buffRadius);
        }
    }
}
