using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks cooldowns for all skills.
/// Provides events for cooldown state changes.
/// </summary>
public class SkillCooldownTracker : MonoBehaviour
{
    private Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();
    private Dictionary<string, float> cooldownDurations = new Dictionary<string, float>();

    /// <summary>
    /// Fired when a skill goes on cooldown. Provides skillId and duration.
    /// </summary>
    public event Action<string, float> OnCooldownStarted;

    /// <summary>
    /// Fired when a skill comes off cooldown.
    /// </summary>
    public event Action<string> OnCooldownEnded;

    /// <summary>
    /// Fired every frame while any skill is on cooldown.
    /// Provides skillId and remaining time.
    /// </summary>
    public event Action<string, float> OnCooldownTick;

    private List<string> expiredCooldowns = new List<string>();

    private void Update()
    {
        if (cooldownEndTimes.Count == 0) return;

        float currentTime = Time.time;
        expiredCooldowns.Clear();

        foreach (var kvp in cooldownEndTimes)
        {
            float remaining = kvp.Value - currentTime;

            if (remaining <= 0f)
            {
                expiredCooldowns.Add(kvp.Key);
            }
            else
            {
                OnCooldownTick?.Invoke(kvp.Key, remaining);
            }
        }

        foreach (var skillId in expiredCooldowns)
        {
            cooldownEndTimes.Remove(skillId);
            cooldownDurations.Remove(skillId);
            OnCooldownEnded?.Invoke(skillId);
        }
    }

    /// <summary>
    /// Starts a cooldown for the specified skill.
    /// </summary>
    public void StartCooldown(string skillId, float duration)
    {
        if (string.IsNullOrEmpty(skillId) || duration <= 0f)
            return;

        cooldownEndTimes[skillId] = Time.time + duration;
        cooldownDurations[skillId] = duration;

        OnCooldownStarted?.Invoke(skillId, duration);
    }

    /// <summary>
    /// Checks if a skill is currently on cooldown.
    /// </summary>
    public bool IsOnCooldown(string skillId)
    {
        if (!cooldownEndTimes.TryGetValue(skillId, out float endTime))
            return false;

        return Time.time < endTime;
    }

    /// <summary>
    /// Gets the remaining cooldown time for a skill.
    /// Returns 0 if not on cooldown.
    /// </summary>
    public float GetRemainingCooldown(string skillId)
    {
        if (!cooldownEndTimes.TryGetValue(skillId, out float endTime))
            return 0f;

        return Mathf.Max(0f, endTime - Time.time);
    }

    /// <summary>
    /// Gets the cooldown progress (0 = just started, 1 = ready).
    /// </summary>
    public float GetCooldownProgress(string skillId)
    {
        if (!cooldownEndTimes.TryGetValue(skillId, out float endTime))
            return 1f;

        if (!cooldownDurations.TryGetValue(skillId, out float duration) || duration <= 0f)
            return 1f;

        float remaining = endTime - Time.time;
        if (remaining <= 0f)
            return 1f;

        return 1f - (remaining / duration);
    }

    /// <summary>
    /// Reduces the cooldown of a skill by the specified amount.
    /// </summary>
    public void ReduceCooldown(string skillId, float amount)
    {
        if (!cooldownEndTimes.ContainsKey(skillId))
            return;

        cooldownEndTimes[skillId] -= amount;

        if (cooldownEndTimes[skillId] <= Time.time)
        {
            cooldownEndTimes.Remove(skillId);
            cooldownDurations.Remove(skillId);
            OnCooldownEnded?.Invoke(skillId);
        }
    }

    /// <summary>
    /// Resets the cooldown of a skill immediately.
    /// </summary>
    public void ResetCooldown(string skillId)
    {
        if (!cooldownEndTimes.ContainsKey(skillId))
            return;

        cooldownEndTimes.Remove(skillId);
        cooldownDurations.Remove(skillId);
        OnCooldownEnded?.Invoke(skillId);
    }

    /// <summary>
    /// Resets all cooldowns immediately.
    /// </summary>
    public void ResetAllCooldowns()
    {
        var skills = new List<string>(cooldownEndTimes.Keys);
        cooldownEndTimes.Clear();
        cooldownDurations.Clear();

        foreach (var skillId in skills)
        {
            OnCooldownEnded?.Invoke(skillId);
        }
    }

    /// <summary>
    /// Gets all skills currently on cooldown.
    /// </summary>
    public string[] GetSkillsOnCooldown()
    {
        var onCooldown = new List<string>();
        float currentTime = Time.time;

        foreach (var kvp in cooldownEndTimes)
        {
            if (kvp.Value > currentTime)
            {
                onCooldown.Add(kvp.Key);
            }
        }

        return onCooldown.ToArray();
    }
}
