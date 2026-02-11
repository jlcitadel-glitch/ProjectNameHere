using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectName.Editor
{
    public static class BuildScript
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/SampleScene.unity",
        };

        [MenuItem("Build/Windows (Development)")]
        public static void BuildWindowsDev()
        {
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        [MenuItem("Build/Windows (Release)")]
        public static void BuildWindowsRelease()
        {
            Build(BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        /// <summary>
        /// Entry point for CLI batch builds: -executeMethod ProjectName.Editor.BuildScript.BatchBuild
        /// </summary>
        public static void BatchBuild()
        {
            string buildPath = GetArgValue("-buildPath", "Builds/ProjectNameHere.exe");
            bool development = HasArg("-development");

            var options = development
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None;

            Build(BuildTarget.StandaloneWindows64, options, buildPath);
        }

        private static void Build(
            BuildTarget target,
            BuildOptions options,
            string outputPath = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = target == BuildTarget.StandaloneWindows64
                    ? "Builds/ProjectNameHere.exe"
                    : "Builds/ProjectNameHere";
            }

            Debug.Log($"[BuildScript] Starting {target} build → {outputPath}");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outputPath,
                target = target,
                options = options,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build succeeded: {summary.totalSize / (1024 * 1024):F1} MB in {summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                Debug.LogError($"[BuildScript] Build failed: {summary.result} ({summary.totalErrors} errors)");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static string GetArgValue(string argName, string defaultValue)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argName)
                {
                    return args[i + 1];
                }
            }
            return defaultValue;
        }

        private static bool HasArg(string argName)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == argName) return true;
            }
            return false;
        }
    }
}
