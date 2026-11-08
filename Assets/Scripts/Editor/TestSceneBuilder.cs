using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Core;
using Wickfall.Core.Events;
using Wickfall.Data;
using Wickfall.DebugTools;
using Wickfall.Enemies;
using Wickfall.Player;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Test > Build Aziz Test Scene": makes sure the data and art exist, builds the fully wired
    /// Player, Dripling and WaxPickup prefabs if they are missing, then creates a small dark test scene with
    /// the player, a box, two Driplings, a wax pickup, the camera and the HUD.
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string TestRoot = "Assets/Test";
        private static readonly string[] TesterFolders = { "Aziz", "Makrem", "Safi" };
        private const string AzizScenePath = TestRoot + "/Aziz/Aziz_TestScene.unity";

        internal const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        internal const string DriplingPrefabPath = "Assets/Prefabs/Enemies/Dripling.prefab";
        internal const string PickupPrefabPath = "Assets/Prefabs/Pickups/WaxPickup.prefab";
        internal const string SquarePath = "Assets/Art/Sprites/Props/placeholder_square.png";
        internal const string CirclePath = "Assets/Art/Sprites/Props/placeholder_circle.png";
        private const string DataEnemies = "Assets/Data/Enemies/";

        // Full-wax detection is 2 + 6 x 1.5 = 11 units, so these start just out of sight.
        private static readonly Vector3[] DriplingPositions = { new Vector3(12f, 6f, 0f), new Vector3(-12f, -6f, 0f) };
        private static readonly Vector3 PickupPosition = new Vector3(-3f, 2f, 0f);

        private const string DataPlayer = "Assets/Data/Player/";
        private const string DataEvents = "Assets/Data/EventChannels/";
        private const string AnimPlayer = "Assets/Animations/Player/";

        // Candle position measured on the 26x56 walk frames (pivot at the feet).
        private static readonly Vector3 FlamePosition = new Vector3(0.06f, 2.19f, 0f);
        private static readonly Vector3 LightPosition = new Vector3(0.06f, 2.3f, 0f);

        [MenuItem("Tools/Wickfall/Test/Build Aziz Test Scene", priority = 0)]
        public static void BuildAzizTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var folder in TesterFolders) WickfallEditorUtil.EnsureFolder($"{TestRoot}/{folder}");

            WickfallDataCreator.CreateDefaultData();
            CandlemanSpriteImporter.SetupAll();
            var square = CreatePlaceholderSprite(SquarePath, circle: false);
            var circle = CreatePlaceholderSprite(CirclePath, circle: true);
            var playerPrefab = LoadOrBuildPlayerPrefab(square);
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath) ?? BuildPickupPrefab(circle);
            var driplingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DriplingPrefabPath) ?? BuildDriplingPrefab(circle, square, pickupPrefab);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateGlobalLight();
            CreateSprite("Ground", square, Vector3.zero, new Vector3(40f, 40f, 1f), new Color(0.22f, 0.19f, 0.17f), -10);
            var box = CreateSprite("Box", square, new Vector3(4f, 1f, 0f), new Vector3(2f, 2f, 1f), new Color(0.45f, 0.32f, 0.2f), 0);
            box.AddComponent<BoxCollider2D>();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;

            for (int i = 0; i < DriplingPositions.Length; i++)
            {
                var dripling = (GameObject)PrefabUtility.InstantiatePrefab(driplingPrefab);
                dripling.name = $"Dripling ({i + 1})";
                dripling.transform.position = DriplingPositions[i];
            }

            var pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab);
            pickup.transform.position = PickupPosition;

            WickfallSceneTools.CreateWaxHud();

            EditorSceneManager.SaveScene(scene, AzizScenePath);
            Selection.activeGameObject = player;
            Debug.Log($"[Wickfall] Test scene saved at {AzizScenePath}. Press Play to test.");
        }

        [MenuItem("Tools/Wickfall/Test/Rebuild Player Prefab", priority = 20)]
        public static void RebuildPlayerPrefab()
        {
            if (File.Exists(PlayerPrefabPath) &&
                !EditorUtility.DisplayDialog("Rebuild Player prefab?",
                    $"This replaces {PlayerPrefabPath} and loses any changes made to it by hand.", "Rebuild", "Cancel"))
            {
                return;
            }

            WickfallDataCreator.CreateDefaultData();
            CandlemanSpriteImporter.SetupAll();
            BuildPlayerPrefab(CreatePlaceholderSprite(SquarePath, circle: false));
        }

        [MenuItem("Tools/Wickfall/Test/Rebuild Dripling and Pickup Prefabs", priority = 21)]
        public static void RebuildEnemyPrefabs()
        {
            if ((File.Exists(DriplingPrefabPath) || File.Exists(PickupPrefabPath)) &&
                !EditorUtility.DisplayDialog("Rebuild Dripling and WaxPickup prefabs?",
                    "This replaces both prefabs and loses any changes made to them by hand.", "Rebuild", "Cancel"))
            {
                return;
            }

            WickfallDataCreator.CreateDefaultData();
            var square = CreatePlaceholderSprite(SquarePath, circle: false);
            var circle = CreatePlaceholderSprite(CirclePath, circle: true);
            BuildDriplingPrefab(circle, square, BuildPickupPrefab(circle));
        }

        // ---------- Scene ----------

        internal static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 1.5f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 2f / WickfallEditorUtil.PixelsPerUnit;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var pixelPerfect = go.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = (int)WickfallEditorUtil.PixelsPerUnit;
            pixelPerfect.refResolutionX = 480;
            pixelPerfect.refResolutionY = 270;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;

            var follow = go.AddComponent<CameraFollow>();
            SetRef(follow, "runtime", Load<PlayerRuntimeSO>(DataPlayer + "PlayerRuntime.asset"));
        }

        internal static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.08f;
            light.color = new Color(0.23f, 0.25f, 0.38f);
        }

        private static GameObject CreateSprite(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return go;
        }

        // ---------- Player prefab ----------

        internal static GameObject LoadOrBuildPlayerPrefab(Sprite square)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            return existing != null ? existing : BuildPlayerPrefab(square);
        }

        private static GameObject BuildPlayerPrefab(Sprite square)
        {
            var root = new GameObject("Player") { tag = "Player" };

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var feet = root.AddComponent<CapsuleCollider2D>();
            feet.direction = CapsuleDirection2D.Horizontal;
            feet.size = new Vector2(0.9f, 0.4f);
            feet.offset = new Vector2(0f, 0.2f);

            // Visual: the walk sheet and its animator.
            var visual = Child(root, "Visual", Vector3.zero);
            var bodyRenderer = visual.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = FirstSprite("Assets/Art/Sprites/Player/candleman_walk_down_small.png");
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<AnimatorController>(AnimPlayer + "Candleman.controller");

            // Flame: separate sprite on the candle, scaled by LightController.
            var flame = Child(root, "Flame", FlamePosition);
            var flameRenderer = flame.AddComponent<SpriteRenderer>();
            flameRenderer.sprite = FirstSprite("Assets/Art/Sprites/Player/flame_placeholder.png");
            flameRenderer.sortingOrder = 1;
            flame.AddComponent<Animator>().runtimeAnimatorController = Load<AnimatorController>(AnimPlayer + "Flame.controller");

            // Candle light: own child so flame scaling never touches it.
            var candleLight = Child(root, "CandleLight", LightPosition).AddComponent<Light2D>();
            candleLight.lightType = Light2D.LightType.Point;

            // Attack: pivot rotates toward facing, hitbox on +X, off until a swing.
            var pivot = Child(root, "AttackPivot", new Vector3(0f, 0.5f, 0f));
            var hitboxGo = Child(pivot, "AttackHitbox", new Vector3(0.9f, 0f, 0f));
            var hitboxCollider = hitboxGo.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector2(1.2f, 1.4f);
            var hitbox = hitboxGo.AddComponent<Hitbox>();
            SetEnum(hitbox, "team", (int)Team.Player);
            var swing = Child(hitboxGo, "SwingPreview", Vector3.zero);
            swing.transform.localScale = new Vector3(1.2f, 1.4f, 1f);
            var swingRenderer = swing.AddComponent<SpriteRenderer>();
            swingRenderer.sprite = square;
            swingRenderer.color = new Color(1f, 0.55f, 0.15f, 0.4f);
            swingRenderer.sortingOrder = 5;
            hitboxGo.SetActive(false);

            // Systems.
            var runtime = Load<PlayerRuntimeSO>(DataPlayer + "PlayerRuntime.asset");
            var input = Load<InputReaderSO>(DataPlayer + "InputReader.asset");
            var playerConfig = Load<PlayerConfigSO>(DataPlayer + "PlayerConfig.asset");
            var waxChanged = Load<WaxEventChannelSO>(DataEvents + "WaxChanged.asset");
            var waxGain = Load<FloatEventChannelSO>(DataEvents + "WaxGainRequested.asset");
            var extinguished = Load<VoidEventChannelSO>(DataEvents + "PlayerExtinguished.asset");
            var respawned = Load<VoidEventChannelSO>(DataEvents + "PlayerRespawned.asset");
            var brazierRested = Load<Vector3EventChannelSO>(DataEvents + "BrazierRested.asset");

            var wax = root.AddComponent<WaxSystem>();
            SetRef(wax, "config", playerConfig);
            SetRef(wax, "waxChanged", waxChanged);
            SetRef(wax, "playerExtinguished", extinguished);
            SetRef(wax, "waxGainRequested", waxGain);
            SetRef(wax, "brazierRested", brazierRested);
            SetRef(wax, "playerRespawned", respawned);

            var lightController = root.AddComponent<LightController>();
            SetRef(lightController, "config", Load<LightConfigSO>(DataPlayer + "LightConfig.asset"));
            SetRef(lightController, "runtime", runtime);
            SetRef(lightController, "candleLight", candleLight);
            SetRef(lightController, "flame", flame.transform);
            SetRef(lightController, "waxChanged", waxChanged);

            var dim = root.AddComponent<DimController>();
            SetRef(dim, "config", Load<DimConfigSO>(DataPlayer + "DimConfig.asset"));
            SetRef(dim, "input", input);
            SetRef(dim, "runtime", runtime);
            SetRef(dim, "waxSystem", wax);
            SetRef(dim, "lightController", lightController);

            var controller = root.AddComponent<PlayerController>();
            SetRef(controller, "config", playerConfig);
            SetRef(controller, "input", input);
            SetRef(controller, "runtime", runtime);
            SetRef(controller, "waxSystem", wax);
            SetRef(controller, "dimController", dim);
            SetRef(controller, "animator", animator);
            SetRef(controller, "bodyRenderer", bodyRenderer);
            SetRef(controller, "attackPivot", pivot.transform);
            SetRef(controller, "attackHitbox", hitbox);
            SetRef(controller, "playerRespawned", respawned);
            SetRef(controller, "playerExtinguished", extinguished);
            SetRef(controller, "brazierRested", brazierRested);

            SetRef(root.AddComponent<PlayerHurtbox>(), "playerController", controller);

            var debugKeys = root.AddComponent<WaxDebugKeys>();
            SetRef(debugKeys, "waxSystem", wax);
            SetRef(debugKeys, "playerController", controller);
            SetRef(debugKeys, "waxGainRequested", waxGain);

            var prefab = SavePrefab(root, PlayerPrefabPath);
            PlayerSwordSetup.AddSword(PlayerPrefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) ?? prefab;
        }

        // ---------- Wax pickup prefab ----------

        internal static GameObject BuildPickupPrefab(Sprite circle)
        {
            var root = new GameObject("WaxPickup");

            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.3f;

            var visual = Child(root, "Visual", Vector3.zero);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = new Color(1f, 0.7f, 0.28f);
            renderer.sortingOrder = 2;

            // Faint glow so drops can be found in the dark.
            var glow = Child(root, "Glow", Vector3.zero).AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.pointLightOuterRadius = 0.8f;
            glow.pointLightInnerRadius = 0.1f;
            glow.intensity = 0.6f;
            glow.color = new Color(1f, 0.7f, 0.35f);

            var pickup = root.AddComponent<WaxPickup>();
            SetRef(pickup, "runtime", Load<PlayerRuntimeSO>(DataPlayer + "PlayerRuntime.asset"));
            SetRef(pickup, "waxGainRequested", Load<FloatEventChannelSO>(DataEvents + "WaxGainRequested.asset"));
            SetRef(pickup, "visual", visual.transform);

            return SavePrefab(root, PickupPrefabPath);
        }

        // ---------- Dripling prefab ----------

        internal static GameObject BuildDriplingPrefab(Sprite circle, Sprite square, GameObject pickupPrefab)
        {
            var config = Load<EnemyConfigSO>(DataEnemies + "DriplingConfig.asset");
            var root = new GameObject("Dripling");

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Feet-level collider (the root sits at the Dripling's feet).
            var feet = root.AddComponent<CircleCollider2D>();
            feet.radius = 0.6f;
            feet.offset = new Vector2(0f, 0.5f);

            // Body: 0x72's mud blob, animated, standing on the root's position.
            var bodyGo = Child(root, "Body", Vector3.zero);
            var bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            var mudBlob = DungeonArtImporter.DriplingSprite;
            if (mudBlob != null)
            {
                bodyRenderer.sprite = mudBlob;
                bodyGo.AddComponent<Animator>().runtimeAnimatorController = DungeonArtImporter.DriplingController;
            }
            else
            {
                // Placeholder if the dungeon tileset is missing.
                bodyGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                bodyRenderer.sprite = circle;
                bodyRenderer.color = new Color(0.85f, 0.8f, 0.69f);
            }

            var pivot = Child(root, "AttackPivot", new Vector3(0f, 0.5f, 0f));
            var hitboxGo = Child(pivot, "AttackHitbox", new Vector3(1f, 0f, 0f));
            var hitboxCollider = hitboxGo.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector2(1f, 1f);
            var hitbox = hitboxGo.AddComponent<Hitbox>();
            SetEnum(hitbox, "team", (int)Team.Enemy);
            var preview = Child(hitboxGo, "StrikePreview", Vector3.zero);
            preview.transform.localScale = new Vector3(1f, 1f, 1f);
            var previewRenderer = preview.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = square;
            previewRenderer.color = new Color(1f, 0.2f, 0.15f, 0.4f);
            previewRenderer.sortingOrder = 5;
            hitboxGo.SetActive(false);

            var damageable = root.AddComponent<Damageable>();
            SetEnum(damageable, "team", (int)Team.Enemy);
            SetRef(damageable, "flashRenderer", bodyRenderer);

            var perception = root.AddComponent<EnemyPerception>();
            SetRef(perception, "config", config);
            SetRef(perception, "runtime", Load<PlayerRuntimeSO>(DataPlayer + "PlayerRuntime.asset"));

            var brain = root.AddComponent<EnemyBrain>();
            SetRef(brain, "config", config);
            SetRef(brain, "perception", perception);
            SetRef(brain, "damageable", damageable);
            SetRef(brain, "attackPivot", pivot.transform);
            SetRef(brain, "attackHitbox", hitbox);
            SetRef(brain, "bodyRenderer", bodyRenderer);

            var dropper = root.AddComponent<WaxDropper>();
            SetRef(dropper, "config", config);
            SetRef(dropper, "damageable", damageable);
            SetRef(dropper, "pickupPrefab", pickupPrefab != null ? pickupPrefab.GetComponent<WaxPickup>() : null);

            return SavePrefab(root, DriplingPrefabPath);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Wickfall] Prefab built at {path}.");
            return prefab;
        }

        // ---------- Placeholder art ----------

        /// <summary>A plain white 16x16 sprite (1 unit), square or round, tinted per use.</summary>
        internal static Sprite CreatePlaceholderSprite(string path, bool circle)
        {
            if (!File.Exists(path))
            {
                WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        // Pixel centers within radius 8 of the middle form the disc.
                        float dx = x + 0.5f - 8f;
                        float dy = y + 0.5f - 8f;
                        bool inside = !circle || dx * dx + dy * dy <= 64f;
                        pixels[y * 16 + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = WickfallEditorUtil.PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- Helpers ----------

        private static GameObject Child(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning($"[Wickfall] Missing asset: {path}");
            return asset;
        }

        private static Sprite FirstSprite(string texturePath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            {
                if (asset is Sprite sprite && sprite.name.EndsWith("_0")) return sprite;
            }
            Debug.LogWarning($"[Wickfall] No sliced sprite found in {texturePath}");
            return null;
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[Wickfall] {target.GetType().Name} has no field '{property}'.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string property, int index)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
