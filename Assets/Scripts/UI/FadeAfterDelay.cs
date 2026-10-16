using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Fades a UI group down to a low opacity after a delay. Used for the in-game
    /// controls hint: fully visible when the game starts, then unobtrusive so it
    /// does not distract from the play area.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeAfterDelay : MonoBehaviour
    {
        [SerializeField] private float delay = 5f;
        [SerializeField] private float fadeDuration = 1.5f;
        [SerializeField] private float targetAlpha = 0.35f;

        private CanvasGroup group;
        private float elapsed;

        private void Awake() => group = GetComponent<CanvasGroup>();

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01((elapsed - delay) / fadeDuration);
            group.alpha = Mathf.Lerp(1f, targetAlpha, t);
        }
    }
}
