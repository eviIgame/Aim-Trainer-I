using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Tapshot.EditorTools
{
    /// <summary>
    /// Generates the Tapshot menu art (rounded-rect sprites, icons, TMP font assets) and builds
    /// the main menu scene plus the four placeholder drill scenes. Safe to re-run.
    /// </summary>
    public static class TapshotMenuBuilder
    {
        const string Root = "Assets/Tapshot";
        const string SpriteDir = Root + "/Sprites";
        const string FontDir = Root + "/Fonts";
        const string SceneDir = Root + "/Scenes";
        public const string MenuScenePath = "Assets/Scenes/my scene.unity";
        const string MenuCanvasName = "MainMenuCanvas";

        static Sprite fillR16, fillR12, fillR10, lineR16w1, lineR16w2, lineR10w1, ringR20w2, ringR14w2, logo;
        static Sprite[] drillIcons;
        static TMP_FontAsset sgBold, sgMedium, jbMedium;

        static readonly (string name, string desc)[] Drills =
        {
            ("Grid", "Clear a wall of targets."),
            ("Flick", "Snap to far targets."),
            ("Track", "Track a moving target."),
            ("Jumbo Cubes", "Big cubes, less time."),
        };

        [MenuItem("Tools/Tapshot/Rebuild Menu Scenes")]
        public static string BuildAll()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    return "Aborted: save open scenes first (" + SceneManager.GetSceneAt(i).path + " has unsaved changes).";

            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(SceneDir);
            GenerateSprites();
            CreateFonts();

            var drillPaths = new List<string>();
            foreach (DrillType d in Enum.GetValues(typeof(DrillType)))
                drillPaths.Add(BuildDrillScene(d));

            BuildMenuScene();
            UpdateBuildSettings(drillPaths);
            AssetDatabase.SaveAssets();
            return "Built menu + " + drillPaths.Count + " drill scenes";
        }

        // ---------------------------------------------------------------- sprites

        static void GenerateSprites()
        {
            fillR16 = RoundRect("Fill_R16", 16, 0);
            fillR12 = RoundRect("Fill_R12", 12, 0);
            fillR10 = RoundRect("Fill_R10", 10, 0);
            lineR16w1 = RoundRect("Line_R16_W1", 16, 1);
            lineR16w2 = RoundRect("Line_R16_W2", 16, 2);
            lineR10w1 = RoundRect("Line_R10_W1", 10, 1);
            ringR20w2 = RoundRect("Line_R20_W2", 20, 2);
            ringR14w2 = RoundRect("Line_R14_W2", 14, 2);

            logo = Icon("Logo", 44, p =>
                Mathf.Min(Ring(p, new Vector2(22, 22), 17f, 4.5f), Circle(p, new Vector2(22, 22), 4.5f)));

            var c = new Vector2(24, 24);
            drillIcons = new[]
            {
                Icon("Icon_Grid", 48, p =>
                {
                    float d = float.MaxValue;
                    for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++)
                        d = Mathf.Min(d, Circle(p, new Vector2(10 + 14 * x, 10 + 14 * y), 5f));
                    return d;
                }),
                Icon("Icon_Flick", 48, p => Mathf.Min(
                    Mathf.Min(Ring(p, new Vector2(33, 33), 9f, 3f), Circle(p, new Vector2(33, 33), 3f)),
                    Mathf.Min(Segment(p, new Vector2(7, 7), new Vector2(22, 22), 3.5f),
                        Mathf.Min(Segment(p, new Vector2(22, 22), new Vector2(22, 14), 3.5f),
                            Segment(p, new Vector2(22, 22), new Vector2(14, 22), 3.5f))))),
                Icon("Icon_Track", 48, p =>
                {
                    float a = 40f * Mathf.Deg2Rad;
                    var dot = c + 16f * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    return Mathf.Min(Mathf.Min(Ring(p, c, 16f, 2.5f), Circle(p, dot, 6f)), Circle(p, c, 2.5f));
                }),
                Icon("Icon_JumboCubes", 48, p =>
                {
                    var v = new Vector2[6];
                    for (int k = 0; k < 6; k++)
                    {
                        float a = (30f + 60f * k) * Mathf.Deg2Rad;
                        v[k] = c + 19f * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    }
                    float d = float.MaxValue;
                    for (int k = 0; k < 6; k++) d = Mathf.Min(d, Segment(p, v[k], v[(k + 1) % 6], 3f));
                    // v[0]=30deg, v[2]=150deg, v[4]=270deg
                    d = Mathf.Min(d, Segment(p, c, v[0], 3f));
                    d = Mathf.Min(d, Segment(p, c, v[2], 3f));
                    d = Mathf.Min(d, Segment(p, c, v[4], 3f));
                    return d;
                }),
            };
        }

        static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float Ring(Vector2 p, Vector2 c, float r, float t) => Mathf.Abs((p - c).magnitude - r) - t * 0.5f;

        static float Segment(Vector2 p, Vector2 a, Vector2 b, float t)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - t * 0.5f;
        }

        // Sprites are drawn at 2x and imported at 200 PPU so they stay crisp at 2560x1440.
        const int Scale = 2;

        static Sprite RoundRect(string name, int radius, float thickness)
        {
            int logical = 2 * radius + 4;
            int size = logical * Scale;
            float half = size * 0.5f, r = radius * Scale, t = thickness * Scale;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - r);
                float qy = Mathf.Abs(y + 0.5f - half) - (half - r);
                float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
                float a = Mathf.Clamp01(0.5f - d);
                if (t > 0) a *= Mathf.Clamp01(0.5f + d + t);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
            }
            return SaveSprite(name, size, px, radius * Scale + 1);
        }

        static Sprite Icon(string name, int logical, Func<Vector2, float> sdf)
        {
            int size = logical * Scale;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / Scale, (y + 0.5f) / Scale);
                float a = Mathf.Clamp01(0.5f - sdf(p) * Scale);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
            }
            return SaveSprite(name, size, px, 0);
        }

        static Sprite SaveSprite(string name, int size, Color32[] px, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            string path = SpriteDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100 * Scale;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.spriteBorder = new Vector4(border, border, border, border);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------------------------------------------------------- fonts

        static void CreateFonts()
        {
            sgBold = FontAsset("SpaceGrotesk-Bold");
            sgMedium = FontAsset("SpaceGrotesk-Medium");
            jbMedium = FontAsset("JetBrainsMono-Medium");
        }

        static TMP_FontAsset FontAsset(string fontName)
        {
            string path = FontDir + "/" + fontName + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/" + fontName + ".ttf");
            if (font == null) throw new Exception("Missing font " + fontName);

            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            fa.name = fontName + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = fontName + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = fontName + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);

            var chars = new System.Text.StringBuilder();
            for (char ch = (char)32; ch < 127; ch++) chars.Append(ch);
            chars.Append("—–·×←→");
            fa.TryAddCharacters(chars.ToString(), out _);

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        // ---------------------------------------------------------------- UI helpers

        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Band(RectTransform rt, float anchorY, float left, float right, float y, float height)
        {
            rt.anchorMin = new Vector2(0, anchorY);
            rt.anchorMax = new Vector2(1, anchorY);
            rt.pivot = new Vector2(0.5f, anchorY);
            rt.offsetMin = new Vector2(left, 0);
            rt.offsetMax = new Vector2(-right, 0);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        }

        static UnityEngine.UI.Image Img(RectTransform rt, Sprite sprite, Color color, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            img.type = sprite != null && sprite.border != Vector4.zero
                ? UnityEngine.UI.Image.Type.Sliced
                : UnityEngine.UI.Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        static TextMeshProUGUI Text(RectTransform rt, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, float spacing = 0f, bool wrap = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.characterSpacing = spacing;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static RectTransform Canvas(string name, out GameObject go)
        {
            go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var rt = (RectTransform)go.transform;
            var bg = UI("Background", rt);
            Stretch(bg, 0, 0, 0, 0);
            Img(bg, null, Palette.Background);

            var content = UI("Content", rt);
            Stretch(content, 72, 56, 72, 56);
            return content;
        }

        static void EventSystemObject()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        static void SetupCamera(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
        }

        static MenuOption Card(Transform parent, int index, string title, string desc, Sprite iconSprite)
        {
            var rt = UI("Card_" + title.Replace(" ", ""), parent);
            var bg = Img(rt, fillR16, Palette.Card, true);
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.targetGraphic = bg;

            var ring = UI("FocusRing", rt);
            Stretch(ring, -4, -4, -4, -4);
            var ringImg = Img(ring, ringR20w2, Palette.Muted);

            var border = UI("Border", rt);
            Stretch(border, 0, 0, 0, 0);
            var borderImg = Img(border, lineR16w1, Palette.Border);

            var icon = UI("Icon", rt);
            Place(icon, new Vector2(0, 1), new Vector2(24, -24), new Vector2(48, 48));
            var iconImg = Img(icon, iconSprite, Palette.Muted);

            var num = UI("Index", rt);
            Place(num, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(60, 20));
            Text(num, (index + 1).ToString("00"), jbMedium, 12, Palette.Muted, TextAlignmentOptions.TopRight, 4);

            var name = UI("Name", rt);
            Band(name, 0, 24, 24, 52, 32);
            Text(name, title, sgBold, 24, Palette.Text, TextAlignmentOptions.BottomLeft);

            var description = UI("Description", rt);
            Band(description, 0, 24, 24, 24, 20);
            var descText = Text(description, desc, sgMedium, 14, Palette.Muted, TextAlignmentOptions.TopLeft);
            descText.overflowMode = TextOverflowModes.Ellipsis;

            var opt = rt.gameObject.AddComponent<MenuOption>();
            opt.Setup(MenuOption.Style.Card, bg, borderImg, ringImg, iconImg, null, lineR16w1, lineR16w2);
            return opt;
        }

        static MenuOption Pill(Transform parent, string objName, string label, Vector2 pos, Vector2 size)
        {
            var rt = UI(objName, parent);
            Place(rt, Vector2.zero, pos, size);
            var bg = Img(rt, fillR10, Palette.Card, true);
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.targetGraphic = bg;

            var ring = UI("FocusRing", rt);
            Stretch(ring, -4, -4, -4, -4);
            var ringImg = Img(ring, ringR14w2, Palette.Muted);

            var border = UI("Border", rt);
            Stretch(border, 0, 0, 0, 0);
            var borderImg = Img(border, lineR10w1, Palette.Border);

            var text = UI("Label", rt);
            Stretch(text, 0, 0, 0, 0);
            var tmp = Text(text, label, jbMedium, 14, Palette.Text, TextAlignmentOptions.Center, 2);

            var opt = rt.gameObject.AddComponent<MenuOption>();
            opt.Setup(MenuOption.Style.Pill, bg, borderImg, ringImg, null, tmp, null, null);
            return opt;
        }

        static TextMeshProUGUI SmallLabel(Transform parent, string objName, string text, Vector2 pos, float width)
        {
            var rt = UI(objName, parent);
            Place(rt, Vector2.zero, pos, new Vector2(width, 16));
            return Text(rt, text, jbMedium, 12, Palette.Muted, TextAlignmentOptions.BottomLeft, 6);
        }

        static void Header(RectTransform content, bool showBest)
        {
            var header = UI("Header", content);
            Band(header, 1, 0, 0, 0, 80);

            var logoRt = UI("Logo", header);
            Place(logoRt, new Vector2(0, 1), new Vector2(0, -4), new Vector2(44, 44));
            Img(logoRt, logo, Palette.Accent);

            var title = UI("Title", header);
            Place(title, new Vector2(0, 1), new Vector2(60, 0), new Vector2(400, 52));
            Text(title, "TAPSHOT", sgBold, 44, Palette.Text, TextAlignmentOptions.Left, 2);

            var tagline = UI("Tagline", header);
            Band(tagline, 1, 0, 0, -58, 22);
            Text(tagline, "Four drills in one 3D room. Thirty seconds each. Get sharper.", sgMedium, 16,
                Palette.Muted, TextAlignmentOptions.Left);

            if (showBest)
            {
                var best = UI("BestToday", header);
                Place(best, new Vector2(1, 1), new Vector2(0, 0), new Vector2(320, 52));
                Text(best, "Best today: <color=#E8ECF2>—</color>", jbMedium, 14, Palette.Muted,
                    TextAlignmentOptions.Right, 2);
            }
        }

        // ---------------------------------------------------------------- scenes

        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == MenuCanvasName || go.GetComponent<EventSystem>() != null)
                    UnityEngine.Object.DestroyImmediate(go);
                else if (go.TryGetComponent(out Camera cam))
                    SetupCamera(cam);
            }

            var content = Canvas(MenuCanvasName, out var canvasGo);
            Header(content, true);

            // Drill picker
            var picker = UI("DrillPicker", content);
            Stretch(picker, 0, 96, 0, 128);

            var label = UI("Label", picker);
            Band(label, 1, 0, 0, 0, 18);
            Text(label, "CHOOSE A DRILL", jbMedium, 13, Palette.Muted, TextAlignmentOptions.TopLeft, 8);

            var cards = UI("Cards", picker);
            Stretch(cards, 0, 0, 0, 34);
            var hlg = cards.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            var cardOptions = new MenuOption[Drills.Length];
            for (int i = 0; i < Drills.Length; i++)
                cardOptions[i] = Card(cards, i, Drills[i].name, Drills[i].desc, drillIcons[i]);

            // Footer
            var footer = UI("Footer", content);
            Band(footer, 0, 0, 0, 0, 56);

            var sizeGroup = UI("TargetSize", footer);
            Place(sizeGroup, Vector2.zero, Vector2.zero, new Vector2(160, 56));
            SmallLabel(sizeGroup, "Label", "TARGET SIZE", new Vector2(0, 40), 200);
            var sizes = new[] { "S", "M", "L" };
            var sizeOptions = new MenuOption[3];
            for (int i = 0; i < 3; i++)
                sizeOptions[i] = Pill(sizeGroup, "Pill_" + sizes[i], sizes[i], new Vector2(i * 56, 0), new Vector2(48, 32));

            var lengthGroup = UI("Length", footer);
            Place(lengthGroup, Vector2.zero, new Vector2(200, 0), new Vector2(136, 56));
            SmallLabel(lengthGroup, "Label", "LENGTH", new Vector2(0, 40), 136);
            var lengthOptions = new MenuOption[SessionSettings.RoundLengths.Length];
            for (int i = 0; i < lengthOptions.Length; i++)
            {
                string s = SessionSettings.RoundLengths[i] + "s";
                lengthOptions[i] = Pill(lengthGroup, "Pill_" + s, s, new Vector2(i * 72, 0), new Vector2(64, 32));
            }

            var start = UI("StartButton", footer);
            Place(start, new Vector2(1, 0), Vector2.zero, new Vector2(200, 56));
            var startBg = Img(start, fillR12, Palette.Accent, true);
            var startBtn = start.gameObject.AddComponent<UnityEngine.UI.Button>();
            startBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            startBtn.targetGraphic = startBg;
            var startRing = UI("FocusRing", start);
            Stretch(startRing, -4, -4, -4, -4);
            var startRingImg = Img(startRing, lineR16w2, Palette.Text);
            var startLabel = UI("Label", start);
            Stretch(startLabel, 0, 0, 0, 0);
            var startText = Text(startLabel, "START", sgBold, 18, Palette.Background, TextAlignmentOptions.Center, 8);
            var startOpt = start.gameObject.AddComponent<MenuOption>();
            startOpt.Setup(MenuOption.Style.Primary, startBg, null, startRingImg, null, startText, null, null);

            var controller = canvasGo.AddComponent<MainMenuController>();
            controller.Setup(cardOptions, sizeOptions, lengthOptions, startOpt);

            // Editor preview of the default state (Grid, M, 30s).
            cardOptions[0].SetSelected(true);
            sizeOptions[1].SetSelected(true);
            lengthOptions[0].SetSelected(true);
            foreach (var ring in canvasGo.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(x => x.name == "FocusRing"))
                ring.enabled = false;

            EventSystemObject();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static string BuildDrillScene(DrillType drill)
        {
            string sceneName = SessionSettings.SceneFor(drill);
            string path = SceneDir + "/" + sceneName + ".unity";
            if (drill == DrillType.Track) return BuildTrackScene(path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            SetupCamera(camGo.GetComponent<Camera>());

            var content = Canvas("DrillCanvas", out var canvasGo);
            Header(content, false);

            var title = UI("SceneTitle", content);
            Band(title, 1, 0, 0, -128, 40);
            var titleText = Text(title, sceneName, sgBold, 32, Palette.Text, TextAlignmentOptions.Left);

            var note = UI("Note", content);
            Band(note, 1, 0, 0, -172, 22);
            Text(note, "Placeholder scene. Gameplay isn't built yet; these are the settings it received.",
                sgMedium, 16, Palette.Muted, TextAlignmentOptions.Left);

            var panel = UI("SettingsPanel", content);
            Place(panel, new Vector2(0, 1), new Vector2(0, -222), new Vector2(520, 172));
            Img(panel, fillR16, Palette.Card);
            var panelBorder = UI("Border", panel);
            Stretch(panelBorder, 0, 0, 0, 0);
            Img(panelBorder, lineR16w1, Palette.Border);
            var panelLabel = UI("Label", panel);
            Place(panelLabel, new Vector2(0, 1), new Vector2(24, -24), new Vector2(472, 18));
            Text(panelLabel, "SESSIONSETTINGS", jbMedium, 12, Palette.Muted, TextAlignmentOptions.TopLeft, 6);
            var details = UI("Details", panel);
            Place(details, new Vector2(0, 1), new Vector2(24, -60), new Vector2(472, 116));
            var detailsText = Text(details, "DrillType\nTargetSize\nRoundSeconds", jbMedium, 18, Palette.Text,
                TextAlignmentOptions.TopLeft);
            detailsText.lineSpacing = 30;

            var footer = UI("Footer", content);
            Band(footer, 0, 0, 0, 0, 56);
            var back = Pill(footer, "BackButton", "BACK TO MENU", Vector2.zero, new Vector2(220, 48));
            var hint = UI("Hint", footer);
            Place(hint, Vector2.zero, new Vector2(240, 16), new Vector2(240, 16));
            Text(hint, "Esc / B", jbMedium, 12, Palette.Muted, TextAlignmentOptions.BottomLeft, 4);
            back.transform.Find("FocusRing").GetComponent<UnityEngine.UI.Image>().enabled = false;

            canvasGo.AddComponent<DrillPlaceholder>().Setup(titleText, detailsText, back);

            EventSystemObject();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------- track drill

        static Material MakeMaterial(string name, Color color, float smoothness)
        {
            string dir = Root + "/Materials";
            Directory.CreateDirectory(dir);
            string path = dir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static UnityEngine.UI.Image SolidBar(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var rt = UI(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return Img(rt, null, Palette.Text);
        }

        static string BuildTrackScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);

            // Player: yaw/pitch controller with the camera as a child, eye height 1.6.
            var playerGo = new GameObject("Player", typeof(PlayerController));
            playerGo.transform.position = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(playerGo.transform, false);
            var cam = camGo.GetComponent<Camera>();
            SetupCamera(cam);
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            var urpData = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpData != null) camGo.AddComponent(urpData);
            var player = playerGo.GetComponent<PlayerController>();
            player.Setup(cam);

            // Lighting
            var lightGo = new GameObject("Directional Light", typeof(Light));
            lightGo.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.9f);

            // Room: back wall + floor give depth cues
            var wallMat = MakeMaterial("Track_Wall", Palette.CardSelected, 0.1f);
            var floorMat = MakeMaterial("Track_Floor", Palette.Card, 0.2f);
            var room = new GameObject("Room");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "BackWall";
            wall.transform.SetParent(room.transform, false);
            wall.transform.position = new Vector3(0f, 3.5f, 18f);
            wall.transform.localScale = new Vector3(30f, 14f, 0.5f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            UnityEngine.Object.DestroyImmediate(wall.GetComponent<Collider>());
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform, false);
            floor.transform.position = new Vector3(0f, -0.25f, 8f);
            floor.transform.localScale = new Vector3(30f, 0.5f, 40f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());

            // Target ball
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TrackTarget";
            ball.transform.position = new Vector3(0f, 1.6f, 14f);
            ball.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Track_Ball", Palette.Accent, 0.6f);
            var targetComp = ball.AddComponent<TrackTarget>();

            // HUD
            var canvasGo = new GameObject("HUDCanvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var hud = (RectTransform)canvasGo.transform;

            // Crosshair: four bars with a gap + centre dot (plain UI Images).
            var crossRt = UI("Crosshair", hud);
            crossRt.anchorMin = crossRt.anchorMax = crossRt.pivot = new Vector2(0.5f, 0.5f);
            crossRt.anchoredPosition = Vector2.zero;
            crossRt.sizeDelta = new Vector2(40, 40);
            const float gap = 7f, len = 12f, thick = 2f;
            float off = gap + len * 0.5f;
            var parts = new[]
            {
                SolidBar(crossRt, "Top", new Vector2(0, off), new Vector2(thick, len)),
                SolidBar(crossRt, "Bottom", new Vector2(0, -off), new Vector2(thick, len)),
                SolidBar(crossRt, "Left", new Vector2(-off, 0), new Vector2(len, thick)),
                SolidBar(crossRt, "Right", new Vector2(off, 0), new Vector2(len, thick)),
                SolidBar(crossRt, "Dot", Vector2.zero, new Vector2(thick, thick)),
            };
            var crosshair = crossRt.gameObject.AddComponent<Crosshair>();
            crosshair.Setup(parts);

            var timeRt = UI("Time", hud);
            Place(timeRt, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(200, 64));
            timeRt.pivot = new Vector2(0.5f, 1);
            var timeText = Text(timeRt, "30", sgBold, 48, Palette.Text, TextAlignmentOptions.Top);

            var scoreRt = UI("Hits", hud);
            Place(scoreRt, new Vector2(0, 1), new Vector2(32, -32), new Vector2(260, 32));
            var scoreText = Text(scoreRt, "HITS  0", jbMedium, 20, Palette.Text, TextAlignmentOptions.TopLeft, 2);

            var accRt = UI("Accuracy", hud);
            Place(accRt, new Vector2(1, 1), new Vector2(-32, -32), new Vector2(260, 32));
            var accText = Text(accRt, "ACC  0%", jbMedium, 20, Palette.Text, TextAlignmentOptions.TopRight, 2);

            var hintRt = UI("Hint", hud);
            Place(hintRt, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(400, 20));
            hintRt.pivot = new Vector2(0.5f, 0);
            Text(hintRt, "Click to shoot  ·  Esc for menu", jbMedium, 12, Palette.Muted, TextAlignmentOptions.Bottom, 4);

            var resultRt = UI("Result", hud);
            Stretch(resultRt, 0, 0, 0, 0);
            var resultBg = Img(resultRt, null, new Color(Palette.Background.r, Palette.Background.g, Palette.Background.b, 0.8f));
            var resultTextRt = UI("Text", resultRt);
            Stretch(resultTextRt, 0, 0, 0, 0);
            var resultText = Text(resultTextRt, "", sgBold, 24, Palette.Text, TextAlignmentOptions.Center, 2, true);
            resultText.lineSpacing = 20;

            var drill = canvasGo.AddComponent<TrackDrill>();
            drill.Setup(player, targetComp, crosshair, timeText, scoreText, accText, resultText);

            EventSystemObject();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static void UpdateBuildSettings(List<string> drillPaths)
        {
            var wanted = new List<string> { MenuScenePath };
            wanted.AddRange(drillPaths);
            var list = wanted.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            list.AddRange(EditorBuildSettings.scenes.Where(s => !wanted.Contains(s.path)));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
