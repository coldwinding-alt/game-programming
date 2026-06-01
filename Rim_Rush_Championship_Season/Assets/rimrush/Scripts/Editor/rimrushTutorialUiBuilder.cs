using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace rimrush.EditorTools
{
    public static class rimrushTutorialUiBuilder
    {
        private const string TutorialAssetRoot = "Assets/rimrush/Resources/rimrush/Images/UI/Tutorial";
        private const string TutorialTmpFontRoot = "Assets/rimrush/Resources/rimrush/Fonts/TMP";
        private const string PrefabRoot = "Assets/rimrush/Resources/rimrush/Prefabs/UI";
        private const string MenuPrefabPath = PrefabRoot + "/RimrushTutorialMenuPanel.prefab";
        private const string OverlayPrefabPath = PrefabRoot + "/RimrushTutorialOverlay.prefab";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        private static readonly Color White = Color.white;
        private static readonly Color Clear = Color.clear;
        private static readonly Color32 Ink = new Color32(0xEC, 0xF5, 0xFF, 0xFF);
        private static readonly Color32 MutedInk = new Color32(0xBA, 0xCA, 0xDE, 0xFF);
        private static readonly Color32 AccentGold = new Color32(0xFF, 0xCB, 0x63, 0xFF);
        private static readonly Color32 AccentMint = new Color32(0x88, 0xFF, 0xD4, 0xFF);
        private static readonly Color32 AccentOrange = new Color32(0xFF, 0x9D, 0x4E, 0xFF);
        private static readonly Color32 AccentDark = new Color32(0x19, 0x13, 0x0A, 0xFF);
        private static readonly Color32 Slate = new Color32(0x1F, 0x2B, 0x41, 0xF8);
        private static readonly Color32 SlateDeep = new Color32(0x10, 0x15, 0x23, 0xFF);
        private static readonly Color32 MaskTint = new Color32(0x03, 0x08, 0x12, 0xD4);

        private readonly struct ButtonParts
        {
            public ButtonParts(RectTransform root, Image plate, Button button, TextMeshProUGUI label)
            {
                Root = root;
                Plate = plate;
                Button = button;
                Label = label;
            }

            public RectTransform Root { get; }
            public Image Plate { get; }
            public Button Button { get; }
            public TextMeshProUGUI Label { get; }
        }

        [MenuItem("rimrush/Build Tutorial UI")]
        public static void Build()
        {
            Directory.CreateDirectory(TutorialAssetRoot);
            Directory.CreateDirectory(TutorialTmpFontRoot);
            Directory.CreateDirectory(PrefabRoot);

            CreateTextureAssets();
            CreateTmpFontAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var menuPrefab = BuildMenuPrefab();
            var overlayPrefab = BuildOverlayPrefab();
            AddPrefabInstancesToMainScene(menuPrefab, overlayPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("Rimrush tutorial UI prefabs and scene instances built.");
        }

        private static GameObject BuildMenuPrefab()
        {
            var fonts = LoadFonts();
            var sprites = LoadSprites();

            var root = CreateCanvasRoot("RimrushTutorialMenuPanel", 360);
            root.SetActive(false);
            var panel = root.AddComponent<rimrushTutorialMenuPanel>();

            var panelRoot = CreateStretchRect("PanelRoot", root.transform);
            CreateStretchImage("Dim", panelRoot, sprites.Dim, White, Image.Type.Sliced);

            var panelGlow = CreateImage(
                "PanelGlow",
                panelRoot,
                sprites.Glow,
                new Vector2(0f, -2f),
                new Vector2(780f, 446f),
                new Color32(0x92, 0xFF, 0xDD, 0xCC),
                Image.Type.Sliced);

            var board = CreateImage(
                "Board",
                panelRoot,
                sprites.Board,
                new Vector2(0f, -3f),
                new Vector2(728f, 418f),
                White,
                Image.Type.Sliced);

            CreateImage("OrbLeft", panelRoot, sprites.Orb, new Vector2(-316f, 133f), new Vector2(106f, 106f), new Color32(0x79, 0xFF, 0xD3, 0xA4), Image.Type.Simple);
            CreateImage("OrbRight", panelRoot, sprites.Orb, new Vector2(309f, 138f), new Vector2(126f, 126f), new Color32(0xFF, 0xB5, 0x4C, 0xAE), Image.Type.Simple);

            var boardContent = CreateRect(
                "BoardContent",
                board.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(688f, 392f));

            CreateText(
                "Title",
                boardContent,
                fonts.Title,
                "WITCH TUTORIAL",
                new Vector2(-284f, 158f),
                new Vector2(300f, 42f),
                34,
                AccentGold,
                TextAlignmentOptions.Left);

            CreateText(
                "Subtitle",
                boardContent,
                fonts.Body,
                "A fast guided run that shows the full game loop.",
                new Vector2(-282f, 128f),
                new Vector2(360f, 20f),
                13,
                AccentMint,
                TextAlignmentOptions.Left);

            var overviewTab = CreateButton(
                "OverviewTab",
                boardContent,
                sprites.Tab,
                new Vector2(178f, 156f),
                new Vector2(118f, 38f),
                "OVERVIEW",
                fonts.Button,
                14,
                Ink,
                false);

            var controlsTab = CreateButton(
                "ControlsTab",
                boardContent,
                sprites.Tab,
                new Vector2(304f, 156f),
                new Vector2(118f, 38f),
                "CONTROLS",
                fonts.Button,
                14,
                Ink,
                false);

            var closeButton = CreateButton(
                "CloseButton",
                boardContent,
                sprites.Tab,
                new Vector2(347f, 156f),
                new Vector2(40f, 38f),
                "X",
                fonts.Button,
                16,
                Ink,
                false);

            CreateImage(
                "HeaderRule",
                boardContent,
                sprites.Rule,
                new Vector2(0f, 116f),
                new Vector2(652f, 3f),
                new Color32(0xB6, 0xFF, 0xEC, 0xE0),
                Image.Type.Sliced);

            var infoCard = CreateImage(
                "ModeInfoCard",
                boardContent,
                sprites.Card,
                new Vector2(72f, 76f),
                new Vector2(556f, 74f),
                White,
                Image.Type.Sliced);

            var modeTitleText = CreateText(
                "ModeTitle",
                infoCard.transform as RectTransform,
                fonts.Button,
                "LEARN THE FULL MATCH LOOP",
                new Vector2(-244f, 17f),
                new Vector2(420f, 24f),
                18,
                AccentGold,
                TextAlignmentOptions.Left);

            var modeBodyText = CreateText(
                "ModeBody",
                infoCard.transform as RectTransform,
                fonts.Body,
                "Dash, shoot, steal, block, and trigger your signature super in one guided run.",
                new Vector2(-244f, -10f),
                new Vector2(470f, 30f),
                11,
                MutedInk,
                TextAlignmentOptions.Left);
            modeBodyText.enableWordWrapping = true;

            var overviewPage = CreateRect(
                "OverviewPage",
                boardContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(668f, 252f));

            var controlsPage = CreateRect(
                "ControlsPage",
                boardContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f),
                new Vector2(668f, 252f));

            var heroCard = CreateImage(
                "HeroCard",
                overviewPage,
                sprites.Card,
                new Vector2(-170f, 4f),
                new Vector2(314f, 228f),
                White,
                Image.Type.Sliced);

            CreateText(
                "HeroCardTag",
                heroCard.transform as RectTransform,
                fonts.Button,
                "COACHED BY THE WITCH",
                new Vector2(-132f, 87f),
                new Vector2(220f, 18f),
                12,
                AccentMint,
                TextAlignmentOptions.Left);

            CreateText(
                "HeroCardBody",
                heroCard.transform as RectTransform,
                fonts.Body,
                "The examiner learns movement, timing, defense, and flashy character identity by doing it once.",
                new Vector2(-132f, 58f),
                new Vector2(258f, 38f),
                10,
                MutedInk,
                TextAlignmentOptions.Left);

            var characterPreviewMount = CreateRect(
                "CharacterPreviewMount",
                heroCard.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-44f, 0f),
                new Vector2(126f, 156f));

            var witchPreviewMount = CreateRect(
                "WitchPreviewMount",
                heroCard.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(82f, -2f),
                new Vector2(118f, 148f));

            CreateImage("PreviewHalo", heroCard.transform as RectTransform, sprites.Glow, new Vector2(-10f, 10f), new Vector2(240f, 176f), new Color32(0x92, 0xFF, 0xD9, 0x84), Image.Type.Sliced);
            CreateText("GuideTag", heroCard.transform as RectTransform, fonts.Button, "90 SECOND FLOW", new Vector2(70f, -76f), new Vector2(140f, 18f), 12, AccentGold, TextAlignmentOptions.Center);

            var ballPreviewFrame = CreateImage(
                "BallPreviewFrame",
                heroCard.transform as RectTransform,
                sprites.Chip,
                new Vector2(106f, 74f),
                new Vector2(72f, 72f),
                White,
                Image.Type.Sliced);

            var ballPreviewImage = CreateImage(
                "BallPreviewImage",
                ballPreviewFrame.transform as RectTransform,
                null,
                Vector2.zero,
                new Vector2(54f, 54f),
                White,
                Image.Type.Simple);
            ballPreviewImage.preserveAspect = true;

            var valueCard = CreateImage(
                "ValueCard",
                overviewPage,
                sprites.Card,
                new Vector2(170f, 4f),
                new Vector2(314f, 228f),
                White,
                Image.Type.Sliced);

            CreateText(
                "ValueTag",
                valueCard.transform as RectTransform,
                fonts.Button,
                "WHAT THE JUDGE SEES",
                new Vector2(-120f, 87f),
                new Vector2(220f, 18f),
                12,
                AccentGold,
                TextAlignmentOptions.Left);

            CreateFeatureStrip(valueCard.transform as RectTransform, sprites.Card, fonts, new Vector2(0f, 42f), "DASH + RHYTHM", "Double-tap speed and apex timing make the first impression feel skillful.");
            CreateFeatureStrip(valueCard.transform as RectTransform, sprites.Card, fonts, new Vector2(0f, 0f), "DEFENSE MATTERS", "The tutorial proves you can steal, protect the rim, and punish bad offense.");
            CreateFeatureStrip(valueCard.transform as RectTransform, sprites.Card, fonts, new Vector2(0f, -42f), "CHARACTER IDENTITY", "The last beat fires a full super so the game reads deeper than simple shooting.");

            var characterCard = CreateImage(
                "CharacterCard",
                overviewPage,
                sprites.Card,
                new Vector2(-170f, -102f),
                new Vector2(314f, 58f),
                White,
                Image.Type.Sliced);

            CreateText(
                "CharacterLabel",
                characterCard.transform as RectTransform,
                fonts.Button,
                "CHARACTER",
                new Vector2(-118f, 0f),
                new Vector2(104f, 18f),
                11,
                AccentMint,
                TextAlignmentOptions.Left);

            var previousCharacterButton = CreateButton(
                "PreviousCharacter",
                characterCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(-16f, 0f),
                new Vector2(34f, 32f),
                "<",
                fonts.Button,
                18,
                Ink,
                false);

            var nextCharacterButton = CreateButton(
                "NextCharacter",
                characterCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(134f, 0f),
                new Vector2(34f, 32f),
                ">",
                fonts.Button,
                18,
                Ink,
                false);

            var characterNameText = CreateText(
                "CharacterName",
                characterCard.transform as RectTransform,
                fonts.Button,
                "REAPER",
                new Vector2(63f, 0f),
                new Vector2(144f, 24f),
                18,
                Ink,
                TextAlignmentOptions.Center);

            var ballCard = CreateImage(
                "BallCard",
                overviewPage,
                sprites.Card,
                new Vector2(170f, -102f),
                new Vector2(314f, 58f),
                White,
                Image.Type.Sliced);

            CreateText(
                "BallLabel",
                ballCard.transform as RectTransform,
                fonts.Button,
                "BALL THEME",
                new Vector2(-118f, 0f),
                new Vector2(110f, 18f),
                11,
                AccentMint,
                TextAlignmentOptions.Left);

            var previousBallButton = CreateButton(
                "PreviousBall",
                ballCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(-14f, 0f),
                new Vector2(34f, 32f),
                "<",
                fonts.Button,
                18,
                Ink,
                false);

            var nextBallButton = CreateButton(
                "NextBall",
                ballCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(134f, 0f),
                new Vector2(34f, 32f),
                ">",
                fonts.Button,
                18,
                Ink,
                false);

            var ballNameText = CreateText(
                "BallName",
                ballCard.transform as RectTransform,
                fonts.Button,
                "CLASSIC ORIGINAL",
                new Vector2(60f, 0f),
                new Vector2(158f, 24f),
                16,
                Ink,
                TextAlignmentOptions.Center);

            BuildControlsPage(controlsPage, sprites, fonts);

            var backButton = CreateButton(
                "BackButton",
                boardContent,
                sprites.ButtonSecondary,
                new Vector2(-268f, -170f),
                new Vector2(128f, 44f),
                "BACK",
                fonts.Button,
                16,
                Ink,
                false);

            var startTrainingButton = CreateButton(
                "StartTrainingButton",
                boardContent,
                sprites.ButtonSecondary,
                new Vector2(98f, -170f),
                new Vector2(168f, 50f),
                "FREE TRAINING",
                fonts.Button,
                16,
                Ink,
                false);

            var startTutorialButton = CreateButton(
                "StartTutorialButton",
                boardContent,
                sprites.ButtonPrimary,
                new Vector2(272f, -170f),
                new Vector2(190f, 54f),
                "START TUTORIAL",
                fonts.Button,
                18,
                AccentDark,
                true);

            panel.EditorConfigure(
                panelRoot.gameObject,
                overviewPage.gameObject,
                controlsPage.gameObject,
                overviewTab.Plate,
                controlsTab.Plate,
                overviewTab.Label,
                controlsTab.Label,
                characterNameText,
                ballNameText,
                modeTitleText,
                modeBodyText,
                ballPreviewImage,
                closeButton.Button,
                backButton.Button,
                startTutorialButton.Button,
                startTrainingButton.Button,
                previousCharacterButton.Button,
                nextCharacterButton.Button,
                previousBallButton.Button,
                nextBallButton.Button,
                overviewTab.Button,
                controlsTab.Button,
                characterPreviewMount,
                witchPreviewMount,
                panelGlow);

            controlsPage.gameObject.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildOverlayPrefab()
        {
            var fonts = LoadFonts();
            var sprites = LoadSprites();

            var root = CreateCanvasRoot("RimrushTutorialOverlay", 1180);
            root.SetActive(false);
            var overlay = root.AddComponent<rimrushTutorialOverlay>();

            var overlayRoot = CreateStretchRect("OverlayRoot", root.transform);

            var maskTop = CreateTopLeftImage("MaskTop", overlayRoot, sprites.Dim, 0f, 0f, 800f, 92f, MaskTint);
            var maskBottom = CreateTopLeftImage("MaskBottom", overlayRoot, sprites.Dim, 0f, 360f, 800f, 120f, MaskTint);
            var maskLeft = CreateTopLeftImage("MaskLeft", overlayRoot, sprites.Dim, 0f, 92f, 226f, 268f, MaskTint);
            var maskRight = CreateTopLeftImage("MaskRight", overlayRoot, sprites.Dim, 574f, 92f, 226f, 268f, MaskTint);
            var focusGlow = CreateTopLeftImage("FocusGlow", overlayRoot, sprites.Glow, 280f, 110f, 248f, 204f, new Color32(0x8D, 0xFF, 0xDC, 0xA2));
            var focusFrame = CreateTopLeftImage("FocusFrame", overlayRoot, sprites.FocusFrame, 290f, 120f, 228f, 184f, White);

            var feedbackText = CreateText(
                "FeedbackText",
                overlayRoot,
                fonts.Button,
                string.Empty,
                new Vector2(0f, 182f),
                new Vector2(320f, 26f),
                17,
                AccentMint,
                TextAlignmentOptions.Center);

            var boardGlow = CreateImage(
                "BoardGlow",
                overlayRoot,
                sprites.Glow,
                new Vector2(210f, 104f),
                new Vector2(360f, 220f),
                new Color32(0x98, 0xFF, 0xDD, 0x92),
                Image.Type.Sliced);

            var board = CreateImage(
                "Board",
                overlayRoot,
                sprites.Board,
                new Vector2(214f, 108f),
                new Vector2(332f, 196f),
                White,
                Image.Type.Sliced);

            var boardContent = CreateRect(
                "BoardContent",
                board.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(300f, 170f));

            var stepCounterText = CreateText(
                "StepCounter",
                boardContent,
                fonts.Button,
                "STEP 2 / 5",
                new Vector2(-122f, 67f),
                new Vector2(210f, 18f),
                12,
                AccentMint,
                TextAlignmentOptions.Left);

            var titleText = CreateText(
                "Title",
                boardContent,
                fonts.Title,
                "HIT THE SHOT AT THE APEX",
                new Vector2(-122f, 34f),
                new Vector2(242f, 40f),
                22,
                AccentGold,
                TextAlignmentOptions.Left);
            titleText.enableWordWrapping = true;

            var bodyText = CreateText(
                "Body",
                boardContent,
                fonts.Body,
                "Jump first, then release the ball right at the top of your rise.",
                new Vector2(-122f, -6f),
                new Vector2(246f, 56f),
                12,
                Ink,
                TextAlignmentOptions.Left);
            bodyText.enableWordWrapping = true;

            var tipText = CreateText(
                "Tip",
                boardContent,
                fonts.Button,
                "Highest point = cleanest rhythm.",
                new Vector2(-122f, -56f),
                new Vector2(246f, 22f),
                13,
                AccentGold,
                TextAlignmentOptions.Left);

            var footerHintText = CreateText(
                "FooterHint",
                boardContent,
                fonts.Body,
                "Tutorial overlay preview",
                new Vector2(-122f, -80f),
                new Vector2(246f, 18f),
                10,
                MutedInk,
                TextAlignmentOptions.Left);

            var progressDots = new Image[5];
            for (var i = 0; i < progressDots.Length; i++)
            {
                progressDots[i] = CreateImage(
                    $"Dot{i + 1}",
                    boardContent,
                    sprites.Orb,
                    new Vector2(70f + i * 24f, 70f),
                    new Vector2(16f, 16f),
                    i == 1 ? AccentGold : new Color32(0x42, 0x5F, 0x87, 0xFF),
                    Image.Type.Simple);
            }

            var narratorGlow = CreateImage(
                "NarratorGlow",
                overlayRoot,
                sprites.Glow,
                new Vector2(-234f, -140f),
                new Vector2(290f, 170f),
                new Color32(0xFF, 0xBC, 0x64, 0x86),
                Image.Type.Sliced);

            var narratorCard = CreateImage(
                "NarratorCard",
                overlayRoot,
                sprites.Card,
                new Vector2(-234f, -140f),
                new Vector2(258f, 146f),
                White,
                Image.Type.Sliced);

            CreateText("NarratorTag", narratorCard.transform as RectTransform, fonts.Button, "WITCH COACH", new Vector2(-74f, 44f), new Vector2(150f, 18f), 12, AccentGold, TextAlignmentOptions.Left);
            CreateText("NarratorBody", narratorCard.transform as RectTransform, fonts.Body, "Follow the spotlight. One action at a time.", new Vector2(-8f, 66f), new Vector2(126f, 34f), 10, MutedInk, TextAlignmentOptions.Left);

            var witchMount = CreateRect(
                "WitchMount",
                narratorCard.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-76f, 6f),
                new Vector2(118f, 136f));

            var keyChipRoots = new GameObject[3];
            var keyChipLabels = new TMP_Text[3];
            for (var i = 0; i < keyChipRoots.Length; i++)
            {
                var chip = CreateImage(
                    $"KeyChip{i + 1}",
                    overlayRoot,
                    sprites.Chip,
                    new Vector2(134f + i * 92f, -34f),
                    new Vector2(82f, 38f),
                    White,
                    Image.Type.Sliced);
                keyChipRoots[i] = chip.gameObject;
                keyChipLabels[i] = CreateText(
                    $"KeyChipLabel{i + 1}",
                    chip.transform as RectTransform,
                    fonts.Button,
                    i == 0 ? "W" : i == 1 ? "B" : string.Empty,
                    Vector2.zero,
                    new Vector2(68f, 20f),
                    16,
                    AccentDark,
                    TextAlignmentOptions.Center);
            }

            var outroRoot = CreateRect(
                "OutroRoot",
                overlayRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 22f),
                new Vector2(460f, 192f));
            outroRoot.gameObject.SetActive(false);

            var outroCard = CreateImage(
                "OutroCard",
                outroRoot,
                sprites.Board,
                Vector2.zero,
                new Vector2(460f, 192f),
                White,
                Image.Type.Sliced);

            CreateText("OutroTag", outroCard.transform as RectTransform, fonts.Button, "WHAT NEXT", new Vector2(0f, -46f), new Vector2(180f, 18f), 12, AccentMint, TextAlignmentOptions.Center);
            CreateText("OutroHint", outroCard.transform as RectTransform, fonts.Body, "Keep the momentum while the mechanics are fresh.", new Vector2(0f, -70f), new Vector2(260f, 20f), 10, MutedInk, TextAlignmentOptions.Center);

            var replayButton = CreateButton(
                "ReplayButton",
                outroCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(-146f, 28f),
                new Vector2(132f, 46f),
                "REPLAY",
                fonts.Button,
                15,
                Ink,
                false);

            var trainingButton = CreateButton(
                "TrainingButton",
                outroCard.transform as RectTransform,
                sprites.ButtonSecondary,
                new Vector2(0f, 28f),
                new Vector2(144f, 46f),
                "FREE TRAINING",
                fonts.Button,
                14,
                Ink,
                false);

            var quickMatchButton = CreateButton(
                "QuickMatchButton",
                outroCard.transform as RectTransform,
                sprites.ButtonPrimary,
                new Vector2(152f, 28f),
                new Vector2(144f, 48f),
                "QUICK MATCH",
                fonts.Button,
                14,
                AccentDark,
                true);

            overlay.EditorConfigure(
                overlayRoot.gameObject,
                stepCounterText,
                titleText,
                bodyText,
                tipText,
                feedbackText,
                footerHintText,
                progressDots,
                keyChipRoots,
                keyChipLabels,
                maskTop,
                maskBottom,
                maskLeft,
                maskRight,
                focusFrame,
                focusGlow,
                narratorGlow,
                boardGlow,
                witchMount,
                outroRoot.gameObject,
                replayButton.Button,
                trainingButton.Button,
                quickMatchButton.Button);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OverlayPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildControlsPage(RectTransform controlsPage, TutorialSprites sprites, TutorialFonts fonts)
        {
            var controlsCard = CreateImage(
                "ControlsCard",
                controlsPage,
                sprites.Card,
                new Vector2(-170f, 4f),
                new Vector2(314f, 228f),
                White,
                Image.Type.Sliced);

            CreateText("ControlsTag", controlsCard.transform as RectTransform, fonts.Button, "THE ONLY KEYS YOU NEED", new Vector2(-120f, 86f), new Vector2(220f, 18f), 12, AccentGold, TextAlignmentOptions.Left);
            CreateControlRow(controlsCard.transform as RectTransform, sprites, fonts, new Vector2(0f, 50f), "MOVE", "A / D", "Double-tap to dash");
            CreateControlRow(controlsCard.transform as RectTransform, sprites, fonts, new Vector2(0f, 12f), "JUMP", "W", "Leave the floor first");
            CreateControlRow(controlsCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -26f), "ACTION", "B", "Shoot or steal");
            CreateControlRow(controlsCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -64f), "DOWN", "S", "Pump fake / hold ground");
            CreateControlRow(controlsCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -102f), "SUPER", "N", "Use it when full");

            var flowCard = CreateImage(
                "FlowCard",
                controlsPage,
                sprites.Card,
                new Vector2(170f, 4f),
                new Vector2(314f, 228f),
                White,
                Image.Type.Sliced);

            CreateText("FlowTag", flowCard.transform as RectTransform, fonts.Button, "TUTORIAL FLOW", new Vector2(-120f, 86f), new Vector2(180f, 18f), 12, AccentMint, TextAlignmentOptions.Left);
            CreateTimelineStep(flowCard.transform as RectTransform, sprites, fonts, new Vector2(0f, 54f), "1", "DASH", "Burst into space fast.");
            CreateTimelineStep(flowCard.transform as RectTransform, sprites, fonts, new Vector2(0f, 18f), "2", "APEX SHOT", "Release at the highest point.");
            CreateTimelineStep(flowCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -18f), "3", "STEAL", "Get close, then swipe.");
            CreateTimelineStep(flowCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -54f), "4", "BLOCK", "Jump into the path.");
            CreateTimelineStep(flowCard.transform as RectTransform, sprites, fonts, new Vector2(0f, -90f), "5", "SUPER", "Show off the signature move.");
        }

        private static void CreateFeatureStrip(RectTransform parent, Sprite cardSprite, TutorialFonts fonts, Vector2 position, string title, string body)
        {
            var strip = CreateImage("Feature_" + title.Replace(" ", string.Empty), parent, cardSprite, position, new Vector2(278f, 36f), new Color32(0xFF, 0xFF, 0xFF, 0xEA), Image.Type.Sliced);
            CreateText(title + "_Title", strip.transform as RectTransform, fonts.Button, title, new Vector2(-122f, 0f), new Vector2(110f, 18f), 11, AccentGold, TextAlignmentOptions.Left);
            var bodyText = CreateText(title + "_Body", strip.transform as RectTransform, fonts.Body, body, new Vector2(-6f, 0f), new Vector2(164f, 24f), 9, MutedInk, TextAlignmentOptions.Left);
            bodyText.enableWordWrapping = true;
        }

        private static void CreateControlRow(RectTransform parent, TutorialSprites sprites, TutorialFonts fonts, Vector2 position, string label, string key, string body)
        {
            var strip = CreateImage(label + "Row", parent, sprites.Card, position, new Vector2(280f, 30f), new Color32(0xFF, 0xFF, 0xFF, 0xEA), Image.Type.Sliced);
            CreateText(label + "Label", strip.transform as RectTransform, fonts.Button, label, new Vector2(-121f, 0f), new Vector2(72f, 18f), 11, AccentMint, TextAlignmentOptions.Left);

            var chip = CreateImage(label + "Chip", strip.transform as RectTransform, sprites.Chip, new Vector2(-34f, 0f), new Vector2(58f, 24f), White, Image.Type.Sliced);
            CreateText(label + "Key", chip.transform as RectTransform, fonts.Button, key, Vector2.zero, new Vector2(48f, 18f), 11, AccentDark, TextAlignmentOptions.Center);

            CreateText(label + "Body", strip.transform as RectTransform, fonts.Body, body, new Vector2(74f, 0f), new Vector2(126f, 18f), 9, MutedInk, TextAlignmentOptions.Left);
        }

        private static void CreateTimelineStep(RectTransform parent, TutorialSprites sprites, TutorialFonts fonts, Vector2 position, string number, string title, string body)
        {
            var strip = CreateImage("Step_" + title.Replace(" ", string.Empty), parent, sprites.Card, position, new Vector2(280f, 28f), new Color32(0xFF, 0xFF, 0xFF, 0xEA), Image.Type.Sliced);
            var badge = CreateImage("Badge_" + title.Replace(" ", string.Empty), strip.transform as RectTransform, sprites.Chip, new Vector2(-118f, 0f), new Vector2(28f, 24f), White, Image.Type.Sliced);
            CreateText("BadgeText_" + title.Replace(" ", string.Empty), badge.transform as RectTransform, fonts.Button, number, Vector2.zero, new Vector2(20f, 18f), 11, AccentDark, TextAlignmentOptions.Center);
            CreateText(title + "Title", strip.transform as RectTransform, fonts.Button, title, new Vector2(-74f, 0f), new Vector2(88f, 18f), 11, AccentGold, TextAlignmentOptions.Left);
            CreateText(title + "Body", strip.transform as RectTransform, fonts.Body, body, new Vector2(58f, 0f), new Vector2(124f, 18f), 9, MutedInk, TextAlignmentOptions.Left);
        }

        private static GameObject CreateCanvasRoot(string name, int sortingOrder)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.layer = ResolveUiLayer();

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 480f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 100f;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 480f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return root;
        }

        private static int ResolveUiLayer()
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            return uiLayer >= 0 ? uiLayer : 0;
        }

        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.gameObject.layer = ResolveUiLayer();
            return rect;
        }

        private static Image CreateStretchImage(string name, Transform parent, Sprite sprite, Color color, Image.Type type)
        {
            var rect = CreateStretchRect(name, parent);
            return ConfigureImage(rect, sprite, color, type);
        }

        private static RectTransform CreateTopLeftImage(string name, Transform parent, Sprite sprite, float left, float top, float width, float height, Color color)
        {
            var rect = CreateRect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(left, -top),
                new Vector2(width, height));
            ConfigureImage(rect, sprite, color, Image.Type.Sliced);
            return rect;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            Image.Type type)
        {
            var rect = CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                size);
            return ConfigureImage(rect, sprite, color, type);
        }

        private static Image ConfigureImage(RectTransform rect, Sprite sprite, Color color, Image.Type type)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                size);

            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.margin = Vector4.zero;
            return label;
        }

        private static ButtonParts CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            TMP_FontAsset font,
            float fontSize,
            Color labelColor,
            bool primary)
        {
            var rect = CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                size);

            var plate = rect.gameObject.AddComponent<Image>();
            plate.sprite = sprite;
            plate.type = Image.Type.Sliced;
            plate.color = White;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = primary ? PrimaryButtonColors() : SecondaryButtonColors();

            var labelText = CreateText(
                name + "Label",
                rect,
                font,
                label,
                new Vector2(0f, 1f),
                new Vector2(size.x - 20f, size.y - 8f),
                fontSize,
                labelColor,
                TextAlignmentOptions.Center);

            return new ButtonParts(rect, plate, button, labelText);
        }

        private static ColorBlock PrimaryButtonColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = White;
            colors.highlightedColor = new Color32(0xFF, 0xED, 0xBE, 0xFF);
            colors.pressedColor = new Color32(0xF2, 0xBA, 0x48, 0xFF);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(0x88, 0x88, 0x88, 0x80);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static ColorBlock SecondaryButtonColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = White;
            colors.highlightedColor = new Color32(0xD6, 0xEE, 0xFF, 0xFF);
            colors.pressedColor = new Color32(0x88, 0x9A, 0xB5, 0xFF);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(0x88, 0x88, 0x88, 0x80);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static void AddPrefabInstancesToMainScene(GameObject menuPrefab, GameObject overlayPrefab)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            RemoveExistingSceneObjects<rimrushTutorialMenuPanel>(scene);
            RemoveExistingSceneObjects<rimrushTutorialOverlay>(scene);
            EnsureEventSystem(scene);

            var menuInstance = PrefabUtility.InstantiatePrefab(menuPrefab, scene) as GameObject;
            var overlayInstance = PrefabUtility.InstantiatePrefab(overlayPrefab, scene) as GameObject;

            if (menuInstance != null)
            {
                menuInstance.name = "RimrushTutorialMenuPanel";
                menuInstance.SetActive(true);
                AssignCanvasCamera(menuInstance, scene);
            }

            if (overlayInstance != null)
            {
                overlayInstance.name = "RimrushTutorialOverlay";
                overlayInstance.SetActive(true);
                AssignCanvasCamera(overlayInstance, scene);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveExistingSceneObjects<T>(Scene scene) where T : Component
        {
            var components = Resources.FindObjectsOfTypeAll<T>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].gameObject.scene == scene)
                {
                    Object.DestroyImmediate(components[i].gameObject);
                }
            }
        }

        private static void EnsureEventSystem(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var existing = roots[i].GetComponentInChildren<EventSystem>(true);
                if (existing != null)
                {
                    if (existing.GetComponent<StandaloneInputModule>() == null)
                    {
                        existing.gameObject.AddComponent<StandaloneInputModule>();
                    }
                    return;
                }
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.layer = ResolveUiLayer();
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
        }

        private static void AssignCanvasCamera(GameObject instance, Scene scene)
        {
            if (instance == null)
            {
                return;
            }

            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                return;
            }

            var camera = FindMainCamera(scene);
            if (camera != null)
            {
                canvas.worldCamera = camera;
            }
        }

        private static Camera FindMainCamera(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var cameras = roots[i].GetComponentsInChildren<Camera>(true);
                for (var j = 0; j < cameras.Length; j++)
                {
                    if (cameras[j] != null && cameras[j].CompareTag("MainCamera"))
                    {
                        return cameras[j];
                    }
                }
            }

            for (var i = 0; i < roots.Length; i++)
            {
                var camera = roots[i].GetComponentInChildren<Camera>(true);
                if (camera != null)
                {
                    return camera;
                }
            }

            return null;
        }

        private static void CreateTextureAssets()
        {
            WriteTexture("tutorial_dim", CreateSolidTexture(32, 32, new Color(0.01f, 0.03f, 0.07f, 0.82f)));
            WriteTexture("tutorial_rule", CreateRuleTexture(1024, 16), new Vector4(0f, 0f, 0f, 0f), 2048);
            WriteTexture("tutorial_board", CreateRoundedTexture(1536, 960, 116, 14, new Color32(0x18, 0x22, 0x37, 0xFB), new Color32(0x0B, 0x10, 0x1A, 0xFB), new Color32(0xFF, 0xB7, 0x49, 0xFF), new Color32(0x86, 0xFF, 0xDE, 0x70), new Color32(0x8A, 0xB8, 0xE0, 0x26)), new Vector4(112f, 112f, 112f, 112f), 2048);
            WriteTexture("tutorial_card", CreateRoundedTexture(960, 420, 78, 12, new Color32(0x22, 0x30, 0x48, 0xF5), new Color32(0x10, 0x16, 0x25, 0xF5), new Color32(0x6E, 0x8A, 0xB4, 0x9B), new Color32(0x9D, 0xFF, 0xE1, 0x34), new Color32(0xFF, 0xD4, 0x74, 0x14)), new Vector4(74f, 74f, 74f, 74f), 2048);
            WriteTexture("tutorial_tab", CreateRoundedTexture(640, 220, 72, 12, new Color32(0x2A, 0x3A, 0x58, 0xF8), new Color32(0x16, 0x22, 0x37, 0xF8), new Color32(0xFF, 0xC1, 0x56, 0xAA), new Color32(0xE8, 0xF6, 0xFF, 0x24), new Color32(0x9E, 0xFF, 0xE2, 0x14)), new Vector4(60f, 60f, 60f, 60f), 1024);
            WriteTexture("tutorial_button_primary", CreateRoundedTexture(768, 256, 88, 14, new Color32(0xFF, 0xD9, 0x7B, 0xFF), new Color32(0xF2, 0xA6, 0x3B, 0xFF), new Color32(0xFF, 0xF0, 0xBA, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0x48), new Color32(0xB5, 0x4A, 0x0A, 0x24)), new Vector4(82f, 82f, 82f, 82f), 1024);
            WriteTexture("tutorial_button_secondary", CreateRoundedTexture(768, 248, 84, 14, new Color32(0xC8, 0xD7, 0xE8, 0xFF), new Color32(0x8C, 0xA3, 0xBE, 0xFF), new Color32(0xF4, 0xFA, 0xFF, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0x42), new Color32(0x3A, 0x48, 0x5B, 0x1E)), new Vector4(78f, 78f, 78f, 78f), 1024);
            WriteTexture("tutorial_chip", CreateRoundedTexture(512, 176, 68, 10, new Color32(0xF8, 0xE7, 0xB3, 0xFF), new Color32(0xF4, 0xBB, 0x59, 0xFF), new Color32(0xFF, 0xFA, 0xE0, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0x42), new Color32(0x6D, 0x43, 0x0F, 0x20)), new Vector4(62f, 62f, 62f, 62f), 1024);
            WriteTexture("tutorial_focus_frame", CreateFocusFrameTexture(896, 560, 92, 18, new Color32(0xFF, 0xD5, 0x70, 0xFF), new Color32(0x7E, 0xFF, 0xDF, 0xDE)), new Vector4(88f, 88f, 88f, 88f), 1024);
            WriteTexture("tutorial_glow", CreateGlowTexture(1024, 640, new Color32(0xB8, 0xFF, 0xE6, 0xE0), new Color32(0xFF, 0xB0, 0x55, 0xA6)), new Vector4(140f, 140f, 140f, 140f), 2048);
            WriteTexture("tutorial_orb", CreateOrbTexture(512, new Color32(0xFF, 0xDE, 0x94, 0xF8), new Color32(0x7B, 0xFF, 0xD7, 0xD0), new Color32(0x12, 0x18, 0x28, 0x00)), new Vector4(0f, 0f, 0f, 0f), 1024);
        }

        private static void CreateTmpFontAssets()
        {
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/Impact2.ttf", "Impact2 SDF");
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/AgencyBold.ttf", "AgencyBold SDF");
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/Rajdhani-Bold.ttf", "RajdhaniBold SDF");
        }

        private static void CreateTmpFontAsset(string sourcePath, string assetName)
        {
            var assetPath = $"{TutorialTmpFontRoot}/{assetName}.asset";
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
                Debug.LogWarning($"Cannot build TMP font asset because the source font is missing: {sourcePath}");
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

        private static void WriteTexture(string name, Texture2D texture, Vector4 border, int maxTextureSize)
        {
            var path = $"{TutorialAssetRoot}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxTextureSize;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void WriteTexture(string name, Texture2D texture)
        {
            WriteTexture(name, texture, Vector4.zero, 1024);
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

        private static Texture2D CreateRuleTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var fy = Mathf.Abs(y - (height - 1) * 0.5f) / Mathf.Max(1f, (height - 1) * 0.5f);
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - fy), 2.2f);
                    var shimmer = 0.82f + Mathf.Sin(x * 0.022f) * 0.08f;
                    pixels[y * width + x] = new Color(0.65f, 0.98f, 0.93f, alpha * shimmer * 0.78f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRoundedTexture(
            int width,
            int height,
            int radius,
            int borderWidth,
            Color top,
            Color bottom,
            Color border,
            Color highlight,
            Color warmth)
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
                        pixels[index] = Clear;
                        continue;
                    }

                    var u = x / Mathf.Max(1f, width - 1f);
                    var v = y / Mathf.Max(1f, height - 1f);
                    var baseColor = Color.Lerp(bottom, top, v);
                    var noise = Mathf.Sin(u * 13.4f + v * 4.7f) * 0.5f + Mathf.Sin(u * 27.3f - v * 9.1f) * 0.25f;
                    baseColor = Color.Lerp(baseColor, highlight, (noise * 0.5f + 0.5f) * 0.06f);
                    baseColor = Color.Lerp(baseColor, warmth, Mathf.Clamp01((u * 0.85f + (1f - v) * 0.65f - 0.92f) * 2.8f) * 0.34f);

                    var edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    if (edgeDistance < borderWidth || !InsideRoundedRect(x, y, width, height, radius - borderWidth))
                    {
                        baseColor = Color.Lerp(baseColor, border, 0.82f);
                    }

                    var topSheen = Mathf.Clamp01(1f - Mathf.Abs(v - 0.84f) * 7.5f) * Mathf.Clamp01(1f - Mathf.Abs(u - 0.48f) * 1.8f);
                    baseColor = Color.Lerp(baseColor, new Color(1f, 1f, 1f, baseColor.a), topSheen * 0.08f);

                    var bottomShade = Mathf.Clamp01((0.18f - v) * 3.8f);
                    baseColor = Color.Lerp(baseColor, new Color(0f, 0f, 0f, baseColor.a), bottomShade * 0.18f);
                    pixels[index] = baseColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFocusFrameTexture(int width, int height, int radius, int borderWidth, Color outer, Color inner)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var innerWidth = Mathf.Max(1, width - borderWidth * 2);
            var innerHeight = Mathf.Max(1, height - borderWidth * 2);
            var innerRadius = Mathf.Max(0, radius - borderWidth);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var insideOuter = InsideRoundedRect(x, y, width, height, radius);
                    var insideInner = x >= borderWidth &&
                                      y >= borderWidth &&
                                      x < width - borderWidth &&
                                      y < height - borderWidth &&
                                      InsideRoundedRect(x - borderWidth, y - borderWidth, innerWidth, innerHeight, innerRadius);

                    if (!insideOuter || insideInner)
                    {
                        pixels[index] = Clear;
                        continue;
                    }

                    var edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    var t = Mathf.Clamp01(edgeDistance / Mathf.Max(1f, borderWidth - 1f));
                    var color = Color.Lerp(outer, inner, t);
                    color.a *= 0.94f;
                    pixels[index] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateGlowTexture(int width, int height, Color mint, Color amber)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var center = new Vector2(width * 0.5f, height * 0.52f);
            var ringY = height * 0.56f;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = (x - center.x) / (width * 0.48f);
                    var ny = (y - center.y) / (height * 0.42f);
                    var radial = Mathf.Sqrt(nx * nx + ny * ny);
                    var soft = Mathf.Clamp01(1f - radial);
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(radial - 0.72f) * 3.4f);
                    var sweep = Mathf.Clamp01(1f - Mathf.Abs((y - ringY) / (height * 0.18f)));
                    var color = Color.Lerp(amber, mint, Mathf.Clamp01(0.5f + nx * 0.45f));
                    color.a = soft * soft * 0.16f + ring * 0.22f + sweep * 0.05f;
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateOrbTexture(int size, Color warm, Color mint, Color fade)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var nx = (x - center.x) / (size * 0.5f);
                    var ny = (y - center.y) / (size * 0.5f);
                    var radial = Mathf.Sqrt(nx * nx + ny * ny);
                    if (radial > 1f)
                    {
                        pixels[index] = Clear;
                        continue;
                    }

                    var color = Color.Lerp(warm, mint, Mathf.Clamp01(0.5f + nx * 0.5f - ny * 0.18f));
                    color = Color.Lerp(fade, color, Mathf.Pow(Mathf.Clamp01(1f - radial), 1.6f));
                    color.a = Mathf.Pow(Mathf.Clamp01(1f - radial), 2.2f) * 0.94f;
                    pixels[index] = color;
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

        private static TutorialSprites LoadSprites()
        {
            return new TutorialSprites(
                LoadSprite("tutorial_board"),
                LoadSprite("tutorial_card"),
                LoadSprite("tutorial_tab"),
                LoadSprite("tutorial_button_primary"),
                LoadSprite("tutorial_button_secondary"),
                LoadSprite("tutorial_chip"),
                LoadSprite("tutorial_focus_frame"),
                LoadSprite("tutorial_glow"),
                LoadSprite("tutorial_orb"),
                LoadSprite("tutorial_dim"),
                LoadSprite("tutorial_rule"));
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{TutorialAssetRoot}/{name}.png");
        }

        private static TutorialFonts LoadFonts()
        {
            return new TutorialFonts(
                LoadFont("Impact2 SDF"),
                LoadFont("AgencyBold SDF"),
                LoadFont("RajdhaniBold SDF"));
        }

        private static TMP_FontAsset LoadFont(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{TutorialTmpFontRoot}/{name}.asset");
        }

        private readonly struct TutorialSprites
        {
            public TutorialSprites(
                Sprite board,
                Sprite card,
                Sprite tab,
                Sprite buttonPrimary,
                Sprite buttonSecondary,
                Sprite chip,
                Sprite focusFrame,
                Sprite glow,
                Sprite orb,
                Sprite dim,
                Sprite rule)
            {
                Board = board;
                Card = card;
                Tab = tab;
                ButtonPrimary = buttonPrimary;
                ButtonSecondary = buttonSecondary;
                Chip = chip;
                FocusFrame = focusFrame;
                Glow = glow;
                Orb = orb;
                Dim = dim;
                Rule = rule;
            }

            public Sprite Board { get; }
            public Sprite Card { get; }
            public Sprite Tab { get; }
            public Sprite ButtonPrimary { get; }
            public Sprite ButtonSecondary { get; }
            public Sprite Chip { get; }
            public Sprite FocusFrame { get; }
            public Sprite Glow { get; }
            public Sprite Orb { get; }
            public Sprite Dim { get; }
            public Sprite Rule { get; }
        }

        private readonly struct TutorialFonts
        {
            public TutorialFonts(TMP_FontAsset title, TMP_FontAsset button, TMP_FontAsset body)
            {
                Title = title;
                Button = button;
                Body = body;
            }

            public TMP_FontAsset Title { get; }
            public TMP_FontAsset Button { get; }
            public TMP_FontAsset Body { get; }
        }
    }
}
