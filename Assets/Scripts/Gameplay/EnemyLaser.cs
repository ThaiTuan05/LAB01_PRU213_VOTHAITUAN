using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// A red bolt fired by an enemy ship toward the player. It flies straight in the
    /// direction set at spawn, damages the ship on contact (handled by
    /// <see cref="PlayerController"/>) and removes itself after a short lifetime.
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Dynamic, gravity 0) + trigger BoxCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyLaser : MonoBehaviour
    {
        [SerializeField] private float speed = 6f;

        [Tooltip("Seconds before the bolt is destroyed automatically.")]
        [SerializeField] private float lifetime = 4f;

        private Vector2 direction = Vector2.down;

        /// <summary>Creates a bolt at <paramref name="position"/> moving along <paramref name="direction"/>.</summary>
        public static EnemyLaser Spawn(EnemyLaser prefab, Vector2 position, Vector2 direction)
        {
            if (prefab == null)
            {
                return null;
            }

            EnemyLaser bolt = Instantiate(prefab, position, Quaternion.identity);
            bolt.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            return bolt;
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
            // The damage itself is applied by the player; the bolt just disappears on contact.
            if (other.TryGetComponent(out PlayerController _))
            {
                Destroy(gameObject);
            }
        }
    }
}
