using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Carries data between scenes. Static fields survive a scene load, so the
    /// Gameplay scene writes the final score here and the End Game scene reads it.
    /// The high score is persisted with PlayerPrefs so it survives closing the game.
    /// </summary>
    public static class GameSession
    {
        private const string HighScoreKey = "SpaceExplorer.HighScore";

        /// <summary>Score of the most recently finished run.</summary>
        public static int LastScore { get; private set; }

        /// <summary>Best score ever reached on this machine.</summary>
        public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

        /// <summary>True when the last run beat the previous high score.</summary>
        public static bool IsNewHighScore { get; private set; }

        /// <summary>Stores the result of a finished run and updates the high score if needed.</summary>
        public static void SubmitScore(int score)
        {
            LastScore = score;
            IsNewHighScore = score > HighScore;
            if (IsNewHighScore)
            {
                PlayerPrefs.SetInt(HighScoreKey, score);
                PlayerPrefs.Save();
            }
        }
    }
}
