using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor wizard for quickly creating enemy prefabs and data assets.
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

    [MenuItem("Tools/Enemy Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<EnemySetupWizard>("Enemy Setup Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enemy Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

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

        // Add AudioSource
        enemyGO.AddComponent<AudioSource>();

        // Set layer
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            enemyGO.layer = enemyLayer;
        }

        // Set tag
        enemyGO.tag = "Enemy";

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
