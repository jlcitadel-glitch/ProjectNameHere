using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Component attached to the player that executes skills.
/// Manages hotbar bindings, cooldowns, and mana costs.
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    [Header("Hotbar Configuration")]
    [Tooltip("Number of hotbar slots")]
    [SerializeField] private int hotbarSlots = 6;

    [Header("References")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private HealthSystem healthSystem;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference skill1Action;
    [SerializeField] private InputActionReference skill2Action;
    [SerializeField] private InputActionReference skill3Action;
    [SerializeField] private InputActionReference skill4Action;
    [SerializeField] private InputActionReference skill5Action;
    [SerializeField] private InputActionReference skill6Action;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool logSkillUse = true;

    // Runtime state
    private string[] hotbarSkillIds;
    private SkillCooldownTracker cooldownTracker;
    private Animator animator;
    private bool isCasting;
    private float castEndTime;
    private string pendingSkillId;

    // Properties
    public int HotbarSlots => hotbarSlots;
    public bool IsCasting => isCasting;

    // Events
    public event Action<int, string> OnHotbarChanged;
    public event Action<string, SkillInstance> OnSkillUsed;
    public event Action<string> OnSkillReady;
    public event Action<string> OnSkillFailed;
    public event Action OnCastStarted;
    public event Action OnCastCompleted;
    public event Action OnCastCancelled;

    private void Awake()
    {
        hotbarSkillIds = new string[hotbarSlots];
        cooldownTracker = gameObject.AddComponent<SkillCooldownTracker>();
        animator = GetComponent<Animator>();

        if (manaSystem == null)
            manaSystem = GetComponent<ManaSystem>();

        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        EnableInputActions();
        cooldownTracker.OnCooldownEnded += HandleCooldownEnded;
    }

    private void OnDisable()
    {
        DisableInputActions();
        cooldownTracker.OnCooldownEnded -= HandleCooldownEnded;
    }

    private void EnableInputActions()
    {
        if (skill1Action?.action != null) { skill1Action.action.Enable(); skill1Action.action.performed += OnSkill1; }
        if (skill2Action?.action != null) { skill2Action.action.Enable(); skill2Action.action.performed += OnSkill2; }
        if (skill3Action?.action != null) { skill3Action.action.Enable(); skill3Action.action.performed += OnSkill3; }
        if (skill4Action?.action != null) { skill4Action.action.Enable(); skill4Action.action.performed += OnSkill4; }
        if (skill5Action?.action != null) { skill5Action.action.Enable(); skill5Action.action.performed += OnSkill5; }
        if (skill6Action?.action != null) { skill6Action.action.Enable(); skill6Action.action.performed += OnSkill6; }
    }

    private void DisableInputActions()
    {
        if (skill1Action?.action != null) { skill1Action.action.performed -= OnSkill1; }
        if (skill2Action?.action != null) { skill2Action.action.performed -= OnSkill2; }
        if (skill3Action?.action != null) { skill3Action.action.performed -= OnSkill3; }
        if (skill4Action?.action != null) { skill4Action.action.performed -= OnSkill4; }
        if (skill5Action?.action != null) { skill5Action.action.performed -= OnSkill5; }
        if (skill6Action?.action != null) { skill6Action.action.performed -= OnSkill6; }
    }

    private void OnSkill1(InputAction.CallbackContext ctx) => UseSkill(0);
    private void OnSkill2(InputAction.CallbackContext ctx) => UseSkill(1);
    private void OnSkill3(InputAction.CallbackContext ctx) => UseSkill(2);
    private void OnSkill4(InputAction.CallbackContext ctx) => UseSkill(3);
    private void OnSkill5(InputAction.CallbackContext ctx) => UseSkill(4);
    private void OnSkill6(InputAction.CallbackContext ctx) => UseSkill(5);

    private void Update()
    {
        // Handle cast time completion
        if (isCasting && Time.time >= castEndTime)
        {
            CompleteCast();
        }
    }

    /// <summary>
    /// Uses the skill in the specified hotbar slot.
    /// </summary>
    public bool UseSkill(int hotbarIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= hotbarSlots)
            return false;

        string skillId = hotbarSkillIds[hotbarIndex];
        if (string.IsNullOrEmpty(skillId))
            return false;

        return UseSkillById(skillId);
    }

    /// <summary>
    /// Uses a skill by its ID.
    /// </summary>
    public bool UseSkillById(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        // Check if we're already casting
        if (isCasting)
        {
            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Cannot use {skillId}: already casting");
            OnSkillFailed?.Invoke(skillId);
            return false;
        }

        // Get skill instance
        var skillManager = SkillManager.Instance;
        if (skillManager == null)
        {
            Debug.LogError("[PlayerSkillController] SkillManager not found");
            return false;
        }

        var skillInstance = skillManager.GetLearnedSkill(skillId);
        if (skillInstance == null)
        {
            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Skill not learned: {skillId}");
            OnSkillFailed?.Invoke(skillId);
            return false;
        }

        // Handle toggle skills
        if (skillInstance.SkillType == SkillType.Toggle)
        {
            return ToggleSkill(skillInstance);
        }

        // Check cooldown
        if (cooldownTracker.IsOnCooldown(skillId))
        {
            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Skill on cooldown: {skillId} ({cooldownTracker.GetRemainingCooldown(skillId):F1}s)");
            OnSkillFailed?.Invoke(skillId);
            return false;
        }

        // Check mana cost
        float manaCost = skillInstance.GetManaCost();
        if (manaSystem != null && !manaSystem.CanAfford(manaCost))
        {
            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Not enough mana for {skillId}: need {manaCost}, have {manaSystem.CurrentMana}");
            OnSkillFailed?.Invoke(skillId);
            return false;
        }

        // Handle passive skills (should not be usable)
        if (skillInstance.SkillType == SkillType.Passive)
        {
            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Cannot use passive skill: {skillId}");
            OnSkillFailed?.Invoke(skillId);
            return false;
        }

        // Check cast time
        float castTime = skillInstance.skillData?.castTime ?? 0f;
        if (castTime > 0f)
        {
            StartCast(skillInstance, castTime);
            return true;
        }

        // Execute immediately
        return ExecuteSkill(skillInstance);
    }

    private void StartCast(SkillInstance skillInstance, float castTime)
    {
        isCasting = true;
        castEndTime = Time.time + castTime;
        pendingSkillId = skillInstance.SkillId;

        // Play cast animation
        if (animator != null && !string.IsNullOrEmpty(skillInstance.skillData.animationTrigger))
        {
            animator.SetTrigger(skillInstance.skillData.animationTrigger + "_Cast");
        }

        if (logSkillUse)
            Debug.Log($"[PlayerSkillController] Casting {skillInstance.SkillName} ({castTime}s)");

        OnCastStarted?.Invoke();
    }

    private void CompleteCast()
    {
        isCasting = false;
        string skillId = pendingSkillId;
        pendingSkillId = null;

        if (!string.IsNullOrEmpty(skillId))
        {
            var skillInstance = SkillManager.Instance?.GetLearnedSkill(skillId);
            if (skillInstance != null)
            {
                ExecuteSkill(skillInstance);
            }
        }

        OnCastCompleted?.Invoke();
    }

    /// <summary>
    /// Cancels the current cast.
    /// </summary>
    public void CancelCast()
    {
        if (!isCasting) return;

        isCasting = false;
        pendingSkillId = null;

        if (logSkillUse)
            Debug.Log("[PlayerSkillController] Cast cancelled");

        OnCastCancelled?.Invoke();
    }

    private bool ExecuteSkill(SkillInstance skillInstance)
    {
        string skillId = skillInstance.SkillId;
        float manaCost = skillInstance.GetManaCost();
        float cooldown = skillInstance.GetCooldown();

        // Spend mana
        if (manaSystem != null && manaCost > 0)
        {
            if (!manaSystem.SpendMana(manaCost))
            {
                OnSkillFailed?.Invoke(skillId);
                return false;
            }
        }

        // Start cooldown
        if (cooldown > 0)
        {
            cooldownTracker.StartCooldown(skillId, cooldown);
        }

        // Play animation
        if (animator != null && !string.IsNullOrEmpty(skillInstance.skillData?.animationTrigger))
        {
            animator.SetTrigger(skillInstance.skillData.animationTrigger);
        }

        // Play sound
        if (audioSource != null && skillInstance.skillData?.castSound != null)
        {
            audioSource.PlayOneShot(skillInstance.skillData.castSound);
        }

        // Spawn skill prefab
        if (skillInstance.skillData?.skillPrefab != null)
        {
            SpawnSkillPrefab(skillInstance);
        }

        // Apply skill effects
        ApplySkillEffects(skillInstance);

        if (logSkillUse)
            Debug.Log($"[PlayerSkillController] Used skill: {skillInstance.SkillName} (Lv.{skillInstance.currentLevel})");

        OnSkillUsed?.Invoke(skillId, skillInstance);
        return true;
    }

    private bool ToggleSkill(SkillInstance skillInstance)
    {
        if (skillInstance.isActive)
        {
            // Deactivate
            skillInstance.isActive = false;

            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Deactivated toggle skill: {skillInstance.SkillName}");

            OnSkillUsed?.Invoke(skillInstance.SkillId, skillInstance);
            return true;
        }
        else
        {
            // Check mana to activate
            float manaCost = skillInstance.GetManaCost();
            if (manaSystem != null && !manaSystem.CanAfford(manaCost))
            {
                OnSkillFailed?.Invoke(skillInstance.SkillId);
                return false;
            }

            // Activate
            skillInstance.isActive = true;

            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Activated toggle skill: {skillInstance.SkillName}");

            OnSkillUsed?.Invoke(skillInstance.SkillId, skillInstance);
            return true;
        }
    }

    private void SpawnSkillPrefab(SkillInstance skillInstance)
    {
        var prefab = skillInstance.skillData.skillPrefab;
        if (prefab == null) return;

        // Spawn at player position, facing direction
        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        // Adjust for 2D - check sprite renderer for facing direction
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.flipX)
        {
            spawnRot = Quaternion.Euler(0, 180, 0);
        }

        var instance = Instantiate(prefab, spawnPos, spawnRot);

        // Pass skill data to the spawned prefab if it has a handler
        var handler = instance.GetComponent<ISkillEffectHandler>();
        handler?.Initialize(skillInstance, gameObject);
    }

    private void ApplySkillEffects(SkillInstance skillInstance)
    {
        if (skillInstance.skillData?.effects == null) return;

        foreach (var effectData in skillInstance.skillData.effects)
        {
            if (effectData == null) continue;

            switch (effectData.effectType)
            {
                case SkillEffectData.EffectType.Heal:
                    if (healthSystem != null)
                    {
                        float healAmount = effectData.GetValue(skillInstance.currentLevel);
                        healthSystem.Heal(healAmount);
                    }
                    break;

                case SkillEffectData.EffectType.Buff:
                    // Apply buff effects (would integrate with a stat system)
                    ApplyBuff(skillInstance, effectData);
                    break;

                // Other effect types handled by spawned prefabs or separate systems
            }
        }
    }

    private void ApplyBuff(SkillInstance skillInstance, SkillEffectData effectData)
    {
        // Buff implementation would integrate with a stat modifier system
        float duration = effectData.GetDuration(skillInstance.currentLevel);

        if (logSkillUse)
            Debug.Log($"[PlayerSkillController] Applied buff: {effectData.effectId} for {duration}s");
    }

    private void HandleCooldownEnded(string skillId)
    {
        OnSkillReady?.Invoke(skillId);
    }

    /// <summary>
    /// Sets a skill in the hotbar.
    /// </summary>
    public void SetHotbarSkill(int index, string skillId)
    {
        if (index < 0 || index >= hotbarSlots)
            return;

        string previousSkillId = hotbarSkillIds[index];
        hotbarSkillIds[index] = skillId;

        OnHotbarChanged?.Invoke(index, skillId);

        if (logSkillUse)
            Debug.Log($"[PlayerSkillController] Hotbar slot {index}: {previousSkillId ?? "empty"} -> {skillId ?? "empty"}");
    }

    /// <summary>
    /// Gets the skill ID in a hotbar slot.
    /// </summary>
    public string GetHotbarSkill(int index)
    {
        if (index < 0 || index >= hotbarSlots)
            return null;

        return hotbarSkillIds[index];
    }

    /// <summary>
    /// Gets all hotbar skill IDs.
    /// </summary>
    public string[] GetHotbarSkills()
    {
        return (string[])hotbarSkillIds.Clone();
    }

    /// <summary>
    /// Gets the cooldown tracker.
    /// </summary>
    public SkillCooldownTracker GetCooldownTracker()
    {
        return cooldownTracker;
    }

    /// <summary>
    /// Checks if a skill is ready to use.
    /// </summary>
    public bool IsSkillReady(string skillId)
    {
        if (cooldownTracker.IsOnCooldown(skillId))
            return false;

        var skillInstance = SkillManager.Instance?.GetLearnedSkill(skillId);
        if (skillInstance == null)
            return false;

        if (manaSystem != null && !manaSystem.CanAfford(skillInstance.GetManaCost()))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the remaining cooldown for a skill.
    /// </summary>
    public float GetCooldownRemaining(string skillId)
    {
        return cooldownTracker.GetRemainingCooldown(skillId);
    }

    /// <summary>
    /// Saves hotbar configuration.
    /// </summary>
    public string[] SaveHotbar()
    {
        return GetHotbarSkills();
    }

    /// <summary>
    /// Loads hotbar configuration.
    /// </summary>
    public void LoadHotbar(string[] skillIds)
    {
        if (skillIds == null) return;

        for (int i = 0; i < hotbarSlots && i < skillIds.Length; i++)
        {
            hotbarSkillIds[i] = skillIds[i];
        }
    }
}

/// <summary>
/// Interface for skill effect prefabs to receive initialization data.
/// </summary>
public interface ISkillEffectHandler
{
    void Initialize(SkillInstance skillInstance, GameObject caster);
}
