#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to generate sample precipitation presets.
/// Access via Tools > Precipitation > Create Sample Presets
/// </summary>
public static class PrecipitationPresetFactory
{
    private const string PRESET_PATH = "Assets/_Project/ScriptableObjects/Precipitation";

    private static void EnsureDirectoryExists()
    {
        // Create directories step by step
        if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            AssetDatabase.Refresh();
        }
        if (!AssetDatabase.IsValidFolder(PRESET_PATH))
        {
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Precipitation");
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/Precipitation/Migrate Existing Presets")]
    public static void MigrateExistingPresets()
    {
        string[] guids = AssetDatabase.FindAssets("t:PrecipitationPreset", new[] { PRESET_PATH });
        int migrated = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PrecipitationPreset preset = AssetDatabase.LoadAssetAtPath<PrecipitationPreset>(path);

            if (preset == null) continue;

            bool modified = false;

            // Infer type from name if not set
            if (preset.type == PrecipitationType.Custom || preset.type == PrecipitationType.Rain)
            {
                string nameLower = preset.displayName.ToLower();
                PrecipitationType inferredType = InferTypeFromName(nameLower);

                if (inferredType != preset.type)
                {
                    preset.type = inferredType;
                    modified = true;
                }
            }

            // Calculate intensity from emission rate if using default
            if (Mathf.Approximately(preset.intensity, 0.5f))
            {
                // Estimate intensity based on emission rate relative to typical ranges
                float normalizedEmission = Mathf.InverseLerp(10f, 200f, preset.emissionRate);
                preset.intensity = Mathf.Clamp01(normalizedEmission);
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(preset);
                migrated++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PrecipitationPresetFactory] Migrated {migrated} presets to new format.");
    }

    private static PrecipitationType InferTypeFromName(string nameLower)
    {
        if (nameLower.Contains("rain") || nameLower.Contains("storm"))
            return PrecipitationType.Rain;
        if (nameLower.Contains("snow") || nameLower.Contains("blizzard"))
            return PrecipitationType.Snow;
        if (nameLower.Contains("ash"))
            return PrecipitationType.Ash;
        if (nameLower.Contains("spore"))
            return PrecipitationType.Spores;
        if (nameLower.Contains("pollen") || nameLower.Contains("seed"))
            return PrecipitationType.Pollen;
        if (nameLower.Contains("dust") || nameLower.Contains("debris"))
            return PrecipitationType.Dust;
        if (nameLower.Contains("ember"))
            return PrecipitationType.Embers;

        return PrecipitationType.Custom;
    }

    [MenuItem("Tools/Precipitation/Create Sample Presets")]
    public static void CreateAllSamplePresets()
    {
        EnsureDirectoryExists();

        CreateRainPresets();
        CreateSnowPresets();
        CreateAshPreset();
        CreateSporePreset();
        CreatePollenPreset();
        CreateDustPreset();
        CreateEmberPreset();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PrecipitationPresetFactory] Created sample presets in {PRESET_PATH}");

        // Ping the folder in Project window
        var folder = AssetDatabase.LoadAssetAtPath<Object>(PRESET_PATH);
        EditorGUIUtility.PingObject(folder);
    }

    [MenuItem("Tools/Precipitation/Create Rain Presets")]
    public static void CreateRainPresets()
    {
        EnsureDirectoryExists();

        // Light Rain
        var lightRain = CreatePreset("Rain_Light");
        lightRain.type = PrecipitationType.Rain;
        lightRain.displayName = "Light Rain";
        lightRain.intensity = 0.3f;
        lightRain.tint = new Color(0.7f, 0.8f, 0.9f, 0.6f);
        lightRain.sizeRange = new Vector2(0.02f, 0.04f);
        lightRain.emissionRate = 30f;
        lightRain.lifetime = 1.5f;
        lightRain.lifetimeVariation = 0.3f;
        lightRain.maxParticles = 300;
        lightRain.fallSpeed = 12f;
        lightRain.fallSpeedVariation = 2f;
        lightRain.driftAmount = 0.1f;
        lightRain.driftFrequency = 0.5f;
        lightRain.windInfluenceMultiplier = 0.4f;
        lightRain.turbulenceInfluenceMultiplier = 0.2f;
        lightRain.enableRotation = false;
        lightRain.fadeIn = false;
        lightRain.fadeOut = true;
        lightRain.fadeOutPoint = 0.9f;
        lightRain.zOffset = -1f;
        SavePreset(lightRain, "Rain_Light");

        // Heavy Rain
        var heavyRain = CreatePreset("Rain_Heavy");
        heavyRain.type = PrecipitationType.Rain;
        heavyRain.displayName = "Heavy Rain";
        heavyRain.intensity = 0.7f;
        heavyRain.tint = new Color(0.6f, 0.7f, 0.85f, 0.7f);
        heavyRain.sizeRange = new Vector2(0.03f, 0.06f);
        heavyRain.emissionRate = 80f;
        heavyRain.lifetime = 1.2f;
        heavyRain.lifetimeVariation = 0.2f;
        heavyRain.maxParticles = 600;
        heavyRain.fallSpeed = 18f;
        heavyRain.fallSpeedVariation = 3f;
        heavyRain.driftAmount = 0.15f;
        heavyRain.driftFrequency = 0.3f;
        heavyRain.windInfluenceMultiplier = 0.6f;
        heavyRain.turbulenceInfluenceMultiplier = 0.3f;
        heavyRain.enableRotation = false;
        heavyRain.fadeIn = false;
        heavyRain.fadeOut = true;
        heavyRain.fadeOutPoint = 0.85f;
        heavyRain.zOffset = -1f;
        SavePreset(heavyRain, "Rain_Heavy");

        // Storm Rain
        var stormRain = CreatePreset("Rain_Storm");
        stormRain.type = PrecipitationType.Rain;
        stormRain.displayName = "Storm Rain";
        stormRain.intensity = 1.0f;
        stormRain.tint = new Color(0.5f, 0.6f, 0.75f, 0.8f);
        stormRain.sizeRange = new Vector2(0.04f, 0.08f);
        stormRain.emissionRate = 100f;
        stormRain.lifetime = 1.0f;
        stormRain.lifetimeVariation = 0.2f;
        stormRain.maxParticles = 600;
        stormRain.fallSpeed = 22f;
        stormRain.fallSpeedVariation = 4f;
        stormRain.driftAmount = 0.3f;
        stormRain.driftFrequency = 0.5f;
        stormRain.windInfluenceMultiplier = 1.0f;
        stormRain.turbulenceInfluenceMultiplier = 0.5f;
        stormRain.enableRotation = false;
        stormRain.fadeIn = false;
        stormRain.fadeOut = true;
        stormRain.fadeOutPoint = 0.8f;
        stormRain.zOffset = -1f;
        SavePreset(stormRain, "Rain_Storm");
    }

    [MenuItem("Tools/Precipitation/Create Snow Presets")]
    public static void CreateSnowPresets()
    {
        EnsureDirectoryExists();

        // Light Snow
        var lightSnow = CreatePreset("Snow_Light");
        lightSnow.type = PrecipitationType.Snow;
        lightSnow.displayName = "Light Snow";
        lightSnow.intensity = 0.3f;
        lightSnow.tint = new Color(1f, 1f, 1f, 0.8f);
        lightSnow.sizeRange = new Vector2(0.04f, 0.1f);
        lightSnow.sizeVariation = 0.5f;
        lightSnow.emissionRate = 25f;
        lightSnow.lifetime = 6f;
        lightSnow.lifetimeVariation = 1f;
        lightSnow.maxParticles = 400;
        lightSnow.fallSpeed = 1.5f;
        lightSnow.fallSpeedVariation = 0.5f;
        lightSnow.driftAmount = 1.2f;
        lightSnow.driftFrequency = 0.4f;
        lightSnow.windInfluenceMultiplier = 2.0f;
        lightSnow.turbulenceInfluenceMultiplier = 1.5f;
        lightSnow.enableRotation = true;
        lightSnow.rotationSpeedRange = new Vector2(-60f, 60f);
        lightSnow.fadeIn = true;
        lightSnow.fadeInPoint = 0.1f;
        lightSnow.fadeOut = true;
        lightSnow.fadeOutPoint = 0.85f;
        lightSnow.zOffset = -1f;
        SavePreset(lightSnow, "Snow_Light");

        // Heavy Snow
        var heavySnow = CreatePreset("Snow_Heavy");
        heavySnow.type = PrecipitationType.Snow;
        heavySnow.displayName = "Heavy Snow";
        heavySnow.intensity = 0.7f;
        heavySnow.tint = new Color(0.95f, 0.97f, 1f, 0.9f);
        heavySnow.sizeRange = new Vector2(0.06f, 0.15f);
        heavySnow.sizeVariation = 0.4f;
        heavySnow.emissionRate = 50f;
        heavySnow.lifetime = 5f;
        heavySnow.lifetimeVariation = 1f;
        heavySnow.maxParticles = 500;
        heavySnow.fallSpeed = 2.5f;
        heavySnow.fallSpeedVariation = 0.8f;
        heavySnow.driftAmount = 1.5f;
        heavySnow.driftFrequency = 0.5f;
        heavySnow.windInfluenceMultiplier = 1.8f;
        heavySnow.turbulenceInfluenceMultiplier = 1.2f;
        heavySnow.enableRotation = true;
        heavySnow.rotationSpeedRange = new Vector2(-90f, 90f);
        heavySnow.fadeIn = true;
        heavySnow.fadeInPoint = 0.1f;
        heavySnow.fadeOut = true;
        heavySnow.fadeOutPoint = 0.8f;
        heavySnow.zOffset = -1f;
        SavePreset(heavySnow, "Snow_Heavy");

        // Blizzard
        var blizzard = CreatePreset("Snow_Blizzard");
        blizzard.type = PrecipitationType.Snow;
        blizzard.displayName = "Blizzard";
        blizzard.intensity = 1.0f;
        blizzard.tint = new Color(0.9f, 0.93f, 1f, 0.85f);
        blizzard.sizeRange = new Vector2(0.05f, 0.12f);
        blizzard.sizeVariation = 0.5f;
        blizzard.emissionRate = 80f;
        blizzard.lifetime = 4f;
        blizzard.lifetimeVariation = 1f;
        blizzard.maxParticles = 600;
        blizzard.fallSpeed = 3f;
        blizzard.fallSpeedVariation = 1f;
        blizzard.driftAmount = 2.5f;
        blizzard.driftFrequency = 0.8f;
        blizzard.windInfluenceMultiplier = 3.0f;
        blizzard.turbulenceInfluenceMultiplier = 2.0f;
        blizzard.enableRotation = true;
        blizzard.rotationSpeedRange = new Vector2(-180f, 180f);
        blizzard.fadeIn = true;
        blizzard.fadeInPoint = 0.05f;
        blizzard.fadeOut = true;
        blizzard.fadeOutPoint = 0.75f;
        blizzard.zOffset = -1f;
        SavePreset(blizzard, "Snow_Blizzard");
    }

    [MenuItem("Tools/Precipitation/Create Ash Preset")]
    public static void CreateAshPreset()
    {
        EnsureDirectoryExists();

        var ash = CreatePreset("Ash_Fall");
        ash.type = PrecipitationType.Ash;
        ash.displayName = "Ash Fall";
        ash.intensity = 0.5f;
        ash.tint = new Color(0.4f, 0.4f, 0.42f, 0.7f);
        ash.sizeRange = new Vector2(0.03f, 0.08f);
        ash.sizeVariation = 0.4f;
        ash.emissionRate = 30f;
        ash.lifetime = 5f;
        ash.lifetimeVariation = 1f;
        ash.maxParticles = 300;
        ash.fallSpeed = 2f;
        ash.fallSpeedVariation = 0.6f;
        ash.driftAmount = 1.0f;
        ash.driftFrequency = 0.3f;
        ash.windInfluenceMultiplier = 1.5f;
        ash.turbulenceInfluenceMultiplier = 1.0f;
        ash.enableRotation = true;
        ash.rotationSpeedRange = new Vector2(-45f, 45f);
        ash.fadeIn = true;
        ash.fadeInPoint = 0.15f;
        ash.fadeOut = true;
        ash.fadeOutPoint = 0.7f;
        ash.zOffset = -1f;
        SavePreset(ash, "Ash_Fall");
    }

    [MenuItem("Tools/Precipitation/Create Spore Preset")]
    public static void CreateSporePreset()
    {
        EnsureDirectoryExists();

        var spores = CreatePreset("Spore_Drift");
        spores.type = PrecipitationType.Spores;
        spores.displayName = "Spore Drift";
        spores.intensity = 0.4f;
        spores.tint = new Color(0.6f, 0.9f, 0.5f, 0.6f);
        spores.sizeRange = new Vector2(0.02f, 0.05f);
        spores.sizeVariation = 0.3f;
        spores.emissionRate = 20f;
        spores.lifetime = 8f;
        spores.lifetimeVariation = 2f;
        spores.maxParticles = 200;
        spores.fallSpeed = 0.5f;
        spores.fallSpeedVariation = 0.3f;
        spores.driftAmount = 2.0f;
        spores.driftFrequency = 0.2f;
        spores.windInfluenceMultiplier = 2.5f;
        spores.turbulenceInfluenceMultiplier = 2.0f;
        spores.enableRotation = true;
        spores.rotationSpeedRange = new Vector2(-30f, 30f);
        spores.fadeIn = true;
        spores.fadeInPoint = 0.2f;
        spores.fadeOut = true;
        spores.fadeOutPoint = 0.75f;
        spores.zOffset = -0.5f;
        SavePreset(spores, "Spore_Drift");
    }

    [MenuItem("Tools/Precipitation/Create Pollen Preset")]
    public static void CreatePollenPreset()
    {
        EnsureDirectoryExists();

        var pollen = CreatePreset("Pollen_Seeds");
        pollen.type = PrecipitationType.Pollen;
        pollen.displayName = "Pollen & Seeds";
        pollen.intensity = 0.35f;
        pollen.tint = new Color(1f, 0.95f, 0.7f, 0.5f);
        pollen.sizeRange = new Vector2(0.015f, 0.04f);
        pollen.sizeVariation = 0.5f;
        pollen.emissionRate = 15f;
        pollen.lifetime = 10f;
        pollen.lifetimeVariation = 3f;
        pollen.maxParticles = 150;
        pollen.fallSpeed = 0.3f;
        pollen.fallSpeedVariation = 0.2f;
        pollen.driftAmount = 2.5f;
        pollen.driftFrequency = 0.15f;
        pollen.windInfluenceMultiplier = 3.0f;
        pollen.turbulenceInfluenceMultiplier = 2.5f;
        pollen.enableRotation = true;
        pollen.rotationSpeedRange = new Vector2(-20f, 20f);
        pollen.fadeIn = true;
        pollen.fadeInPoint = 0.15f;
        pollen.fadeOut = true;
        pollen.fadeOutPoint = 0.8f;
        pollen.zOffset = -0.5f;
        SavePreset(pollen, "Pollen_Seeds");
    }

    [MenuItem("Tools/Precipitation/Create Dust Preset")]
    public static void CreateDustPreset()
    {
        EnsureDirectoryExists();

        var dust = CreatePreset("Dust_Debris");
        dust.type = PrecipitationType.Dust;
        dust.displayName = "Dust & Debris";
        dust.intensity = 0.4f;
        dust.tint = new Color(0.7f, 0.6f, 0.5f, 0.4f);
        dust.sizeRange = new Vector2(0.01f, 0.03f);
        dust.sizeVariation = 0.6f;
        dust.emissionRate = 25f;
        dust.lifetime = 6f;
        dust.lifetimeVariation = 2f;
        dust.maxParticles = 250;
        dust.fallSpeed = 0.4f;
        dust.fallSpeedVariation = 0.3f;
        dust.driftAmount = 1.8f;
        dust.driftFrequency = 0.25f;
        dust.windInfluenceMultiplier = 2.0f;
        dust.turbulenceInfluenceMultiplier = 1.8f;
        dust.enableRotation = true;
        dust.rotationSpeedRange = new Vector2(-90f, 90f);
        dust.fadeIn = true;
        dust.fadeInPoint = 0.1f;
        dust.fadeOut = true;
        dust.fadeOutPoint = 0.7f;
        dust.zOffset = -0.5f;
        SavePreset(dust, "Dust_Debris");
    }

    [MenuItem("Tools/Precipitation/Create Ember Preset")]
    public static void CreateEmberPreset()
    {
        EnsureDirectoryExists();

        var embers = CreatePreset("Embers");
        embers.type = PrecipitationType.Embers;
        embers.displayName = "Embers";
        embers.intensity = 0.45f;
        embers.tint = new Color(1f, 0.5f, 0.1f, 0.9f);
        embers.sizeRange = new Vector2(0.02f, 0.05f);
        embers.sizeVariation = 0.4f;
        embers.emissionRate = 20f;
        embers.lifetime = 4f;
        embers.lifetimeVariation = 1f;
        embers.maxParticles = 200;
        embers.fallSpeed = -0.5f; // Negative = rise upward!
        embers.fallSpeedVariation = 0.8f;
        embers.driftAmount = 1.2f;
        embers.driftFrequency = 0.4f;
        embers.windInfluenceMultiplier = 1.2f;
        embers.turbulenceInfluenceMultiplier = 1.5f;
        embers.enableRotation = true;
        embers.rotationSpeedRange = new Vector2(-120f, 120f);
        embers.fadeIn = true;
        embers.fadeInPoint = 0.1f;
        embers.fadeOut = true;
        embers.fadeOutPoint = 0.6f;
        embers.zOffset = -1f;
        SavePreset(embers, "Embers");

        // Rising Embers variant (more upward)
        var risingEmbers = CreatePreset("Embers_Rising");
        risingEmbers.type = PrecipitationType.Embers;
        risingEmbers.displayName = "Rising Embers";
        risingEmbers.intensity = 0.6f;
        risingEmbers.tint = new Color(1f, 0.6f, 0.2f, 0.85f);
        risingEmbers.sizeRange = new Vector2(0.015f, 0.04f);
        risingEmbers.sizeVariation = 0.5f;
        risingEmbers.emissionRate = 25f;
        risingEmbers.lifetime = 3f;
        risingEmbers.lifetimeVariation = 0.8f;
        risingEmbers.maxParticles = 250;
        risingEmbers.fallSpeed = -2f; // Rise faster
        risingEmbers.fallSpeedVariation = 0.5f;
        risingEmbers.driftAmount = 0.8f;
        risingEmbers.driftFrequency = 0.5f;
        risingEmbers.windInfluenceMultiplier = 1.0f;
        risingEmbers.turbulenceInfluenceMultiplier = 1.2f;
        risingEmbers.enableRotation = true;
        risingEmbers.rotationSpeedRange = new Vector2(-180f, 180f);
        risingEmbers.fadeIn = true;
        risingEmbers.fadeInPoint = 0.15f;
        risingEmbers.fadeOut = true;
        risingEmbers.fadeOutPoint = 0.5f;
        risingEmbers.zOffset = -1f;
        SavePreset(risingEmbers, "Embers_Rising");
    }

    private static PrecipitationPreset CreatePreset(string name)
    {
        var preset = ScriptableObject.CreateInstance<PrecipitationPreset>();
        preset.name = name;
        return preset;
    }

    private static void SavePreset(PrecipitationPreset preset, string fileName)
    {
        string path = $"{PRESET_PATH}/{fileName}.asset";

        // Check if asset already exists
        var existing = AssetDatabase.LoadAssetAtPath<PrecipitationPreset>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(preset, existing);
            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(preset, path);
        }
    }
}
#endif
