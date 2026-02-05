using System;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a skill tree layout.
/// Contains node positions and connection data for UI rendering.
/// </summary>
[CreateAssetMenu(fileName = "NewSkillTree", menuName = "Skills/Skill Tree Data")]
public class SkillTreeData : ScriptableObject
{
    [Serializable]
    public class SkillNode
    {
        [Tooltip("The skill at this node")]
        public SkillData skill;

        [Tooltip("Position in the skill tree (0,0 = center)")]
        public Vector2 position;

        [Tooltip("Connected child nodes (skills this unlocks)")]
        public int[] childNodeIndices;

        [Tooltip("Visual row in the tree (0 = top)")]
        public int row;

        [Tooltip("Visual column in the tree")]
        public int column;
    }

    [Serializable]
    public class NodeConnection
    {
        [Tooltip("Index of the parent node")]
        public int fromNodeIndex;

        [Tooltip("Index of the child node")]
        public int toNodeIndex;

        [Tooltip("Custom curve points for the connection line (optional)")]
        public Vector2[] curvePoints;
    }

    [Header("Identity")]
    [Tooltip("Unique identifier for this skill tree")]
    public string treeId;

    [Tooltip("Display name")]
    public string treeName;

    [Header("Layout")]
    [Tooltip("All nodes in this skill tree")]
    public SkillNode[] nodes;

    [Tooltip("Connections between nodes (for rendering lines)")]
    public NodeConnection[] connections;

    [Header("Display Settings")]
    [Tooltip("Spacing between nodes horizontally")]
    public float horizontalSpacing = 150f;

    [Tooltip("Spacing between nodes vertically")]
    public float verticalSpacing = 120f;

    [Tooltip("Offset for the entire tree")]
    public Vector2 treeOffset = Vector2.zero;

    [Header("Background")]
    [Tooltip("Background image for this skill tree")]
    public Sprite backgroundImage;

    [Tooltip("Background tint color")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

    /// <summary>
    /// Gets a skill node by skill ID.
    /// </summary>
    public SkillNode GetNodeBySkillId(string skillId)
    {
        if (nodes == null) return null;

        foreach (var node in nodes)
        {
            if (node.skill != null && node.skill.skillId == skillId)
                return node;
        }
        return null;
    }

    /// <summary>
    /// Gets the index of a node by skill ID.
    /// </summary>
    public int GetNodeIndex(string skillId)
    {
        if (nodes == null) return -1;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].skill != null && nodes[i].skill.skillId == skillId)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Gets all skills in this tree.
    /// </summary>
    public SkillData[] GetAllSkills()
    {
        if (nodes == null) return new SkillData[0];

        var skills = new SkillData[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            skills[i] = nodes[i].skill;
        }
        return skills;
    }

    /// <summary>
    /// Gets the world position for a node based on grid coordinates.
    /// </summary>
    public Vector2 GetNodeWorldPosition(SkillNode node)
    {
        return new Vector2(
            node.column * horizontalSpacing + treeOffset.x,
            -node.row * verticalSpacing + treeOffset.y
        );
    }

    /// <summary>
    /// Gets the world position for a node by index.
    /// </summary>
    public Vector2 GetNodeWorldPosition(int nodeIndex)
    {
        if (nodes == null || nodeIndex < 0 || nodeIndex >= nodes.Length)
            return Vector2.zero;

        return GetNodeWorldPosition(nodes[nodeIndex]);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Auto-generates connections based on prerequisite skills.
    /// </summary>
    [ContextMenu("Generate Connections From Prerequisites")]
    public void GenerateConnectionsFromPrerequisites()
    {
        if (nodes == null) return;

        var connectionList = new System.Collections.Generic.List<NodeConnection>();

        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node.skill == null || node.skill.prerequisiteSkills == null)
                continue;

            foreach (var prereq in node.skill.prerequisiteSkills)
            {
                if (prereq == null) continue;

                int prereqIndex = GetNodeIndex(prereq.skillId);
                if (prereqIndex >= 0)
                {
                    connectionList.Add(new NodeConnection
                    {
                        fromNodeIndex = prereqIndex,
                        toNodeIndex = i
                    });
                }
            }
        }

        connections = connectionList.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
