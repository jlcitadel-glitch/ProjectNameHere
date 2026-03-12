using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Modal popup showing 6 hotbar slots for the player to assign a skill.
    /// Built at runtime following the SkillTreeController pattern.
    /// </summary>
    public class SkillHotbarAssignPopup : MonoBehaviour
    {
        public const int DefaultSlotCount = 6;

        public static SkillHotbarAssignPopup Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            _whiteSprite = null;
        }

        private CanvasGroup canvasGroup;
        private TMP_Text titleText;
        private Button[] slotButtons;
        private TMP_Text[] slotLabels;
        private TMP_Text[] slotKeyLabels;
        private string pendingSkillId;
        private bool isVisible;

        // Colors matching SkillTreeController gothic theme
        private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        private static readonly Color AgedGold = new Color(0.812f, 0.710f, 0.231f, 1f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);
        private static readonly Color BtnNormal = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color BtnHover = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color BtnPress = new Color(0.15f, 0.15f, 0.2f, 1f);
        private static readonly Color SubtleText = new Color(0.7f, 0.65f, 0.55f, 1f);
        private static readonly Color OccupiedSlot = new Color(0.25f, 0.25f, 0.3f, 1f);

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
                }
                return _whiteSprite;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!isVisible) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            // Check hotbar keybinds — read actual bindings from PlayerSkillController's input actions
            var controller = FindAnyObjectByType<PlayerSkillController>();
            if (controller != null)
            {
                int slotCount = Mathf.Min(controller.HotbarSlots, slotButtons != null ? slotButtons.Length : 6);
                for (int i = 0; i < slotCount; i++)
                {
                    var action = controller.GetHotbarAction(i);
                    if (action != null && action.WasPressedThisFrame())
                    {
                        OnSlotClicked(i);
                        return;
                    }
                }
            }
            else
            {
                // Fallback: check default 1-6 keys
                KeyControl[] keys = { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key };
                int count = slotButtons != null ? Mathf.Min(keys.Length, slotButtons.Length) : keys.Length;
                for (int i = 0; i < count; i++)
                {
                    if (keys[i].wasPressedThisFrame)
                    {
                        OnSlotClicked(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Shows the popup for assigning the given skill to a hotbar slot.
        /// </summary>
        public void Show(string skillId)
        {
            pendingSkillId = skillId;
            isVisible = true;
            gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            RefreshSlots();

            // Update title with skill name
            if (titleText != null)
            {
                var skillData = SkillManager.Instance?.GetSkillData(skillId);
                titleText.text = skillData != null
                    ? $"Assign {skillData.skillName}"
                    : "Assign to Hotbar";
            }
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            pendingSkillId = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void RefreshSlots()
        {
            var controller = FindAnyObjectByType<PlayerSkillController>();
            if (controller == null || slotButtons == null) return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                string skillId = controller.GetHotbarSkill(i);
                string label;

                if (string.IsNullOrEmpty(skillId))
                {
                    label = "Empty";
                }
                else
                {
                    var skillData = SkillManager.Instance?.GetSkillData(skillId);
                    label = skillData?.skillName ?? skillId;
                }

                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                {
                    slotLabels[i].text = label;
                    slotLabels[i].color = string.IsNullOrEmpty(skillId) ? SubtleText : BoneWhite;
                }

                // Update key label to reflect actual binding
                if (slotKeyLabels != null && i < slotKeyLabels.Length && slotKeyLabels[i] != null)
                {
                    var action = controller.GetHotbarAction(i);
                    if (action != null)
                        slotKeyLabels[i].text = action.GetBindingDisplayString(0);
                    else
                        slotKeyLabels[i].text = (i + 1).ToString();
                }
            }
        }

        private void OnSlotClicked(int index)
        {
            if (string.IsNullOrEmpty(pendingSkillId)) return;

            var controller = FindAnyObjectByType<PlayerSkillController>();
            if (controller == null) return;

            controller.SetHotbarSkill(index, pendingSkillId);

            UIManager.Instance?.PlayConfirmSound();
            Hide();
        }

        /// <summary>
        /// Builds the popup UI at runtime.
        /// </summary>
        public static SkillHotbarAssignPopup CreateRuntimeUI(Transform rootCanvas)
        {
            // Find existing canvas or use root
            Canvas parentCanvas = rootCanvas?.GetComponentInParent<Canvas>();
            Transform parent = parentCanvas != null ? parentCanvas.transform : rootCanvas;

            // Create popup container
            var popupGo = new GameObject("SkillHotbarAssignPopup", typeof(RectTransform));
            popupGo.transform.SetParent(parent, false);

            var popupRect = popupGo.GetComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            var cg = popupGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Dark overlay (click to dismiss)
            var overlayGo = MakeRect("Overlay", popupGo.transform);
            Stretch(overlayGo);
            var overlayImg = overlayGo.AddComponent<Image>();
            overlayImg.sprite = WhiteSprite;
            overlayImg.color = new Color(0f, 0f, 0f, 0.5f);

            var overlayBtn = overlayGo.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;

            // Main panel (320 wide, height driven by content)
            var panelGo = MakeRect("Panel", popupGo.transform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320f, 0f);

            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = WhiteSprite;
            panelImg.color = PanelBg;

            var panelVLG = panelGo.AddComponent<VerticalLayoutGroup>();
            panelVLG.padding = new RectOffset(10, 10, 10, 10);
            panelVLG.spacing = 0;
            panelVLG.childControlWidth = true;
            panelVLG.childControlHeight = true;
            panelVLG.childForceExpandWidth = true;
            panelVLG.childForceExpandHeight = false;

            var panelCSF = panelGo.AddComponent<ContentSizeFitter>();
            panelCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            var titleGo = MakeRect("Title", panelGo.transform);
            AddLayout(titleGo, prefH: 35);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Assign to Hotbar";
            titleTmp.fontSize = 20;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = AgedGold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            FontManager.EnsureFont(titleTmp);

            // Divider
            BuildLayoutDivider(panelGo.transform, true);

            // Slot container (VLG — slot count derived from PlayerSkillController)
            var controller = FindAnyObjectByType<PlayerSkillController>();
            int slotCount = controller != null ? controller.HotbarSlots : DefaultSlotCount;

            var slotsContainer = MakeRect("Slots", panelGo.transform);
            var slotsVLG = slotsContainer.AddComponent<VerticalLayoutGroup>();
            slotsVLG.padding = new RectOffset(5, 5, 5, 10);
            slotsVLG.spacing = 4;
            slotsVLG.childControlWidth = true;
            slotsVLG.childControlHeight = true;
            slotsVLG.childForceExpandWidth = true;
            slotsVLG.childForceExpandHeight = false;

            float slotHeight = 40f;
            var buttons = new Button[slotCount];
            var labels = new TMP_Text[slotCount];
            var keyLabels = new TMP_Text[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var slotGo = MakeRect($"Slot_{i + 1}", slotsContainer.transform);
                AddLayout(slotGo, prefH: slotHeight);

                var slotImg = slotGo.AddComponent<Image>();
                slotImg.sprite = WhiteSprite;
                slotImg.color = BtnNormal;

                var btn = slotGo.AddComponent<Button>();
                var btnColors = btn.colors;
                btnColors.normalColor = BtnNormal;
                btnColors.highlightedColor = BtnHover;
                btnColors.pressedColor = BtnPress;
                btnColors.selectedColor = BtnHover;
                btnColors.fadeDuration = 0.1f;
                btn.colors = btnColors;

                // Key label (left side)
                var keyGo = MakeRect("Key", slotGo.transform);
                var keyRect = keyGo.GetComponent<RectTransform>();
                keyRect.anchorMin = new Vector2(0, 0);
                keyRect.anchorMax = new Vector2(0, 1);
                keyRect.pivot = new Vector2(0, 0.5f);
                keyRect.anchoredPosition = new Vector2(10, 0);
                keyRect.sizeDelta = new Vector2(30, 0);
                var keyTmp = keyGo.AddComponent<TextMeshProUGUI>();
                keyTmp.text = (i + 1).ToString();
                keyTmp.fontSize = 18;
                keyTmp.fontStyle = FontStyles.Bold;
                keyTmp.color = AgedGold;
                keyTmp.alignment = TextAlignmentOptions.Left;
                FontManager.EnsureFont(keyTmp);

                // Skill name label (center)
                var labelGo = MakeRect("Label", slotGo.transform);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(1, 1);
                labelRect.offsetMin = new Vector2(45, 0);
                labelRect.offsetMax = new Vector2(-10, 0);
                var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                labelTmp.text = "Empty";
                labelTmp.fontSize = 16;
                labelTmp.color = SubtleText;
                labelTmp.alignment = TextAlignmentOptions.Left;
                FontManager.EnsureFont(labelTmp);

                buttons[i] = btn;
                labels[i] = labelTmp;
                keyLabels[i] = keyTmp;

                int slotIndex = i;
                btn.onClick.AddListener(() =>
                {
                    popupGo.GetComponent<SkillHotbarAssignPopup>()?.OnSlotClicked(slotIndex);
                });
            }

            // Wire component
            var popup = popupGo.AddComponent<SkillHotbarAssignPopup>();
            popup.canvasGroup = cg;
            popup.titleText = titleTmp;
            popup.slotButtons = buttons;
            popup.slotLabels = labels;
            popup.slotKeyLabels = keyLabels;

            // Wire overlay dismiss
            overlayBtn.onClick.AddListener(popup.Hide);

            popupGo.SetActive(false);

            Debug.Log("[SkillHotbarAssignPopup] Runtime UI created.");
            return popup;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static LayoutElement AddLayout(GameObject go,
            float prefH = -1, float prefW = -1,
            float flexH = -1, float flexW = -1,
            float minH = -1, float minW = -1)
        {
            var le = go.AddComponent<LayoutElement>();
            if (prefH >= 0) le.preferredHeight = prefH;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (minH >= 0) le.minHeight = minH;
            if (minW >= 0) le.minWidth = minW;
            return le;
        }

        private static void BuildLayoutDivider(Transform parent, bool horizontal)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = new Color(AgedGold.r, AgedGold.g, AgedGold.b, 0.3f);
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (horizontal) { le.preferredHeight = 2; le.flexibleWidth = 1; }
            else { le.preferredWidth = 2; le.flexibleHeight = 1; }
        }
    }
}
