using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Core.Events;
using Wickfall.UI;

namespace Wickfall.EditorTools
{
    /// <summary>Menus under "Tools > Wickfall > Scene" that build scene objects already wired to their channels.</summary>
    public static class WickfallSceneTools
    {
        private const string WaxChangedPath = "Assets/Data/EventChannels/WaxChanged.asset";

        // Bar size in reference pixels (the canvas scales from 480x270, the camera resolution).
        private static readonly Vector2 BarSize = new Vector2(100f, 8f);
        private static readonly Vector2 BarMargin = new Vector2(12f, 12f);

        [MenuItem("Tools/Wickfall/Scene/Create Wax HUD")]
        public static void CreateWaxHud()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Wax HUD");

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = CreateRect("WaxBar", canvasGo.transform);
            bar.anchorMin = bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(BarMargin.x, -BarMargin.y);
            bar.sizeDelta = BarSize;

            AddImage(CreateStretched("Background", bar), new Color(0.08f, 0.07f, 0.07f, 0.9f));
            var trail = CreateStretched("Trail", bar);
            AddImage(trail, new Color(0.9f, 0.88f, 0.8f, 0.8f));
            var fill = CreateStretched("Fill", bar);
            var fillImage = AddImage(fill, new Color(1f, 0.85f, 0.5f));

            var hud = bar.gameObject.AddComponent<WaxBarHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("waxChanged").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WaxEventChannelSO>(WaxChangedPath);
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("fillImage").objectReferenceValue = fillImage;
            so.FindProperty("trail").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (so.FindProperty("waxChanged").objectReferenceValue == null)
            {
                Debug.LogWarning($"[Wickfall] {WaxChangedPath} not found. Run Tools > Wickfall > Create Default Data, then assign it on WaxBar.");
            }

            Selection.activeGameObject = bar.gameObject;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Fills the parent, with the pivot on the left so scaling X shrinks it toward the left.</summary>
        private static RectTransform CreateStretched(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
