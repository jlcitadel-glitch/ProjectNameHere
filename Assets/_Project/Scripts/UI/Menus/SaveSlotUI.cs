using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// UI component for displaying a single save slot with gothic styling.
    /// All visual elements are defined in the prefab — this script only handles data binding.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button slotButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text infoLineText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private Button deleteButton;

        [Header("Visual States")]
        [SerializeField] private GameObject emptyStateGroup;
        [SerializeField] private GameObject filledStateGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image accentImage;

        // Palette — matches main menu dark/red scheme
        private static readonly Color SlotBgEmpty = new Color(0.10f, 0.10f, 0.18f, 0.6f);
        private static readonly Color SlotBgFilled = new Color(0.10f, 0.10f, 0.18f, 1f);
        private static readonly Color FrameGold = new Color(0.81f, 0.71f, 0.23f, 1f);
        private static readonly Color SubtleGold = new Color(0.81f, 0.71f, 0.23f, 0.4f);
        private static readonly Color EmptyBorderCol = new Color(0.15f, 0.15f, 0.22f, 0.3f);

        private int slotIndex;
        private bool isEmpty = true;
        private bool isSelected;

        public int SlotIndex => slotIndex;
        public bool IsEmpty => isEmpty;

        public event Action<SaveSlotUI> OnSlotClicked;
        public event Action<SaveSlotUI> OnDeleteClicked;

        private void Awake()
        {
            SetupButtons();
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

            if (characterNameText != null)
                characterNameText.text = !string.IsNullOrEmpty(info.characterName) ? info.characterName : "Hero";

            if (levelText != null)
                levelText.text = $"Lv. {info.playerLevel}";

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

            if (dateText != null)
                dateText.text = info.FormattedDate;

            if (accentImage != null)
            {
                accentImage.gameObject.SetActive(true);
                accentImage.color = GetClassAccentColor(info.startingClass);
            }

            if (deleteButton != null)
                deleteButton.gameObject.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.color = SlotBgFilled;

            if (borderImage != null && !isSelected)
                borderImage.color = SubtleGold;
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
                backgroundImage.color = SlotBgEmpty;

            if (borderImage != null && !isSelected)
                borderImage.color = EmptyBorderCol;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (borderImage != null)
                borderImage.color = selected ? FrameGold : (isEmpty ? EmptyBorderCol : SubtleGold);
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
