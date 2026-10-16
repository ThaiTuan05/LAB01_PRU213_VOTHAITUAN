using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceExplorer
{
    /// <summary>
    /// Smooth scene transitions: fades the screen to black, loads the next scene,
    /// then fades back in. The fader creates itself the first time it is used and
    /// lives across scenes (DontDestroyOnLoad), so no scene has to contain it.
    ///
    /// Usage from any script: <c>SceneFader.LoadScene(SceneNames.Gameplay);</c>
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        /// <summary>Seconds for each half of the transition (fade out, then fade in).</summary>
        public const float FadeDuration = 0.5f;

        private static SceneFader instance;

        private CanvasGroup canvasGroup;
        private bool isTransitioning;

        /// <summary>Fades out, loads <paramref name="sceneName"/>, and fades back in.</summary>
        public static void LoadScene(string sceneName)
        {
            GetOrCreate().BeginTransition(sceneName);
        }

        private static SceneFader GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            // Build a full-screen black overlay on top of every other canvas.
            var root = new GameObject("SceneFader");
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var image = new GameObject("Overlay").AddComponent<Image>();
            image.transform.SetParent(root.transform, false);
            image.color = Color.black;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            instance = root.AddComponent<SceneFader>();
            instance.canvasGroup = root.AddComponent<CanvasGroup>();
            instance.canvasGroup.alpha = 0f;
            instance.canvasGroup.blocksRaycasts = false;
            return instance;
        }

        private void BeginTransition(string sceneName)
        {
            // Ignore double clicks while a transition is already running.
            if (isTransitioning)
            {
                return;
            }

            StartCoroutine(TransitionRoutine(sceneName));
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            isTransitioning = true;
            canvasGroup.blocksRaycasts = true; // block clicks during the fade

            yield return Fade(0f, 1f);

            // Gameplay may have changed the time scale; always start a scene at normal speed.
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName);

            yield return Fade(1f, 0f);

            canvasGroup.blocksRaycasts = false;
            isTransitioning = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            // Unscaled time keeps the fade working even when the game is paused.
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
