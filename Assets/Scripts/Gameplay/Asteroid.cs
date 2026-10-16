using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// A floating asteroid. Each asteroid gets a random size, speed, direction and
    /// spin when it spawns, changes direction randomly every few seconds and bounces
    /// off the screen edges, so the field always looks alive.
    ///
    /// Shooting an asteroid (<see cref="Shatter"/>) breaks it into two smaller pieces
    /// (up to <see cref="maxSplits"/> times) and can drop a <see cref="PowerUp"/>.
    /// Ramming the spaceship (<see cref="Explode"/>) just destroys it and costs a life
    /// (handled by <see cref="PlayerController"/> and <see cref="GameManager"/>).
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Kinematic) + trigger CircleCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Asteroid : MonoBehaviour
    {
        /// <summary>All asteroids currently alive. The spawner uses this to keep the count up.</summary>
        public static readonly List<Asteroid> Active = new List<Asteroid>();

        /// <summary>
        /// Global speed factor applied to newly spawned asteroids. The spawner raises
        /// it over time so the field slowly becomes more dangerous.
        /// </summary>
        public static float SpeedMultiplier = 1f;

        [SerializeField] private Vector2 speedRange = new Vector2(1f, 3f);

        [Tooltip("Random multiplier applied to the prefab's own scale (min, max).")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.6f, 1.4f);
        [SerializeField] private Vector2 spinRange = new Vector2(-90f, 90f);

        [Tooltip("Seconds between random direction changes (min, max).")]
        [SerializeField] private Vector2 turnIntervalRange = new Vector2(2f, 4f);

        [Tooltip("Max angle (degrees) of a random direction change.")]
        [SerializeField] private float maxTurnAngle = 60f;

        [Tooltip("Color used for the explosion pop effect.")]
        [SerializeField] private Color explosionColor = new Color(1f, 0.6f, 0.3f);

        [Header("Splitting")]
        [Tooltip("How many times a shot asteroid can split before it is fully destroyed.")]
        [SerializeField] private int maxSplits = 2;

        [Tooltip("Size of each child compared to its parent.")]
        [SerializeField] private float childScale = 0.6f;

        [Tooltip("Angle (degrees) between the two children's directions of travel.")]
        [SerializeField] private float splitAngle = 45f;

        [Tooltip("Children move this much faster than the parent did.")]
        [SerializeField] private float childSpeedBoost = 1.25f;

        [Header("Scoring and drops")]
        [Tooltip("Points for shooting an asteroid; smaller pieces are worth more.")]
        [SerializeField] private int shotPoints = 2;

        [Tooltip("Extra points per split level.")]
        [SerializeField] private int pointsPerSplit = 2;

        [SerializeField] private PowerUp powerUpPrefab;

        [Tooltip("Chance (0..1) that shooting this asteroid drops a power-up.")]
        [SerializeField] [Range(0f, 1f)] private float powerUpChance = 0.18f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private float nextTurnTime;
        private bool exploded;

        // Set when this asteroid was created as a split child of another asteroid.
        private int splitLevel;
        private bool sizedByParent;
        private float parentScale;
        private Vector2 inheritedVelocity;

        /// <summary>Points awarded when this asteroid is shot down.</summary>
        public int ShotPoints => shotPoints + splitLevel * pointsPerSplit;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        private void Start()
        {
            if (sizedByParent)
            {
                // A split child: use the size and velocity its parent gave it.
                transform.localScale = Vector3.one * parentScale;
                body.linearVelocity = inheritedVelocity;
            }
            else
            {
                // Random size, direction and speed -> every asteroid feels different.
                transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);
                body.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(speedRange.x, speedRange.y) * SpeedMultiplier;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            body.angularVelocity = Random.Range(spinRange.x, spinRange.y);
            ScheduleNextTurn();
        }

        /// <summary>Configures a freshly instantiated child asteroid. Called before its Start runs.</summary>
        public void ConfigureAsChild(int level, float scale, Vector2 velocity)
        {
            splitLevel = level;
            sizedByParent = true;
            parentScale = scale;
            inheritedVelocity = velocity;
        }

        private void FixedUpdate()
        {
            if (Time.time >= nextTurnTime)
            {
                // Rotate the velocity vector by a random angle: "random movement".
                float angle = Random.Range(-maxTurnAngle, maxTurnAngle);
                body.linearVelocity = Quaternion.Euler(0f, 0f, angle) * body.linearVelocity;
                ScheduleNextTurn();
            }

            BounceOffEdges();
        }

        private void ScheduleNextTurn()
        {
            nextTurnTime = Time.time + Random.Range(turnIntervalRange.x, turnIntervalRange.y);
        }

        /// <summary>Reverses the velocity on an axis when the asteroid reaches that screen edge.</summary>
        private void BounceOffEdges()
        {
            Vector2 half = spriteRenderer.bounds.extents;
            Rect area = ScreenBounds.Get();
            Vector2 pos = body.position;
            Vector2 vel = body.linearVelocity;

            if ((pos.x - half.x < area.xMin && vel.x < 0f) || (pos.x + half.x > area.xMax && vel.x > 0f)) vel.x = -vel.x;
            if ((pos.y - half.y < area.yMin && vel.y < 0f) || (pos.y + half.y > area.yMax && vel.y > 0f)) vel.y = -vel.y;

            body.linearVelocity = vel;
        }

        /// <summary>
        /// Called by a player laser: breaks the asteroid into two smaller pieces when it
        /// still can, possibly drops a power-up, then destroys it.
        /// </summary>
        public void Shatter()
        {
            if (exploded)
            {
                return;
            }

            if (splitLevel < maxSplits && childScale > 0f)
            {
                SpawnChildren();
            }

            TryDropPowerUp();
            Explode();
        }

        private void SpawnChildren()
        {
            Vector2 velocity = body.linearVelocity;
            Vector2 baseDirection = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Random.insideUnitCircle.normalized;
            float childSpeed = Mathf.Max(velocity.magnitude, speedRange.x) * childSpeedBoost;
            float childSize = transform.localScale.x * childScale;

            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? 1f : -1f;
                Vector2 direction = Quaternion.Euler(0f, 0f, sign * splitAngle * 0.5f) * baseDirection;

                GameObject clone = Instantiate(gameObject, transform.position, Quaternion.identity, transform.parent);
                if (clone.TryGetComponent(out Asteroid child))
                {
                    child.ConfigureAsChild(splitLevel + 1, childSize, direction * childSpeed);
                }
            }
        }

        private void TryDropPowerUp()
        {
            if (powerUpPrefab == null || Random.value > powerUpChance)
            {
                return;
            }

            PowerUp.Spawn(powerUpPrefab, transform.position, PowerUp.RandomKind());
        }

        /// <summary>Destroys the asteroid with a small explosion effect. Also used when it rams the ship.</summary>
        public void Explode()
        {
            if (exploded)
            {
                return;
            }

            exploded = true;
            PopEffect.Spawn(spriteRenderer, explosionColor);
            SoundManager.Play(Sfx.Explosion, 0.55f);
            Destroy(gameObject);
        }
    }
}
