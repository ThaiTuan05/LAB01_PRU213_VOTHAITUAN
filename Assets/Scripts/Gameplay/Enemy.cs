using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// A hostile ship. It drifts toward the player, stops at a comfortable firing
    /// distance and shoots bolts at the ship. Player lasers damage it; it takes a
    /// couple of hits before it explodes into points (and sometimes a power-up).
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Kinematic) + trigger Collider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>All enemies currently alive. The spawner uses this to keep the count up.</summary>
        public static readonly List<Enemy> Active = new List<Enemy>();

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("How quickly the ship turns toward its target (higher = snappier).")]
        [SerializeField] private float steer = 3f;

        [Tooltip("Distance at which the enemy stops closing in and starts firing.")]
        [SerializeField] private float stopDistance = 2.4f;

        [Tooltip("Turn the ship to face the player, so it fires along its nose.")]
        [SerializeField] private bool faceTarget = true;

        [Tooltip("Degrees per second the ship rotates toward the player.")]
        [SerializeField] private float turnSpeed = 260f;

        [Header("Combat")]
        [SerializeField] private int health = 2;
        [SerializeField] private Vector2 fireIntervalRange = new Vector2(1.6f, 2.8f);

        [Tooltip("Random aim error (degrees) so shots are dodgeable.")]
        [SerializeField] private float aimInaccuracy = 9f;

        [Tooltip("Distance in front of the ship (along its nose) where bolts appear.")]
        [SerializeField] private float muzzleDistance = 0.45f;

        [SerializeField] private EnemyLaser laserPrefab;
        [SerializeField] private PowerUp powerUpPrefab;
        [SerializeField] [Range(0f, 1f)] private float powerUpChance = 0.4f;
        [SerializeField] private int points = 25;

        [Header("Feel")]
        [SerializeField] private Color normalColor = new Color(1f, 0.55f, 0.55f);
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashTime = 0.08f;
        [SerializeField] private float knockback = 0.3f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private float nextFireTime;
        private float facingAngle;
        private bool exploded;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            body.bodyType = RigidbodyType2D.Kinematic;
            spriteRenderer.color = normalColor;
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        private void Start() => nextFireTime = Time.time + Random.Range(fireIntervalRange.x, fireIntervalRange.y);

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            if (Target() != null && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + Random.Range(fireIntervalRange.x, fireIntervalRange.y);
            }
        }

        private void FixedUpdate()
        {
            Transform target = Target();
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - body.position;
                Vector2 desired = toTarget.magnitude > stopDistance ? toTarget.normalized * moveSpeed : Vector2.zero;
                body.linearVelocity = Vector2.Lerp(body.linearVelocity, desired, steer * Time.fixedDeltaTime);
            }
            else
            {
                body.linearVelocity = Vector2.Lerp(body.linearVelocity, Vector2.zero, steer * Time.fixedDeltaTime);
            }

            FaceTarget(target);
            KeepInsideScreen();
        }

        /// <summary>Rotates the ship so its nose points at the player (or its last aim if there is none).</summary>
        private void FaceTarget(Transform target)
        {
            if (!faceTarget || target == null)
            {
                return;
            }

            Vector2 toTarget = (Vector2)target.position - body.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float desired = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            facingAngle = Mathf.MoveTowardsAngle(facingAngle, desired, turnSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, facingAngle);
        }

        private Transform Target()
        {
            return GameManager.Instance != null && GameManager.Instance.Player != null
                ? GameManager.Instance.Player.transform
                : null;
        }

        private void Shoot()
        {
            Transform target = Target();
            if (laserPrefab == null || target == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)target.position - body.position).normalized;
            direction = Quaternion.Euler(0f, 0f, Random.Range(-aimInaccuracy, aimInaccuracy)) * direction;
            Vector2 origin = (Vector2)transform.position + direction * muzzleDistance;
            EnemyLaser.Spawn(laserPrefab, origin, direction);
            SoundManager.Play(Sfx.EnemyShoot, 0.4f);
        }

        private void KeepInsideScreen()
        {
            Vector2 half = spriteRenderer.bounds.extents;
            Rect area = ScreenBounds.Get();
            Vector2 pos = body.position;
            pos.x = Mathf.Clamp(pos.x, area.xMin + half.x, area.xMax - half.x);
            pos.y = Mathf.Clamp(pos.y, area.yMin + half.y, area.yMax - half.y);
            body.position = pos;
        }

        /// <summary>Called by a player laser. Loses one hit point; explodes at zero.</summary>
        public void TakeHit()
        {
            if (exploded)
            {
                return;
            }

            health--;
            if (health <= 0)
            {
                Explode();
                return;
            }

            // Small knockback away from the player + a quick flash so hits read clearly.
            Transform target = Target();
            if (target != null)
            {
                Vector2 away = (body.position - (Vector2)target.position).normalized;
                body.position += away * knockback;
            }

            StartCoroutine(FlashRoutine());
            SoundManager.Play(Sfx.Hit, 0.35f);
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashTime);
            spriteRenderer.color = normalColor;
        }

        /// <summary>Destroys the enemy: points, an effect and sometimes a power-up. Also used when it rams the ship.</summary>
        public void Explode()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            StopAllCoroutines();
            PopEffect.Spawn(spriteRenderer, new Color(1f, 0.4f, 0.4f));
            SoundManager.Play(Sfx.Explosion, 0.7f);

            if (powerUpPrefab != null && Random.value < powerUpChance)
            {
                PowerUp.Spawn(powerUpPrefab, transform.position, PowerUp.RandomKind());
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyDestroyed(points);
            }

            Destroy(gameObject);
        }
    }
}
