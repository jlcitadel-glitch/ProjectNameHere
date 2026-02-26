using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace ProjectName.Editor.MCP
{
    /// <summary>
    /// Captures screenshots from the Game View or Scene View for MCP-connected agents.
    /// Designed to work with MCP's execute_menu_item tool.
    ///
    /// Usage:
    ///   1. Optionally write config to Assets/_Project/Editor/MCP/Data/screenshot_op.json
    ///   2. Call execute_menu_item("Tools/MCP/Capture Game View") or
    ///      execute_menu_item("Tools/MCP/Capture Scene View")
    ///   3. Screenshot saved to Assets/_Project/Editor/MCP/Data/screenshot.png
    ///   4. Base64 written to Assets/_Project/Editor/MCP/Data/screenshot_result.json
    ///
    /// Config JSON format (optional):
    /// {
    ///   "width": 1280,
    ///   "height": 720,
    ///   "superSize": 1
    /// }
    /// </summary>
    public static class MCPScreenshotCapture
    {
        private const string DataDir = "Assets/_Project/Scripts/Editor/MCP/Data";
        private const string ConfigPath = "Assets/_Project/Scripts/Editor/MCP/Data/screenshot_op.json";
        private const string ScreenshotPath = "Assets/_Project/Scripts/Editor/MCP/Data/screenshot.png";
        private const string ResultPath = "Assets/_Project/Scripts/Editor/MCP/Data/screenshot_result.json";

        [MenuItem("Tools/MCP/Capture Game View")]
        public static void CaptureGameView()
        {
            EnsureDataDir();

            var config = LoadConfig();
            string fullPath = Path.Combine(Application.dataPath, "..", ScreenshotPath);

            try
            {
                // Use ScreenCapture for game view
                ScreenCapture.CaptureScreenshot(fullPath, config.superSize);

                // ScreenCapture is async — need to wait for file
                EditorApplication.delayCall += () =>
                {
                    // Give Unity a frame to write the file
                    EditorApplication.delayCall += () =>
                    {
                        if (File.Exists(fullPath))
                        {
                            byte[] bytes = File.ReadAllBytes(fullPath);
                            string base64 = Convert.ToBase64String(bytes);
                            WriteResult(true, "Game view captured", base64, fullPath);
                            Debug.Log($"[MCP Screenshot] Game view captured: {fullPath} ({bytes.Length} bytes)");
                        }
                        else
                        {
                            WriteResult(false, "Screenshot file not created. Is Game View open?", "", fullPath);
                            Debug.LogWarning("[MCP Screenshot] Screenshot file not created. Ensure Game View is open.");
                        }
                    };
                };
            }
            catch (Exception e)
            {
                WriteResult(false, $"Error capturing game view: {e.Message}", "", fullPath);
                Debug.LogError($"[MCP Screenshot] Error: {e.Message}");
            }
        }

        [MenuItem("Tools/MCP/Capture Scene View")]
        public static void CaptureSceneView()
        {
            EnsureDataDir();

            var config = LoadConfig();
            var sceneView = SceneView.lastActiveSceneView;

            if (sceneView == null)
            {
                WriteResult(false, "No active Scene View found", "", "");
                Debug.LogError("[MCP Screenshot] No active Scene View found");
                return;
            }

            try
            {
                int width = config.width > 0 ? config.width : 1280;
                int height = config.height > 0 ? config.height : 720;

                var camera = sceneView.camera;
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                camera.targetTexture = null;
                RenderTexture.active = null;

                byte[] pngBytes = tex.EncodeToPNG();
                string fullPath = Path.Combine(Application.dataPath, "..", ScreenshotPath);
                File.WriteAllBytes(fullPath, pngBytes);

                string base64 = Convert.ToBase64String(pngBytes);
                WriteResult(true, "Scene view captured", base64, fullPath);
                Debug.Log($"[MCP Screenshot] Scene view captured: {fullPath} ({pngBytes.Length} bytes)");

                UnityEngine.Object.DestroyImmediate(tex);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            catch (Exception e)
            {
                WriteResult(false, $"Error capturing scene view: {e.Message}", "", "");
                Debug.LogError($"[MCP Screenshot] Error: {e.Message}");
            }
        }

        private static void EnsureDataDir()
        {
            string fullDir = Path.Combine(Application.dataPath, "..", DataDir);
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);
        }

        private static ScreenshotConfig LoadConfig()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", ConfigPath);
            if (File.Exists(fullPath))
            {
                string json = File.ReadAllText(fullPath);
                return JsonUtility.FromJson<ScreenshotConfig>(json) ?? new ScreenshotConfig();
            }
            return new ScreenshotConfig();
        }

        private static void WriteResult(bool success, string message, string base64, string filePath)
        {
            var result = new ScreenshotResult
            {
                success = success,
                message = message,
                filePath = filePath,
                base64Length = base64.Length,
                base64Preview = base64.Length > 200 ? base64.Substring(0, 200) + "..." : base64
            };

            string json = JsonUtility.ToJson(result, true);
            string fullResultPath = Path.Combine(Application.dataPath, "..", ResultPath);
            File.WriteAllText(fullResultPath, json);
        }

        [Serializable]
        private class ScreenshotConfig
        {
            public int width = 1280;
            public int height = 720;
            public int superSize = 1;
        }

        [Serializable]
        private class ScreenshotResult
        {
            public bool success;
            public string message;
            public string filePath;
            public int base64Length;
            public string base64Preview;
        }
    }
}
