using UnityEngine;
using System;
using System.IO;

namespace ProjectName.Editor.MCP
{
    /// <summary>
    /// Simple file-based scene lock to prevent multi-agent MCP conflicts.
    ///
    /// Lock file: .claude/scene.lock
    /// Format: { "agent": "environment", "scene": "SampleScene", "timestamp": "ISO8601" }
    ///
    /// Rules:
    /// - Agents must acquire lock before scene mutations via MCP
    /// - Read-only operations (get_scene_info, get_console_logs) don't require lock
    /// - Locks expire after 5 minutes (stale agent detection)
    /// - Only one agent can hold the lock at a time
    ///
    /// Usage from agents:
    ///   1. Write lock file: .claude/scene.lock with agent name and scene
    ///   2. Check if lock exists and is valid before MCP mutations
    ///   3. Delete lock file when done with mutations
    ///
    /// This class provides a menu item for manual lock management.
    /// </summary>
    public static class MCPSceneLock
    {
        private const string LockFileName = ".claude/scene.lock";
        private const int LockTimeoutMinutes = 5;

        /// <summary>
        /// Checks if the scene lock is currently held and valid.
        /// Returns the lock info if held, null if available.
        /// </summary>
        public static SceneLockInfo GetCurrentLock()
        {
            string lockPath = Path.Combine(Application.dataPath, "..", LockFileName);

            if (!File.Exists(lockPath))
                return null;

            string json = File.ReadAllText(lockPath);
            var lockInfo = JsonUtility.FromJson<SceneLockInfo>(json);

            if (lockInfo == null)
                return null;

            // Check expiry
            if (DateTime.TryParse(lockInfo.timestamp, out var lockTime))
            {
                if ((DateTime.UtcNow - lockTime).TotalMinutes > LockTimeoutMinutes)
                {
                    // Lock expired — remove it
                    Debug.LogWarning($"[MCP SceneLock] Stale lock from agent '{lockInfo.agent}' expired. Removing.");
                    ReleaseLock();
                    return null;
                }
            }

            return lockInfo;
        }

        /// <summary>
        /// Attempts to acquire the scene lock for the given agent.
        /// Returns true if lock was acquired, false if already held by another agent.
        /// </summary>
        public static bool TryAcquireLock(string agentName, string sceneName)
        {
            var existing = GetCurrentLock();

            if (existing != null && existing.agent != agentName)
            {
                Debug.LogWarning($"[MCP SceneLock] Lock held by '{existing.agent}' on scene '{existing.scene}'. " +
                               $"Cannot acquire for '{agentName}'.");
                return false;
            }

            var lockInfo = new SceneLockInfo
            {
                agent = agentName,
                scene = sceneName,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            string lockPath = Path.Combine(Application.dataPath, "..", LockFileName);
            string json = JsonUtility.ToJson(lockInfo, true);
            File.WriteAllText(lockPath, json);

            Debug.Log($"[MCP SceneLock] Lock acquired by '{agentName}' on scene '{sceneName}'.");
            return true;
        }

        /// <summary>
        /// Releases the scene lock.
        /// </summary>
        public static void ReleaseLock()
        {
            string lockPath = Path.Combine(Application.dataPath, "..", LockFileName);
            if (File.Exists(lockPath))
            {
                File.Delete(lockPath);
                Debug.Log("[MCP SceneLock] Lock released.");
            }
        }

        [UnityEditor.MenuItem("Tools/MCP/Show Scene Lock Status")]
        public static void ShowLockStatus()
        {
            var lockInfo = GetCurrentLock();
            if (lockInfo == null)
            {
                Debug.Log("[MCP SceneLock] No active lock. Scene is available for mutations.");
            }
            else
            {
                Debug.Log($"[MCP SceneLock] Lock held by agent '{lockInfo.agent}' " +
                         $"on scene '{lockInfo.scene}' since {lockInfo.timestamp}");
            }
        }

        [UnityEditor.MenuItem("Tools/MCP/Force Release Scene Lock")]
        public static void ForceReleaseLock()
        {
            ReleaseLock();
        }

        [Serializable]
        public class SceneLockInfo
        {
            public string agent;
            public string scene;
            public string timestamp;
        }
    }
}
