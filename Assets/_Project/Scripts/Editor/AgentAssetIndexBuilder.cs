#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Editor tool that generates .claude/assets.json — a pre-built index of project assets
/// so Claude agents can resolve asset locations in a single Read call.
/// </summary>
public static class AgentAssetIndexBuilder
{
    private const string OutputPath = ".claude/assets.json";
    private const string ScenesRoot = "Assets/Scenes";
    private const string ResourcesRoot = "Assets/_Project/Resources";
    private const string ScriptableObjectsRoot = "Assets/_Project/ScriptableObjects";
    private const string PrefabsRoot = "Assets/_Project/Prefabs";
    private const string ScriptsRoot = "Assets/_Project/Scripts";
    private const string IconsRoot = "Assets/_Project/Art/UI/Icons/Skills";
    private const string TilesRoot = "Assets/_Project/Art/Tiles/SunnyLandForest";

    [MenuItem("Tools/Agent Tools/Rebuild Asset Index")]
    public static void RebuildIndex()
    {
        var sw = Stopwatch.StartNew();
        var jw = new JsonWriter();
        jw.BeginObject();

        WriteMeta(jw);
        WriteScenes(jw);
        WriteResources(jw);
        WriteScriptableObjects(jw);
        WritePrefabs(jw);
        WriteScripts(jw);
        WriteIcons(jw);
        WriteTiles(jw);
        WriteEditorTools(jw);
        WriteCriticalAssets(jw);

        jw.EndObject();

        string fullPath = Path.Combine(Application.dataPath, "..", OutputPath);
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, jw.ToString() + "\n");

        sw.Stop();
        Debug.Log($"[AgentAssetIndexBuilder] Asset index rebuilt in {sw.ElapsedMilliseconds}ms → {OutputPath}");
    }

    #region Section Writers

    private static void WriteMeta(JsonWriter jw)
    {
        jw.BeginObject("_meta");
        jw.WriteString("generated", DateTime.UtcNow.ToString("o"));
        jw.WriteString("git_commit", GetGitCommitHash());
        jw.WriteString("unity_version", Application.unityVersion);
        jw.WriteString("generator", "Tools/Agent Tools/Rebuild Asset Index");
        jw.EndObject();
    }

    private static void WriteScenes(JsonWriter jw)
    {
        jw.BeginArray("scenes");
        string[] guids = AssetDatabase.FindAssets("t:SceneAsset", new[] { ScenesRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            jw.BeginObject();
            jw.WriteString("name", name);
            jw.WriteString("path", path);
            jw.EndObject();
        }
        jw.EndArray();
    }

    private static void WriteResources(JsonWriter jw)
    {
        jw.BeginObject("resources");

        string[] guids = AssetDatabase.FindAssets("", new[] { ResourcesRoot });
        var byDir = new SortedDictionary<string, List<(string name, string path, string guid)>>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path)) continue;

            string dir = Path.GetDirectoryName(path).Replace("\\", "/");
            string relDir = GetRelativeDir(dir, ResourcesRoot);
            string name = Path.GetFileNameWithoutExtension(path);

            if (!byDir.TryGetValue(relDir, out var list))
            {
                list = new List<(string, string, string)>();
                byDir[relDir] = list;
            }
            list.Add((name, path, guid));
        }

        foreach (var kvp in byDir)
        {
            var sorted = kvp.Value.OrderBy(x => x.name).ToList();
            if (sorted.Count > 5)
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, _, _) in sorted)
                    jw.WriteStringValue(name);
                jw.EndArray();
            }
            else
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, path, guid) in sorted)
                {
                    jw.BeginObject();
                    jw.WriteString("name", name);
                    jw.WriteString("path", path);
                    jw.WriteString("guid", guid);
                    jw.EndObject();
                }
                jw.EndArray();
            }
        }

        jw.EndObject();
    }

    private static void WriteScriptableObjects(JsonWriter jw)
    {
        jw.BeginObject("scriptable_objects");

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { ScriptableObjectsRoot });
        var byDir = new SortedDictionary<string, List<(string name, string path)>>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string dir = Path.GetDirectoryName(path).Replace("\\", "/");
            string relDir = GetRelativeDir(dir, ScriptableObjectsRoot);
            string name = Path.GetFileNameWithoutExtension(path);

            if (!byDir.TryGetValue(relDir, out var list))
            {
                list = new List<(string, string)>();
                byDir[relDir] = list;
            }
            list.Add((name, path));
        }

        foreach (var kvp in byDir)
        {
            var sorted = kvp.Value.OrderBy(x => x.name).ToList();
            if (sorted.Count > 5)
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, _) in sorted)
                    jw.WriteStringValue(name);
                jw.EndArray();
            }
            else
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, path) in sorted)
                {
                    jw.BeginObject();
                    jw.WriteString("name", name);
                    jw.WriteString("path", path);
                    jw.EndObject();
                }
                jw.EndArray();
            }
        }

        jw.EndObject();
    }

    private static void WritePrefabs(JsonWriter jw)
    {
        jw.BeginObject("prefabs");

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot });
        var byDir = new SortedDictionary<string, List<(string name, string path)>>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string dir = Path.GetDirectoryName(path).Replace("\\", "/");
            string relDir = GetRelativeDir(dir, PrefabsRoot);
            string name = Path.GetFileNameWithoutExtension(path);

            if (!byDir.TryGetValue(relDir, out var list))
            {
                list = new List<(string, string)>();
                byDir[relDir] = list;
            }
            list.Add((name, path));
        }

        foreach (var kvp in byDir)
        {
            var sorted = kvp.Value.OrderBy(x => x.name).ToList();
            if (sorted.Count > 5)
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, _) in sorted)
                    jw.WriteStringValue(name);
                jw.EndArray();
            }
            else
            {
                jw.BeginArray(kvp.Key);
                foreach (var (name, path) in sorted)
                {
                    jw.BeginObject();
                    jw.WriteString("name", name);
                    jw.WriteString("path", path);
                    jw.EndObject();
                }
                jw.EndArray();
            }
        }

        jw.EndObject();
    }

    private static void WriteScripts(JsonWriter jw)
    {
        jw.BeginObject("scripts");
        jw.WriteString("root", ScriptsRoot);

        string scriptsFullPath = Path.Combine(Application.dataPath, "_Project", "Scripts");
        var directories = new List<(string name, int count, string[] subdirs)>();
        int total = 0;

        if (Directory.Exists(scriptsFullPath))
        {
            // Count scripts in top-level subdirectories
            foreach (string dir in Directory.GetDirectories(scriptsFullPath, "*", SearchOption.TopDirectoryOnly))
            {
                string dirName = Path.GetFileName(dir);
                int count = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length;
                if (count == 0) continue;
                total += count;

                var subdirs = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .OrderBy(s => s)
                    .ToArray();
                directories.Add((dirName, count, subdirs));
            }

            // Count scripts directly in the root
            int rootCount = Directory.GetFiles(scriptsFullPath, "*.cs", SearchOption.TopDirectoryOnly).Length;
            total += rootCount;
        }

        jw.BeginArray("directories");
        foreach (var (name, count, subdirs) in directories.OrderBy(d => d.name))
        {
            jw.BeginObject();
            jw.WriteString("dir", name);
            jw.WriteInt("count", count);
            if (subdirs.Length > 0)
            {
                jw.BeginArray("subdirs");
                foreach (string subdir in subdirs)
                    jw.WriteStringValue(subdir);
                jw.EndArray();
            }
            jw.EndObject();
        }
        jw.EndArray();

        jw.WriteInt("total", total);
        jw.EndObject();
    }

    private static void WriteIcons(JsonWriter jw)
    {
        jw.BeginObject("icons");
        jw.WriteString("root", IconsRoot);

        string iconsFullPath = Path.Combine(Application.dataPath, "_Project", "Art", "UI", "Icons", "Skills");
        int totalIcons = 0;
        var authors = new SortedDictionary<string, int>();

        if (Directory.Exists(iconsFullPath))
        {
            foreach (string authorDir in Directory.GetDirectories(iconsFullPath))
            {
                string authorName = Path.GetFileName(authorDir);
                int count = Directory.GetFiles(authorDir, "*.png", SearchOption.AllDirectories).Length;
                if (count > 0)
                {
                    authors[authorName] = count;
                    totalIcons += count;
                }
            }
        }

        jw.WriteInt("total", totalIcons);
        jw.BeginObject("authors");
        foreach (var kvp in authors.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            jw.WriteInt(kvp.Key, kvp.Value);
        jw.EndObject();
        jw.WriteString("note", "Individual icons not listed. Path pattern: {root}/{author}/{icon}.png");

        jw.EndObject();
    }

    private static void WriteTiles(JsonWriter jw)
    {
        jw.BeginObject("tiles");
        jw.WriteString("root", TilesRoot);

        string tilesFullPath = Path.Combine(Application.dataPath, "_Project", "Art", "Tiles", "SunnyLandForest");
        int count = 0;
        if (Directory.Exists(tilesFullPath))
            count = Directory.GetFiles(tilesFullPath, "*.asset").Length;

        jw.WriteInt("count", count);
        jw.WriteString("note", "Tile assets named tileset_0.asset through tileset_N.asset");
        jw.EndObject();
    }

    private static void WriteEditorTools(JsonWriter jw)
    {
        jw.BeginArray("editor_tools");
        var methods = TypeCache.GetMethodsWithAttribute<MenuItem>();
        var toolPaths = new SortedSet<string>();

        foreach (var method in methods)
        {
            foreach (var attr in method.GetCustomAttributes<MenuItem>())
            {
                if (!attr.validate && attr.menuItem.StartsWith("Tools/"))
                    toolPaths.Add(attr.menuItem);
            }
        }

        foreach (string path in toolPaths)
            jw.WriteStringValue(path);

        jw.EndArray();
    }

    private static void WriteCriticalAssets(JsonWriter jw)
    {
        jw.BeginArray("critical_assets");

        var criticals = new[]
        {
            ("BodyPartRegistry", "Assets/_Project/Resources/BodyPartRegistry.asset", "Resources.Load singleton"),
            ("SkillIconDatabase", "Assets/_Project/Resources/SkillIconDatabase.asset", "Resources.Load, rebuilt by builder"),
            ("UISoundBank", "Assets/_Project/Resources/UISoundBank.asset", "Fallback-loaded by UIManager"),
            ("Player", "Assets/_Project/Prefabs/Player/Player.prefab", "Main player prefab"),
            ("MainMenu_Canvas", "Assets/_Project/Prefabs/UI/Canvases/MainMenu_Canvas.prefab", "Main menu UI"),
            ("HUD_Canvas", "Assets/_Project/Prefabs/UI/Canvases/HUD_Canvas.prefab", "In-game HUD"),
        };

        foreach (var (name, path, note) in criticals)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            jw.BeginObject();
            jw.WriteString("name", name);
            jw.WriteString("path", path);
            if (!string.IsNullOrEmpty(guid))
                jw.WriteString("guid", guid);
            jw.WriteString("note", note);
            jw.EndObject();
        }

        jw.EndArray();
    }

    #endregion

    #region Helpers

    private static string GetRelativeDir(string fullDir, string root)
    {
        return fullDir == root ? "(root)" : fullDir.Substring(root.Length + 1);
    }

    private static string GetGitCommitHash()
    {
        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                WorkingDirectory = Path.Combine(Application.dataPath, ".."),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return "unknown";
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return string.IsNullOrEmpty(output) ? "unknown" : output;
        }
        catch
        {
            return "unknown";
        }
    }

    #endregion

    #region JsonWriter

    private class JsonWriter
    {
        private readonly StringBuilder sb = new();
        private int indent;
        private bool needsComma;

        public void BeginObject(string key = null)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            if (key != null)
                sb.Append('"').Append(Escape(key)).Append("\": {");
            else
                sb.Append('{');
            sb.AppendLine();
            indent++;
            needsComma = false;
        }

        public void EndObject()
        {
            sb.AppendLine();
            indent--;
            WriteIndent();
            sb.Append('}');
            needsComma = true;
        }

        public void BeginArray(string key = null)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            if (key != null)
                sb.Append('"').Append(Escape(key)).Append("\": [");
            else
                sb.Append('[');
            sb.AppendLine();
            indent++;
            needsComma = false;
        }

        public void EndArray()
        {
            sb.AppendLine();
            indent--;
            WriteIndent();
            sb.Append(']');
            needsComma = true;
        }

        public void WriteString(string key, string value)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            sb.Append('"').Append(Escape(key)).Append("\": \"").Append(Escape(value)).Append('"');
            needsComma = true;
        }

        public void WriteInt(string key, int value)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            sb.Append('"').Append(Escape(key)).Append("\": ").Append(value);
            needsComma = true;
        }

        public void WriteStringValue(string value)
        {
            WriteCommaIfNeeded();
            WriteIndent();
            sb.Append('"').Append(Escape(value)).Append('"');
            needsComma = true;
        }

        private void WriteCommaIfNeeded()
        {
            if (needsComma)
            {
                sb.Append(',');
                sb.AppendLine();
            }
        }

        private void WriteIndent()
        {
            for (int i = 0; i < indent; i++)
                sb.Append("  ");
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public override string ToString() => sb.ToString();
    }

    #endregion
}
#endif
