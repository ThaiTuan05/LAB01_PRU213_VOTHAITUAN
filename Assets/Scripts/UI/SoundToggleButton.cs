using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceExplorer
{
    /// <summary>
    /// Speaker button (top-right of the Main Menu and the Gameplay HUD).
    /// Toggles <see cref="SoundManager.Muted"/> and swaps the icon between
    /// "speaker" and "speaker crossed out".
    /// </summary>
    public class SoundToggleButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        private void Start() => Refresh();

        /// <summary>Called by the Button's OnClick event.</summary>
        public void Toggle()
        {
            SoundManager.Muted = !SoundManager.Muted;
            Refresh();

            // Deselect so pressing Space/Enter in game does not toggle it again.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Refresh()
        {
            if (icon != null)
            {
                icon.sprite = SoundManager.Muted ? soundOffSprite : soundOnSprite;
            }
        }
    }
}
