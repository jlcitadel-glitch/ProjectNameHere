#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom inspector for PrecipitationPreset with tiered settings:
/// - Essential: Always visible
/// - Advanced: Collapsed by default
/// - Expert: Only visible in debug mode
/// </summary>
[CustomEditor(typeof(PrecipitationPreset))]
public class PrecipitationPresetEditor : Editor
{
    // Foldout states (persistent across selection changes)
    private static bool showAdvancedSize = false;
    private static bool showAdvancedEmission = false;
    private static bool showAdvancedMovement = false;
    private static bool showAdvancedRotation = false;
    private static bool showAdvancedFade = false;
    private static bool showAdvancedRendering = false;
    private static bool showExpertCollision = false;
    private static bool showLegacySpawn = false;

    // Essential properties
    private SerializedProperty typeProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty intensityProp;
    private SerializedProperty particleSpriteProp;
    private SerializedProperty tintProp;

    // Advanced - Size
    private SerializedProperty sizeRangeProp;
    private SerializedProperty sizeVariationProp;

    // Advanced - Emission
    private SerializedProperty emissionRateProp;
    private SerializedProperty lifetimeProp;
    private SerializedProperty lifetimeVariationProp;
    private SerializedProperty maxParticlesProp;

    // Legacy Spawn (deprecated)
    private SerializedProperty spawnAreaSizeProp;
    private SerializedProperty spawnOffsetProp;

    // Advanced - Movement
    private SerializedProperty fallSpeedProp;
    private SerializedProperty fallSpeedVariationProp;
    private SerializedProperty driftAmountProp;
    private SerializedProperty driftFrequencyProp;
    private SerializedProperty windInfluenceMultiplierProp;
    private SerializedProperty turbulenceInfluenceMultiplierProp;

    // Advanced - Rotation
    private SerializedProperty enableRotationProp;
    private SerializedProperty rotationSpeedRangeProp;

    // Advanced - Fade
    private SerializedProperty fadeInProp;
    private SerializedProperty fadeInPointProp;
    private SerializedProperty fadeOutProp;
    private SerializedProperty fadeOutPointProp;

    // Advanced - Rendering
    private SerializedProperty sortingLayerNameProp;
    private SerializedProperty orderInLayerProp;
    private SerializedProperty zOffsetProp;

    // Expert - Collision
    private SerializedProperty enableCollisionProp;
    private SerializedProperty collisionLayersProp;
    private SerializedProperty collisionBounceProp;
    private SerializedProperty collisionLifetimeLossProp;

    private void OnEnable()
    {
        // Essential
        typeProp = serializedObject.FindProperty("type");
        displayNameProp = serializedObject.FindProperty("displayName");
        intensityProp = serializedObject.FindProperty("intensity");
        particleSpriteProp = serializedObject.FindProperty("particleSprite");
        tintProp = serializedObject.FindProperty("tint");

        // Advanced - Size
        sizeRangeProp = serializedObject.FindProperty("sizeRange");
        sizeVariationProp = serializedObject.FindProperty("sizeVariation");

        // Advanced - Emission
        emissionRateProp = serializedObject.FindProperty("emissionRate");
        lifetimeProp = serializedObject.FindProperty("lifetime");
        lifetimeVariationProp = serializedObject.FindProperty("lifetimeVariation");
        maxParticlesProp = serializedObject.FindProperty("maxParticles");

        // Legacy Spawn
        spawnAreaSizeProp = serializedObject.FindProperty("spawnAreaSize");
        spawnOffsetProp = serializedObject.FindProperty("spawnOffset");

        // Advanced - Movement
        fallSpeedProp = serializedObject.FindProperty("fallSpeed");
        fallSpeedVariationProp = serializedObject.FindProperty("fallSpeedVariation");
        driftAmountProp = serializedObject.FindProperty("driftAmount");
        driftFrequencyProp = serializedObject.FindProperty("driftFrequency");
        windInfluenceMultiplierProp = serializedObject.FindProperty("windInfluenceMultiplier");
        turbulenceInfluenceMultiplierProp = serializedObject.FindProperty("turbulenceInfluenceMultiplier");

        // Advanced - Rotation
        enableRotationProp = serializedObject.FindProperty("enableRotation");
        rotationSpeedRangeProp = serializedObject.FindProperty("rotationSpeedRange");

        // Advanced - Fade
        fadeInProp = serializedObject.FindProperty("fadeIn");
        fadeInPointProp = serializedObject.FindProperty("fadeInPoint");
        fadeOutProp = serializedObject.FindProperty("fadeOut");
        fadeOutPointProp = serializedObject.FindProperty("fadeOutPoint");

        // Advanced - Rendering
        sortingLayerNameProp = serializedObject.FindProperty("sortingLayerName");
        orderInLayerProp = serializedObject.FindProperty("orderInLayer");
        zOffsetProp = serializedObject.FindProperty("zOffset");

        // Expert - Collision
        enableCollisionProp = serializedObject.FindProperty("enableCollision");
        collisionLayersProp = serializedObject.FindProperty("collisionLayers");
        collisionBounceProp = serializedObject.FindProperty("collisionBounce");
        collisionLifetimeLossProp = serializedObject.FindProperty("collisionLifetimeLoss");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PrecipitationPreset preset = (PrecipitationPreset)target;

        // ==================== ESSENTIAL ====================
        EditorGUILayout.LabelField("Essential Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));
        EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Display Name"));

        EditorGUILayout.Space(5);

        // Intensity slider with preview
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(intensityProp, new GUIContent("Intensity"));
        EditorGUILayout.EndHorizontal();

        // Show derived values
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField(
            $"Effective Emission: {preset.GetEffectiveEmissionRate():F1}/s",
            EditorStyles.miniLabel
        );
        EditorGUILayout.LabelField(
            $"Effective Max Particles: {preset.GetEffectiveMaxParticles()}",
            EditorStyles.miniLabel
        );
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(particleSpriteProp, new GUIContent("Particle Sprite"));
        EditorGUILayout.PropertyField(tintProp, new GUIContent("Tint Color"));

        EditorGUILayout.Space(10);

        // ==================== ADVANCED ====================
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Expand sections below to fine-tune particle behavior.", MessageType.None);

        EditorGUILayout.Space(5);

        // Size
        showAdvancedSize = EditorGUILayout.Foldout(showAdvancedSize, "Size", true);
        if (showAdvancedSize)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sizeRangeProp, new GUIContent("Size Range"));
            EditorGUILayout.PropertyField(sizeVariationProp, new GUIContent("Size Variation"));
            EditorGUI.indentLevel--;
        }

        // Emission
        showAdvancedEmission = EditorGUILayout.Foldout(showAdvancedEmission, "Emission", true);
        if (showAdvancedEmission)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(emissionRateProp, new GUIContent("Base Emission Rate"));
            EditorGUILayout.PropertyField(lifetimeProp, new GUIContent("Lifetime"));
            EditorGUILayout.PropertyField(lifetimeVariationProp, new GUIContent("Lifetime Variation"));
            EditorGUILayout.PropertyField(maxParticlesProp, new GUIContent("Base Max Particles"));
            EditorGUI.indentLevel--;
        }

        // Movement
        showAdvancedMovement = EditorGUILayout.Foldout(showAdvancedMovement, "Movement", true);
        if (showAdvancedMovement)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fallSpeedProp, new GUIContent("Fall Speed"));
            EditorGUILayout.PropertyField(fallSpeedVariationProp, new GUIContent("Fall Speed Variation"));
            EditorGUILayout.PropertyField(driftAmountProp, new GUIContent("Drift Amount"));
            EditorGUILayout.PropertyField(driftFrequencyProp, new GUIContent("Drift Frequency"));
            EditorGUILayout.PropertyField(windInfluenceMultiplierProp, new GUIContent("Wind Influence"));
            EditorGUILayout.PropertyField(turbulenceInfluenceMultiplierProp, new GUIContent("Turbulence Influence"));
            EditorGUI.indentLevel--;
        }

        // Rotation
        showAdvancedRotation = EditorGUILayout.Foldout(showAdvancedRotation, "Rotation", true);
        if (showAdvancedRotation)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableRotationProp, new GUIContent("Enable Rotation"));
            if (enableRotationProp.boolValue)
            {
                EditorGUILayout.PropertyField(rotationSpeedRangeProp, new GUIContent("Rotation Speed Range"));
            }
            EditorGUI.indentLevel--;
        }

        // Fade
        showAdvancedFade = EditorGUILayout.Foldout(showAdvancedFade, "Fade", true);
        if (showAdvancedFade)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fadeInProp, new GUIContent("Fade In"));
            if (fadeInProp.boolValue)
            {
                EditorGUILayout.PropertyField(fadeInPointProp, new GUIContent("Fade In Point"));
            }
            EditorGUILayout.PropertyField(fadeOutProp, new GUIContent("Fade Out"));
            if (fadeOutProp.boolValue)
            {
                EditorGUILayout.PropertyField(fadeOutPointProp, new GUIContent("Fade Out Point"));
            }
            EditorGUI.indentLevel--;
        }

        // Rendering
        showAdvancedRendering = EditorGUILayout.Foldout(showAdvancedRendering, "Rendering", true);
        if (showAdvancedRendering)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sortingLayerNameProp, new GUIContent("Sorting Layer"));
            EditorGUILayout.PropertyField(orderInLayerProp, new GUIContent("Order in Layer"));
            EditorGUILayout.PropertyField(zOffsetProp, new GUIContent("Z Offset"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ==================== EXPERT ====================
        EditorGUILayout.LabelField("Expert Settings", EditorStyles.boldLabel);

        // Collision
        showExpertCollision = EditorGUILayout.Foldout(showExpertCollision, "Collision", true);
        if (showExpertCollision)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableCollisionProp, new GUIContent("Enable Collision"));
            if (enableCollisionProp.boolValue)
            {
                EditorGUILayout.PropertyField(collisionLayersProp, new GUIContent("Collision Layers"));
                EditorGUILayout.PropertyField(collisionBounceProp, new GUIContent("Bounce"));
                EditorGUILayout.PropertyField(collisionLifetimeLossProp, new GUIContent("Lifetime Loss"));
            }
            EditorGUI.indentLevel--;
        }

        // Legacy Spawn (deprecated)
        showLegacySpawn = EditorGUILayout.Foldout(showLegacySpawn, "Legacy Spawn Area (Deprecated)", true);
        if (showLegacySpawn)
        {
            EditorGUILayout.HelpBox(
                "These settings are deprecated. Use the PrecipitationController's Bounds settings instead.",
                MessageType.Warning
            );
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(spawnAreaSizeProp, new GUIContent("Spawn Area Size"));
            EditorGUILayout.PropertyField(spawnOffsetProp, new GUIContent("Spawn Offset"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
