using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// A collectible star. It drifts slowly, spins and "twinkles" (pulses in size).
    /// When the spaceship touches it, the player gains <see cref="Points"/>.
    ///
    /// Prefab setup: SpriteRenderer + Rigidbody2D (Kinematic) + trigger CircleCollider2D.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Star : MonoBehaviour
    {
        /// <summary>All stars currently alive. The spawner uses this to keep the count up.</summary>
        public static readonly List<Star> Active = new List<Star>();

        [Tooltip("Points awarded when this star is collected.")]
        [SerializeField] private int points = 10;

        [SerializeField] private float driftSpeed = 0.4f;
        [SerializeField] private float spinSpeed = 45f;

        [Tooltip("Size change amount of the twinkle animation (0.15 = +/-15%).")]
        [SerializeField] private float twinkleAmount = 0.15f;
        [SerializeField] private float twinkleSpeed = 4f;

        [SerializeField] private Color collectColor = new Color(1f, 0.95f, 0.5f);

        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private float twinkleOffset;
        private bool collected;

        /// <summary>Points this star is worth.</summary>
        public int Points => points;

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
            baseScale = transform.localScale;
            twinkleOffset = Random.Range(0f, Mathf.PI * 2f); // so stars do not pulse in sync
            body.linearVelocity = Random.insideUnitCircle.normalized * driftSpeed;
            body.angularVelocity = Random.value < 0.5f ? spinSpeed : -spinSpeed;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * twinkleSpeed + twinkleOffset) * twinkleAmount;
            transform.localScale = baseScale * pulse;

            // Drifting stars wrap back in instead of disappearing off screen.
            Rect area = ScreenBounds.Get();
            Vector2 pos = body.position;
            if (!area.Contains(pos))
            {
                body.position = new Vector2(
                    Mathf.Repeat(pos.x - area.xMin, area.width) + area.xMin,
                    Mathf.Repeat(pos.y - area.yMin, area.height) + area.yMin);
            }
        }

        /// <summary>Removes the star with a sparkle effect. Called by the player on contact.</summary>
        public void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;
            PopEffect.Spawn(spriteRenderer, collectColor);
            SoundManager.Play(Sfx.Pickup, 0.6f);
            Destroy(gameObject);
        }
    }
}
