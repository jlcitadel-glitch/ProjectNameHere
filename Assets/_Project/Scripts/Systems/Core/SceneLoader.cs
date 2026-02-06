using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utility class for handling scene transitions.
/// Manages loading screens, save slot activation, and game state transitions.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public const string MAIN_MENU_SCENE = "MainMenu";
    public const string GAMEPLAY_SCENE = "SampleScene";

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private TMPro.TMP_Text loadingText;

    [Header("Settings")]
    [SerializeField] private float minimumLoadTime = 0.5f;

    public event Action OnLoadStarted;
    public event Action OnLoadCompleted;

    private bool isLoading;
    public bool IsLoading => isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Loads the main menu scene.
    /// </summary>
    public static void LoadMainMenu()
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneAsync(MAIN_MENU_SCENE, -1, false));
        }
        else
        {
            SceneManager.LoadScene(MAIN_MENU_SCENE);
        }
    }

    /// <summary>
    /// Loads the gameplay scene without loading any save data.
    /// </summary>
    public static void LoadGameplay()
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneAsync(GAMEPLAY_SCENE, -1, false));
        }
        else
        {
            SceneManager.LoadScene(GAMEPLAY_SCENE);
        }
    }

    /// <summary>
    /// Loads the gameplay scene and sets the active save slot for loading.
    /// </summary>
    public static void LoadGameplayWithSave(int slotIndex)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneAsync(GAMEPLAY_SCENE, slotIndex, true));
        }
        else
        {
            // Fallback: set save slot and load directly
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetActiveSlot(slotIndex);
                SaveManager.Instance.Load();
            }
            SceneManager.LoadScene(GAMEPLAY_SCENE);
        }
    }

    /// <summary>
    /// Loads the gameplay scene with a new game in the specified slot.
    /// </summary>
    public static void LoadGameplayNewGame(int slotIndex)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneAsync(GAMEPLAY_SCENE, slotIndex, false));
        }
        else
        {
            // Fallback: create new game and load
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CreateNewGame(slotIndex);
            }
            SceneManager.LoadScene(GAMEPLAY_SCENE);
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName, int slotIndex, bool loadSaveData)
    {
        isLoading = true;
        OnLoadStarted?.Invoke();

        // Set game state to loading
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartLoading();
        }

        // Show loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (progressBar != null) progressBar.value = 0f;
            if (loadingText != null) loadingText.text = "Loading...";
        }

        float startTime = Time.realtimeSinceStartup;

        // Set active save slot if specified
        if (slotIndex >= 0 && SaveManager.Instance != null)
        {
            SaveManager.Instance.SetActiveSlot(slotIndex);

            if (loadSaveData)
            {
                SaveManager.Instance.Load();
            }
            else
            {
                SaveManager.Instance.CreateNewGame(slotIndex);
            }
        }

        // Start async load
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait for load to complete
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            // When progress reaches 0.9, Unity is ready to activate the scene
            if (asyncLoad.progress >= 0.9f)
            {
                // Ensure minimum load time for smooth transitions
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= minimumLoadTime)
                {
                    asyncLoad.allowSceneActivation = true;
                }
            }

            yield return null;
        }

        // Hide loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        // Apply save data after scene is loaded (if loading existing save)
        if (loadSaveData && SaveManager.Instance != null)
        {
            // Wait a frame for scene objects to initialize
            yield return null;
            SaveManager.Instance.ApplyLoadedData();
        }

        // Set game state to playing
        if (GameManager.Instance != null)
        {
            GameManager.Instance.FinishLoading();
        }

        isLoading = false;
        OnLoadCompleted?.Invoke();

        Debug.Log($"[SceneLoader] Loaded scene: {sceneName}, Slot: {slotIndex}, LoadSave: {loadSaveData}");
    }

    /// <summary>
    /// Checks if a scene exists in build settings.
    /// </summary>
    public static bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }
}
