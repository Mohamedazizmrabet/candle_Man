using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Level > Build Level 01": builds Level_01 as a dark dungeon room from the 0x72 tileset
    /// (floor, walls with colliders, columns, crates, banners), with the player, Driplings, a wax drop, camera and HUD.
    /// Edit <see cref="Room"/> below to move things around, then run the menu again.
    /// </summary>
    public static class LevelOneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Regions/01_ChampsDeSuif/Level_01.unity";
        private const string MainMenuPath = "Assets/Scenes/MainMenu/MainMenu.unity";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

        // The room's inside, top row first. One character = one 2x2-unit tile. Walls are added all around.
        //   P player start   D Dripling   w wax drop   C column   x crate   k skull   (space) empty floor
        private static readonly string[] Room =
        {
            "                      ",
            "  C      D        C   ",
            "                      ",
            "                  x   ",
            "      k               ",
            "  C               C   ",
            "                      ",
            "           w          ",
            "    D                 ",
            "                    k ",
            "          P           ",
            "                      ",
        };

        // Banners hanging on the top wall, by column (0 = first inside column).
        private static readonly (int column, string sprite)[] Banners =
        {
            (3, "wall_banner_red"), (8, "wall_banner_green"), (14, "wall_banner_red"), (19, "wall_banner_green"),
        };

        private const int FloorOrder = -20;
        private const int WallOrder = -10;
        private const int BannerOrder = -9;

        [MenuItem("Tools/Wickfall/Level/Build Level 01")]
        public static void BuildLevelOne()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            WickfallDataCreator.CreateDefaultData();
            CandlemanSpriteImporter.SetupAll();
            DungeonArtImporter.ImportSheet();
            var sprites = DungeonArtImporter.Sprites();
            if (!sprites.ContainsKey("floor_1"))
            {
                Debug.LogError("[Wickfall] The dungeon tileset is missing; expected it at " + DungeonArtImporter.SheetPath);
                return;
            }

            UseYSorting();

            var square = TestSceneBuilder.CreatePlaceholderSprite(TestSceneBuilder.SquarePath, circle: false);
            var circle = TestSceneBuilder.CreatePlaceholderSprite(TestSceneBuilder.CirclePath, circle: true);
            var playerPrefab = TestSceneBuilder.LoadOrBuildPlayerPrefab(square);
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestSceneBuilder.PickupPrefabPath)
                               ?? TestSceneBuilder.BuildPickupPrefab(circle);
            // Always rebuilt here so the Dripling gets the dungeon art.
            var driplingPrefab = TestSceneBuilder.BuildDriplingPrefab(circle, square, pickupPrefab);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TestSceneBuilder.CreateCamera();
            TestSceneBuilder.CreateGlobalLight();

            BuildRoom(sprites);
            PlaceThings(sprites, playerPrefab, driplingPrefab, pickupPrefab);
            WickfallSceneTools.CreateWaxHud();

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuild();
            Debug.Log($"[Wickfall] Level 01 built at {ScenePath}. Press Play, or start from the main menu.");
        }

        // ---------- Room ----------

        private static int Width => Room[0].Length;
        private static int Height => Room.Length;

        /// <summary>Floor everywhere inside, walls around: two rows on top (rim and face) and bottom, thin edges on the sides.</summary>
        private static void BuildRoom(Dictionary<string, Sprite> sprites)
        {
            var grid = new GameObject("Grid", typeof(Grid));
            grid.GetComponent<Grid>().cellSize = new Vector3(DungeonArtImporter.TileWorldSize, DungeonArtImporter.TileWorldSize, 0f);

            var floor = NewTilemap(grid.transform, "Floor", FloorOrder, solid: false);
            var walls = NewTilemap(grid.transform, "Walls", WallOrder, solid: true);

            // Cell coordinates: inside spans x 1..Width, y 2..Height+1. The wall ring sits around it.
            int left = 0, right = Width + 1, bottomRim = 1, bottomFace = 0, topFace = Height + 2, topRim = Height + 3;

            var random = new System.Random(1);
            for (int x = left; x <= right; x++)
            {
                for (int y = bottomRim; y <= Height + 1; y++)
                {
                    string name = random.NextDouble() < 0.7 ? "floor_1" : $"floor_{2 + random.Next(7)}";
                    floor.SetTile(new Vector3Int(x, y, 0), TileFor(name, Tile.ColliderType.None));
                }
            }

            for (int x = left; x <= right; x++)
            {
                bool l = x == left, r = x == right;
                walls.SetTile(new Vector3Int(x, topRim, 0), TileFor(l ? "wall_top_left" : r ? "wall_top_right" : "wall_top_mid", Tile.ColliderType.Grid));
                walls.SetTile(new Vector3Int(x, topFace, 0), TileFor(l ? "wall_left" : r ? "wall_right" : "wall_mid", Tile.ColliderType.Grid));
                walls.SetTile(new Vector3Int(x, bottomRim, 0), TileFor(l ? "wall_edge_bottom_left" : r ? "wall_edge_bottom_right" : "wall_top_mid", Tile.ColliderType.Grid));
                walls.SetTile(new Vector3Int(x, bottomFace, 0), TileFor(l ? "wall_left" : r ? "wall_right" : "wall_mid", Tile.ColliderType.Grid));
            }

            for (int y = bottomRim + 1; y < topFace; y++)
            {
                // Side walls are thin lines on the outer edge of the cell: collide with the line, not the whole cell.
                walls.SetTile(new Vector3Int(left, y, 0), TileFor("wall_edge_mid_left", Tile.ColliderType.Sprite));
                walls.SetTile(new Vector3Int(right, y, 0), TileFor("wall_edge_mid_right", Tile.ColliderType.Sprite));
            }

            foreach (var (column, spriteName) in Banners)
            {
                var banner = NewSprite(spriteName, sprites, CellBottom(column + 1, topFace));
                banner.GetComponent<SpriteRenderer>().sortingOrder = BannerOrder;
            }
        }

        private static void PlaceThings(Dictionary<string, Sprite> sprites, GameObject player, GameObject dripling, GameObject pickup)
        {
            var props = new GameObject("Props").transform;
            var enemies = new GameObject("Enemies").transform;
            int driplingCount = 0;

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    // Room row 0 is the top inside row.
                    var cell = new Vector2Int(col + 1, Height + 1 - row);
                    var feet = CellBottom(cell.x, cell.y) + Vector3.up * 0.3f;

                    switch (Room[row][col])
                    {
                        case 'P':
                            Spawn(player, feet, null, "Player");
                            break;
                        case 'D':
                            Spawn(dripling, feet, enemies, $"Dripling ({++driplingCount})");
                            break;
                        case 'w':
                            Spawn(pickup, CellCenter(cell.x, cell.y), props, "WaxPickup");
                            break;
                        case 'C':
                            Solid(NewSprite("column", sprites, feet, props), new Vector2(1.6f, 1f));
                            break;
                        case 'x':
                            Solid(NewSprite("crate", sprites, feet, props), new Vector2(1.8f, 1.2f));
                            break;
                        case 'k':
                            NewSprite("skull", sprites, feet, props);
                            break;
                    }
                }
            }
        }

        // ---------- Helpers ----------

        private static Vector3 CellBottom(int x, int y) =>
            new Vector3((x + 0.5f) * DungeonArtImporter.TileWorldSize, y * DungeonArtImporter.TileWorldSize, 0f);

        private static Vector3 CellCenter(int x, int y) => CellBottom(x, y) + Vector3.up * (DungeonArtImporter.TileWorldSize / 2f);

        private static TileBase TileFor(string name, Tile.ColliderType collider) => DungeonArtImporter.GetTile(name, collider);

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

        private static GameObject NewSprite(string spriteName, Dictionary<string, Sprite> sprites, Vector3 position, Transform parent = null)
        {
            var go = new GameObject(spriteName);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            sprites.TryGetValue(spriteName, out var sprite);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            return go;
        }

        /// <summary>A collider on the object's base only, so the player can walk behind its top part.</summary>
        private static void Solid(GameObject go, Vector2 baseSize)
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

        /// <summary>
        /// Top-down draw order: lower on screen is drawn in front, so Candleman can walk behind columns and crates.
        /// Sprites are sorted by their pivot, which sits at their feet.
        /// </summary>
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

        /// <summary>Main menu first, then Level 01 (New Game and Continue load it by name).</summary>
        private static void AddToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int index = scenes.FindIndex(s => s.path == MainMenuPath) + 1;
            scenes.Insert(index, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
