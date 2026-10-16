using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceExplorer
{
    /// <summary>
    /// The spaceship controlled by the player.
    ///
    /// Controls (Input System):
    ///   Arrow keys / WASD : move in all directions
    ///   Space / Left Ctrl : shoot a laser
    ///
    /// The ship turns to face the way it moves and fires in that direction (twin-stick
    /// style); standing still keeps the last aim, which lets the player strafe-shoot.
    ///
    /// Components required on the same GameObject:
    ///   SpriteRenderer  - draws the ship
    ///   Rigidbody2D     - Dynamic, gravity 0; movement is done by setting its velocity
    ///   Collider2D      - solid collider; asteroids, stars, enemies and bolts are triggers that hit it
    ///
    /// Collision rules (see OnTriggerEnter2D):
    ///   Star        -> collected, points added (with combo)
    ///   Asteroid    -> destroyed, one life lost (unless shielded/invulnerable)
    ///   Enemy       -> destroyed, one life lost
    ///   EnemyLaser  -> one life lost
    ///
    /// Power-ups (see PowerUp) grant temporary shield, rapid fire or spread shot, or an extra life.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Top speed in world units per second.")]
        [SerializeField] private float moveSpeed = 7f;

        [Tooltip("How quickly the ship reaches its target speed (higher = snappier).")]
        [SerializeField] private float acceleration = 12f;

        [Tooltip("Turn the ship to face the way it aims (twin-stick style).")]
        [SerializeField] private bool faceAimDirection = true;

        [Tooltip("Degrees per second the ship can rotate toward its aim.")]
        [SerializeField] private float turnSpeed = 900f;

        [Tooltip("Wrap around the screen edges instead of stopping at them.")]
        [SerializeField] private bool wrapScreen = true;

        [Header("Shooting")]
        [SerializeField] private Laser laserPrefab;

        [Tooltip("Distance in front of the ship where lasers appear.")]
        [SerializeField] private float muzzleDistance = 0.75f;

        [Tooltip("Minimum seconds between two shots.")]
        [SerializeField] private float fireCooldown = 0.22f;

        [Tooltip("Number of lasers fired at once while spread shot is active.")]
        [SerializeField] private int spreadShotCount = 3;

        [Tooltip("Angle (degrees) between the lasers of a spread shot.")]
        [SerializeField] private float spreadAngle = 14f;

        [Tooltip("Cooldown multiplier while rapid fire is active.")]
        [SerializeField] private float rapidFireFactor = 0.45f;

        [Header("Damage")]
        [Tooltip("Seconds the ship cannot be hit again after taking damage.")]
        [SerializeField] private float invulnerabilityTime = 1.5f;

        [Tooltip("Seconds of protection when the game starts, so the player can get ready.")]
        [SerializeField] private float spawnInvulnerabilityTime = 2f;

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private Vector2 aimDirection = Vector2.up;
        private float facingAngle;
        private float nextFireTime;
        private bool isInvulnerable;

        // Active power-up timers (seconds remaining).
        private float shieldRemaining;
        private float rapidFireRemaining;
        private float spreadRemaining;

        /// <summary>Seconds of shield left (0 when not shielded).</summary>
        public float ShieldRemaining => shieldRemaining;

        /// <summary>Seconds of rapid fire left (0 when inactive).</summary>
        public float RapidFireRemaining => rapidFireRemaining;

        /// <summary>Seconds of spread shot left (0 when inactive).</summary>
        public float SpreadRemaining => spreadRemaining;

        /// <summary>True while the shield power-up is active.</summary>
        public bool IsShielded => shieldRemaining > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Start() => StartCoroutine(InvulnerabilityRoutine(spawnInvulnerabilityTime));

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                return; // paused: ignore movement and shooting keys
            }

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                moveInput = Vector2.zero;
                return;
            }

            TickPowerUps();
            ReadInput();
            FaceAimDirection();
            UpdateShieldVisual();
        }

        private void FixedUpdate()
        {
            // Smoothly accelerate toward the desired velocity.
            Vector2 targetVelocity = moveInput * moveSpeed;
            body.linearVelocity = Vector2.Lerp(body.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            KeepInsideScreen();
        }

        /// <summary>Counts the active power-up timers down.</summary>
        private void TickPowerUps()
        {
            if (shieldRemaining > 0f) shieldRemaining -= Time.deltaTime;
            if (rapidFireRemaining > 0f) rapidFireRemaining -= Time.deltaTime;
            if (spreadRemaining > 0f) spreadRemaining -= Time.deltaTime;
        }

        /// <summary>Reads keyboard state into <see cref="moveInput"/> and fires when the shoot key is held.</summary>
        private void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float x = 0f, y = 0f;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) x += 1f;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) y += 1f;

            // Normalize so moving diagonally is not faster than moving straight.
            moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

            // Moving sets the aim; standing still keeps the previous aim.
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                aimDirection = moveInput.normalized;
            }

            if ((keyboard.spaceKey.isPressed || keyboard.leftCtrlKey.isPressed) && Time.time >= nextFireTime)
            {
                Shoot();
            }
        }

        /// <summary>Rotates the ship so its nose points along <see cref="aimDirection"/>.</summary>
        private void FaceAimDirection()
        {
            if (!faceAimDirection)
            {
                return;
            }

            float target = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
            facingAngle = Mathf.MoveTowardsAngle(facingAngle, target, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, facingAngle);
        }

        /// <summary>Spawns one laser (or a spread of them) at the muzzle, flying along the aim.</summary>
        private void Shoot()
        {
            if (laserPrefab == null)
            {
                return;
            }

            float cooldown = rapidFireRemaining > 0f ? fireCooldown * rapidFireFactor : fireCooldown;
            nextFireTime = Time.time + cooldown;

            Vector2 origin = (Vector2)transform.position + aimDirection * muzzleDistance;
            int count = spreadRemaining > 0f ? Mathf.Max(1, spreadShotCount) : 1;
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * spreadAngle;
                Vector2 direction = Quaternion.Euler(0f, 0f, offset) * aimDirection;
                Laser.Spawn(laserPrefab, origin, direction);
            }

            SoundManager.Play(Sfx.Laser, 0.5f);
        }

        /// <summary>Clamps or wraps the ship's position to keep it on screen.</summary>
        private void KeepInsideScreen()
        {
            Vector2 halfSize = spriteRenderer.bounds.extents;
            Rect area = ScreenBounds.Get();
            Vector2 pos = body.position;

            if (wrapScreen)
            {
                if (pos.x < area.xMin - halfSize.x) pos.x = area.xMax + halfSize.x;
                else if (pos.x > area.xMax + halfSize.x) pos.x = area.xMin - halfSize.x;
                if (pos.y < area.yMin - halfSize.y) pos.y = area.yMax + halfSize.y;
                else if (pos.y > area.yMax + halfSize.y) pos.y = area.yMin - halfSize.y;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, area.xMin + halfSize.x, area.xMax - halfSize.x);
                pos.y = Mathf.Clamp(pos.y, area.yMin + halfSize.y, area.yMax - halfSize.y);
            }

            body.position = pos;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
            {
                return;
            }

            if (other.TryGetComponent(out Star star))
            {
                GameManager.Instance.OnStarCollected(star.Points);
                star.Collect();
            }
            else if (other.TryGetComponent(out Asteroid asteroid))
            {
                asteroid.Explode();
                TakeDamage();
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                enemy.Explode();
                TakeDamage();
            }
            else if (other.TryGetComponent(out EnemyLaser _))
            {
                TakeDamage();
            }
        }

        /// <summary>Applies one hit: the shield blocks it, otherwise a life is lost (with a short invulnerability).</summary>
        private void TakeDamage()
        {
            if (IsShielded)
            {
                SoundManager.Play(Sfx.Shield, 0.6f);
                return;
            }

            if (isInvulnerable)
            {
                return;
            }

            SoundManager.Play(Sfx.Hit);
            GameManager.Instance.OnPlayerHit();

            if (!GameManager.Instance.IsGameOver)
            {
                StartCoroutine(InvulnerabilityRoutine(invulnerabilityTime));
            }
        }

        /// <summary>Makes the ship blink and ignore damage for <paramref name="duration"/> seconds.</summary>
        private IEnumerator InvulnerabilityRoutine(float duration)
        {
            isInvulnerable = true;
            for (float t = 0f; t < duration; t += 0.1f)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.1f);
            }

            spriteRenderer.enabled = true;
            isInvulnerable = false;
        }

        /// <summary>Tints the ship cyan while the shield is active.</summary>
        private void UpdateShieldVisual()
        {
            if (IsShielded)
            {
                float pulse = 0.75f + Mathf.Sin(Time.time * 10f) * 0.15f;
                spriteRenderer.color = new Color(0.5f * pulse, 0.95f * pulse, 1f);
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }

        /// <summary>Power-up: temporary shield.</summary>
        public void ActivateShield(float duration) => shieldRemaining = Mathf.Max(shieldRemaining, duration);

        /// <summary>Power-up: faster shooting.</summary>
        public void ActivateRapidFire(float duration) => rapidFireRemaining = Mathf.Max(rapidFireRemaining, duration);

        /// <summary>Power-up: three-way spread shot.</summary>
        public void ActivateSpread(float duration) => spreadRemaining = Mathf.Max(spreadRemaining, duration);

        /// <summary>Called by the GameManager when the game ends: plays an explosion and hides the ship.</summary>
        public void Die()
        {
            StopAllCoroutines();
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            PopEffect.Spawn(spriteRenderer, new Color(1f, 0.5f, 0.2f));
            body.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }
}
