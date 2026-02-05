using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Popup for selecting a job advancement.
    /// Displays available advancement options with requirements.
    /// </summary>
    public class JobAdvancementPopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text currentJobText;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject jobOptionPrefab;

        [Header("Selection")]
        [SerializeField] private GameObject selectedJobPanel;
        [SerializeField] private Image selectedJobIcon;
        [SerializeField] private TMP_Text selectedJobName;
        [SerializeField] private TMP_Text selectedJobDescription;
        [SerializeField] private TMP_Text selectedJobStats;
        [SerializeField] private TMP_Text selectedJobRequirements;
        [SerializeField] private Button advanceButton;
        [SerializeField] private TMP_Text advanceButtonText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        // Runtime
        private List<JobOptionButton> optionButtons = new List<JobOptionButton>();
        private JobClassData selectedJob;
        private bool isVisible;

        public event System.Action<JobClassData> OnJobAdvanced;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (advanceButton != null)
            {
                advanceButton.onClick.AddListener(OnAdvanceClicked);
            }

            Hide();
        }

        private void Update()
        {
            // Handle escape key
            if (isVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        /// <summary>
        /// Shows the advancement popup with available options.
        /// </summary>
        public void Show()
        {
            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            var currentJob = skillManager.CurrentJob;
            if (currentJob == null || currentJob.childJobs == null || currentJob.childJobs.Length == 0)
            {
                Debug.Log("[JobAdvancementPopup] No advancement options available");
                return;
            }

            // Update title
            if (titleText != null)
            {
                titleText.text = "Job Advancement";
            }

            if (currentJobText != null)
            {
                currentJobText.text = $"Current: {currentJob.jobName}";
            }

            // Clear existing options
            ClearOptions();

            // Create option buttons
            foreach (var childJob in currentJob.childJobs)
            {
                if (childJob == null) continue;
                CreateJobOption(childJob);
            }

            // Select first available option
            if (optionButtons.Count > 0)
            {
                SelectJob(optionButtons[0].JobData);
            }

            // Show panel
            gameObject.SetActive(true);
            isVisible = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Hides the advancement popup.
        /// </summary>
        public void Hide()
        {
            isVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void ClearOptions()
        {
            foreach (var button in optionButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            optionButtons.Clear();
        }

        private void CreateJobOption(JobClassData job)
        {
            if (jobOptionPrefab == null || optionsContainer == null) return;

            var optionGO = Instantiate(jobOptionPrefab, optionsContainer);
            var optionButton = optionGO.GetComponent<JobOptionButton>();

            if (optionButton == null)
            {
                // Create a simple button if prefab doesn't have the component
                var button = optionGO.GetComponent<Button>();
                if (button == null)
                    button = optionGO.AddComponent<Button>();

                optionButton = optionGO.AddComponent<JobOptionButton>();
            }

            optionButton.Initialize(job);
            optionButton.OnSelected += SelectJob;

            optionButtons.Add(optionButton);
        }

        private void SelectJob(JobClassData job)
        {
            selectedJob = job;
            UpdateSelectedJobPanel();
        }

        private void UpdateSelectedJobPanel()
        {
            if (selectedJobPanel == null || selectedJob == null) return;

            selectedJobPanel.SetActive(true);

            if (selectedJobIcon != null && selectedJob.jobIcon != null)
            {
                selectedJobIcon.sprite = selectedJob.jobIcon;
                selectedJobIcon.color = selectedJob.jobColor;
            }

            if (selectedJobName != null)
            {
                selectedJobName.text = selectedJob.jobName;
            }

            if (selectedJobDescription != null)
            {
                selectedJobDescription.text = selectedJob.description;
            }

            if (selectedJobStats != null)
            {
                var stats = new System.Text.StringBuilder();
                stats.AppendLine($"Tier: {selectedJob.tier}");
                stats.AppendLine($"SP per Level: {selectedJob.spPerLevel}");
                stats.AppendLine($"Advancement Bonus: +{selectedJob.bonusSPOnAdvancement} SP");

                if (selectedJob.baseHPBonus != 0)
                    stats.AppendLine($"HP Bonus: +{selectedJob.baseHPBonus}");
                if (selectedJob.baseMPBonus != 0)
                    stats.AppendLine($"MP Bonus: +{selectedJob.baseMPBonus}");

                selectedJobStats.text = stats.ToString();
            }

            if (selectedJobRequirements != null)
            {
                selectedJobRequirements.text = selectedJob.GetRequirementsSummary();
            }

            UpdateAdvanceButton();
        }

        private void UpdateAdvanceButton()
        {
            if (advanceButton == null) return;

            var skillManager = SkillManager.Instance;
            bool canAdvance = skillManager?.CanAdvanceJob(selectedJob) ?? false;

            advanceButton.interactable = canAdvance;

            if (advanceButtonText != null)
            {
                advanceButtonText.text = canAdvance ? "Advance" : "Requirements Not Met";
            }
        }

        private void OnAdvanceClicked()
        {
            if (selectedJob == null) return;

            var skillManager = SkillManager.Instance;
            if (skillManager == null) return;

            if (skillManager.AdvanceJob(selectedJob))
            {
                OnJobAdvanced?.Invoke(selectedJob);

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PlayConfirmSound();
                }

                Hide();
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PlayErrorSound();
                }
            }
        }
    }

    /// <summary>
    /// Button component for a job advancement option.
    /// </summary>
    public class JobOptionButton : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        private JobClassData jobData;

        public JobClassData JobData => jobData;
        public event System.Action<JobClassData> OnSelected;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        public void Initialize(JobClassData job)
        {
            jobData = job;

            if (iconImage != null && job.jobIcon != null)
            {
                iconImage.sprite = job.jobIcon;
            }

            if (nameText != null)
            {
                nameText.text = job.jobName;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = job.jobColor * 0.5f;
            }

            // Check if requirements are met
            var skillManager = SkillManager.Instance;
            bool canAdvance = skillManager?.CanAdvanceJob(job) ?? false;

            if (button != null)
            {
                // Still selectable for viewing, but visual difference
                var colors = button.colors;
                colors.normalColor = canAdvance ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                button.colors = colors;
            }
        }

        private void OnClick()
        {
            OnSelected?.Invoke(jobData);
        }
    }
}
