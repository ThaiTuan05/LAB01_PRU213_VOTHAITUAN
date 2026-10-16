using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>The four kinds of collectible power-up.</summary>
    public enum PowerUpKind
    {
        /// <summary>Temporary shield: ignores damage while it lasts.</summary>
        Shield,

        /// <summary>Shorter firing cooldown for a while.</summary>
        RapidFire,

        /// <summary>Immediately grants one extra life.</summary>
        ExtraLife,

        /// <summary>Fires a three-way spread shot for a while.</summary>
        Spread,
    }

    /// <summary>
    /// A collectible dropped by destroyed asteroids and enemy ships. The single
    /// prefab is reused for every kind: <see cref="Spawn"/> picks the sprite, the
    /// color and the effect from <see cref="PowerUpKind"/>.
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Kinematic) + trigger CircleCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PowerUp : MonoBehaviour
    {
        [Header("Icons (one per kind)")]
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite rapidFireSprite;
        [SerializeField] private Sprite extraLifeSprite;
        [SerializeField] private Sprite spreadSprite;

        [Header("Colors")]
        [SerializeField] private Color shieldColor = new Color(0.45f, 0.9f, 1f);
        [SerializeField] private Color rapidFireColor = new Color(1f, 0.83f, 0.29f);
        [SerializeField] private Color extraLifeColor = new Color(0.5f, 1f, 0.6f);
        [SerializeField] private Color spreadColor = new Color(1f, 0.5f, 0.9f);

        [Header("Effect durations (seconds)")]
        [SerializeField] private float shieldDuration = 6f;
        [SerializeField] private float rapidFireDuration = 7f;
        [SerializeField] private float spreadDuration = 8f;
        [SerializeField] private int collectPoints = 5;

        [Header("Feel")]
        [Tooltip("Seconds before an uncollected power-up disappears.")]
        [SerializeField] private float lifetime = 12f;
        [SerializeField] private float spinSpeed = 70f;
        [SerializeField] private float pulseAmount = 0.12f;
        [SerializeField] private float pulseSpeed = 4f;

        private PowerUpKind kind;
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private float pulseOffset;
        private bool collected;

        /// <summary>Creates a power-up of <paramref name="kind"/> at <paramref name="position"/>.</summary>
        public static void Spawn(PowerUp prefab, Vector2 position, PowerUpKind kind)
        {
            if (prefab == null)
            {
                return;
            }

            PowerUp instance = Instantiate(prefab, position, Quaternion.identity);
            instance.kind = kind;
        }

        /// <summary>Random kind, used by the drop logic.</summary>
        public static PowerUpKind RandomKind()
        {
            return (PowerUpKind)Random.Range(0, System.Enum.GetValues(typeof(PowerUpKind)).Length);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Start()
        {
            baseScale = transform.localScale;
            pulseOffset = Random.Range(0f, Mathf.PI * 2f);
            ApplyVisual();
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + pulseOffset) * pulseAmount;
            transform.localScale = baseScale * pulse;
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            switch (kind)
            {
                case PowerUpKind.Shield:
                    spriteRenderer.sprite = shieldSprite;
                    spriteRenderer.color = shieldColor;
                    break;
                case PowerUpKind.RapidFire:
                    spriteRenderer.sprite = rapidFireSprite;
                    spriteRenderer.color = rapidFireColor;
                    break;
                case PowerUpKind.ExtraLife:
                    spriteRenderer.sprite = extraLifeSprite;
                    spriteRenderer.color = extraLifeColor;
                    break;
                default:
                    spriteRenderer.sprite = spreadSprite;
                    spriteRenderer.color = spreadColor;
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.TryGetComponent(out PlayerController player))
            {
                return;
            }

            collected = true;
            Apply(player);
            PopEffect.Spawn(spriteRenderer, spriteRenderer.color);
            SoundManager.Play(Sfx.PowerUp, 0.6f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(collectPoints);
            }

            Destroy(gameObject);
        }

        private void Apply(PlayerController player)
        {
            switch (kind)
            {
                case PowerUpKind.Shield:
                    player.ActivateShield(shieldDuration);
                    break;
                case PowerUpKind.RapidFire:
                    player.ActivateRapidFire(rapidFireDuration);
                    break;
                case PowerUpKind.ExtraLife:
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddLife();
                    }
                    break;
                default:
                    player.ActivateSpread(spreadDuration);
                    break;
            }
        }
    }
}
