using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Art > Create Placeholder Flame": draws a tiny 3-frame flame (5x8 px each),
    /// imports it as pixel art and builds a flicker clip and animator. Replace the PNG with real art later.
    /// </summary>
    public static class FlamePlaceholderGenerator
    {
        private const string FlamePath = "Assets/Art/Sprites/Player/flame_placeholder.png";
        private const string FramePrefix = "flame_placeholder";
        private const int FrameWidth = 5;
        private const int FrameHeight = 8;
        private const float FlickerFps = 10f;

        private const string AnimDir = "Assets/Animations/Player";
        private const string ClipPath = AnimDir + "/Flame_Flicker.anim";
        private const string ControllerPath = AnimDir + "/Flame.controller";

        // Top row first. r = deep red tip, o = orange, y = yellow, w = pale core, . = transparent.
        private static readonly string[][] Frames =
        {
            new[] { "..r..", "..o..", ".ooo.", ".oyo.", "oyyyo", "oywyo", ".oyo.", "..o.." },
            new[] { ".....", ".r...", ".oo..", ".oyo.", "oyyo.", "oywyo", ".oyo.", "..o.." },
            new[] { "...r.", "..oo.", "..oo.", ".oyo.", "oyyyo", "oywyo", ".oyo.", "..o.." },
        };

        [MenuItem("Tools/Wickfall/Art/Create Placeholder Flame", priority = 21)]
        public static void CreatePlaceholderFlame()
        {
            WriteTexture();

            var frames = WickfallEditorUtil.ImportAndSliceRow(FlamePath, FrameWidth, FrameHeight, Frames.Length, FramePrefix);
            if (frames.Length != Frames.Length)
            {
                Debug.LogError($"[Wickfall] Expected {Frames.Length} flame frames, got {frames.Length}.");
                return;
            }

            // 0,1,0,2 reads as an irregular flicker with only three drawings.
            var flicker = WickfallEditorUtil.CreateSpriteClip(ClipPath, new[] { frames[0], frames[1], frames[0], frames[2] }, FlickerFps);

            var controller = WickfallEditorUtil.LoadOrCreateController(ControllerPath, out bool created);
            if (created)
            {
                var state = controller.layers[0].stateMachine.AddState("Flicker");
                state.motion = flicker;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Wickfall] Placeholder flame created at {FlamePath}, animator at {ControllerPath}.");
        }

        private static void WriteTexture()
        {
            var texture = new Texture2D(FrameWidth * Frames.Length, FrameHeight, TextureFormat.RGBA32, false);
            for (int f = 0; f < Frames.Length; f++)
            {
                for (int row = 0; row < FrameHeight; row++)
                {
                    string line = Frames[f][row];
                    for (int x = 0; x < FrameWidth; x++)
                    {
                        // Texture y goes up, rows are listed top-down.
                        texture.SetPixel(f * FrameWidth + x, FrameHeight - 1 - row, ColorFor(line[x]));
                    }
                }
            }

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(FlamePath).Replace('\\', '/'));
            File.WriteAllBytes(FlamePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FlamePath, ImportAssetOptions.ForceUpdate);
        }

        private static Color32 ColorFor(char c)
        {
            switch (c)
            {
                case 'r': return new Color32(200, 60, 20, 255);
                case 'o': return new Color32(255, 140, 30, 255);
                case 'y': return new Color32(255, 215, 90, 255);
                case 'w': return new Color32(255, 245, 200, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }
    }
}
