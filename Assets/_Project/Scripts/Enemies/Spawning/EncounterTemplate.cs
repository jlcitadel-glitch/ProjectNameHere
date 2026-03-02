using System;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a role-based encounter group.
/// Each slot specifies a CombatRole; the EncounterBuilder fills slots
/// from the enemy pool at runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewEncounter", menuName = "Enemies/Encounter Template")]
public class EncounterTemplate : ScriptableObject
{
    [Serializable]
    public class RoleSlot
    {
        public CombatRole role;
        [Tooltip("If set, always use this enemy for this slot instead of picking from the pool")]
        public EnemyData pinned;
    }

    public string encounterName;
    public RoleSlot[] slots;
    [Tooltip("First wave this encounter can appear in")]
    public int minWaveToAppear = 1;
    [Tooltip("Relative selection frequency. Higher = more likely to be picked")]
    public float selectionWeight = 1f;
}
