using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu item that creates the Guardian Boss prefab with all required components,
/// wires the GuardianBossData ScriptableObject, configures BossController,
/// and assigns the prefab as bossPrefab in SurvivalWaveConfig.
/// </summary>
public static class CreateGuardianBoss
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/GuardianBoss.prefab";
    private const string DataPath = "Assets/_Project/ScriptableObjects/Enemies/Types/GuardianBossData.asset";
    private const string WaveConfigPath = "Assets/_Project/ScriptableObjects/Enemies/SurvivalWaveConfig.asset";

    [MenuItem("Tools/Create Guardian Boss")]
    public static void Create()
    {
        // Load the EnemyData asset
        EnemyData bossData = AssetDatabase.LoadAssetAtPath<EnemyData>(DataPath);
        if (bossData == null)
        {
            Debug.LogError($"[CreateGuardianBoss] GuardianBossData not found at {DataPath}. Import the asset first.");
            return;
        }

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Guardian Boss",
                "GuardianBoss.prefab already exists. Overwrite?", "Overwrite", "Cancel"))
            {
                return;
            }
        }

        // Ensure prefab directory exists
        EnsureDirectoryExists("Assets/_Project/Prefabs/Enemies");

        // Create the root GameObject
        GameObject bossGO = new GameObject("GuardianBoss");

        // --- SpriteRenderer with placeholder ---
        SpriteRenderer sr = bossGO.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.sortingOrder = 1;

        // --- Rigidbody2D ---
        Rigidbody2D rb = bossGO.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.mass = 5f;

        // --- BoxCollider2D (larger than standard enemies) ---
        BoxCollider2D col = bossGO.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.8f, 2.2f);
        col.offset = new Vector2(0f, 0.1f);

        // --- Animator (no controller yet — designer assigns later) ---
        bossGO.AddComponent<Animator>();

        // --- AudioSource ---
        AudioSource audioSource = bossGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // --- HealthSystem ---
        bossGO.AddComponent<HealthSystem>();

        // --- EnemyController (wire EnemyData) ---
        EnemyController controller = bossGO.AddComponent<EnemyController>();
        SerializedObject soController = new SerializedObject(controller);
        soController.FindProperty("enemyData").objectReferenceValue = bossData;
        soController.FindProperty("debugLogging").boolValue = true;
        soController.ApplyModifiedProperties();

        // --- GroundPatrolMovement ---
        bossGO.AddComponent<GroundPatrolMovement>();

        // --- EnemyCombat ---
        bossGO.AddComponent<EnemyCombat>();

        // --- EnemySensors ---
        bossGO.AddComponent<EnemySensors>();

        // --- BossController ---
        BossController boss = bossGO.AddComponent<BossController>();
        SerializedObject soBoss = new SerializedObject(boss);
        soBoss.FindProperty("bossName").stringValue = "Guardian";
        soBoss.FindProperty("phase2HealthPercent").floatValue = 0.5f;
        soBoss.FindProperty("enrageHealthPercent").floatValue = 0.2f;
        soBoss.FindProperty("phase2SpeedMultiplier").floatValue = 1.3f;
        soBoss.FindProperty("phase2DamageMultiplier").floatValue = 1.2f;
        soBoss.FindProperty("phase2CooldownMultiplier").floatValue = 0.7f;
        soBoss.FindProperty("enrageSpeedMultiplier").floatValue = 1.5f;
        soBoss.FindProperty("enrageDamageMultiplier").floatValue = 1.5f;
        soBoss.FindProperty("enrageCooldownMultiplier").floatValue = 0.56f;
        soBoss.FindProperty("debugLogging").boolValue = true;
        soBoss.ApplyModifiedProperties();

        // --- Layer and Tag ---
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            bossGO.layer = enemyLayer;
        }
        else
        {
            // Fallback: layer 13 is configured as Enemy in TagManager
            bossGO.layer = 13;
            Debug.LogWarning("[CreateGuardianBoss] 'Enemy' layer not found by name, falling back to layer 13.");
        }
        bossGO.tag = "Enemy";

        // --- Child Transforms ---
        CreateChildTransform(bossGO, "GroundCheck", new Vector3(0f, -1.1f, 0f));
        CreateChildTransform(bossGO, "WallCheck", new Vector3(0.9f, 0f, 0f));
        CreateChildTransform(bossGO, "LedgeCheck", new Vector3(0.9f, -1.1f, 0f));
        CreateChildTransform(bossGO, "AttackOrigin", new Vector3(0.5f, 0.2f, 0f));

        // --- Save as Prefab ---
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bossGO, PrefabPath);
        Object.DestroyImmediate(bossGO);

        // --- Wire into SurvivalWaveConfig ---
        WaveConfig waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
        if (waveConfig != null)
        {
            SerializedObject soWave = new SerializedObject(waveConfig);
            soWave.FindProperty("bossPrefab").objectReferenceValue = prefab;
            soWave.ApplyModifiedProperties();
            EditorUtility.SetDirty(waveConfig);
            Debug.Log("[CreateGuardianBoss] Assigned GuardianBoss as bossPrefab in SurvivalWaveConfig.");
        }
        else
        {
            Debug.LogWarning($"[CreateGuardianBoss] SurvivalWaveConfig not found at {WaveConfigPath}. Assign bossPrefab manually.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new prefab
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[CreateGuardianBoss] Guardian Boss prefab created at {PrefabPath}");
    }

    private static void CreateChildTransform(GameObject parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = localPosition;
    }

    private static Sprite CreatePlaceholderSprite()
    {
        // Create a 32x48 placeholder texture (larger than standard enemies)
        int width = 32;
        int height = 48;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color bodyColor = new Color(0.4f, 0.2f, 0.5f, 1f);   // dark purple
        Color eyeColor = new Color(1f, 0.2f, 0.2f, 1f);       // red eyes
        Color outlineColor = new Color(0.2f, 0.1f, 0.25f, 1f); // darker outline

        // Fill transparent
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        // Draw body (rectangle with outline)
        for (int y = 2; y < height - 2; y++)
        {
            for (int x = 4; x < width - 4; x++)
            {
                bool isOutline = (x == 4 || x == width - 5 || y == 2 || y == height - 3);
                pixels[y * width + x] = isOutline ? outlineColor : bodyColor;
            }
        }

        // Draw eyes (two red squares near the top)
        for (int y = height - 14; y < height - 8; y++)
        {
            for (int x = 8; x < 13; x++)
                pixels[y * width + x] = eyeColor;
            for (int x = 19; x < 24; x++)
                pixels[y * width + x] = eyeColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // Save texture as asset so the prefab can reference it
        string texDir = "Assets/_Project/Art/Sprites/Enemies";
        EnsureDirectoryExists(texDir);
        string texPath = texDir + "/GuardianBoss_Placeholder.png";

        byte[] pngData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(
            System.IO.Path.GetFullPath(texPath), pngData);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        // Configure texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
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
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }
    }
}
