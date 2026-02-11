# Hazards

> **Unity 6 2D** - All hazards use trigger colliders and deal damage via HealthSystem.

## Base Hazard Pattern

```csharp
// All hazards implement damage via trigger colliders
// Contact damage uses HealthSystem.TakeDamage()
// Hazards should respect invulnerability frames

[RequireComponent(typeof(Collider2D))]
public class BaseHazard : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private Vector2 knockbackDirection = Vector2.up;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var health = other.GetComponent<HealthSystem>();
        if (health != null) health.TakeDamage(damage);
    }
}
```

## Hazard Types

### Static Hazards (Spikes, Pits)

```csharp
// Spikes: trigger collider on surface, constant damage on contact
// Pits: trigger at bottom of pit, instant kill or high damage + respawn
// No movement, no state — simplest hazard type
```

### Projectile Traps

```csharp
// Fires projectiles at intervals or when triggered
[SerializeField] private GameObject projectilePrefab;
[SerializeField] private float fireInterval = 2f;
[SerializeField] private float projectileSpeed = 5f;
[SerializeField] private Transform firePoint;

// Projectiles: Rigidbody2D (Dynamic), trigger collider, auto-destroy on hit/lifetime
```

### Timed Hazards

```csharp
// Hazards that cycle on/off (flame jets, retractable spikes)
[SerializeField] private float activeTime = 1.5f;
[SerializeField] private float inactiveTime = 2f;

// Visual: enable/disable sprite + collider
// Audio: play warning sound before activation
```

### Environmental Damage Zones

```csharp
// Continuous damage while player stays in zone (lava, acid, poison gas)
// Uses OnTriggerStay2D with damage tick rate

[SerializeField] private float damageInterval = 0.5f;
[SerializeField] private int damagePerTick = 1;
```

## Config (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "HazardData", menuName = "Environment/Hazard Data")]
public class HazardData : ScriptableObject
{
    public int damage = 1;
    public float knockbackForce = 5f;
    public bool respectInvulnerability = true;
    public AudioClip hitSound;
    public GameObject hitVFXPrefab;
}
```
