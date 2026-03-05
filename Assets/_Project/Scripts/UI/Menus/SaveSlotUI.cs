using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI component for displaying a single save slot with gothic styling.
    /// Shows atmospheric empty state or save data with class-colored accent.
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

        // Gothic palette (matches CharacterCreationController)
        private static readonly Color GothicBgEmpty = new Color(0.06f, 0.05f, 0.08f, 0.5f);
        private static readonly Color GothicBgFilled = new Color(0.08f, 0.07f, 0.10f, 0.95f);
        private static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.82f, 1f);
        private static readonly Color FrameGold = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color TextSec = new Color(0.65f, 0.60f, 0.52f, 1f);
        private static readonly Color TextDim = new Color(0.45f, 0.42f, 0.38f, 1f);
        private static readonly Color GoldBorder = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color FilledBorderCol = new Color(0.25f, 0.22f, 0.18f, 0.6f);
        private static readonly Color EmptyBorderCol = new Color(0.20f, 0.18f, 0.15f, 0.3f);

        // Runtime-created elements
        private TMP_Text infoLineText;
        private Image accentImage;

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
            ApplyGothicLayout();
            SetupButtons();
        }

        private void AutoFindReferences()
        {
            if (slotButton == null)
                slotButton = GetComponent<Button>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

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
        /// Applies gothic visual overhaul: class accent strip, redesigned text layout,
        /// atmospheric empty state, and cohesive dark palette.
        /// </summary>
        private void ApplyGothicLayout()
        {
            // Override serialized colors with gothic palette
            emptyBackgroundColor = GothicBgEmpty;
            filledBackgroundColor = GothicBgFilled;
            selectedBorderColor = GoldBorder;
            normalBorderColor = FilledBorderCol;

            // Hide "Slot N" label — user doesn't want numbered slots
            if (slotNumberText != null)
                slotNumberText.gameObject.SetActive(false);

            // Ensure border exists and is always visible
            if (borderImage == null)
            {
                var borderGo = new GameObject("Border", typeof(RectTransform));
                borderGo.transform.SetParent(transform, false);
                borderImage = borderGo.AddComponent<Image>();
                var brt = borderGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;
                borderGo.transform.SetAsFirstSibling();
            }
            borderImage.color = EmptyBorderCol;

            // Class accent strip (left edge, 4px wide)
            var existingAccent = transform.Find("ClassAccent");
            if (existingAccent != null)
            {
                accentImage = existingAccent.GetComponent<Image>();
            }
            else
            {
                var accentGo = new GameObject("ClassAccent", typeof(RectTransform));
                accentGo.transform.SetParent(transform, false);
                accentImage = accentGo.AddComponent<Image>();
                var art = accentGo.GetComponent<RectTransform>();
                art.anchorMin = new Vector2(0, 0);
                art.anchorMax = new Vector2(0, 1);
                art.pivot = new Vector2(0, 0.5f);
                art.offsetMin = new Vector2(3, 6);
                art.offsetMax = new Vector2(7, -6);
            }
            accentImage.color = FrameGold;
            accentImage.gameObject.SetActive(false);

            // --- Empty state ---
            if (emptyStateGroup == null)
            {
                var go = new GameObject("EmptyState", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                emptyStateGroup = go;
            }
            else
            {
                var rt = emptyStateGroup.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            // Atmospheric empty text (centered, italic)
            statusText = EnsureSlotText(statusText, emptyStateGroup.transform, "StatusText",
                "An untold tale awaits\u2026", 20,
                TextAlignmentOptions.Center, TextSec,
                Vector2.zero, Vector2.one,
                new Vector2(10, 0), new Vector2(-10, 0));
            statusText.fontStyle = FontStyles.Italic;

            // --- Filled state ---
            if (filledStateGroup == null)
            {
                var go = new GameObject("FilledState", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.02f, 0);
                rt.anchorMax = new Vector2(0.94f, 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.SetActive(false);
                filledStateGroup = go;
            }
            else
            {
                var rt = filledStateGroup.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.02f, 0);
                    rt.anchorMax = new Vector2(0.94f, 1);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
            }

            var parent = filledStateGroup.transform;

            // Top row: Character name (left, bold), Level (right, gold)
            characterNameText = EnsureSlotText(characterNameText, parent, "CharacterNameText",
                "Hero", 24, TextAlignmentOptions.Left, BoneWhite,
                new Vector2(0, 0.48f), new Vector2(0.7f, 1),
                new Vector2(14, 0), new Vector2(0, -4));
            characterNameText.fontStyle = FontStyles.Bold;

            levelText = EnsureSlotText(levelText, parent, "LevelText",
                "Lv. 1", 22, TextAlignmentOptions.Right, FrameGold,
                new Vector2(0.7f, 0.48f), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(-4, -4));

            // Bottom row: Info line (left), Date (right)
            infoLineText = EnsureSlotText(infoLineText, parent, "InfoLineText",
                "", 17, TextAlignmentOptions.Left, TextSec,
                new Vector2(0, 0), new Vector2(0.7f, 0.52f),
                new Vector2(14, 4), new Vector2(0, 0));

            dateText = EnsureSlotText(dateText, parent, "DateText",
                "", 17, TextAlignmentOptions.Right, TextDim,
                new Vector2(0.7f, 0), new Vector2(1, 0.52f),
                new Vector2(0, 4), new Vector2(-4, 0));

            // Hide legacy separate text elements (combined into infoLineText)
            if (playTimeText != null && playTimeText.gameObject != infoLineText?.gameObject)
                playTimeText.gameObject.SetActive(false);
            if (waveText != null && waveText.gameObject != infoLineText?.gameObject)
                waveText.gameObject.SetActive(false);

            // Style delete button as gothic X
            if (deleteButton != null)
            {
                var delRect = deleteButton.GetComponent<RectTransform>();
                if (delRect != null)
                {
                    delRect.anchorMin = new Vector2(1, 1);
                    delRect.anchorMax = new Vector2(1, 1);
                    delRect.pivot = new Vector2(1, 1);
                    delRect.anchoredPosition = new Vector2(-2, -6);
                    delRect.sizeDelta = new Vector2(28, 28);
                }
                var delText = deleteButton.GetComponentInChildren<TMP_Text>();
                if (delText != null)
                {
                    delText.fontSize = 16;
                    delText.text = "\u2715";
                    delText.color = new Color(0.65f, 0.30f, 0.30f, 1f);
                }
                var delColors = deleteButton.colors;
                delColors.normalColor = new Color(0.12f, 0.08f, 0.08f, 0.5f);
                delColors.highlightedColor = new Color(0.35f, 0.10f, 0.10f, 0.9f);
                delColors.pressedColor = new Color(0.55f, 0f, 0f, 1f);
                delColors.selectedColor = new Color(0.35f, 0.10f, 0.10f, 0.9f);
                deleteButton.colors = delColors;
            }

            // Subtle hover tint on the slot button
            if (slotButton != null)
            {
                var btnColors = slotButton.colors;
                btnColors.normalColor = Color.white;
                btnColors.highlightedColor = new Color(1.15f, 1.12f, 1.08f, 1f);
                btnColors.pressedColor = new Color(0.85f, 0.82f, 0.78f, 1f);
                btnColors.selectedColor = new Color(1.10f, 1.08f, 1.05f, 1f);
                slotButton.colors = btnColors;
            }
        }

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
                existing.fontSize = fontSize;
                existing.alignment = alignment;
                existing.color = color;
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

        public void Initialize(int index)
        {
            slotIndex = index;
            // Slot number text is hidden — no "Slot 1-5" labels
        }

        public void SetSlotData(SaveSlotInfo info)
        {
            if (info == null || info.isEmpty)
            {
                SetEmpty();
                return;
            }

            isEmpty = false;
            slotIndex = info.slotIndex;

            if (emptyStateGroup != null) emptyStateGroup.SetActive(false);
            if (filledStateGroup != null) filledStateGroup.SetActive(true);

            // Character name
            if (characterNameText != null)
                characterNameText.text = !string.IsNullOrEmpty(info.characterName) ? info.characterName : "Hero";

            // Level
            if (levelText != null)
                levelText.text = $"Lv. {info.playerLevel}";

            // Build info line: "Warrior · 2h 34m · Wave 5"
            if (infoLineText != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(info.startingClass))
                    parts.Add(info.startingClass);
                if (info.playTimeSeconds > 0)
                    parts.Add(info.FormattedPlayTime);
                string wave = info.FormattedWave;
                if (!string.IsNullOrEmpty(wave))
                    parts.Add(wave);
                infoLineText.text = string.Join(" \u00b7 ", parts);
            }

            // Date
            if (dateText != null)
                dateText.text = info.FormattedDate;

            // Class accent strip
            if (accentImage != null)
            {
                accentImage.gameObject.SetActive(true);
                accentImage.color = GetClassAccentColor(info.startingClass);
            }

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.color = filledBackgroundColor;

            if (borderImage != null && !isSelected)
                borderImage.color = normalBorderColor;
        }

        public void SetEmpty()
        {
            isEmpty = true;

            if (emptyStateGroup != null) emptyStateGroup.SetActive(true);
            if (filledStateGroup != null) filledStateGroup.SetActive(false);

            if (statusText != null)
                statusText.text = "An untold tale awaits\u2026";

            if (accentImage != null)
                accentImage.gameObject.SetActive(false);

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(false);

            if (backgroundImage != null)
                backgroundImage.color = emptyBackgroundColor;

            if (borderImage != null && !isSelected)
                borderImage.color = EmptyBorderCol;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (borderImage != null)
                borderImage.color = selected ? selectedBorderColor : (isEmpty ? EmptyBorderCol : normalBorderColor);
        }

        public void SetInteractable(bool interactable)
        {
            if (slotButton != null)
                slotButton.interactable = interactable;
        }

        private static Color GetClassAccentColor(string className)
        {
            if (string.IsNullOrEmpty(className)) return FrameGold;
            switch (className.ToLowerInvariant())
            {
                case "warrior": return new Color(0.8f, 0.2f, 0.2f, 1f);
                case "mage": return new Color(0.2f, 0.4f, 0.9f, 1f);
                case "rogue": return new Color(0.6f, 0.2f, 0.8f, 1f);
                default: return FrameGold;
            }
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
