using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// HUD element showing the currently equipped weapon type.
    /// Subscribes to CombatController.OnWeaponSwitched.
    /// </summary>
    public class WeaponIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text weaponNameText;

        [Header("Weapon Icons")]
        [SerializeField] private Sprite meleeIcon;
        [SerializeField] private Sprite rangedIcon;
        [SerializeField] private Sprite magicIcon;
        [SerializeField] private Sprite emptyIcon;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;
        [SerializeField] private Color meleeColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        [SerializeField] private Color rangedColor = new Color(0.4f, 0.7f, 0.3f, 1f);
        [SerializeField] private Color magicColor = new Color(0.5f, 0.3f, 0.9f, 1f);

        [Header("Animation")]
        [SerializeField] private float switchAnimDuration = 0.2f;
        [SerializeField] private float pulseScale = 1.2f;

        private CombatController combatController;
        private WeaponType currentWeaponType;
        private float animTimer;
        private bool isAnimating;
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void Start()
        {
            FindCombatController();
            InitializeStyle();
            UpdateDisplay(currentWeaponType);
        }

        private void OnDestroy()
        {
            if (combatController != null)
            {
                combatController.OnWeaponSwitched -= HandleWeaponSwitched;
            }
        }

        private void Update()
        {
            UpdateAnimation();
        }

        private void FindCombatController()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                combatController = player.GetComponent<CombatController>();
                if (combatController != null)
                {
                    combatController.OnWeaponSwitched += HandleWeaponSwitched;
                    currentWeaponType = combatController.ActiveWeaponType;
                    Debug.Log("[WeaponIndicator] Connected to CombatController");
                }
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null && UIManager.Instance != null)
            {
                styleGuide = UIManager.Instance.StyleGuide;
            }

            if (styleGuide != null && backgroundImage != null)
            {
                backgroundImage.color = styleGuide.charcoal;
            }
        }

        private void HandleWeaponSwitched(WeaponType newType)
        {
            currentWeaponType = newType;
            UpdateDisplay(newType);
            PlaySwitchAnimation();
        }

        private void UpdateDisplay(WeaponType type)
        {
            if (weaponIcon != null)
            {
                weaponIcon.sprite = GetIconForType(type);
                weaponIcon.color = GetColorForType(type);
            }

            if (weaponNameText != null)
            {
                weaponNameText.text = GetNameForType(type);
                weaponNameText.color = GetColorForType(type);
            }
        }

        private Sprite GetIconForType(WeaponType type)
        {
            return type switch
            {
                WeaponType.Melee => meleeIcon,
                WeaponType.Ranged => rangedIcon,
                WeaponType.Magic => magicIcon,
                _ => emptyIcon
            } ?? emptyIcon;
        }

        private Color GetColorForType(WeaponType type)
        {
            return type switch
            {
                WeaponType.Melee => meleeColor,
                WeaponType.Ranged => rangedColor,
                WeaponType.Magic => magicColor,
                _ => Color.white
            };
        }

        private string GetNameForType(WeaponType type)
        {
            // Try to get actual weapon name from combat controller
            if (combatController != null)
            {
                var weapon = combatController.GetActiveWeapon();
                if (weapon != null)
                {
                    return weapon.weaponName;
                }
            }

            return type switch
            {
                WeaponType.Melee => "Melee",
                WeaponType.Ranged => "Ranged",
                WeaponType.Magic => "Magic",
                _ => "None"
            };
        }

        private void PlaySwitchAnimation()
        {
            isAnimating = true;
            animTimer = 0f;
        }

        private void UpdateAnimation()
        {
            if (!isAnimating)
                return;

            animTimer += Time.deltaTime;
            float progress = animTimer / switchAnimDuration;

            if (progress >= 1f)
            {
                transform.localScale = originalScale;
                isAnimating = false;
                return;
            }

            // Pulse scale animation
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (pulseScale - 1f);
            transform.localScale = originalScale * scale;
        }

        /// <summary>
        /// Manually sets the combat controller reference.
        /// </summary>
        public void SetCombatController(CombatController controller)
        {
            if (combatController != null)
            {
                combatController.OnWeaponSwitched -= HandleWeaponSwitched;
            }

            combatController = controller;

            if (combatController != null)
            {
                combatController.OnWeaponSwitched += HandleWeaponSwitched;
                currentWeaponType = combatController.ActiveWeaponType;
                UpdateDisplay(currentWeaponType);
            }
        }
    }
}
