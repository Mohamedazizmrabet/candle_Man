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
    /// docs/Main menu.jpg. The image is cut into a clean background (menu box emptied, title removed), the title,
    /// the glowing frame (text removed), the four labels, the menu box (reused for Settings) and a blurred backdrop.
    /// The picture always fits on screen; the backdrop fills the rest. Also creates Level_01 and the build order.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string SourceImage = "docs/Main menu.jpg"; // relative to the project folder
        private const string ArtDir = "Assets/Art/UI/Menus";
        private const string SceneDir = "Assets/Scenes/MainMenu";
        private const string ScenePath = SceneDir + "/MainMenu.unity";
        private const string LevelOnePath = "Assets/Scenes/Regions/01_ChampsDeSuif/Level_01.unity";
        private const string LevelTemplatePath = "Assets/Test/Aziz/Aziz_TestScene.unity";
        private const string UiActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Measured on the 1376x768 reference, in image pixels with the origin at the top-left.
        private const int ImageWidth = 1376;
        private const int ImageHeight = 768;
        private static readonly RectInt PanelInterior = new RectInt(520, 319, 335, 345);
        private static readonly RectInt HighlightBox = new RectInt(527, 335, 322, 95);
        private static readonly RectInt HighlightText = new RectInt(546, 353, 284, 58);
        // The whole menu box with its corner ornaments; reused (9-sliced) as the Settings panel.
        private static readonly RectInt PanelRect = new RectInt(502, 302, 371, 379);
        private const int PanelBorder = 26;
        private const int HighlightBorder = 16;
        private const int FeatherWidth = 24;

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
            var clean = MakeBackground(source, logoMask);
            var art = new MenuArt
            {
                Backdrop = SaveSprite(MakeBackdrop(clean), "MainMenu_Backdrop"),
                Background = SaveSprite(Feather(clean, FeatherWidth), "MainMenu_Background"),
                Logo = SaveSprite(MakeLogo(source, logoMask), "MainMenu_Logo"),
                Highlight = SaveSprite(MakeHighlight(source), "MainMenu_Highlight", HighlightBorder),
                Panel = SaveSprite(clean.Crop(PanelRect), "MainMenu_Panel", PanelBorder),
                Labels = Labels.Select(l => SaveSprite(MakeLabel(source, l.rect), $"MainMenu_Label_{l.name}")).ToArray(),
            };

            EnsureLevelOne();
            BuildScene(art);
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

        /// <summary>Soft transparent edges, so the menu picture melts into the blurred fill on screens of another shape.</summary>
        private static PixelImage Feather(PixelImage source, int width)
        {
            var image = source.Copy();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, image.Width - 1 - x), Mathf.Min(y, image.Height - 1 - y));
                    if (edge >= width) continue;
                    var c = image.Get(x, y);
                    c.a = (byte)Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, edge / (float)width) * 255f);
                    image.Set(x, y, c);
                }
            }
            return image;
        }

        /// <summary>A small, blurred, darkened copy of the hall that fills the screen behind the menu picture.</summary>
        private static PixelImage MakeBackdrop(PixelImage source)
        {
            const int block = 8;
            int w = source.Width / block, h = source.Height / block;
            var small = new Vector3[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector3 sum = Vector3.zero;
                    for (int dy = 0; dy < block; dy++)
                    {
                        for (int dx = 0; dx < block; dx++)
                        {
                            var c = source.Get(x * block + dx, y * block + dy);
                            sum += new Vector3(c.r, c.g, c.b);
                        }
                    }
                    small[x, y] = sum / (block * block);
                }
            }

            for (int pass = 0; pass < 3; pass++)
            {
                var next = new Vector3[w, h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Vector3 sum = Vector3.zero;
                        int count = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = Mathf.Clamp(x + dx, 0, w - 1), ny = Mathf.Clamp(y + dy, 0, h - 1);
                                sum += small[nx, ny];
                                count++;
                            }
                        }
                        next[x, y] = sum / count;
                    }
                }
                small = next;
            }

            var image = PixelImage.Create(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = small[x, y] * 0.45f;
                    image.Set(x, y, new Color32((byte)c.x, (byte)c.y, (byte)c.z, 255));
                }
            }
            return image;
        }

        /// <param name="border">Pixels kept unstretched on each side when the sprite is 9-sliced (0 = none).</param>
        private static Sprite SaveSprite(PixelImage image, string name, int border = 0)
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
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- Scene ----------

        private class MenuArt
        {
            public Sprite Backdrop, Background, Logo, Highlight, Panel;
            public Sprite[] Labels;
        }

        private static void BuildScene(MenuArt art)
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

            // Behind everything: a blurred copy of the hall that always covers the whole screen.
            var backdrop = NewImage("Backdrop", canvasGo.transform, art.Backdrop, raycast: false);
            Stretch(backdrop.rectTransform);
            AddAspect(backdrop.gameObject, AspectRatioFitter.AspectMode.EnvelopeParent);

            // The menu picture always fits entirely on screen; everything inside is placed in image pixels.
            var frame = NewRect("Frame", canvasGo.transform);
            Stretch(frame);
            AddAspect(frame.gameObject, AspectRatioFitter.AspectMode.FitInParent);

            var background = NewImage("Background", frame, art.Background, raycast: false);
            Stretch(background.rectTransform);

            var embers = NewRect("Embers", frame);
            Stretch(embers);

            var logo = NewImage("Logo", frame, art.Logo, raycast: false);
            Place(logo.rectTransform, ToRect(LogoRegion), FullImage);

            var controller = canvasGo.AddComponent<MainMenuController>();
            var mainButtons = BuildMainEntries(frame, art, out var mainGroup);
            var settings = BuildSettings(frame, art, out var settingsGroup);

            SetRef(controller, "newGame", mainButtons[0]);
            SetRef(controller, "continueGame", mainButtons[1]);
            SetRef(controller, "settings", mainButtons[2]);
            SetRef(controller, "quit", mainButtons[3]);
            SetRef(controller, "mainGroup", mainGroup);
            SetRef(controller, "settingsMenu", settings);
            SetRef(controller, "settingsGroup", settingsGroup);
            var so = new SerializedObject(controller);
            so.FindProperty("levelOneScene").stringValue = Path.GetFileNameWithoutExtension(LevelOnePath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var ambience = canvasGo.AddComponent<MenuAmbience>();
            SetRef(ambience, "background", background);
            SetRef(ambience, "emberLayer", embers);

            eventSystemGo.GetComponent<EventSystem>().firstSelectedGameObject = mainButtons[0].gameObject;

            WickfallEditorUtil.EnsureFolder(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = canvasGo;
            Debug.Log($"[Wickfall] Main menu built at {ScenePath}. Press Play to try it.");
        }

        /// <summary>New Game, Continue, Settings, Quit, with the sliding frame, inside the baked menu box.</summary>
        private static Button[] BuildMainEntries(RectTransform frame, MenuArt art, out CanvasGroup group)
        {
            var main = NewRect("Main", frame);
            Stretch(main);
            group = main.gameObject.AddComponent<CanvasGroup>();

            var highlightImage = NewImage("Highlight", main, art.Highlight, raycast: false);
            Place(highlightImage.rectTransform, ToRect(HighlightBox), FullImage);
            var highlight = highlightImage.gameObject.AddComponent<MenuHighlight>();

            var buttons = new List<Button>();
            Vector2 firstCenter = Labels[0].rect.center;
            for (int i = 0; i < Labels.Length; i++)
            {
                var label = Labels[i];
                // Each entry's slot is the frame's size, shifted by how far its label sits below "New Game".
                float shift = label.rect.center.y - firstCenter.y;
                var slot = new Rect(HighlightBox.x, HighlightBox.y + shift, HighlightBox.width, HighlightBox.height);

                var slotImage = NewImage($"Button_{label.name}", main, null, raycast: true);
                slotImage.color = Color.clear;
                // Slots overlap a little; trim the clickable area so hover picks the right entry.
                slotImage.raycastPadding = new Vector4(0f, 8f, 0f, 8f);
                Place(slotImage.rectTransform, slot, FullImage);

                var button = slotImage.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;

                var text = NewImage("Label", slotImage.rectTransform, art.Labels[i], raycast: false);
                Place(text.rectTransform, ToRect(label.rect), slot);
                AddShadow(text.gameObject);

                var entry = slotImage.gameObject.AddComponent<MenuEntry>();
                SetRef(entry, "highlight", highlight);
                buttons.Add(button);
            }

            ChainVertically(buttons.Cast<Selectable>().ToList());
            return buttons.ToArray();
        }

        /// <summary>Settings panel drawn over the menu box: Volume slider, Fullscreen toggle, Back.</summary>
        private static SettingsMenu BuildSettings(RectTransform frame, MenuArt art, out CanvasGroup group)
        {
            var panelRect = ToRect(PanelRect);
            var root = NewRect("Settings", frame);
            Place(root, panelRect, FullImage);
            group = root.gameObject.AddComponent<CanvasGroup>();
            // Hidden until Settings is clicked (also keeps it out of the way in the Scene view).
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var panel = NewImage("Panel", root, art.Panel, raycast: true);
            panel.type = Image.Type.Sliced;
            Stretch(panel.rectTransform);

            // Reuses the "Settings" lettering from the reference as the panel title.
            var title = NewImage("Title", root, art.Labels[2], raycast: false);
            Place(title.rectTransform, new Rect(SettingsCenterX - Labels[2].rect.width / 2f, 332f, Labels[2].rect.width, Labels[2].rect.height), panelRect);
            AddShadow(title.gameObject);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Volume: label, value and a slider track inside one selectable row.
            var volumeRow = Row("Row_Volume", root, new Rect(RowX, 404f, RowWidth, 82f), panelRect);
            var volumeLabel = NewText("Label", volumeRow, font, "Volume", TextAnchor.UpperLeft, 30);
            Place(volumeLabel.rectTransform, new Rect(RowX + 26f, 414f, 180f, 36f), new Rect(RowX, 404f, RowWidth, 82f));
            var volumeValue = NewText("Value", volumeRow, font, "80%", TextAnchor.UpperRight, 26);
            Place(volumeValue.rectTransform, new Rect(RowX + RowWidth - 126f, 417f, 100f, 32f), new Rect(RowX, 404f, RowWidth, 82f));
            var slider = BuildSlider(volumeRow, new Rect(RowX + 28f, 456f, RowWidth - 56f, 14f), new Rect(RowX, 404f, RowWidth, 82f));

            // Fullscreen: label and a check box.
            var fullRect = new Rect(RowX, 496f, RowWidth, 66f);
            var fullRow = Row("Row_Fullscreen", root, fullRect, panelRect);
            var fullLabel = NewText("Label", fullRow, font, "Fullscreen", TextAnchor.MiddleLeft, 30);
            Place(fullLabel.rectTransform, new Rect(RowX + 26f, 496f, 200f, 66f), fullRect);
            var toggle = BuildToggle(fullRow, new Rect(RowX + RowWidth - 62f, 513f, 32f, 32f), fullRect);

            // Back.
            var backRect = new Rect(RowX, 584f, RowWidth, 66f);
            var backRow = Row("Row_Back", root, backRect, panelRect);
            var backLabel = NewText("Label", backRow, font, "Back", TextAnchor.MiddleCenter, 30);
            Place(backLabel.rectTransform, backRect, backRect);
            var back = backRow.gameObject.AddComponent<Button>();
            back.transition = Selectable.Transition.None;

            ShowSelectedInAmber(slider, volumeLabel);
            ShowSelectedInAmber(toggle, fullLabel);
            ShowSelectedInAmber(back, backLabel);
            ChainVertically(new List<Selectable> { slider, toggle, back });

            var menu = root.gameObject.AddComponent<SettingsMenu>();
            SetRef(menu, "volume", slider);
            SetRef(menu, "volumeValue", volumeValue);
            SetRef(menu, "fullscreen", toggle);
            SetRef(menu, "back", back);
            return menu;
        }

        // Settings layout, in reference image pixels.
        private const float RowX = 533f;
        private const float RowWidth = 309f;
        private static float SettingsCenterX => PanelRect.x + PanelRect.width / 2f;

        private static readonly Color Cream = new Color32(249, 250, 242, 255);
        private static readonly Color DarkFill = new Color32(28, 20, 16, 255);
        private static readonly Color Amber = new Color32(224, 138, 44, 255);

        private static RectTransform Row(string name, RectTransform parent, Rect rect, Rect parentRect)
        {
            var image = NewImage(name, parent, null, raycast: true);
            image.color = Color.clear;
            Place(image.rectTransform, rect, parentRect);
            return image.rectTransform;
        }

        private static Slider BuildSlider(RectTransform row, Rect track, Rect rowRect)
        {
            var trackImage = NewImage("Track", row, null, raycast: false);
            trackImage.color = DarkFill;
            Place(trackImage.rectTransform, track, rowRect);
            trackImage.gameObject.AddComponent<Outline>().effectColor = new Color(0.55f, 0.42f, 0.3f, 1f);

            var fillArea = NewRect("Fill Area", trackImage.rectTransform);
            Stretch(fillArea);
            var fill = NewImage("Fill", fillArea, null, raycast: false);
            fill.color = Amber;
            Stretch(fill.rectTransform);

            var handleArea = NewRect("Handle Slide Area", trackImage.rectTransform);
            Stretch(handleArea);
            var handle = NewImage("Handle", handleArea, null, raycast: false);
            handle.color = Cream;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(10f, 12f);
            AddShadow(handle.gameObject);

            var slider = row.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle BuildToggle(RectTransform row, Rect box, Rect rowRect)
        {
            var boxImage = NewImage("Box", row, null, raycast: false);
            boxImage.color = DarkFill;
            Place(boxImage.rectTransform, box, rowRect);
            boxImage.gameObject.AddComponent<Outline>().effectColor = new Color(0.55f, 0.42f, 0.3f, 1f);

            var check = NewImage("Check", boxImage.rectTransform, null, raycast: false);
            check.color = Amber;
            check.rectTransform.anchorMin = new Vector2(0.22f, 0.22f);
            check.rectTransform.anchorMax = new Vector2(0.78f, 0.78f);
            check.rectTransform.offsetMin = Vector2.zero;
            check.rectTransform.offsetMax = Vector2.zero;

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        /// <summary>
        /// Unity's standard color tint: the row's label turns amber while selected with the keyboard or a click.
        /// Mouse hover changes nothing.
        /// </summary>
        private static void ShowSelectedInAmber(Selectable selectable, Graphic label)
        {
            selectable.transition = Selectable.Transition.ColorTint;
            selectable.targetGraphic = label;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(1f, 0.7f, 0.35f);
            colors.pressedColor = new Color(0.85f, 0.6f, 0.3f);
            colors.fadeDuration = 0f;
            selectable.colors = colors;
        }

        /// <summary>Up and down move between the entries and wrap around; left and right are left to sliders.</summary>
        private static void ChainVertically(List<Selectable> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i + items.Count - 1) % items.Count],
                    selectOnDown = items[(i + 1) % items.Count],
                };
            }
        }

        private static void AddShadow(GameObject go)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private static void AddAspect(GameObject go, AspectRatioFitter.AspectMode mode)
        {
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = mode;
            fitter.aspectRatio = ImageWidth / (float)ImageHeight;
        }

        private static Text NewText(string name, Transform parent, Font font, string value, TextAnchor anchor, int size)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.alignment = anchor;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = Cream;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(text.gameObject);
            return text;
        }

        /// <summary>Uses the template's "UI" action map (Navigate, Submit, Cancel, Click...) for menu navigation.</summary>
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

        /// <summary>Level 1 starts as a copy of Aziz's test scene, so the test scene stays free to break.</summary>
        private static void EnsureLevelOne()
        {
            if (File.Exists(LevelOnePath)) return;
            if (!File.Exists(LevelTemplatePath))
            {
                Debug.LogWarning($"[Wickfall] {LevelTemplatePath} not found, so {LevelOnePath} was not created. Build the Aziz test scene first.");
                return;
            }

            WickfallEditorUtil.EnsureFolder(Path.GetDirectoryName(LevelOnePath).Replace('\\', '/'));
            AssetDatabase.CopyAsset(LevelTemplatePath, LevelOnePath);
            Debug.Log($"[Wickfall] Created {LevelOnePath} from {LevelTemplatePath}.");
        }

        /// <summary>Build order: 0 = main menu, 1 = level 1, then everything that was already listed.</summary>
        private static void AddScenesToBuild()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != LevelOnePath).ToList();
            var first = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            if (File.Exists(LevelOnePath)) first.Add(new EditorBuildSettingsScene(LevelOnePath, true));
            EditorBuildSettings.scenes = first.Concat(scenes).ToArray();
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

            public static PixelImage Create(int width, int height) => new PixelImage(width, height);

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
