using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Imports the free 0x72 "16x16 DungeonTileset II" (CC0) sheet: slices every sprite by the names in its
    /// tile list, makes Tile assets for the floor and walls, and builds enemy animations from its frames.
    /// The sheet is drawn at double size (8 pixels per unit, so one 16px tile = 2 units): at that scale
    /// Candleman (56px at 16 PPU) is about 1.75 tiles tall, like 0x72's own characters.
    /// </summary>
    public static class DungeonArtImporter
    {
        public const string SheetPath = "Assets/Art/Tilesets/Dungeon_0x72/dungeon_0x72.png";
        private const string ListPath = "Assets/Art/Tilesets/Dungeon_0x72/dungeon_0x72_tiles.txt";
        private const string TileDir = "Assets/Tilemaps/Dungeon_0x72";
        private const string EnemyAnimDir = "Assets/Animations/Enemies";

        public const float PixelsPerUnit = 8f;
        public const float TileWorldSize = 16f / PixelsPerUnit; // 2 units

        [MenuItem("Tools/Wickfall/Art/Import Dungeon Tileset (0x72)", priority = 22)]
        public static void ImportSheet()
        {
            if (!File.Exists(ListPath))
            {
                Debug.LogError($"[Wickfall] {ListPath} not found.");
                return;
            }

            const int sheetHeight = 512;
            var slices = new List<(string, Rect, Vector2)>();
            foreach (var line in File.ReadAllLines(ListPath))
            {
                var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                int x = int.Parse(parts[1]), y = int.Parse(parts[2]), w = int.Parse(parts[3]), h = int.Parse(parts[4]);
                // The list uses a top-left origin; Unity sprites use bottom-left.
                var rect = new Rect(x, sheetHeight - y - h, w, h);
                // Floor and wall pieces sit centered in their grid cell; everything else stands on its feet.
                bool isTile = parts[0].StartsWith("floor_") || parts[0].StartsWith("wall_");
                slices.Add((parts[0], rect, isTile ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f)));
            }

            WickfallEditorUtil.ImportAndSliceRects(SheetPath, PixelsPerUnit, slices);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Wickfall] Dungeon tileset imported: {slices.Count} sprites at {PixelsPerUnit} pixels per unit.");
        }

        /// <summary>Every sprite of the sheet by name. Imports the sheet first if it was never sliced.</summary>
        public static Dictionary<string, Sprite> Sprites()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            if (sprites.Count == 0)
            {
                ImportSheet();
                sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            }
            return sprites;
        }

        /// <summary>A Tile asset for one sprite (created once, then reused).</summary>
        public static Tile GetTile(string spriteName, Tile.ColliderType collider)
        {
            string path = $"{TileDir}/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                WickfallEditorUtil.EnsureFolder(TileDir);
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            Sprites().TryGetValue(spriteName, out var sprite);
            if (sprite == null) Debug.LogWarning($"[Wickfall] No sprite named {spriteName} in the dungeon sheet.");
            tile.sprite = sprite;
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>
        /// An invisible, fully solid tile: blocks movement but draws nothing. Used on wall corners that
        /// have no sprite, so the wall collision has no gaps.
        /// </summary>
        public static Tile GetBlockerTile()
        {
            string path = $"{TileDir}/wall_blocker.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                WickfallEditorUtil.EnsureFolder(TileDir);
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = null;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>
        /// A looping animation for an enemy from numbered frames of the sheet (prefix0, prefix1...),
        /// saved as Animations/Enemies/{name}_Loop.anim with its own controller. Returns the first frame too.
        /// </summary>
        public static (Sprite sprite, RuntimeAnimatorController controller) EnemyArt(string enemyName, string framePrefix, int frameCount, float fps = 8f)
        {
            var sprites = Sprites();
            var frames = Enumerable.Range(0, frameCount)
                .Select(i => sprites.TryGetValue($"{framePrefix}{i}", out var s) ? s : null)
                .Where(s => s != null)
                .ToArray();
            if (frames.Length == 0)
            {
                Debug.LogWarning($"[Wickfall] No frames named {framePrefix}0.. in the dungeon sheet.");
                return (null, null);
            }

            var clip = WickfallEditorUtil.CreateSpriteClip($"{EnemyAnimDir}/{enemyName}_Loop.anim", frames, fps);
            var controller = WickfallEditorUtil.LoadOrCreateController($"{EnemyAnimDir}/{enemyName}.controller", out bool created);
            if (created) controller.layers[0].stateMachine.AddState("Loop").motion = clip;
            return (frames[0], controller);
        }

        /// <summary>Numbered frames of the sheet (prefix0, prefix1...), for props that animate with SpriteFlipbook.</summary>
        public static Sprite[] Frames(string framePrefix, int frameCount)
        {
            var sprites = Sprites();
            return Enumerable.Range(0, frameCount)
                .Select(i => sprites.TryGetValue($"{framePrefix}{i}", out var s) ? s : null)
                .Where(s => s != null)
                .ToArray();
        }
    }
}
