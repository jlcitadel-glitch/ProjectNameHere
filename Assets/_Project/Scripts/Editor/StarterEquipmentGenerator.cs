#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to generate starter equipment ScriptableObject assets
/// for Warrior, Mage, and Rogue classes. Run from Tools menu.
/// </summary>
public static class StarterEquipmentGenerator
{
    private const string EQUIPMENT_PATH = "Assets/_Project/Resources/Equipment";

    [MenuItem("Tools/Generate Starter Equipment")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(EQUIPMENT_PATH))
            Directory.CreateDirectory(EQUIPMENT_PATH);

        // Warrior gear
        CreateEquipment("warrior_sword", "Iron Sword", "A sturdy iron blade.",
            EquipmentSlotType.Weapon, bonusSTR: 2, visualPartId: "weapon_longsword");
        CreateEquipment("warrior_chainmail", "Chainmail", "Heavy chain armor.",
            EquipmentSlotType.Armor, bonusSTR: 1, bonusAGI: 1, visualPartId: "torso_chainmail");
        CreateEquipment("warrior_greaves", "Iron Greaves", "Iron leg armor.",
            EquipmentSlotType.Boots, bonusSTR: 1, visualPartId: "legs_armour");

        // Mage gear
        CreateEquipment("mage_staff", "Apprentice Staff", "A staff crackling with arcane energy.",
            EquipmentSlotType.Weapon, bonusINT: 2, visualPartId: "weapon_staff");
        CreateEquipment("mage_robe", "Cloth Robe", "Enchanted cloth robes.",
            EquipmentSlotType.Armor, bonusINT: 2, visualPartId: "torso_longsleeve");
        CreateEquipment("mage_shoes", "Cloth Shoes", "Light mage footwear.",
            EquipmentSlotType.Boots, bonusAGI: 1, visualPartId: "legs_pants_teal");

        // Rogue gear
        CreateEquipment("rogue_dagger", "Iron Dagger", "A quick and deadly blade.",
            EquipmentSlotType.Weapon, bonusSTR: 1, bonusAGI: 1, visualPartId: "weapon_dagger");
        CreateEquipment("rogue_vest", "Leather Vest", "Lightweight leather armor.",
            EquipmentSlotType.Armor, bonusSTR: 1, bonusAGI: 1, visualPartId: "torso_leather");
        CreateEquipment("rogue_boots", "Leather Boots", "Swift leather boots.",
            EquipmentSlotType.Boots, bonusAGI: 2, visualPartId: "legs_boots");

        // Wire to JobClassData assets
        WireStarterGear();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StarterEquipmentGenerator] All starter equipment generated and wired.");
    }

    private static void CreateEquipment(string id, string displayName, string description,
        EquipmentSlotType slot, int bonusSTR = 0, int bonusINT = 0, int bonusAGI = 0,
        string visualPartId = null)
    {
        string path = $"{EQUIPMENT_PATH}/{id}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
        if (existing != null)
        {
            // Update existing
            existing.equipmentId = id;
            existing.displayName = displayName;
            existing.description = description;
            existing.slotType = slot;
            existing.bonusSTR = bonusSTR;
            existing.bonusINT = bonusINT;
            existing.bonusAGI = bonusAGI;
            WireVisualPart(existing, visualPartId);
            EditorUtility.SetDirty(existing);
            return;
        }

        var asset = ScriptableObject.CreateInstance<EquipmentData>();
        asset.equipmentId = id;
        asset.displayName = displayName;
        asset.description = description;
        asset.slotType = slot;
        asset.bonusSTR = bonusSTR;
        asset.bonusINT = bonusINT;
        asset.bonusAGI = bonusAGI;
        WireVisualPart(asset, visualPartId);

        AssetDatabase.CreateAsset(asset, path);
    }

    private static void WireVisualPart(EquipmentData equipment, string visualPartId)
    {
        if (string.IsNullOrEmpty(visualPartId))
            return;

        string bpPath = $"Assets/_Project/ScriptableObjects/Character/BodyParts/{visualPartId}.asset";
        var part = AssetDatabase.LoadAssetAtPath<BodyPartData>(bpPath);
        if (part != null)
        {
            equipment.visualPart = part;
        }
        else
        {
            Debug.LogWarning($"[StarterEquipmentGenerator] BodyPartData not found at: {bpPath}");
        }
    }

    private static void WireStarterGear()
    {
        var allJobs = FindAllJobClassData();

        foreach (var job in allJobs)
        {
            if (job == null || string.IsNullOrEmpty(job.jobId)) continue;
            string id = job.jobId.ToLower();

            EquipmentData[] gear = null;

            if (id == "warrior")
            {
                gear = new[]
                {
                    LoadEquipment("warrior_sword"),
                    LoadEquipment("warrior_chainmail"),
                    LoadEquipment("warrior_greaves")
                };
            }
            else if (id == "mage")
            {
                gear = new[]
                {
                    LoadEquipment("mage_staff"),
                    LoadEquipment("mage_robe"),
                    LoadEquipment("mage_shoes")
                };
            }
            else if (id == "rogue")
            {
                gear = new[]
                {
                    LoadEquipment("rogue_dagger"),
                    LoadEquipment("rogue_vest"),
                    LoadEquipment("rogue_boots")
                };
            }

            if (gear != null)
            {
                job.starterEquipment = gear;
                EditorUtility.SetDirty(job);
            }
        }
    }

    private static EquipmentData LoadEquipment(string id)
    {
        return AssetDatabase.LoadAssetAtPath<EquipmentData>($"{EQUIPMENT_PATH}/{id}.asset");
    }

    private static JobClassData[] FindAllJobClassData()
    {
        var guids = AssetDatabase.FindAssets("t:JobClassData");
        var result = new JobClassData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            result[i] = AssetDatabase.LoadAssetAtPath<JobClassData>(path);
        }
        return result;
    }
}
#endif
