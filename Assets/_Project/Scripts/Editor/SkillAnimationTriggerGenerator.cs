#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to populate animationTrigger fields on all SkillData assets.
/// Maps each skill to the appropriate ULPC animator trigger (Slash,
/// Thrust, Spellcast, Shoot) based on skill combat style.
/// Run from Tools menu.
/// </summary>
public static class SkillAnimationTriggerGenerator
{
    [MenuItem("Tools/Wire Skill Animation Triggers")]
    public static void WireAll()
    {
        var guids = AssetDatabase.FindAssets("t:SkillData");
        int wired = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null) continue;

            string trigger = GetTriggerForSkill(skill.skillId);
            if (trigger == null) continue; // passives get no trigger

            if (skill.animationTrigger != trigger)
            {
                skill.animationTrigger = trigger;
                EditorUtility.SetDirty(skill);
                wired++;
                Debug.Log($"[SkillAnimationTriggerGenerator] {skill.skillId} -> {trigger}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillAnimationTriggerGenerator] Wired {wired} skill animation triggers.");
    }

    private static string GetTriggerForSkill(string skillId)
    {
        // Slash   = melee swings, battle stance
        // Thrust  = stabs, piercing attacks
        // Spellcast = magic, buffs, healing
        // Shoot   = ranged projectiles
        return skillId switch
        {
            // Melee / physical skills -> Slash
            "power_strike" or "triple_slash" or "quick_strike"
                or "ground_slam" or "shadow_strike" => "Slash",

            // Warrior buff shouts -> Slash (battle stance)
            "berserk" or "war_cry" => "Slash",

            // Thrust / stab skills
            "poison_blade" => "Thrust",

            // Magic / casting skills -> Spellcast
            "fireball" or "ice_bolt" or "meteor"
                or "recovery" or "guard" or "magic_shield" => "Spellcast",

            // Evasion is instant (no animation needed)
            "evasion" => null,

            // Passives have no animation
            "critical_eye" or "iron_skin"
                or "mana_mastery" or "critical_mastery" => null,

            _ => null
        };
    }
}
#endif
