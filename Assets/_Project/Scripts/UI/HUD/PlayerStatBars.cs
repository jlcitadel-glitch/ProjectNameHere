using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// World-space stat bars that appear near the player.
    /// Health bar appears ABOVE the player, mana bar BELOW.
    /// Bars are conditionally visible based on resource percentage.
    /// </summary>
    public class PlayerStatBars : MonoBehaviour
    {
        [Header("Health Bar Position")]
        [SerializeField] private Vector2 healthBarOffset = new Vector2(0f, 1.3f);

        [Header("Mana Bar Position")]
        [SerializeField] private Vector2 manaBarOffset = new Vector2(0f, -0.3f);

        [Header("Bar Settings")]
        [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.12f);
        [SerializeField] private float backgroundPadding = 0.02f;

        [Header("Health Bar Colors")]
        [SerializeField] private Color healthFillColor = new Color(0.545f, 0f, 0f, 1f);
        [SerializeField] private Color healthBackgroundColor = new Color(0.2f, 0.05f, 0.05f, 0.8f);

        [Header("Mana Bar Colors")]
        [SerializeField] private Color manaFillColor = new Color(0f, 0.808f, 0.820f, 1f);
        [SerializeField] private Color manaBackgroundColor = new Color(0.05f, 0.1f, 0.2f, 0.8f);

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float lowHealthThreshold = 0.25f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseIntensity = 0.3f;

        [Header("Visibility Rules")]
        [SerializeField] private bool showHealthWhenFull = false;
        [SerializeField] private bool showManaWhenFull = false;
        [SerializeField] private float visibilityFadeDuration = 0.3f;
        [SerializeField] private float recentChangeDisplayTime = 2f;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int baseSortingOrder = 100;

        private HealthSystem healthSystem;
        private ManaSystem manaSystem;

        private GameObject healthBarContainer;
        private GameObject manaBarContainer;
        private SpriteRenderer healthBackground;
        private SpriteRenderer healthFill;
        private SpriteRenderer manaBackground;
        private SpriteRenderer manaFill;

        private float displayedHealth = 1f;
        private float targetHealth = 1f;
        private float displayedMana = 1f;
        private float targetMana = 1f;

        private float healthTargetAlpha = 0f;
        private float healthCurrentAlpha = 0f;
        private float manaTargetAlpha = 0f;
        private float manaCurrentAlpha = 0f;

        private float healthRecentChangeTimer;
        private float manaRecentChangeTimer;

        private float pulseTimer;

        private SpriteRenderer healthGlow;
        private SpriteRenderer manaGlow;
        private Coroutine healthGlowCoroutine;
        private Coroutine manaGlowCoroutine;

        private void Start()
        {
            CreateBars();
            InitializeValues();
        }

        private void OnEnable()
        {
            FindReferences();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            if (healthBarContainer != null)
            {
                Destroy(healthBarContainer);
            }

            if (manaBarContainer != null)
            {
                Destroy(manaBarContainer);
            }
        }

        private void FindReferences()
        {
            if (healthSystem == null)
            {
                healthSystem = GetComponent<HealthSystem>();
            }

            if (manaSystem == null)
            {
                manaSystem = GetComponent<ManaSystem>();
            }
        }

        private void Update()
        {
            UpdateBarPositions();
            UpdateBarFills();
            UpdateRecentChangeTimers();
            UpdateHealthVisibility();
            UpdateManaVisibility();
            UpdatePulse();
        }

        private void CreateBars()
        {
            // Create health bar container (ABOVE player)
            healthBarContainer = new GameObject("HealthBar");
            healthBarContainer.transform.SetParent(transform);
            healthBarContainer.transform.localPosition = healthBarOffset;
            healthBarContainer.transform.localRotation = Quaternion.identity;

            healthBackground = CreateBarSprite(healthBarContainer.transform, "HealthBackground", 0f, healthBackgroundColor, true);
            healthFill = CreateBarSprite(healthBarContainer.transform, "HealthFill", 0f, healthFillColor, false);
            healthFill.sortingOrder = healthBackground.sortingOrder + 1;

            healthGlow = CreateBarSprite(healthBarContainer.transform, "HealthGlow", 0f, new Color(healthFillColor.r, healthFillColor.g, healthFillColor.b, 0f), true);
            healthGlow.sortingOrder = healthBackground.sortingOrder + 2;

            // Create mana bar container (BELOW player)
            manaBarContainer = new GameObject("ManaBar");
            manaBarContainer.transform.SetParent(transform);
            manaBarContainer.transform.localPosition = manaBarOffset;
            manaBarContainer.transform.localRotation = Quaternion.identity;

            manaBackground = CreateBarSprite(manaBarContainer.transform, "ManaBackground", 0f, manaBackgroundColor, true);
            manaFill = CreateBarSprite(manaBarContainer.transform, "ManaFill", 0f, manaFillColor, false);
            manaFill.sortingOrder = manaBackground.sortingOrder + 1;

            manaGlow = CreateBarSprite(manaBarContainer.transform, "ManaGlow", 0f, new Color(manaFillColor.r, manaFillColor.g, manaFillColor.b, 0f), true);
            manaGlow.sortingOrder = manaBackground.sortingOrder + 2;
        }

        private SpriteRenderer CreateBarSprite(Transform parent, string name, float yOffset, Color color, bool isBackground)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent);
            barObj.transform.localPosition = new Vector3(0f, yOffset, 0f);
            barObj.transform.localRotation = Quaternion.identity;

            SpriteRenderer sr = barObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = color;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = baseSortingOrder;

            Vector2 size = isBackground
                ? new Vector2(barSize.x + backgroundPadding * 2f, barSize.y + backgroundPadding * 2f)
                : barSize;

            barObj.transform.localScale = new Vector3(size.x, size.y, 1f);

            return sr;
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void SubscribeToEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged += HandleManaChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= HandleHealthChanged;
            }

            if (manaSystem != null)
            {
                manaSystem.OnManaChanged -= HandleManaChanged;
            }
        }

        private void InitializeValues()
        {
            if (healthSystem != null)
            {
                targetHealth = healthSystem.HealthPercent;
                displayedHealth = targetHealth;
            }

            if (manaSystem != null)
            {
                targetMana = manaSystem.ManaPercent;
                displayedMana = targetMana;
            }

            // Initialize visibility based on current resource levels
            healthCurrentAlpha = targetHealth < 1f ? 1f : 0f;
            manaCurrentAlpha = targetMana < 1f ? 1f : 0f;

            UpdateBarFills();
            ApplyHealthAlpha(healthCurrentAlpha);
            ApplyManaAlpha(manaCurrentAlpha);
        }

        private void HandleHealthChanged(float current, float max)
        {
            float prev = targetHealth;
            targetHealth = max > 0f ? current / max : 0f;
            healthRecentChangeTimer = recentChangeDisplayTime;

            if (!Mathf.Approximately(prev, targetHealth))
                FlashGlow(healthGlow, healthFillColor, ref healthGlowCoroutine);
        }

        private void HandleManaChanged(float current, float max)
        {
            float prev = targetMana;
            targetMana = max > 0f ? current / max : 0f;
            manaRecentChangeTimer = recentChangeDisplayTime;

            if (!Mathf.Approximately(prev, targetMana))
                FlashGlow(manaGlow, manaFillColor, ref manaGlowCoroutine);
        }

        private void UpdateBarPositions()
        {
            // Counter-flip bars so they remain stable when the player flips via localScale.x
            float parentScaleX = transform.localScale.x;
            float counterScaleX = parentScaleX != 0f ? Mathf.Sign(parentScaleX) : 1f;

            if (healthBarContainer != null)
            {
                healthBarContainer.transform.rotation = Quaternion.identity;
                healthBarContainer.transform.localScale = new Vector3(counterScaleX, 1f, 1f);
            }

            if (manaBarContainer != null)
            {
                manaBarContainer.transform.rotation = Quaternion.identity;
                manaBarContainer.transform.localScale = new Vector3(counterScaleX, 1f, 1f);
            }
        }

        private void UpdateBarFills()
        {
            // Use unscaledDeltaTime so bars animate even when time is paused (e.g. stat menu open)
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, smoothSpeed * Time.unscaledDeltaTime);
            displayedMana = Mathf.Lerp(displayedMana, targetMana, smoothSpeed * Time.unscaledDeltaTime);

            if (Mathf.Abs(displayedHealth - targetHealth) < 0.001f)
                displayedHealth = targetHealth;
            if (Mathf.Abs(displayedMana - targetMana) < 0.001f)
                displayedMana = targetMana;

            UpdateFillBar(healthFill, displayedHealth);
            UpdateFillBar(manaFill, displayedMana);
        }

        private void UpdateFillBar(SpriteRenderer fill, float percent)
        {
            if (fill == null)
                return;

            Vector3 scale = fill.transform.localScale;
            scale.x = barSize.x * Mathf.Clamp01(percent);
            fill.transform.localScale = scale;

            float xOffset = (barSize.x - scale.x) / -2f;
            Vector3 pos = fill.transform.localPosition;
            pos.x = xOffset;
            fill.transform.localPosition = pos;
        }

        private void UpdateRecentChangeTimers()
        {
            // Use unscaledDeltaTime so timers run even when game is paused (e.g. stat menu open)
            if (healthRecentChangeTimer > 0f)
            {
                healthRecentChangeTimer -= Time.unscaledDeltaTime;
            }

            if (manaRecentChangeTimer > 0f)
            {
                manaRecentChangeTimer -= Time.unscaledDeltaTime;
            }
        }

        private void UpdateHealthVisibility()
        {
            bool healthRecentlyChanged = healthRecentChangeTimer > 0f;
            bool shouldShowHealth = showHealthWhenFull || targetHealth < 1f || healthRecentlyChanged;
            healthTargetAlpha = shouldShowHealth ? 1f : 0f;

            float fadeSpeed = 1f / visibilityFadeDuration;
            healthCurrentAlpha = Mathf.MoveTowards(healthCurrentAlpha, healthTargetAlpha, fadeSpeed * Time.unscaledDeltaTime);

            ApplyHealthAlpha(healthCurrentAlpha);
        }

        private void UpdateManaVisibility()
        {
            bool manaRecentlyChanged = manaRecentChangeTimer > 0f;
            bool shouldShowMana = showManaWhenFull || targetMana < 1f || manaRecentlyChanged;
            manaTargetAlpha = shouldShowMana ? 1f : 0f;

            float fadeSpeed = 1f / visibilityFadeDuration;
            manaCurrentAlpha = Mathf.MoveTowards(manaCurrentAlpha, manaTargetAlpha, fadeSpeed * Time.unscaledDeltaTime);

            ApplyManaAlpha(manaCurrentAlpha);
        }

        private void ApplyHealthAlpha(float alpha)
        {
            SetSpriteAlpha(healthBackground, healthBackgroundColor.a * alpha);
            SetSpriteAlpha(healthFill, healthFillColor.a * alpha);
        }

        private void ApplyManaAlpha(float alpha)
        {
            SetSpriteAlpha(manaBackground, manaBackgroundColor.a * alpha);
            SetSpriteAlpha(manaFill, manaFillColor.a * alpha);
        }

        private void SetSpriteAlpha(SpriteRenderer sr, float alpha)
        {
            if (sr == null)
                return;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        private void UpdatePulse()
        {
            if (displayedHealth > lowHealthThreshold)
                return;

            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseIntensity;

            Color c = healthFillColor;
            c.r = Mathf.Clamp01(c.r + pulse);
            c.g = Mathf.Clamp01(c.g + pulse);
            c.b = Mathf.Clamp01(c.b + pulse);
            c.a = healthFillColor.a * healthCurrentAlpha;

            if (healthFill != null)
            {
                healthFill.color = c;
            }
        }

        /// <summary>
        /// Forces health bar to be visible.
        /// </summary>
        public void ForceShowHealth()
        {
            healthRecentChangeTimer = recentChangeDisplayTime;
        }

        /// <summary>
        /// Forces mana bar to be visible.
        /// </summary>
        public void ForceShowMana()
        {
            manaRecentChangeTimer = recentChangeDisplayTime;
        }

        /// <summary>
        /// Forces both bars to be visible.
        /// </summary>
        public void ForceShowBoth()
        {
            ForceShowHealth();
            ForceShowMana();
        }

        /// <summary>
        /// Immediately hides the health bar.
        /// </summary>
        public void ForceHideHealth()
        {
            healthRecentChangeTimer = 0f;
            healthCurrentAlpha = 0f;
            ApplyHealthAlpha(0f);
        }

        /// <summary>
        /// Immediately hides the mana bar.
        /// </summary>
        public void ForceHideMana()
        {
            manaRecentChangeTimer = 0f;
            manaCurrentAlpha = 0f;
            ApplyManaAlpha(0f);
        }

        /// <summary>
        /// Immediately hides both bars.
        /// </summary>
        public void ForceHideBoth()
        {
            ForceHideHealth();
            ForceHideMana();
        }

        /// <summary>
        /// Sets whether health bar shows when at full health.
        /// </summary>
        public void SetShowHealthWhenFull(bool show)
        {
            showHealthWhenFull = show;
        }

        /// <summary>
        /// Sets whether mana bar shows when at full mana.
        /// </summary>
        public void SetShowManaWhenFull(bool show)
        {
            showManaWhenFull = show;
        }

        /// <summary>
        /// Gets the current health bar alpha.
        /// </summary>
        public float GetHealthAlpha() => healthCurrentAlpha;

        /// <summary>
        /// Gets the current mana bar alpha.
        /// </summary>
        public float GetManaAlpha() => manaCurrentAlpha;

        private void FlashGlow(SpriteRenderer glow, Color baseColor, ref Coroutine coroutine)
        {
            if (glow == null) return;
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = StartCoroutine(GlowFlashCoroutine(glow, baseColor));
        }

        private IEnumerator GlowFlashCoroutine(SpriteRenderer glow, Color baseColor)
        {
            float duration = 0.2f;
            float startAlpha = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(startAlpha, 0f, t * t);
                glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }
    }
}
