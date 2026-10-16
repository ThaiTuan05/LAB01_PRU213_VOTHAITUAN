using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// A laser bolt fired by the spaceship. It flies in the direction it was launched,
    /// breaks the first asteroid or enemy it touches (awarding points) and removes
    /// itself after a short lifetime so off-screen lasers do not pile up.
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Dynamic, gravity 0) + trigger Collider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Laser : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;

        [Tooltip("Seconds before the laser is destroyed automatically.")]
        [SerializeField] private float lifetime = 1.5f;

        private Vector2 direction = Vector2.up;

        /// <summary>Creates a laser at <paramref name="position"/> flying along <paramref name="direction"/>.</summary>
        public static Laser Spawn(Laser prefab, Vector2 position, Vector2 direction)
        {
            if (prefab == null)
            {
                return null;
            }

            Laser laser = Instantiate(prefab, position, Quaternion.identity);
            laser.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            return laser;
        }

        private void Start()
        {
            var body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearVelocity = direction * speed;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Asteroid asteroid))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnAsteroidShot(asteroid.ShotPoints);
                }

                asteroid.Shatter();
                Destroy(gameObject);
                return;
            }

            if (other.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeHit();
                Destroy(gameObject);
            }

            // Lasers pass through stars, the player and other lasers.
        }
    }
}
