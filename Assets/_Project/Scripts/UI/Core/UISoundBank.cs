using UnityEngine;

namespace ProjectName.UI
{
    /// <summary>
    /// Audio asset containing all UI sound effects.
    /// Provides consistent audio feedback across the gothic UI.
    /// </summary>
    [CreateAssetMenu(fileName = "UISoundBank", menuName = "Audio/UI Sound Bank")]
    public class UISoundBank : ScriptableObject
    {
        [Header("Navigation")]
        [Tooltip("Subtle tick when navigating between elements")]
        public AudioClip navigate;

        [Tooltip("Deeper confirmation when selecting an element")]
        public AudioClip select;

        [Tooltip("Soft whoosh when canceling/going back")]
        public AudioClip cancel;

        [Tooltip("Page turn / stone slide for tab switching")]
        public AudioClip tabSwitch;

        [Header("Actions")]
        [Tooltip("Satisfying click for confirmations")]
        public AudioClip confirm;

        [Tooltip("Low buzz for errors/invalid actions")]
        public AudioClip error;

        [Tooltip("Mystical chime for item pickup")]
        public AudioClip itemPickup;

        [Tooltip("Inventory slot equip/unequip sound")]
        public AudioClip equipItem;

        [Header("Menu Transitions")]
        [Tooltip("Stone door / book open for menu opening")]
        public AudioClip menuOpen;

        [Tooltip("Reverse of open for menu closing")]
        public AudioClip menuClose;

        [Tooltip("Pause game sound")]
        public AudioClip pause;

        [Tooltip("Resume game sound")]
        public AudioClip resume;

        [Header("Gothic Ambience")]
        [Tooltip("Low cathedral reverb background")]
        public AudioClip backgroundDrone;

        [Tooltip("Subtle fire crackle for atmosphere")]
        public AudioClip candleFlicker;

        [Tooltip("Wind howling for outdoor menus")]
        public AudioClip windAmbience;

        [Header("Feedback")]
        [Tooltip("Health gained sound")]
        public AudioClip healthGain;

        [Tooltip("Health lost sound")]
        public AudioClip healthLoss;

        [Tooltip("Soul/magic meter fill sound")]
        public AudioClip soulFill;

        [Tooltip("Ability ready notification")]
        public AudioClip abilityReady;

        [Header("Volume Settings")]
        [Tooltip("Default volume for navigation sounds")]
        [Range(0f, 1f)]
        public float navigationVolume = 0.5f;

        [Tooltip("Default volume for action sounds")]
        [Range(0f, 1f)]
        public float actionVolume = 0.7f;

        [Tooltip("Default volume for ambient sounds")]
        [Range(0f, 1f)]
        public float ambienceVolume = 0.3f;

        /// <summary>
        /// Plays a UI sound at the specified volume.
        /// </summary>
        public void PlaySound(AudioClip clip, AudioSource source, float volumeMultiplier = 1f)
        {
            if (clip == null || source == null)
                return;

            source.PlayOneShot(clip, volumeMultiplier);
        }

        /// <summary>
        /// Plays a navigation sound.
        /// </summary>
        public void PlayNavigate(AudioSource source)
        {
            PlaySound(navigate, source, navigationVolume);
        }

        /// <summary>
        /// Plays a selection sound.
        /// </summary>
        public void PlaySelect(AudioSource source)
        {
            PlaySound(select, source, navigationVolume);
        }

        /// <summary>
        /// Plays a cancel sound.
        /// </summary>
        public void PlayCancel(AudioSource source)
        {
            PlaySound(cancel, source, navigationVolume);
        }

        /// <summary>
        /// Plays a tab switch sound.
        /// </summary>
        public void PlayTabSwitch(AudioSource source)
        {
            PlaySound(tabSwitch, source, actionVolume);
        }

        /// <summary>
        /// Plays a confirmation sound.
        /// </summary>
        public void PlayConfirm(AudioSource source)
        {
            PlaySound(confirm, source, actionVolume);
        }

        /// <summary>
        /// Plays an error sound.
        /// </summary>
        public void PlayError(AudioSource source)
        {
            PlaySound(error, source, actionVolume);
        }

        /// <summary>
        /// Plays menu open sound.
        /// </summary>
        public void PlayMenuOpen(AudioSource source)
        {
            PlaySound(menuOpen, source, actionVolume);
        }

        /// <summary>
        /// Plays menu close sound.
        /// </summary>
        public void PlayMenuClose(AudioSource source)
        {
            PlaySound(menuClose, source, actionVolume);
        }
    }
}
