using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI panel for display settings (resolution, window mode, aspect ratio).
    /// Connect to DisplaySettings system.
    /// </summary>
    public class DisplaySettingsPanel : MonoBehaviour
    {
        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Text resolutionLabel;

        [Header("Window Mode")]
        [SerializeField] private TMP_Dropdown windowModeDropdown;
        [SerializeField] private TMP_Text windowModeLabel;

        [Header("Aspect Ratio")]
        [SerializeField] private TMP_Dropdown aspectRatioDropdown;
        [SerializeField] private TMP_Text aspectRatioLabel;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button revertButton;
        [SerializeField] private Button defaultsButton;

        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private float confirmationTimeout = 15f;

        // Cached previous settings for revert
        private int previousResolutionIndex;
        private int previousWindowModeIndex;
        private int previousAspectRatioIndex;

        private float confirmationTimer;
        private bool waitingForConfirmation;

        private void Start()
        {
            SetupDropdowns();
            SetupButtons();
            LoadCurrentSettings();

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (waitingForConfirmation)
            {
                confirmationTimer -= Time.unscaledDeltaTime;

                if (confirmationText != null)
                {
                    confirmationText.text = $"Keep these settings?\n\nReverting in {Mathf.CeilToInt(confirmationTimer)} seconds...";
                }

                if (confirmationTimer <= 0)
                {
                    RevertSettings();
                }
            }
        }

        #region Setup

        private void SetupDropdowns()
        {
            if (DisplaySettings.Instance == null)
            {
                Debug.LogWarning("[DisplaySettingsPanel] DisplaySettings.Instance not found!");
                return;
            }

            // Resolution dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    DisplaySettings.Instance.GetResolutionStrings()));
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // Window mode dropdown
            if (windowModeDropdown != null)
            {
                windowModeDropdown.ClearOptions();
                windowModeDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    DisplaySettings.Instance.GetWindowModeStrings()));
                windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
            }

            // Aspect ratio dropdown
            if (aspectRatioDropdown != null)
            {
                aspectRatioDropdown.ClearOptions();
                aspectRatioDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    DisplaySettings.Instance.GetAspectRatioStrings()));
                aspectRatioDropdown.onValueChanged.AddListener(OnAspectRatioChanged);
            }
        }

        private void SetupButtons()
        {
            if (applyButton != null)
            {
                applyButton.onClick.AddListener(OnApplyClicked);
            }

            if (revertButton != null)
            {
                revertButton.onClick.AddListener(RevertSettings);
            }

            if (defaultsButton != null)
            {
                defaultsButton.onClick.AddListener(OnDefaultsClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.AddListener(ConfirmSettings);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.AddListener(RevertSettings);
            }
        }

        private void LoadCurrentSettings()
        {
            if (DisplaySettings.Instance == null) return;

            // Set dropdowns to current values
            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(DisplaySettings.Instance.CurrentResolutionIndex);
            }

            if (windowModeDropdown != null)
            {
                windowModeDropdown.SetValueWithoutNotify((int)DisplaySettings.Instance.CurrentWindowMode);
            }

            if (aspectRatioDropdown != null)
            {
                aspectRatioDropdown.SetValueWithoutNotify((int)DisplaySettings.Instance.CurrentAspectRatio);
            }

            // Cache for revert
            CachePreviousSettings();
        }

        private void CachePreviousSettings()
        {
            if (DisplaySettings.Instance == null) return;

            previousResolutionIndex = DisplaySettings.Instance.CurrentResolutionIndex;
            previousWindowModeIndex = (int)DisplaySettings.Instance.CurrentWindowMode;
            previousAspectRatioIndex = (int)DisplaySettings.Instance.CurrentAspectRatio;
        }

        #endregion

        #region Dropdown Callbacks

        private void OnResolutionChanged(int index)
        {
            DisplaySettings.Instance?.SetResolution(index);
            UpdateLabels();
        }

        private void OnWindowModeChanged(int index)
        {
            DisplaySettings.Instance?.SetWindowMode(index);
            UpdateLabels();
        }

        private void OnAspectRatioChanged(int index)
        {
            DisplaySettings.Instance?.SetAspectRatio(index);

            // Refresh resolution dropdown (filtered by aspect ratio)
            if (resolutionDropdown != null && DisplaySettings.Instance != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(
                    DisplaySettings.Instance.GetResolutionStrings()));
                resolutionDropdown.SetValueWithoutNotify(DisplaySettings.Instance.CurrentResolutionIndex);
            }

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (DisplaySettings.Instance == null) return;

            if (resolutionLabel != null)
            {
                var res = DisplaySettings.Instance.CurrentResolution;
                resolutionLabel.text = $"{res.width} x {res.height}";
            }

            if (windowModeLabel != null)
            {
                windowModeLabel.text = DisplaySettings.Instance.GetWindowModeStrings()[(int)DisplaySettings.Instance.CurrentWindowMode];
            }

            if (aspectRatioLabel != null)
            {
                aspectRatioLabel.text = DisplaySettings.Instance.GetAspectRatioStrings()[(int)DisplaySettings.Instance.CurrentAspectRatio];
            }
        }

        #endregion

        #region Button Callbacks

        private void OnApplyClicked()
        {
            CachePreviousSettings();
            DisplaySettings.Instance?.ApplySettings();
            ShowConfirmation();
        }

        private void OnDefaultsClicked()
        {
            DisplaySettings.Instance?.ResetToDefaults();
            LoadCurrentSettings();
        }

        private void ShowConfirmation()
        {
            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(true);
                waitingForConfirmation = true;
                confirmationTimer = confirmationTimeout;
            }
            else
            {
                // No confirmation panel, just save
                ConfirmSettings();
            }
        }

        private void ConfirmSettings()
        {
            waitingForConfirmation = false;

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            DisplaySettings.Instance?.SaveSettings();
            CachePreviousSettings();

            Debug.Log("[DisplaySettingsPanel] Settings confirmed and saved.");
        }

        private void RevertSettings()
        {
            waitingForConfirmation = false;

            if (confirmationPanel != null)
            {
                confirmationPanel.SetActive(false);
            }

            if (DisplaySettings.Instance != null)
            {
                DisplaySettings.Instance.SetAspectRatio(previousAspectRatioIndex);
                DisplaySettings.Instance.SetWindowMode(previousWindowModeIndex);
                DisplaySettings.Instance.SetResolution(previousResolutionIndex);
                DisplaySettings.Instance.ApplySettings();
            }

            LoadCurrentSettings();

            Debug.Log("[DisplaySettingsPanel] Settings reverted.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the panel to reflect current DisplaySettings state.
        /// </summary>
        public void Refresh()
        {
            SetupDropdowns();
            LoadCurrentSettings();
        }

        #endregion
    }
}
