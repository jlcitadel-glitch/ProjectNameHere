#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for browsing, searching, and assigning skill icons.
/// Displays icons in a paginated grid with search and tag filtering.
/// </summary>
public class SkillIconBrowserWindow : EditorWindow
{
    private const int IconsPerPage = 200;
    private const float IconSize = 64f;
    private const float IconPadding = 4f;

    private SkillIconDatabase database;
    private string searchFilter = "";
    private int currentPage;
    private Vector2 scrollPosition;
    private List<SkillIconEntry> filteredIcons;
    private SkillData targetSkillData;

    // Preview
    private SkillIconEntry previewEntry;
    private Color previewTint = Color.white;

    [MenuItem("Tools/Skill Icons/Icon Browser")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillIconBrowserWindow>("Skill Icon Browser");
        window.minSize = new Vector2(400, 300);
    }

    /// <summary>
    /// Opens the browser targeting a specific SkillData for assignment.
    /// </summary>
    public static void ShowForSkillData(SkillData skill)
    {
        var window = GetWindow<SkillIconBrowserWindow>("Skill Icon Browser");
        window.targetSkillData = skill;
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        RefreshDatabase();
        ApplyFilter();
    }

    private void RefreshDatabase()
    {
        database = AssetDatabase.LoadAssetAtPath<SkillIconDatabase>(
            "Assets/_Project/Resources/SkillIconDatabase.asset");
    }

    private void OnGUI()
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "No SkillIconDatabase found. Use Tools/Skill Icons/Rebuild Database first.",
                MessageType.Warning);

            if (GUILayout.Button("Rebuild Database"))
            {
                SkillIconDatabaseBuilder.RebuildDatabase();
                RefreshDatabase();
                ApplyFilter();
            }
            return;
        }

        DrawToolbar();
        DrawTarget();
        DrawPreview();
        DrawIconGrid();
        DrawPagination();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField,
            GUILayout.MinWidth(200));
        if (EditorGUI.EndChangeCheck())
        {
            currentPage = 0;
            ApplyFilter();
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SkillIconDatabaseBuilder.RebuildDatabase();
            RefreshDatabase();
            ApplyFilter();
        }

        GUILayout.FlexibleSpace();

        int totalIcons = database.allIcons?.Length ?? 0;
        int filtered = filteredIcons?.Count ?? 0;
        EditorGUILayout.LabelField($"{filtered}/{totalIcons} icons", EditorStyles.miniLabel,
            GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTarget()
    {
        if (targetSkillData == null) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Assigning to: {targetSkillData.skillName}", EditorStyles.boldLabel);

        if (!string.IsNullOrEmpty(targetSkillData.iconId))
        {
            EditorGUILayout.LabelField($"Current: {targetSkillData.iconId}", GUILayout.Width(200));
        }

        if (GUILayout.Button("Clear Target", GUILayout.Width(90)))
        {
            targetSkillData = null;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview()
    {
        if (previewEntry == null) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // Large preview
        if (previewEntry.sprite != null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96));
            GUI.DrawTexture(previewRect, previewEntry.sprite.texture, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(previewEntry.displayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {previewEntry.iconId}");

        if (previewEntry.tags != null && previewEntry.tags.Length > 0)
        {
            EditorGUILayout.LabelField($"Tags: {string.Join(", ", previewEntry.tags)}");
        }

        previewTint = EditorGUILayout.ColorField("Preview Tint", previewTint);

        EditorGUILayout.BeginHorizontal();
        if (targetSkillData != null && GUILayout.Button("Assign to Skill"))
        {
            AssignToSkill(previewEntry);
        }
        if (GUILayout.Button("Copy ID"))
        {
            EditorGUIUtility.systemCopyBuffer = previewEntry.iconId;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawIconGrid()
    {
        if (filteredIcons == null || filteredIcons.Count == 0)
        {
            EditorGUILayout.HelpBox("No icons match the current filter.", MessageType.Info);
            return;
        }

        // Calculate grid dimensions
        float availableWidth = position.width - 20f;
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (IconSize + IconPadding)));

        // Page slice
        int startIndex = currentPage * IconsPerPage;
        int endIndex = Mathf.Min(startIndex + IconsPerPage, filteredIcons.Count);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int col = 0;
        EditorGUILayout.BeginHorizontal();

        for (int i = startIndex; i < endIndex; i++)
        {
            var entry = filteredIcons[i];
            if (entry?.sprite == null) continue;

            // Draw icon button
            Rect iconRect = GUILayoutUtility.GetRect(IconSize, IconSize,
                GUILayout.Width(IconSize), GUILayout.Height(IconSize));

            // Highlight if selected
            if (previewEntry == entry)
            {
                EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
            }

            // Draw with tint if previewing
            Color drawColor = (previewEntry == entry) ? previewTint : Color.white;
            GUI.color = drawColor;
            GUI.DrawTexture(iconRect, entry.sprite.texture, ScaleMode.ScaleToFit);
            GUI.color = Color.white;

            // Handle click
            if (Event.current.type == EventType.MouseDown && iconRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    previewEntry = entry;
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.button == 1 && targetSkillData != null)
                {
                    // Right-click to quick-assign
                    AssignToSkill(entry);
                    Event.current.Use();
                }
            }

            // Tooltip
            if (iconRect.Contains(Event.current.mousePosition))
            {
                GUI.tooltip = $"{entry.displayName}\n{entry.iconId}";
            }

            col++;
            if (col >= columns)
            {
                col = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawPagination()
    {
        if (filteredIcons == null) return;

        int totalPages = Mathf.CeilToInt((float)filteredIcons.Count / IconsPerPage);
        if (totalPages <= 1) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.enabled = currentPage > 0;
        if (GUILayout.Button("<", GUILayout.Width(30)))
        {
            currentPage--;
            scrollPosition = Vector2.zero;
        }

        GUI.enabled = true;
        EditorGUILayout.LabelField($"Page {currentPage + 1}/{totalPages}",
            GUILayout.Width(80));

        GUI.enabled = currentPage < totalPages - 1;
        if (GUILayout.Button(">", GUILayout.Width(30)))
        {
            currentPage++;
            scrollPosition = Vector2.zero;
        }

        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyFilter()
    {
        if (database?.allIcons == null)
        {
            filteredIcons = new List<SkillIconEntry>();
            return;
        }

        if (string.IsNullOrEmpty(searchFilter))
        {
            filteredIcons = new List<SkillIconEntry>(database.allIcons);
        }
        else
        {
            filteredIcons = database.Search(searchFilter);
        }
    }

    private void AssignToSkill(SkillIconEntry entry)
    {
        if (targetSkillData == null || entry == null) return;

        Undo.RecordObject(targetSkillData, "Assign Skill Icon");
        targetSkillData.iconId = entry.iconId;
        EditorUtility.SetDirty(targetSkillData);

        Debug.Log($"[SkillIconBrowser] Assigned '{entry.iconId}' to '{targetSkillData.skillName}'");
    }
}
#endif
