using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility that fills EncounterTemplate role slots from the enemy pool.
/// Pure logic — no MonoBehaviour dependency (like WaveScaler).
/// </summary>
public static class EncounterBuilder
{
    /// <summary>
    /// Builds a list of enemy prefabs by filling each template slot from the eligible pool.
    /// Returns an empty list only if no slots can be filled at all.
    /// </summary>
    public static List<GameObject> BuildEncounter(
        EncounterTemplate template,
        List<WaveConfig.EnemySpawnEntry> eligiblePool)
    {
        var result = new List<GameObject>();
        var pickedData = new List<EnemyData>();

        foreach (var slot in template.slots)
        {
            GameObject prefab = FillSlot(slot, eligiblePool, pickedData);
            if (prefab != null)
            {
                result.Add(prefab);
                EnemyController controller = prefab.GetComponent<EnemyController>();
                if (controller != null && controller.Data != null)
                    pickedData.Add(controller.Data);
            }
        }

        return result;
    }

    private static GameObject FillSlot(
        EncounterTemplate.RoleSlot slot,
        List<WaveConfig.EnemySpawnEntry> pool,
        List<EnemyData> alreadyPicked)
    {
        // Pinned override — find the matching prefab in pool
        if (slot.pinned != null)
        {
            foreach (var entry in pool)
            {
                if (entry.prefab == null) continue;
                EnemyController ctrl = entry.prefab.GetComponent<EnemyController>();
                if (ctrl != null && ctrl.Data == slot.pinned)
                    return entry.prefab;
            }
            Debug.LogWarning($"[EncounterBuilder] Pinned enemy '{slot.pinned.enemyName}' not found in pool — skipping slot");
            return null;
        }

        // Filter pool by role, exclude deathSpawnOnly
        var candidates = new List<WaveConfig.EnemySpawnEntry>();
        foreach (var entry in pool)
        {
            if (entry.prefab == null) continue;
            EnemyController ctrl = entry.prefab.GetComponent<EnemyController>();
            if (ctrl == null || ctrl.Data == null) continue;
            if (ctrl.Data.isDeathSpawnOnly) continue;
            if (ctrl.Data.combatRole != slot.role) continue;
            candidates.Add(entry);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[EncounterBuilder] No candidates for role {slot.role} — skipping slot");
            return null;
        }

        // Weighted random pick
        GameObject picked = PickWeighted(candidates);

        // Anti-synergy check: if picked enemy conflicts with already-picked, re-roll once
        EnemyData pickedData = picked.GetComponent<EnemyController>().Data;
        if (HasAntiSynergy(pickedData, alreadyPicked) && candidates.Count > 1)
        {
            // Remove the conflicting entry and try again
            candidates.RemoveAll(e =>
            {
                var c = e.prefab.GetComponent<EnemyController>();
                return c != null && c.Data == pickedData;
            });
            if (candidates.Count > 0)
                picked = PickWeighted(candidates);
        }

        return picked;
    }

    private static GameObject PickWeighted(List<WaveConfig.EnemySpawnEntry> candidates)
    {
        float totalWeight = 0f;
        foreach (var c in candidates)
            totalWeight += c.spawnWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var c in candidates)
        {
            cumulative += c.spawnWeight;
            if (roll <= cumulative)
                return c.prefab;
        }

        return candidates[candidates.Count - 1].prefab;
    }

    private static bool HasAntiSynergy(EnemyData candidate, List<EnemyData> existing)
    {
        if (candidate.antiSynergyPartners == null || candidate.antiSynergyPartners.Length == 0)
            return false;

        foreach (var partner in candidate.antiSynergyPartners)
        {
            if (partner == null) continue;
            foreach (var picked in existing)
            {
                if (picked == partner)
                    return true;
            }
        }
        return false;
    }
}
