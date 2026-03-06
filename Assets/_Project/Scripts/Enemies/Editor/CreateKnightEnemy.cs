using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Creates the Knight/Guard enemy — the first humanoid LPC enemy type.
/// Creates: EnemyAttackData, EnemyData (with appearance config), AnimatorController, Prefab.
/// Run via Tools > Create Knight Enemy.
/// </summary>
public static class CreateKnightEnemy
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Enemies";
    private const string AnimDir = "Assets/_Project/Animations/Enemies";
    private const string DataDir = "Assets/_Project/ScriptableObjects/Enemies";
    private const string BodyPartsDir = "Assets/_Project/ScriptableObjects/Character/BodyParts";
    private const string AppearanceDir = "Assets/_Project/ScriptableObjects/Character/Appearances";

    [MenuItem("Tools/Create Knight Enemy")]
    public static void Create()
    {
        EnsureDirectory(PrefabDir);
        EnsureDirectory(AnimDir);
        EnsureDirectory($"{DataDir}/Types");
        EnsureDirectory($"{DataDir}/Attacks");
        EnsureDirectory($"{DataDir}/DesignCards");
        EnsureDirectory(AppearanceDir);

        // 1. Create the attack data
        var swordSweep = CreateAttackData();

        // 2. Create the appearance config
        var appearance = CreateAppearanceConfig();

        // 3. Find the frame map
        var frameMap = AssetDatabase.LoadAssetAtPath<AnimationStateFrameMap>(
            "Assets/_Project/ScriptableObjects/Character/LPCSideViewFrameMap.asset");

        // 4. Create the enemy data
        var enemyData = CreateEnemyData(swordSweep, appearance, frameMap);

        // 5. Create the animator controller
        var animator = CreateAnimatorController();

        // 6. Create the prefab
        CreatePrefab(enemyData, animator);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateKnightEnemy] Knight/Guard enemy created successfully.");
    }

    private static EnemyAttackData CreateAttackData()
    {
        string path = $"{DataDir}/Attacks/KnightSwordSweep.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyAttackData>(path);
        if (existing != null) return existing;

        var attack = ScriptableObject.CreateInstance<EnemyAttackData>();
        attack.attackName = "Sword Sweep";
        attack.baseDamage = 15f;
        attack.knockbackForce = 6f;
        attack.knockbackDirection = new Vector2(1f, 0.3f);

        // Design card step 7: 0.4s wind-up, 0.3s active, 0.6s recovery
        attack.windUpDuration = 0.4f;
        attack.activeDuration = 0.3f;
        attack.recoveryDuration = 0.6f;

        attack.hitboxSize = new Vector2(1.5f, 1.2f);
        attack.hitboxOffset = new Vector2(1.0f, 0f);
        attack.targetLayers = LayerMask.GetMask("Player");

        attack.isProjectile = false;
        attack.minRange = 0f;
        attack.maxRange = 2f;
        attack.isParryable = true; // Design card step 3: parry is the primary counter
        attack.animationTrigger = "Attack";

        AssetDatabase.CreateAsset(attack, path);
        Debug.Log($"[CreateKnightEnemy] Created attack: {path}");
        return attack;
    }

    private static CharacterAppearanceConfig CreateAppearanceConfig()
    {
        string path = $"{AppearanceDir}/KnightAppearance.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CharacterAppearanceConfig>(path);
        if (existing != null) return existing;

        var config = ScriptableObject.CreateInstance<CharacterAppearanceConfig>();
        config.configId = "knight_guard";
        config.displayName = "Knight Guard";
        config.bodyType = "male";

        // Dark iron tints for a menacing guard
        config.skinTint = new Color(0.85f, 0.72f, 0.6f, 1f);
        config.hairTint = new Color(0.2f, 0.15f, 0.1f, 1f);
        config.armorPrimaryTint = new Color(0.4f, 0.4f, 0.45f, 1f);
        config.armorSecondaryTint = new Color(0.3f, 0.25f, 0.2f, 1f);

        // Assign body parts — look up existing BodyPartData assets
        AssignPart(config, BodyPartSlot.Body, "body_body_color_male");
        AssignPart(config, BodyPartSlot.Torso, "chainmail_chainmail_male");
        AssignPart(config, BodyPartSlot.Legs, "legs_armour_male");
        AssignPart(config, BodyPartSlot.Feet, "shoes_armour_male");
        AssignPart(config, BodyPartSlot.WeaponFront, "weapon_longsword_male");
        AssignPart(config, BodyPartSlot.Hat, "hat_armet_male");

        AssetDatabase.CreateAsset(config, path);
        Debug.Log($"[CreateKnightEnemy] Created appearance: {path}");
        return config;
    }

    private static void AssignPart(CharacterAppearanceConfig config, BodyPartSlot slot, string assetName)
    {
        string slotFolder = slot switch
        {
            BodyPartSlot.Body => "Body",
            BodyPartSlot.Head => "Head",
            BodyPartSlot.Hair => "Hair",
            BodyPartSlot.Torso => "Torso",
            BodyPartSlot.Legs => "Legs",
            BodyPartSlot.Feet => "Feet",
            BodyPartSlot.Gloves => "Gloves",
            BodyPartSlot.Hat => "Hat",
            BodyPartSlot.WeaponFront => "WeaponFront",
            BodyPartSlot.WeaponBehind => "WeaponBehind",
            BodyPartSlot.Cape => "Cape",
            BodyPartSlot.Accessories => "Accessories",
            BodyPartSlot.Shield => "Shield",
            _ => slot.ToString()
        };

        string partPath = $"{BodyPartsDir}/{slotFolder}/{assetName}.asset";
        var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(partPath);
        if (part != null)
        {
            config.SetPart(slot, part);
            Debug.Log($"[CreateKnightEnemy] Assigned {slot}: {assetName}");
        }
        else
        {
            Debug.LogWarning($"[CreateKnightEnemy] BodyPartData not found: {partPath}");
        }
    }

    private static EnemyData CreateEnemyData(EnemyAttackData attack,
        CharacterAppearanceConfig appearance, AnimationStateFrameMap frameMap)
    {
        string path = $"{DataDir}/Types/KnightData.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = "Knight Guard";
        data.enemyType = EnemyType.GroundPatrol;

        // Step 1 (Tank): high HP, moderate damage
        data.maxHealth = 80f;
        data.invulnerabilityDuration = 0.15f;

        // Step 2 (Low mobility): slow patrol, moderate chase
        data.moveSpeed = 1.5f;
        data.chaseSpeed = 3f;

        // Contact damage
        data.contactDamage = 8f;
        data.contactKnockbackForce = 4f;

        // Step 4 (Delayed threat): moderate detection range, delayed response
        data.detectionType = DetectionType.Radius;
        data.detectionRange = 7f;
        data.detectionAngle = 60f;
        data.loseAggroRange = 12f;

        // Combat
        data.attacks = new EnemyAttackData[] { attack };
        data.attackRange = 1.8f;
        data.attackCooldown = 1.2f; // Step 7: 1.2s between attacks

        // Step 1 (Tank): resistant to knockback, short stun
        data.knockbackResistance = 0.6f;
        data.stunDuration = 0.3f;

        // Step 9 (Death consequence): high XP reward
        data.experienceValue = 35;
        data.dropChance = 0.4f;

        // Design card axes
        data.combatRole = CombatRole.Tank;
        data.threatClock = ThreatClock.Delayed;
        data.axisAggression = 2;
        data.axisMobility = 2;
        data.axisRange = 1;
        data.axisPredictability = 4;
        data.axisPersistence = 4;

        // Appearance
        data.appearanceConfig = appearance;
        data.appearanceFrameMap = frameMap;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[CreateKnightEnemy] Created enemy data: {path}");
        return data;
    }

    private static AnimatorController CreateAnimatorController()
    {
        string path = $"{AnimDir}/Knight_AC.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        // Create a simple animator controller with states matching the frame map.
        // The LayeredSpriteController doesn't use animation clips — it reads the
        // Animator state name and uses the frame map to drive sprite frames.
        // So the clips can be empty placeholders.
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Create empty animation clips as state placeholders
        var idleClip = CreateEmptyClip($"{AnimDir}/Knight_Idle.anim", true);
        var runClip = CreateEmptyClip($"{AnimDir}/Knight_Run.anim", true);
        var attackClip = CreateEmptyClip($"{AnimDir}/Knight_Attack.anim", false, 1.3f);
        var hurtClip = CreateEmptyClip($"{AnimDir}/Knight_Hurt.anim", false, 0.3f);
        var deathClip = CreateEmptyClip($"{AnimDir}/Knight_Death.anim", false, 1.0f);

        // Create states
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;
        var runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;
        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;
        var hurtState = rootStateMachine.AddState("Hurt");
        hurtState.motion = hurtClip;
        var deathState = rootStateMachine.AddState("Death");
        deathState.motion = deathClip;

        rootStateMachine.defaultState = idleState;

        // Transitions: Idle <-> Run based on Speed
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.1f;

        var runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.1f;

        // Any -> Attack (trigger)
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.05f;

        // Attack -> Idle (exit time)
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0.1f;

        // Any -> Hurt (trigger)
        var anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        anyToHurt.duration = 0.05f;

        // Hurt -> Idle (exit time)
        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;
        hurtToIdle.duration = 0.1f;

        // Any -> Death (trigger)
        var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.05f;

        Debug.Log($"[CreateKnightEnemy] Created animator controller: {path}");
        return controller;
    }

    private static AnimationClip CreateEmptyClip(string path, bool loop, float length = 1f)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Set clip length by adding a single keyframe at the desired time
        var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(length, 0f));
        clip.SetCurve("", typeof(GameObject), "m_IsActive", curve);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void CreatePrefab(EnemyData data, AnimatorController animController)
    {
        string path = $"{PrefabDir}/Knight.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            Debug.Log($"[CreateKnightEnemy] Prefab already exists: {path}");
            return;
        }

        // Create the root GameObject
        var go = new GameObject("Knight");

        // SpriteRenderer (will be disabled by EnemyAppearance, but needed for collider bounds)
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 10;
        // Material: built-in Sprites-Default
        sr.material = new Material(Shader.Find("Sprites/Default"));

        // Rigidbody2D
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Collider — humanoid sized
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.6f);
        col.offset = new Vector2(0f, 0.8f);

        // Animator
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animController;
        animator.applyRootMotion = false;

        // EnemyController (references EnemyData)
        var controller = go.AddComponent<EnemyController>();
        controller.SetData(data);

        // HealthSystem
        go.AddComponent<HealthSystem>();

        // Movement
        go.AddComponent<GroundPatrolMovement>();

        // Sensors
        go.AddComponent<EnemySensors>();

        // Combat
        go.AddComponent<EnemyCombat>();

        // Child transforms for ground/wall/ledge detection
        CreateChild(go, "GroundCheck", new Vector3(0f, -0.05f, 0f));
        CreateChild(go, "WallCheck", new Vector3(0.5f, 0.5f, 0f));
        CreateChild(go, "LedgeCheck", new Vector3(0.5f, -0.1f, 0f));
        CreateChild(go, "AttackOrigin", new Vector3(0.8f, 0.8f, 0f));

        // Set layer and tag
        go.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            go.layer = enemyLayer;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[CreateKnightEnemy] Created prefab: {path}");
    }

    private static void CreateChild(GameObject parent, string name, Vector3 localPos)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPos;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
