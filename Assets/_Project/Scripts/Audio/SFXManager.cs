using UnityEngine;

/// <summary>
/// Static helper for playing sound effects at the correct volume.
/// Reads Audio_Master and Audio_SFX from PlayerPrefs.
/// </summary>
public static class SFXManager
{
    /// <summary>
    /// Returns the combined master * SFX volume.
    /// </summary>
    public static float GetVolume()
    {
        float master = PlayerPrefs.GetFloat("Audio_Master", 1f);
        float sfx = PlayerPrefs.GetFloat("Audio_SFX", 1f);
        return master * sfx;
    }

    /// <summary>
    /// Plays a one-shot clip through the given AudioSource at the correct SFX volume.
    /// </summary>
    public static void PlayOneShot(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        source.PlayOneShot(clip, GetVolume());
    }

    /// <summary>
    /// Plays a clip at a world position using PlayClipAtPoint, scaled to SFX volume.
    /// </summary>
    public static void PlayAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, GetVolume());
    }
}
