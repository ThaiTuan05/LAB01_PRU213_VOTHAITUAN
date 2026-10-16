using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceExplorer
{
    /// <summary>
    /// Main Menu scene logic. The buttons call these public methods through their
    /// OnClick() events (wired in the Inspector):
    ///   Play         -> <see cref="Play"/>             : fade to the Gameplay scene
    ///   Instructions -> <see cref="ShowInstructions"/> : open the instructions panel
    ///   Close        -> <see cref="HideInstructions"/> : close the instructions panel
    ///   Quit         -> <see cref="Quit"/>             : exit the game
    /// The menu also works with the keyboard: arrows to move between buttons, Enter to press.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Title + Play/Instructions/Quit buttons; hidden while the instructions are open.")]
        [SerializeField] private GameObject menuGroup;

        [Tooltip("UI panel containing the game instructions; hidden at start.")]
        [SerializeField] private GameObject instructionsPanel;

        [Tooltip("Button selected when the instructions close (keyboard focus).")]
        [SerializeField] private GameObject instructionsButton;

        [Tooltip("Button selected when the instructions open (keyboard focus).")]
        [SerializeField] private GameObject closeButton;

        private void Start() => SetInstructionsVisible(false, false);

        public void Play() => SceneFader.LoadScene(SceneNames.Gameplay);

        public void ShowInstructions() => SetInstructionsVisible(true, true);

        public void HideInstructions() => SetInstructionsVisible(false, true);

        public void Quit() => QuitHelper.Quit();

        /// <summary>Shows either the menu or the instructions panel, never both.</summary>
        private void SetInstructionsVisible(bool visible, bool moveFocus)
        {
            if (instructionsPanel != null) instructionsPanel.SetActive(visible);
            if (menuGroup != null) menuGroup.SetActive(!visible);

            if (moveFocus && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(visible ? closeButton : instructionsButton);
            }
        }
    }
}
