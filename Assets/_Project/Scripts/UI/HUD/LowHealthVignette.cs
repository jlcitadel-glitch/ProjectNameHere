using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Screen edge vignette effect that appears when health is low.
    /// Pulses in intensity based on health percentage.
    /// </summary>
    public class LowHealthVignette : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image vignetteImage;

        [Header("Thresholds")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private float criticalHealthThreshold = 0.15f;

        [Header("Style")]
        [SerializeField] private Color vignetteColor = new Color(0.5f, 0f, 0f, 0.6f);
        [SerializeField] private Color criticalColor = new Color(0.8f, 0f, 0f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float criticalPulseSpeed = 4f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 0.7f;
        [SerializeField] private float fadeSpeed = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip heartbeatSound;
        [SerializeField] private float heartbeatVolume = 0.3f;

        private HealthSystem healthSystem;
        private AudioSource audioSource;
        private float currentAlpha;
        private float targetAlpha;
        private float pulseTimer;
        private bool isLowHealth;
        private bool isCritical;
        private float lastHeartbeat;

        private void Start()
        {
            FindHealthSystem();
            InitializeAudio();
            SetVignetteAlpha(0f);
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            UpdatePulse();
            UpdateAlpha();
            UpdateHeartbeat();
        }

        private void FindHealthSystem()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.OnHealthChanged += HandleHealthChanged;
                    // Initialize based on current health
                    HandleHealthChanged(healthSystem.CurrentHealth, healthSystem.MaxHealth);
                    Debug.Log("[LowHealthVignette] Connected to HealthSystem");
                }
            }
        }

        private void InitializeAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.loop = false;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float percent = max > 0f ? current / max : 0f;

            bool wasLowHealth = isLowHealth;
            isLowHealth = percent <= lowHealthThreshold && percent > 0f;
            isCritical = percent <= criticalHealthThreshold && percent > 0f;

            if (!isLowHealth)
            {
                targetAlpha = 0f;
            }

            // Play heartbeat on transition to low health
            if (isLowHealth && !wasLowHealth)
            {
                PlayHeartbeat();
            }
        }

        private void UpdatePulse()
        {
            if (!isLowHealth)
                return;

            float speed = isCritical ? criticalPulseSpeed : pulseSpeed;
            pulseTimer += Time.deltaTime * speed;

            // Calculate pulse using sine wave
            float pulse = (Mathf.Sin(pulseTimer * Mathf.PI * 2f) + 1f) / 2f;

            // Lerp between min and max alpha
            float alphaRange = isCritical ? maxAlpha : maxAlpha * 0.7f;
            targetAlpha = Mathf.Lerp(minAlpha, alphaRange, pulse);
        }

        private void UpdateAlpha()
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            SetVignetteAlpha(currentAlpha);
        }

        private void UpdateHeartbeat()
        {
            if (!isLowHealth || heartbeatSound == null)
                return;

            float interval = isCritical ? 0.5f : 0.8f;

            if (Time.time - lastHeartbeat >= interval)
            {
                PlayHeartbeat();
            }
        }

        private void PlayHeartbeat()
        {
            if (heartbeatSound != null && audioSource != null && isLowHealth)
            {
                audioSource.PlayOneShot(heartbeatSound, heartbeatVolume);
                lastHeartbeat = Time.time;
            }
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (vignetteImage == null)
                return;

            Color c = isCritical ? criticalColor : vignetteColor;
            c.a = alpha;
            vignetteImage.color = c;
        }

        /// <summary>
        /// Manually sets the health system reference.
        /// </summary>
        public void SetHealthSystem(HealthSystem system)
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            healthSystem = system;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
        }

        /// <summary>
        /// Creates a radial vignette sprite programmatically.
        /// Call this from editor script if needed.
        /// </summary>
        public static Texture2D CreateVignetteTexture(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDist = dist / maxDist;

                    // Create radial gradient from transparent center to opaque edges
                    float alpha = Mathf.Clamp01(Mathf.Pow(normalizedDist, 2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
