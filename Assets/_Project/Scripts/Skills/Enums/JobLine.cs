/// <summary>
/// Advancement lines within each archetype. Used to categorize and organize
/// the job advancement tree. A job belongs to exactly one line.
/// </summary>
public enum JobLine
{
    // -- General --
    None = 0,

    // -- Warrior Lines --
    Fighter = 100,
    Knight = 101,
    Berserker = 102,
    DarkKnight = 103,
    WeaponSpecialist = 104,
    Monk = 105,
    Spellsword = 106,

    // -- Mage Lines --
    Wizard = 200,
    Warlock = 201,
    Summoner = 202,
    Cleric = 203,
    Druid = 204,
    Shaman = 205,
    Enchanter = 206,
    Alchemist = 207,
    GlyphCaster = 208,
    Sage = 209,

    // -- Rogue Lines --
    Thief = 300,
    Bandit = 301,
    Ranger = 302,
    Swashbuckler = 303,
    Scout = 304,
    Dancer = 305,
    Wraith = 306,
    Trapper = 307,

    // -- Cross-Archetype Hybrids --
    WarriorMage = 400,
    WarriorRogue = 401,
    MageRogue = 402,
    TripleHybrid = 403
}
