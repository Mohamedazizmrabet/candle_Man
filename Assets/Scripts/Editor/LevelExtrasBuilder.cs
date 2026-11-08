using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Wickfall.Core;
using Wickfall.Core.Events;
using Wickfall.UI;
using Wickfall.World;

namespace Wickfall.EditorTools
{
    /// <summary>Pieces every level needs besides the room: the Brazier prefab and the pause menu.</summary>
    public static class LevelExtrasBuilder
    {
        public const string BrazierPrefabPath = "Assets/Prefabs/Props/Brazier.prefab";
        private const string PanelSpritePath = "Assets/Art/UI/Menus/MainMenu_Panel.png";

        private static readonly Color Cream = new Color32(249, 250, 242, 255);

        /// <summary>Rebuilt every time, so it always matches the current scripts and art.</summary>
        public static GameObject BuildBrazierPrefab()
        {
            BrazierArtGenerator.CreateBrazierArt();

            var root = new GameObject("Brazier");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = BrazierArtGenerator.Sprite(BrazierArtGenerator.BrazierPath, "brazier_0");
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BrazierArtGenerator.ControllerPath);

            // Solid base, so the player stops in front of it (and can walk behind its top).
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.4f, 0.8f);
            box.offset = new Vector2(0f, 0.4f);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var glow = glowGo.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.pointLightOuterRadius = 5f;
            glow.pointLightInnerRadius = 0.8f;
            glow.color = new Color(1f, 0.65f, 0.3f);
            glow.intensity = 0f;

            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(root.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var promptRenderer = promptGo.AddComponent<SpriteRenderer>();
            promptRenderer.sprite = BrazierArtGenerator.Sprite(BrazierArtGenerator.PromptPath, "prompt_e");
            promptRenderer.sortingOrder = 50;

            var brazier = root.AddComponent<Brazier>();
            var so = new SerializedObject(brazier);
            so.FindProperty("runtime").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerRuntimeSO>("Assets/Data/Player/PlayerRuntime.asset");
            so.FindProperty("input").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputReaderSO>("Assets/Data/Player/InputReader.asset");
            so.FindProperty("brazierRested").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Vector3EventChannelSO>("Assets/Data/EventChannels/BrazierRested.asset");
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("glow").objectReferenceValue = glow;
            so.FindProperty("prompt").objectReferenceValue = promptGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(BrazierPrefabPath).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BrazierPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>EventSystem plus a hidden pause panel: Paused, Resume, Main Menu, Quit.</summary>
        public static void CreatePauseMenu()
        {
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            MainMenuBuilder.AssignUiActions(eventSystemGo.AddComponent<InputSystemUIInputModule>());

            var canvasGo = new GameObject("PauseMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the HUD
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1376f, 768f);
            scaler.matchWidthOrHeight = 0.5f;
            var group = canvasGo.AddComponent<CanvasGroup>();

            // Darken the frozen game behind the panel.
            var dim = MainMenuBuilder.NewImage("Dim", canvasGo.transform, null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            MainMenuBuilder.Stretch(dim.rectTransform);

            var panel = MainMenuBuilder.NewImage("Panel", canvasGo.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath), raycast: true);
            if (panel.sprite != null) panel.type = Image.Type.Sliced;
            else panel.color = new Color(0.06f, 0.05f, 0.05f, 0.95f);
            panel.rectTransform.sizeDelta = new Vector2(380f, 380f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var title = MainMenuBuilder.NewText("Title", panel.rectTransform, font, "Paused", TextAnchor.MiddleCenter, 40);
            Place(title.rectTransform, 120f, 320f, 60f);

            var resume = Entry(panel.rectTransform, font, "Resume", 20f);
            var mainMenu = Entry(panel.rectTransform, font, "Main Menu", -55f);
            var quit = Entry(panel.rectTransform, font, "Quit", -130f);
            MainMenuBuilder.ChainVertically(new List<Selectable> { resume, mainMenu, quit });

            var menu = canvasGo.AddComponent<PauseMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("input").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputReaderSO>("Assets/Data/Player/InputReader.asset");
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("resume").objectReferenceValue = resume;
            so.FindProperty("mainMenu").objectReferenceValue = mainMenu;
            so.FindProperty("quit").objectReferenceValue = quit;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hidden until Escape; also keeps it out of the way in the Scene view.
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static Button Entry(RectTransform panel, Font font, string label, float y)
        {
            var row = MainMenuBuilder.NewImage($"Button_{label.Replace(" ", "")}", panel, null, raycast: true);
            row.color = Color.clear;
            Place(row.rectTransform, y, 300f, 64f);

            var text = MainMenuBuilder.NewText("Label", row.rectTransform, font, label, TextAnchor.MiddleCenter, 32);
            MainMenuBuilder.Stretch(text.rectTransform);
            text.color = Cream;

            var button = row.gameObject.AddComponent<Button>();
            MainMenuBuilder.ShowSelectedInAmber(button, text);
            return button;
        }

        /// <summary>Centered horizontally in the panel, at a vertical offset from its center.</summary>
        private static void Place(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
