using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer
{
    /// <summary>
    /// Owns the rules of one play session in the Gameplay scene:
    /// score, lives, the combo meter, the HUD and the transition to the End Game scene.
    ///
    /// Scoring:
    ///   Collect a star      : +<see cref="Star.Points"/> x combo multiplier (10 x1..x5)
    ///   Shoot an asteroid   : + points based on its size (smaller pieces are worth more)
    ///   Destroy an enemy    : + its points
    ///   Pick up a power-up  : + a few points
    ///   Hit by an asteroid  : -<see cref="hitPenalty"/> and lose 1 life
    ///   Hit by an enemy     : -<see cref="hitPenalty"/> and lose 1 life
    /// Collecting stars in quick succession builds a combo multiplier (up to
    /// <see cref="maxComboMultiplier"/>x) that resets after <see cref="comboWindow"/> seconds.
    /// The game ends when lives reach 0.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>The GameManager of the current Gameplay scene (null in other scenes).</summary>
        public static GameManager Instance { get; private set; }

        [Header("Rules")]
        [SerializeField] private int startingLives = 3;

        [Tooltip("Lives are capped at the number of HUD icons.")]
        [SerializeField] private int maxLives = 5;

        [Tooltip("Points lost when the ship is hit.")]
        [SerializeField] private int hitPenalty = 5;

        [Tooltip("Seconds to wait after the explosion before going to the End Game scene.")]
        [SerializeField] private float endGameDelay = 1.2f;

        [Header("Combo (star chain)")]
        [Tooltip("Collecting a star within this many seconds of the previous one keeps the combo going.")]
        [SerializeField] private float comboWindow = 2.5f;

        [Tooltip("Highest combo multiplier reached.")]
        [SerializeField] private int maxComboMultiplier = 5;

        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Text scoreText;

        [Tooltip("One small ship icon per life; lost lives are dimmed.")]
        [SerializeField] private Image[] lifeIcons;

        [Tooltip("Shows the current combo multiplier (e.g. x3). Optional.")]
        [SerializeField] private Text comboText;

        [Tooltip("Shows the names + times of active power-ups. Optional.")]
        [SerializeField] private Text buffText;

        /// <summary>Current score (never below 0).</summary>
        public int Score { get; private set; }

        /// <summary>Remaining lives.</summary>
        public int Lives { get; private set; }

        /// <summary>Current combo multiplier (1 when no combo is active).</summary>
        public int ComboMultiplier { get; private set; } = 1;

        /// <summary>True once the ship has been destroyed; gameplay scripts stop reacting.</summary>
        public bool IsGameOver { get; private set; }

        /// <summary>The player ship, exposed so enemies and power-ups can find it.</summary>
        public PlayerController Player => player;

        private int comboCount;
        private float lastStarTime = float.NegativeInfinity;
        private readonly StringBuilder buffBuilder = new StringBuilder();

        private void Awake()
        {
            Instance = this;
            Lives = startingLives;
            Score = 0;
            ComboMultiplier = 1;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start() => RefreshHud();

        private void Update()
        {
            // Ease the score text back to normal size after a "punch" (see AddScore).
            if (scoreText != null)
            {
                Transform t = scoreText.transform;
                t.localScale = Vector3.Lerp(t.localScale, Vector3.one, Time.unscaledDeltaTime * 10f);
            }

            if (comboText != null)
            {
                comboText.transform.localScale = Vector3.Lerp(comboText.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 12f);
            }

            UpdateCombo();
            UpdateBuffText();
        }

        /// <summary>Adds (or, with a negative value, removes) points and updates the HUD.</summary>
        public void AddScore(int amount)
        {
            if (IsGameOver)
            {
                return;
            }

            Score = Mathf.Max(0, Score + amount);
            RefreshHud();

            if (amount > 0 && scoreText != null)
            {
                scoreText.transform.localScale = Vector3.one * 1.3f; // small "punch" feedback
            }
        }

        /// <summary>Called when the player collects a star: awards points multiplied by the combo.</summary>
        public void OnStarCollected(int basePoints)
        {
            if (IsGameOver)
            {
                return;
            }

            comboCount = Time.time - lastStarTime <= comboWindow ? comboCount + 1 : 1;
            lastStarTime = Time.time;
            ComboMultiplier = Mathf.Clamp(comboCount, 1, maxComboMultiplier);

            AddScore(basePoints * ComboMultiplier);
            UpdateComboText();

            if (comboText != null && ComboMultiplier > 1)
            {
                comboText.transform.localScale = Vector3.one * 1.4f;
            }
        }

        /// <summary>Bonus for destroying an asteroid with a laser (amount depends on its size).</summary>
        public void OnAsteroidShot(int points) => AddScore(points);

        /// <summary>Bonus for destroying an enemy ship.</summary>
        public void OnEnemyDestroyed(int points) => AddScore(points);

        /// <summary>Called by the player when it is hit by an asteroid, an enemy or an enemy bolt.</summary>
        public void OnPlayerHit()
        {
            if (IsGameOver)
            {
                return;
            }

            AddScore(-hitPenalty);
            Lives = Mathf.Max(0, Lives - 1);
            RefreshHud();

            if (Lives <= 0)
            {
                StartCoroutine(GameOverRoutine());
            }
        }

        /// <summary>Extra life from a power-up (capped at the number of HUD icons).</summary>
        public void AddLife()
        {
            if (IsGameOver)
            {
                return;
            }

            int cap = lifeIcons != null && lifeIcons.Length > 0 ? Mathf.Min(maxLives, lifeIcons.Length) : maxLives;
            Lives = Mathf.Min(cap, Lives + 1);
            RefreshHud();
        }

        private IEnumerator GameOverRoutine()
        {
            IsGameOver = true;
            if (comboText != null)
            {
                comboText.text = string.Empty;
            }

            if (buffText != null)
            {
                buffText.text = string.Empty;
            }

            if (player != null)
            {
                player.Die();
            }

            GameSession.SubmitScore(Score);
            yield return new WaitForSeconds(endGameDelay);
            SceneFader.LoadScene(SceneNames.EndGame);
        }

        /// <summary>Resets the combo once the player stops collecting stars.</summary>
        private void UpdateCombo()
        {
            if (comboCount > 0 && Time.time - lastStarTime > comboWindow)
            {
                comboCount = 0;
                ComboMultiplier = 1;
                UpdateComboText();
            }
        }

        private void UpdateComboText()
        {
            if (comboText != null)
            {
                comboText.text = ComboMultiplier > 1 ? $"x{ComboMultiplier}" : string.Empty;
            }
        }

        private void UpdateBuffText()
        {
            if (buffText == null || player == null)
            {
                return;
            }

            buffBuilder.Length = 0;
            if (player.ShieldRemaining > 0f) buffBuilder.Append($"SHIELD {player.ShieldRemaining:0.0}s");
            if (player.RapidFireRemaining > 0f)
            {
                if (buffBuilder.Length > 0) buffBuilder.Append("    ");
                buffBuilder.Append($"RAPID {player.RapidFireRemaining:0.0}s");
            }

            if (player.SpreadRemaining > 0f)
            {
                if (buffBuilder.Length > 0) buffBuilder.Append("    ");
                buffBuilder.Append($"SPREAD {player.SpreadRemaining:0.0}s");
            }

            buffText.text = buffBuilder.ToString();
        }

        private void RefreshHud()
        {
            if (scoreText != null)
            {
                scoreText.text = Score.ToString();
            }

            if (lifeIcons == null)
            {
                return;
            }

            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].color = i < Lives ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                }
            }
        }
    }
}
