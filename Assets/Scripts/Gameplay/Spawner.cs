using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Instantiates asteroids, stars and enemy ships at random positions in the Gameplay scene.
    ///
    /// - Keeps the number of stars at <see cref="starCount"/>: a new one appears soon
    ///   after one is collected.
    /// - The number of asteroids starts at <see cref="startAsteroids"/> and slowly grows
    ///   to <see cref="maxAsteroids"/>, so the game gets harder over time.
    /// - After <see cref="enemyStartDelay"/> seconds enemies begin to appear, up to
    ///   <see cref="maxEnemies"/> at once.
    /// - Asteroids also speed up over time (see <see cref="Asteroid.SpeedMultiplier"/>).
    /// - Objects never spawn right on top of the player.
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private Asteroid asteroidPrefab;
        [SerializeField] private Star starPrefab;
        [SerializeField] private Enemy enemyPrefab;

        [Header("Amounts")]
        [SerializeField] private int startAsteroids = 4;
        [SerializeField] private int maxAsteroids = 12;

        [Tooltip("Seconds of play needed to allow one more asteroid.")]
        [SerializeField] private float secondsPerExtraAsteroid = 8f;

        [SerializeField] private int starCount = 6;

        [Tooltip("Seconds between two spawn checks.")]
        [SerializeField] private float spawnInterval = 0.75f;

        [Header("Enemies")]
        [Tooltip("Seconds of play before the first enemy ship appears.")]
        [SerializeField] private float enemyStartDelay = 15f;

        [SerializeField] private int startEnemies = 1;

        [SerializeField] private int maxEnemies = 3;

        [Tooltip("Seconds needed to allow one more enemy, once they start appearing.")]
        [SerializeField] private float secondsPerExtraEnemy = 20f;

        [Header("Difficulty ramp")]
        [Tooltip("Asteroids gain this fraction of extra speed per second of play.")]
        [SerializeField] private float asteroidSpeedPerSecond = 1f / 90f;

        [SerializeField] private float maxAsteroidSpeedMultiplier = 2.2f;

        [Header("Placement")]
        [Tooltip("Objects never spawn closer than this to the player.")]
        [SerializeField] private float safeRadius = 3f;

        [SerializeField] private float edgeMargin = 0.8f;
        [SerializeField] private Transform player;

        private float elapsed;
        private float nextSpawnTime;

        private void Start()
        {
            // Fill the scene immediately so the game does not start empty.
            for (int i = 0; i < startAsteroids; i++) SpawnAsteroid();
            for (int i = 0; i < starCount; i++) SpawnStar();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            elapsed += Time.deltaTime;

            // The field slowly gets faster.
            Asteroid.SpeedMultiplier = Mathf.Min(maxAsteroidSpeedMultiplier, 1f + elapsed * asteroidSpeedPerSecond);

            if (Time.time < nextSpawnTime)
            {
                return;
            }

            nextSpawnTime = Time.time + spawnInterval;

            // Difficulty curve: allowed asteroid count grows with time played.
            int allowedAsteroids = Mathf.Min(maxAsteroids, startAsteroids + (int)(elapsed / secondsPerExtraAsteroid));
            if (Asteroid.Active.Count < allowedAsteroids) SpawnAsteroid();
            if (Star.Active.Count < starCount) SpawnStar();

            // Enemies join after a grace period and become more numerous over time.
            if (enemyPrefab != null && elapsed >= enemyStartDelay)
            {
                int allowedEnemies = Mathf.Min(maxEnemies, startEnemies + (int)((elapsed - enemyStartDelay) / secondsPerExtraEnemy));
                if (Enemy.Active.Count < allowedEnemies) SpawnEnemy();
            }
        }

        private void SpawnAsteroid()
        {
            if (asteroidPrefab != null)
            {
                Instantiate(asteroidPrefab, FindSpawnPoint(), Quaternion.identity, transform);
            }
        }

        private void SpawnStar()
        {
            if (starPrefab != null)
            {
                Instantiate(starPrefab, FindSpawnPoint(), Quaternion.identity, transform);
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab != null)
            {
                Instantiate(enemyPrefab, FindSpawnPoint(), Quaternion.identity, transform);
            }
        }

        /// <summary>Random on-screen point that is at least <see cref="safeRadius"/> away from the player.</summary>
        private Vector2 FindSpawnPoint()
        {
            Vector2 point = ScreenBounds.RandomPoint(edgeMargin);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (player == null || !player.gameObject.activeInHierarchy ||
                    Vector2.Distance(point, player.position) >= safeRadius)
                {
                    break;
                }

                point = ScreenBounds.RandomPoint(edgeMargin);
            }

            return point;
        }
    }
}
