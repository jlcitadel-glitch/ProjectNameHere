using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Spawns and manages floating damage numbers using a screen-space overlay canvas.
    /// Uses TextMeshProUGUI for reliable rendering in 2D URP.
    /// </summary>
    public class DamageNumberSpawner : MonoBehaviour
    {
        public static DamageNumberSpawner Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;

        [Header("Animation")]
        [SerializeField] private float floatSpeed = 120f;
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float fadeStartTime = 0.5f;
        [SerializeField] private float randomOffsetRange = 20f;

        [Header("Font Size")]
        [SerializeField] private float baseFontSize = 28f;
        [SerializeField] private float criticalFontSize = 38f;
        [SerializeField] private float healFontSize = 30f;

        [Header("Colors")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color manaColor = new Color(0f, 0.808f, 0.820f, 1f);
        [SerializeField] private Color physicalColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color magicColor = new Color(0.6f, 0.4f, 1f, 1f);
        [SerializeField] private Color fireColor = new Color(1f, 0.4f, 0.2f, 1f);
        [SerializeField] private Color iceColor = new Color(0.5f, 0.8f, 1f, 1f);
        [SerializeField] private Color lightningColor = new Color(1f, 1f, 0.4f, 1f);

        private Canvas canvas;
        private RectTransform canvasRect;
        private Queue<DamageNumber> pool = new Queue<DamageNumber>();
        private List<DamageNumber> activeNumbers = new List<DamageNumber>();
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateCanvas();
            InitializePool();
            Debug.Log($"[DamageNumberSpawner] Initialized with {pool.Count} pooled numbers. Canvas: {canvas != null}, Camera: {Camera.main != null}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            UpdateActiveNumbers();
        }

        private void CreateCanvas()
        {
            var canvasGo = new GameObject("DamageNumberCanvas");
            canvasGo.transform.SetParent(transform);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasGo.GetComponent<RectTransform>();
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledNumber();
            }
        }

        private DamageNumber CreatePooledNumber()
        {
            var go = new GameObject("DmgNum", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = baseFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            FontManager.EnsureFont(tmp);

            // Black outline for visibility against any background
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);

            // Size the rect to fit text without clipping
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 60);

            // Deactivate AFTER TMP is fully configured
            go.SetActive(false);

            var number = new DamageNumber
            {
                gameObject = go,
                rectTransform = rt,
                textMesh = tmp
            };

            pool.Enqueue(number);
            return number;
        }

        private DamageNumber GetFromPool()
        {
            if (pool.Count == 0)
            {
                if (activeNumbers.Count < maxPoolSize)
                {
                    CreatePooledNumber();
                }
                else
                {
                    var oldest = activeNumbers[0];
                    ReturnToPool(oldest);
                }
            }

            return pool.Dequeue();
        }

        private void ReturnToPool(DamageNumber number)
        {
            number.gameObject.SetActive(false);
            activeNumbers.Remove(number);
            pool.Enqueue(number);
        }

        private void UpdateActiveNumbers()
        {
            if (mainCamera == null) return;

            for (int i = activeNumbers.Count - 1; i >= 0; i--)
            {
                var number = activeNumbers[i];
                number.elapsedTime += Time.deltaTime;

                // Calculate world position with upward float
                float yOffset = Mathf.Min(number.elapsedTime * floatSpeed, floatDistance);
                Vector3 worldPos = number.worldStartPosition + Vector3.up * (yOffset / 100f);

                // Convert world position to screen position, then to canvas local position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // Don't render if behind camera
                if (screenPos.z < 0)
                {
                    number.gameObject.SetActive(false);
                    if (number.elapsedTime >= lifetime)
                        ReturnToPool(number);
                    continue;
                }

                // Convert screen position to canvas position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, null, out Vector2 canvasPos);
                number.rectTransform.anchoredPosition = canvasPos;

                if (!number.gameObject.activeSelf)
                    number.gameObject.SetActive(true);

                // Fade out
                if (number.elapsedTime > fadeStartTime && number.textMesh != null)
                {
                    float fadeProgress = (number.elapsedTime - fadeStartTime) / (lifetime - fadeStartTime);
                    Color c = number.textMesh.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    number.textMesh.color = c;
                }

                // Scale pulse for criticals
                if (number.isCritical && number.elapsedTime < 0.3f)
                {
                    float pulse = 1f + Mathf.Sin(number.elapsedTime * Mathf.PI * 8f) * 0.15f;
                    number.rectTransform.localScale = Vector3.one * pulse;
                }
                else if (number.isCritical && number.elapsedTime >= 0.3f)
                {
                    number.rectTransform.localScale = Vector3.one;
                }

                // Return to pool when lifetime expires
                if (number.elapsedTime >= lifetime)
                {
                    ReturnToPool(number);
                }
            }
        }

        /// <summary>
        /// Spawns a damage number at the specified world position.
        /// </summary>
        public void SpawnDamage(Vector3 position, float amount, DamageNumberType type = DamageNumberType.Normal, bool isCritical = false)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[DamageNumberSpawner] SpawnDamage called but Camera.main is null");
                return;
            }

            Debug.Log($"[DamageNumberSpawner] SpawnDamage: {amount} at {position} type={type} crit={isCritical}");
            var number = GetFromPool();

            // Random offset in world space
            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.3f),
                0f
            );

            number.worldStartPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = isCritical;

            if (number.textMesh != null)
            {
                string prefix = type == DamageNumberType.Heal ? "+" : "";
                string text = prefix + Mathf.RoundToInt(amount).ToString();
                if (isCritical) text += "!";

                number.textMesh.text = text;
                number.textMesh.color = GetColorForType(type, isCritical);
                number.textMesh.fontSize = type == DamageNumberType.Heal ? healFontSize :
                                           isCritical ? criticalFontSize : baseFontSize;
            }

            number.rectTransform.localScale = Vector3.one;

            // Position immediately
            Vector3 screenPos = mainCamera.WorldToScreenPoint(number.worldStartPosition);
            if (screenPos.z > 0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, null, out Vector2 canvasPos);
                number.rectTransform.anchoredPosition = canvasPos;
            }

            number.gameObject.SetActive(true);
            activeNumbers.Add(number);
        }

        /// <summary>
        /// Spawns a damage number with specific damage type coloring.
        /// </summary>
        public void SpawnDamageWithType(Vector3 position, float amount, DamageType damageType, bool isCritical = false)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[DamageNumberSpawner] SpawnDamageWithType called but Camera.main is null");
                return;
            }

            Debug.Log($"[DamageNumberSpawner] SpawnDamageWithType: {amount} {damageType} at {position} crit={isCritical}");
            var number = GetFromPool();

            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.3f),
                0f
            );

            number.worldStartPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = isCritical;

            if (number.textMesh != null)
            {
                string text = Mathf.RoundToInt(amount).ToString();
                if (isCritical) text += "!";

                number.textMesh.text = text;
                number.textMesh.color = GetColorForDamageType(damageType, isCritical);
                number.textMesh.fontSize = isCritical ? criticalFontSize : baseFontSize;
            }

            number.rectTransform.localScale = Vector3.one;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(number.worldStartPosition);
            if (screenPos.z > 0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, null, out Vector2 canvasPos);
                number.rectTransform.anchoredPosition = canvasPos;
            }

            number.gameObject.SetActive(true);
            activeNumbers.Add(number);
        }

        /// <summary>
        /// Spawns a text popup (for status effects, etc).
        /// </summary>
        public void SpawnText(Vector3 position, string text, Color color)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null) return;

            var number = GetFromPool();

            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.3f),
                0f
            );

            number.worldStartPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = false;

            if (number.textMesh != null)
            {
                number.textMesh.text = text;
                number.textMesh.color = color;
                number.textMesh.fontSize = baseFontSize;
            }

            number.rectTransform.localScale = Vector3.one;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(number.worldStartPosition);
            if (screenPos.z > 0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, null, out Vector2 canvasPos);
                number.rectTransform.anchoredPosition = canvasPos;
            }

            number.gameObject.SetActive(true);
            activeNumbers.Add(number);
        }

        private Color GetColorForType(DamageNumberType type, bool isCritical)
        {
            if (isCritical && type != DamageNumberType.Heal)
            {
                return criticalDamageColor;
            }

            return type switch
            {
                DamageNumberType.Heal => healColor,
                DamageNumberType.Mana => manaColor,
                _ => normalDamageColor
            };
        }

        private Color GetColorForDamageType(DamageType type, bool isCritical)
        {
            if (isCritical)
            {
                return criticalDamageColor;
            }

            return type switch
            {
                DamageType.Physical => physicalColor,
                DamageType.Magic => magicColor,
                DamageType.Fire => fireColor,
                DamageType.Ice => iceColor,
                DamageType.Lightning => lightningColor,
                _ => normalDamageColor
            };
        }

        private class DamageNumber
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public TextMeshProUGUI textMesh;
            public Vector3 worldStartPosition;
            public float elapsedTime;
            public bool isCritical;
        }
    }

    public enum DamageNumberType
    {
        Normal,
        Heal,
        Mana
    }
}
