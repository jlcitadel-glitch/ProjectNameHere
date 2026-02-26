using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ProjectName.Editor.MCP
{
    /// <summary>
    /// Assigns asset references to component properties via a JSON operation file.
    /// Designed to work with MCP's execute_menu_item tool.
    ///
    /// Usage:
    ///   1. Write operation JSON to Assets/_Project/Editor/MCP/Data/assign_asset_op.json
    ///   2. Call execute_menu_item("Tools/MCP/Assign Assets")
    ///   3. Read result from Assets/_Project/Editor/MCP/Data/assign_asset_result.json
    ///
    /// Operation JSON format:
    /// {
    ///   "assignments": [
    ///     {
    ///       "gameObjectPath": "Player",
    ///       "componentType": "SpriteRenderer",
    ///       "propertyName": "sprite",
    ///       "assetPath": "Assets/_Project/Art/Sprites/Player/idle.png",
    ///       "subAssetName": ""
    ///     }
    ///   ]
    /// }
    /// </summary>
    public static class MCPAssetAssigner
    {
        private const string OpFilePath = "Assets/_Project/Scripts/Editor/MCP/Data/assign_asset_op.json";
        private const string ResultFilePath = "Assets/_Project/Scripts/Editor/MCP/Data/assign_asset_result.json";

        [MenuItem("Tools/MCP/Assign Assets")]
        public static void ExecuteFromFile()
        {
            string fullOpPath = Path.Combine(Application.dataPath, "..", OpFilePath);

            if (!File.Exists(fullOpPath))
            {
                WriteResult(false, "Operation file not found: " + OpFilePath);
                return;
            }

            string json = File.ReadAllText(fullOpPath);
            var op = JsonUtility.FromJson<AssignmentBatch>(json);

            if (op == null || op.assignments == null || op.assignments.Length == 0)
            {
                WriteResult(false, "No assignments found in operation file");
                return;
            }

            var results = new List<AssignmentResult>();
            int successCount = 0;
            int failCount = 0;

            Undo.SetCurrentGroupName("MCP Asset Assignment");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var assignment in op.assignments)
            {
                var result = ProcessAssignment(assignment);
                results.Add(result);
                if (result.success) successCount++;
                else failCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();

            var batchResult = new BatchResult
            {
                success = failCount == 0,
                message = $"Completed: {successCount} succeeded, {failCount} failed",
                results = results.ToArray()
            };

            string resultJson = JsonUtility.ToJson(batchResult, true);
            string fullResultPath = Path.Combine(Application.dataPath, "..", ResultFilePath);
            string resultDir = Path.GetDirectoryName(fullResultPath);
            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);
            File.WriteAllText(fullResultPath, resultJson);

            if (failCount > 0)
                Debug.LogWarning($"[MCP AssetAssigner] {batchResult.message}");
            else
                Debug.Log($"[MCP AssetAssigner] {batchResult.message}");
        }

        private static AssignmentResult ProcessAssignment(AssetAssignment assignment)
        {
            // Find GameObject
            var go = GameObject.Find(assignment.gameObjectPath);
            if (go == null)
            {
                return new AssignmentResult
                {
                    success = false,
                    gameObjectPath = assignment.gameObjectPath,
                    error = $"GameObject not found: {assignment.gameObjectPath}"
                };
            }

            // Find component
            var component = FindComponent(go, assignment.componentType);
            if (component == null)
            {
                return new AssignmentResult
                {
                    success = false,
                    gameObjectPath = assignment.gameObjectPath,
                    error = $"Component not found: {assignment.componentType} on {assignment.gameObjectPath}"
                };
            }

            // Load asset
            UnityEngine.Object asset = LoadAsset(assignment.assetPath, assignment.subAssetName);
            if (asset == null)
            {
                return new AssignmentResult
                {
                    success = false,
                    gameObjectPath = assignment.gameObjectPath,
                    error = $"Asset not found: {assignment.assetPath}" +
                            (string.IsNullOrEmpty(assignment.subAssetName) ? "" : $" (sub-asset: {assignment.subAssetName})")
                };
            }

            // Assign via SerializedObject for proper undo support
            var so = new SerializedObject(component);
            var prop = so.FindProperty(assignment.propertyName);

            if (prop == null)
            {
                // Fallback: try reflection for public properties
                return TryReflectionAssignment(component, assignment.propertyName, asset, assignment);
            }

            if (prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                return new AssignmentResult
                {
                    success = false,
                    gameObjectPath = assignment.gameObjectPath,
                    error = $"Property '{assignment.propertyName}' is not an object reference (type: {prop.propertyType})"
                };
            }

            Undo.RecordObject(component, $"Assign {assignment.propertyName}");
            prop.objectReferenceValue = asset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);

            return new AssignmentResult
            {
                success = true,
                gameObjectPath = assignment.gameObjectPath,
                error = ""
            };
        }

        private static AssignmentResult TryReflectionAssignment(
            Component component, string propertyName, UnityEngine.Object asset, AssetAssignment assignment)
        {
            var type = component.GetType();

            // Try property first
            var propInfo = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            if (propInfo != null && propInfo.CanWrite)
            {
                Undo.RecordObject(component, $"Assign {propertyName}");
                propInfo.SetValue(component, asset);
                EditorUtility.SetDirty(component);

                return new AssignmentResult
                {
                    success = true,
                    gameObjectPath = assignment.gameObjectPath,
                    error = ""
                };
            }

            // Try field
            var fieldInfo = type.GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                Undo.RecordObject(component, $"Assign {propertyName}");
                fieldInfo.SetValue(component, asset);
                EditorUtility.SetDirty(component);

                return new AssignmentResult
                {
                    success = true,
                    gameObjectPath = assignment.gameObjectPath,
                    error = ""
                };
            }

            return new AssignmentResult
            {
                success = false,
                gameObjectPath = assignment.gameObjectPath,
                error = $"Property '{propertyName}' not found on {type.Name} (tried SerializedProperty, property, and field)"
            };
        }

        private static Component FindComponent(GameObject go, string typeName)
        {
            // Try exact type match first
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                if (comp.GetType().Name == typeName || comp.GetType().FullName == typeName)
                    return comp;
            }
            return null;
        }

        private static UnityEngine.Object LoadAsset(string assetPath, string subAssetName)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            // If sub-asset specified, load all and find by name
            if (!string.IsNullOrEmpty(subAssetName))
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var a in allAssets)
                {
                    if (a.name == subAssetName) return a;
                }
                return null;
            }

            // For sprite sheets (.png, .jpg), try to find the first Sprite sub-asset
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                // Try loading as Sprite first (most common for 2D games)
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var a in allAssets)
                {
                    if (a is Sprite) return a;
                }
                // Fall back to the main asset (Texture2D)
            }

            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        }

        private static void WriteResult(bool success, string message)
        {
            var result = new BatchResult
            {
                success = success,
                message = message,
                results = Array.Empty<AssignmentResult>()
            };

            string json = JsonUtility.ToJson(result, true);
            string fullPath = Path.Combine(Application.dataPath, "..", ResultFilePath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, json);

            if (!success)
                Debug.LogError($"[MCP AssetAssigner] {message}");
        }

        [Serializable]
        private class AssignmentBatch
        {
            public AssetAssignment[] assignments;
        }

        [Serializable]
        private class AssetAssignment
        {
            public string gameObjectPath;
            public string componentType;
            public string propertyName;
            public string assetPath;
            public string subAssetName;
        }

        [Serializable]
        private class AssignmentResult
        {
            public bool success;
            public string gameObjectPath;
            public string error;
        }

        [Serializable]
        private class BatchResult
        {
            public bool success;
            public string message;
            public AssignmentResult[] results;
        }
    }
}
