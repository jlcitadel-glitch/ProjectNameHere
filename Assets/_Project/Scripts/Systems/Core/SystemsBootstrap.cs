using UnityEngine;

/// <summary>
/// Ensures core game systems (GameManager, SaveManager) exist at runtime.
/// Add this to any scene to guarantee managers are present.
/// Creates missing managers automatically on Awake.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SystemsBootstrap : MonoBehaviour
{
    [Header("Auto-Create Settings")]
    [Tooltip("Create GameManager if not found")]
    [SerializeField] private bool ensureGameManager = true;

    [Tooltip("Create SaveManager if not found")]
    [SerializeField] private bool ensureSaveManager = true;

    [Tooltip("Create SkillManager if not found")]
    [SerializeField] private bool ensureSkillManager = true;

    [Tooltip("Create MusicManager if not found")]
    [SerializeField] private bool ensureMusicManager = true;

    [Tooltip("Create SceneLoader if not found")]
    [SerializeField] private bool ensureSceneLoader = true;

    [Tooltip("Create DisplaySettings if not found")]
    [SerializeField] private bool ensureDisplaySettings = true;

    [Header("Debug")]
    [SerializeField] private bool logCreation = true;

    private void Awake()
    {
        EnsureManagers();
    }

    private void EnsureManagers()
    {
        GameObject managersGO = null;

        // Find or create Managers container
        if (GameManager.Instance != null)
        {
            managersGO = GameManager.Instance.gameObject;
        }
        else if (SaveManager.Instance != null)
        {
            managersGO = SaveManager.Instance.gameObject;
        }

        if (managersGO == null && (ensureGameManager || ensureSaveManager))
        {
            managersGO = new GameObject("Managers");
            DontDestroyOnLoad(managersGO);

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created Managers GameObject");
            }
        }

        // Ensure GameManager
        if (ensureGameManager && GameManager.Instance == null)
        {
            managersGO.AddComponent<GameManager>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created GameManager");
            }
        }

        // Ensure SaveManager
        if (ensureSaveManager && SaveManager.Instance == null)
        {
            managersGO.AddComponent<SaveManager>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created SaveManager");
            }
        }

        // Ensure SkillManager
        if (ensureSkillManager && SkillManager.Instance == null)
        {
            managersGO.AddComponent<SkillManager>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created SkillManager");
            }
        }

        // Ensure MusicManager
        if (ensureMusicManager && MusicManager.Instance == null)
        {
            managersGO.AddComponent<MusicManager>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created MusicManager");
            }
        }

        // Ensure SceneLoader
        if (ensureSceneLoader && SceneLoader.Instance == null)
        {
            managersGO.AddComponent<SceneLoader>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created SceneLoader");
            }
        }

        // Ensure DisplaySettings
        if (ensureDisplaySettings && ProjectName.UI.DisplaySettings.Instance == null)
        {
            managersGO.AddComponent<ProjectName.UI.DisplaySettings>();

            if (logCreation)
            {
                Debug.Log("[SystemsBootstrap] Created DisplaySettings");
            }
        }

        // Self-destruct - we only need to run once
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("Create Managers Now")]
    private void CreateManagersNow()
    {
        EnsureManagers();
    }
#endif
}
