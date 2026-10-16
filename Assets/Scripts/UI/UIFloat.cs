using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Gentle idle animation for decorative UI elements (the big spaceship and the
    /// asteroids / stars on the Main Menu): bobs up and down and optionally spins.
    /// </summary>
    public class UIFloat : MonoBehaviour
    {
        [SerializeField] private float bobAmount = 12f;   // UI units
        [SerializeField] private float bobSpeed = 1f;     // cycles per ~6 seconds
        [SerializeField] private float spinSpeed = 0f;    // degrees per second

        private RectTransform rect;
        private Vector2 basePosition;
        private float phase;

        /// <summary>Editor-time setup used by the scene builder.</summary>
        public void Setup(float bob, float speed, float spin)
        {
            bobAmount = bob;
            bobSpeed = speed;
            spinSpeed = spin;
        }

        private void Start()
        {
            rect = (RectTransform)transform;
            basePosition = rect.anchoredPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            rect.anchoredPosition = basePosition + Vector2.up * Mathf.Sin(t * bobSpeed + phase) * bobAmount;
            if (spinSpeed != 0f)
            {
                rect.Rotate(0f, 0f, spinSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
