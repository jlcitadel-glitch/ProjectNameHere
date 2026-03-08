/// <summary>
/// Equipment slot categories. Each slot holds one piece of equipment.
/// Serialized as int in .asset files — do not renumber existing values.
/// </summary>
public enum EquipmentSlotType
{
    Weapon = 0,
    Armor = 1,
    Legs = 2,
    Accessory = 3,
    Feet = 4
}
