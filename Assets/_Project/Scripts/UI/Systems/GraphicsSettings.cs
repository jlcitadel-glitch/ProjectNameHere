using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Simple brightness control via screen overlay, matching Dark Souls / Hollow Knight style.
    /// Darkening uses a black overlay, brightening uses a white overlay with tuned alpha curves.
    /// </summary>
    public class GraphicsSettings : MonoBehaviour
    {
        public static GraphicsSettings Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private const string PREF_BRIGHTNESS = "Graphics_Brightness";
        private const float DEFAULT_BRIGHTNESS = 0f;

        private Canvas overlayCanvas;
        private Image darkenImage;
        private Image brightenImage;

        private float brightness;
        public float Brightness => brightness;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateOverlay();
            LoadSettings();
            ApplyBrightness();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void CreateOverlay()
        {
            var overlayGO = new GameObject("BrightnessOverlay");
            overlayGO.transform.SetParent(transform, false);

            overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 30000;

            var scaler = overlayGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Darken layer (black overlay for negative brightness)
            darkenImage = CreateFullscreenImage(overlayGO.transform, "Darken");
            darkenImage.color = Color.clear;

            // Brighten layer (white overlay for positive brightness)
            brightenImage = CreateFullscreenImage(overlayGO.transform, "Brighten");
            brightenImage.color = Color.clear;
        }

        private Image CreateFullscreenImage(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return img;
        }

        /// <summary>
        /// Set brightness. Range [-1, 1]. 0 = default, -1 = very dark, +1 = very bright.
        /// </summary>
        public void SetBrightness(float value)
        {
            brightness = Mathf.Clamp(value, -1f, 1f);
            ApplyBrightness();
            PlayerPrefs.SetFloat(PREF_BRIGHTNESS, brightness);
        }

        public void ResetToDefaults()
        {
            SetBrightness(DEFAULT_BRIGHTNESS);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            brightness = PlayerPrefs.GetFloat(PREF_BRIGHTNESS, DEFAULT_BRIGHTNESS);
        }

        private void ApplyBrightness()
        {
            if (darkenImage == null || brightenImage == null) return;

            if (brightness < -0.01f)
            {
                // Darkening: black overlay. Quadratic curve for natural feel.
                // -1.0 maps to alpha ~0.7 (very dark but not black)
                float t = Mathf.Abs(brightness);
                float alpha = t * t * 0.7f;
                darkenImage.color = new Color(0f, 0f, 0f, alpha);
                brightenImage.color = Color.clear;
            }
            else if (brightness > 0.01f)
            {
                // Brightening: white overlay with low alpha.
                // +1.0 maps to alpha ~0.35 (brighter but not washed out)
                float t = brightness;
                float alpha = t * t * 0.35f;
                darkenImage.color = Color.clear;
                brightenImage.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                darkenImage.color = Color.clear;
                brightenImage.color = Color.clear;
            }
        }
    }
}
