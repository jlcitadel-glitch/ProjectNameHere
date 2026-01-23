using UnityEngine;

public class DoubleJumpAbility : MonoBehaviour
{
    [Header("Double Jump Settings")]
    [SerializeField] int extraJumps = 1;

    private int jumpsUsed = 0;

    public bool CanJump()
    {
        return jumpsUsed < extraJumps;
    }

    public void ConsumeJump()
    {
        jumpsUsed++;
    }

    public void ResetJumps()
    {
        jumpsUsed = 0;
    }
}