using UnityEngine;
using UnityEngine.UI;
using Wickfall.Core;

namespace Wickfall.UI
{
    /// <summary>End-of-demo victory card shown after the Demon Lord is defeated.</summary>
    public sealed class VictoryScreen : MonoBehaviour
    {
        private static VictoryScreen instance;

        public static void ShowVictory()
        {
            if (instance != null) return;

            var root = new GameObject("Victory Screen", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            instance = root.AddComponent<VictoryScreen>();
            instance.Build();
            Time.timeScale = 0f;
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            var panel = new GameObject("Dark Overlay", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.025f, 0.018f, 0.03f, 0.92f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateLabel("Victory Title", "YOU WON!", font, 30, FontStyle.Bold,
                new Vector2(0f, 58f), new Vector2(420f, 48f), new Color(1f, 0.78f, 0.36f));
            CreateLabel("Victory Message", "The Demon Lord is defeated.\nThanks for playing Wickfall.\nMore of the game is coming soon.",
                font, 16, FontStyle.Normal, new Vector2(0f, -14f), new Vector2(420f, 70f), Color.white);

            var buttonObject = new GameObject("Back to Menu", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -79f);
            buttonRect.sizeDelta = new Vector2(210f, 40f);
            buttonObject.GetComponent<Image>().color = new Color(0.34f, 0.16f, 0.06f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(BackToMenu);
            CreateLabel("Button Label", "BACK TO MAIN MENU", font, 14, FontStyle.Bold,
                Vector2.zero, new Vector2(200f, 36f), Color.white, buttonObject.transform);
        }

        private void CreateLabel(string objectName, string value, Font font, int size, FontStyle style,
            Vector2 position, Vector2 dimensions, Color color, Transform parent = null)
        {
            var label = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent != null ? parent : transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            var text = label.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private void BackToMenu()
        {
            if (SceneFader.FadeToScene("MainMenu", 0.6f, 0.8f))
                Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
