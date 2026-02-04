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

    [Tooltip("Initial game state when GameManager is created")]
    [SerializeField] private GameManager.GameState initialState = GameManager.GameState.Playing;

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
