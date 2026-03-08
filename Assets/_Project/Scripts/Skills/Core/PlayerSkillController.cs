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
    private SkillExecutor skillExecutor;
    private bool isCasting;
    private float castEndTime;
    private string pendingSkillId;

    // Fallback input actions created at runtime when serialized references are null
    private InputAction[] fallbackSkillActions;

    // Properties
    public int HotbarSlots => hotbarSlots;
    public bool IsCasting => isCasting;

    /// <summary>
    /// Returns the InputAction bound to the given hotbar slot, respecting rebinds.
    /// </summary>
    public InputAction GetHotbarAction(int index)
    {
        var refs = new[] { skill1Action, skill2Action, skill3Action, skill4Action, skill5Action, skill6Action };
        if (index >= 0 && index < refs.Length && refs[index]?.action != null)
            return refs[index].action;
        if (fallbackSkillActions != null && index >= 0 && index < fallbackSkillActions.Length)
            return fallbackSkillActions[index];
        return null;
    }

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
        skillExecutor = GetComponent<SkillExecutor>();

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

        if (SkillManager.Instance != null)
            SkillManager.Instance.OnSkillLearned += HandleSkillLearned;
    }

    private void OnDisable()
    {
        DisableInputActions();
        cooldownTracker.OnCooldownEnded -= HandleCooldownEnded;

        if (SkillManager.Instance != null)
            SkillManager.Instance.OnSkillLearned -= HandleSkillLearned;
    }

    private void EnableInputActions()
    {
        var refs = new[] { skill1Action, skill2Action, skill3Action, skill4Action, skill5Action, skill6Action };
        var handlers = new Action<InputAction.CallbackContext>[] { OnSkill1, OnSkill2, OnSkill3, OnSkill4, OnSkill5, OnSkill6 };
        var bindings = new[] { "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3", "<Keyboard>/4", "<Keyboard>/5", "<Keyboard>/6" };

        for (int i = 0; i < refs.Length && i < hotbarSlots; i++)
        {
            if (refs[i]?.action != null)
            {
                refs[i].action.Enable();
                refs[i].action.performed += handlers[i];
            }
            else
            {
                // Create fallback InputAction when serialized reference is null
                // (component was added at runtime, not from a prefab)
                if (fallbackSkillActions == null)
                    fallbackSkillActions = new InputAction[hotbarSlots];

                fallbackSkillActions[i] = new InputAction($"Skill{i + 1}", InputActionType.Button, bindings[i]);
                fallbackSkillActions[i].Enable();
                fallbackSkillActions[i].performed += handlers[i];
            }
        }
    }

    private void DisableInputActions()
    {
        var refs = new[] { skill1Action, skill2Action, skill3Action, skill4Action, skill5Action, skill6Action };
        var handlers = new Action<InputAction.CallbackContext>[] { OnSkill1, OnSkill2, OnSkill3, OnSkill4, OnSkill5, OnSkill6 };

        for (int i = 0; i < refs.Length && i < hotbarSlots; i++)
        {
            if (refs[i]?.action != null)
                refs[i].action.performed -= handlers[i];

            if (fallbackSkillActions != null && i < fallbackSkillActions.Length && fallbackSkillActions[i] != null)
            {
                fallbackSkillActions[i].performed -= handlers[i];
                fallbackSkillActions[i].Disable();
            }
        }
    }

    private void OnDestroy()
    {
        if (fallbackSkillActions != null)
        {
            foreach (var action in fallbackSkillActions)
                action?.Dispose();
            fallbackSkillActions = null;
        }
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
        if (skillInstance.skillData?.castSound != null)
        {
            SFXManager.PlayOneShot(audioSource, skillInstance.skillData.castSound);
        }

        // Execute skill via SkillExecutor (handles all 19 skills inline)
        if (skillExecutor != null)
        {
            skillExecutor.Execute(skillInstance);
        }
        else
        {
            // Fallback to original path (prefab + effects)
            if (skillInstance.skillData?.skillPrefab != null)
                SpawnSkillPrefab(skillInstance);
            ApplySkillEffects(skillInstance);
        }

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

    private void HandleSkillLearned(SkillInstance instance)
    {
        // Only auto-assign usable skills (Active, Buff, Toggle) to the first empty hotbar slot
        if (instance.SkillType == SkillType.Passive)
            return;

        int emptySlot = FindFirstEmptySlot();
        if (emptySlot >= 0)
        {
            SetHotbarSkill(emptySlot, instance.SkillId);

            if (logSkillUse)
                Debug.Log($"[PlayerSkillController] Auto-assigned {instance.SkillName} to hotbar slot {emptySlot + 1}");
        }
    }

    /// <summary>
    /// Finds the first empty hotbar slot. Returns -1 if all full.
    /// </summary>
    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < hotbarSlots; i++)
        {
            if (string.IsNullOrEmpty(hotbarSkillIds[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Checks whether a skill is already assigned to any hotbar slot.
    /// </summary>
    public bool IsSkillOnHotbar(string skillId)
    {
        for (int i = 0; i < hotbarSlots; i++)
        {
            if (hotbarSkillIds[i] == skillId)
                return true;
        }
        return false;
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
