using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Tiny visual feedback effect: a copy of a sprite that grows and fades out,
    /// then destroys itself. Used when an asteroid explodes or a star is collected.
    /// It is created from code, so it needs no prefab.
    /// </summary>
    public class PopEffect : MonoBehaviour
    {
        private const float Duration = 0.35f;

        private SpriteRenderer spriteRenderer;
        private Color startColor;
        private Vector3 startScale;
        private float elapsed;

        /// <summary>Spawns a pop effect that looks like <paramref name="source"/>.</summary>
        public static void Spawn(SpriteRenderer source, Color tint)
        {
            if (source == null || source.sprite == null)
            {
                return;
            }

            var go = new GameObject("PopEffect");
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            go.transform.localScale = source.transform.lossyScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sharedMaterial = source.sharedMaterial;
            sr.sortingOrder = source.sortingOrder + 1;
            sr.color = tint;

            var effect = go.AddComponent<PopEffect>();
            effect.spriteRenderer = sr;
            effect.startColor = tint;
            effect.startScale = go.transform.localScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;

            transform.localScale = startScale * (1f + t); // grow to 2x
            var c = startColor;
            c.a = startColor.a * (1f - t);                 // fade to transparent
            spriteRenderer.color = c;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
