using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Spawns and manages floating damage numbers in world space.
    /// Attach to a persistent GameObject or use as singleton.
    /// </summary>
    public class DamageNumberSpawner : MonoBehaviour
    {
        public static DamageNumberSpawner Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private GameObject damageNumberPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;

        [Header("Animation")]
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float floatDistance = 1.5f;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float fadeStartTime = 0.5f;
        [SerializeField] private float randomOffsetRange = 0.3f;

        [Header("Scaling")]
        [SerializeField] private float baseScale = 0.02f;
        [SerializeField] private float criticalScale = 0.03f;
        [SerializeField] private float healScale = 0.025f;

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

        private Queue<DamageNumber> pool = new Queue<DamageNumber>();
        private List<DamageNumber> activeNumbers = new List<DamageNumber>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
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
            UpdateActiveNumbers();
        }

        private void InitializePool()
        {
            if (damageNumberPrefab == null)
            {
                CreateDefaultPrefab();
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledNumber();
            }
        }

        private void CreateDefaultPrefab()
        {
            damageNumberPrefab = new GameObject("DamageNumberTemplate");
            damageNumberPrefab.SetActive(false);

            var tmp = damageNumberPrefab.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 6;
            tmp.fontStyle = FontStyles.Bold;
            tmp.sortingOrder = 1000;

            damageNumberPrefab.transform.SetParent(transform);
        }

        private DamageNumber CreatePooledNumber()
        {
            GameObject obj = Instantiate(damageNumberPrefab, transform);
            obj.SetActive(false);

            var number = new DamageNumber
            {
                gameObject = obj,
                textMesh = obj.GetComponent<TextMeshPro>()
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
                    return CreatePooledNumber();
                }
                else
                {
                    // Recycle oldest active number
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
            for (int i = activeNumbers.Count - 1; i >= 0; i--)
            {
                var number = activeNumbers[i];
                number.elapsedTime += Time.deltaTime;

                // Move upward
                float yOffset = number.elapsedTime * floatSpeed;
                yOffset = Mathf.Min(yOffset, floatDistance);
                number.gameObject.transform.position = number.startPosition + Vector3.up * yOffset;

                // Fade out
                if (number.elapsedTime > fadeStartTime && number.textMesh != null)
                {
                    float fadeProgress = (number.elapsedTime - fadeStartTime) / (lifetime - fadeStartTime);
                    Color c = number.textMesh.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    number.textMesh.color = c;
                }

                // Scale pulse for criticals
                if (number.isCritical && number.elapsedTime < 0.2f)
                {
                    float pulse = 1f + Mathf.Sin(number.elapsedTime * Mathf.PI * 10f) * 0.2f;
                    number.gameObject.transform.localScale = Vector3.one * number.scale * pulse;
                }

                // Return to pool when lifetime expires
                if (number.elapsedTime >= lifetime)
                {
                    ReturnToPool(number);
                }
            }
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// Spawns a damage number at the specified position.
        /// </summary>
        public void SpawnDamage(Vector3 position, float amount, DamageNumberType type = DamageNumberType.Normal, bool isCritical = false)
        {
            var number = GetFromPool();

            // Random offset
            Vector3 offset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(0f, randomOffsetRange),
                0f
            );

            number.startPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = isCritical;

            // Set scale
            number.scale = type == DamageNumberType.Heal ? healScale :
                           isCritical ? criticalScale : baseScale;
            number.gameObject.transform.localScale = Vector3.one * number.scale;
            number.gameObject.transform.position = number.startPosition;

            // Set text
            if (number.textMesh != null)
            {
                string prefix = type == DamageNumberType.Heal ? "+" : "";
                string text = prefix + Mathf.RoundToInt(amount).ToString();

                if (isCritical)
                {
                    text += "!";
                }

                number.textMesh.text = text;
                number.textMesh.color = GetColorForType(type, isCritical);
            }

            number.gameObject.SetActive(true);
            activeNumbers.Add(number);
        }

        /// <summary>
        /// Spawns a damage number with specific damage type coloring.
        /// </summary>
        public void SpawnDamageWithType(Vector3 position, float amount, DamageType damageType, bool isCritical = false)
        {
            var number = GetFromPool();

            Vector3 offset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(0f, randomOffsetRange),
                0f
            );

            number.startPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = isCritical;
            number.scale = isCritical ? criticalScale : baseScale;
            number.gameObject.transform.localScale = Vector3.one * number.scale;
            number.gameObject.transform.position = number.startPosition;

            if (number.textMesh != null)
            {
                string text = Mathf.RoundToInt(amount).ToString();
                if (isCritical) text += "!";

                number.textMesh.text = text;
                number.textMesh.color = GetColorForDamageType(damageType, isCritical);
            }

            number.gameObject.SetActive(true);
            activeNumbers.Add(number);
        }

        /// <summary>
        /// Spawns a text popup (for status effects, etc).
        /// </summary>
        public void SpawnText(Vector3 position, string text, Color color)
        {
            var number = GetFromPool();

            Vector3 offset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(0f, randomOffsetRange),
                0f
            );

            number.startPosition = position + offset;
            number.elapsedTime = 0f;
            number.isCritical = false;
            number.scale = baseScale;
            number.gameObject.transform.localScale = Vector3.one * number.scale;
            number.gameObject.transform.position = number.startPosition;

            if (number.textMesh != null)
            {
                number.textMesh.text = text;
                number.textMesh.color = color;
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
            public TextMeshPro textMesh;
            public Vector3 startPosition;
            public float elapsedTime;
            public bool isCritical;
            public float scale;
        }
    }

    public enum DamageNumberType
    {
        Normal,
        Heal,
        Mana
    }
}
