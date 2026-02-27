using UnityEngine;

public enum PowerUpType
{
    DoubleJump,
    Dash
}

public class PowerUpPickup : MonoBehaviour
{
    [Header("PowerUp Settings")]
    [SerializeField] private PowerUpType powerUpType;
    [SerializeField] private bool destroyOnPickup = true;

    public PowerUpType Type => powerUpType;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (logDebug) Debug.Log($"Trigger detected! Object: {other.gameObject.name}, IsPlayer: {other.CompareTag("Player")}");

        if (other.CompareTag("Player"))
        {
            if (logDebug) Debug.Log("Player detected! Granting power-up...");
            GrantPowerUp(other.gameObject);

            if (destroyOnPickup)
            {
                // Spawn collection VFX before destroying
                if (TryGetComponent<PowerUpVFX>(out var vfx))
                    vfx.SpawnCollectionVFX();

                Destroy(gameObject);
            }
        }
        else
        {
            if (logDebug) Debug.Log($"Not player. Object: {other.gameObject.name}");
        }
    }

    private void GrantPowerUp(GameObject player)
    {
        // Get or add PowerUpManager
        if (!player.TryGetComponent<PowerUpManager>(out var powerUpManager))
        {
            powerUpManager = player.AddComponent<PowerUpManager>();
        }

        // Register the unlock
        powerUpManager.UnlockPowerUp(powerUpType);

        if (!player.TryGetComponent<PlayerControllerScript>(out var controller))
        {
            Debug.LogError($"[PowerUpPickup] PlayerControllerScript not found on {player.name}");
            return;
        }

        switch (powerUpType)
        {
            case PowerUpType.DoubleJump:
                if (player.GetComponent<DoubleJumpAbility>() == null)
                {
                    player.AddComponent<DoubleJumpAbility>();
                    controller.RefreshAbilities();
                    if (logDebug) Debug.Log("Double Jump Unlocked!");
                }
                break;

            case PowerUpType.Dash:
                if (player.GetComponent<DashAbility>() == null)
                {
                    player.AddComponent<DashAbility>();
                    controller.RefreshAbilities();
                    if (logDebug) Debug.Log("Dash Unlocked!");
                }
                break;
        }
    }
}