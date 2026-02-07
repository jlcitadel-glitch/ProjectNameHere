using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main combat controller handling attacks, state machine, and weapon management.
/// Attach to Player alongside PlayerControllerScript.
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponData equippedMelee;
    [SerializeField] private WeaponData equippedRanged;
    [SerializeField] private WeaponData equippedMagic;

    [Header("Direction Detection")]
    [Tooltip("Y-axis threshold to detect up/down input")]
    [SerializeField] private float directionThreshold = 0.7f;

    [Header("Hitbox Settings")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private GameObject hitboxPrefab;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string defaultAttackTrigger = "Attack1";

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logDebugMessages = true;
    [SerializeField] private Color hitboxDebugColor = new Color(1f, 0f, 0f, 0.5f);

    // Current state
    private CombatState currentState = CombatState.Idle;
    private AttackData currentAttack;
    private float stateTimer;
    private float comboWindowTimer;
    private bool comboQueued;
    private AttackData queuedComboAttack;

    // Input tracking
    private Vector2 moveInput;
    private WeaponType activeWeaponType = WeaponType.Melee;

    // Component references
    private Rigidbody2D rb;
    private ManaSystem manaSystem;
    private PlayerControllerScript playerController;
    private StatSystem statSystem;
    private AudioSource audioSource;

    // Active hitbox reference
    private GameObject activeHitbox;

    // Events
    public event Action<AttackData> OnAttackStarted;
    public event Action<AttackData> OnAttackHit;
    public event Action<AttackData> OnAttackEnded;
    public event Action<WeaponType> OnWeaponSwitched;

    // Public accessors
    public bool IsAttacking => currentState != CombatState.Idle;
    public CombatState CurrentState => currentState;
    public AttackData CurrentAttack => currentAttack;
    public WeaponType ActiveWeaponType => activeWeaponType;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        manaSystem = GetComponent<ManaSystem>();
        playerController = GetComponent<PlayerControllerScript>();
        statSystem = GetComponent<StatSystem>();

        // Ensure AudioSource for combat sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Auto-find or create attack origin
        if (attackOrigin == null)
        {
            attackOrigin = transform.Find("AttackOrigin");
            if (attackOrigin == null)
            {
                GameObject origin = new GameObject("AttackOrigin");
                origin.transform.SetParent(transform);
                origin.transform.localPosition = Vector3.zero;
                attackOrigin = origin.transform;
            }
        }
    }

    private void Start()
    {
        // Log setup status
        if (logDebugMessages)
        {
            Debug.Log($"[CombatController] Initialized on {gameObject.name}");
            Debug.Log($"[CombatController] Melee Weapon: {(equippedMelee != null ? equippedMelee.weaponName : "None")}");
            Debug.Log($"[CombatController] Ranged Weapon: {(equippedRanged != null ? equippedRanged.weaponName : "None")}");
            Debug.Log($"[CombatController] Magic Weapon: {(equippedMagic != null ? equippedMagic.weaponName : "None")}");
            Debug.Log($"[CombatController] Animator: {(animator != null ? "Found" : "Not Found")}");
        }
    }

    private void Update()
    {
        UpdateStateMachine();
        UpdateComboWindow();
    }

    private void UpdateStateMachine()
    {
        if (currentState == CombatState.Idle)
            return;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            TransitionToNextState();
        }
    }

    private void TransitionToNextState()
    {
        switch (currentState)
        {
            case CombatState.WindUp:
                EnterAttackingState();
                break;

            case CombatState.Attacking:
                DestroyActiveHitbox();
                EnterRecoveryState();
                break;

            case CombatState.Recovery:
                // Check for queued combo
                if (comboQueued && queuedComboAttack != null)
                {
                    comboQueued = false;
                    StartAttack(queuedComboAttack);
                }
                else
                {
                    EnterIdleState();
                }
                break;
        }
    }

    private void EnterIdleState()
    {
        if (currentAttack != null)
        {
            OnAttackEnded?.Invoke(currentAttack);
        }

        currentState = CombatState.Idle;
        currentAttack = null;
        stateTimer = 0f;
        comboQueued = false;
        queuedComboAttack = null;
    }

    private void EnterWindUpState(AttackData attack)
    {
        currentState = CombatState.WindUp;
        currentAttack = attack;
        stateTimer = attack.windUpDuration;

        // Skip wind-up if duration is 0
        if (stateTimer <= 0f)
        {
            EnterAttackingState();
        }
    }

    private void EnterAttackingState()
    {
        currentState = CombatState.Attacking;
        stateTimer = currentAttack.activeDuration;

        SpawnHitbox();

        // Play attack sound
        if (currentAttack.attackSound != null)
        {
            SFXManager.PlayOneShot(audioSource, currentAttack.attackSound);
        }

        OnAttackStarted?.Invoke(currentAttack);
    }

    private void EnterRecoveryState()
    {
        currentState = CombatState.Recovery;
        stateTimer = currentAttack.recoveryDuration;
        comboWindowTimer = currentAttack.comboWindowDuration;
    }

    private void UpdateComboWindow()
    {
        if (currentState == CombatState.Recovery && comboWindowTimer > 0f)
        {
            comboWindowTimer -= Time.deltaTime;
        }
    }

    #region Input Handling

    /// <summary>
    /// Called from PlayerControllerScript when Attack input is received.
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (logDebugMessages)
            Debug.Log($"[CombatController] Attack input received! State: {currentState}");

        TryAttack();
    }

    /// <summary>
    /// Called from PlayerControllerScript to track move input for direction detection.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero;
        }
    }

    /// <summary>
    /// Called to switch between weapon types.
    /// </summary>
    public void OnWeaponSwitch(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // Can't switch while attacking
        if (IsAttacking)
            return;

        CycleWeapon();
    }

    /// <summary>
    /// Cycles through equipped weapons.
    /// </summary>
    public void CycleWeapon()
    {
        WeaponType newType = activeWeaponType;

        // Try to find next equipped weapon
        for (int i = 0; i < 3; i++)
        {
            newType = (WeaponType)(((int)newType + 1) % 3);

            if (GetWeapon(newType) != null)
            {
                activeWeaponType = newType;
                OnWeaponSwitched?.Invoke(activeWeaponType);
                return;
            }
        }
    }

    /// <summary>
    /// Directly set the active weapon type.
    /// </summary>
    public void SetWeaponType(WeaponType type)
    {
        if (GetWeapon(type) != null)
        {
            activeWeaponType = type;
            OnWeaponSwitched?.Invoke(activeWeaponType);
        }
    }

    #endregion

    #region Attack Logic

    private void TryAttack()
    {
        // Handle combo during recovery window
        if (currentState == CombatState.Recovery && comboWindowTimer > 0f)
        {
            if (currentAttack != null && currentAttack.comboNextAttack != null)
            {
                // Check mana for combo attack
                if (CanAffordAttack(currentAttack.comboNextAttack))
                {
                    comboQueued = true;
                    queuedComboAttack = currentAttack.comboNextAttack;
                }
            }
            return;
        }

        // Can't attack if already attacking
        if (IsAttacking)
        {
            if (logDebugMessages)
                Debug.Log($"[CombatController] Cannot attack - already attacking");
            return;
        }

        WeaponData weapon = GetWeapon(activeWeaponType);
        if (weapon == null)
        {
            if (logDebugMessages)
                Debug.LogWarning($"[CombatController] No weapon equipped for type: {activeWeaponType}");
            return;
        }

        // Always use Forward for combo chain (directional attacks disabled for now)
        AttackDirection dir = AttackDirection.Forward;
        bool grounded = IsGrounded();

        if (logDebugMessages)
            Debug.Log($"[CombatController] Attempting {dir} attack with {weapon.weaponName} (Grounded: {grounded})");

        AttackData attack = weapon.GetAttack(dir, grounded);
        if (attack == null)
        {
            if (logDebugMessages)
                Debug.LogWarning($"[CombatController] No attack data found for direction: {dir}");
            return;
        }

        // Check aerial restriction
        if (!grounded && !attack.canUseInAir)
            return;

        // Check mana cost
        if (!CanAffordAttack(attack))
            return;

        // Spend mana
        SpendManaForAttack(attack);

        StartAttack(attack);
    }

    private void StartAttack(AttackData attack)
    {
        currentAttack = attack;
        comboQueued = false;
        queuedComboAttack = null;

        if (logDebugMessages)
            Debug.Log($"[CombatController] Starting attack: {attack.attackName} (Direction: {attack.direction})");

        // Trigger animation
        TriggerAttackAnimation(attack);

        if (attack.windUpDuration > 0f)
        {
            EnterWindUpState(attack);
        }
        else
        {
            EnterAttackingState();
        }
    }

    private void TriggerAttackAnimation(AttackData attack)
    {
        if (animator == null)
            return;

        string trigger = !string.IsNullOrEmpty(attack.animationTrigger)
            ? attack.animationTrigger
            : defaultAttackTrigger;

        if (HasAnimatorParameter(trigger))
        {
            if (logDebugMessages)
                Debug.Log($"[CombatController] Triggering animation: {trigger}");
            animator.SetTrigger(trigger);
        }
        else if (logDebugMessages)
        {
            Debug.LogWarning($"[CombatController] Animator parameter '{trigger}' not found. " +
                "Check AttackData.animationTrigger matches your Animator Controller parameters.");
        }
    }

    private bool HasAnimatorParameter(string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    private AttackDirection GetAttackDirection()
    {
        if (moveInput.y > directionThreshold)
            return AttackDirection.Up;
        if (moveInput.y < -directionThreshold)
            return AttackDirection.Down;
        return AttackDirection.Forward;
    }

    private WeaponData GetWeapon(WeaponType type)
    {
        return type switch
        {
            WeaponType.Melee => equippedMelee,
            WeaponType.Ranged => equippedRanged,
            WeaponType.Magic => equippedMagic,
            _ => null
        };
    }

    /// <summary>
    /// Gets the currently equipped weapon.
    /// </summary>
    public WeaponData GetActiveWeapon()
    {
        return GetWeapon(activeWeaponType);
    }

    #endregion

    #region Mana

    private bool CanAffordAttack(AttackData attack)
    {
        if (attack.manaCost <= 0f)
            return true;

        if (manaSystem == null)
            return true; // No mana system, allow attack

        return manaSystem.CanAfford(attack.manaCost);
    }

    private void SpendManaForAttack(AttackData attack)
    {
        if (attack.manaCost <= 0f)
            return;

        manaSystem?.SpendMana(attack.manaCost);
    }

    #endregion

    #region Hitbox

    private void SpawnHitbox()
    {
        if (currentAttack == null)
            return;

        // Projectile attacks
        if (currentAttack.projectilePrefab != null)
        {
            SpawnProjectile();
            return;
        }

        // Melee hitbox
        SpawnMeleeHitbox();
    }

    private void SpawnMeleeHitbox()
    {
        Vector2 offset = CalculateHitboxOffset();
        Vector3 spawnPos = attackOrigin.position + (Vector3)offset;

        if (logDebugMessages)
            Debug.Log($"[CombatController] Spawning hitbox at {spawnPos}, size: {currentAttack.hitboxSize}");

        // Create hitbox GameObject
        GameObject hitboxObj;
        if (hitboxPrefab != null)
        {
            hitboxObj = Instantiate(hitboxPrefab, spawnPos, Quaternion.identity, transform);
        }
        else
        {
            hitboxObj = new GameObject("AttackHitbox");
            hitboxObj.transform.position = spawnPos;
            hitboxObj.transform.SetParent(transform);

            // Add visual indicator for debugging
            if (showDebugGizmos)
            {
                AddHitboxVisual(hitboxObj, currentAttack.hitboxSize);
            }
        }

        // Setup hitbox component
        AttackHitbox hitbox = hitboxObj.GetComponent<AttackHitbox>();
        if (hitbox == null)
        {
            hitbox = hitboxObj.AddComponent<AttackHitbox>();
        }

        hitbox.Initialize(currentAttack, this);
        activeHitbox = hitboxObj;

        // Spawn VFX
        if (currentAttack.hitboxVFXPrefab != null)
        {
            Instantiate(currentAttack.hitboxVFXPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void AddHitboxVisual(GameObject hitboxObj, Vector2 size)
    {
        // Create a child object for the visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(hitboxObj.transform);
        visual.transform.localPosition = Vector3.zero;

        // Add sprite renderer with a simple colored quad
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBoxSprite();
        sr.color = hitboxDebugColor;
        sr.sortingOrder = 100;

        // Scale to match hitbox size
        visual.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private Sprite CreateBoxSprite()
    {
        // Create a simple 1x1 white texture
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void SpawnProjectile()
    {
        Vector2 offset = CalculateHitboxOffset();
        Vector3 spawnPos = attackOrigin.position + (Vector3)offset;

        Vector2 direction = CalculateProjectileDirection();
        Quaternion rotation = Quaternion.FromToRotation(Vector2.right, direction);

        GameObject projectileObj = Instantiate(currentAttack.projectilePrefab, spawnPos, rotation);

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = projectileObj.AddComponent<Projectile>();
        }

        projectile.Initialize(currentAttack, direction, this);
    }

    private Vector2 CalculateHitboxOffset()
    {
        Vector2 offset = currentAttack.hitboxOffset;
        float facingDir = Mathf.Sign(transform.localScale.x);

        // Adjust offset based on direction
        switch (currentAttack.direction)
        {
            case AttackDirection.Forward:
                offset.x *= facingDir;
                break;
            case AttackDirection.Up:
                // Rotate offset 90 degrees
                offset = new Vector2(0f, Mathf.Abs(offset.x));
                break;
            case AttackDirection.Down:
                // Rotate offset -90 degrees
                offset = new Vector2(0f, -Mathf.Abs(offset.x));
                break;
        }

        return offset;
    }

    private Vector2 CalculateProjectileDirection()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);

        return currentAttack.direction switch
        {
            AttackDirection.Up => Vector2.up,
            AttackDirection.Down => Vector2.down,
            _ => new Vector2(facingDir, 0f)
        };
    }

    private void DestroyActiveHitbox()
    {
        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }
    }

    #endregion

    #region Player Integration

    /// <summary>
    /// Returns true if the player can move (not locked by attack).
    /// </summary>
    public bool CanMove()
    {
        if (!IsAttacking)
            return true;

        if (currentAttack != null && currentAttack.allowMovement)
            return true;

        return false;
    }

    /// <summary>
    /// Returns the movement speed multiplier during attack.
    /// </summary>
    public float GetMovementMultiplier()
    {
        if (!IsAttacking)
            return 1f;

        if (currentAttack != null && currentAttack.allowMovement)
            return currentAttack.movementMultiplier;

        return 0f;
    }

    private bool IsGrounded()
    {
        // Use PlayerControllerScript's grounded check if available
        if (playerController != null)
        {
            return playerController.GetIsGrounded();
        }

        // Fall back to simple raycast
        return Physics2D.Raycast(transform.position, Vector2.down, 0.1f, LayerMask.GetMask("Ground"));
    }

    /// <summary>
    /// Called when an attack hits a target.
    /// </summary>
    /// <summary>
    /// Returns the damage multiplier based on active weapon type, stats, and job class.
    /// Melee/Ranged use STR-based multiplier + attackModifier.
    /// Magic uses INT-based multiplier + magicModifier.
    /// </summary>
    public float GetDamageMultiplier()
    {
        float statMultiplier;
        float jobModifier = 1f;

        var currentJob = SkillManager.Instance != null ? SkillManager.Instance.CurrentJob : null;

        if (activeWeaponType == WeaponType.Magic)
        {
            statMultiplier = statSystem != null ? statSystem.SkillDamageMultiplier : 1f;
            if (currentJob != null)
                jobModifier = currentJob.magicModifier;
        }
        else
        {
            statMultiplier = statSystem != null ? statSystem.MeleeDamageMultiplier : 1f;
            if (currentJob != null)
                jobModifier = currentJob.attackModifier;
        }

        return statMultiplier * jobModifier;
    }

    /// <summary>
    /// Returns the crit chance from agility stats (0-1 range).
    /// </summary>
    public float GetCritChance()
    {
        return statSystem != null ? statSystem.CritChance : 0f;
    }

    public void ReportHit(AttackData attack, Collider2D target)
    {
        OnAttackHit?.Invoke(attack);

        // Play hit sound
        if (attack.hitSound != null)
        {
            SFXManager.PlayOneShot(audioSource, attack.hitSound);
        }

        // Handle pogo bounce for down attacks
        if (attack.pogoOnDownHit && attack.direction == AttackDirection.Down)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, attack.pogoForce);
            }
        }
    }

    #endregion

    #region Weapon Management

    /// <summary>
    /// Equips a weapon to the appropriate slot.
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        if (weapon == null)
            return;

        switch (weapon.weaponType)
        {
            case WeaponType.Melee:
                equippedMelee = weapon;
                break;
            case WeaponType.Ranged:
                equippedRanged = weapon;
                break;
            case WeaponType.Magic:
                equippedMagic = weapon;
                break;
        }
    }

    /// <summary>
    /// Checks if any weapon is equipped.
    /// </summary>
    public bool HasAnyWeapon()
    {
        return equippedMelee != null || equippedRanged != null || equippedMagic != null;
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        if (currentAttack == null && !Application.isPlaying)
            return;

        // Draw attack hitbox preview
        AttackData previewAttack = currentAttack;

        if (previewAttack == null)
        {
            // In editor, show equipped weapon's forward attack
            WeaponData weapon = GetWeapon(activeWeaponType);
            if (weapon != null)
                previewAttack = weapon.forwardAttack;
        }

        if (previewAttack == null)
            return;

        Transform origin = attackOrigin != null ? attackOrigin : transform;
        Vector2 offset = previewAttack.hitboxOffset;
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;

        switch (previewAttack.direction)
        {
            case AttackDirection.Forward:
                offset.x *= facingDir;
                break;
            case AttackDirection.Up:
                offset = new Vector2(0f, Mathf.Abs(offset.x));
                break;
            case AttackDirection.Down:
                offset = new Vector2(0f, -Mathf.Abs(offset.x));
                break;
        }

        Vector3 center = origin.position + (Vector3)offset;

        Gizmos.color = IsAttacking ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, previewAttack.hitboxSize);
    }

    #endregion
}
