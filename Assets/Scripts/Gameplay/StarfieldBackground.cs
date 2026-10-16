using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Decorative scrolling starfield drawn behind everything. It creates many small
    /// dots on three depth layers; far dots are dim and slow, near dots are bright
    /// and fast (parallax), which gives the feeling of flying through space.
    /// Dots that leave the bottom of the screen reappear at the top.
    /// Purely visual: no colliders, no gameplay effect.
    /// </summary>
    public class StarfieldBackground : MonoBehaviour
    {
        [SerializeField] private Sprite dotSprite;
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private int dotsPerLayer = 40;
        [SerializeField] private float baseScrollSpeed = 0.5f;

        private Transform[] dots;
        private float[] speeds;

        private void Start()
        {
            if (dotSprite == null)
            {
                return;
            }

            const int layers = 3;
            dots = new Transform[dotsPerLayer * layers];
            speeds = new float[dots.Length];
            Rect area = ScreenBounds.Get();

            for (int i = 0; i < dots.Length; i++)
            {
                int layer = i / dotsPerLayer;          // 0 = far, 2 = near
                float depth = (layer + 1f) / layers;   // 0.33 .. 1

                var go = new GameObject("Dot");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax), 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.8f, depth);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
                sr.sortingOrder = -100 + layer;
                sr.color = new Color(0.8f, 0.9f, 1f, Mathf.Lerp(0.25f, 0.9f, depth));

                dots[i] = go.transform;
                speeds[i] = baseScrollSpeed * depth * 2f;
            }
        }

        private void Update()
        {
            if (dots == null)
            {
                return;
            }

            Rect area = ScreenBounds.Get();
            for (int i = 0; i < dots.Length; i++)
            {
                Vector3 p = dots[i].position;
                p.y -= speeds[i] * Time.deltaTime;
                if (p.y < area.yMin)
                {
                    p.y = area.yMax;
                    p.x = Random.Range(area.xMin, area.xMax);
                }

                dots[i].position = p;
            }
        }
    }
}
