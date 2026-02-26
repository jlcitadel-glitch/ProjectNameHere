using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Batch creation of all enemy prefabs with animator controllers.
/// Creates: Flying Eye, Goblin, Mushroom, Skeleton prefabs and wires them
/// into SurvivalWaveConfig's enemy pool.
/// Run via Tools > Create All Enemy Prefabs.
/// </summary>
public static class CreateAllEnemies
{
    private const string PrefabDir = "Assets/_Project/Prefabs/Enemies";
    private const string AnimDir = "Assets/_Project/Animations/Enemies";
    private const string DataDir = "Assets/_Project/ScriptableObjects/Enemies/Types";
    private const string WaveConfigPath = "Assets/_Project/ScriptableObjects/Enemies/SurvivalWaveConfig.asset";
    private const string SpriteDir = "Assets/Monsters Creatures Fantasy/Sprites";

    private struct EnemyDef
    {
        public string name;
        public string dataAsset;
        public string spriteFolder;
        public string idleSheet;   // Idle or Flight for Flying Eye
        public string moveSheet;   // Run or Walk or null
        public string[] attackSheets;
        public string hitSheet;
        public string deathSheet;
        public bool isFlying;
        public bool isHopping;
        public float colliderWidth;
        public float colliderHeight;
        public float colliderOffsetY;
        public float spawnWeight;
        public int minWave;
    }

    [MenuItem("Tools/Create All Enemy Prefabs")]
    public static void CreateAll()
    {
        EnsureDirectoryExists(PrefabDir);
        EnsureDirectoryExists(AnimDir);

        var enemies = new EnemyDef[]
        {
            new EnemyDef
            {
                name = "FlyingEye",
                dataAsset = "FlyingEyeData",
                spriteFolder = "Flying eye",
                idleSheet = "Flight",
                moveSheet = null, // Flying Eye uses Flight for both
                attackSheets = new[] { "Attack1", "Attack2" },
                hitSheet = "Take Hit",
                deathSheet = "Death",
                isFlying = true,
                isHopping = false,
                colliderWidth = 0.8f,
                colliderHeight = 0.8f,
                colliderOffsetY = 0f,
                spawnWeight = 0.7f,
                minWave = 3,
            },
            new EnemyDef
            {
                name = "Goblin",
                dataAsset = "GoblinData",
                spriteFolder = "Goblin",
                idleSheet = "Idle",
                moveSheet = "Run",
                attackSheets = new[] { "Attack1", "Attack2" },
                hitSheet = "Take Hit",
                deathSheet = "Death",
                isFlying = false,
                isHopping = false,
                colliderWidth = 0.8f,
                colliderHeight = 1.0f,
                colliderOffsetY = 0f,
                spawnWeight = 0.9f,
                minWave = 2,
            },
            new EnemyDef
            {
                name = "Mushroom",
                dataAsset = "MushroomData",
                spriteFolder = "Mushroom",
                idleSheet = "Idle",
                moveSheet = "Run",
                attackSheets = new[] { "Attack1", "Attack2" },
                hitSheet = "Take Hit",
                deathSheet = "Death",
                isFlying = false,
                isHopping = true,
                colliderWidth = 0.8f,
                colliderHeight = 0.9f,
                colliderOffsetY = 0f,
                spawnWeight = 0.6f,
                minWave = 4,
            },
            new EnemyDef
            {
                name = "Skeleton",
                dataAsset = "SkeletonData",
                spriteFolder = "Skeleton",
                idleSheet = "Idle",
                moveSheet = "Walk",
                attackSheets = new[] { "Attack1", "Attack2" },
                hitSheet = "Take Hit",
                deathSheet = "Death",
                isFlying = false,
                isHopping = false,
                colliderWidth = 0.8f,
                colliderHeight = 1.2f,
                colliderOffsetY = 0.1f,
                spawnWeight = 0.5f,
                minWave = 5,
            },
        };

        var createdPrefabs = new List<(GameObject prefab, float weight, int minWave)>();

        foreach (var def in enemies)
        {
            GameObject prefab = CreateEnemy(def);
            if (prefab != null)
            {
                createdPrefabs.Add((prefab, def.spawnWeight, def.minWave));
            }
        }

        // Wire into SurvivalWaveConfig
        WireWaveConfig(createdPrefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateAllEnemies] Created {createdPrefabs.Count} enemy prefabs and updated wave config.");
    }

    private static GameObject CreateEnemy(EnemyDef def)
    {
        // Load data asset
        string dataPath = $"{DataDir}/{def.dataAsset}.asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        if (data == null)
        {
            Debug.LogError($"[CreateAllEnemies] {def.name}: EnemyData not found at {dataPath}");
            return null;
        }

        string prefabPath = $"{PrefabDir}/{def.name}.prefab";

        // Build animator controller
        AnimatorController animController = BuildAnimatorController(def);

        // Load idle sprite for the SpriteRenderer default
        Sprite defaultSprite = LoadFirstSprite(def.spriteFolder, def.idleSheet);

        // Create the GameObject
        GameObject go = new GameObject(def.name);

        // SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (defaultSprite != null)
            sr.sprite = defaultSprite;
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 10;

        // Rigidbody2D
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        if (def.isFlying)
            rb.gravityScale = 0f;

        // Collider
        if (def.isFlying)
        {
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = def.colliderWidth * 0.5f;
            col.offset = new Vector2(0f, def.colliderOffsetY);
        }
        else
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(def.colliderWidth, def.colliderHeight);
            col.offset = new Vector2(0f, def.colliderOffsetY);
        }

        // Animator
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
        soCtrl.ApplyModifiedProperties();

        // Movement component
        if (def.isFlying)
            go.AddComponent<FlyingMovement>();
        else if (def.isHopping)
            go.AddComponent<HoppingMovement>();
        else
            go.AddComponent<GroundPatrolMovement>();

        // Combat and Sensors
        go.AddComponent<EnemyCombat>();
        go.AddComponent<EnemySensors>();

        // Layer and Tag
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        go.layer = enemyLayer != -1 ? enemyLayer : 13;
        go.tag = "Enemy";

        // Child Transforms
        if (!def.isFlying)
        {
            float halfH = def.colliderHeight * 0.5f + def.colliderOffsetY;
            CreateChild(go, "GroundCheck", new Vector3(0f, -halfH - 0.1f, 0f));
            CreateChild(go, "WallCheck", new Vector3(def.colliderWidth * 0.5f + 0.1f, 0f, 0f));
            CreateChild(go, "LedgeCheck", new Vector3(def.colliderWidth * 0.5f + 0.1f, -halfH - 0.1f, 0f));
        }
        CreateChild(go, "AttackOrigin", new Vector3(def.colliderWidth * 0.3f, 0.1f, 0f));

        // --- Wire serialized references ---

        // EnemyController: animator + audioSource
        soCtrl.Update();
        soCtrl.FindProperty("animator").objectReferenceValue = anim;
        soCtrl.FindProperty("audioSource").objectReferenceValue = audio;
        soCtrl.ApplyModifiedProperties();

        // EnemySensors: targetLayers + obstacleLayers
        EnemySensors sensors = go.GetComponent<EnemySensors>();
        SerializedObject soSensors = new SerializedObject(sensors);
        soSensors.FindProperty("targetLayers.m_Bits").intValue = LayerMask.GetMask("Player");
        soSensors.FindProperty("obstacleLayers.m_Bits").intValue = LayerMask.GetMask("Ground");
        soSensors.ApplyModifiedProperties();

        // Movement: groundLayer + check transforms (ground/hopping only)
        if (!def.isFlying)
        {
            BaseEnemyMovement movement = go.GetComponent<BaseEnemyMovement>();
            SerializedObject soMove = new SerializedObject(movement);
            soMove.FindProperty("groundLayer.m_Bits").intValue = LayerMask.GetMask("Ground");
            soMove.FindProperty("groundCheck").objectReferenceValue = go.transform.Find("GroundCheck");
            soMove.FindProperty("wallCheck").objectReferenceValue = go.transform.Find("WallCheck");
            soMove.FindProperty("ledgeCheck").objectReferenceValue = go.transform.Find("LedgeCheck");
            soMove.ApplyModifiedProperties();
        }

        // EnemyCombat: attackOrigin
        EnemyCombat combat = go.GetComponent<EnemyCombat>();
        SerializedObject soCombat = new SerializedObject(combat);
        soCombat.FindProperty("attackOrigin").objectReferenceValue = go.transform.Find("AttackOrigin");
        soCombat.ApplyModifiedProperties();

        // SpriteRenderer: material (use Bat.prefab's known-good material)
        GameObject refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Bat.prefab");
        if (refPrefab != null)
        {
            Material refMat = refPrefab.GetComponent<SpriteRenderer>()?.sharedMaterial;
            if (refMat != null)
                sr.sharedMaterial = refMat;
        }

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[CreateAllEnemies] Created {def.name} at {prefabPath}");
        return prefab;
    }

    private static AnimatorController BuildAnimatorController(EnemyDef def)
    {
        string enemyAnimDir = $"{AnimDir}/{def.name}";
        EnsureDirectoryExists(enemyAnimDir);

        string controllerPath = $"{enemyAnimDir}/{def.name}.controller";

        // Create controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // Get the base layer state machine
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Create animation clips from sprite sheets
        AnimationClip idleClip = CreateClipFromSheet(def, def.idleSheet, enemyAnimDir, 10f, true);
        AnimationClip attackClip = CreateClipFromSheet(def, def.attackSheets[0], enemyAnimDir, 12f, false);
        AnimationClip hitClip = CreateClipFromSheet(def, def.hitSheet, enemyAnimDir, 10f, false);
        AnimationClip deathClip = CreateClipFromSheet(def, def.deathSheet, enemyAnimDir, 10f, false);
        AnimationClip moveClip = def.moveSheet != null
            ? CreateClipFromSheet(def, def.moveSheet, enemyAnimDir, 10f, true)
            : null;

        // Add states
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        AnimatorState moveState = null;
        if (moveClip != null)
        {
            moveState = sm.AddState("Move");
            moveState.motion = moveClip;
        }

        AnimatorState attackState = sm.AddState("Attack");
        attackState.motion = attackClip;

        AnimatorState hurtState = sm.AddState("Hurt");
        hurtState.motion = hitClip;

        AnimatorState deathState = sm.AddState("Death");
        deathState.motion = deathClip;

        // Transitions: Idle <-> Move (based on Speed)
        if (moveState != null)
        {
            var toMove = idleState.AddTransition(moveState);
            toMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toMove.hasExitTime = false;
            toMove.duration = 0.1f;

            var toIdle = moveState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
        }

        // Any State -> Attack (trigger)
        var toAttack = sm.AddAnyStateTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAttack.hasExitTime = false;
        toAttack.duration = 0.05f;

        // Attack -> Idle (exit time)
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        // Any State -> Hurt (trigger)
        var toHurt = sm.AddAnyStateTransition(hurtState);
        toHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        toHurt.hasExitTime = false;
        toHurt.duration = 0.05f;

        // Hurt -> Idle (exit time)
        var hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 0.9f;
        hurtToIdle.duration = 0.1f;

        // Any State -> Death (trigger)
        var toDeath = sm.AddAnyStateTransition(deathState);
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
        toDeath.hasExitTime = false;
        toDeath.duration = 0.05f;

        AssetDatabase.SaveAssets();
        Debug.Log($"[CreateAllEnemies] Created animator controller: {controllerPath}");
        return controller;
    }

    private static AnimationClip CreateClipFromSheet(EnemyDef def, string sheetName, string outputDir, float frameRate, bool loop)
    {
        string sheetPath = $"{SpriteDir}/{def.spriteFolder}/{sheetName}.png";

        // Load all sprites from the sheet
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        var sprites = new List<Sprite>();
        foreach (Object obj in assets)
        {
            if (obj is Sprite sprite)
                sprites.Add(sprite);
        }

        // Sort by name to ensure correct frame order
        sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[CreateAllEnemies] No sprites found in {sheetPath}");
            return new AnimationClip { name = sheetName };
        }

        // Build the animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = $"{def.name}_{sheetName}";
        clip.frameRate = frameRate;

        // Create keyframes for the sprite swap
        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // Set loop
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Save clip as asset
        string clipPath = $"{outputDir}/{clip.name}.anim";
        AssetDatabase.CreateAsset(clip, clipPath);

        return clip;
    }

    private static Sprite LoadFirstSprite(string spriteFolder, string sheetName)
    {
        string sheetPath = $"{SpriteDir}/{spriteFolder}/{sheetName}.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        foreach (Object obj in assets)
        {
            if (obj is Sprite sprite)
                return sprite;
        }
        return null;
    }

    private static void WireWaveConfig(List<(GameObject prefab, float weight, int minWave)> prefabs)
    {
        WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
        if (config == null)
        {
            Debug.LogWarning("[CreateAllEnemies] SurvivalWaveConfig not found, skipping wave config update.");
            return;
        }

        // Build new pool: keep existing entries, add new ones
        var pool = new List<WaveConfig.EnemySpawnEntry>();
        if (config.enemyPool != null)
        {
            foreach (var entry in config.enemyPool)
            {
                if (entry != null && entry.prefab != null)
                    pool.Add(entry);
            }
        }

        foreach (var (prefab, weight, minWave) in prefabs)
        {
            // Check if already in pool
            bool exists = false;
            foreach (var entry in pool)
            {
                if (entry.prefab == prefab)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                pool.Add(new WaveConfig.EnemySpawnEntry
                {
                    prefab = prefab,
                    spawnWeight = weight,
                    minWaveToAppear = minWave,
                });
            }
        }

        Undo.RecordObject(config, "Update Wave Config Enemy Pool");
        config.enemyPool = pool.ToArray();
        EditorUtility.SetDirty(config);

        Debug.Log($"[CreateAllEnemies] Wave config now has {pool.Count} enemies in pool.");
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
