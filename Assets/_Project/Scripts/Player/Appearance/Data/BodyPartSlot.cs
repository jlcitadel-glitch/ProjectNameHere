/// <summary>
/// Defines the layer slots for a modular LPC character.
/// Int values determine default rendering order (higher = in front).
/// Safe to renumber — save data stores partId strings, not enum ints.
/// </summary>
public enum BodyPartSlot
{
    WeaponBehind = 0,
    Shadow = 1,
    Body = 2,
    Head = 3,
    Eyes = 4,
    Beard = 5,
    Torso = 6,
    Shoulders = 7,
    Cape = 8,
    Legs = 9,
    Feet = 10,
    Gloves = 11,
    Hair = 12,
    Hat = 13,
    Accessories = 14,
    WeaponFront = 15,
    Shield = 16
}
