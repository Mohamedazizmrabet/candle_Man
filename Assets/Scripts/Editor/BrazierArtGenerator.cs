using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Art > Create Brazier Art": draws the brazier (16x16 frames, same scale as the
    /// 0x72 dungeon art) and the "E" interact prompt, then builds the Unlit/Lit animator. Replace the PNGs with
    /// your own art later; keep the frame layout (5 frames of 16x16: unlit, then 4 lit) and run this again.
    /// </summary>
    public static class BrazierArtGenerator
    {
        public const string BrazierPath = "Assets/Art/Sprites/Props/brazier.png";
        public const string PromptPath = "Assets/Art/Sprites/Props/prompt_e.png";
        private const string AnimDir = "Assets/Animations/Props";
        public const string ControllerPath = AnimDir + "/Brazier.controller";
        public const string LitParameter = "Lit";

        // Iron bowl on a short stand, shared by every frame (bottom 8 rows).
        private static readonly string[] Base =
        {
            ".##############.",
            ".#hmmmmmmmmmmh#.",
            "..#mmmmmmmmmm#..",
            "...#mmmmmmmm#...",
            "....##mmmm##....",
            "......#mm#......",
            ".....#hmmh#.....",
            "....########....",
        };

        // Top 8 rows: cold ash, then four flame drawings for the lit loop.
        private static readonly string[] Ash =
        {
            "................", "................", "................", "................",
            "................", "................", "......aaaa......", "...aaaeaaaaaa...",
        };

        private static readonly string[][] Flames =
        {
            new[] { "........y.......", ".......yo.......", "......yoo...y...", ".....yorro.yo...", "....oyrrroyoo...", "...oyrwwrryoyo..", "...oywwwwryyoo..", "..ooyywwwwyyoo.." },
            new[] { "......y.........", ".....yo.........", ".....oo...y.....", "....yrroo.oy....", "...oyrrryooo....", "...oyrwwrryoo...", "..ooywwwwryyo...", "..ooyywwwwyyoo.." },
            new[] { ".........y......", "........yo......", "....y...ooy.....", "...yo..yrro.....", "...ooyyrrroy....", "...oyrwwrryoo...", "..ooywwwwryyoo..", "..ooyywwwwyyoo.." },
            new[] { "...........y....", "...........oy...", ".....y....ooy...", "....oyo..orry...", "....oyrrrroyo...", "...oyrwwrryyoo..", "..ooywwwwryyoo..", "..ooyywwwwyyoo.." },
        };

        // A small key cap with an E: the interact key (same place on AZERTY and QWERTY).
        private static readonly string[] PromptE =
        {
            ".#######.", "#ccccccc#", "#cc###cc#", "#cc#cccc#", "#cc##ccc#", "#cc#cccc#", "#cc###cc#", "#ccccccc#", ".#######.",
        };

        private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            ['#'] = new Color32(34, 30, 34, 255),
            ['m'] = new Color32(78, 72, 78, 255),
            ['h'] = new Color32(125, 118, 120, 255),
            ['a'] = new Color32(95, 90, 88, 255),
            ['e'] = new Color32(150, 60, 30, 255),
            ['r'] = new Color32(205, 55, 20, 255),
            ['o'] = new Color32(250, 135, 30, 255),
            ['y'] = new Color32(255, 210, 90, 255),
            ['w'] = new Color32(255, 245, 205, 255),
            ['c'] = new Color32(240, 235, 220, 255),
        };

        [MenuItem("Tools/Wickfall/Art/Create Brazier Art", priority = 23)]
        public static void CreateBrazierArt()
        {
            var frames = new List<string[]> { Ash.Concat(Base).ToArray() };
            frames.AddRange(Flames.Select(f => f.Concat(Base).ToArray()));

            WritePng(BrazierPath, frames);
            WickfallEditorUtil.ImportAndSliceRects(BrazierPath, DungeonArtImporter.PixelsPerUnit,
                Enumerable.Range(0, frames.Count).Select(i => ($"brazier_{i}", new Rect(i * 16, 0, 16, 16), new Vector2(0.5f, 0f))));

            WritePng(PromptPath, new List<string[]> { PromptE });
            WickfallEditorUtil.ImportAndSliceRects(PromptPath, DungeonArtImporter.PixelsPerUnit,
                new[] { ("prompt_e", new Rect(0, 0, 9, 9), new Vector2(0.5f, 0f)) });

            var sprites = AssetDatabase.LoadAllAssetsAtPath(BrazierPath).OfType<Sprite>().ToDictionary(s => s.name);
            var unlit = WickfallEditorUtil.CreateSpriteClip($"{AnimDir}/Brazier_Unlit.anim", new[] { sprites["brazier_0"] }, 8f);
            var lit = WickfallEditorUtil.CreateSpriteClip($"{AnimDir}/Brazier_Lit.anim",
                new[] { sprites["brazier_1"], sprites["brazier_2"], sprites["brazier_3"], sprites["brazier_4"] }, 8f);

            var controller = WickfallEditorUtil.LoadOrCreateController(ControllerPath, out bool created);
            if (created)
            {
                controller.AddParameter(LitParameter, AnimatorControllerParameterType.Bool);
                var machine = controller.layers[0].stateMachine;
                var unlitState = machine.AddState("Unlit");
                unlitState.motion = unlit;
                var litState = machine.AddState("Lit");
                litState.motion = lit;
                machine.defaultState = unlitState;

                var toLit = unlitState.AddTransition(litState);
                toLit.hasExitTime = false;
                toLit.duration = 0f;
                toLit.AddCondition(AnimatorConditionMode.If, 0f, LitParameter);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Wickfall] Brazier art created at {BrazierPath}.");
        }

        public static Sprite Sprite(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);

        /// <summary>Draws the frames side by side (rows listed top first) into a PNG.</summary>
        private static void WritePng(string path, List<string[]> frames)
        {
            int w = frames[0][0].Length, h = frames[0].Length;
            var texture = new Texture2D(w * frames.Count, h, TextureFormat.RGBA32, false);
            for (int f = 0; f < frames.Count; f++)
            {
                for (int row = 0; row < h; row++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var color = Palette.TryGetValue(frames[f][row][x], out var c) ? c : new Color32(0, 0, 0, 0);
                        texture.SetPixel(f * w + x, h - 1 - row, color);
                    }
                }
            }

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
