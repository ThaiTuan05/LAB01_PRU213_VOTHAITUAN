namespace SpaceExplorer
{
    /// <summary>
    /// Central place for scene names so that no script uses "magic strings".
    /// The names must match the scene files in Assets/Scenes and the Build Profile scene list.
    /// </summary>
    public static class SceneNames
    {
        /// <summary>Main menu: Play / Instructions / Quit.</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>The actual game: spaceship, asteroids, stars, score HUD.</summary>
        public const string Gameplay = "Gameplay";

        /// <summary>Shown after the game ends: final score, high score, Main Menu / Quit.</summary>
        public const string EndGame = "EndGame";
    }
}
