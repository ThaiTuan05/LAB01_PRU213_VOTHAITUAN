using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer
{
    /// <summary>
    /// End Game scene logic: shows the final score (read from <see cref="GameSession"/>)
    /// and the high score, and offers Play Again, Main Menu and Quit buttons.
    /// </summary>
    public class EndGameController : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;

        private void Start()
        {
            if (scoreText != null)
            {
                scoreText.text = GameSession.LastScore.ToString();
            }

            if (highScoreText != null)
            {
                highScoreText.text = GameSession.IsNewHighScore
                    ? "NEW HIGH SCORE!"
                    : $"HIGH SCORE: {GameSession.HighScore}";
            }
        }

        public void PlayAgain() => SceneFader.LoadScene(SceneNames.Gameplay);

        public void MainMenu() => SceneFader.LoadScene(SceneNames.MainMenu);

        public void Quit() => QuitHelper.Quit();
    }
}
