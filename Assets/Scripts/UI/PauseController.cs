using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SpaceExplorer
{
    /// <summary>
    /// Pause menu for the Gameplay scene. Esc or P (or the pause button in the HUD)
    /// freezes the game with Time.timeScale = 0 and shows an overlay with
    /// RESUME and MAIN MENU buttons.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private GameObject resumeButton;

        /// <summary>True while the game is paused.</summary>
        public bool IsPaused { get; private set; }

        private void Start()
        {
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
            {
                Toggle();
            }
        }

        /// <summary>Pause button / Esc.</summary>
        public void Toggle()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            if (pauseOverlay != null) pauseOverlay.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (pauseOverlay != null) pauseOverlay.SetActive(false);

            // Clear the selection so Space (shoot) never "clicks" a UI button.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>Leaves the game; SceneFader restores Time.timeScale after the fade.</summary>
        public void MainMenu() => SceneFader.LoadScene(SceneNames.MainMenu);

        private void OnDestroy() => Time.timeScale = 1f;
    }
}
