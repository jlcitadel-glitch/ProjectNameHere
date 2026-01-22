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

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger detected! Object: {other.gameObject.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected! Granting power-up...");
            GrantPowerUp(other.gameObject);

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"Not player. Expected 'Player' tag but got '{other.tag}'");
        }
    }

    void GrantPowerUp(GameObject player)
    {
        switch (powerUpType)
        {
            case PowerUpType.DoubleJump:
                if (player.GetComponent<DoubleJumpAbility>() == null)
                {
                    player.AddComponent<DoubleJumpAbility>();
                    player.GetComponent<PlayerControllerScript>().RefreshAbilities();
                    Debug.Log("Double Jump Unlocked!");
                }
                break;

            case PowerUpType.Dash:
                if (player.GetComponent<DashAbility>() == null)
                {
                    player.AddComponent<DashAbility>();
                    player.GetComponent<PlayerControllerScript>().RefreshAbilities();
                    Debug.Log("Dash Unlocked!");
                }
                break;
        }
    }
}