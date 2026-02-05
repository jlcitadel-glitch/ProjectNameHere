using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectName.UI
{
    /// <summary>
    /// Toast-style notification system for game events.
    /// Displays level ups, skill unlocks, SP gains, and other messages.
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;

        [Header("Layout")]
        [SerializeField] private int maxVisibleNotifications = 4;
        [SerializeField] private float notificationSpacing = 10f;
        [SerializeField] private float slideInDuration = 0.3f;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Style")]
        [SerializeField] private UIStyleGuide styleGuide;

        [Header("Notification Colors")]
        [SerializeField] private Color levelUpColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color skillUnlockColor = new Color(0.5f, 0.8f, 1f, 1f);
        [SerializeField] private Color spGainColor = new Color(0.6f, 1f, 0.6f, 1f);
        [SerializeField] private Color itemColor = new Color(0.9f, 0.7f, 0.4f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.3f, 1f);
        [SerializeField] private Color infoColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private AudioClip skillUnlockSound;
        [SerializeField] private AudioClip genericSound;

        private Queue<NotificationData> pendingNotifications = new Queue<NotificationData>();
        private List<ActiveNotification> activeNotifications = new List<ActiveNotification>();
        private AudioSource audioSource;
        private SkillManager skillManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeAudio();
            CreateDefaultPrefab();
        }

        private void Start()
        {
            SubscribeToEvents();
            InitializeStyle();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            ProcessPendingNotifications();
            UpdateActiveNotifications();
        }

        private void InitializeAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void InitializeStyle()
        {
            if (styleGuide == null && UIManager.Instance != null)
            {
                styleGuide = UIManager.Instance.StyleGuide;
            }

            if (styleGuide != null)
            {
                levelUpColor = styleGuide.agedGold;
                skillUnlockColor = styleGuide.spectralCyan;
                warningColor = styleGuide.bloodRed;
            }
        }

        private void CreateDefaultPrefab()
        {
            if (notificationPrefab != null)
                return;

            notificationPrefab = new GameObject("NotificationTemplate");
            notificationPrefab.SetActive(false);

            var rect = notificationPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 60);

            var canvasGroup = notificationPrefab.AddComponent<CanvasGroup>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(notificationPrefab.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(notificationPrefab.transform, false);
            var iconImage = icon.AddComponent<Image>();
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(40, 40);

            // Text
            var text = new GameObject("Text");
            text.transform.SetParent(notificationPrefab.transform, false);
            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(60, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            notificationPrefab.transform.SetParent(transform);
        }

        private void SubscribeToEvents()
        {
            skillManager = SkillManager.Instance;
            if (skillManager != null)
            {
                skillManager.OnPlayerLevelChanged += HandleLevelUp;
                skillManager.OnSkillLearned += HandleSkillLearned;
                skillManager.OnSPChanged += HandleSPChanged;
                skillManager.OnJobChanged += HandleJobChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (skillManager != null)
            {
                skillManager.OnPlayerLevelChanged -= HandleLevelUp;
                skillManager.OnSkillLearned -= HandleSkillLearned;
                skillManager.OnSPChanged -= HandleSPChanged;
                skillManager.OnJobChanged -= HandleJobChanged;
            }
        }

        private void HandleLevelUp(int oldLevel, int newLevel)
        {
            if (newLevel > oldLevel)
            {
                ShowNotification(
                    $"Level Up! You are now level {newLevel}",
                    NotificationType.LevelUp
                );
            }
        }

        private void HandleSkillLearned(SkillInstance skill)
        {
            ShowNotification(
                $"Skill Learned: {skill.SkillName}",
                NotificationType.SkillUnlock,
                skill.skillData?.icon
            );
        }

        private void HandleSPChanged(int oldSP, int newSP)
        {
            int gained = newSP - oldSP;
            if (gained > 0)
            {
                ShowNotification(
                    $"+{gained} Skill Points",
                    NotificationType.SPGain
                );
            }
        }

        private void HandleJobChanged(JobClassData oldJob, JobClassData newJob)
        {
            if (newJob != null)
            {
                ShowNotification(
                    $"Job Advanced: {newJob.jobName}",
                    NotificationType.LevelUp,
                    newJob.jobIcon
                );
            }
        }

        private void ProcessPendingNotifications()
        {
            while (pendingNotifications.Count > 0 && activeNotifications.Count < maxVisibleNotifications)
            {
                var data = pendingNotifications.Dequeue();
                SpawnNotification(data);
            }
        }

        private void SpawnNotification(NotificationData data)
        {
            if (notificationPrefab == null || notificationContainer == null)
                return;

            GameObject obj = Instantiate(notificationPrefab, notificationContainer);
            obj.SetActive(true);

            var rect = obj.GetComponent<RectTransform>();
            var canvasGroup = obj.GetComponent<CanvasGroup>();

            // Set initial position (off-screen to the right)
            float yPos = -activeNotifications.Count * (rect.sizeDelta.y + notificationSpacing);
            rect.anchoredPosition = new Vector2(rect.sizeDelta.x + 50, yPos);

            // Configure content
            var textComponent = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = data.message;
                textComponent.color = GetColorForType(data.type);
            }

            var iconImage = obj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                if (data.icon != null)
                {
                    iconImage.sprite = data.icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    iconImage.color = GetColorForType(data.type);
                }
            }

            var notification = new ActiveNotification
            {
                gameObject = obj,
                rectTransform = rect,
                canvasGroup = canvasGroup,
                data = data,
                state = NotificationState.SlideIn,
                timer = 0f,
                targetX = 0f
            };

            activeNotifications.Add(notification);

            // Play sound
            PlaySoundForType(data.type);
        }

        private void UpdateActiveNotifications()
        {
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = activeNotifications[i];
                notification.timer += Time.deltaTime;

                switch (notification.state)
                {
                    case NotificationState.SlideIn:
                        UpdateSlideIn(notification);
                        break;
                    case NotificationState.Display:
                        UpdateDisplay(notification);
                        break;
                    case NotificationState.FadeOut:
                        UpdateFadeOut(notification, i);
                        break;
                }
            }

            // Update positions for remaining notifications
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                var notification = activeNotifications[i];
                float targetY = -i * (notification.rectTransform.sizeDelta.y + notificationSpacing);
                Vector2 pos = notification.rectTransform.anchoredPosition;
                pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 10f);
                notification.rectTransform.anchoredPosition = pos;
            }
        }

        private void UpdateSlideIn(ActiveNotification notification)
        {
            float progress = notification.timer / slideInDuration;

            if (progress >= 1f)
            {
                notification.rectTransform.anchoredPosition = new Vector2(
                    notification.targetX,
                    notification.rectTransform.anchoredPosition.y
                );
                notification.state = NotificationState.Display;
                notification.timer = 0f;
                return;
            }

            float x = Mathf.Lerp(
                notification.rectTransform.sizeDelta.x + 50,
                notification.targetX,
                EaseOutBack(progress)
            );

            notification.rectTransform.anchoredPosition = new Vector2(
                x,
                notification.rectTransform.anchoredPosition.y
            );
        }

        private void UpdateDisplay(ActiveNotification notification)
        {
            if (notification.timer >= displayDuration)
            {
                notification.state = NotificationState.FadeOut;
                notification.timer = 0f;
            }
        }

        private void UpdateFadeOut(ActiveNotification notification, int index)
        {
            float progress = notification.timer / fadeOutDuration;

            if (progress >= 1f)
            {
                Destroy(notification.gameObject);
                activeNotifications.RemoveAt(index);
                return;
            }

            if (notification.canvasGroup != null)
            {
                notification.canvasGroup.alpha = 1f - progress;
            }

            // Slide out
            float x = Mathf.Lerp(
                notification.targetX,
                notification.rectTransform.sizeDelta.x + 50,
                progress
            );
            notification.rectTransform.anchoredPosition = new Vector2(
                x,
                notification.rectTransform.anchoredPosition.y
            );
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private Color GetColorForType(NotificationType type)
        {
            return type switch
            {
                NotificationType.LevelUp => levelUpColor,
                NotificationType.SkillUnlock => skillUnlockColor,
                NotificationType.SPGain => spGainColor,
                NotificationType.Item => itemColor,
                NotificationType.Warning => warningColor,
                _ => infoColor
            };
        }

        private void PlaySoundForType(NotificationType type)
        {
            AudioClip clip = type switch
            {
                NotificationType.LevelUp => levelUpSound,
                NotificationType.SkillUnlock => skillUnlockSound,
                _ => genericSound
            };

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Shows a notification with the specified message and type.
        /// </summary>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, Sprite icon = null)
        {
            pendingNotifications.Enqueue(new NotificationData
            {
                message = message,
                type = type,
                icon = icon
            });
        }

        /// <summary>
        /// Shows a simple info notification.
        /// </summary>
        public void ShowInfo(string message)
        {
            ShowNotification(message, NotificationType.Info);
        }

        /// <summary>
        /// Shows a warning notification.
        /// </summary>
        public void ShowWarning(string message)
        {
            ShowNotification(message, NotificationType.Warning);
        }

        /// <summary>
        /// Clears all active notifications.
        /// </summary>
        public void ClearAll()
        {
            foreach (var notification in activeNotifications)
            {
                if (notification.gameObject != null)
                {
                    Destroy(notification.gameObject);
                }
            }
            activeNotifications.Clear();
            pendingNotifications.Clear();
        }

        private class NotificationData
        {
            public string message;
            public NotificationType type;
            public Sprite icon;
        }

        private class ActiveNotification
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public NotificationData data;
            public NotificationState state;
            public float timer;
            public float targetX;
        }

        private enum NotificationState
        {
            SlideIn,
            Display,
            FadeOut
        }
    }

    public enum NotificationType
    {
        Info,
        LevelUp,
        SkillUnlock,
        SPGain,
        Item,
        Warning
    }
}
