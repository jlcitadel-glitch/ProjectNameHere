using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to set design card defaults on all enemy data assets
/// and create starter encounter template assets.
/// Run via Tools > Setup Encounter System.
/// </summary>
public static class SetupEncounterSystem
{
    private const string DataDir = "Assets/_Project/ScriptableObjects/Enemies/Types";
    private const string EncounterDir = "Assets/_Project/ScriptableObjects/Enemies/Encounters";
    private const string WaveConfigPath = "Assets/_Project/ScriptableObjects/Enemies/SurvivalWaveConfig.asset";

    [MenuItem("Tools/Setup Encounter System")]
    public static void Setup()
    {
        EnsureDirectoryExists(EncounterDir);

        SetDesignCardDefaults();
        CreateEncounterTemplates();
        WireWaveConfig();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupEncounterSystem] Complete — design card defaults set, encounter templates created.");
    }

    private static void SetDesignCardDefaults()
    {
        SetDefaults("SlimeData",      CombatRole.DPS,        ThreatClock.ShortFuse, 3, 2, 1, 4, 3, false);
        SetDefaults("MiniSlimeData",  CombatRole.DPS,        ThreatClock.Immediate, 4, 3, 1, 3, 3, true);
        SetDefaults("MicroSlimeData", CombatRole.DPS,        ThreatClock.Immediate, 4, 4, 1, 3, 2, true);
        SetDefaults("MushroomData",   CombatRole.Controller, ThreatClock.Delayed,   3, 2, 1, 4, 3, false);
        SetDefaults("BatData",        CombatRole.DPS,        ThreatClock.Immediate, 4, 4, 1, 2, 3, false);
        SetDefaults("GoblinData",     CombatRole.DPS,        ThreatClock.ShortFuse, 4, 3, 1, 3, 4, false);
        SetDefaults("FlyingEyeData",  CombatRole.DPS,        ThreatClock.ShortFuse, 4, 4, 3, 2, 4, false);
        SetDefaults("SkeletonData",   CombatRole.Tank,       ThreatClock.Delayed,   2, 1, 2, 4, 4, false);
        SetDefaults("TurretData",     CombatRole.Artillery,  ThreatClock.Delayed,   3, 1, 5, 3, 5, false);
        SetDefaults("GuardianBossData", CombatRole.Tank,     ThreatClock.Delayed,   4, 2, 3, 3, 5, false);
    }

    private static void SetDefaults(string assetName, CombatRole role, ThreatClock threat,
        int aggression, int mobility, int range, int predictability, int persistence, bool deathSpawnOnly)
    {
        string path = $"{DataDir}/{assetName}.asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data == null)
        {
            Debug.LogWarning($"[SetupEncounterSystem] {assetName} not found at {path} — skipping");
            return;
        }

        Undo.RecordObject(data, $"Set design card defaults for {assetName}");
        data.combatRole = role;
        data.threatClock = threat;
        data.axisAggression = aggression;
        data.axisMobility = mobility;
        data.axisRange = range;
        data.axisPredictability = predictability;
        data.axisPersistence = persistence;
        data.isDeathSpawnOnly = deathSpawnOnly;
        EditorUtility.SetDirty(data);

        Debug.Log($"[SetupEncounterSystem] Set defaults for {assetName}: {role}, {threat}");
    }

    private static void CreateEncounterTemplates()
    {
        CreateTemplate("BasicPack",       1, 1.5f, new[] { CombatRole.DPS, CombatRole.DPS });
        CreateTemplate("GroundAndAir",    3, 1.0f, new[] { CombatRole.DPS, CombatRole.DPS });
        CreateTemplate("TankAndDPS",      5, 1.0f, new[] { CombatRole.Tank, CombatRole.DPS });
        CreateTemplate("ZoneDenial",      4, 0.8f, new[] { CombatRole.Controller, CombatRole.DPS });
        CreateTemplate("ArtilleryScreen", 6, 0.7f, new[] { CombatRole.Artillery, CombatRole.DPS, CombatRole.DPS });
        CreateTemplate("NightmarePack",   8, 0.5f, new[] { CombatRole.Tank, CombatRole.DPS, CombatRole.DPS, CombatRole.Controller });
    }

    private static void CreateTemplate(string name, int minWave, float weight, CombatRole[] roles)
    {
        string path = $"{EncounterDir}/{name}.asset";

        // Don't overwrite existing
        if (AssetDatabase.LoadAssetAtPath<EncounterTemplate>(path) != null)
        {
            Debug.Log($"[SetupEncounterSystem] {name} already exists — skipping");
            return;
        }

        EncounterTemplate template = ScriptableObject.CreateInstance<EncounterTemplate>();
        template.encounterName = name;
        template.minWaveToAppear = minWave;
        template.selectionWeight = weight;

        template.slots = new EncounterTemplate.RoleSlot[roles.Length];
        for (int i = 0; i < roles.Length; i++)
        {
            template.slots[i] = new EncounterTemplate.RoleSlot { role = roles[i] };
        }

        AssetDatabase.CreateAsset(template, path);
        Debug.Log($"[SetupEncounterSystem] Created encounter template: {name} ({roles.Length} slots, minWave={minWave})");
    }

    private static void WireWaveConfig()
    {
        WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
        if (config == null)
        {
            Debug.LogWarning("[SetupEncounterSystem] SurvivalWaveConfig not found — skipping wire-up");
            return;
        }

        // Load all encounter templates from the directory
        string[] guids = AssetDatabase.FindAssets("t:EncounterTemplate", new[] { EncounterDir });
        var templates = new EncounterTemplate[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            templates[i] = AssetDatabase.LoadAssetAtPath<EncounterTemplate>(assetPath);
        }

        Undo.RecordObject(config, "Wire encounter templates into WaveConfig");
        config.encounterTemplates = templates;
        EditorUtility.SetDirty(config);

        Debug.Log($"[SetupEncounterSystem] Wired {templates.Length} encounter templates into SurvivalWaveConfig");
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
