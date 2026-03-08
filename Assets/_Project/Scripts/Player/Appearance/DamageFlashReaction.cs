using UnityEngine;

/// <summary>
/// Reacts to player damage by flashing white then swapping to the angry head.
/// The angry head is a full head sprite with an angry expression, matched to skin tone.
/// Attach to the Player GameObject alongside LayeredSpriteController and HealthSystem.
/// </summary>
public class DamageFlashReaction : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float angryFaceDuration = 1.0f;

    [Header("Angry Face (overlay on Eyes slot — one per skin tone)")]
    [SerializeField] private BodyPartData angryFace;

    private LayeredSpriteController layeredSprite;
    private HealthSystem healthSystem;
    private BodyPartData normalEyes;
    private float flashTimer;
    private float angryTimer;
    private bool isFlashing;
    private bool isAngry;

    private void Awake()
    {
        layeredSprite = GetComponent<LayeredSpriteController>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(float damage)
    {
        if (layeredSprite == null || healthSystem.IsDead)
            return;

        // White flash on the character sprites
        layeredSprite.FlashAll(flashColor);
        flashTimer = flashDuration;
        isFlashing = true;

        // Overlay angry face on Eyes slot (renders above Head)
        if (angryFace != null)
        {
            if (!isAngry)
                normalEyes = layeredSprite.GetPart(BodyPartSlot.Eyes);
            layeredSprite.SetPart(BodyPartSlot.Eyes, angryFace);
            angryTimer = angryFaceDuration;
            isAngry = true;
        }
    }

    private void Update()
    {
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                layeredSprite.RestoreAllTints();
                isFlashing = false;
            }
        }

        if (isAngry)
        {
            angryTimer -= Time.deltaTime;
            if (angryTimer <= 0f)
            {
                layeredSprite.SetPart(BodyPartSlot.Eyes, normalEyes);
                normalEyes = null;
                isAngry = false;
            }
        }
    }
}
