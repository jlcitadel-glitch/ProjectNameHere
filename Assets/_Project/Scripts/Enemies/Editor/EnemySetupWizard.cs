using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor wizard for creating and repairing enemy prefabs and data assets.
/// </summary>
public class EnemySetupWizard : EditorWindow
{
    private string enemyName = "NewEnemy";
    private EnemyType enemyType = EnemyType.GroundPatrol;

    // Stats
    private float maxHealth = 30f;
    private float moveSpeed = 3f;
    private float chaseSpeed = 5f;
    private float contactDamage = 10f;

    // Detection
    private DetectionType detectionType = DetectionType.Radius;
    private float detectionRange = 6f;
    private float loseAggroRange = 10f;

    // Combat
    private float attackDamage = 10f;
    private float attackRange = 1.5f;
    private float attackCooldown = 1f;

    // Rewards
    private int experienceValue = 10;

    // References
    private Sprite enemySprite;

    // Repair
    private GameObject repairPrefab;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Enemy Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<EnemySetupWizard>("Enemy Setup Wizard");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Enemy Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // === REPAIR SECTION ===
        DrawRepairSection();

        EditorGUILayout.Space();
        DrawSeparator();
        EditorGUILayout.Space();

        // === CREATE SECTION ===
        DrawCreateSection();

        EditorGUILayout.EndScrollView();
    }

    #region Repair

    private void DrawRepairSection()
    {
        GUILayout.Label("Repair Existing Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans enemy prefabs for missing components and adds them without overwriting existing configuration.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Single prefab repair
        repairPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab to Repair", repairPrefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = repairPrefab != null;
        if (GUILayout.Button("Repair Selected Prefab"))
        {
            RepairPrefab(repairPrefab);
        }
        GUI.enabled = true;

        if (GUILayout.Button("Repair All Enemy Prefabs"))
        {
            RepairAllPrefabs();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RepairAllPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Enemies" });

        if (guids.Length == 0)
        {
            Debug.LogWarning("[EnemySetupWizard] No prefabs found in Assets/_Project/Prefabs/Enemies/");
            return;
        }

        int repairedCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.GetComponent<EnemyController>() != null)
            {
                if (RepairPrefab(prefab))
                    repairedCount++;
            }
        }

        Debug.Log($"[EnemySetupWizard] Repair complete. {repairedCount}/{guids.Length} prefabs were modified.");
    }

    /// <summary>
    /// Repairs a single enemy prefab by adding any missing required components.
    /// Returns true if the prefab was modified.
    /// </summary>
    private bool RepairPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[EnemySetupWizard] {prefab.name}: Not a project asset, cannot repair.");
            return false;
        }

        // Load the prefab contents for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        bool modified = false;

        EnemyController controller = prefabRoot.GetComponent<EnemyController>();
        if (controller == null)
        {
            Debug.LogWarning($"[EnemySetupWizard] {prefab.name}: No EnemyController found, skipping.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return false;
        }

        // Read the EnemyData to determine what type of enemy this is
        SerializedObject so = new SerializedObject(controller);
        EnemyData data = so.FindProperty("enemyData").objectReferenceValue as EnemyData;

        if (data == null)
        {
            Debug.LogWarning($"[EnemySetupWizard] {prefab.name}: No EnemyData assigned, skipping.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return false;
        }

        EnemyType type = data.enemyType;
        string report = $"[EnemySetupWizard] Repairing {prefab.name} (type: {type}):\n";
        int issueCount = 0;

        // --- HealthSystem ---
        if (prefabRoot.GetComponent<HealthSystem>() == null)
        {
            prefabRoot.AddComponent<HealthSystem>();
            report += "  + Added HealthSystem\n";
            modified = true;
            issueCount++;
        }

        // --- Rigidbody2D ---
        Rigidbody2D rb = prefabRoot.GetComponent<Rigidbody2D>();
        if (rb == null && type != EnemyType.Stationary)
        {
            rb = prefabRoot.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            if (type == EnemyType.Flying)
                rb.gravityScale = 0f;
            report += "  + Added Rigidbody2D\n";
            modified = true;
            issueCount++;
        }

        // --- Collider2D ---
        if (prefabRoot.GetComponent<Collider2D>() == null)
        {
            if (type == EnemyType.Flying)
            {
                CircleCollider2D col = prefabRoot.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
            }
            else
            {
                BoxCollider2D col = prefabRoot.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);
            }
            report += "  + Added Collider2D\n";
            modified = true;
            issueCount++;
        }

        // --- Movement component (based on EnemyType) ---
        BaseEnemyMovement existingMovement = prefabRoot.GetComponent<BaseEnemyMovement>();
        if (existingMovement == null && type != EnemyType.Stationary)
        {
            switch (type)
            {
                case EnemyType.GroundPatrol:
                    prefabRoot.AddComponent<GroundPatrolMovement>();
                    report += "  + Added GroundPatrolMovement\n";
                    break;
                case EnemyType.Flying:
                    prefabRoot.AddComponent<FlyingMovement>();
                    report += "  + Added FlyingMovement\n";
                    break;
            }
            modified = true;
            issueCount++;
        }
        else if (existingMovement != null)
        {
            // Verify the correct movement type is present
            bool wrongType = false;
            if (type == EnemyType.GroundPatrol && !(existingMovement is GroundPatrolMovement))
                wrongType = true;
            if (type == EnemyType.Flying && !(existingMovement is FlyingMovement))
                wrongType = true;

            if (wrongType)
            {
                report += $"  ! WARNING: Has {existingMovement.GetType().Name} but EnemyData says {type}. " +
                           "Remove the wrong component manually and re-run repair.\n";
                issueCount++;
            }
        }

        // --- EnemyCombat ---
        if (prefabRoot.GetComponent<EnemyCombat>() == null)
        {
            prefabRoot.AddComponent<EnemyCombat>();
            report += "  + Added EnemyCombat\n";
            modified = true;
            issueCount++;
        }

        // --- EnemySensors ---
        if (prefabRoot.GetComponent<EnemySensors>() == null)
        {
            prefabRoot.AddComponent<EnemySensors>();
            report += "  + Added EnemySensors\n";
            modified = true;
            issueCount++;
        }

        // --- AudioSource ---
        if (prefabRoot.GetComponent<AudioSource>() == null)
        {
            AudioSource audio = prefabRoot.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            report += "  + Added AudioSource\n";
            modified = true;
            issueCount++;
        }

        // --- Layer and Tag ---
        if (!prefabRoot.CompareTag("Enemy"))
        {
            prefabRoot.tag = "Enemy";
            report += "  + Set tag to 'Enemy'\n";
            modified = true;
            issueCount++;
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && prefabRoot.layer != enemyLayer)
        {
            prefabRoot.layer = enemyLayer;
            report += $"  + Set layer to 'Enemy' ({enemyLayer})\n";
            modified = true;
            issueCount++;
        }

        // --- Wire serialized references (fixes null refs on existing prefabs) ---

        // Child transforms (ground/hopping only)
        bool isGround = (type == EnemyType.GroundPatrol || type == EnemyType.Hopping);
        if (isGround)
        {
            if (prefabRoot.transform.Find("GroundCheck") == null)
            {
                CreateChildTransform(prefabRoot, "GroundCheck", new Vector3(0f, -0.6f, 0f));
                report += "  + Added GroundCheck child\n";
                modified = true;
                issueCount++;
            }
            if (prefabRoot.transform.Find("WallCheck") == null)
            {
                CreateChildTransform(prefabRoot, "WallCheck", new Vector3(0.6f, 0f, 0f));
                report += "  + Added WallCheck child\n";
                modified = true;
                issueCount++;
            }
            if (prefabRoot.transform.Find("LedgeCheck") == null)
            {
                CreateChildTransform(prefabRoot, "LedgeCheck", new Vector3(0.6f, -0.6f, 0f));
                report += "  + Added LedgeCheck child\n";
                modified = true;
                issueCount++;
            }
        }
        if (prefabRoot.transform.Find("AttackOrigin") == null)
        {
            CreateChildTransform(prefabRoot, "AttackOrigin", new Vector3(0.3f, 0.1f, 0f));
            report += "  + Added AttackOrigin child\n";
            modified = true;
            issueCount++;
        }

        // EnemyController: animator + audioSource
        Animator animComp = prefabRoot.GetComponent<Animator>();
        if (animComp == null)
        {
            animComp = prefabRoot.AddComponent<Animator>();
            report += "  + Added Animator\n";
            modified = true;
            issueCount++;
        }
        AudioSource audioComp = prefabRoot.GetComponent<AudioSource>();

        SerializedObject soCtrl = new SerializedObject(controller);
        var animProp = soCtrl.FindProperty("animator");
        var audioProp = soCtrl.FindProperty("audioSource");
        if (animProp != null && animProp.objectReferenceValue == null)
        {
            animProp.objectReferenceValue = animComp;
            report += "  + Wired EnemyController.animator\n";
            modified = true;
            issueCount++;
        }
        if (audioProp != null && audioProp.objectReferenceValue == null && audioComp != null)
        {
            audioProp.objectReferenceValue = audioComp;
            report += "  + Wired EnemyController.audioSource\n";
            modified = true;
            issueCount++;
        }
        soCtrl.ApplyModifiedProperties();

        // EnemySensors: targetLayers + obstacleLayers
        EnemySensors sensorsComp = prefabRoot.GetComponent<EnemySensors>();
        if (sensorsComp != null)
        {
            SerializedObject soSensors = new SerializedObject(sensorsComp);
            var targetProp = soSensors.FindProperty("targetLayers.m_Bits");
            var obstacleProp = soSensors.FindProperty("obstacleLayers.m_Bits");
            if (targetProp != null && targetProp.intValue == 0)
            {
                targetProp.intValue = LayerMask.GetMask("Player");
                report += "  + Set EnemySensors.targetLayers = Player\n";
                modified = true;
                issueCount++;
            }
            if (obstacleProp != null && obstacleProp.intValue == 0)
            {
                obstacleProp.intValue = LayerMask.GetMask("Ground");
                report += "  + Set EnemySensors.obstacleLayers = Ground\n";
                modified = true;
                issueCount++;
            }
            soSensors.ApplyModifiedProperties();
        }

        // Movement: groundLayer + check transforms
        BaseEnemyMovement moveComp = prefabRoot.GetComponent<BaseEnemyMovement>();
        if (moveComp != null && isGround)
        {
            SerializedObject soMove = new SerializedObject(moveComp);
            var groundLayerProp = soMove.FindProperty("groundLayer.m_Bits");
            if (groundLayerProp != null && groundLayerProp.intValue == 0)
            {
                groundLayerProp.intValue = LayerMask.GetMask("Ground");
                report += "  + Set movement.groundLayer = Ground\n";
                modified = true;
                issueCount++;
            }
            var gcProp = soMove.FindProperty("groundCheck");
            if (gcProp != null && gcProp.objectReferenceValue == null)
            {
                gcProp.objectReferenceValue = prefabRoot.transform.Find("GroundCheck");
                report += "  + Wired movement.groundCheck\n";
                modified = true;
                issueCount++;
            }
            var wcProp = soMove.FindProperty("wallCheck");
            if (wcProp != null && wcProp.objectReferenceValue == null)
            {
                wcProp.objectReferenceValue = prefabRoot.transform.Find("WallCheck");
                report += "  + Wired movement.wallCheck\n";
                modified = true;
                issueCount++;
            }
            var lcProp = soMove.FindProperty("ledgeCheck");
            if (lcProp != null && lcProp.objectReferenceValue == null)
            {
                lcProp.objectReferenceValue = prefabRoot.transform.Find("LedgeCheck");
                report += "  + Wired movement.ledgeCheck\n";
                modified = true;
                issueCount++;
            }
            soMove.ApplyModifiedProperties();
        }

        // EnemyCombat: attackOrigin
        EnemyCombat combatRepair = prefabRoot.GetComponent<EnemyCombat>();
        if (combatRepair != null)
        {
            SerializedObject soCombat = new SerializedObject(combatRepair);
            var aoProp = soCombat.FindProperty("attackOrigin");
            if (aoProp != null && aoProp.objectReferenceValue == null)
            {
                aoProp.objectReferenceValue = prefabRoot.transform.Find("AttackOrigin");
                report += "  + Wired EnemyCombat.attackOrigin\n";
                modified = true;
                issueCount++;
            }
            soCombat.ApplyModifiedProperties();
        }

        // SpriteRenderer: material
        SpriteRenderer srRepair = prefabRoot.GetComponent<SpriteRenderer>();
        if (srRepair != null)
        {
            GameObject refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Bat.prefab");
            if (refPrefab != null)
            {
                Material refMat = refPrefab.GetComponent<SpriteRenderer>()?.sharedMaterial;
                if (refMat != null && srRepair.sharedMaterial != refMat)
                {
                    srRepair.sharedMaterial = refMat;
                    report += "  + Set SpriteRenderer material to Sprites-Default\n";
                    modified = true;
                    issueCount++;
                }
            }
        }

        // Save or discard
        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log(report);
        }
        else
        {
            report += "  No issues found.";
            Debug.Log(report);
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
        return modified;
    }

    #endregion

    #region Create

    private void DrawCreateSection()
    {
        GUILayout.Label("Create New Enemy", EditorStyles.boldLabel);

        // Basic Info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        enemyName = EditorGUILayout.TextField("Enemy Name", enemyName);
        enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", enemyType);
        enemySprite = (Sprite)EditorGUILayout.ObjectField("Sprite", enemySprite, typeof(Sprite), false);

        EditorGUILayout.Space();

        // Stats
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        maxHealth = EditorGUILayout.FloatField("Max Health", maxHealth);
        moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);
        chaseSpeed = EditorGUILayout.FloatField("Chase Speed", chaseSpeed);
        contactDamage = EditorGUILayout.FloatField("Contact Damage", contactDamage);

        EditorGUILayout.Space();

        // Detection
        EditorGUILayout.LabelField("Detection", EditorStyles.boldLabel);
        detectionType = (DetectionType)EditorGUILayout.EnumPopup("Detection Type", detectionType);
        detectionRange = EditorGUILayout.FloatField("Detection Range", detectionRange);
        loseAggroRange = EditorGUILayout.FloatField("Lose Aggro Range", loseAggroRange);

        EditorGUILayout.Space();

        // Combat
        EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
        attackDamage = EditorGUILayout.FloatField("Attack Damage", attackDamage);
        attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);
        attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", attackCooldown);

        EditorGUILayout.Space();

        // Rewards
        EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
        experienceValue = EditorGUILayout.IntField("Experience Value", experienceValue);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Create buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create Data Only"))
        {
            CreateEnemyData();
        }

        if (GUILayout.Button("Create Full Enemy"))
        {
            CreateFullEnemy();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Presets
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Slime (Ground)"))
        {
            ApplySlimePreset();
        }

        if (GUILayout.Button("Bat (Flying)"))
        {
            ApplyBatPreset();
        }

        if (GUILayout.Button("Turret (Stationary)"))
        {
            ApplyTurretPreset();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void CreateEnemyData()
    {
        // Create EnemyData ScriptableObject
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
        ConfigureEnemyData(enemyData);

        // Create attack data
        EnemyAttackData attackData = CreateAttackData();

        // Assign attack to enemy data
        enemyData.attacks = new EnemyAttackData[] { attackData };

        // Save assets
        string dataPath = $"Assets/_Project/ScriptableObjects/Enemies/Types/{enemyName}Data.asset";
        string attackPath = $"Assets/_Project/ScriptableObjects/Enemies/Attacks/{enemyName}Attack.asset";

        EnsureDirectoryExists("Assets/_Project/ScriptableObjects/Enemies/Types");
        EnsureDirectoryExists("Assets/_Project/ScriptableObjects/Enemies/Attacks");

        AssetDatabase.CreateAsset(attackData, attackPath);
        AssetDatabase.CreateAsset(enemyData, dataPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = enemyData;

        Debug.Log($"Created EnemyData: {dataPath}");
    }

    private void CreateFullEnemy()
    {
        // First create the data assets
        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
        ConfigureEnemyData(enemyData);

        EnemyAttackData attackData = CreateAttackData();
        enemyData.attacks = new EnemyAttackData[] { attackData };

        // Save data assets
        string dataPath = $"Assets/_Project/ScriptableObjects/Enemies/Types/{enemyName}Data.asset";
        string attackPath = $"Assets/_Project/ScriptableObjects/Enemies/Attacks/{enemyName}Attack.asset";

        EnsureDirectoryExists("Assets/_Project/ScriptableObjects/Enemies/Types");
        EnsureDirectoryExists("Assets/_Project/ScriptableObjects/Enemies/Attacks");

        AssetDatabase.CreateAsset(attackData, attackPath);
        AssetDatabase.CreateAsset(enemyData, dataPath);

        // Create the prefab GameObject
        GameObject enemyGO = new GameObject(enemyName);

        // Add SpriteRenderer
        SpriteRenderer sr = enemyGO.AddComponent<SpriteRenderer>();
        if (enemySprite != null)
        {
            sr.sprite = enemySprite;
        }
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 10;

        // Add Rigidbody2D
        Rigidbody2D rb = enemyGO.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (enemyType == EnemyType.Flying)
        {
            rb.gravityScale = 0f;
        }

        // Add Collider
        if (enemyType == EnemyType.Flying)
        {
            CircleCollider2D col = enemyGO.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
        }
        else
        {
            BoxCollider2D col = enemyGO.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
        }

        // Add core components
        HealthSystem health = enemyGO.AddComponent<HealthSystem>();

        Animator anim = enemyGO.AddComponent<Animator>();

        AudioSource audio = enemyGO.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        EnemyController controller = enemyGO.AddComponent<EnemyController>();
        // Set enemyData via SerializedObject
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("enemyData").objectReferenceValue = enemyData;
        so.ApplyModifiedProperties();

        // Add movement component based on type
        switch (enemyType)
        {
            case EnemyType.GroundPatrol:
                enemyGO.AddComponent<GroundPatrolMovement>();
                break;
            case EnemyType.Flying:
                enemyGO.AddComponent<FlyingMovement>();
                break;
            // Stationary doesn't need movement
        }

        // Add combat and sensors
        enemyGO.AddComponent<EnemyCombat>();
        enemyGO.AddComponent<EnemySensors>();

        // Set layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            enemyGO.layer = enemyLayer;
        }

        // Set tag
        enemyGO.tag = "Enemy";

        // Child transforms
        if (enemyType == EnemyType.GroundPatrol || enemyType == EnemyType.Hopping)
        {
            CreateChildTransform(enemyGO, "GroundCheck", new Vector3(0f, -0.6f, 0f));
            CreateChildTransform(enemyGO, "WallCheck", new Vector3(0.6f, 0f, 0f));
            CreateChildTransform(enemyGO, "LedgeCheck", new Vector3(0.6f, -0.6f, 0f));
        }
        CreateChildTransform(enemyGO, "AttackOrigin", new Vector3(0.3f, 0.1f, 0f));

        // --- Wire serialized references ---

        // EnemyController: animator + audioSource
        so.Update();
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.ApplyModifiedProperties();

        // EnemySensors: targetLayers + obstacleLayers
        EnemySensors sensors = enemyGO.GetComponent<EnemySensors>();
        SerializedObject soSensors = new SerializedObject(sensors);
        soSensors.FindProperty("targetLayers.m_Bits").intValue = LayerMask.GetMask("Player");
        soSensors.FindProperty("obstacleLayers.m_Bits").intValue = LayerMask.GetMask("Ground");
        soSensors.ApplyModifiedProperties();

        // Movement refs (ground/hopping only)
        if (enemyType == EnemyType.GroundPatrol || enemyType == EnemyType.Hopping)
        {
            BaseEnemyMovement movement = enemyGO.GetComponent<BaseEnemyMovement>();
            SerializedObject soMove = new SerializedObject(movement);
            soMove.FindProperty("groundLayer.m_Bits").intValue = LayerMask.GetMask("Ground");
            soMove.FindProperty("groundCheck").objectReferenceValue = enemyGO.transform.Find("GroundCheck");
            soMove.FindProperty("wallCheck").objectReferenceValue = enemyGO.transform.Find("WallCheck");
            soMove.FindProperty("ledgeCheck").objectReferenceValue = enemyGO.transform.Find("LedgeCheck");
            soMove.ApplyModifiedProperties();
        }

        // EnemyCombat: attackOrigin
        EnemyCombat combatComp = enemyGO.GetComponent<EnemyCombat>();
        SerializedObject soCombat = new SerializedObject(combatComp);
        soCombat.FindProperty("attackOrigin").objectReferenceValue = enemyGO.transform.Find("AttackOrigin");
        soCombat.ApplyModifiedProperties();

        // SpriteRenderer: material (use Bat.prefab's known-good material)
        GameObject refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Bat.prefab");
        if (refPrefab != null)
        {
            Material refMat = refPrefab.GetComponent<SpriteRenderer>()?.sharedMaterial;
            if (refMat != null)
                sr.sharedMaterial = refMat;
        }

        // Save as prefab
        string prefabPath = $"Assets/_Project/Prefabs/Enemies/{enemyName}.prefab";
        EnsureDirectoryExists("Assets/_Project/Prefabs/Enemies");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemyGO, prefabPath);

        // Clean up scene object
        DestroyImmediate(enemyGO);

        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;

        Debug.Log($"Created Enemy Prefab: {prefabPath}");
    }

    #endregion

    #region Helpers

    private void ConfigureEnemyData(EnemyData data)
    {
        data.enemyName = enemyName;
        data.enemyType = enemyType;
        data.maxHealth = maxHealth;
        data.moveSpeed = moveSpeed;
        data.chaseSpeed = chaseSpeed;
        data.contactDamage = contactDamage;
        data.detectionType = detectionType;
        data.detectionRange = detectionRange;
        data.loseAggroRange = loseAggroRange;
        data.attackRange = attackRange;
        data.attackCooldown = attackCooldown;
        data.experienceValue = experienceValue;
    }

    private EnemyAttackData CreateAttackData()
    {
        EnemyAttackData attack = ScriptableObject.CreateInstance<EnemyAttackData>();
        attack.attackName = $"{enemyName} Attack";
        attack.baseDamage = attackDamage;
        attack.knockbackForce = 5f;
        attack.knockbackDirection = new Vector2(1f, 0.5f);
        attack.windUpDuration = 0.2f;
        attack.activeDuration = 0.15f;
        attack.recoveryDuration = 0.3f;
        attack.hitboxSize = new Vector2(1f, 1f);
        attack.hitboxOffset = new Vector2(1f, 0f);
        attack.minRange = 0f;
        attack.maxRange = attackRange;
        attack.targetLayers = LayerMask.GetMask("Player");

        return attack;
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }

    private void CreateChildTransform(GameObject parent, string name, Vector3 localPos)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = localPos;
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 2f);
        rect.height = 2f;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    #endregion

    #region Presets

    private void ApplySlimePreset()
    {
        enemyName = "Slime";
        enemyType = EnemyType.GroundPatrol;
        maxHealth = 30f;
        moveSpeed = 3f;
        chaseSpeed = 5f;
        contactDamage = 10f;
        detectionType = DetectionType.Radius;
        detectionRange = 6f;
        loseAggroRange = 10f;
        attackDamage = 15f;
        attackRange = 1.5f;
        attackCooldown = 1f;
        experienceValue = 10;
    }

    private void ApplyBatPreset()
    {
        enemyName = "Bat";
        enemyType = EnemyType.Flying;
        maxHealth = 15f;
        moveSpeed = 4f;
        chaseSpeed = 6f;
        contactDamage = 8f;
        detectionType = DetectionType.Radius;
        detectionRange = 8f;
        loseAggroRange = 12f;
        attackDamage = 10f;
        attackRange = 1f;
        attackCooldown = 0.8f;
        experienceValue = 8;
    }

    private void ApplyTurretPreset()
    {
        enemyName = "Turret";
        enemyType = EnemyType.Stationary;
        maxHealth = 50f;
        moveSpeed = 0f;
        chaseSpeed = 0f;
        contactDamage = 0f;
        detectionType = DetectionType.Cone;
        detectionRange = 10f;
        loseAggroRange = 12f;
        attackDamage = 12f;
        attackRange = 10f;
        attackCooldown = 1.5f;
        experienceValue = 15;
    }

    #endregion
}
