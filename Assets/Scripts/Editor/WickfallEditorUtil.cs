using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>Shared helpers for the Wickfall editor menus: folders, sprite sheet slicing, sprite animation clips.</summary>
    public static class WickfallEditorUtil
    {
        public const float PixelsPerUnit = 16f;

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>
        /// Pixel art import settings (Multiple, Point, no compression, 16 PPU), then slices one horizontal row
        /// of equal frames with a bottom-center pivot. Re-running keeps each frame's ID, so clips stay linked.
        /// </summary>
        public static Sprite[] ImportAndSliceRow(string texturePath, int frameWidth, int frameHeight, int frameCount, string framePrefix)
        {
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Wickfall] Texture not found: {texturePath}");
                return new Sprite[0];
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var previousIds = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                string name = $"{framePrefix}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    spriteID = previousIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                    rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            return LoadFrames(texturePath, framePrefix, frameCount);
        }

        /// <summary>Loads the sliced sprites in frame order (prefix_0, prefix_1...).</summary>
        public static Sprite[] LoadFrames(string texturePath, string framePrefix, int frameCount)
        {
            var byName = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToDictionary(s => s.name);
            var frames = new List<Sprite>();
            for (int i = 0; i < frameCount; i++)
            {
                if (byName.TryGetValue($"{framePrefix}_{i}", out var sprite)) frames.Add(sprite);
            }
            return frames.ToArray();
        }

        /// <summary>
        /// Creates (or updates in place) a looping clip that swaps the SpriteRenderer's sprite on the same object.
        /// The last frame is repeated at the end so it is shown for a full frame before looping.
        /// </summary>
        public static AnimationClip CreateSpriteClip(string clipPath, Sprite[] frames, float framesPerSecond, bool loop = true)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                EnsureFolder(Path.GetDirectoryName(clipPath).Replace('\\', '/'));
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = framesPerSecond;
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / framesPerSecond, value = frames[i] };
            }
            keys[frames.Length] = new ObjectReferenceKeyframe
            {
                time = frames.Length / framesPerSecond,
                value = frames[frames.Length - 1],
            };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>Loads the controller at the path, or creates an empty one.</summary>
        public static AnimatorController LoadOrCreateController(string controllerPath, out bool created)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            created = controller == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(controllerPath).Replace('\\', '/'));
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }
            return controller;
        }
    }
}
