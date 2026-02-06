using UnityEngine;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Manages font assets for the project. Provides access to the project's
    /// default TMP font for runtime UI creation.
    /// </summary>
    public class FontManager : MonoBehaviour
    {
        public static FontManager Instance { get; private set; }

        [Header("Font Assets")]
        [Tooltip("The primary font for UI text")]
        [SerializeField] private TMP_FontAsset primaryFont;

        [Tooltip("Bold variant of the primary font")]
        [SerializeField] private TMP_FontAsset primaryFontBold;

        [Tooltip("Fallback font if primary is not available")]
        [SerializeField] private TMP_FontAsset fallbackFont;

        private static TMP_FontAsset cachedDefaultFont;

        public TMP_FontAsset PrimaryFont => primaryFont;
        public TMP_FontAsset PrimaryFontBold => primaryFontBold;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache the font for static access
            if (primaryFont != null)
            {
                cachedDefaultFont = primaryFont;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Gets the default font for the project.
        /// Works both with and without FontManager instance.
        /// </summary>
        public static TMP_FontAsset GetDefaultFont()
        {
            // Try instance first
            if (Instance != null && Instance.primaryFont != null)
            {
                return Instance.primaryFont;
            }

            // Try cached font
            if (cachedDefaultFont != null)
            {
                return cachedDefaultFont;
            }

            // Try to load from Resources
            var font = Resources.Load<TMP_FontAsset>("Fonts/Cinzel-Regular SDF");
            if (font != null)
            {
                cachedDefaultFont = font;
                return font;
            }

            // Try to find any Cinzel font in the project
            font = FindFontAsset("Cinzel");
            if (font != null)
            {
                cachedDefaultFont = font;
                return font;
            }

            // Last resort: find any TMP font
            font = FindAnyFontAsset();
            if (font != null)
            {
                cachedDefaultFont = font;
                return font;
            }

            Debug.LogWarning("[FontManager] No TMP font asset found!");
            return null;
        }

        /// <summary>
        /// Gets the bold font variant.
        /// </summary>
        public static TMP_FontAsset GetBoldFont()
        {
            if (Instance != null && Instance.primaryFontBold != null)
            {
                return Instance.primaryFontBold;
            }

            // Try to find bold variant
            var font = FindFontAsset("Cinzel-Bold");
            if (font != null) return font;

            font = FindFontAsset("Cinzel-SemiBold");
            if (font != null) return font;

            // Fall back to regular
            return GetDefaultFont();
        }

        /// <summary>
        /// Finds a TMP font asset by partial name match.
        /// </summary>
        private static TMP_FontAsset FindFontAsset(string partialName)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{partialName} t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
#endif
            // Runtime: try Resources folder
            var fonts = Resources.LoadAll<TMP_FontAsset>("");
            foreach (var font in fonts)
            {
                if (font.name.Contains(partialName))
                {
                    return font;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds any available TMP font asset.
        /// </summary>
        private static TMP_FontAsset FindAnyFontAsset()
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
#endif
            // Runtime: try Resources folder
            var fonts = Resources.LoadAll<TMP_FontAsset>("");
            if (fonts.Length > 0)
            {
                return fonts[0];
            }
            return null;
        }

        /// <summary>
        /// Applies the default font to a TMP_Text component if it has no font assigned.
        /// </summary>
        public static void EnsureFont(TMP_Text text)
        {
            if (text != null && text.font == null)
            {
                text.font = GetDefaultFont();
            }
        }

        /// <summary>
        /// Creates a TextMeshProUGUI with the default font already assigned.
        /// Use this instead of AddComponent<TextMeshProUGUI>() for runtime UI creation.
        /// </summary>
        public static TextMeshProUGUI CreateText(GameObject go)
        {
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = GetDefaultFont();
            return text;
        }
    }
}
