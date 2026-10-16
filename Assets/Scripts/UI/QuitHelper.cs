using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Quits the game. Application.Quit() does nothing inside the Unity Editor,
    /// so in the Editor we stop Play Mode instead - this makes the Quit button
    /// testable without building the game.
    /// </summary>
    public static class QuitHelper
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
