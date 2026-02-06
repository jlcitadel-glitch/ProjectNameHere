using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor wizard that creates a fully wired SurvivalArena in the active scene.
/// Creates the arena GameObject, spawn points, and hooks up all component references.
/// Run via Tools > Arena Setup Wizard.
/// </summary>
public class ArenaSetupWizard : EditorWindow
{
    private Vector2 arenaCenter = Vector2.zero;
    private Vector2 arenaSize = new Vector2(20f, 10f);
    private int spawnPointCount = 6;
    private WaveConfig waveConfig;

    [MenuItem("Tools/Arena Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<ArenaSetupWizard>("Arena Setup Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("Survival Arena Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Arena", EditorStyles.boldLabel);
        arenaCenter = EditorGUILayout.Vector2Field("Center Position", arenaCenter);
        arenaSize = EditorGUILayout.Vector2Field("Size", arenaSize);
        spawnPointCount = EditorGUILayout.IntSlider("Spawn Points", spawnPointCount, 2, 10);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        waveConfig = (WaveConfig)EditorGUILayout.ObjectField("Wave Config", waveConfig, typeof(WaveConfig), false);

        EditorGUILayout.Space();

        if (waveConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a WaveConfig asset. Create one via:\nRight-click > Create > Enemies > Wave Config", MessageType.Warning);
        }
        else
        {
            bool hasValidEntries = false;
            if (waveConfig.enemyPool != null)
            {
                foreach (var entry in waveConfig.enemyPool)
                {
                    if (entry != null && entry.prefab != null)
                    {
                        hasValidEntries = true;
                        break;
                    }
                }
            }

            if (!hasValidEntries)
            {
                EditorGUILayout.HelpBox("WaveConfig has no enemy prefabs assigned. Click below to auto-populate with Slime, Bat, and Turret.", MessageType.Warning);
            }

            if (GUILayout.Button("Auto-Populate Enemy Pool"))
            {
                AutoPopulateEnemyPool();
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Arena", GUILayout.Height(30)))
        {
            CreateArena();
        }
    }

    private void CreateArena()
    {
        // --- Root GameObject ---
        GameObject arenaGO = new GameObject("SurvivalArena");
        arenaGO.transform.position = new Vector3(arenaCenter.x, arenaCenter.y, 0f);
        Undo.RegisterCreatedObjectUndo(arenaGO, "Create Survival Arena");

        // BoxCollider2D trigger (covers the arena area)
        BoxCollider2D col = arenaGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = arenaSize;

        // --- Add components ---
        EnemySpawnManager spawnMgr = arenaGO.AddComponent<EnemySpawnManager>();
        WaveManager waveMgr = arenaGO.AddComponent<WaveManager>();
        SurvivalArena arena = arenaGO.AddComponent<SurvivalArena>();

        // --- Create spawn points as children ---
        Transform[] points = new Transform[spawnPointCount];
        for (int i = 0; i < spawnPointCount; i++)
        {
            GameObject sp = new GameObject($"SpawnPoint_{i + 1}");
            sp.transform.SetParent(arenaGO.transform);

            // Distribute around the perimeter
            float angle = (360f / spawnPointCount) * i * Mathf.Deg2Rad;
            float rx = (arenaSize.x * 0.4f) * Mathf.Cos(angle);
            float ry = (arenaSize.y * 0.4f) * Mathf.Sin(angle);
            sp.transform.localPosition = new Vector3(rx, ry, 0f);

            points[i] = sp.transform;

            // Add a small gizmo icon for visibility
            Undo.RegisterCreatedObjectUndo(sp, "Create Spawn Point");
        }

        // --- Wire references via SerializedObject ---
        // EnemySpawnManager.spawnPoints
        SerializedObject soSpawn = new SerializedObject(spawnMgr);
        SerializedProperty spawnPointsProp = soSpawn.FindProperty("spawnPoints");
        spawnPointsProp.arraySize = spawnPointCount;
        for (int i = 0; i < spawnPointCount; i++)
        {
            spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        }
        soSpawn.ApplyModifiedProperties();

        // WaveManager references
        SerializedObject soWave = new SerializedObject(waveMgr);
        soWave.FindProperty("waveConfig").objectReferenceValue = waveConfig;
        soWave.FindProperty("spawnManager").objectReferenceValue = spawnMgr;
        soWave.ApplyModifiedProperties();

        // SurvivalArena references
        SerializedObject soArena = new SerializedObject(arena);
        soArena.FindProperty("waveManager").objectReferenceValue = waveMgr;
        soArena.FindProperty("spawnManager").objectReferenceValue = spawnMgr;
        soArena.ApplyModifiedProperties();

        // Select the new arena
        Selection.activeGameObject = arenaGO;
        EditorGUIUtility.PingObject(arenaGO);

        string configStatus = waveConfig != null ? "WaveConfig assigned" : "WARNING: no WaveConfig assigned — drag one in the Inspector";

        Debug.Log($"[ArenaSetupWizard] Arena created at ({arenaCenter.x}, {arenaCenter.y}) with {spawnPointCount} spawn points. {configStatus}.");
    }

    private void AutoPopulateEnemyPool()
    {
        if (waveConfig == null)
            return;

        // Find enemy prefabs by path
        var entries = new System.Collections.Generic.List<WaveConfig.EnemySpawnEntry>();

        AddEnemyEntry(entries, "Assets/_Project/Prefabs/Enemies/Slime.prefab", 1.0f, 1);
        AddEnemyEntry(entries, "Assets/_Project/Prefabs/Enemies/Bat.prefab", 0.8f, 3);
        AddEnemyEntry(entries, "Assets/_Project/Prefabs/Enemies/Turret.prefab", 0.6f, 5);

        if (entries.Count == 0)
        {
            Debug.LogWarning("[ArenaSetupWizard] No enemy prefabs found in Assets/_Project/Prefabs/Enemies/");
            return;
        }

        Undo.RecordObject(waveConfig, "Populate Wave Config Enemy Pool");
        waveConfig.enemyPool = entries.ToArray();
        EditorUtility.SetDirty(waveConfig);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ArenaSetupWizard] Added {entries.Count} enemies to WaveConfig pool.");
    }

    private void AddEnemyEntry(System.Collections.Generic.List<WaveConfig.EnemySpawnEntry> list, string path, float weight, int minWave)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            list.Add(new WaveConfig.EnemySpawnEntry
            {
                prefab = prefab,
                spawnWeight = weight,
                minWaveToAppear = minWave
            });
        }
        else
        {
            Debug.LogWarning($"[ArenaSetupWizard] Prefab not found at: {path}");
        }
    }
}
