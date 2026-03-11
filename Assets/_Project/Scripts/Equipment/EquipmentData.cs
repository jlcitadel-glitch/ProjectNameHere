using UnityEngine;

/// <summary>
/// ScriptableObject defining a piece of equipment.
/// Equipment provides stat bonuses and optionally changes character appearance.
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "Game/Equipment/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("Identity")]
    public string equipmentId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Slot")]
    public EquipmentSlotType slotType;

    [Header("Stat Bonuses")]
    [Tooltip("Bonus to Strength stat")]
    public int bonusSTR;
    [Tooltip("Bonus to Intelligence stat")]
    public int bonusINT;
    [Tooltip("Bonus to Agility stat")]
    public int bonusAGI;

    [Header("Visual (Optional)")]
    [Tooltip("BodyPartData to display on character when equipped (armor/boots)")]
    public BodyPartData visualPart;

    [Header("Weapon (Optional)")]
    [Tooltip("WeaponData for combat system (weapon slot only)")]
    public WeaponData weaponData;

    /// <summary>
    /// Returns a formatted stat summary for UI display.
    /// </summary>
    public string GetStatSummary()
    {
        var sb = new System.Text.StringBuilder();
        if (bonusSTR != 0) sb.Append($"STR {bonusSTR:+#;-#}   ");
        if (bonusINT != 0) sb.Append($"INT {bonusINT:+#;-#}   ");
        if (bonusAGI != 0) sb.Append($"AGI {bonusAGI:+#;-#}   ");
        return sb.ToString().TrimEnd();
    }
}
