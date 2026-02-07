using System;
using UnityEngine;

/// <summary>
/// Central game state manager. Owns game state, time control, and core game flow.
/// All other systems react to state changes via events.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Cutscene,
        Loading,
        GameOver,
        BossFight
    }

    [Header("State")]
    [SerializeField] private GameState initialState = GameState.MainMenu;

    private GameState currentState;
    private GameState previousState;
    private float previousTimeScale = 1f;

    public GameState CurrentState => currentState;
    public GameState PreviousState => previousState;
    public bool IsPaused => currentState == GameState.Paused;
    public bool IsPlaying => currentState == GameState.Playing || currentState == GameState.BossFight;
    public bool IsInMenu => currentState == GameState.MainMenu || currentState == GameState.Paused;

    /// <summary>
    /// Fired when game state changes. Provides previous and new state.
    /// </summary>
    public event Action<GameState, GameState> OnGameStateChanged;

    /// <summary>
    /// Convenience event for pause specifically.
    /// </summary>
    public event Action OnPause;

    /// <summary>
    /// Convenience event for resume specifically.
    /// </summary>
    public event Action OnResume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Expected on scene reload — DontDestroyOnLoad instance already exists
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentState = initialState;
        previousState = initialState;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Sets the game state. Handles time scale and fires events.
    /// </summary>
    public void SetState(GameState newState)
    {
        if (currentState == newState)
            return;

        previousState = currentState;
        currentState = newState;

        ApplyTimeScaleForState(newState);

        Debug.Log($"[GameManager] State changed: {previousState} -> {currentState}");

        OnGameStateChanged?.Invoke(previousState, currentState);

        if (currentState == GameState.Paused)
        {
            OnPause?.Invoke();
        }
        else if (previousState == GameState.Paused &&
                 (currentState == GameState.Playing || currentState == GameState.BossFight))
        {
            OnResume?.Invoke();
        }
    }

    private void ApplyTimeScaleForState(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.GameOver:
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                break;

            case GameState.Playing:
            case GameState.BossFight:
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
                break;

            case GameState.MainMenu:
            case GameState.Cutscene:
            case GameState.Loading:
                Time.timeScale = 1f;
                break;
        }
    }

    /// <summary>
    /// Pauses the game. Only works from Playing or BossFight states.
    /// </summary>
    public void Pause()
    {
        if (currentState == GameState.Playing || currentState == GameState.BossFight)
        {
            SetState(GameState.Paused);
        }
    }

    /// <summary>
    /// Resumes the game from pause.
    /// </summary>
    public void Resume()
    {
        if (currentState == GameState.Paused)
        {
            GameState targetState = previousState == GameState.Paused ? GameState.Playing : previousState;
            if (targetState != GameState.Playing && targetState != GameState.BossFight)
            {
                targetState = GameState.Playing;
            }
            SetState(targetState);
        }
    }

    /// <summary>
    /// Toggles between paused and playing states.
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    /// <summary>
    /// Starts gameplay from main menu or after loading.
    /// </summary>
    public void StartPlaying()
    {
        SetState(GameState.Playing);
    }

    /// <summary>
    /// Enters boss fight mode.
    /// </summary>
    public void EnterBossFight()
    {
        SetState(GameState.BossFight);
    }

    /// <summary>
    /// Exits boss fight mode back to normal play.
    /// </summary>
    public void ExitBossFight()
    {
        if (currentState == GameState.BossFight)
        {
            SetState(GameState.Playing);
        }
    }

    /// <summary>
    /// Triggers game over state.
    /// </summary>
    public void GameOver()
    {
        SetState(GameState.GameOver);
    }

    /// <summary>
    /// Starts a cutscene.
    /// </summary>
    public void StartCutscene()
    {
        SetState(GameState.Cutscene);
    }

    /// <summary>
    /// Ends a cutscene and returns to playing.
    /// </summary>
    public void EndCutscene()
    {
        if (currentState == GameState.Cutscene)
        {
            SetState(GameState.Playing);
        }
    }

    /// <summary>
    /// Returns to main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        SetState(GameState.MainMenu);
    }

    /// <summary>
    /// Enters loading state.
    /// </summary>
    public void StartLoading()
    {
        SetState(GameState.Loading);
    }

    /// <summary>
    /// Exits loading state and starts playing.
    /// </summary>
    public void FinishLoading()
    {
        if (currentState == GameState.Loading)
        {
            SetState(GameState.Playing);
        }
    }

    /// <summary>
    /// Sets time scale for slow motion effects. Only works in Playing or BossFight states.
    /// </summary>
    public void SetTimeScale(float scale)
    {
        if (currentState == GameState.Playing || currentState == GameState.BossFight)
        {
            Time.timeScale = Mathf.Clamp(scale, 0f, 2f);
            previousTimeScale = Time.timeScale;
        }
    }
}
