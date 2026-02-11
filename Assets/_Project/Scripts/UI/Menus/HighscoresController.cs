using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Displays the local highscores table in the main menu.
    /// Built at runtime via CreateRuntimeUI following the DeathScreen pattern.
    /// </summary>
    public class HighscoresController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rowContainer;
        [SerializeField] private TMP_Text emptyStateText;
        [SerializeField] private Button backButton;

        // Gothic color palette
        private static readonly Color MidnightBlue = new Color(0.098f, 0.098f, 0.439f, 0.95f);
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color HeaderColor = new Color(0.7f, 0.6f, 0.4f, 1f);
        private static readonly Color PanelBg = new Color(0.06f, 0.06f, 0.18f, 0.95f);
        private static readonly Color RowBgEven = new Color(0.08f, 0.08f, 0.25f, 0.5f);
        private static readonly Color RowBgOdd = new Color(0.1f, 0.1f, 0.3f, 0.5f);
        private static readonly Color BtnNormal = new Color(0.12f, 0.1f, 0.25f, 1f);
        private static readonly Color BtnHover = new Color(0.2f, 0.15f, 0.35f, 1f);

        private readonly List<GameObject> entryRows = new List<GameObject>();

        public event Action OnBackPressed;

        private void OnEnable()
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Rebuilds the score display from HighscoreManager data.
        /// </summary>
        public void RefreshDisplay()
        {
            // Clear old rows
            foreach (var row in entryRows)
            {
                if (row != null) Destroy(row);
            }
            entryRows.Clear();

            var scores = HighscoreManager.Instance != null
                ? HighscoreManager.Instance.GetTopScores()
                : new List<HighscoreEntry>();

            bool hasScores = scores != null && scores.Count > 0;

            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(!hasScores);
            }

            if (!hasScores || rowContainer == null)
                return;

            for (int i = 0; i < scores.Count; i++)
            {
                var entry = scores[i];
                bool isFirst = i == 0;
                Color textColor = isFirst ? AgedGold : BoneWhite;
                Color bgColor = i % 2 == 0 ? RowBgEven : RowBgOdd;

                var row = CreateEntryRow(
                    rowContainer,
                    i + 1,
                    entry.characterName,
                    entry.startingClass,
                    entry.maxWaveReached,
                    entry.FormattedPlayTime,
                    textColor,
                    bgColor
                );
                entryRows.Add(row);
            }
        }

        private void HandleBack()
        {
            UIManager.Instance?.PlayCancelSound();
            OnBackPressed?.Invoke();
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
            }
        }

        #region Runtime UI Builder

        /// <summary>
        /// Builds the entire highscores panel at runtime.
        /// Returns the HighscoresController attached to the root panel.
        /// </summary>
        public static HighscoresController CreateRuntimeUI(Transform parent)
        {
            // --- Root panel ---
            var panelGo = MakeUIObject("HighscoresPanel", parent);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(800, 600);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;

            // Vertical layout for the whole panel
            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 20, 20);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // --- Title ---
            var titleGo = MakeUIObject("Title", panelGo.transform);
            var titleTmp = FontManager.CreateText(titleGo);
            titleTmp.text = "HIGHSCORES";
            titleTmp.fontSize = 48;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 65f;

            // --- Column headers ---
            var headerRow = CreateHeaderRow(panelGo.transform);
            var headerLayout = headerRow.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 30f;

            // --- Score rows container (scrollable area) ---
            var containerGo = MakeUIObject("RowContainer", panelGo.transform);
            var containerLayout = containerGo.AddComponent<LayoutElement>();
            containerLayout.flexibleHeight = 1f;

            var containerVlg = containerGo.AddComponent<VerticalLayoutGroup>();
            containerVlg.spacing = 2f;
            containerVlg.childAlignment = TextAnchor.UpperCenter;
            containerVlg.childControlWidth = true;
            containerVlg.childControlHeight = false;
            containerVlg.childForceExpandWidth = true;
            containerVlg.childForceExpandHeight = false;

            // --- Empty state text ---
            var emptyGo = MakeUIObject("EmptyState", containerGo.transform);
            var emptyTmp = FontManager.CreateText(emptyGo);
            emptyTmp.text = "No scores recorded yet";
            emptyTmp.fontSize = 24;
            emptyTmp.color = BoneWhite;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.fontStyle = FontStyles.Italic;
            var emptyLayout = emptyGo.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 60f;

            // --- Back button ---
            var backBtn = MakeButton("Back", panelGo.transform);
            var backBtnLayout = backBtn.gameObject.AddComponent<LayoutElement>();
            backBtnLayout.preferredHeight = 40f;
            backBtnLayout.preferredWidth = 160f;

            // --- Wire component ---
            var controller = panelGo.AddComponent<HighscoresController>();
            controller.rowContainer = containerGo.transform;
            controller.emptyStateText = emptyTmp;
            controller.backButton = backBtn;
            backBtn.onClick.AddListener(controller.HandleBack);

            panelGo.SetActive(false);
            return controller;
        }

        private static GameObject CreateHeaderRow(Transform parent)
        {
            var rowGo = MakeUIObject("HeaderRow", parent);

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            CreateHeaderCell(rowGo.transform, "#", 50f);
            CreateHeaderCell(rowGo.transform, "NAME", 200f);
            CreateHeaderCell(rowGo.transform, "CLASS", 140f);
            CreateHeaderCell(rowGo.transform, "WAVE", 100f);
            CreateHeaderCell(rowGo.transform, "TIME", 140f);

            return rowGo;
        }

        private static void CreateHeaderCell(Transform parent, string text, float width)
        {
            var cellGo = MakeUIObject("Header_" + text, parent);
            var tmp = FontManager.CreateText(cellGo);
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = HeaderColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            var le = cellGo.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

        private static GameObject CreateEntryRow(
            Transform parent, int rank, string name, string className,
            int wave, string time, Color textColor, Color bgColor)
        {
            var rowGo = MakeUIObject($"Row_{rank}", parent);

            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = bgColor;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.padding = new RectOffset(0, 0, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 36f;

            CreateCell(rowGo.transform, rank.ToString(), 50f, 20, textColor);
            CreateCell(rowGo.transform, name, 200f, 20, textColor);
            CreateCell(rowGo.transform, className, 140f, 20, textColor);
            CreateCell(rowGo.transform, wave.ToString(), 100f, 20, textColor);
            CreateCell(rowGo.transform, time, 140f, 20, textColor);

            return rowGo;
        }

        private static void CreateCell(Transform parent, string text, float width, float fontSize, Color color)
        {
            var cellGo = MakeUIObject("Cell", parent);
            var tmp = FontManager.CreateText(cellGo);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            var le = cellGo.AddComponent<LayoutElement>();
            le.preferredWidth = width;
        }

        private static GameObject MakeUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button MakeButton(string label, Transform parent)
        {
            var btnGo = MakeUIObject(label + "Button", parent);

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = BtnNormal;

            var btn = btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BtnNormal;
            colors.highlightedColor = BtnHover;
            colors.pressedColor = AgedGold;
            colors.selectedColor = BtnHover;
            btn.colors = colors;

            var textGo = MakeUIObject("Text", btnGo.transform);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var tmp = FontManager.CreateText(textGo);
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.color = BoneWhite;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        #endregion
    }
}
