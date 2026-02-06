using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI component for displaying a single save slot.
    /// Shows empty state or save data (level, play time, last saved).
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button slotButton;
        [SerializeField] private TMP_Text slotNumberText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text playTimeText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private Button deleteButton;

        [Header("Visual States")]
        [SerializeField] private GameObject emptyStateGroup;
        [SerializeField] private GameObject filledStateGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;

        [Header("Colors")]
        [SerializeField] private Color emptyBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color filledBackgroundColor = new Color(0.102f, 0.102f, 0.102f, 0.95f);
        [SerializeField] private Color selectedBorderColor = new Color(0.812f, 0.710f, 0.231f, 1f);
        [SerializeField] private Color normalBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private int slotIndex;
        private bool isEmpty = true;
        private bool isSelected;

        public int SlotIndex => slotIndex;
        public bool IsEmpty => isEmpty;

        public event Action<SaveSlotUI> OnSlotClicked;
        public event Action<SaveSlotUI> OnDeleteClicked;

        private void Awake()
        {
            AutoFindReferences();
            SetupButtons();
        }

        private void AutoFindReferences()
        {
            if (slotButton == null)
                slotButton = GetComponent<Button>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            // Find child elements by name if not assigned
            if (slotNumberText == null)
            {
                var found = transform.Find("SlotNumber");
                if (found != null) slotNumberText = found.GetComponent<TMP_Text>();
            }

            if (statusText == null)
            {
                var found = transform.Find("StatusText");
                if (found != null) statusText = found.GetComponent<TMP_Text>();
            }

            if (emptyStateGroup == null)
            {
                var found = transform.Find("EmptyState");
                if (found != null) emptyStateGroup = found.gameObject;
            }

            if (filledStateGroup == null)
            {
                var found = transform.Find("FilledState");
                if (found != null) filledStateGroup = found.gameObject;
            }

            if (filledStateGroup != null)
            {
                if (characterNameText == null)
                {
                    var found = filledStateGroup.transform.Find("CharacterNameText");
                    if (found != null) characterNameText = found.GetComponent<TMP_Text>();
                }

                if (levelText == null)
                {
                    var found = filledStateGroup.transform.Find("LevelText");
                    if (found != null) levelText = found.GetComponent<TMP_Text>();
                }

                if (playTimeText == null)
                {
                    var found = filledStateGroup.transform.Find("PlayTimeText");
                    if (found != null) playTimeText = found.GetComponent<TMP_Text>();
                }

                if (dateText == null)
                {
                    var found = filledStateGroup.transform.Find("DateText");
                    if (found != null) dateText = found.GetComponent<TMP_Text>();
                }

                if (waveText == null)
                {
                    var found = filledStateGroup.transform.Find("WaveText");
                    if (found != null) waveText = found.GetComponent<TMP_Text>();
                }
            }

            if (deleteButton == null)
            {
                var found = transform.Find("DeleteButton");
                if (found != null) deleteButton = found.GetComponent<Button>();
            }

            if (borderImage == null)
            {
                var found = transform.Find("Border");
                if (found != null) borderImage = found.GetComponent<Image>();
            }
        }

        private void SetupButtons()
        {
            if (slotButton != null)
                slotButton.onClick.AddListener(HandleSlotClick);

            if (deleteButton != null)
                deleteButton.onClick.AddListener(HandleDeleteClick);
        }

        /// <summary>
        /// Initializes the slot with an index.
        /// </summary>
        public void Initialize(int index)
        {
            slotIndex = index;

            if (slotNumberText != null)
                slotNumberText.text = $"Slot {index + 1}";
        }

        /// <summary>
        /// Sets the slot to display save data.
        /// </summary>
        public void SetSlotData(SaveSlotInfo info)
        {
            if (info == null || info.isEmpty)
            {
                SetEmpty();
                return;
            }

            isEmpty = false;
            slotIndex = info.slotIndex;

            // Show filled state
            if (emptyStateGroup != null) emptyStateGroup.SetActive(false);
            if (filledStateGroup != null) filledStateGroup.SetActive(true);

            // Update display
            if (slotNumberText != null)
                slotNumberText.text = $"Slot {info.slotIndex + 1}";

            if (characterNameText != null)
                characterNameText.text = !string.IsNullOrEmpty(info.characterName) ? info.characterName : "Hero";

            if (levelText != null)
                levelText.text = $"Lv. {info.playerLevel}";

            if (playTimeText != null)
                playTimeText.text = info.FormattedPlayTime;

            if (dateText != null)
                dateText.text = info.FormattedDate;

            if (waveText != null)
            {
                string waveDisplay = info.FormattedWave;
                waveText.text = waveDisplay;
                waveText.gameObject.SetActive(!string.IsNullOrEmpty(waveDisplay));
            }

            if (statusText != null)
                statusText.text = "";

            // Show delete button for filled slots
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(true);

            // Update background color
            if (backgroundImage != null)
                backgroundImage.color = filledBackgroundColor;
        }

        /// <summary>
        /// Sets the slot to empty state.
        /// </summary>
        public void SetEmpty()
        {
            isEmpty = true;

            // Show empty state
            if (emptyStateGroup != null) emptyStateGroup.SetActive(true);
            if (filledStateGroup != null) filledStateGroup.SetActive(false);

            if (statusText != null)
                statusText.text = "Empty Slot";

            // Hide delete button for empty slots
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);

            // Update background color
            if (backgroundImage != null)
                backgroundImage.color = emptyBackgroundColor;
        }

        /// <summary>
        /// Sets the visual selected state of the slot.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (borderImage != null)
            {
                borderImage.color = selected ? selectedBorderColor : normalBorderColor;
                borderImage.gameObject.SetActive(selected || !isEmpty);
            }
        }

        /// <summary>
        /// Enables or disables the slot for interaction.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (slotButton != null)
                slotButton.interactable = interactable;
        }

        private void HandleSlotClick()
        {
            OnSlotClicked?.Invoke(this);
            UIManager.Instance?.PlaySelectSound();
        }

        private void HandleDeleteClick()
        {
            OnDeleteClicked?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (slotButton != null)
                slotButton.onClick.RemoveListener(HandleSlotClick);

            if (deleteButton != null)
                deleteButton.onClick.RemoveListener(HandleDeleteClick);
        }
    }
}
