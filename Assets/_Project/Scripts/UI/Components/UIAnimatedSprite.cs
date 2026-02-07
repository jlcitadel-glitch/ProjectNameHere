using UnityEngine;
using UnityEngine.UI;

namespace ProjectName.UI
{
    /// <summary>
    /// Cycles through a Sprite[] on a UI Image at a given frame rate.
    /// Used for animated class previews in character creation.
    /// </summary>
    public class UIAnimatedSprite : MonoBehaviour
    {
        private Image targetImage;
        private Sprite[] frames;
        private float frameRate;
        private float timer;
        private int currentFrame;
        private bool isPlaying;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
        }

        private void Update()
        {
            if (!isPlaying || frames == null || frames.Length <= 1)
                return;

            timer += Time.unscaledDeltaTime;
            float interval = 1f / frameRate;

            if (timer >= interval)
            {
                timer -= interval;
                currentFrame = (currentFrame + 1) % frames.Length;

                if (targetImage != null && frames[currentFrame] != null)
                {
                    targetImage.sprite = frames[currentFrame];
                }
            }
        }

        /// <summary>
        /// Starts animating through the provided sprite frames.
        /// </summary>
        public void Play(Sprite[] spriteFrames, float framesPerSecond)
        {
            if (spriteFrames == null || spriteFrames.Length == 0)
                return;

            frames = spriteFrames;
            frameRate = Mathf.Max(framesPerSecond, 0.1f);
            currentFrame = 0;
            timer = 0f;
            isPlaying = true;

            if (targetImage != null && frames[0] != null)
            {
                targetImage.sprite = frames[0];
                targetImage.color = Color.white;
            }
        }

        /// <summary>
        /// Stops animation on the current frame.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        /// <summary>
        /// Shows a single static sprite with no animation.
        /// </summary>
        public void SetStaticSprite(Sprite sprite)
        {
            isPlaying = false;
            frames = null;

            if (targetImage != null)
            {
                targetImage.sprite = sprite;
                if (sprite != null)
                    targetImage.color = Color.white;
            }
        }
    }
}
