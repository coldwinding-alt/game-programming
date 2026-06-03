// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushHelpPanelPrefabBuilder 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

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
        /// <summary>
        /// Executes Build for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
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

        [MenuItem("rimrush/Build Help Panel Prefab Only")]
        public static void BuildPrefabAssetOnly()
        {
            Directory.CreateDirectory(HelpAssetRoot);
            Directory.CreateDirectory(HelpTmpFontRoot);
            Directory.CreateDirectory(PrefabRoot);
            CreateTextureAssets();
            CreateTmpFontAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("Rimrush help panel prefab rebuilt.");
        }

        /// <summary>
        /// Executes Build Prefab for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject BuildPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

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

            BuildKeyboardPage(keyboardPage.transform, card, stage, tab, chip, keycap, spotlight, buttons, out var demoRows, out var demoTitle, out var demoDescription, out var demoCoach, out var witchMount, out var witchSpotlight);
            BuildRulesPage(rulesPage.transform, stage, tab, buttons);

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

        /// <summary>
        /// Executes Build Keyboard Page for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="card">Input value used by this step of the workflow.</param>
        /// <param name="stage">Input value used by this step of the workflow.</param>
        /// <param name="chip">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="spotlight">Input value used by this step of the workflow.</param>
        /// <param name="buttons">Input value used by this step of the workflow.</param>
        /// <param name="demoRows">Input value used by this step of the workflow.</param>
        /// <param name="demoTitle">Input value used by this step of the workflow.</param>
        /// <param name="demoDescription">Input value used by this step of the workflow.</param>
        /// <param name="demoCoach">Input value used by this step of the workflow.</param>
        /// <param name="witchMount">Input value used by this step of the workflow.</param>
        /// <param name="witchSpotlight">Input value used by this step of the workflow.</param>
        private static void BuildKeyboardPage(
            Transform parent,
            Sprite card,
            Sprite stage,
            Sprite tab,
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

            AddControlRow(parent, card, keycap, 65f, 166f, "MOVE", "A / D", "Move left / right.\nDouble-tap to dash.");
            AddControlRow(parent, card, keycap, 65f, 202f, "JUMP", "W", "Jump.\nAir shots and contests.");
            AddControlRow(parent, card, keycap, 65f, 238f, "ACTION", "B", "With ball: Shoot.\nNo ball: Steal.");
            AddControlRow(parent, card, keycap, 65f, 274f, "DOWN", "S", "With ball: Pump fake.\nDefense: Block.");
            AddControlRow(parent, card, keycap, 65f, 310f, "SUPER", "N / V", "Use at full energy.");

            AddProfileStrip(parent, card, keycap);

            AddSprite("PreviewStage", stage, 574f, 206f, 0.86f, 326f, 178f, 895, parent);
            witchSpotlight = AddSprite("WitchSpotlight", spotlight, 552f, 248f, 0.855f, 190f, 62f, 899, parent);
            AddText("PreviewHeader", "DRILL PREVIEW", 430f, 121f, 16, new Color32(0xFF, 0xD2, 0x75, 0xFF), TextAnchor.MiddleLeft, 930, parent, rimrushTextStyle.TournamentAccent);
            CreateTextButton(
                "KeyboardReplayTutorialButton",
                "REPLAY TUTORIAL",
                rimrushHelpButtonAction.ReplayTutorial,
                tab,
                658f,
                121f,
                144f,
                30f,
                parent,
                915,
                buttons,
                9);

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

            demoTitle = AddText("DemoTitle", "DOWN: BLOCK", 430f, 314f, 14, new Color32(0xD8, 0xFF, 0x89, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentAccent);
            demoDescription = AddText("DemoDescription", "Hold S to block.\nJump into the shot path.", 430f, 338f, 10, new Color32(0xF4, 0xF7, 0xFF, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentBody);
            demoCoach = AddText("DemoCoach", "Tip: time the jump.", 590f, 328f, 9, new Color32(0x9F, 0xFF, 0xD3, 0xFF), TextAnchor.MiddleLeft, 933, parent, rimrushTextStyle.TournamentBody);
        }

        /// <summary>
        /// Executes Add Control Row for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="card">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="label">Input value used by this step of the workflow.</param>
        /// <param name="p1Key">Input value used by this step of the workflow.</param>
        /// <param name="description">Input value used by this step of the workflow.</param>
        private static void AddControlRow(
            Transform parent,
            Sprite card,
            Sprite keycap,
            float x,
            float y,
            string label,
            string keyText,
            string description)
        {
            AddSprite(label + "Card", card, x + 165f, y, 0.858f, 330f, 33f, 892, parent);
            AddText(label + "Label", label, x + 12f, y + 1f, 12, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);
            AddKey(parent, keycap, keyText, x + 108f, y, keyText.Length > 5 ? 76f : 52f);
            AddText(label + "Desc", description, x + 168f, y + 1f, 10, new Color32(0xE5, 0xEE, 0xFA, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentBody);
        }

        /// <summary>
        /// Executes Add Key for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="keyText">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        private static void AddKey(Transform parent, Sprite keycap, string keyText, float x, float y, float width)
        {
            AddSprite("Key_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keycap, x, y, 0.852f, width, 24f, 906, parent);
            AddText("KeyText_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keyText, x, y + 1f, 10, new Color32(0x20, 0x27, 0x32, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Executes Add Direction Label for the rimrushHelpPanelPrefabBuilder workflow.
        /// This keeps arrow-key words readable without making them look like physical keycaps.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="keyText">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        private static void AddDirectionLabel(Transform parent, string keyText, float x, float y, float width)
        {
            var sanitized = keyText.Replace(" ", string.Empty).Replace("/", string.Empty);
            var text = AddText(
                "DirectionText_" + sanitized,
                keyText,
                x,
                y + 1f,
                keyText.Length > 5 ? 9 : 10,
                new Color32(0xB8, 0xFF, 0xE2, 0xFF),
                TextAnchor.MiddleCenter,
                930,
                parent,
                rimrushTextStyle.TournamentAccent);

            text.rectTransform.sizeDelta = new Vector2(width, 5f);
            text.characterSpacing = keyText.Length > 5 ? 1.5f : 3f;
        }

        /// <summary>
        /// Executes Add Profile Strip for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="card">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        private static void AddProfileStrip(Transform parent, Sprite card, Sprite keycap)
        {
            AddSprite("ProfileStrip", card, 400f, 400f, 0.858f, 676f, 108f, 892, parent);
            AddText("ProfileStripTitle", "QUICK PROFILE", 76f, 351f, 15, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);
            AddText("ProfileStripLeftLabel", "1P", 78f, 376f, 12, new Color32(0xB6, 0xFF, 0xDC, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);
            AddText("ProfileStripRightLabel", "2P", 416f, 376f, 12, new Color32(0xB6, 0xFF, 0xDC, 0xFF), TextAnchor.MiddleLeft, 926, parent, rimrushTextStyle.TournamentAccent);

            AddProfileMoveCluster(parent, keycap, 172f, 405f, "W", "A", "S", "D");
            AddProfileActionRow(parent, keycap, 266f, 390f, "N / V", "SUPER");
            AddProfileActionRow(parent, keycap, 266f, 420f, "B", "ACTION");

            AddProfileMoveCluster(parent, keycap, 510f, 405f, "^", "<", "v", ">");
            AddProfileActionRow(parent, keycap, 604f, 390f, "K", "SUPER");
            AddProfileActionRow(parent, keycap, 604f, 420f, "L", "ACTION");
        }

        /// <summary>
        /// Executes Add Profile Move Cluster for the rimrushHelpPanelPrefabBuilder workflow.
        /// This lays out the movement keys in a familiar directional shape for quick scanning.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="centerX">Input value used by this step of the workflow.</param>
        /// <param name="centerY">Input value used by this step of the workflow.</param>
        /// <param name="topKey">Input value used by this step of the workflow.</param>
        /// <param name="leftKey">Input value used by this step of the workflow.</param>
        /// <param name="bottomKey">Input value used by this step of the workflow.</param>
        /// <param name="rightKey">Input value used by this step of the workflow.</param>
        private static void AddProfileMoveCluster(Transform parent, Sprite keycap, float centerX, float centerY, string topKey, string leftKey, string bottomKey, string rightKey)
        {
            AddProfileKey(parent, keycap, topKey, centerX, centerY - 17f, 32f, 32f);
            AddProfileKey(parent, keycap, leftKey, centerX - 34f, centerY + 17f, 32f, 32f);
            AddProfileKey(parent, keycap, bottomKey, centerX, centerY + 17f, 32f, 32f);
            AddProfileKey(parent, keycap, rightKey, centerX + 34f, centerY + 17f, 32f, 32f);
        }

        /// <summary>
        /// Executes Add Profile Action Row for the rimrushHelpPanelPrefabBuilder workflow.
        /// This pairs a tactile keycap with its action label.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="keyText">Input value used by this step of the workflow.</param>
        /// <param name="label">Input value used by this step of the workflow.</param>
        private static void AddProfileActionRow(Transform parent, Sprite keycap, float x, float y, string keyText, string label)
        {
            var keyWidth = keyText.Length > 1 ? 52f : 32f;
            AddProfileKey(parent, keycap, keyText, x, y, keyWidth, 32f);
            AddText(
                "ProfileAction_" + keyText,
                label,
                x + (keyWidth * 0.5f) + 18f,
                y + 1f,
                12,
                new Color32(0xF4, 0xF7, 0xFF, 0xFF),
                TextAnchor.MiddleLeft,
                928,
                parent,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Executes Add Profile Key for the rimrushHelpPanelPrefabBuilder workflow.
        /// This builds the larger bottom-panel keycaps used by the two-player quick profile.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="keycap">Input value used by this step of the workflow.</param>
        /// <param name="keyText">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        private static void AddProfileKey(Transform parent, Sprite keycap, string keyText, float x, float y, float width, float height)
        {
            var sanitized = keyText
                .Replace(" ", string.Empty)
                .Replace("/", string.Empty)
                .Replace("<", "Left")
                .Replace(">", "Right")
                .Replace("^", "Up");
            sanitized = sanitized == "v" ? "Down" : sanitized;

            AddSprite("ProfileKey_" + sanitized, keycap, x, y, 0.852f, width, height, 906, parent);
            AddText(
                "ProfileKeyText_" + sanitized,
                keyText,
                x,
                y + 1f,
                11,
                new Color32(0x20, 0x27, 0x32, 0xFF),
                TextAnchor.MiddleCenter,
                930,
                parent,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Executes Build Rules Page for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="stage">Input value used by this step of the workflow.</param>
        private static void BuildRulesPage(Transform parent, Sprite stage, Sprite tab, List<rimrushHelpButton> buttons)
        {
            AddSprite("RulesEmptyStage", stage, 400f, 254f, 0.86f, 560f, 242f, 895, parent);
            AddText("RulesTitle", "QUICK START", 400f, 182f, 26, new Color32(0xFF, 0xB9, 0x48, 0xFF), TextAnchor.MiddleCenter, 930, parent, rimrushTextStyle.DisplayTitle);
            CreateTextButton(
                "RulesReplayTutorialButton",
                "REPLAY TUTORIAL",
                rimrushHelpButtonAction.ReplayTutorial,
                tab,
                400f,
                346f,
                220f,
                38f,
                parent,
                910,
                buttons,
                12);
        }

        /// <summary>
        /// Executes Create Demo Button for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="chip">Input value used by this step of the workflow.</param>
        /// <param name="buttons">Input value used by this step of the workflow.</param>
        /// <param name="demoRows">Input value used by this step of the workflow.</param>
        /// <param name="demo">Input value used by this step of the workflow.</param>
        /// <param name="action">Input value used by this step of the workflow.</param>
        /// <param name="label">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
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
            /// <summary>
            /// Executes Text Button Parts for the rimrushHelpPanelPrefabBuilder workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="plate">Input value used by this step of the workflow.</param>
            /// <param name="label">Input value used by this step of the workflow.</param>
            public TextButtonParts(SpriteRenderer plate, TMP_Text label)
            {
                Plate = plate;
                Label = label;
            }

            public readonly SpriteRenderer Plate;
            public readonly TMP_Text Label;
        }

        /// <summary>
        /// Executes Create Text Button for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="label">Input value used by this step of the workflow.</param>
        /// <param name="action">Input value used by this step of the workflow.</param>
        /// <param name="sprite">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="buttons">Input value used by this step of the workflow.</param>
        /// <param name="fontSize">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Add Sprite for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="sprite">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="z">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Add Text for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="text">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="fontSize">Input value used by this step of the workflow.</param>
        /// <param name="color">Input value used by this step of the workflow.</param>
        /// <param name="anchor">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="style">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static TMP_Text AddText(string name, string text, float x, float y, int fontSize, Color color, TextAnchor anchor, int sortingOrder, Transform parent, rimrushTextStyle style)
        {
            return rimrushRender.TmpText(name, text, x, y, fontSize, color, anchor, sortingOrder, parent, style);
        }

        /// <summary>
        /// Executes Apply Pixel Transform for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="transform">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="z">Input value used by this step of the workflow.</param>
        /// <param name="scaleX">Input value used by this step of the workflow.</param>
        /// <param name="scaleY">Input value used by this step of the workflow.</param>
        private static void ApplyPixelTransform(Transform transform, float x, float y, float z, float scaleX, float scaleY)
        {
            transform.position = rimrushConstants.PixelToWorldSnapped(x, y, z);
            transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * scaleX,
                rimrushConstants.UnitsPerPixel * scaleY,
                1f);
        }

        /// <summary>
        /// Executes Load Sprite for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{HelpAssetRoot}/{name}.png");
        }

        /// <summary>
        /// Executes Create Texture Assets for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
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

        /// <summary>
        /// Executes Create Tmp Font Assets for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private static void CreateTmpFontAssets()
        {
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/AgencyBold.ttf", "AgencyBold SDF");
            CreateTmpFontAsset("Assets/rimrush/Resources/rimrush/Fonts/Impact2.ttf", "Impact2 SDF");
        }

        /// <summary>
        /// Executes Create Tmp Font Asset for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="sourcePath">Input value used by this step of the workflow.</param>
        /// <param name="assetName">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Create Solid Texture for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="color">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Create Rounded Texture for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="radius">Input value used by this step of the workflow.</param>
        /// <param name="borderWidth">Input value used by this step of the workflow.</param>
        /// <param name="top">Input value used by this step of the workflow.</param>
        /// <param name="bottom">Input value used by this step of the workflow.</param>
        /// <param name="border">Input value used by this step of the workflow.</param>
        /// <param name="glint">Input value used by this step of the workflow.</param>
        /// <param name="pattern">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Create Spotlight Texture for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Inside Rounded Rect for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="radius">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

        /// <summary>
        /// Executes Write Texture for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="texture">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Add Prefab Instance To Main Scene for the rimrushHelpPanelPrefabBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="prefab">Input value used by this step of the workflow.</param>
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
