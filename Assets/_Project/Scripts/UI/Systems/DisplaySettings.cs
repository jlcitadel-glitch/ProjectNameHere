using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectName.UI
{
    /// <summary>
    /// Manages display settings: resolution, window mode, aspect ratio.
    /// Persists settings via PlayerPrefs.
    /// </summary>
    public class DisplaySettings : MonoBehaviour
    {
        public static DisplaySettings Instance { get; private set; }

        [Header("Default Settings")]
        [SerializeField] private WindowMode defaultWindowMode = WindowMode.FullscreenWindowed;
        [SerializeField] private AspectRatioPreset defaultAspectRatio = AspectRatioPreset.Auto;
        [SerializeField] private int defaultResolutionIndex = -1; // -1 = native

        [Header("Settings")]
        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private bool saveSettings = true;

        // Current state
        private Resolution[] availableResolutions;
        private List<Resolution> filteredResolutions;
        private int currentResolutionIndex;
        private WindowMode currentWindowMode;
        private AspectRatioPreset currentAspectRatio;

        // Events
        public event Action<Resolution> OnResolutionChanged;
        public event Action<WindowMode> OnWindowModeChanged;
        public event Action<AspectRatioPreset> OnAspectRatioChanged;
        public event Action OnSettingsApplied;

        // PlayerPrefs keys
        private const string PREF_RESOLUTION_WIDTH = "Display_ResolutionWidth";
        private const string PREF_RESOLUTION_HEIGHT = "Display_ResolutionHeight";
        private const string PREF_REFRESH_RATE = "Display_RefreshRate";
        private const string PREF_WINDOW_MODE = "Display_WindowMode";
        private const string PREF_ASPECT_RATIO = "Display_AspectRatio";

        #region Enums

        public enum WindowMode
        {
            Fullscreen,         // Exclusive fullscreen
            FullscreenWindowed, // Borderless windowed
            Windowed            // Standard window
        }

        public enum AspectRatioPreset
        {
            Auto,           // Use native aspect ratio
            Standard_4_3,   // 4:3 (1.33)
            Standard_16_9,  // 16:9 (1.78) - Most common
            Standard_16_10, // 16:10 (1.6)
            Widescreen_21_9,// 21:9 (2.33) - Ultrawide
            Widescreen_32_9 // 32:9 (3.56) - Super ultrawide
        }

        #endregion

        #region Properties

        public Resolution[] AvailableResolutions => filteredResolutions?.ToArray() ?? availableResolutions;
        public Resolution CurrentResolution => filteredResolutions != null && currentResolutionIndex >= 0 && currentResolutionIndex < filteredResolutions.Count
            ? filteredResolutions[currentResolutionIndex]
            : Screen.currentResolution;
        public WindowMode CurrentWindowMode => currentWindowMode;
        public AspectRatioPreset CurrentAspectRatio => currentAspectRatio;
        public int CurrentResolutionIndex => currentResolutionIndex;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CacheAvailableResolutions();
            LoadSettings();
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplySettings();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Resolution Management

        private void CacheAvailableResolutions()
        {
            // Get all available resolutions and remove duplicates
            availableResolutions = Screen.resolutions
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
                .OrderBy(r => r.width)
                .ThenBy(r => r.height)
                .ToArray();

            FilterResolutionsByAspectRatio();
        }

        private void FilterResolutionsByAspectRatio()
        {
            if (currentAspectRatio == AspectRatioPreset.Auto)
            {
                filteredResolutions = availableResolutions.ToList();
            }
            else
            {
                float targetRatio = GetAspectRatioValue(currentAspectRatio);
                float tolerance = 0.1f;

                filteredResolutions = availableResolutions
                    .Where(r => Mathf.Abs((float)r.width / r.height - targetRatio) < tolerance)
                    .ToList();

                // If no resolutions match, fall back to all
                if (filteredResolutions.Count == 0)
                {
                    filteredResolutions = availableResolutions.ToList();
                    Debug.LogWarning($"[DisplaySettings] No resolutions found for aspect ratio {currentAspectRatio}. Showing all.");
                }
            }

            // Clamp current index
            if (currentResolutionIndex >= filteredResolutions.Count)
            {
                currentResolutionIndex = filteredResolutions.Count - 1;
            }
        }

        public void SetResolution(int index)
        {
            if (index < 0 || index >= filteredResolutions.Count)
            {
                Debug.LogWarning($"[DisplaySettings] Invalid resolution index: {index}");
                return;
            }

            currentResolutionIndex = index;
            OnResolutionChanged?.Invoke(filteredResolutions[currentResolutionIndex]);
        }

        public void SetResolution(int width, int height)
        {
            int index = filteredResolutions.FindIndex(r => r.width == width && r.height == height);
            if (index >= 0)
            {
                SetResolution(index);
            }
            else
            {
                Debug.LogWarning($"[DisplaySettings] Resolution {width}x{height} not found.");
            }
        }

        public string[] GetResolutionStrings()
        {
            return filteredResolutions
                .Select(r => $"{r.width} x {r.height}")
                .ToArray();
        }

        #endregion

        #region Window Mode

        public void SetWindowMode(WindowMode mode)
        {
            currentWindowMode = mode;
            OnWindowModeChanged?.Invoke(mode);
        }

        public void SetWindowMode(int index)
        {
            if (Enum.IsDefined(typeof(WindowMode), index))
            {
                SetWindowMode((WindowMode)index);
            }
        }

        public string[] GetWindowModeStrings()
        {
            return new string[]
            {
                "Fullscreen",
                "Borderless Windowed",
                "Windowed"
            };
        }

        #endregion

        #region Aspect Ratio

        public void SetAspectRatio(AspectRatioPreset preset)
        {
            currentAspectRatio = preset;
            FilterResolutionsByAspectRatio();
            OnAspectRatioChanged?.Invoke(preset);
        }

        public void SetAspectRatio(int index)
        {
            if (Enum.IsDefined(typeof(AspectRatioPreset), index))
            {
                SetAspectRatio((AspectRatioPreset)index);
            }
        }

        public string[] GetAspectRatioStrings()
        {
            return new string[]
            {
                "Auto",
                "4:3 (Standard)",
                "16:9 (Widescreen)",
                "16:10 (Widescreen)",
                "21:9 (Ultrawide)",
                "32:9 (Super Ultrawide)"
            };
        }

        public static float GetAspectRatioValue(AspectRatioPreset preset)
        {
            return preset switch
            {
                AspectRatioPreset.Standard_4_3 => 4f / 3f,
                AspectRatioPreset.Standard_16_9 => 16f / 9f,
                AspectRatioPreset.Standard_16_10 => 16f / 10f,
                AspectRatioPreset.Widescreen_21_9 => 21f / 9f,
                AspectRatioPreset.Widescreen_32_9 => 32f / 9f,
                _ => (float)Screen.width / Screen.height
            };
        }

        #endregion

        #region Apply Settings

        public void ApplySettings()
        {
            Resolution res = CurrentResolution;
            FullScreenMode fullScreenMode = currentWindowMode switch
            {
                WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                WindowMode.FullscreenWindowed => FullScreenMode.FullScreenWindow,
                WindowMode.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };

            Screen.SetResolution(res.width, res.height, fullScreenMode, res.refreshRateRatio);

            Debug.Log($"[DisplaySettings] Applied: {res.width}x{res.height} @ {res.refreshRateRatio}Hz, Mode: {currentWindowMode}");

            if (saveSettings)
            {
                SaveSettings();
            }

            OnSettingsApplied?.Invoke();
        }

        public void ApplyAndSave()
        {
            ApplySettings();
            SaveSettings();
        }

        #endregion

        #region Save/Load

        public void SaveSettings()
        {
            Resolution res = CurrentResolution;
            PlayerPrefs.SetInt(PREF_RESOLUTION_WIDTH, res.width);
            PlayerPrefs.SetInt(PREF_RESOLUTION_HEIGHT, res.height);
            PlayerPrefs.SetFloat(PREF_REFRESH_RATE, (float)res.refreshRateRatio.value);
            PlayerPrefs.SetInt(PREF_WINDOW_MODE, (int)currentWindowMode);
            PlayerPrefs.SetInt(PREF_ASPECT_RATIO, (int)currentAspectRatio);
            PlayerPrefs.Save();

            Debug.Log("[DisplaySettings] Settings saved.");
        }

        public void LoadSettings()
        {
            // Load aspect ratio first (affects resolution filtering)
            currentAspectRatio = (AspectRatioPreset)PlayerPrefs.GetInt(PREF_ASPECT_RATIO, (int)defaultAspectRatio);
            FilterResolutionsByAspectRatio();

            // Load window mode
            currentWindowMode = (WindowMode)PlayerPrefs.GetInt(PREF_WINDOW_MODE, (int)defaultWindowMode);

            // Load resolution
            if (PlayerPrefs.HasKey(PREF_RESOLUTION_WIDTH))
            {
                int width = PlayerPrefs.GetInt(PREF_RESOLUTION_WIDTH);
                int height = PlayerPrefs.GetInt(PREF_RESOLUTION_HEIGHT);

                int index = filteredResolutions.FindIndex(r => r.width == width && r.height == height);
                currentResolutionIndex = index >= 0 ? index : filteredResolutions.Count - 1;
            }
            else
            {
                // Default to highest resolution or specified default
                currentResolutionIndex = defaultResolutionIndex >= 0
                    ? Mathf.Min(defaultResolutionIndex, filteredResolutions.Count - 1)
                    : filteredResolutions.Count - 1;
            }

            Debug.Log($"[DisplaySettings] Loaded: {CurrentResolution.width}x{CurrentResolution.height}, Mode: {currentWindowMode}, Aspect: {currentAspectRatio}");
        }

        public void ResetToDefaults()
        {
            currentWindowMode = defaultWindowMode;
            currentAspectRatio = defaultAspectRatio;
            FilterResolutionsByAspectRatio();
            currentResolutionIndex = defaultResolutionIndex >= 0
                ? Mathf.Min(defaultResolutionIndex, filteredResolutions.Count - 1)
                : filteredResolutions.Count - 1;

            ApplySettings();
        }

        #endregion
    }
}
