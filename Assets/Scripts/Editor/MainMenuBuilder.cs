using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Wickfall.UI;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Scene > Build Main Menu": rebuilds the main menu from the reference image
    /// docs/Main menu.jpg. The image is cut into a clean background (menu panel emptied), the glowing
    /// frame (text removed) and the four labels, so the selection can move while looking exactly like the reference.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string SourceImage = "docs/Main menu.jpg"; // relative to the project folder
        private const string ArtDir = "Assets/Art/UI/Menus";
        private const string SceneDir = "Assets/Scenes/MainMenu";
        private const string ScenePath = SceneDir + "/MainMenu.unity";
        private const string GameScenePath = "Assets/Test/Aziz/Aziz_TestScene.unity";
        private const string UiActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Measured on the 1376x768 reference, in image pixels with the origin at the top-left.
        private const int ImageWidth = 1376;
        private const int ImageHeight = 768;
        private static readonly RectInt PanelInterior = new RectInt(520, 319, 335, 345);
        private static readonly RectInt HighlightBox = new RectInt(527, 335, 322, 95);
        private static readonly RectInt HighlightText = new RectInt(546, 353, 284, 58);

        // The WICKFALL title. Letters (and their light rim) are found by brightness, kept only if they form
        // a big piece or sit right next to one (so cobwebs stay in the wall), then grown to take the dark outline.
        private static readonly RectInt LogoRegion = new RectInt(360, 40, 641, 216);
        private const float LogoBrightness = 60f;
        private const int LogoMinPiece = 150;
        private const int LogoRimReach = 10;
        private const int LogoOutline = 4;

        private static readonly (string name, RectInt rect)[] Labels =
        {
            ("NewGame", new RectInt(572, 356, 232, 49)),
            ("Continue", new RectInt(591, 439, 194, 44)),
            ("Settings", new RectInt(596, 513, 183, 55)),
            ("Quit", new RectInt(640, 588, 95, 53)),
        };

        private static readonly Color32 TextColor = new Color32(249, 250, 242, 255);

        [MenuItem("Tools/Wickfall/Scene/Build Main Menu")]
        public static void BuildMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string sourcePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), SourceImage);
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[Wickfall] Reference image not found: {sourcePath}");
                return;
            }

            var source = PixelImage.Load(sourcePath);
            if (source.Width != ImageWidth || source.Height != ImageHeight)
            {
                Debug.LogError($"[Wickfall] {SourceImage} is {source.Width}x{source.Height}; the menu measurements assume {ImageWidth}x{ImageHeight}.");
                return;
            }

            var logoMask = MakeLogoMask(source);
            var background = SaveSprite(MakeBackground(source, logoMask), "MainMenu_Background");
            var logo = SaveSprite(MakeLogo(source, logoMask), "MainMenu_Logo");
            var highlight = SaveSprite(MakeHighlight(source), "MainMenu_Highlight");
            var labelSprites = Labels.Select(l => SaveSprite(MakeLabel(source, l.rect), $"MainMenu_Label_{l.name}")).ToArray();

            BuildScene(background, logo, highlight, labelSprites);
            AddScenesToBuild();
        }

        // ---------- Image cutting ----------

        /// <summary>The reference with the panel's inside emptied (no labels, no frame) and the title removed.</summary>
        private static PixelImage MakeBackground(PixelImage source, bool[,] logoMask)
        {
            var image = source.Copy();
            FillBehindLogo(image, logoMask);
            for (int y = PanelInterior.yMin; y < PanelInterior.yMax; y++)
            {
                // The panel's inside is a flat near-black: blend each row between its two edge pixels.
                Color32 left = source.Get(PanelInterior.xMin, y);
                Color32 right = source.Get(PanelInterior.xMax - 1, y);
                for (int x = PanelInterior.xMin; x < PanelInterior.xMax; x++)
                {
                    float t = (x - PanelInterior.xMin) / (float)(PanelInterior.width - 1);
                    image.Set(x, y, Color32.Lerp(left, right, t));
                }
            }
            return image;
        }

        /// <summary>Which pixels of LogoRegion belong to the title, indexed [x, y] inside the region.</summary>
        private static bool[,] MakeLogoMask(PixelImage source)
        {
            int w = LogoRegion.width, h = LogoRegion.height;
            var area = new RectInt(0, 0, w, h);

            var bright = new bool[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bright[x, y] = Luminance(source.Get(LogoRegion.x + x, LogoRegion.y + y)) > LogoBrightness;
                }
            }

            // Letters are big connected pieces; specks and cobweb strands are small.
            var letters = new bool[w, h];
            var seen = new bool[w, h];
            var stack = new Stack<Vector2Int>();
            var piece = new List<Vector2Int>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!bright[x, y] || seen[x, y]) continue;

                    piece.Clear();
                    stack.Push(new Vector2Int(x, y));
                    seen[x, y] = true;
                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        piece.Add(p);
                        foreach (var n in new[] { p + Vector2Int.right, p + Vector2Int.left, p + Vector2Int.up, p + Vector2Int.down })
                        {
                            if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h || !bright[n.x, n.y] || seen[n.x, n.y]) continue;
                            seen[n.x, n.y] = true;
                            stack.Push(n);
                        }
                    }

                    if (piece.Count >= LogoMinPiece)
                    {
                        foreach (var p in piece) letters[p.x, p.y] = true;
                    }
                }
            }

            // Small bright bits hugging a letter are its broken-up light rim: keep them too.
            var near = Dilate(letters, area, LogoRimReach);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (bright[x, y] && near[x, y]) letters[x, y] = true;
                }
            }

            return Dilate(letters, area, LogoOutline);
        }

        /// <summary>The title alone, on a transparent background, in the same pixels as the reference.</summary>
        private static PixelImage MakeLogo(PixelImage source, bool[,] logoMask)
        {
            var image = source.Crop(LogoRegion);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (!logoMask[x, y]) image.Set(x, y, new Color32(0, 0, 0, 0));
                }
            }
            return image;
        }

        /// <summary>
        /// Paints the wall back in where the title was, growing it inward from the edges and then softening it.
        /// Only seen when the Logo object is moved or faded; replace the background art for a perfect wall.
        /// </summary>
        private static void FillBehindLogo(PixelImage image, bool[,] logoMask)
        {
            int w = LogoRegion.width, h = LogoRegion.height;
            var colors = new Vector3[w, h];
            var known = new bool[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = image.Get(LogoRegion.x + x, LogoRegion.y + y);
                    colors[x, y] = new Vector3(c.r, c.g, c.b);
                    known[x, y] = !logoMask[x, y];
                }
            }

            // Onion peel: each pass fills the hidden pixels that touch a known one with their average.
            var batch = new List<(int x, int y, Vector3 color)>();
            do
            {
                batch.Clear();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (known[x, y]) continue;
                        Vector3 sum = Vector3.zero;
                        int count = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h || !known[nx, ny]) continue;
                                sum += colors[nx, ny];
                                count++;
                            }
                        }
                        if (count > 0) batch.Add((x, y, sum / count));
                    }
                }
                foreach (var (x, y, color) in batch)
                {
                    colors[x, y] = color;
                    known[x, y] = true;
                }
            } while (batch.Count > 0);

            // Soften the streaks the peeling leaves behind.
            for (int pass = 0; pass < 4; pass++)
            {
                var next = (Vector3[,])colors.Clone();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!logoMask[x, y]) continue;
                        Vector3 sum = Vector3.zero;
                        int count = 0;
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                sum += colors[nx, ny];
                                count++;
                            }
                        }
                        next[x, y] = sum / count;
                    }
                }
                colors = next;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!logoMask[x, y]) continue;
                    var c = colors[x, y];
                    image.Set(LogoRegion.x + x, LogoRegion.y + y, new Color32((byte)c.x, (byte)c.y, (byte)c.z, 255));
                }
            }
        }

        /// <summary>The glowing frame around "New Game", with the words painted out.</summary>
        private static PixelImage MakeHighlight(PixelImage source)
        {
            var image = source.Crop(HighlightBox);
            var area = new RectInt(HighlightText.x - HighlightBox.x, HighlightText.y - HighlightBox.y, HighlightText.width, HighlightText.height);

            // Text is bright cream with a near-black outline; the frame's inside is a dark brown (brightness ~40).
            var mask = new bool[image.Width, image.Height];
            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    float lum = Luminance(image.Get(x, y));
                    mask[x, y] = lum > 70f || lum < 20f;
                }
            }
            mask = Dilate(mask, area, 2);

            // Fill each masked run by blending the clean pixels on its left and right, keeping the frame's gradient.
            for (int y = area.yMin; y < area.yMax; y++)
            {
                int x = area.xMin;
                while (x < area.xMax)
                {
                    if (!mask[x, y]) { x++; continue; }

                    int start = x;
                    while (x < area.xMax && mask[x, y]) x++;
                    int end = x - 1;

                    Color32 left = image.Get(Mathf.Max(start - 1, area.xMin), y);
                    Color32 right = image.Get(Mathf.Min(end + 1, area.xMax - 1), y);
                    if (start == area.xMin) left = right;
                    if (end == area.xMax - 1) right = left;

                    for (int i = start; i <= end; i++)
                    {
                        float t = (i - start + 1) / (float)(end - start + 2);
                        image.Set(i, y, Color32.Lerp(left, right, t));
                    }
                }
            }
            return image;
        }

        /// <summary>One label as cream text on a transparent background.</summary>
        private static PixelImage MakeLabel(PixelImage source, RectInt rect)
        {
            var image = source.Crop(rect);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    float alpha = Mathf.InverseLerp(90f, 190f, Luminance(image.Get(x, y)));
                    var c = TextColor;
                    c.a = (byte)Mathf.RoundToInt(alpha * 255f);
                    image.Set(x, y, c);
                }
            }
            return image;
        }

        private static bool[,] Dilate(bool[,] mask, RectInt area, int radius)
        {
            var result = (bool[,])mask.Clone();
            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (!mask[x, y]) continue;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= area.xMin && nx < area.xMax && ny >= area.yMin && ny < area.yMax) result[nx, ny] = true;
                        }
                    }
                }
            }
            return result;
        }

        private static float Luminance(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        private static Sprite SaveSprite(PixelImage image, string name)
        {
            WickfallEditorUtil.EnsureFolder(ArtDir);
            string path = $"{ArtDir}/{name}.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Bilinear, not Point: the menu is stretched to the screen by a non-whole factor.
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- Scene ----------

        private static void BuildScene(Sprite background, Sprite logo, Sprite highlightSprite, Sprite[] labelSprites)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            AssignUiActions(eventSystemGo.AddComponent<InputSystemUIInputModule>());

            var canvasGo = new GameObject("MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ImageWidth, ImageHeight);
            scaler.matchWidthOrHeight = 0.5f;

            // Frame keeps the image's aspect ratio and covers the screen; everything inside is placed in image pixels.
            var frame = NewRect("Frame", canvasGo.transform);
            Stretch(frame);
            var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ImageWidth / (float)ImageHeight;

            var backgroundImage = NewImage("Background", frame, background, raycast: false);
            Stretch(backgroundImage.rectTransform);

            // The title on its own object, exactly where it was in the reference, so it can be animated or swapped.
            var logoImage = NewImage("Logo", frame, logo, raycast: false);
            Place(logoImage.rectTransform, ToRect(LogoRegion), FullImage);

            var highlightImage = NewImage("Highlight", frame, highlightSprite, raycast: false);
            Place(highlightImage.rectTransform, ToRect(HighlightBox), FullImage);

            var controller = canvasGo.AddComponent<MainMenuController>();
            var buttons = new List<MainMenuButton>();
            Vector2 firstCenter = Labels[0].rect.center;

            for (int i = 0; i < Labels.Length; i++)
            {
                var label = Labels[i];
                // Each entry's slot is the frame's size, shifted by how far its label sits below "New Game".
                float shift = label.rect.center.y - firstCenter.y;
                var slot = new Rect(HighlightBox.x, HighlightBox.y + shift, HighlightBox.width, HighlightBox.height);

                var slotImage = NewImage($"Button_{label.name}", frame, null, raycast: true);
                slotImage.color = new Color(0f, 0f, 0f, 0f);
                // Slots overlap a little; trim the clickable area so hover picks the right entry.
                slotImage.raycastPadding = new Vector4(0f, 8f, 0f, 8f);
                Place(slotImage.rectTransform, slot, FullImage);

                var button = slotImage.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                var menuButton = slotImage.gameObject.AddComponent<MainMenuButton>();
                SetRef(menuButton, "controller", controller);
                buttons.Add(menuButton);

                var text = NewImage("Label", slotImage.rectTransform, labelSprites[i], raycast: false);
                Place(text.rectTransform, ToRect(label.rect), slot);
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }

            // Up and down wrap around, like most game menus.
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].GetComponent<Button>().navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i + buttons.Count - 1) % buttons.Count].GetComponent<Button>(),
                    selectOnDown = buttons[(i + 1) % buttons.Count].GetComponent<Button>(),
                };
            }

            SetRef(controller, "newGame", buttons[0]);
            SetRef(controller, "continueGame", buttons[1]);
            SetRef(controller, "settings", buttons[2]);
            SetRef(controller, "quit", buttons[3]);
            SetRef(controller, "highlight", highlightImage.rectTransform);
            SetRef(controller, "highlightImage", highlightImage);

            eventSystemGo.GetComponent<EventSystem>().firstSelectedGameObject = buttons[0].gameObject;

            WickfallEditorUtil.EnsureFolder(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = canvasGo;
            Debug.Log($"[Wickfall] Main menu built at {ScenePath}. Press Play to try it.");
        }

        /// <summary>Uses the template's "UI" action map (Navigate, Submit, Click...) for menu navigation.</summary>
        private static void AssignUiActions(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(UiActionsPath);
            var references = AssetDatabase.LoadAllAssetsAtPath(UiActionsPath).OfType<InputActionReference>().ToList();
            if (asset == null || references.Count == 0)
            {
                Debug.LogWarning($"[Wickfall] {UiActionsPath} not found: assign the EventSystem's UI actions by hand.");
                return;
            }

            InputActionReference Find(string action) =>
                references.FirstOrDefault(r => r.name == $"UI/{action}")
                ?? references.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == action);

            module.actionsAsset = asset;
            module.move = Find("Navigate");
            module.submit = Find("Submit");
            module.cancel = Find("Cancel");
            module.point = Find("Point");
            module.leftClick = Find("Click");
            module.rightClick = Find("RightClick");
            module.middleClick = Find("MiddleClick");
            module.scrollWheel = Find("ScrollWheel");
            module.trackedDevicePosition = Find("TrackedDevicePosition");
            module.trackedDeviceOrientation = Find("TrackedDeviceOrientation");
        }

        /// <summary>Main menu first in the build, then the game scene New Game loads.</summary>
        private static void AddScenesToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            if (File.Exists(GameScenePath) && scenes.All(s => s.path != GameScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Layout helpers ----------

        private static readonly Rect FullImage = new Rect(0f, 0f, ImageWidth, ImageHeight);

        private static Rect ToRect(RectInt r) => new Rect(r.x, r.y, r.width, r.height);

        /// <summary>Places a child by anchors so it scales with its parent. Both rects are in image pixels, top-left origin.</summary>
        private static void Place(RectTransform rect, Rect child, Rect parent)
        {
            rect.anchorMin = new Vector2((child.xMin - parent.xMin) / parent.width, 1f - (child.yMax - parent.yMin) / parent.height);
            rect.anchorMax = new Vector2((child.xMax - parent.xMin) / parent.width, 1f - (child.yMin - parent.yMin) / parent.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, bool raycast)
        {
            var image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        private static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Pixel buffer with a top-left origin (like the measurements) ----------

        private class PixelImage
        {
            private readonly Color32[] pixels;

            public int Width { get; }
            public int Height { get; }

            private PixelImage(int width, int height)
            {
                Width = width;
                Height = height;
                pixels = new Color32[width * height];
            }

            public static PixelImage Load(string path)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(path));
                var image = new PixelImage(texture.width, texture.height);
                var raw = texture.GetPixels32(); // bottom-left origin
                for (int y = 0; y < image.Height; y++)
                {
                    System.Array.Copy(raw, (image.Height - 1 - y) * image.Width, image.pixels, y * image.Width, image.Width);
                }
                Object.DestroyImmediate(texture);
                return image;
            }

            public Color32 Get(int x, int y) => pixels[y * Width + x];

            public void Set(int x, int y, Color32 c) => pixels[y * Width + x] = c;

            public PixelImage Copy()
            {
                var copy = new PixelImage(Width, Height);
                System.Array.Copy(pixels, copy.pixels, pixels.Length);
                return copy;
            }

            public PixelImage Crop(RectInt rect)
            {
                var crop = new PixelImage(rect.width, rect.height);
                for (int y = 0; y < rect.height; y++)
                {
                    System.Array.Copy(pixels, (rect.y + y) * Width + rect.x, crop.pixels, y * rect.width, rect.width);
                }
                return crop;
            }

            public byte[] EncodeToPNG()
            {
                var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                var raw = new Color32[pixels.Length];
                for (int y = 0; y < Height; y++)
                {
                    System.Array.Copy(pixels, y * Width, raw, (Height - 1 - y) * Width, Width);
                }
                texture.SetPixels32(raw);
                byte[] png = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                return png;
            }
        }
    }
}
