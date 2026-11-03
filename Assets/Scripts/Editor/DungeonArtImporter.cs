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
    /// tile list, makes Tile assets for the floor and walls, and builds the Dripling animation from "muddy".
    /// The sheet is drawn at double size (8 pixels per unit, so one 16px tile = 2 units): at that scale
    /// Candleman (56px at 16 PPU) is about 1.75 tiles tall, like 0x72's own characters.
    /// </summary>
    public static class DungeonArtImporter
    {
        public const string SheetPath = "Assets/Art/Tilesets/Dungeon_0x72/dungeon_0x72.png";
        private const string ListPath = "Assets/Art/Tilesets/Dungeon_0x72/dungeon_0x72_tiles.txt";
        private const string TileDir = "Assets/Tilemaps/Dungeon_0x72";
        private const string EnemyAnimDir = "Assets/Animations/Enemies";
        private const string DriplingClipPath = EnemyAnimDir + "/Dripling_Idle.anim";
        private const string DriplingControllerPath = EnemyAnimDir + "/Dripling.controller";

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
            BuildDriplingAnimation();
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

        public static RuntimeAnimatorController DriplingController =>
            AssetDatabase.LoadAssetAtPath<AnimatorController>(DriplingControllerPath);

        public static Sprite DriplingSprite => Sprites().TryGetValue("muddy_anim_f0", out var s) ? s : null;

        /// <summary>The Dripling uses 0x72's mud blob ("muddy"), 4-frame loop.</summary>
        private static void BuildDriplingAnimation()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            var frames = Enumerable.Range(0, 4)
                .Select(i => sprites.TryGetValue($"muddy_anim_f{i}", out var s) ? s : null)
                .Where(s => s != null)
                .ToArray();
            if (frames.Length == 0) return;

            var clip = WickfallEditorUtil.CreateSpriteClip(DriplingClipPath, frames, 8f);
            var controller = WickfallEditorUtil.LoadOrCreateController(DriplingControllerPath, out bool created);
            if (created) controller.layers[0].stateMachine.AddState("Idle").motion = clip;
        }
    }
}
