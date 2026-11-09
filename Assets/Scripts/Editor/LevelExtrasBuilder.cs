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
        private const string BossChannelPath = "Assets/Data/EventChannels/BossStatusChanged.asset";

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

        /// <summary>
        /// Adds a BossStatusReporter to an enemy prefab so the boss health bar shows its name and health.
        /// Edits the prefab in place; safe to run again.
        /// </summary>
        public static void MakeBoss(string prefabPath, string bossName)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var reporter = root.GetComponent<Wickfall.Enemies.BossStatusReporter>();
                if (reporter == null) reporter = root.AddComponent<Wickfall.Enemies.BossStatusReporter>();
                var so = new SerializedObject(reporter);
                so.FindProperty("bossName").stringValue = bossName;
                so.FindProperty("damageable").objectReferenceValue = root.GetComponent<Wickfall.Combat.Damageable>();
                so.FindProperty("perception").objectReferenceValue = root.GetComponent<Wickfall.Enemies.EnemyPerception>();
                so.FindProperty("bossStatusChanged").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossStatusEventChannelSO>(BossChannelPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Boss health bar at the bottom of the screen: name, dark frame, pale trail, red fill. Hidden until a boss engages.</summary>
        public static void CreateBossHealthBar()
        {
            var canvasGo = new GameObject("BossHealthBar", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // above the HUD, below the pause menu
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1376f, 768f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = MainMenuBuilder.NewRect("Bar", canvasGo.transform);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 56f);
            root.sizeDelta = new Vector2(720f, 20f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = MainMenuBuilder.NewText("Name", root, font, "Demon Lord", TextAnchor.LowerCenter, 30);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            label.rectTransform.sizeDelta = new Vector2(0f, 36f);

            var frame = MainMenuBuilder.NewImage("Frame", root, null, raycast: false);
            frame.color = new Color(0.08f, 0.06f, 0.06f, 0.95f);
            MainMenuBuilder.Stretch(frame.rectTransform);
            frame.rectTransform.offsetMin = new Vector2(-4f, -4f);
            frame.rectTransform.offsetMax = new Vector2(4f, 4f);
            frame.gameObject.AddComponent<Outline>().effectColor = new Color(0.55f, 0.42f, 0.3f, 1f);

            var trail = BarPart("Trail", root, new Color(0.95f, 0.85f, 0.75f, 0.85f));
            var fill = BarPart("Fill", root, new Color(0.72f, 0.12f, 0.1f, 1f));

            var bar = canvasGo.AddComponent<BossHealthBar>();
            var so = new SerializedObject(bar);
            so.FindProperty("bossStatusChanged").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossStatusEventChannelSO>(BossChannelPath);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("nameLabel").objectReferenceValue = label;
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("trail").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fills the bar, with its pivot on the left so scaling X shrinks it toward the left.</summary>
        private static RectTransform BarPart(string name, RectTransform parent, Color color)
        {
            var image = MainMenuBuilder.NewImage(name, parent, null, raycast: false);
            image.color = color;
            var rect = image.rectTransform;
            MainMenuBuilder.Stretch(rect);
            rect.pivot = new Vector2(0f, 0.5f);
            return rect;
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
