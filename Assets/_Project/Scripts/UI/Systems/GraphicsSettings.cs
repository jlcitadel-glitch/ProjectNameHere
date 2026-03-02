using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectName.UI
{
    public class GraphicsSettings : MonoBehaviour
    {
        public static GraphicsSettings Instance { get; private set; }

        private const string PREF_BRIGHTNESS = "Graphics_Brightness";
        private const string PREF_CONTRAST = "Graphics_Contrast";
        private const string PREF_SATURATION = "Graphics_Saturation";

        private const float DEFAULT_BRIGHTNESS = 0f;
        private const float DEFAULT_CONTRAST = 0f;
        private const float DEFAULT_SATURATION = 0f;

        private Volume volume;
        private ColorAdjustments colorAdjustments;

        private float brightness;
        private float contrast;
        private float saturation;

        public float Brightness => brightness;
        public float Contrast => contrast;
        public float Saturation => saturation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateRuntimeVolume();
            LoadSettings();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void CreateRuntimeVolume()
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            colorAdjustments = profile.Add<ColorAdjustments>(overrides: true);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.saturation.overrideState = true;

            volume.profile = profile;
        }

        public void SetBrightness(float value)
        {
            brightness = Mathf.Clamp(value, -2f, 2f);
            if (colorAdjustments != null)
                colorAdjustments.postExposure.value = brightness;
            PlayerPrefs.SetFloat(PREF_BRIGHTNESS, brightness);
        }

        public void SetContrast(float value)
        {
            contrast = Mathf.Clamp(value, -100f, 100f);
            if (colorAdjustments != null)
                colorAdjustments.contrast.value = contrast;
            PlayerPrefs.SetFloat(PREF_CONTRAST, contrast);
        }

        public void SetSaturation(float value)
        {
            saturation = Mathf.Clamp(value, -100f, 100f);
            if (colorAdjustments != null)
                colorAdjustments.saturation.value = saturation;
            PlayerPrefs.SetFloat(PREF_SATURATION, saturation);
        }

        public void ResetToDefaults()
        {
            SetBrightness(DEFAULT_BRIGHTNESS);
            SetContrast(DEFAULT_CONTRAST);
            SetSaturation(DEFAULT_SATURATION);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            brightness = PlayerPrefs.GetFloat(PREF_BRIGHTNESS, DEFAULT_BRIGHTNESS);
            contrast = PlayerPrefs.GetFloat(PREF_CONTRAST, DEFAULT_CONTRAST);
            saturation = PlayerPrefs.GetFloat(PREF_SATURATION, DEFAULT_SATURATION);
        }

        private void ApplySettings()
        {
            if (colorAdjustments == null) return;
            colorAdjustments.postExposure.value = brightness;
            colorAdjustments.contrast.value = contrast;
            colorAdjustments.saturation.value = saturation;
        }
    }
}
