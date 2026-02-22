using UnityEngine;

/// <summary>
/// Maps Animator state names to frame ranges within the body part sprite arrays.
/// All BodyPartData frames[] arrays follow this same layout, enabling synchronized
/// frame-index-driven animation across all layers.
/// </summary>
[CreateAssetMenu(fileName = "NewFrameMap", menuName = "Game/Character/Animation Frame Map")]
public class AnimationStateFrameMap : ScriptableObject
{
    [System.Serializable]
    public class StateFrameEntry
    {
        [Tooltip("Animator state name (must match exactly)")]
        public string stateName;

        [Tooltip("Index of the first frame for this animation in the frames[] array")]
        public int startFrameIndex;

        [Tooltip("Number of frames in this animation")]
        public int frameCount;

        [Tooltip("Playback speed in frames per second")]
        public float frameRate = 8f;

        [Tooltip("Whether this animation loops")]
        public bool loop = true;
    }

    [Tooltip("Frame mapping for each animator state")]
    public StateFrameEntry[] entries;

    [Tooltip("Fallback entry used when the current animator state isn't found in the map")]
    public StateFrameEntry fallback;

    /// <summary>
    /// Finds the frame entry for the given animator state name.
    /// Returns the fallback entry if no match is found.
    /// </summary>
    public StateFrameEntry GetEntry(string stateName)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].stateName == stateName)
                    return entries[i];
            }
        }
        return fallback;
    }

    /// <summary>
    /// Computes the global frame index for a given state and normalized time.
    /// </summary>
    public int GetGlobalFrameIndex(string stateName, float normalizedTime)
    {
        var entry = GetEntry(stateName);
        if (entry == null || entry.frameCount <= 0)
            return 0;

        int localFrame;
        if (entry.loop)
        {
            localFrame = Mathf.FloorToInt((normalizedTime % 1f) * entry.frameCount);
            localFrame = Mathf.Clamp(localFrame, 0, entry.frameCount - 1);
        }
        else
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);
            localFrame = Mathf.Min(
                Mathf.FloorToInt(clampedTime * entry.frameCount),
                entry.frameCount - 1);
        }

        return entry.startFrameIndex + localFrame;
    }
}
