using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool that generates MiniSlime/MicroSlime ScriptableObjects, prefabs,
/// NoxiousCloud prefab, and wires death effects into SlimeData and MushroomData.
/// Run via Tools > Create Split Enemy Assets.
/// </summary>
public static class CreateSplitEnemies
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Enemies";
    private const string DataDir = "Assets/_Project/ScriptableObjects/Enemies/Types";
    private const string AttackDir = "Assets/_Project/ScriptableObjects/Enemies/Attacks";
    private const string AnimDir = "Assets/_Project/Art/Animations/Enemies/Slime";

    [MenuItem("Tools/Create Split Enemy Assets")]
    public static void CreateAll()
    {
        EnsureDirectoryExists(PrefabDir);
        EnsureDirectoryExists(DataDir);
        EnsureDirectoryExists(AttackDir);

        // Load reference assets
        EnemyData slimeData = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataDir}/SlimeData.asset");
        EnemyData mushroomData = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataDir}/MushroomData.asset");
        EnemyAttackData slimeAttack = AssetDatabase.LoadAssetAtPath<EnemyAttackData>($"{AttackDir}/SlimeAttack.asset");
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Slime.prefab");
        GameObject batPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Bat.prefab");

        if (slimeData == null)
        {
            Debug.LogError("[CreateSplitEnemies] SlimeData.asset not found!");
            return;
        }
        if (slimeAttack == null)
        {
            Debug.LogError("[CreateSplitEnemies] SlimeAttack.asset not found!");
            return;
        }
        if (slimePrefab == null)
        {
            Debug.LogError("[CreateSplitEnemies] Slime.prefab not found!");
            return;
        }

        // Get reference material from Bat prefab (known-good Sprites-Default)
        Material refMaterial = null;
        if (batPrefab != null)
        {
            SpriteRenderer batSR = batPrefab.GetComponent<SpriteRenderer>();
            if (batSR != null)
                refMaterial = batSR.sharedMaterial;
        }

        // Get Slime's animator controller and default sprite
        RuntimeAnimatorController slimeAnimController = null;
        Sprite slimeSprite = null;
        Animator slimeAnim = slimePrefab.GetComponent<Animator>();
        if (slimeAnim != null)
            slimeAnimController = slimeAnim.runtimeAnimatorController;
        SpriteRenderer slimeSR = slimePrefab.GetComponent<SpriteRenderer>();
        if (slimeSR != null)
            slimeSprite = slimeSR.sprite;

        // 1. Create attack data assets
        EnemyAttackData miniSlimeAttack = CreateAttackData("MiniSlimeAttack",
            baseDamage: 8f, knockbackForce: 3f,
            hitboxSize: new Vector2(0.8f, 0.6f),
            slimeAttack);

        EnemyAttackData microSlimeAttack = CreateAttackData("MicroSlimeAttack",
            baseDamage: 5f, knockbackForce: 2f,
            hitboxSize: new Vector2(0.6f, 0.4f),
            slimeAttack);

        // 2. Create MicroSlime prefab first (MiniSlime references it)
        EnemyData microSlimeData = CreateEnemyData("MicroSlimeData",
            enemyName: "Micro Slime",
            maxHealth: 10f, moveSpeed: 4f, chaseSpeed: 6f,
            contactDamage: 4f, detectionRange: 4f, attackRange: 1.0f,
            hopForce: 4f, hopCooldown: 0.5f, hopChaseCooldown: 0.25f,
            experienceValue: 2,
            attacks: new EnemyAttackData[] { microSlimeAttack },
            deathSpawnPrefab: null, deathSpawnCount: 0);

        GameObject microSlimePrefab = CreateSlimeVariantPrefab("MicroSlime",
            microSlimeData, new Vector3(0.5f, 0.5f, 1f),
            slimeAnimController, slimeSprite, refMaterial);

        // 3. Create MiniSlime prefab (references MicroSlime for chain split)
        EnemyData miniSlimeData = CreateEnemyData("MiniSlimeData",
            enemyName: "Mini Slime",
            maxHealth: 18f, moveSpeed: 3.5f, chaseSpeed: 5.5f,
            contactDamage: 6f, detectionRange: 5f, attackRange: 1.2f,
            hopForce: 5f, hopCooldown: 0.7f, hopChaseCooldown: 0.35f,
            experienceValue: 3,
            attacks: new EnemyAttackData[] { miniSlimeAttack },
            deathSpawnPrefab: microSlimePrefab, deathSpawnCount: 2);

        GameObject miniSlimePrefab = CreateSlimeVariantPrefab("MiniSlime",
            miniSlimeData, new Vector3(0.7f, 0.7f, 1f),
            slimeAnimController, slimeSprite, refMaterial);

        // 4. Create NoxiousCloud prefab
        GameObject noxiousCloudPrefab = CreateNoxiousCloudPrefab();

        // 5. Wire death effects into existing data assets
        // SlimeData -> splits into MiniSlime
        Undo.RecordObject(slimeData, "Wire Slime Death Split");
        slimeData.deathSpawnPrefab = miniSlimePrefab;
        slimeData.deathSpawnCount = 2;
        slimeData.deathSpawnSpread = 0.5f;
        EditorUtility.SetDirty(slimeData);

        // MushroomData -> spawns NoxiousCloud
        if (mushroomData != null)
        {
            Undo.RecordObject(mushroomData, "Wire Mushroom Death Hazard");
            mushroomData.deathHazardPrefab = noxiousCloudPrefab;
            EditorUtility.SetDirty(mushroomData);
        }
        else
        {
            Debug.LogWarning("[CreateSplitEnemies] MushroomData.asset not found — skipping death hazard wiring.");
        }

        // 6. Update MiniSlimeData to reference the saved MicroSlime prefab
        // (It was set during creation, but let's ensure it's the asset reference)
        miniSlimeData.deathSpawnPrefab = microSlimePrefab;
        EditorUtility.SetDirty(miniSlimeData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateSplitEnemies] Complete! Created: MiniSlime, MicroSlime, NoxiousCloud prefabs + data assets. Wired SlimeData and MushroomData death effects.");
    }

    private static EnemyAttackData CreateAttackData(string name, float baseDamage, float knockbackForce, Vector2 hitboxSize, EnemyAttackData reference)
    {
        string path = $"{AttackDir}/{name}.asset";

        // Check if already exists
        EnemyAttackData existing = AssetDatabase.LoadAssetAtPath<EnemyAttackData>(path);
        if (existing != null)
        {
            Debug.Log($"[CreateSplitEnemies] {name} already exists, updating.");
            Undo.RecordObject(existing, $"Update {name}");
            existing.baseDamage = baseDamage;
            existing.knockbackForce = knockbackForce;
            existing.hitboxSize = hitboxSize;
            existing.knockbackDirection = reference.knockbackDirection;
            existing.windUpDuration = reference.windUpDuration;
            existing.activeDuration = reference.activeDuration;
            existing.recoveryDuration = reference.recoveryDuration;
            existing.hitboxOffset = new Vector2(hitboxSize.x * 0.5f, 0f);
            existing.targetLayers = reference.targetLayers;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        EnemyAttackData attack = ScriptableObject.CreateInstance<EnemyAttackData>();
        attack.attackName = name.Replace("Attack", " Attack");
        attack.baseDamage = baseDamage;
        attack.knockbackForce = knockbackForce;
        attack.knockbackDirection = reference.knockbackDirection;
        attack.windUpDuration = reference.windUpDuration;
        attack.activeDuration = reference.activeDuration;
        attack.recoveryDuration = reference.recoveryDuration;
        attack.hitboxSize = hitboxSize;
        attack.hitboxOffset = new Vector2(hitboxSize.x * 0.5f, 0f);
        attack.targetLayers = reference.targetLayers;
        attack.isParryable = true;

        AssetDatabase.CreateAsset(attack, path);
        Debug.Log($"[CreateSplitEnemies] Created {path}");
        return attack;
    }

    private static EnemyData CreateEnemyData(string name,
        string enemyName, float maxHealth, float moveSpeed, float chaseSpeed,
        float contactDamage, float detectionRange, float attackRange,
        float hopForce, float hopCooldown, float hopChaseCooldown,
        int experienceValue, EnemyAttackData[] attacks,
        GameObject deathSpawnPrefab, int deathSpawnCount)
    {
        string path = $"{DataDir}/{name}.asset";

        // Check if already exists
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (existing != null)
        {
            Debug.Log($"[CreateSplitEnemies] {name} already exists, updating.");
            Undo.RecordObject(existing, $"Update {name}");
            existing.enemyName = enemyName;
            existing.enemyType = EnemyType.Hopping;
            existing.maxHealth = maxHealth;
            existing.moveSpeed = moveSpeed;
            existing.chaseSpeed = chaseSpeed;
            existing.contactDamage = contactDamage;
            existing.detectionType = DetectionType.Radius;
            existing.detectionRange = detectionRange;
            existing.loseAggroRange = detectionRange + 4f;
            existing.attackRange = attackRange;
            existing.attackCooldown = 1f;
            existing.hopForce = hopForce;
            existing.hopHorizontalSpeed = moveSpeed;
            existing.hopCooldown = hopCooldown;
            existing.hopChaseCooldown = hopChaseCooldown;
            existing.hopFallGravityMultiplier = 3f;
            existing.experienceValue = experienceValue;
            existing.attacks = attacks;
            existing.deathSpawnPrefab = deathSpawnPrefab;
            existing.deathSpawnCount = deathSpawnCount;
            existing.deathSpawnSpread = 0.5f;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = enemyName;
        data.enemyType = EnemyType.Hopping;
        data.maxHealth = maxHealth;
        data.invulnerabilityDuration = 0.1f;
        data.moveSpeed = moveSpeed;
        data.chaseSpeed = chaseSpeed;
        data.contactDamage = contactDamage;
        data.contactKnockbackForce = 3f;
        data.detectionType = DetectionType.Radius;
        data.detectionRange = detectionRange;
        data.detectionAngle = 60f;
        data.loseAggroRange = detectionRange + 4f;
        data.attacks = attacks;
        data.attackRange = attackRange;
        data.attackCooldown = 1f;
        data.knockbackResistance = 0f;
        data.stunDuration = 0.5f;
        data.experienceValue = experienceValue;
        data.dropChance = 0f; // Split enemies don't drop loot
        data.hopForce = hopForce;
        data.hopHorizontalSpeed = moveSpeed;
        data.hopCooldown = hopCooldown;
        data.hopChaseCooldown = hopChaseCooldown;
        data.hopFallGravityMultiplier = 3f;
        data.deathSpawnPrefab = deathSpawnPrefab;
        data.deathSpawnCount = deathSpawnCount;
        data.deathSpawnSpread = 0.5f;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[CreateSplitEnemies] Created {path}");
        return data;
    }

    private static GameObject CreateSlimeVariantPrefab(string name, EnemyData data,
        Vector3 scale, RuntimeAnimatorController animController, Sprite defaultSprite,
        Material refMaterial)
    {
        string prefabPath = $"{PrefabDir}/{name}.prefab";

        // Check if already exists
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log($"[CreateSplitEnemies] {name}.prefab already exists, skipping prefab creation.");
            // Update the data reference
            EnemyController existingCtrl = existing.GetComponent<EnemyController>();
            if (existingCtrl != null)
            {
                SerializedObject so = new SerializedObject(existingCtrl);
                so.FindProperty("enemyData").objectReferenceValue = data;
                so.ApplyModifiedProperties();
            }
            return existing;
        }

        GameObject go = new GameObject(name);
        go.transform.localScale = scale;

        // SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (defaultSprite != null)
            sr.sprite = defaultSprite;
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 10;
        if (refMaterial != null)
            sr.sharedMaterial = refMaterial;

        // Rigidbody2D
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // BoxCollider2D (local size matches base Slime; transform scale handles proportional sizing)
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.7f);
        col.offset = new Vector2(0f, 0f);

        // Animator (reuse Slime's controller)
        Animator anim = go.AddComponent<Animator>();
        if (animController != null)
            anim.runtimeAnimatorController = animController;

        // AudioSource
        AudioSource audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        // HealthSystem
        go.AddComponent<HealthSystem>();

        // EnemyController
        EnemyController controller = go.AddComponent<EnemyController>();
        SerializedObject soCtrl = new SerializedObject(controller);
        soCtrl.FindProperty("enemyData").objectReferenceValue = data;
        soCtrl.FindProperty("animator").objectReferenceValue = anim;
        soCtrl.FindProperty("audioSource").objectReferenceValue = audio;
        soCtrl.ApplyModifiedProperties();

        // HoppingMovement
        go.AddComponent<HoppingMovement>();

        // EnemyCombat + EnemySensors
        go.AddComponent<EnemyCombat>();
        EnemySensors sensors = go.AddComponent<EnemySensors>();

        // Layer and Tag
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        go.layer = enemyLayer != -1 ? enemyLayer : 13;
        go.tag = "Enemy";

        // Child transforms (scaled positions)
        float halfH = col.size.y * 0.5f;
        CreateChild(go, "GroundCheck", new Vector3(0f, -halfH - 0.1f, 0f));
        CreateChild(go, "WallCheck", new Vector3(col.size.x * 0.5f + 0.1f, 0f, 0f));
        CreateChild(go, "LedgeCheck", new Vector3(col.size.x * 0.5f + 0.1f, -halfH - 0.1f, 0f));
        CreateChild(go, "AttackOrigin", new Vector3(col.size.x * 0.3f, 0.1f, 0f));

        // Wire serialized references
        SerializedObject soSensors = new SerializedObject(sensors);
        soSensors.FindProperty("targetLayers.m_Bits").intValue = LayerMask.GetMask("Player");
        soSensors.FindProperty("obstacleLayers.m_Bits").intValue = LayerMask.GetMask("Ground");
        soSensors.ApplyModifiedProperties();

        BaseEnemyMovement movement = go.GetComponent<BaseEnemyMovement>();
        SerializedObject soMove = new SerializedObject(movement);
        soMove.FindProperty("groundLayer.m_Bits").intValue = LayerMask.GetMask("Ground");
        soMove.FindProperty("groundCheck").objectReferenceValue = go.transform.Find("GroundCheck");
        soMove.FindProperty("wallCheck").objectReferenceValue = go.transform.Find("WallCheck");
        soMove.FindProperty("ledgeCheck").objectReferenceValue = go.transform.Find("LedgeCheck");
        soMove.ApplyModifiedProperties();

        EnemyCombat combat = go.GetComponent<EnemyCombat>();
        SerializedObject soCombat = new SerializedObject(combat);
        soCombat.FindProperty("attackOrigin").objectReferenceValue = go.transform.Find("AttackOrigin");
        soCombat.ApplyModifiedProperties();

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[CreateSplitEnemies] Created {prefabPath}");
        return prefab;
    }

    private static GameObject CreateNoxiousCloudPrefab()
    {
        string prefabPath = $"{PrefabDir}/NoxiousCloud.prefab";

        // Check if already exists
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log("[CreateSplitEnemies] NoxiousCloud.prefab already exists, skipping.");
            return existing;
        }

        GameObject go = new GameObject("NoxiousCloud");
        go.AddComponent<NoxiousCloud>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[CreateSplitEnemies] Created {prefabPath}");
        return prefab;
    }

    private static void CreateChild(GameObject parent, string name, Vector3 localPos)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = localPos;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                currentPath = newPath;
            }
        }
    }
}
