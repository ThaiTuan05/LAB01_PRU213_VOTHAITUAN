using UnityEngine;

namespace SpaceExplorer
{
    /// <summary>
    /// Helper that converts the orthographic camera's view into world-space bounds.
    /// Used to keep the spaceship on screen, bounce asteroids off the edges and
    /// pick random spawn positions.
    /// </summary>
    public static class ScreenBounds
    {
        /// <summary>World-space rectangle currently visible through the main camera.</summary>
        public static Rect Get()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return new Rect(-8.9f, -5f, 17.8f, 10f); // sensible 16:9 fallback
            }

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 center = cam.transform.position;
            return new Rect(center.x - width / 2f, center.y - height / 2f, width, height);
        }

        /// <summary>Visible rectangle shrunk by <paramref name="margin"/> on every side.</summary>
        public static Rect GetInset(float margin)
        {
            Rect r = Get();
            return new Rect(r.xMin + margin, r.yMin + margin, r.width - margin * 2f, r.height - margin * 2f);
        }

        /// <summary>Random point inside the visible area, keeping <paramref name="margin"/> from the edges.</summary>
        public static Vector2 RandomPoint(float margin)
        {
            Rect r = GetInset(margin);
            return new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
        }
    }
}
