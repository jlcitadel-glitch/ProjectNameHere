using UnityEngine;

/// <summary>
/// Auto-destroys the GameObject after all child ParticleSystems finish playing,
/// or after a maximum lifetime (whichever comes first).
/// Attach to VFX prefab roots.
/// </summary>
public class SelfDestructVFX : MonoBehaviour
{
    [SerializeField] private float maxLifetime = 5f;

    private ParticleSystem[] particleSystems;
    private float timer;

    private void Awake()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Wait at least one frame before checking completion
        if (timer < 0.1f)
            return;

        // Check if all particle systems have stopped
        bool allStopped = true;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null && particleSystems[i].IsAlive(true))
            {
                allStopped = false;
                break;
            }
        }

        if (allStopped)
        {
            Destroy(gameObject);
        }
    }
}
