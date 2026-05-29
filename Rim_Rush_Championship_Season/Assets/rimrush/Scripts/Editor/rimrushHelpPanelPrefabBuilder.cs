using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace rimrush.EditorTools
{
    public static class rimrushHelpPanelPrefabBuilder
    {
        private const string HelpAssetRoot = "Assets/rimrush/Resources/rimrush/Help";
        private const string HelpTmpFontRoot = "Assets/rimrush/Resources/rimrush/Fonts/TMP";
        private const string PrefabRoot = "Assets/rimrush/Resources/rimrush/Prefabs/UI";
        private const string PrefabPath = PrefabRoot + "/RimrushHelpPanel.prefab";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("rimrush/Build Help Panel Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(HelpAssetRoot);
            Directory.CreateDirectory(HelpTmpFontRoot);
            Directory.CreateDirectory(PrefabRoot);
            CreateTextureAssets();
            CreateTmpFontAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var prefab = BuildPrefab();
            AddPrefabInstanceToMainScene(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Rimrush help panel prefab and scene instance built.");
        }

        private static GameObject BuildPrefab()
        {
            var root = new GameObject("RimrushHelpPanel");
            var panel = root.AddComponent<rimrushHelpPanel>();
            var panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(root.transform, false);

            var buttons = new List<rimrushHelpButton>();
            var dim = LoadSprite("help_dim");
            var board = LoadSprite("help_board");
            var card = LoadSprite("help_card");
            var stage = LoadSprite("help_stage");
            var tab = LoadSprite("help_tab");
            var chip = LoadSprite("help_chip");
            var keycap = LoadSprite("help_keycap");
            var spotlight = LoadSprite("help_spotlight");

            AddSprite("DimBackdrop", dim, 400f, 240f, 0.88f, 800f, 480f, 870, panelRoot.transform);
            AddSprite("MainBoard", board, 400f, 246f, 0.87f, 742f, 430f, 880, panelRoot.transform);
            AddSprite("HeaderRule", LoadSprite("help_line"), 400f, 96f, 0.865f, 690f, 2f, 901, panelRoot.transform);

            AddText("Title", "HOW TO PLAY", 65f, 56f, 34, new Color32(0xFF, 0xB7, 0x3C, 0xFF), TextAnchor.MiddleLeft, 930, panelRoot.transform, rimrushTextStyle.DisplayTitle);
            AddText("Subtitle", "WITCH TRAINING BOARD", 68f, 83f, 12, new Color32(0xB7, 0xFF, 0xE2, 0xFF), TextAnchor.MiddleLeft, 931, panelRoot.transform, rimrushTextStyle.TournamentAccent);

            var keyboardTab = CreateTextButton(
                "KeyboardTab",
                "KEYBOARD",
                rimrushHelpButtonAction.KeyboardTab,
                tab,
                544f,
                58f,
                116f,
                34f,
                panelRoot.transform,
                915,
                buttons);
            var rulesTab = CreateTextButton(
                "RulesTab",
                "RULES",
                rimrushHelpButtonAction.RulesTab,
                tab,
                662f,
                58f,
                96f,
                34f,
                panelRoot.transform,
                915,
                buttons);
            CreateTextButton(
                "CloseButton",
                "X",
                rimrushHelpButtonAction.Close,
                tab,
                738f,
                58f,
                38f,
                34f,
                panelRoot.transform,
                918,
                buttons,
                18);

            var keyboardPage = new GameObject("KeyboardPage");
            keyboardPage.transform.SetParent(panelRoot.transform, false);
            var rulesPage = new GameObject("RulesPage");
            rulesPage.transform.SetParent(panelRoot.transform, false);

            BuildKeyboardPage(keyboardPage.transform, card, stage, chip, keycap, spotlight, buttons, out var demoRows, out var demoTitle, out var demoDescription, out var demoCoach, out var witchMount, out var witchSpotlight);
            BuildRulesPage(rulesPage.transform, card, stage);

            panel.EditorConfigure(
                panelRoot,
                keyboardPage,
                rulesPage,
                buttons.ToArray(),
                keyboardTab.Plate,
                rulesTab.Plate,
                keyboardTab.Label,
                rulesTab.Label,
                demoRows,
                demoTitle,
                demoDescription,
                demoCoach,
                witchMount,
                witchSpotlight);

            keyboardPage.SetActive(true);
            rulesPage.SetActive(false);
            panelRoot.SetActive(true);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildKeyboardPage(
            Transform parent,
            Sprite card,
            Sprite stage,
            Sprite chip,
            Sprite keycap,
            Sprite spotlight,
            List<rimrushHelpButton> buttons,
            out SpriteRenderer[] demoRows,
            out TMP_Text demoTitle,
            out TMP_Text demoDescription,
            out TMP_Text demoCoach,
            out Transform witchMount,
            out SpriteRenderer witchSpotlight)
        {
            AddText("KeyboardHeader", "KEYBOARD MAP", 64f, 119f, 17, new Color32(0xF2, 0xF7, 0xFF, 0xFF), TextAnchor.MiddleLeft, 930, parent, rimrushTextStyle.TournamentAccent);
            AddText("KeyboardSub", "Same controls in every mode.", 64f, 140f, 11, new Color32(0xC7, 0xD7, 0xE8, 0xFF), TextAnchor.MiddleLeft, 930, parent, rimrushTextStyle.TournamentBody);

            AddControlRow(parent, card, keycap, 65f, 166f, "MOVE", "A / D", "LEFT / RIGHT", "Hold to run.\nDouble-tap: Dash.");
            AddControlRow(parent, card, keycap, 65f, 202f, "JUMP", "W", "UP", "Shoot in air.\nContest shots.");
            AddControlRow(parent, card, keycap, 65f, 238f, "ACTION", "B", "L", "Ball: Shoot.\nNo ball: Steal.");
            AddControlRow(parent, card, keycap, 65f, 274f, "DOWN", "S", "DOWN", "Defense: Block.\nWith ball: Pump.");
            AddControlRow(parent, card, keycap, 65f, 310f, "SUPER", "N / V", "K", "Use when\nenergy is full.");

            AddProfileStrip(parent, card);
            BuildBottomGuide(parent, card);

            AddSprite("PreviewStage", stage, 574f, 206f, 0.86f, 326f, 178f, 895, parent);
            witchSpotlight = AddSprite("WitchSpotlight", spotlight, 552f, 248f, 0.855f, 190f, 62f, 899, parent);
            AddText("PreviewHeader", "DRILL PREVIEW", 430f, 121f, 16, new Color32(0xFF, 0xD2, 0x75, 0xFF), TextAnchor.MiddleLeft, 930, parent, rimrushTextStyle.TournamentAccent);
            AddText("PreviewHint", "Click a chip to replay.", 430f, 140f, 11, new Color32(0xBE, 0xCF, 0xE2, 0xFF), TextAnchor.MiddleLeft, 930, parent, rimrushTextStyle.TournamentBody);

            witchMount = new GameObject("WitchMount").transform;
            witchMount.SetParent(parent, false);
            ApplyPixelTransform(witchMount, 552f, 240f, 0.84f, 1f, 1f);

            demoRows = new SpriteRenderer[7];
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Move, rimrushHelpButtonAction.DemoMove, "MOVE", 702f, 154f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Jump, rimrushHelpButtonAction.DemoJump, "JUMP", 702f, 181f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Shoot, rimrushHelpButtonAction.DemoShoot, "SHOT", 702f, 208f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Pump, rimrushHelpButtonAction.DemoPump, "PUMP", 702f, 235f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Dash, rimrushHelpButtonAction.DemoDash, "DASH", 702f, 262f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Steal, rimrushHelpButtonAction.DemoSteal, "STEAL", 702f, 289f);
            CreateDemoButton(parent, chip, buttons, demoRows, rimrushHelpDemo.Block, rimrushHelpButtonAction.DemoBlock, "BLOCK", 702f, 316f);

            demoTitle = AddText("DemoTitle", "DOWN: BLOCK", 430f, 326f, 14, new Color32(0xD8, 0xFF, 0x89, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentAccent);
            demoDescription = AddText("DemoDescription", "Hold S or DOWN to block.\nJump into the shot path.", 430f, 350f, 10, new Color32(0xF4, 0xF7, 0xFF, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentBody);
            demoCoach = AddText("DemoCoach", "Tip: use DOWN to block.", 430f, 377f, 9, new Color32(0x9F, 0xFF, 0xD3, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentBody);
        }

        private static void AddControlRow(Transform parent, Sprite card, Sprite keycap, float x, float y, string label, string p1Key, string p2Key, string description)
        {
            AddSprite(label + "Card", card, x + 165f, y, 0.858f, 330f, 33f, 892, parent);
            AddText(label + "Label", label, x + 12f, y + 1f, 12, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);
            AddKey(parent, keycap, p1Key, x + 92f, y, p1Key.Length > 5 ? 76f : 52f);
            AddKey(parent, keycap, p2Key, x + 171f, y, p2Key.Length > 5 ? 92f : 52f);
            AddText(label + "Desc", description, x + 222f, y + 1f, 10, new Color32(0xE5, 0xEE, 0xFA, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentBody);
        }

        private static void AddKey(Transform parent, Sprite keycap, string keyText, float x, float y, float width)
        {
            AddSprite("Key_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keycap, x, y, 0.852f, width, 24f, 906, parent);
            AddText("KeyText_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keyText, x, y + 1f, 10, new Color32(0x20, 0x27, 0x32, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.TournamentAccent);
        }

        private static void AddProfileStrip(Transform parent, Sprite card)
        {
            AddSprite("ProfileStrip", card, 230f, 365f, 0.86f, 330f, 34f, 892, parent);
            AddText("ProfileStripTitle", "QUICK PROFILE", 76f, 356f, 10, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);
            AddText("ProfileStripBody", "1P: A D W S B N    2P-L: A D W S B V\n2P-R: LEFT RIGHT UP DOWN L K", 76f, 375f, 8, new Color32(0xE8, 0xF1, 0xFF, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentBody);
        }

        private static void BuildBottomGuide(Transform parent, Sprite card)
        {
            AddGuideCard(parent, card, 170f, 426f, "OFFENSE", "ACTION shoots.\nJump first for air release.\nDOWN becomes pump fake.");
            AddGuideCard(parent, card, 400f, 426f, "DEFENSE", "ACTION swipes for steal.\nDOWN plants a block.\nJump blocks shot paths.");
            AddGuideCard(parent, card, 630f, 426f, "TIMING", "Double-tap to Dash.\nDash has cooldown.\nSuper needs full energy.");
        }

        private static void AddGuideCard(Transform parent, Sprite card, float x, float y, string title, string body)
        {
            AddSprite(title + "GuideCard", card, x, y, 0.855f, 206f, 58f, 891, parent);
            AddText(title + "GuideTitle", title, x - 88f, y - 14f, 10, new Color32(0xB6, 0xFF, 0xDC, 0xFF), TextAnchor.MiddleLeft, 925, parent, rimrushTextStyle.TournamentAccent);
            AddText(title + "GuideBody", body, x - 88f, y + 10f, 8, new Color32(0xD9, 0xE7, 0xF2, 0xFF), TextAnchor.MiddleLeft, 925, parent, rimrushTextStyle.TournamentBody);
        }

        private static void BuildRulesPage(Transform parent, Sprite card, Sprite stage)
        {
            AddSprite("RulesEmptyStage", stage, 400f, 254f, 0.86f, 560f, 242f, 895, parent);
            AddText("RulesTitle", "RULES", 400f, 190f, 28, new Color32(0xFF, 0xB9, 0x48, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.DisplayTitle);
            AddText("RulesEmpty", "COMING SOON", 400f, 245f, 18, new Color32(0xB8, 0xFF, 0xE2, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.TournamentAccent);
            AddText("RulesNote", "Rules text will go here.", 400f, 278f, 10, new Color32(0xC9, 0xD6, 0xE6, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.TournamentBody);
            AddSprite("RulesSmallCard", card, 400f, 347f, 0.855f, 320f, 54f, 898, parent);
        }

        private static void CreateDemoButton(
            Transform parent,
            Sprite chip,
            List<rimrushHelpButton> buttons,
            SpriteRenderer[] demoRows,
            rimrushHelpDemo demo,
            rimrushHelpButtonAction action,
            string label,
            float x,
            float y)
        {
            var created = CreateTextButton(
                "Demo" + label,
                label,
                action,
                chip,
                x,
                y,
                76f,
                23f,
                parent,
                912,
                buttons,
                9);
            demoRows[(int)demo] = created.Plate;
        }

        private readonly struct TextButtonParts
        {
            public TextButtonParts(SpriteRenderer plate, TMP_Text label)
            {
                Plate = plate;
                Label = label;
            }

            public readonly SpriteRenderer Plate;
            public readonly TMP_Text Label;
        }

        private static TextButtonParts CreateTextButton(
            string name,
            string label,
            rimrushHelpButtonAction action,
            Sprite sprite,
            float x,
            float y,
            float width,
            float height,
            Transform parent,
            int sortingOrder,
            List<rimrushHelpButton> buttons,
            int fontSize = 11)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var plate = AddSprite(name + "Plate", sprite, x, y, 0.84f, width, height, sortingOrder, root.transform);
            var text = AddText(name + "Label", label, x, y + 1f, fontSize, new Color32(0xE9, 0xF3, 0xFF, 0xFF), TextAnchor.MiddleCenter, sortingOrder + 20, root.transform, rimrushTextStyle.TournamentAccent);
            var button = root.AddComponent<rimrushHelpButton>();
            button.EditorConfigure(
                action,
                new Vector2(x, y),
                new Vector2(width, height),
                root.transform,
                new[] { plate },
                new[] { text },
                new Color32(0x22, 0x30, 0x4C, 0xF2),
                new Color32(0x36, 0x4C, 0x70, 0xFF),
                new Color32(0x2D, 0xE6, 0xA3, 0xEE),
                new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                new Color32(0xFF, 0xD6, 0x6A, 0xFF),
                new Color32(0x14, 0x1B, 0x25, 0xFF),
                1.035f);
            buttons.Add(button);
            return new TextButtonParts(plate, text);
        }

        private static SpriteRenderer AddSprite(string name, Sprite sprite, float x, float y, float z, float width, float height, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            var spriteWidth = sprite != null ? Mathf.Max(1f, sprite.rect.width) : 1f;
            var spriteHeight = sprite != null ? Mathf.Max(1f, sprite.rect.height) : 1f;
            ApplyPixelTransform(go.transform, x, y, z, width / spriteWidth, height / spriteHeight);
            return renderer;
        }

        private static TMP_Text AddText(string name, string text, float x, float y, int fontSize, Color color, TextAnchor anchor, int sortingOrder, Transform parent, rimrushTextStyle style)
        {
            return rimrushRender.TmpText(name, text, x, y, fontSize, color, anchor, sortingOrder, parent, style);
        }

        private static void ApplyPixelTransform(Transform transform, float x, float y, float z, float scaleX, float scaleY)
        {
            transform.position = rimrushConstants.PixelToWorldSnapped(x, y, z);
            transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * scaleX,
                rimrushConstants.UnitsPerPixel * scaleY,
                1f);
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{HelpAssetRoot}/{name}.png");
        }

        private static void CreateTextureAssets()
        {
            WriteTexture("help_dim", CreateSolidTexture(16, 16, new Color(0f, 0.02f, 0.06f, 0.78f)));
            WriteTexture("help_line", CreateSolidTexture(8, 8, new Color(0.75f, 0.98f, 1f, 0.22f)));
            WriteTexture("help_board", CreateRoundedTexture(742, 430, 22, 3, new Color32(0x10, 0x15, 0x25, 0xF6), new Color32(0x26, 0x19, 0x45, 0xF6), new Color32(0xFF, 0xA6, 0x36, 0xFF), new Color32(0x61, 0xFF, 0xCB, 0x44), true));
            WriteTexture("help_stage", CreateRoundedTexture(326, 210, 18, 2, new Color32(0x0B, 0x14, 0x23, 0xF2), new Color32(0x18, 0x27, 0x3E, 0xF2), new Color32(0x7D, 0xC8, 0xFF, 0x66), new Color32(0xFF, 0xC1, 0x55, 0x34), true));
            WriteTexture("help_card", CreateRoundedTexture(330, 86, 10, 2, new Color32(0x12, 0x1D, 0x31, 0xE8), new Color32(0x0D, 0x13, 0x23, 0xE8), new Color32(0x8B, 0xA2, 0xC5, 0x5A), new Color32(0x91, 0xFF, 0xD6, 0x2A), false));
            WriteTexture("help_tab", CreateRoundedTexture(116, 34, 9, 2, new Color32(0x24, 0x34, 0x55, 0xF5), new Color32(0x14, 0x20, 0x36, 0xF5), new Color32(0xFF, 0xC5, 0x4E, 0x66), new Color32(0xFF, 0xFF, 0xFF, 0x20), false));
            WriteTexture("help_chip", CreateRoundedTexture(86, 26, 8, 2, new Color32(0x18, 0x27, 0x3F, 0xF2), new Color32(0x0C, 0x15, 0x25, 0xF2), new Color32(0x70, 0xFF, 0xCC, 0x66), new Color32(0xFF, 0xB5, 0x43, 0x22), false));
            WriteTexture("help_keycap", CreateRoundedTexture(92, 30, 7, 2, new Color32(0xFF, 0xF8, 0xE9, 0xFF), new Color32(0xC9, 0xD5, 0xE0, 0xFF), new Color32(0x49, 0x54, 0x63, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0x55), false));
            WriteTexture("help_spotlight", CreateSpotlightTexture(256, 96));
        }

        private static void CreateTmpFontAssets()
        {
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/AgencyBold.ttf", "AgencyBold SDF");
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/Impact2.ttf", "Impact2 SDF");
        }

        private static void CreateTmpFontAsset(string sourcePath, string assetName)
        {
            var assetPath = $"{HelpTmpFontRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && existing.atlasTexture != null && existing.material != null)
            {
                return;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"Cannot build help TMP font asset because the source font is missing: {sourcePath}");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                144,
                12,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                Debug.LogWarning($"TMP font asset creation failed for {sourcePath}");
                return;
            }

            fontAsset.name = assetName;
            var atlasTexture = fontAsset.atlasTexture;
            if (atlasTexture != null)
            {
                atlasTexture.name = assetName + " Atlas";
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                fontAsset.material.mainTexture = atlasTexture;
                fontAsset.material.SetFloat("_Sharpness", 0.18f);
                fontAsset.material.SetFloat("_PerspectiveFilter", 0f);
            }

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRoundedTexture(int width, int height, int radius, int borderWidth, Color top, Color bottom, Color border, Color glint, bool pattern)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (!InsideRoundedRect(x, y, width, height, radius))
                    {
                        pixels[index] = Color.clear;
                        continue;
                    }

                    var vertical = height <= 1 ? 0f : y / (float)(height - 1);
                    var color = Color.Lerp(bottom, top, vertical);
                    if (pattern && ((x + y * 3) % 97 < 2 || (x * 2 + y) % 131 < 2))
                    {
                        color = Color.Lerp(color, glint, 0.45f);
                    }

                    var edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    if (edgeDistance < borderWidth + 0.5f || !InsideRoundedRect(x, y, width, height, radius - borderWidth))
                    {
                        color = Color.Lerp(color, border, 0.72f);
                    }
                    else if (y > height - radius - 12 && x > radius && x < width - radius)
                    {
                        color = Color.Lerp(color, glint, 0.24f);
                    }

                    pixels[index] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSpotlightTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var center = new Vector2(width * 0.5f, height * 0.55f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalized = new Vector2((x - center.x) / (width * 0.5f), (y - center.y) / (height * 0.48f));
                    var d = normalized.sqrMagnitude;
                    var alpha = Mathf.Clamp01(1f - d);
                    alpha = alpha * alpha * 0.46f;
                    pixels[y * width + x] = new Color(0.55f, 1f, 0.82f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            radius = Mathf.Max(0, radius);
            if (radius <= 0)
            {
                return x >= 0 && y >= 0 && x < width && y < height;
            }

            var cx = x < radius ? radius : x >= width - radius ? width - radius - 1 : x;
            var cy = y < radius ? radius : y >= height - radius ? height - radius - 1 : y;
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void WriteTexture(string name, Texture2D texture)
        {
            var path = $"{HelpAssetRoot}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void AddPrefabInstanceToMainScene(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot add help panel instance because prefab build failed.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var panels = Resources.FindObjectsOfTypeAll<rimrushHelpPanel>();
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                if (panel != null && panel.gameObject.scene == scene)
                {
                    Object.DestroyImmediate(panel.gameObject);
                }
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance != null)
            {
                instance.name = "RimrushHelpPanel";
                instance.transform.position = Vector3.zero;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
