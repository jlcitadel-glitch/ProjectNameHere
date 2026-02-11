using System;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a sequence of cutscene beats.
/// Each beat contains dialogue text, timing, and optional VFX/camera settings.
/// </summary>
[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    [Serializable]
    public class CutsceneBeat
    {
        [TextArea(2, 5)]
        [Tooltip("Dialogue text displayed during this beat")]
        public string dialogueText;

        [Tooltip("How long this beat displays (seconds)")]
        public float displayDuration = 3f;

        [Tooltip("Camera zoom level (1 = normal, <1 = zoom in)")]
        public float cameraZoom = 1f;

        [Tooltip("Screen shake intensity (0 = none)")]
        public float screenShake;

        [Tooltip("Optional VFX prefab spawned during this beat")]
        public GameObject vfxPrefab;
    }

    [Header("Cutscene Beats")]
    public CutsceneBeat[] beats;
}
