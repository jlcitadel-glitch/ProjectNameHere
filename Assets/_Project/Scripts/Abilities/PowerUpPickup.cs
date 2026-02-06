using UnityEngine;

public enum PowerUpType
{
    DoubleJump,
    Dash
}

public class PowerUpPickup : MonoBehaviour
{
    [Header("PowerUp Settings")]
    [SerializeField] PowerUpType powerUpType;
    [SerializeField] bool destroyOnPickup = true;

    [Header("Debug")]
    [SerializeField] bool logDebug = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (logDebug) Debug.Log($"Trigger detected! Object: {other.gameObject.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            if (logDebug) Debug.Log("Player detected! Granting power-up...");
            GrantPowerUp(other.gameObject);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (logDebug) Debug.Log($"Not player. Expected 'Player' tag but got '{other.tag}'");
        }
    }

    void GrantPowerUp(GameObject player)
    {
        // Get or add PowerUpManager
        PowerUpManager powerUpManager = player.GetComponent<PowerUpManager>();
        if (powerUpManager == null)
        {
            powerUpManager = player.AddComponent<PowerUpManager>();
        }

        // Register the unlock
        powerUpManager.UnlockPowerUp(powerUpType);

        switch (powerUpType)
        {
            case PowerUpType.DoubleJump:
                if (player.GetComponent<DoubleJumpAbility>() == null)
                {
                    player.AddComponent<DoubleJumpAbility>();
                    player.GetComponent<PlayerControllerScript>().RefreshAbilities();
                    if (logDebug) Debug.Log("Double Jump Unlocked!");
                }
                break;

            case PowerUpType.Dash:
                if (player.GetComponent<DashAbility>() == null)
                {
                    player.AddComponent<DashAbility>();
                    player.GetComponent<PlayerControllerScript>().RefreshAbilities();
                    if (logDebug) Debug.Log("Dash Unlocked!");
                }
                break;
        }
    }
}