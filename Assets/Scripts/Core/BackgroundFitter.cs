using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Scales a background sprite so it always covers the whole camera view,
    /// whatever the screen aspect ratio is (16:9, 16:10, 4:3...). The image keeps
    /// its proportions; any overflow is simply cropped by the camera.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundFitter : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lastAspect;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void Start() => Fit();

        private void Update()
        {
            // Re-fit when the window / Game view is resized.
            if (Camera.main != null && !Mathf.Approximately(Camera.main.aspect, lastAspect))
            {
                Fit();
            }
        }

        private void Fit()
        {
            if (spriteRenderer.sprite == null || Camera.main == null)
            {
                return;
            }

            lastAspect = Camera.main.aspect;
            Rect view = ScreenBounds.Get();
            Vector2 size = spriteRenderer.sprite.bounds.size;
            float scale = Mathf.Max(view.width / size.x, view.height / size.y);
            transform.position = new Vector3(view.center.x, view.center.y, 0f);
            transform.localScale = Vector3.one * scale;
        }
    }
}
