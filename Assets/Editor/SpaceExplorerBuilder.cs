using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceExplorer.EditorTools
{
    /// <summary>
    /// One-click project setup: menu "Space Explorer > Build Project".
    ///
    /// Builds the three scenes following the UI/UX mockups in DesignPreviews/ (Menu.png, Gameplay.png),
    /// using the artwork in Assets/SpaceExplorer, and generates:
    ///   Assets/Art/Generated/*.png    - UI shapes (rounded panels/buttons, key caps, icons) + starfield dot
    ///   Assets/Art/SpriteUnlit.mat    - unlit sprite material (no 2D lights needed)
    ///   Assets/Prefabs/*.prefab       - Laser, Asteroid, Star
    ///   Assets/Scenes/MainMenu, Gameplay, EndGame .unity
    ///   Build Profile scene list      - MainMenu (0), Gameplay (1), EndGame (2)
    ///
    /// All UI positions use a 1920x1080 reference canvas. Running the builder again
    /// regenerates the scenes and prefabs (manual edits in them are lost).
    /// </summary>
    public static class SpaceExplorerBuilder
    {
        private const string ArtRoot = "Assets/SpaceExplorer";
        private const string GeneratedDir = "Assets/Art/Generated";
        private const string PrefabDir = "Assets/Prefabs";
        private const string SceneDir = "Assets/Scenes";
        private const string MaterialPath = "Assets/Art/SpriteUnlit.mat";

        // World scales for the 256 PPU artwork with an orthographic size of 5 (see Assets/SpaceExplorer/README.md).
        private const float ShipScale = 0.3f;      // 1254 px -> ~1.5 units
        private const float AsteroidScale = 0.25f; // x random 0.6..1.4 -> 0.15..0.35
        private const float StarScale = 1.2f;      // 128 px -> ~0.6 units
        private const float LaserScale = 1f;       // 32x128 px -> 0.125x0.5 units
        private const float PowerUpScale = 0.8f;   // 64x64 px -> ~0.5 units

        // ---- Palette (from the mockups)
        private static readonly Color SpaceColor = new Color(0.02f, 0.02f, 0.08f);
        private static readonly Color Cyan = new Color(0.33f, 0.9f, 1f);
        private static readonly Color CyanBright = new Color(0.62f, 0.96f, 1f);
        private static readonly Color Gold = new Color(1f, 0.83f, 0.29f);
        private static readonly Color TextSoft = new Color(0.82f, 0.88f, 0.93f);
        private static readonly Color TextMuted = new Color(0.6f, 0.66f, 0.72f);
        private static readonly Color PanelFill = new Color(0.02f, 0.05f, 0.09f, 1f); // opaque: the project uses Linear color space, so partial alpha looks much lighter than its value
        private static readonly Color HudFill = new Color(0.02f, 0.05f, 0.09f, 0.6f);
        private static readonly Color Dim = new Color(0f, 0.01f, 0.04f, 0.94f);

        // ---- Anchors / pivots
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);

        private enum ButtonStyle { Primary, Secondary, Subdued }

        private static Font font;
        private static Material spriteMaterial;
        private static Sprite shipSprite, asteroidSprite, starSprite, laserSprite, background, dot;
        private static Sprite roundFill, roundOutline, softGlow, triangle, speakerOn, speakerOff, pauseIcon;
        private static Sprite iconShield, iconBolt, iconHeart, iconSpread;

        [MenuItem("Space Explorer/Build Project")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(GeneratedDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(SceneDir);

            // Make sure the artwork went through SpaceExplorerAssetImporter (sprite type, PPU, borders).
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            spriteMaterial = CreateSpriteMaterial();
            GenerateUiSprites();
            LoadSprites();

            CreateLaserPrefab();
            CreatePowerUpPrefab();
            CreateAsteroidPrefab();
            CreateStarPrefab();
            CreateEnemyLaserPrefab();
            CreateEnemyPrefab();

            string menuPath = BuildMainMenuScene();
            string endPath = BuildEndGameScene();
            string gamePath = BuildGameplayScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(gamePath, true),
                new EditorBuildSettingsScene(endPath, true),
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(menuPath);
            Debug.Log("[Space Explorer] Project built. Open MainMenu and press Play.");
        }

        /// <summary>
        /// Sprites are (re)loaded after every NewScene call because creating a scene unloads
        /// unused assets, which would turn previously held references into null.
        /// </summary>
        private static void LoadSprites()
        {
            shipSprite = LoadSprite($"{ArtRoot}/Sprites/Spaceship.png");
            asteroidSprite = LoadSprite($"{ArtRoot}/Sprites/Asteroid.png");
            starSprite = LoadSprite($"{ArtRoot}/Sprites/Star.png");
            laserSprite = LoadSprite($"{ArtRoot}/Sprites/Laser.png");
            background = LoadSprite($"{ArtRoot}/Backgrounds/Space.png");
            dot = LoadSprite($"{GeneratedDir}/Dot.png");
            roundFill = LoadSprite($"{GeneratedDir}/RoundFill.png");
            roundOutline = LoadSprite($"{GeneratedDir}/RoundOutline.png");
            softGlow = LoadSprite($"{GeneratedDir}/SoftGlow.png");
            triangle = LoadSprite($"{GeneratedDir}/Triangle.png");
            speakerOn = LoadSprite($"{GeneratedDir}/SpeakerOn.png");
            speakerOff = LoadSprite($"{GeneratedDir}/SpeakerOff.png");
            pauseIcon = LoadSprite($"{GeneratedDir}/Pause.png");
            iconShield = LoadSprite($"{GeneratedDir}/IconShield.png");
            iconBolt = LoadSprite($"{GeneratedDir}/IconBolt.png");
            iconHeart = LoadSprite($"{GeneratedDir}/IconHeart.png");
            iconSpread = LoadSprite($"{GeneratedDir}/IconSpread.png");
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        }

        // ================================================================== MAIN MENU

        private static string BuildMainMenuScene()
        {
            Scene scene = NewScene();
            Canvas canvas = AddCanvas();
            EventSystem eventSystem = AddEventSystem();
            Transform root = canvas.transform;

            var controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();

            // ---- Right half: decorative hero spaceship with asteroids and stars (always visible).
            var decor = AddGroup(root, "Decor");
            AddDecor(decor, asteroidSprite, new Vector2(895, -965), 600, new Color(0.42f, 0.44f, 0.5f), 4f, 0.25f, 1.5f);
            AddDecor(decor, asteroidSprite, new Vector2(1645, -185), 340, new Color(0.62f, 0.64f, 0.7f), 8f, 0.4f, 3f);
            AddDecor(decor, asteroidSprite, new Vector2(1810, -645), 240, new Color(0.62f, 0.64f, 0.7f), 8f, 0.5f, -4f);
            var smallRocks = new[]
            {
                new Vector3(995, -132, 62), new Vector3(1040, -300, 64), new Vector3(1030, -475, 110),
                new Vector3(1190, -835, 56), new Vector3(1676, -482, 42), new Vector3(1556, -900, 84),
                new Vector3(1700, -1010, 72), new Vector3(900, -205, 32), new Vector3(1272, -985, 42),
            };
            foreach (Vector3 r in smallRocks)
            {
                AddDecor(decor, asteroidSprite, r, r.z, new Color(0.55f, 0.57f, 0.63f), 6f, Random.Range(0.4f, 0.8f), Random.Range(-12f, 12f));
            }

            foreach (Vector3 s in new[] { new Vector3(1148, -155, 66), new Vector3(1791, -385, 66), new Vector3(968, -626, 52), new Vector3(1685, -850, 66) })
            {
                AddDecor(decor, starSprite, s, s.z, Color.white, 5f, 1.2f, 0f);
            }

            AddDecor(decor, shipSprite, new Vector2(1343, -494), 640, Color.white, 14f, 0.9f, 0f);

            // ---- Top bar: emblem + small spaced title (left), sound toggle (right).
            AddEmblem(root);
            AddSoundButton(root, new Vector2(-30, -26));

            // ---- Menu group: big title, subtitle, buttons and key hint (hidden while instructions are open).
            Transform menu = AddGroup(root, "Menu");
            AddText(menu, "TitleSpace", "SPACE", 132, Color.white, TopLeft, MidLeft, new Vector2(124, -262), new Vector2(900, 150), TextAnchor.MiddleLeft);
            AddText(menu, "TitleExplorer", "EXPLORER", 132, Cyan, TopLeft, MidLeft, new Vector2(124, -376), new Vector2(900, 150), TextAnchor.MiddleLeft);
            AddText(menu, "Subtitle", "Collect stars. Explore the unknown.", 34, TextSoft, TopLeft, MidLeft, new Vector2(132, -478), new Vector2(900, 50), TextAnchor.MiddleLeft, FontStyle.Normal);

            Button play = AddButton(menu, "PlayButton", "PLAY", ButtonStyle.Primary, TopLeft, MidLeft, new Vector2(132, -580), new Vector2(620, 92), 40, controller.Play);
            Button instructions = AddButton(menu, "InstructionsButton", "INSTRUCTIONS", ButtonStyle.Secondary, TopLeft, MidLeft, new Vector2(132, -692), new Vector2(620, 82), 34, controller.ShowInstructions);
            AddButton(menu, "QuitButton", "QUIT", ButtonStyle.Subdued, TopLeft, MidLeft, new Vector2(132, -800), new Vector2(620, 80), 32, controller.Quit);

            AddArrowKeys(menu, BottomLeft, new Vector2(110, 82), 34, new Color(1f, 1f, 1f, 0.85f));
            AddText(menu, "KeyHint", "Arrow keys to move  ·  Space to shoot", 26, TextSoft, BottomLeft, MidLeft, new Vector2(190, 66), new Vector2(800, 40), TextAnchor.MiddleLeft, FontStyle.Normal);

            // ---- Instructions overlay.
            Transform overlay = AddGroup(root, "InstructionsPanel", Dim);
            Transform window = AddWindow(overlay, "Window", new Vector2(1080, 880));
            AddText(window, "Header", "HOW TO PLAY", 54, Cyan, Center, Center, new Vector2(0, 360), new Vector2(900, 70));
            AddLine(window, Center, new Vector2(0, 306), new Vector2(140, 3), Cyan);

            const float iconX = -330f, textX = -250f;
            float[] rows = { 235f, 158f, 81f, 4f, -73f, -150f, -227f };
            AddArrowKeys(window, Center, new Vector2(iconX, rows[0]), 28, Color.white);
            AddInfoText(window, "Row1", "Move the spaceship", new Vector2(textX, rows[0]));
            AddKeyCap(window, Center, Center, new Vector2(iconX, rows[1]), new Vector2(118, 32), Color.white);
            AddInfoText(window, "Row2", "Shoot lasers — the ship aims where it moves", new Vector2(textX, rows[1]));
            AddImage(window, "StarIcon", starSprite, Color.white, Center, Center, new Vector2(iconX, rows[2]), new Vector2(58, 58));
            AddInfoText(window, "Row3", "Collect stars   <color=#FFD54A>+10</color>  (chain them for up to <color=#FFD54A>x5</color>)", new Vector2(textX, rows[2]));
            AddImage(window, "LaserIcon", laserSprite, Color.white, Center, Center, new Vector2(iconX, rows[3]), new Vector2(16, 64));
            AddInfoText(window, "Row4", "Shoot asteroids — big rocks <color=#55E6FF>split into smaller pieces</color>", new Vector2(textX, rows[3]));
            AddImage(window, "PowerIcon", iconShield, Color.white, Center, Center, new Vector2(iconX, rows[4]), new Vector2(56, 56));
            AddInfoText(window, "Row5", "Grab power-ups: <color=#55E6FF>shield</color> · <color=#FFD54A>rapid</color> · <color=#8CFF9E>extra life</color> · <color=#FF80E0>spread</color>", new Vector2(textX, rows[4]));
            AddImage(window, "EnemyIcon", shipSprite, new Color(1f, 0.5f, 0.5f), Center, Center, new Vector2(iconX, rows[5]), new Vector2(52, 52));
            AddInfoText(window, "Row6", "Dodge <color=#FF7A70>enemy ships</color> — they shoot back", new Vector2(textX, rows[5]));
            AddImage(window, "AsteroidIcon", asteroidSprite, Color.white, Center, Center, new Vector2(iconX, rows[6]), new Vector2(64, 64));
            AddInfoText(window, "Row7", "Hit an asteroid   <color=#FF7A70>-5 points, lose a life</color>", new Vector2(textX, rows[6]));
            AddText(window, "Footer", "3 lives (up to 5) · wrap around the screen edges · Esc to pause", 26, TextMuted, Center, Center, new Vector2(0, -290), new Vector2(1000, 40), TextAnchor.MiddleCenter, FontStyle.Normal);
            Button close = AddButton(window, "CloseButton", "CLOSE", ButtonStyle.Primary, Center, Center, new Vector2(0, -365), new Vector2(360, 80), 34, controller.HideInstructions);

            SetRef(controller, "menuGroup", menu.gameObject);
            SetRef(controller, "instructionsPanel", overlay.gameObject);
            SetRef(controller, "instructionsButton", instructions.gameObject);
            SetRef(controller, "closeButton", close.gameObject);
            eventSystem.firstSelectedGameObject = play.gameObject;

            return SaveScene(scene, SceneNames.MainMenu);
        }

        // ================================================================== GAMEPLAY

        private static string BuildGameplayScene()
        {
            Scene scene = NewScene();
            Canvas canvas = AddCanvas();
            AddEventSystem();
            Transform root = canvas.transform;

            // Load the prefabs AFTER NewScene (see LoadSprites for why).
            var laserPrefab = LoadPrefab<Laser>("Laser");
            var asteroidPrefab = LoadPrefab<Asteroid>("Asteroid");
            var starPrefab = LoadPrefab<Star>("Star");
            var enemyPrefab = LoadPrefab<Enemy>("Enemy");

            // ---- Player spaceship.
            var ship = new GameObject("Spaceship");
            ship.transform.position = new Vector3(0f, -3f, 0f);
            ship.transform.localScale = Vector3.one * ShipScale;
            AddSpriteRenderer(ship, shipSprite, 10);
            var body = ship.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            AddShapeCollider(ship, shipSprite, false);
            var player = ship.AddComponent<PlayerController>();
            SetRef(player, "laserPrefab", laserPrefab);

            // ---- HUD top-left: SCORE panel.
            Transform scorePanel = AddWindow(root, "ScorePanel", new Vector2(250, 80), TopLeft, TopLeft, new Vector2(25, -20), HudFill, 0.7f);
            AddImage(scorePanel, "StarIcon", starSprite, Color.white, MidLeft, Center, new Vector2(46, 0), new Vector2(54, 54));
            AddText(scorePanel, "ScoreLabel", "SCORE", 18, TextSoft, MidLeft, MidLeft, new Vector2(88, 20), new Vector2(150, 24), TextAnchor.MiddleLeft);
            Text score = AddText(scorePanel, "ScoreText", "0", 36, Color.white, MidLeft, MidLeft, new Vector2(88, -13), new Vector2(150, 42), TextAnchor.MiddleLeft);

            // ---- HUD top-left: LIVES panel (label + one ship icon per life, in a row).
            Transform livesPanel = AddWindow(root, "LivesPanel", new Vector2(430, 80), TopLeft, TopLeft, new Vector2(290, -20), HudFill, 0.7f);
            AddText(livesPanel, "LivesLabel", "LIVES", 18, TextSoft, MidLeft, MidLeft, new Vector2(22, 0), new Vector2(80, 24), TextAnchor.MiddleLeft);
            var lifeIcons = new Object[5];
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                lifeIcons[i] = AddImage(livesPanel, $"Life{i + 1}", shipSprite, Color.white, MidLeft, Center, new Vector2(125 + i * 52, 0), new Vector2(46, 46));
            }

            // ---- HUD top-left: combo multiplier, just under the score panel.
            Text combo = AddText(root, "ComboText", "", 42, Gold, TopLeft, TopLeft, new Vector2(28, -112), new Vector2(260, 50), TextAnchor.MiddleLeft);

            // ---- HUD top-center: understated game title between two lines.
            AddText(root, "HudTitle", Spaced("SPACE EXPLORER"), 24, new Color(0.82f, 0.9f, 0.95f, 0.75f), TopCenter, TopCenter, new Vector2(0, -20), new Vector2(600, 40));
            AddLine(root, TopCenter, new Vector2(-235, -40), new Vector2(64, 2), new Color(0.82f, 0.9f, 0.95f, 0.45f));
            AddLine(root, TopCenter, new Vector2(235, -40), new Vector2(64, 2), new Color(0.82f, 0.9f, 0.95f, 0.45f));

            // ---- HUD top-right: sound + pause buttons.
            var pause = new GameObject("PauseController").AddComponent<PauseController>();
            AddSoundButton(root, new Vector2(-113, -20));
            AddIconButton(root, "PauseButton", pauseIcon, new Vector2(-25, -20), pause.Toggle);

            // ---- HUD bottom-center: controls help strip (fades to low opacity after a few seconds).
            Transform help = AddWindow(root, "HelpStrip", new Vector2(560, 84), BottomCenter, BottomCenter, new Vector2(0, 28), HudFill, 0.45f);
            help.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            help.gameObject.AddComponent<FadeAfterDelay>();
            AddArrowKeys(help, Center, new Vector2(-205, 0), 25, new Color(1f, 1f, 1f, 0.85f));
            AddText(help, "MoveLabel", "MOVE", 22, TextSoft, Center, MidLeft, new Vector2(-145, 0), new Vector2(100, 34), TextAnchor.MiddleLeft, FontStyle.Normal);
            AddLine(help, Center, new Vector2(-25, 0), new Vector2(2, 46), new Color(1f, 1f, 1f, 0.25f));
            AddKeyCap(help, Center, Center, new Vector2(70, 0), new Vector2(120, 28), new Color(1f, 1f, 1f, 0.85f));
            AddText(help, "ShootLabel", "SHOOT", 22, TextSoft, Center, MidLeft, new Vector2(150, 0), new Vector2(100, 34), TextAnchor.MiddleLeft, FontStyle.Normal);

            // ---- HUD bottom-left: names and remaining times of active power-ups.
            Text buffs = AddText(root, "BuffText", "", 24, TextSoft, BottomLeft, BottomLeft, new Vector2(25, 26), new Vector2(760, 40), TextAnchor.MiddleLeft, FontStyle.Normal);

            // ---- Pause overlay.
            Transform pauseOverlay = AddGroup(root, "PauseOverlay", Dim);
            Transform pauseWindow = AddWindow(pauseOverlay, "Window", new Vector2(700, 520));
            AddText(pauseWindow, "Title", "PAUSED", 64, Color.white, Center, Center, new Vector2(0, 160), new Vector2(600, 80));
            AddText(pauseWindow, "Hint", "Press Esc to resume", 26, TextMuted, Center, Center, new Vector2(0, 95), new Vector2(600, 40), TextAnchor.MiddleCenter, FontStyle.Normal);
            Button resume = AddButton(pauseWindow, "ResumeButton", "RESUME", ButtonStyle.Primary, Center, Center, new Vector2(0, -20), new Vector2(460, 86), 38, pause.Resume);
            AddButton(pauseWindow, "MainMenuButton", "MAIN MENU", ButtonStyle.Secondary, Center, Center, new Vector2(0, -132), new Vector2(460, 78), 32, pause.MainMenu);
            SetRef(pause, "pauseOverlay", pauseOverlay.gameObject);
            SetRef(pause, "resumeButton", resume.gameObject);

            // ---- Game manager.
            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(manager, "player", player);
            SetRef(manager, "scoreText", score);
            SetRef(manager, "comboText", combo);
            SetRef(manager, "buffText", buffs);
            SetRefArray(manager, "lifeIcons", lifeIcons);

            // ---- Spawner.
            var spawner = new GameObject("Spawner").AddComponent<Spawner>();
            SetRef(spawner, "asteroidPrefab", asteroidPrefab);
            SetRef(spawner, "starPrefab", starPrefab);
            SetRef(spawner, "enemyPrefab", enemyPrefab);
            SetRef(spawner, "player", ship.transform);

            return SaveScene(scene, SceneNames.Gameplay);
        }

        // ================================================================== END GAME

        private static string BuildEndGameScene()
        {
            Scene scene = NewScene();
            Canvas canvas = AddCanvas();
            EventSystem eventSystem = AddEventSystem();
            Transform root = canvas.transform;

            var controller = new GameObject("EndGameController").AddComponent<EndGameController>();
            AddEmblem(root);

            Transform window = AddWindow(root, "ResultPanel", new Vector2(900, 820));
            AddText(window, "Title", "GAME <color=#55E6FF>OVER</color>", 104, Color.white, Center, Center, new Vector2(0, 300), new Vector2(860, 130));
            AddLine(window, Center, new Vector2(0, 222), new Vector2(120, 3), Cyan);
            AddText(window, "ScoreLabel", Spaced("SCORE"), 26, TextSoft, Center, Center, new Vector2(0, 165), new Vector2(600, 40));
            Text score = AddText(window, "ScoreText", "0", 96, Color.white, Center, Center, new Vector2(0, 82), new Vector2(500, 110));
            AddImage(window, "StarLeft", starSprite, Color.white, Center, Center, new Vector2(-210, 82), new Vector2(64, 64));
            AddImage(window, "StarRight", starSprite, Color.white, Center, Center, new Vector2(210, 82), new Vector2(64, 64));
            Text high = AddText(window, "HighScoreText", "HIGH SCORE: 0", 30, Cyan, Center, Center, new Vector2(0, -18), new Vector2(800, 44));

            Button again = AddButton(window, "PlayAgainButton", "PLAY AGAIN", ButtonStyle.Primary, Center, Center, new Vector2(0, -110), new Vector2(520, 88), 38, controller.PlayAgain);
            AddButton(window, "MainMenuButton", "MAIN MENU", ButtonStyle.Secondary, Center, Center, new Vector2(0, -215), new Vector2(520, 80), 32, controller.MainMenu);
            AddButton(window, "QuitButton", "QUIT", ButtonStyle.Subdued, Center, Center, new Vector2(0, -315), new Vector2(520, 76), 30, controller.Quit);

            SetRef(controller, "scoreText", score);
            SetRef(controller, "highScoreText", high);
            eventSystem.firstSelectedGameObject = again.gameObject;
            return SaveScene(scene, SceneNames.EndGame);
        }

        // ================================================================== scene helpers

        /// <summary>New empty scene with camera, painted background and parallax starfield.</summary>
        private static Scene NewScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadSprites();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SpaceColor;
            camGo.AddComponent<AudioListener>();

            if (background != null)
            {
                var bg = new GameObject("Background");
                bg.transform.localScale = Vector3.one * 1.07f; // fits 16:9; BackgroundFitter adapts at runtime
                AddSpriteRenderer(bg, background, -200);
                bg.AddComponent<BackgroundFitter>();
            }

            var field = new GameObject("StarfieldBackground").AddComponent<StarfieldBackground>();
            SetRef(field, "dotSprite", dot);
            SetRef(field, "spriteMaterial", spriteMaterial);
            var so = new SerializedObject(field);
            so.FindProperty("dotsPerLayer").intValue = 20; // the painted background already has stars
            so.ApplyModifiedPropertiesWithoutUndo();
            return scene;
        }

        private static string SaveScene(Scene scene, string name)
        {
            string path = $"{SceneDir}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Canvas AddCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static EventSystem AddEventSystem()
        {
            // The project uses the new Input System only, so UI needs InputSystemUIInputModule.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return go.GetComponent<EventSystem>();
        }

        // ================================================================== UI building blocks

        private static RectTransform Place(GameObject go, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            go.transform.SetParent(parent, false);
            if (!go.TryGetComponent(out RectTransform rect))
            {
                rect = go.AddComponent<RectTransform>();
            }

            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Full-screen container; with a color it also dims and blocks clicks behind it.</summary>
        private static Transform AddGroup(Transform parent, string name, Color? dimColor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (dimColor.HasValue)
            {
                go.AddComponent<Image>().color = dimColor.Value;
            }

            return go.transform;
        }

        private static Image AddImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            bool sliced = false, float ppuMultiplier = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Place(go, parent, anchor, pivot, pos, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = ppuMultiplier;
            }
            else
            {
                image.preserveAspect = true;
            }

            return image;
        }

        /// <summary>Thin solid line / divider.</summary>
        private static void AddLine(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            AddImage(parent, "Line", null, color, anchor, Center, pos, size);
        }

        /// <summary>Rounded dark panel with a thin cyan border (as in the mockups).</summary>
        private static Transform AddWindow(Transform parent, string name, Vector2 size) =>
            AddWindow(parent, name, size, Center, Center, Vector2.zero, PanelFill, 0.85f);

        private static Transform AddWindow(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 pos, Color fill, float borderAlpha)
        {
            Image panel = AddImage(parent, name, roundFill, fill, anchor, pivot, pos, size, true);
            panel.raycastTarget = true; // panels block clicks to things behind them
            var border = AddImage(panel.transform, "Border", roundOutline, new Color(Cyan.r, Cyan.g, Cyan.b, borderAlpha), Vector2.zero, Center, Vector2.zero, Vector2.zero, true);
            Stretch(border.rectTransform);
            return panel.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Text AddText(Transform parent, string name, string content, int size, Color color,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box,
            TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            Place(go, parent, anchor, pivot, pos, box);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -2f);
            return text;
        }

        private static void AddInfoText(Transform parent, string name, string content, Vector2 pos)
        {
            AddText(parent, name, content, 32, Color.white, Center, MidLeft, pos, new Vector2(640, 46), TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        /// <summary>"ABC DEF" -> "A B C     D E F" (fake letter spacing for the legacy Text component).</summary>
        private static string Spaced(string s, int wordGap = 5)
        {
            string[] words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = string.Join(" ", words[i].ToCharArray());
            }

            return string.Join(new string(' ', wordGap), words);
        }

        /// <summary>Small ship emblem + spaced game name, top-left (Main Menu and End Game).</summary>
        private static void AddEmblem(Transform root)
        {
            AddImage(root, "Emblem", shipSprite, Color.white, TopLeft, Center, new Vector2(92, -75), new Vector2(58, 58));
            AddText(root, "EmblemText", Spaced("SPACE EXPLORER"), 24, new Color(1f, 1f, 1f, 0.92f), TopLeft, MidLeft, new Vector2(150, -75), new Vector2(600, 40), TextAnchor.MiddleLeft);
        }

        private static void AddDecor(Transform parent, Sprite sprite, Vector2 pos, float size, Color tint, float bob, float bobSpeed, float spin)
        {
            Image image = AddImage(parent, sprite.name, sprite, tint, TopLeft, Center, pos, new Vector2(size, size));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f) * (spin != 0f ? 1f : 0f));
            image.gameObject.AddComponent<UIFloat>().Setup(bob, bobSpeed, spin);
        }

        /// <summary>Keyboard key outline with an optional child (arrow) inside.</summary>
        private static Image AddKeyCap(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            return AddImage(parent, "Key", roundOutline, color, anchor, pivot, pos, size, true, 2f);
        }

        /// <summary>Arrow-key cluster icon (up on top, left/down/right below), centered on <paramref name="pos"/>.</summary>
        private static void AddArrowKeys(Transform parent, Vector2 anchor, Vector2 pos, float key, Color color)
        {
            var go = new GameObject("ArrowKeys", typeof(RectTransform));
            Place(go, parent, anchor, Center, pos, new Vector2(key * 3.3f, key * 2.2f));
            float gap = key * 1.1f;
            var keys = new[]
            {
                (new Vector2(0, gap / 2), 0f),
                (new Vector2(-gap, -gap / 2), 90f),
                (new Vector2(0, -gap / 2), 180f),
                (new Vector2(gap, -gap / 2), -90f),
            };
            foreach (var (offset, angle) in keys)
            {
                Image cap = AddKeyCap(go.transform, Center, Center, offset, new Vector2(key, key), color);
                Image arrow = AddImage(cap.transform, "Arrow", triangle, color, Center, Center, Vector2.zero, new Vector2(key * 0.42f, key * 0.42f));
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>Rounded text button in one of the three mockup styles, with hover/press feedback.</summary>
        private static Button AddButton(Transform parent, string name, string label, ButtonStyle style,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, anchor, pivot, pos, size);

            Color fillN, fillH, borderN, borderH, labelN, labelH;
            switch (style)
            {
                case ButtonStyle.Primary:
                    fillN = Cyan; fillH = CyanBright;
                    borderN = borderH = Color.clear;
                    labelN = labelH = new Color(0.02f, 0.08f, 0.12f);
                    // Soft halo behind the primary button (extends 30 units around it and fades out).
                    AddImage(go.transform, "Glow", softGlow, new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f), Center, Center, Vector2.zero, size + new Vector2(60, 60), true);
                    break;
                case ButtonStyle.Secondary:
                    fillN = new Color(0.02f, 0.05f, 0.09f, 0.85f); fillH = new Color(0.05f, 0.2f, 0.27f, 0.92f);
                    borderN = Cyan; borderH = CyanBright;
                    labelN = Color.white; labelH = Color.white;
                    break;
                default:
                    fillN = new Color(0.04f, 0.05f, 0.07f, 0.75f); fillH = new Color(0.1f, 0.12f, 0.15f, 0.88f);
                    borderN = new Color(0.36f, 0.4f, 0.45f); borderH = new Color(0.62f, 0.67f, 0.72f);
                    labelN = TextMuted; labelH = Color.white;
                    break;
            }

            Image fill = AddImage(go.transform, "Fill", roundFill, fillN, Center, Center, Vector2.zero, size, true);
            Stretch(fill.rectTransform);
            fill.raycastTarget = true;

            Image border = null;
            if (style != ButtonStyle.Primary)
            {
                border = AddImage(go.transform, "Border", roundOutline, borderN, Center, Center, Vector2.zero, size, true);
                Stretch(border.rectTransform);
            }

            Text text = AddText(go.transform, "Label", Spaced(label, 3), fontSize, labelN, Center, Center, Vector2.zero, size);
            if (style == ButtonStyle.Primary)
            {
                Object.DestroyImmediate(text.GetComponent<Shadow>()); // dark text on cyan: no shadow
            }

            var button = go.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None; // ButtonFx handles the visuals
            UnityEventTools.AddPersistentListener(button.onClick, onClick);

            go.AddComponent<ButtonFx>().Setup(fill, border, text, fillN, fillH, borderN, borderH, labelN, labelH, true);
            return button;
        }

        /// <summary>Square HUD button with an icon (sound, pause). Not keyboard-navigable so Space never clicks it.</summary>
        private static Button AddIconButton(Transform parent, string name, Sprite icon, Vector2 pos, UnityAction onClick)
        {
            var size = new Vector2(64, 64);
            var go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, TopRight, TopRight, pos, size);

            Image fill = AddImage(go.transform, "Fill", roundFill, HudFill, Center, Center, Vector2.zero, size, true, 1.4f);
            Stretch(fill.rectTransform);
            fill.raycastTarget = true;
            Image border = AddImage(go.transform, "Border", roundOutline, Cyan, Center, Center, Vector2.zero, size, true, 1.4f);
            Stretch(border.rectTransform);
            Image iconImage = AddImage(go.transform, "Icon", icon, Color.white, Center, Center, Vector2.zero, new Vector2(38, 38));

            var button = go.AddComponent<Button>();
            button.targetGraphic = fill;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(button.onClick, onClick);

            go.AddComponent<ButtonFx>().Setup(fill, border, iconImage,
                HudFill, new Color(0.05f, 0.2f, 0.27f, 0.9f), Cyan, CyanBright, Color.white, Color.white, false);
            return button;
        }

        private static void AddSoundButton(Transform root, Vector2 pos)
        {
            // The toggle component lives on its own object so the button's OnClick can target it.
            var toggle = new GameObject("SoundToggle").AddComponent<SoundToggleButton>();
            Button button = AddIconButton(root, "SoundButton", speakerOn, pos, toggle.Toggle);
            SetRef(toggle, "icon", button.transform.Find("Icon").GetComponent<Image>());
            SetRef(toggle, "soundOnSprite", speakerOn);
            SetRef(toggle, "soundOffSprite", speakerOff);
        }

        // ================================================================== prefabs

        private static void CreateLaserPrefab()
        {
            var go = new GameObject("Laser");
            go.transform.localScale = Vector3.one * LaserScale;
            AddSpriteRenderer(go, laserSprite, 5);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = laserSprite.bounds.size;
            col.isTrigger = true;
            go.AddComponent<Laser>();
            SavePrefab(go);
        }

        private static void CreateAsteroidPrefab()
        {
            var go = new GameObject("Asteroid");
            go.transform.localScale = Vector3.one * AsteroidScale;
            AddSpriteRenderer(go, asteroidSprite, 2);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = asteroidSprite.bounds.extents.x * 0.8f; // the rock fills ~90% of the image
            col.isTrigger = true;
            var asteroid = go.AddComponent<Asteroid>();
            SetRef(asteroid, "powerUpPrefab", LoadPrefab<PowerUp>("PowerUp"));
            SavePrefab(go);
        }

        private static void CreatePowerUpPrefab()
        {
            var go = new GameObject("PowerUp");
            go.transform.localScale = Vector3.one * PowerUpScale;
            AddSpriteRenderer(go, iconShield, 7);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = roundFill.bounds.extents.x; // forgiving pickup radius
            col.isTrigger = true;

            var powerUp = go.AddComponent<PowerUp>();
            SetRef(powerUp, "shieldSprite", iconShield);
            SetRef(powerUp, "rapidFireSprite", iconBolt);
            SetRef(powerUp, "extraLifeSprite", iconHeart);
            SetRef(powerUp, "spreadSprite", iconSpread);
            SavePrefab(go);
        }

        private static void CreateEnemyLaserPrefab()
        {
            var go = new GameObject("EnemyLaser");
            go.transform.localScale = new Vector3(0.35f, 0.9f, 1f);
            AddSpriteRenderer(go, roundFill, 6).color = new Color(1f, 0.35f, 0.35f);
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = roundFill.bounds.size;
            col.isTrigger = true;
            go.AddComponent<EnemyLaser>();
            SavePrefab(go);
        }

        private static void CreateEnemyPrefab()
        {
            var go = new GameObject("Enemy");
            go.transform.localScale = Vector3.one * ShipScale;
            AddSpriteRenderer(go, shipSprite, 8).color = new Color(1f, 0.55f, 0.55f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            AddShapeCollider(go, shipSprite, true);

            var enemy = go.AddComponent<Enemy>();
            SetRef(enemy, "laserPrefab", LoadPrefab<EnemyLaser>("EnemyLaser"));
            SetRef(enemy, "powerUpPrefab", LoadPrefab<PowerUp>("PowerUp"));
            SavePrefab(go);
        }

        private static void CreateStarPrefab()
        {
            var go = new GameObject("Star");
            go.transform.localScale = Vector3.one * StarScale;
            AddSpriteRenderer(go, starSprite, 1);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = starSprite.bounds.extents.x * 0.8f;
            col.isTrigger = true;
            go.AddComponent<Star>();
            SavePrefab(go);
        }

        private static void SavePrefab(GameObject go)
        {
            PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{go.name}.prefab");
            Object.DestroyImmediate(go);
        }

        private static T LoadPrefab<T>(string name) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            return go != null ? go.GetComponent<T>() : null;
        }

        private static SpriteRenderer AddSpriteRenderer(GameObject go, Sprite sprite, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = spriteMaterial;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// PolygonCollider2D that follows the sprite's outline (its physics shape), so the
        /// ship's empty space between the wings does not count as a hit. Falls back to a circle.
        /// </summary>
        private static void AddShapeCollider(GameObject go, Sprite sprite, bool isTrigger)
        {
            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount == 0)
            {
                var circle = go.AddComponent<CircleCollider2D>();
                circle.radius = sprite.bounds.extents.x * 0.6f;
                circle.isTrigger = isTrigger;
                return;
            }

            var poly = go.AddComponent<PolygonCollider2D>();
            poly.pathCount = shapeCount;
            var points = new List<Vector2>();
            for (int i = 0; i < shapeCount; i++)
            {
                sprite.GetPhysicsShape(i, points);
                poly.SetPath(i, points);
            }

            poly.isTrigger = isTrigger;
        }

        // ================================================================== asset helpers

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Space Explorer] Missing sprite: {path}");
            }

            return sprite;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (value == null)
            {
                Debug.LogError($"[Space Explorer] {target.name}.{field} is being set to null - check the asset exists.");
            }

            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateSpriteMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            // Unlit so sprites show their real colors without placing 2D lights.
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }

        // ================================================================== generated UI sprites

        private delegate float AlphaPainter(Vector2 p);

        /// <summary>Paints the white UI shapes (tinted by Image.color at runtime) into Assets/Art/Generated.</summary>
        private static void GenerateUiSprites()
        {
            var nineSlice = new Vector4(20, 20, 20, 20);
            WriteSprite("RoundFill", 64, 64, p => RoundedFill(p, new Vector2(32, 32), new Vector2(31, 31), 12f), nineSlice, 100);
            WriteSprite("RoundOutline", 64, 64, p => RoundedStroke(p, new Vector2(32, 32), new Vector2(31, 31), 12f, 2.5f), nineSlice, 100);
            WriteSprite("SoftGlow", 128, 128, p =>
            {
                float d = RoundedBox(p, new Vector2(64, 64), new Vector2(34, 34), 12f);
                return d <= 0f ? 1f : Mathf.Pow(Mathf.Clamp01(1f - d / 30f), 2f);
            }, new Vector4(48, 48, 48, 48), 100);
            WriteSprite("Triangle", 32, 32, p => Convex(p, new Vector2(4, 7), new Vector2(28, 7), new Vector2(16, 27)), Vector4.zero, 100);
            WriteSprite("SpeakerOn", 64, 64, p => Mathf.Max(SpeakerBody(p), Arc(p, 9f), Arc(p, 17f)), Vector4.zero, 100);
            WriteSprite("SpeakerOff", 64, 64, p => Mathf.Max(SpeakerBody(p), Segment(p, new Vector2(43, 23), new Vector2(57, 41), 2.2f), Segment(p, new Vector2(43, 41), new Vector2(57, 23), 2.2f)), Vector4.zero, 100);
            WriteSprite("Pause", 64, 64, p => Mathf.Max(RoundedFill(p, new Vector2(23, 32), new Vector2(5, 18), 2f), RoundedFill(p, new Vector2(41, 32), new Vector2(5, 18), 2f)), Vector4.zero, 100);
            WriteSprite("Dot", 16, 16, p => Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(8, 8)) / 8f), Vector4.zero, 128);

            // Power-up icons (white shapes tinted per kind at runtime).
            WriteSprite("IconShield", 64, 64, p => Convex(p,
                new Vector2(32, 6), new Vector2(58, 20), new Vector2(54, 44),
                new Vector2(32, 58), new Vector2(10, 44), new Vector2(6, 20)), Vector4.zero, 100);
            WriteSprite("IconBolt", 64, 64, p => Mathf.Max(
                Mathf.Max(Segment(p, new Vector2(12, 26), new Vector2(32, 46), 4f), Segment(p, new Vector2(32, 46), new Vector2(52, 26), 4f)),
                Mathf.Max(Segment(p, new Vector2(12, 8), new Vector2(32, 28), 4f), Segment(p, new Vector2(32, 28), new Vector2(52, 8), 4f))), Vector4.zero, 100);
            WriteSprite("IconHeart", 64, 64, p => Mathf.Max(
                Mathf.Max(Circle(p, new Vector2(22, 40), 12f), Circle(p, new Vector2(42, 40), 12f)),
                Convex(p, new Vector2(10, 39), new Vector2(32, 8), new Vector2(54, 39))), Vector4.zero, 100);
            WriteSprite("IconSpread", 64, 64, p => Mathf.Max(
                Mathf.Max(Segment(p, new Vector2(32, 10), new Vector2(32, 54), 3.5f), Segment(p, new Vector2(32, 10), new Vector2(16, 46), 3.5f)),
                Segment(p, new Vector2(32, 10), new Vector2(48, 46), 3.5f)), Vector4.zero, 100);
        }

        private static void WriteSprite(string name, int w, int h, AlphaPainter painter, Vector4 border, float ppu)
        {
            string path = $"{GeneratedDir}/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(painter(new Vector2(x + 0.5f, y + 0.5f))));
                }
            }

            tex.SetPixels(pixels);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        /// <summary>Signed distance to a rounded box (negative inside).</summary>
        private static float RoundedBox(Vector2 p, Vector2 center, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - (half - new Vector2(r, r));
            Vector2 outside = Vector2.Max(q, Vector2.zero);
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        private static float RoundedFill(Vector2 p, Vector2 c, Vector2 half, float r) => Mathf.Clamp01(0.5f - RoundedBox(p, c, half, r));

        private static float RoundedStroke(Vector2 p, Vector2 c, Vector2 half, float r, float width)
        {
            float d = RoundedBox(p, c, half, r);
            return Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(0.5f + d + width);
        }

        /// <summary>Anti-aliased convex polygon (vertices counter-clockwise).</summary>
        private static float Convex(Vector2 p, params Vector2[] v)
        {
            float min = float.MaxValue;
            for (int i = 0; i < v.Length; i++)
            {
                Vector2 a = v[i], b = v[(i + 1) % v.Length];
                Vector2 e = (b - a).normalized;
                Vector2 inward = new Vector2(-e.y, e.x);
                min = Mathf.Min(min, Vector2.Dot(p - a, inward));
            }

            return Mathf.Clamp01(min + 0.5f);
        }

        /// <summary>Anti-aliased filled circle.</summary>
        private static float Circle(Vector2 p, Vector2 center, float radius) =>
            Mathf.Clamp01(0.5f + radius - Vector2.Distance(p, center));

        private static float SpeakerBody(Vector2 p) => Mathf.Max(
            Convex(p, new Vector2(10, 25), new Vector2(22, 25), new Vector2(22, 39), new Vector2(10, 39)),
            Convex(p, new Vector2(21, 25), new Vector2(34, 13), new Vector2(34, 51), new Vector2(21, 39)));

        /// <summary>Sound-wave arc to the right of the speaker.</summary>
        private static float Arc(Vector2 p, float radius)
        {
            Vector2 d = p - new Vector2(34, 32);
            float angle = Mathf.Abs(Mathf.Atan2(d.y, d.x)) * Mathf.Rad2Deg;
            float ring = Mathf.Clamp01(0.5f + 1.8f - Mathf.Abs(d.magnitude - radius));
            float mask = Mathf.Clamp01((52f - angle) / 4f);
            return ring * mask;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float dist = Vector2.Distance(p, a + ab * t);
            return Mathf.Clamp01(0.5f + halfWidth - dist);
        }
    }
}
