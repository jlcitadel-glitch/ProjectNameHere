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
            EnsureFilledStateUI();
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

        /// <summary>
        /// Creates any missing UI elements and repositions all text for proper layout.
        /// Handles scenes built before these elements were added.
        /// Layout: Top row: [Name] [Level] [Wave]  Bottom row: [Class] [PlayTime] [Date]
        /// </summary>
        private void EnsureFilledStateUI()
        {
            // Ensure FilledState group exists
            if (filledStateGroup == null)
            {
                var go = new GameObject("FilledState", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.18f, 0);
                rt.anchorMax = new Vector2(0.92f, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.SetActive(false);
                filledStateGroup = go;
            }
            else
            {
                // Fix existing FilledState anchors for wider layout
                var rt = filledStateGroup.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.18f, 0);
                    rt.anchorMax = new Vector2(0.92f, 1);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Ensure EmptyState group exists
            if (emptyStateGroup == null)
            {
                var go = new GameObject("EmptyState", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.18f, 0);
                rt.anchorMax = new Vector2(0.92f, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                emptyStateGroup = go;

                if (statusText == null)
                {
                    statusText = CreateSlotText(go.transform, "StatusText", "Empty Slot", 20,
                        TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f, 1f),
                        new Vector2(0, 0), new Vector2(1, 1),
                        new Vector2(10, 0), Vector2.zero);
                }
            }

            var parent = filledStateGroup.transform;

            // Top row: Character Name (left), Level (center), Wave (right)
            characterNameText = EnsureSlotText(characterNameText, parent, "CharacterNameText", "Hero", 22,
                TextAlignmentOptions.Left, new Color(0.961f, 0.961f, 0.863f, 1f),
                new Vector2(0, 0.5f), new Vector2(0.45f, 1),
                new Vector2(10, 4), new Vector2(0, -4));
            characterNameText.fontStyle = FontStyles.Bold;

            levelText = EnsureSlotText(levelText, parent, "LevelText", "Lv. 1", 20,
                TextAlignmentOptions.Center, new Color(0.812f, 0.710f, 0.231f, 1f),
                new Vector2(0.45f, 0.5f), new Vector2(0.65f, 1),
                new Vector2(0, 4), new Vector2(0, -4));

            waveText = EnsureSlotText(waveText, parent, "WaveText", "", 18,
                TextAlignmentOptions.Right, new Color(0.545f, 0.545f, 0.7f, 1f),
                new Vector2(0.65f, 0.5f), new Vector2(1, 1),
                new Vector2(0, 4), new Vector2(-10, -4));

            // Bottom row: Play Time (left), Date (right)
            playTimeText = EnsureSlotText(playTimeText, parent, "PlayTimeText", "", 16,
                TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f, 1f),
                new Vector2(0, 0), new Vector2(0.45f, 0.5f),
                new Vector2(10, 4), new Vector2(0, -4));

            dateText = EnsureSlotText(dateText, parent, "DateText", "", 16,
                TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f, 1f),
                new Vector2(0.45f, 0), new Vector2(1, 0.5f),
                new Vector2(0, 4), new Vector2(-10, -4));
        }

        /// <summary>
        /// Ensures a text element exists and has correct positioning.
        /// Creates it if missing, repositions it if it already exists.
        /// </summary>
        private static TMP_Text EnsureSlotText(TMP_Text existing, Transform parent, string name,
            string defaultText, float fontSize, TextAlignmentOptions alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (existing == null)
            {
                existing = CreateSlotText(parent, name, defaultText, fontSize,
                    alignment, color, anchorMin, anchorMax, offsetMin, offsetMax);
            }
            else
            {
                // Reposition existing element
                existing.fontSize = fontSize;
                existing.alignment = alignment;
                var rt = existing.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = anchorMin;
                    rt.anchorMax = anchorMax;
                    rt.offsetMin = offsetMin;
                    rt.offsetMax = offsetMax;
                }
            }
            return existing;
        }

        private static TMP_Text CreateSlotText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return tmp;
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
