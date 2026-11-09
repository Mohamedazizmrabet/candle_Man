using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Wickfall.Combat;
using Wickfall.Core;
using Wickfall.Data;
using Wickfall.World;
using Object = UnityEngine.Object;

namespace Wickfall.EditorTools
{
    /// <summary>What makes one level different from another: its map, scene, exit and a few style choices.</summary>
    public class DungeonLevelSpec
    {
        public string Name;
        public string ScenePath;
        /// <summary>Top row first; one character per 2x2-unit tile. See <see cref="DungeonLevelBuilder"/> for the legend.</summary>
        public string[] Map;
        /// <summary>Scene the stairs (E) lead to.</summary>
        public string NextScene;
        /// <summary>This level is placed right after this scene in the build list.</summary>
        public string BuildAfterScenePath;
        /// <summary>Chance (0-1) that a brick wall face drips green slime.</summary>
        public float GooChance;
        /// <summary>Chance (0-1) that a brick wall face is broken.</summary>
        public float HoleChance;
        /// <summary>"red" (blood) or "blue" (water) for the F fountains.</summary>
        public string FountainColor = "red";
        public int Seed = 1;
    }

    /// <summary>
    /// Builds a dungeon level scene from a text map with the 0x72 tileset. Walls are drawn automatically from the
    /// map's shape (brick face above a floor, rim below it, thin edge lines on the sides), are solid, and sit on the
    /// "Walls" layer so enemies can't see through them. Map legend:
    ///   #  wall              (space) floor
    ///   P  player start      E  stairs to the next level     B  brazier (checkpoint)
    ///   D  Dripling          K  skeleton (fast, fragile)     G  slime (slow, tough)
    ///   I  imp (tiny, very fast, weak)                       O  Demon Lord (boss)
    ///   w  wax drop          T  chest (wax inside)           M  mimic (a chest that bites)
    ///   S  spike trap        F  fountain (put it on the floor just below a brick wall)
    ///   C  column            x  crate                        k  skull
    /// Leave at least two # rows between rooms stacked on top of each other, so the wall has room for its face.
    /// </summary>
    public static class DungeonLevelBuilder
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu/MainMenu.unity";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string DataEnemies = "Assets/Data/Enemies/";
        private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton.prefab";
        private const string SlimePrefabPath = "Assets/Prefabs/Enemies/Slime.prefab";
        private const string ImpPrefabPath = "Assets/Prefabs/Enemies/Imp.prefab";
        private const string DemonLordPrefabPath = "Assets/Prefabs/Enemies/DemonLord.prefab";

        private const int FloorOrder = -20;
        private const int FloorDecalOrder = -19;
        private const int StairsOrder = -15;
        private const int WallOrder = -10;
        private const int WallDecalOrder = -9;

        private const int BannerSpacing = 7;
        private static readonly string[] BannerSprites = { "wall_banner_red", "wall_banner_green" };

        private class Prefabs
        {
            public GameObject Player, Pickup, Brazier, Dripling, Skeleton, Slime, Imp, DemonLord;
            public Sprite Square, Circle;
        }

        // The level being built (one at a time).
        private static DungeonLevelSpec spec;
        private static Dictionary<string, Sprite> sprites;
        private static System.Random random;

        public static void Build(DungeonLevelSpec levelSpec)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            spec = levelSpec;
            if (spec.Map.Any(row => row.Length != spec.Map[0].Length))
            {
                Debug.LogError($"[Wickfall] {spec.Name}: every row of the map must have the same length.");
                return;
            }

            WickfallDataCreator.CreateDefaultData();
            CandlemanSpriteImporter.SetupAll();
            DungeonArtImporter.ImportSheet();
            sprites = DungeonArtImporter.Sprites();
            if (!sprites.ContainsKey("floor_1"))
            {
                Debug.LogError("[Wickfall] The dungeon tileset is missing; expected it at " + DungeonArtImporter.SheetPath);
                return;
            }

            UseYSorting();
            random = new System.Random(spec.Seed);
            var prefabs = BuildPrefabs();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TestSceneBuilder.CreateCamera();
            TestSceneBuilder.CreateGlobalLight();

            BuildTiles();
            PlaceThings(prefabs);
            WickfallSceneTools.CreateWaxHud();
            if (spec.Map.Any(row => row.Contains('O'))) LevelExtrasBuilder.CreateBossHealthBar();
            LevelExtrasBuilder.CreatePauseMenu();

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(spec.ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, spec.ScenePath);
            AddToBuild();
            Debug.Log($"[Wickfall] {spec.Name} built at {spec.ScenePath}.");
        }

        // ---------- Prefabs ----------

        private static Prefabs BuildPrefabs()
        {
            var p = new Prefabs
            {
                Square = TestSceneBuilder.CreatePlaceholderSprite(TestSceneBuilder.SquarePath, circle: false),
                Circle = TestSceneBuilder.CreatePlaceholderSprite(TestSceneBuilder.CirclePath, circle: true),
            };
            p.Player = TestSceneBuilder.LoadOrBuildPlayerPrefab(p.Square);
            PlayerSwordSetup.AddSword(TestSceneBuilder.PlayerPrefabPath);
            p.Pickup = AssetDatabase.LoadAssetAtPath<GameObject>(TestSceneBuilder.PickupPrefabPath);
            if (p.Pickup == null) p.Pickup = TestSceneBuilder.BuildPickupPrefab(p.Circle);
            p.Brazier = LevelExtrasBuilder.BuildBrazierPrefab();

            // Enemies are rebuilt every time so they always match the current scripts and art.
            p.Dripling = TestSceneBuilder.BuildDriplingPrefab(p.Circle, p.Square, p.Pickup);
            p.Skeleton = BuildEnemy("Skeleton", "skelet_run_anim_f", SkeletonPrefabPath, p, c =>
            {
                c.maxHealth = 20f;
                c.attackDamage = 8f;
                c.wanderSpeed = 1.4f;
                c.chaseSpeed = 3.4f;
                c.returnSpeed = 2.2f;
                c.attackWindup = 0.3f;
                c.attackCooldown = 1f;
                c.waxDropMin = 3;
                c.waxDropMax = 8;
            });
            p.Slime = BuildEnemy("Slime", "swampy_anim_f", SlimePrefabPath, p, c =>
            {
                c.maxHealth = 45f;
                c.attackDamage = 14f;
                c.wanderSpeed = 0.6f;
                c.chaseSpeed = 1.6f;
                c.returnSpeed = 1.2f;
                c.attackWindup = 0.55f;
                c.attackCooldown = 1.4f;
                c.waxDropMin = 8;
                c.waxDropMax = 18;
            });
            p.Imp = BuildEnemy("Imp", "imp_run_anim_f", ImpPrefabPath, p, c =>
            {
                c.maxHealth = 12f;
                c.attackDamage = 6f;
                c.wanderSpeed = 1.8f;
                c.chaseSpeed = 4.2f;
                c.returnSpeed = 2.5f;
                c.attackWindup = 0.25f;
                c.attackCooldown = 0.8f;
                c.waxDropMin = 2;
                c.waxDropMax = 5;
            });
            p.DemonLord = BuildEnemy("DemonLord", "big_demon_run_anim_f", DemonLordPrefabPath, p, c =>
            {
                c.maxHealth = 220f;
                c.attackDamage = 22f;
                c.wanderSpeed = 0.8f;
                c.chaseSpeed = 2f;
                c.returnSpeed = 1.5f;
                c.wanderRadius = 2f;
                c.attackRange = 2.8f;
                c.attackWindup = 0.7f;
                c.attackCooldown = 1.6f;
                c.memoryDuration = 8f;
                c.leashDistance = 18f;
                c.waxDropMin = 40;
                c.waxDropMax = 60;
            }, size: 2f);
            LevelExtrasBuilder.MakeBoss(DemonLordPrefabPath, "Demon Lord");
            p.DemonLord = AssetDatabase.LoadAssetAtPath<GameObject>(DemonLordPrefabPath);
            return p;
        }

        /// <param name="defaults">Applied only when the config asset is first created; later edits in the Inspector are kept.</param>
        private static GameObject BuildEnemy(string enemyName, string framePrefix, string prefabPath, Prefabs p, Action<EnemyConfigSO> defaults, float size = 1f)
        {
            string configPath = $"{DataEnemies}{enemyName}Config.asset";
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfigSO>();
                defaults(config);
                WickfallEditorUtil.EnsureFolder(DataEnemies.TrimEnd('/'));
                AssetDatabase.CreateAsset(config, configPath);
            }

            var art = DungeonArtImporter.EnemyArt(enemyName, framePrefix, 4);
            return TestSceneBuilder.BuildEnemyPrefab(enemyName, config, art.sprite, art.controller, p.Circle, p.Square, p.Pickup, prefabPath, size);
        }

        // ---------- Map ----------

        private static int Width => spec.Map[0].Length;
        private static int Height => spec.Map.Length;

        private static bool Solid(int col, int row) =>
            col < 0 || row < 0 || col >= Width || row >= Height || spec.Map[row][col] == '#';

        /// <summary>Map rows count from the top; tilemap cells count from the bottom.</summary>
        private static Vector3Int Cell(int col, int row) => new Vector3Int(col, Height - 1 - row, 0);

        /// <summary>Which wall sprite a solid cell shows, from its neighbours (null = plain darkness).</summary>
        private static string WallSprite(int col, int row)
        {
            if (!Solid(col, row)) return null;
            if (!Solid(col, row + 1)) return "wall_mid";
            if (!Solid(col, row - 1)) return "wall_top_mid";
            if (Solid(col, row + 1) && !Solid(col, row + 2)) return "wall_top_mid";
            if (Solid(col, row - 1) && !Solid(col, row - 2)) return "wall_mid";
            if (!Solid(col + 1, row)) return "wall_edge_mid_right";
            if (!Solid(col - 1, row)) return "wall_edge_mid_left";
            return null;
        }

        private static bool TouchesFloorDiagonally(int col, int row) =>
            !Solid(col - 1, row - 1) || !Solid(col + 1, row - 1) ||
            !Solid(col - 1, row + 1) || !Solid(col + 1, row + 1);

        private static bool IsFountainAbove(int col, int row) =>
            row + 1 < Height && spec.Map[row + 1][col] == 'F' || row + 2 < Height && spec.Map[row + 2][col] == 'F';

        private static void BuildTiles()
        {
            var grid = new GameObject("Grid", typeof(Grid));
            grid.GetComponent<Grid>().cellSize = new Vector3(DungeonArtImporter.TileWorldSize, DungeonArtImporter.TileWorldSize, 0f);

            var floor = NewTilemap(grid.transform, "Floor", FloorOrder, solid: false);
            var walls = NewTilemap(grid.transform, "Walls", WallOrder, solid: true);
            int wallsLayer = WickfallEditorUtil.EnsureLayer("Walls");
            if (wallsLayer >= 0) walls.gameObject.layer = wallsLayer;

            var decor = new GameObject("WallDecor").transform;
            int faces = 0;

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (!Solid(col, row))
                    {
                        string name = random.NextDouble() < 0.7 ? "floor_1" : $"floor_{2 + random.Next(7)}";
                        floor.SetTile(Cell(col, row), TileFor(name));
                        continue;
                    }

                    string wall = WallSprite(col, row);
                    if (wall == null)
                    {
                        // Corner cells that only touch the floor diagonally have no sprite. Without a
                        // collider there, the player can slip through the gap between two walls.
                        if (TouchesFloorDiagonally(col, row))
                            walls.SetTile(Cell(col, row), DungeonArtImporter.GetBlockerTile());
                        continue;
                    }

                    bool faceOnFloor = wall == "wall_mid" && !Solid(col, row + 1) && !IsFountainAbove(col, row);
                    if (faceOnFloor)
                    {
                        double roll = random.NextDouble();
                        if (roll < spec.GooChance)
                        {
                            wall = "wall_goo";
                            // The slime pools on the floor at the foot of the wall.
                            var below = Cell(col, row + 1);
                            Decal("wall_goo_base", CellCenter(below.x, below.y), FloorDecalOrder, decor);
                        }
                        else if (roll < spec.GooChance + spec.HoleChance)
                        {
                            wall = random.Next(2) == 0 ? "wall_hole_1" : "wall_hole_2";
                        }
                        else if (++faces % BannerSpacing == 3)
                        {
                            var cell = Cell(col, row);
                            Decal(BannerSprites[(faces / BannerSpacing) % BannerSprites.Length], CellCenter(cell.x, cell.y), WallDecalOrder, decor);
                        }
                    }

                    // Every drawn wall cell is fully solid.
                    walls.SetTile(Cell(col, row), TileFor(wall, Tile.ColliderType.Grid));
                }
            }
        }

        private static void PlaceThings(Prefabs p)
        {
            var props = new GameObject("Props").transform;
            var enemies = new GameObject("Enemies").transform;
            var traps = new GameObject("Traps").transform;
            var counts = new Dictionary<string, int>();

            string Numbered(string baseName)
            {
                counts.TryGetValue(baseName, out int n);
                counts[baseName] = ++n;
                return $"{baseName} ({n})";
            }

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    var cell = Cell(col, row);
                    var feet = CellBottom(cell.x, cell.y) + Vector3.up * 0.3f;
                    var center = CellCenter(cell.x, cell.y);

                    switch (spec.Map[row][col])
                    {
                        case 'P': Spawn(p.Player, feet, null, "Player"); break;
                        case 'D': Spawn(p.Dripling, feet, enemies, Numbered("Dripling")); break;
                        case 'K': Spawn(p.Skeleton, feet, enemies, Numbered("Skeleton")); break;
                        case 'G': Spawn(p.Slime, feet, enemies, Numbered("Slime")); break;
                        case 'I': Spawn(p.Imp, feet, enemies, Numbered("Imp")); break;
                        case 'O': Spawn(p.DemonLord, feet, enemies, "Demon Lord"); break;
                        case 'B': Spawn(p.Brazier, feet, props, Numbered("Brazier")); break;
                        case 'w': Spawn(p.Pickup, center, props, Numbered("WaxPickup")); break;
                        case 'E': CreateExit(center); break;
                        case 'T': CreateChest(feet, props, mimic: false, p); break;
                        case 'M': CreateChest(feet, props, mimic: true, p); break;
                        case 'S': CreateSpikes(center, traps, col, row); break;
                        case 'F': CreateFountain(col, row, props); break;
                        case 'C': SolidBase(NewSprite("column", feet, props), new Vector2(1.6f, 1f)); break;
                        case 'x': SolidBase(NewSprite("crate", feet, props), new Vector2(1.8f, 1.2f)); break;
                        case 'k': NewSprite("skull", feet, props); break;
                    }
                }
            }
        }

        // ---------- Special pieces ----------

        /// <summary>Stairs down: walking onto them goes to the next level.</summary>
        private static void CreateExit(Vector3 center)
        {
            var stairs = Decal("floor_stairs", center, StairsOrder, null);
            stairs.name = $"Exit_To_{spec.NextScene}";

            var trigger = stairs.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.4f, 1.4f);

            var exit = stairs.AddComponent<LevelExit>();
            SetString(exit, "nextScene", spec.NextScene);
        }

        /// <summary>Floor spikes that rise and sink on a loop; neighbours fire one after another.</summary>
        private static void CreateSpikes(Vector3 center, Transform parent, int col, int row)
        {
            var frames = DungeonArtImporter.Frames("floor_spikes_anim_f", 4);
            var go = Decal("floor_spikes_anim_f0", center, FloorDecalOrder, parent);
            go.name = "SpikeTrap";

            var hitboxGo = new GameObject("Hitbox");
            hitboxGo.transform.SetParent(go.transform, false);
            var box = hitboxGo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 1.5f);
            var hitbox = hitboxGo.AddComponent<Hitbox>();
            var hitboxSo = new SerializedObject(hitbox);
            hitboxSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
            hitboxSo.FindProperty("damage").floatValue = 10f;
            hitboxSo.FindProperty("owner").objectReferenceValue = go.transform;
            hitboxSo.ApplyModifiedPropertiesWithoutUndo();
            hitboxGo.SetActive(false);

            var trap = go.AddComponent<SpikeTrap>();
            var so = new SerializedObject(trap);
            SetArray(so.FindProperty("frames"), frames);
            so.FindProperty("hitbox").objectReferenceValue = hitbox;
            // Diagonal ripple across a field of spikes.
            so.FindProperty("phaseOffset").floatValue = (col + row) * 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A chest (wax inside) or a mimic (bites, fights back, drops more wax when killed).</summary>
        private static void CreateChest(Vector3 feet, Transform parent, bool mimic, Prefabs p)
        {
            string prefix = mimic ? "chest_mimic_open_anim_f" : "chest_full_open_anim_f";
            var frames = DungeonArtImporter.Frames(prefix, 3);

            var go = NewSprite(prefix + "0", feet, parent);
            go.name = mimic ? "Chest (mimic)" : "Chest";
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            SolidBase(go, new Vector2(1.8f, 1.2f));

            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var promptRenderer = promptGo.AddComponent<SpriteRenderer>();
            promptRenderer.sprite = BrazierArtGenerator.Sprite(BrazierArtGenerator.PromptPath, "prompt_e");
            promptRenderer.sortingOrder = 50;

            Hitbox bite = null;
            Damageable damageable = null;
            if (mimic)
            {
                var biteGo = new GameObject("Bite");
                biteGo.transform.SetParent(go.transform, false);
                biteGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                var box = biteGo.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(2.6f, 2.2f);
                bite = biteGo.AddComponent<Hitbox>();
                var biteSo = new SerializedObject(bite);
                biteSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
                biteSo.FindProperty("damage").floatValue = 15f;
                biteSo.FindProperty("owner").objectReferenceValue = go.transform;
                biteSo.ApplyModifiedPropertiesWithoutUndo();
                biteGo.SetActive(false);

                damageable = go.AddComponent<Damageable>();
                var dSo = new SerializedObject(damageable);
                dSo.FindProperty("team").enumValueIndex = (int)Team.Enemy;
                dSo.FindProperty("maxHealth").floatValue = 30f;
                dSo.FindProperty("flashRenderer").objectReferenceValue = spriteRenderer;
                dSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var chest = go.AddComponent<Chest>();
            var so = new SerializedObject(chest);
            so.FindProperty("runtime").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerRuntimeSO>("Assets/Data/Player/PlayerRuntime.asset");
            so.FindProperty("input").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputReaderSO>("Assets/Data/Player/InputReader.asset");
            so.FindProperty("isMimic").boolValue = mimic;
            SetArray(so.FindProperty("frames"), frames);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("prompt").objectReferenceValue = promptGo.transform;
            so.FindProperty("pickupPrefab").objectReferenceValue = p.Pickup != null ? p.Pickup.GetComponent<WaxPickup>() : null;
            so.FindProperty("bite").objectReferenceValue = bite;
            so.FindProperty("damageable").objectReferenceValue = damageable;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// A fountain built into the brick wall above the F cell: spout on the face, top piece on the rim,
        /// basin on the floor, all animated, glowing red (blood) or blue (water).
        /// </summary>
        private static void CreateFountain(int col, int row, Transform parent)
        {
            bool water = spec.FountainColor == "blue";
            string color = water ? "blue" : "red";
            var root = new GameObject(water ? "WaterFountain" : "BloodFountain").transform;
            root.SetParent(parent, false);

            var basinCell = Cell(col, row);
            var faceCell = Cell(col, row - 1);
            var rimCell = Cell(col, row - 2);

            Flipbook(Decal($"wall_fountain_basin_{color}_anim_f0", CellCenter(basinCell.x, basinCell.y), FloorDecalOrder, root),
                DungeonArtImporter.Frames($"wall_fountain_basin_{color}_anim_f", 3));
            Flipbook(Decal($"wall_fountain_mid_{color}_anim_f0", CellCenter(faceCell.x, faceCell.y), WallDecalOrder, root),
                DungeonArtImporter.Frames($"wall_fountain_mid_{color}_anim_f", 3));
            Decal("wall_fountain_top_1", CellCenter(rimCell.x, rimCell.y), WallDecalOrder, root);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root, false);
            glowGo.transform.position = CellCenter(basinCell.x, basinCell.y);
            var glow = glowGo.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.pointLightOuterRadius = 4f;
            glow.pointLightInnerRadius = 0.5f;
            glow.intensity = 0.7f;
            glow.color = water ? new Color(0.3f, 0.55f, 1f) : new Color(1f, 0.25f, 0.15f);

            // The basin is solid: walk around it, not through it.
            var basinCollider = root.gameObject.AddComponent<BoxCollider2D>();
            basinCollider.offset = CellCenter(basinCell.x, basinCell.y) - root.position;
            basinCollider.size = new Vector2(1.6f, 1.2f);
        }

        private static void Flipbook(GameObject go, Sprite[] frames)
        {
            var flipbook = go.AddComponent<SpriteFlipbook>();
            var so = new SerializedObject(flipbook);
            SetArray(so.FindProperty("frames"), frames);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Helpers ----------

        private static Vector3 CellBottom(int x, int y) =>
            new Vector3((x + 0.5f) * DungeonArtImporter.TileWorldSize, y * DungeonArtImporter.TileWorldSize, 0f);

        private static Vector3 CellCenter(int x, int y) => CellBottom(x, y) + Vector3.up * (DungeonArtImporter.TileWorldSize / 2f);

        private static TileBase TileFor(string name, Tile.ColliderType collider = Tile.ColliderType.None) =>
            DungeonArtImporter.GetTile(name, collider);

        private static Tilemap NewTilemap(Transform grid, string name, int order, bool solid)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;

            if (solid)
            {
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                go.AddComponent<CompositeCollider2D>();
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            return go.GetComponent<Tilemap>();
        }

        /// <summary>A standing sprite (feet pivot), sorted against characters by height.</summary>
        private static GameObject NewSprite(string spriteName, Vector3 position, Transform parent = null)
        {
            var go = new GameObject(spriteName);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            sprites.TryGetValue(spriteName, out var sprite);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            return go;
        }

        /// <summary>A flat sprite on the floor or a wall, drawn at a fixed order (never in front of characters).</summary>
        private static GameObject Decal(string spriteName, Vector3 position, int order, Transform parent)
        {
            var go = NewSprite(spriteName, position, parent);
            go.GetComponent<SpriteRenderer>().sortingOrder = order;
            return go;
        }

        /// <summary>A collider on the object's base only, so the player can walk behind its top part.</summary>
        private static void SolidBase(GameObject go, Vector2 baseSize)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = baseSize;
            box.offset = new Vector2(0f, baseSize.y / 2f);
        }

        private static void Spawn(GameObject prefab, Vector3 position, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.name = name;
            go.transform.position = position;
        }

        private static void SetArray(SerializedProperty array, Sprite[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetString(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Top-down draw order: lower on screen is drawn in front (sorted by each sprite's feet).</summary>
        private static void UseYSorting()
        {
            var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Renderer2DPath);
            if (renderer2D == null) return;

            var so = new SerializedObject(renderer2D);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode == null || axis == null) return;

            mode.intValue = (int)TransparencySortMode.CustomAxis;
            axis.vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Puts this level right after the previous one (or the main menu) in the build list.</summary>
        private static void AddToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != spec.ScenePath).ToList();
            int after = scenes.FindIndex(s => s.path == (spec.BuildAfterScenePath ?? MainMenuPath));
            if (after < 0) after = scenes.FindIndex(s => s.path == MainMenuPath);
            scenes.Insert(after + 1, new EditorBuildSettingsScene(spec.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
