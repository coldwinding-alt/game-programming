// 帮助面板预制体生成器 / 在编辑器中自动创建帮助面板的预制体，包括按键说明页面、规则页面、女巫角色演示区域和所有按钮。不需要手动拖拽搭建 UI，运行一次就能生成完整面板。

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace mlp.EditorTools
{
    /// <summary>
    /// 帮助面板预制体生成器：在编辑器中自动创建帮助面板的预制体，包括按键说明页面、规则页面和所有按钮。
    /// </summary>
    public static class mlpHelpPanelPrefabBuilder
    {
        private const string HelpAssetRoot = "Assets/mlp/Resources/mlp/Help";
        private const string HelpTmpFontRoot = "Assets/mlp/Resources/mlp/Fonts/TMP";
        private const string PrefabRoot = "Assets/mlp/Resources/mlp/Prefabs/UI";
        private const string PrefabPath = PrefabRoot + "/MlpHelpPanel.prefab";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("mlp/Build Help Panel Prefab")]
        /// <summary>
        /// 构建帮助面板预制体并在主场景中放置实例。
        /// </summary>
        public static void Build()
        {
            // 1. 确保资源目录存在（不存在则自动创建）
            Directory.CreateDirectory(HelpAssetRoot);
            Directory.CreateDirectory(HelpTmpFontRoot);
            Directory.CreateDirectory(PrefabRoot);
            // 2. 生成面板所需的所有纹理图片（背景、卡片、按键帽等）
            CreateTextureAssets();
            // 3. 为内置字体生成 TextMeshPro 专用字体资源
            CreateTmpFontAssets();
            // 4. 刷新资源数据库，让 Unity 识别刚生成的文件
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // 5. 构建帮助面板的完整预制体（包含所有 UI 元素）
            var prefab = BuildPrefab();
            // 6. 把预制体实例放入主场景中
            AddPrefabInstanceToMainScene(prefab);
            // 7. 保存所有资源改动到磁盘
            AssetDatabase.SaveAssets();
            Debug.Log("Mlp help panel prefab and scene instance built.");
        }

        [MenuItem("mlp/Build Help Panel Prefab Only")]
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
            Debug.Log("Mlp help panel prefab rebuilt.");
        }

        /// <summary>
        /// 创建包含所有精灵、文本和按钮的完整帮助面板预制体层级。
        /// </summary>
        private static GameObject BuildPrefab()
        {
            // 1. 如果已存在旧的预制体文件，先删除再重建
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // 2. 创建根对象和面板组件，以及面板内容的容器
            var root = new GameObject("MlpHelpPanel");
            var panel = root.AddComponent<mlpHelpPanel>();
            var panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(root.transform, false);

            // 3. 加载所有需要的精灵图片（背景、面板、卡片、标签页等）
            var buttons = new List<mlpHelpButton>();
            var dim = LoadSprite("help_dim");
            var board = LoadSprite("help_board");
            var card = LoadSprite("help_card");
            var stage = LoadSprite("help_stage");
            var tab = LoadSprite("help_tab");
            var chip = LoadSprite("help_chip");
            var keycap = LoadSprite("help_keycap");
            var spotlight = LoadSprite("help_spotlight");

            // 4. 添加半透明遮罩背景、主面板底板和标题分割线
            AddSprite("DimBackdrop", dim, 400f, 240f, 0.88f, 800f, 480f, 870, panelRoot.transform);
            AddSprite("MainBoard", board, 400f, 246f, 0.87f, 742f, 430f, 880, panelRoot.transform);
            AddSprite("HeaderRule", LoadSprite("help_line"), 400f, 96f, 0.865f, 690f, 2f, 901, panelRoot.transform);

            // 5. 添加 "HOW TO PLAY" 标题文字
            AddText("Title", "HOW TO PLAY", 65f, 56f, 34, new Color32(0xFF, 0xB7, 0x3C, 0xFF), TextAnchor.MiddleLeft, 930, panelRoot.transform, mlpTextStyle.DisplayTitle);

            // 6. 创建快速测试开关、测试信息按钮和关闭按钮
            var quickTestToggle = CreateTextButton(
                "QuickTestToggle",
                "OFF",
                mlpHelpButtonAction.QuickTestToggle,
                tab,
                604f,
                58f,
                160f,
                34f,
                panelRoot.transform,
                915,
                buttons,
                10);
            var quickTestInfoButton = CreateTextButton(
                "QuickTestInfoButton",
                "?",
                mlpHelpButtonAction.QuickTestInfoToggle,
                tab,
                704f,
                58f,
                26f,
                26f,
                panelRoot.transform,
                916,
                buttons,
                13);
            CreateTextButton(
                "CloseButton",
                "X",
                mlpHelpButtonAction.Close,
                tab,
                738f,
                58f,
                38f,
                34f,
                panelRoot.transform,
                918,
                buttons,
                18);

            // 7. 构建键盘操作说明页面（按键映射 + 女巫演示区域）
            var keyboardPage = new GameObject("KeyboardPage");
            keyboardPage.transform.SetParent(panelRoot.transform, false);

            BuildKeyboardPage(keyboardPage.transform, card, stage, tab, chip, keycap, spotlight, buttons, out var demoRows, out var demoTitle, out var demoDescription, out var demoCoach, out var witchMount, out var witchSpotlight);

            // 8. 创建快速测试信息面板（默认隐藏）
            var quickTestInfoRoot = new GameObject("QuickTestInfoPanel");
            quickTestInfoRoot.transform.SetParent(panelRoot.transform, false);
            AddSprite("QuickTestInfoPanelPlate", card, 596f, 113f, 0.842f, 292f, 72f, 912, quickTestInfoRoot.transform);
            var quickTestInfoText = AddText(
                "QuickTestInfoText",
                "DEV / REVIEW TEST\n15s matches + no skill cooldowns.\nQuickly try the full game flow.",
                466f,
                113f,
                9,
                new Color32(0xF4, 0xF7, 0xFF, 0xFF),
                TextAnchor.MiddleLeft,
                936,
                quickTestInfoRoot.transform,
                mlpTextStyle.TournamentBody);
            quickTestInfoRoot.SetActive(false);

            // 9. 把所有 UI 元素的引用配置到面板组件上
            panel.EditorConfigure(
                panelRoot,
                keyboardPage,
                null,
                buttons.ToArray(),
                null,
                null,
                null,
                null,
                demoRows,
                demoTitle,
                demoDescription,
                demoCoach,
                witchMount,
                witchSpotlight,
                quickTestToggle.Button,
                quickTestToggle.Plate,
                quickTestToggle.Label,
                quickTestInfoButton.Button,
                quickTestInfoRoot,
                quickTestInfoText);

            // 10. 激活页面和面板容器
            keyboardPage.SetActive(true);
            panelRoot.SetActive(true);

            // 11. 将整个对象层级保存为预制体文件，然后销毁临时对象
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// 构建键盘控制页面，包含按键映射、配置栏和女巫演示区域。
        /// </summary>
        private static void BuildKeyboardPage(
            Transform parent,
            Sprite card,
            Sprite stage,
            Sprite tab,
            Sprite chip,
            Sprite keycap,
            Sprite spotlight,
            List<mlpHelpButton> buttons,
            out SpriteRenderer[] demoRows,
            out TMP_Text demoTitle,
            out TMP_Text demoDescription,
            out TMP_Text demoCoach,
            out Transform witchMount,
            out SpriteRenderer witchSpotlight)
        {
            // 1. 添加页面标题 "KEYBOARD MAP"
            AddText("KeyboardHeader", "KEYBOARD MAP", 64f, 119f, 17, new Color32(0xF2, 0xF7, 0xFF, 0xFF), TextAnchor.MiddleLeft, 930, parent, mlpTextStyle.TournamentAccent);

            // 2. 添加五行动作的按键说明行：移动、跳跃、攻击、下蹲、必杀
            AddControlRow(parent, card, keycap, 65f, 166f, "MOVE", "A / D", "Move left / right.\nDouble-tap to dash.");
            AddControlRow(parent, card, keycap, 65f, 202f, "JUMP", "W", "Jump.\nAir shots and contests.");
            AddControlRow(parent, card, keycap, 65f, 238f, "ACTION", "B", "With ball: Shoot.\nNo ball: Steal.");
            AddControlRow(parent, card, keycap, 65f, 274f, "DOWN", "S", "With ball: Pump fake.\nDefense: Block.");
            AddControlRow(parent, card, keycap, 65f, 310f, "SUPER", "N / V", "Use at full energy.");

            // 3. 添加 1P/2P 双人快捷按键配置栏
            AddProfileStrip(parent, card, keycap);

            // 4. 在右半部分添加演示舞台背景和聚光灯效果
            AddSprite("PreviewStage", stage, 574f, 206f, 0.86f, 326f, 178f, 895, parent);
            witchSpotlight = AddSprite("WitchSpotlight", spotlight, 552f, 248f, 0.855f, 190f, 62f, 899, parent);
            // 5. 添加 "DRILL PREVIEW" 标题和 "REPLAY TUTORIAL" 按钮
            AddText("PreviewHeader", "DRILL PREVIEW", 430f, 121f, 16, new Color32(0xFF, 0xD2, 0x75, 0xFF), TextAnchor.MiddleLeft, 930, parent, mlpTextStyle.TournamentAccent);
            CreateTextButton(
                "KeyboardReplayTutorialButton",
                "REPLAY TUTORIAL",
                mlpHelpButtonAction.ReplayTutorial,
                tab,
                658f,
                121f,
                144f,
                30f,
                parent,
                915,
                buttons,
                9);

            // 6. 创建女巫角色的挂载点（用于放置角色演示动画）
            witchMount = new GameObject("WitchMount").transform;
            witchMount.SetParent(parent, false);
            ApplyPixelTransform(witchMount, 552f, 240f, 0.84f, 1f, 1f);

            // 7. 创建七个演示按钮（移动、跳跃、投篮、假动作、冲刺、抢断、盖帽）
            demoRows = new SpriteRenderer[7];
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Move, mlpHelpButtonAction.DemoMove, "MOVE", 702f, 154f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Jump, mlpHelpButtonAction.DemoJump, "JUMP", 702f, 181f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Shoot, mlpHelpButtonAction.DemoShoot, "SHOT", 702f, 208f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Pump, mlpHelpButtonAction.DemoPump, "PUMP", 702f, 235f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Dash, mlpHelpButtonAction.DemoDash, "DASH", 702f, 262f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Steal, mlpHelpButtonAction.DemoSteal, "STEAL", 702f, 289f);
            CreateDemoButton(parent, chip, buttons, demoRows, mlpHelpDemo.Block, mlpHelpButtonAction.DemoBlock, "BLOCK", 702f, 316f);

            // 8. 添加演示说明文字（动作名称、描述和技巧提示）
            demoTitle = AddText("DemoTitle", "DOWN: BLOCK", 430f, 314f, 14, new Color32(0xD8, 0xFF, 0x89, 0xFF), TextAnchor.MiddleLeft, 933, parent, mlpTextStyle.TournamentAccent);
            demoDescription = AddText("DemoDescription", "Hold S to block.\nJump into the shot path.", 430f, 338f, 10, new Color32(0xF4, 0xF7, 0xFF, 0xFF), TextAnchor.MiddleLeft, 933, parent, mlpTextStyle.TournamentBody);
            demoCoach = AddText("DemoCoach", "Tip: time the jump.", 590f, 328f, 9, new Color32(0x9F, 0xFF, 0xD3, 0xFF), TextAnchor.MiddleLeft, 933, parent, mlpTextStyle.TournamentBody);
        }

        /// <summary>
        /// 添加一行显示动作标签、按键帽和说明文字。
        /// </summary>
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
            AddText(label + "Label", label, x + 12f, y + 1f, 12, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, mlpTextStyle.TournamentAccent);
            AddKey(parent, keycap, keyText, x + 108f, y, keyText.Length > 5 ? 76f : 52f);
            AddText(label + "Desc", description, x + 168f, y + 1f, 10, new Color32(0xE5, 0xEE, 0xFA, 0xFF), TextAnchor.MiddleLeft, 926, parent, mlpTextStyle.TournamentBody);
        }

        /// <summary>
        /// 添加带有居中按键标签文字的可视化按键帽精灵。
        /// </summary>
        private static void AddKey(Transform parent, Sprite keycap, string keyText, float x, float y, float width)
        {
            AddSprite("Key_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keycap, x, y, 0.852f, width, 24f, 906, parent);
            AddText("KeyText_" + keyText.Replace(" ", string.Empty).Replace("/", string.Empty), keyText, x, y + 1f, 10, new Color32(0x20, 0x27, 0x32, 0xFF), TextAnchor.MiddleCenter, 930, parent, mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 添加不带按键帽背景的方向键名称纯文本标签。
        /// </summary>
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
                mlpTextStyle.TournamentAccent);

            text.rectTransform.sizeDelta = new Vector2(width, 5f);
            text.characterSpacing = keyText.Length > 5 ? 1.5f : 3f;
        }

        /// <summary>
        /// 添加显示 1P 和 2P 按键布局的双人快速配置栏。
        /// </summary>
        private static void AddProfileStrip(Transform parent, Sprite card, Sprite keycap)
        {
            AddSprite("ProfileStrip", card, 400f, 400f, 0.858f, 676f, 108f, 892, parent);
            AddText("ProfileStripTitle", "QUICK PROFILE", 76f, 351f, 15, new Color32(0xFF, 0xCF, 0x76, 0xFF), TextAnchor.MiddleLeft, 926, parent, mlpTextStyle.TournamentAccent);
            AddText("ProfileStripLeftLabel", "1P", 78f, 376f, 12, new Color32(0xB6, 0xFF, 0xDC, 0xFF), TextAnchor.MiddleLeft, 926, parent, mlpTextStyle.TournamentAccent);
            AddText("ProfileStripRightLabel", "2P", 416f, 376f, 12, new Color32(0xB6, 0xFF, 0xDC, 0xFF), TextAnchor.MiddleLeft, 926, parent, mlpTextStyle.TournamentAccent);

            AddProfileMoveCluster(parent, keycap, 172f, 405f, "W", "A", "S", "D");
            AddProfileActionRow(parent, keycap, 266f, 390f, "N / V", "SUPER");
            AddProfileActionRow(parent, keycap, 266f, 420f, "B", "ACTION");

            AddProfileMoveCluster(parent, keycap, 510f, 405f, "^", "<", "v", ">");
            AddProfileActionRow(parent, keycap, 604f, 390f, "K", "SUPER");
            AddProfileActionRow(parent, keycap, 604f, 420f, "L", "ACTION");
        }

        /// <summary>
        /// 以十字布局排列四个移动按键帽，用于快速配置显示。
        /// </summary>
        private static void AddProfileMoveCluster(Transform parent, Sprite keycap, float centerX, float centerY, string topKey, string leftKey, string bottomKey, string rightKey)
        {
            AddProfileKey(parent, keycap, topKey, centerX, centerY - 17f, 32f, 32f);
            AddProfileKey(parent, keycap, leftKey, centerX - 34f, centerY + 17f, 32f, 32f);
            AddProfileKey(parent, keycap, bottomKey, centerX, centerY + 17f, 32f, 32f);
            AddProfileKey(parent, keycap, rightKey, centerX + 34f, centerY + 17f, 32f, 32f);
        }

        /// <summary>
        /// 在配置栏中并排添加按键帽和动作标签。
        /// </summary>
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
                mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 添加带有居中按键标签的较大配置风格按键帽。
        /// </summary>
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
                mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 创建触发女巫练习演示动画的小型标签按钮。
        /// </summary>
        private static void CreateDemoButton(
            Transform parent,
            Sprite chip,
            List<mlpHelpButton> buttons,
            SpriteRenderer[] demoRows,
            mlpHelpDemo demo,
            mlpHelpButtonAction action,
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
            /// 保存已创建文本按钮的底板渲染器、标签文本和按钮组件。
            /// </summary>
            public TextButtonParts(SpriteRenderer plate, TMP_Text label, mlpHelpButton button)
            {
                Plate = plate;
                Label = label;
                Button = button;
            }

            public readonly SpriteRenderer Plate;
            public readonly TMP_Text Label;
            public readonly mlpHelpButton Button;
        }

        /// <summary>
        /// 创建带有底板精灵、文本标签和悬停颜色配置的可点击按钮。
        /// </summary>
        private static TextButtonParts CreateTextButton(
            string name,
            string label,
            mlpHelpButtonAction action,
            Sprite sprite,
            float x,
            float y,
            float width,
            float height,
            Transform parent,
            int sortingOrder,
            List<mlpHelpButton> buttons,
            int fontSize = 11)
        {
            // 1. 创建按钮的根游戏对象，挂到父节点下
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            // 2. 添加按钮底板精灵（背景图片）
            var plate = AddSprite(name + "Plate", sprite, x, y, 0.84f, width, height, sortingOrder, root.transform);
            // 3. 添加按钮上的文字标签
            var text = AddText(name + "Label", label, x, y + 1f, fontSize, new Color32(0xE9, 0xF3, 0xFF, 0xFF), TextAnchor.MiddleCenter, sortingOrder + 20, root.transform, mlpTextStyle.TournamentAccent);
            // 4. 添加按钮交互组件，并配置点击行为、悬停颜色等参数
            var button = root.AddComponent<mlpHelpButton>();
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
            // 5. 把按钮记录到列表中，最后统一交给面板组件管理
            buttons.Add(button);
            return new TextButtonParts(plate, text, button);
        }

        /// <summary>
        /// 在指定像素位置和尺寸添加 SpriteRenderer GameObject。
        /// </summary>
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
        /// 在指定像素位置添加 TextMeshPro 文本元素。
        /// </summary>
        private static TMP_Text AddText(string name, string text, float x, float y, int fontSize, Color color, TextAnchor anchor, int sortingOrder, Transform parent, mlpTextStyle style)
        {
            return mlpRender.TmpText(name, text, x, y, fontSize, color, anchor, sortingOrder, parent, style);
        }

        /// <summary>
        /// 使用游戏的像素到世界坐标转换来定位和缩放 Transform。
        /// </summary>
        private static void ApplyPixelTransform(Transform transform, float x, float y, float z, float scaleX, float scaleY)
        {
            transform.position = mlpConstants.PixelToWorldSnapped(x, y, z);
            transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * scaleX,
                mlpConstants.UnitsPerPixel * scaleY,
                1f);
        }

        /// <summary>
        /// 根据名称从帮助资源文件夹加载精灵。
        /// </summary>
        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{HelpAssetRoot}/{name}.png");
        }

        /// <summary>
        /// 生成并保存所有 UI 纹理资源（面板、卡片、按键帽等）。
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
        /// 为所有内置字体生成 TextMeshPro SDF 字体资源。
        /// </summary>
        private static void CreateTmpFontAssets()
        {
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/Impact.ttf", "Impact SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/CfCrackBold.ttf", "CfCrackBold SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/AgencyBold.ttf", "AgencyBold SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/Impact2.ttf", "Impact2 SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/Rajdhani-Bold.ttf", "Rajdhani-Bold SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/Rajdhani-SemiBold.ttf", "Rajdhani-SemiBold SDF");
            CreateTmpFontAsset("Assets/mlp/Resources/mlp/Fonts/Griffy-Regular.ttf", "Griffy-Regular SDF");
        }

        /// <summary>
        /// 从源 TTF 文件创建单个 TMP SDF 字体资源。
        /// </summary>
        private static void CreateTmpFontAsset(string sourcePath, string assetName)
        {
            // 1. 如果已存在同名字体资源，先删除旧的
            var assetPath = $"{HelpTmpFontRoot}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            // 2. 加载源 TTF 字体文件，找不到则跳过
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"Cannot build help TMP font asset because the source font is missing: {sourcePath}");
                return;
            }

            // 3. 用 TextMeshPro 的接口从 TTF 创建 SDF 字体资源（带抗锯齿的有符号距离场格式）
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                144,       // 采样大小（分辨率越高细节越多）
                12,        // 字体内边距
                GlyphRenderMode.SDFAA,  // SDF + 抗锯齿渲染模式
                2048,      // 图集宽度
                2048,      // 图集高度
                AtlasPopulationMode.Dynamic,  // 动态模式（按需生成字符）
                true);     // 启用多色字体支持
            if (fontAsset == null)
            {
                Debug.LogWarning($"TMP font asset creation failed for {sourcePath}");
                return;
            }

            // 4. 设置字体资源、图集纹理和材质的名称
            fontAsset.name = assetName;
            var atlasTexture = fontAsset.atlasTexture;
            if (atlasTexture != null)
            {
                atlasTexture.name = assetName + " Atlas";
            }

            // 5. 配置材质参数：绑定纹理、设置锐度和透视过滤
            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                fontAsset.material.mainTexture = atlasTexture;
                fontAsset.material.SetFloat("_Sharpness", 0.18f);
                fontAsset.material.SetFloat("_PerspectiveFilter", 0f);
            }

            // 6. 将字体资源、图集和材质保存到磁盘
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            // 7. 标记资源为已修改并强制重新导入
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// 创建指定尺寸的纯色纹理。
        /// </summary>
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
        /// 创建带渐变填充、边框和可选闪光图案的圆角纹理。
        /// </summary>
        private static Texture2D CreateRoundedTexture(int width, int height, int radius, int borderWidth, Color top, Color bottom, Color border, Color glint, bool pattern)
        {
            // 1. 创建空白纹理和像素数组
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            // 2. 遍历每个像素，逐个计算颜色
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    // 3. 如果像素在圆角矩形外，设为透明
                    if (!InsideRoundedRect(x, y, width, height, radius))
                    {
                        pixels[index] = Color.clear;
                        continue;
                    }

                    // 4. 根据垂直位置在顶部和底部颜色之间做渐变
                    var vertical = height <= 1 ? 0f : y / (float)(height - 1);
                    var color = Color.Lerp(bottom, top, vertical);
                    // 5. 如果开启了图案模式，按数学公式生成闪光点装饰
                    if (pattern && ((x + y * 3) % 97 < 2 || (x * 2 + y) % 131 < 2))
                    {
                        color = Color.Lerp(color, glint, 0.45f);
                    }

                    // 6. 判断像素是否在边框区域，是则混合边框颜色
                    var edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    if (edgeDistance < borderWidth + 0.5f || !InsideRoundedRect(x, y, width, height, radius - borderWidth))
                    {
                        color = Color.Lerp(color, border, 0.72f);
                    }
                    // 7. 顶部圆角附近添加淡淡的高光效果
                    else if (y > height - radius - 12 && x > radius && x < width - radius)
                    {
                        color = Color.Lerp(color, glint, 0.24f);
                    }

                    pixels[index] = color;
                }
            }

            // 8. 把像素数据写入纹理并应用
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 创建带有柔和绿色光晕的径向聚光灯纹理。
        /// </summary>
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
        /// 检查像素坐标是否在圆角矩形内。
        /// </summary>
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
        /// 将生成的纹理保存为 PNG 并配置其精灵导入设置。
        /// </summary>
        private static void WriteTexture(string name, Texture2D texture)
        {
            // 1. 将纹理编码为 PNG 格式并写入文件
            var path = $"{HelpAssetRoot}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            // 2. 释放临时纹理占用的内存
            Object.DestroyImmediate(texture);
            // 3. 让 Unity 识别新生成的图片文件
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            // 4. 配置图片导入格式：精灵模式、不压缩、不开 mipmap、双线性过滤
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1f;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // 5. 保存设置并重新导入，使配置生效
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 将帮助面板预制体实例放入主场景，替换已有的实例。
        /// </summary>
        private static void AddPrefabInstanceToMainScene(GameObject prefab)
        {
            // 1. 如果预制体为空（构建失败），直接报错返回
            if (prefab == null)
            {
                Debug.LogError("Cannot add help panel instance because prefab build failed.");
                return;
            }

            // 2. 打开主场景
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            // 3. 找到场景中已有的帮助面板实例并删除（避免重复）
            var panels = Resources.FindObjectsOfTypeAll<mlpHelpPanel>();
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                if (panel != null && panel.gameObject.scene == scene)
                {
                    Object.DestroyImmediate(panel.gameObject);
                }
            }

            // 4. 在场景中创建预制体的新实例并放到原点位置
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance != null)
            {
                instance.name = "MlpHelpPanel";
                instance.transform.position = Vector3.zero;
            }

            // 5. 标记场景已修改并保存到磁盘
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
