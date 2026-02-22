using UnityEngine;

/// <summary>
/// Bridges the Animator state machine to a frame index for the layered sprite system.
/// Reads the current Animator state's normalized time and computes a global frame index
/// using the AnimationStateFrameMap. Fires OnFrameChanged when the frame advances.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimationFrameDriver : MonoBehaviour
{
    [SerializeField] private AnimationStateFrameMap frameMap;

    private Animator animator;
    private int currentFrame = -1;
    private string currentStateName;

    public int CurrentFrame => currentFrame;
    public string CurrentStateName => currentStateName;

    public event System.Action<int> OnFrameChanged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (animator == null || frameMap == null)
            return;

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            return;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        string stateName = GetStateName(stateInfo);

        if (stateName == null)
            return;

        currentStateName = stateName;
        int newFrame = frameMap.GetGlobalFrameIndex(stateName, stateInfo.normalizedTime);

        if (newFrame != currentFrame)
        {
            currentFrame = newFrame;
            OnFrameChanged?.Invoke(currentFrame);
        }
    }

    /// <summary>
    /// Resolves the current animator state to a name by checking known state name hashes.
    /// Uses the frameMap entries as the source of known state names.
    /// </summary>
    private string GetStateName(AnimatorStateInfo stateInfo)
    {
        if (frameMap.entries == null)
            return null;

        for (int i = 0; i < frameMap.entries.Length; i++)
        {
            var entry = frameMap.entries[i];
            if (stateInfo.IsName(entry.stateName))
                return entry.stateName;
        }

        // Check fallback
        if (frameMap.fallback != null && frameMap.fallback.stateName != null)
        {
            if (stateInfo.IsName(frameMap.fallback.stateName))
                return frameMap.fallback.stateName;
        }

        // No match found — use fallback state name if available
        return frameMap.fallback?.stateName;
    }

    public void SetFrameMap(AnimationStateFrameMap newMap)
    {
        frameMap = newMap;
        currentFrame = -1;
    }
}
