using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ProjectName.UI;

/// <summary>
/// Editor tool to generate the SaveSlotUI prefab with gothic styling matching
/// the character creation screens. Run via: Tools > UI > Build SaveSlot Prefab
/// </summary>
public static class SaveSlotPrefabBuilder
{
    // Gothic palette (matches CharacterCreationController)
    private static readonly Color PanelBg = new Color(0.06f, 0.05f, 0.08f, 0.97f);
    private static readonly Color InnerBg = new Color(0.08f, 0.07f, 0.10f, 1f);
    private static readonly Color FrameGold = new Color(0.81f, 0.71f, 0.23f, 1f);
    private static readonly Color BoneWhite = new Color(0.93f, 0.89f, 0.82f, 1f);
    private static readonly Color TextSec = new Color(0.65f, 0.60f, 0.52f, 1f);
    private static readonly Color TextDim = new Color(0.45f, 0.42f, 0.38f, 1f);
    private static readonly Color DeepCrimson = new Color(0.55f, 0f, 0f, 1f);
    private static readonly Color FilledBorderCol = new Color(0.81f, 0.71f, 0.23f, 0.35f);
    private static readonly Color EmptyBorderCol = new Color(0.81f, 0.71f, 0.23f, 0.15f);
    private static readonly Color HoverTint = new Color(0.81f, 0.71f, 0.23f, 0.08f);

    [MenuItem("Tools/UI/Build SaveSlot Prefab")]
    public static void Build()
    {
        // === Root: gold-bordered gothic frame ===
        var root = new GameObject("SaveSlotUI", typeof(RectTransform));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0, 100);

        // Outer gold border
        var borderImg = root.AddComponent<Image>();
        borderImg.color = EmptyBorderCol;

        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 100;

        // Inner dark background (inset 2px for gold border effect)
        var inner = CreateChild(root.transform, "InnerBg",
            Vector2.zero, Vector2.one,
            new Vector2(2, 2), new Vector2(-2, -2));
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = InnerBg;

        // Invisible button target covers the whole slot
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = innerImg;
        var btnColors = btn.colors;
        btnColors.normalColor = Color.white;
        btnColors.highlightedColor = new Color(1.08f, 1.06f, 1.03f, 1f);
        btnColors.pressedColor = new Color(0.92f, 0.90f, 0.88f, 1f);
        btnColors.selectedColor = new Color(1.06f, 1.04f, 1.02f, 1f);
        btn.colors = btnColors;

        // Class accent strip (left edge inside border, 3px wide)
        var accent = CreateChild(inner.transform, "ClassAccent",
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 4), new Vector2(3, -4));
        accent.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        var accentImg = accent.AddComponent<Image>();
        accentImg.color = FrameGold;
        accent.SetActive(false);

        // Content area (inside accent strip)
        var content = CreateChild(inner.transform, "Content",
            Vector2.zero, Vector2.one,
            new Vector2(12, 0), new Vector2(-6, 0));

        // === Empty State ===
        var emptyState = CreateChild(content.transform, "EmptyState",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var statusText = CreateText(emptyState.transform, "StatusText",
            "An untold tale awaits\u2026", 18,
            TextAlignmentOptions.Center, TextDim,
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        statusText.fontStyle = FontStyles.Italic;

        // === Filled State (hidden by default) ===
        var filledState = CreateChild(content.transform, "FilledState",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        filledState.SetActive(false);

        // Character name — bold, bone white, left-aligned top
        var charName = CreateText(filledState.transform, "CharacterNameText",
            "Hero", 22,
            TextAlignmentOptions.Left, BoneWhite,
            new Vector2(0, 0.50f), new Vector2(0.65f, 1),
            new Vector2(6, 0), new Vector2(0, -6));
        charName.fontStyle = FontStyles.Bold;

        // Level — gold, right-aligned top
        CreateText(filledState.transform, "LevelText",
            "Lv. 1", 20,
            TextAlignmentOptions.Right, FrameGold,
            new Vector2(0.65f, 0.50f), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(-6, -6));

        // Info line — secondary text, left-aligned bottom
        CreateText(filledState.transform, "InfoLineText",
            "", 16,
            TextAlignmentOptions.Left, TextSec,
            new Vector2(0, 0), new Vector2(0.65f, 0.50f),
            new Vector2(6, 6), new Vector2(0, 0));

        // Date — dim text, right-aligned bottom
        CreateText(filledState.transform, "DateText",
            "", 15,
            TextAlignmentOptions.Right, TextDim,
            new Vector2(0.65f, 0), new Vector2(1, 0.50f),
            new Vector2(0, 6), new Vector2(-6, 0));

        // === Delete button (top-right, crimson X) ===
        var deleteGo = CreateChild(root.transform, "DeleteButton",
            new Vector2(1, 1), new Vector2(1, 1),
            Vector2.zero, Vector2.zero);
        var delRect = deleteGo.GetComponent<RectTransform>();
        delRect.pivot = new Vector2(1, 1);
        delRect.anchoredPosition = new Vector2(-4, -4);
        delRect.sizeDelta = new Vector2(26, 26);

        var delBtnImg = deleteGo.AddComponent<Image>();
        delBtnImg.color = new Color(0.08f, 0.07f, 0.10f, 0.8f);
        var delButton = deleteGo.AddComponent<Button>();
        delButton.targetGraphic = delBtnImg;
        var delColors = delButton.colors;
        delColors.normalColor = new Color(0.08f, 0.07f, 0.10f, 0.8f);
        delColors.highlightedColor = new Color(0.55f, 0f, 0f, 0.9f);
        delColors.pressedColor = new Color(0.40f, 0f, 0f, 1f);
        delColors.selectedColor = new Color(0.55f, 0f, 0f, 0.9f);
        delButton.colors = delColors;

        CreateText(deleteGo.transform, "Text",
            "X", 14,
            TextAlignmentOptions.Center, new Color(0.65f, 0.30f, 0.30f, 1f),
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        deleteGo.SetActive(false);

        // === Wire SaveSlotUI component ===
        var slotUI = root.AddComponent<SaveSlotUI>();
        var so = new SerializedObject(slotUI);
        so.FindProperty("slotButton").objectReferenceValue = btn;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("characterNameText").objectReferenceValue = charName;
        so.FindProperty("levelText").objectReferenceValue = filledState.transform.Find("LevelText").GetComponent<TMP_Text>();
        so.FindProperty("dateText").objectReferenceValue = filledState.transform.Find("DateText").GetComponent<TMP_Text>();
        so.FindProperty("infoLineText").objectReferenceValue = filledState.transform.Find("InfoLineText").GetComponent<TMP_Text>();
        so.FindProperty("deleteButton").objectReferenceValue = delButton;
        so.FindProperty("emptyStateGroup").objectReferenceValue = emptyState;
        so.FindProperty("filledStateGroup").objectReferenceValue = filledState;
        so.FindProperty("backgroundImage").objectReferenceValue = innerImg;
        so.FindProperty("borderImage").objectReferenceValue = borderImg;
        so.FindProperty("accentImage").objectReferenceValue = accentImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        // === Save prefab ===
        string dir = "Assets/_Project/Prefabs/UI/Components";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/UI", "Components");
        }

        string path = $"{dir}/SaveSlotUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        // Copy to Resources for runtime loading
        string resourcesDir = "Assets/_Project/Resources/UI";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");
        if (!AssetDatabase.IsValidFolder(resourcesDir))
            AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");

        string resourcesPath = $"{resourcesDir}/SaveSlotUI.prefab";
        if (AssetDatabase.LoadAssetAtPath<Object>(resourcesPath) != null)
            AssetDatabase.DeleteAsset(resourcesPath);
        AssetDatabase.CopyAsset(path, resourcesPath);
        AssetDatabase.Refresh();

        Debug.Log($"[SaveSlotPrefabBuilder] Prefab saved to {path} and {resourcesPath}");
    }

    private static GameObject CreateChild(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = CreateChild(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        return tmp;
    }
}
