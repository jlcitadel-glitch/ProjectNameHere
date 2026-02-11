using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a CutsceneData sequence beat-by-beat.
/// Uses GameManager.StartCutscene() / EndCutscene() for state management.
/// Supports skip by holding Space for 1 second.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CutsceneUI cutsceneUI;

    [Header("Skip Settings")]
    [SerializeField] private float skipHoldDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    private Coroutine playbackCoroutine;
    private bool isPlaying;
    private bool skipRequested;
    private float skipHoldTimer;

    public bool IsPlaying => isPlaying;

    public event Action OnCutsceneStarted;
    public event Action OnCutsceneCompleted;

    private void Update()
    {
        if (!isPlaying)
            return;

        // Hold Space to skip
        if (Input.GetKey(KeyCode.Space))
        {
            skipHoldTimer += Time.unscaledDeltaTime;
            if (cutsceneUI != null)
            {
                float progress = Mathf.Clamp01(skipHoldTimer / skipHoldDuration);
                cutsceneUI.SetSkipPromptText($"Hold Space to skip... ({Mathf.RoundToInt(progress * 100)}%)");
                cutsceneUI.SetSkipPromptVisible(true);
            }

            if (skipHoldTimer >= skipHoldDuration)
            {
                skipRequested = true;
            }
        }
        else
        {
            skipHoldTimer = 0f;
            if (cutsceneUI != null)
            {
                cutsceneUI.SetSkipPromptVisible(false);
            }
        }
    }

    /// <summary>
    /// Plays a cutscene data sequence. Invokes onComplete when finished or skipped.
    /// </summary>
    public void PlayCutscene(CutsceneData data, Action onComplete = null)
    {
        if (data == null || data.beats == null || data.beats.Length == 0)
        {
            if (debugLogging)
                Debug.Log("[CutsceneManager] No cutscene data to play.");
            onComplete?.Invoke();
            return;
        }

        if (isPlaying)
        {
            if (debugLogging)
                Debug.LogWarning("[CutsceneManager] Already playing a cutscene.");
            return;
        }

        playbackCoroutine = StartCoroutine(PlaybackCoroutine(data, onComplete));
    }

    /// <summary>
    /// Immediately stops the current cutscene.
    /// </summary>
    public void StopCutscene()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        CleanUp();
    }

    private IEnumerator PlaybackCoroutine(CutsceneData data, Action onComplete)
    {
        isPlaying = true;
        skipRequested = false;
        skipHoldTimer = 0f;

        // Enter cutscene state
        GameManager.Instance?.StartCutscene();

        if (cutsceneUI != null)
        {
            cutsceneUI.Show();
        }

        OnCutsceneStarted?.Invoke();

        if (debugLogging)
            Debug.Log($"[CutsceneManager] Starting cutscene: {data.name} ({data.beats.Length} beats)");

        // Play each beat
        for (int i = 0; i < data.beats.Length; i++)
        {
            if (skipRequested)
                break;

            CutsceneData.CutsceneBeat beat = data.beats[i];

            if (debugLogging)
                Debug.Log($"[CutsceneManager] Beat {i + 1}/{data.beats.Length}: {beat.dialogueText}");

            // Spawn VFX if specified
            GameObject vfxInstance = null;
            if (beat.vfxPrefab != null)
            {
                vfxInstance = Instantiate(beat.vfxPrefab);
            }

            // Show dialogue with typewriter effect
            if (cutsceneUI != null && !string.IsNullOrEmpty(beat.dialogueText))
            {
                yield return cutsceneUI.ShowDialogue(beat.dialogueText);
            }

            // Wait for display duration
            float timer = 0f;
            while (timer < beat.displayDuration && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Clean up VFX
            if (vfxInstance != null)
            {
                Destroy(vfxInstance);
            }

            // Clear dialogue between beats
            if (cutsceneUI != null)
            {
                cutsceneUI.ClearDialogue();
            }
        }

        // Fade out UI
        if (cutsceneUI != null)
        {
            yield return cutsceneUI.FadeOutAndHide();
        }

        CleanUp();

        if (debugLogging)
            Debug.Log("[CutsceneManager] Cutscene completed.");

        OnCutsceneCompleted?.Invoke();
        onComplete?.Invoke();
    }

    private void CleanUp()
    {
        isPlaying = false;
        skipRequested = false;
        skipHoldTimer = 0f;
        playbackCoroutine = null;

        if (cutsceneUI != null)
        {
            cutsceneUI.SetSkipPromptVisible(false);
            cutsceneUI.Hide();
        }

        // Return to playing state
        GameManager.Instance?.EndCutscene();
    }
}
