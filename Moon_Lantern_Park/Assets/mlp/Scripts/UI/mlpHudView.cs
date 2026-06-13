// 比赛中的 HUD 界面（抬头显示）
// 包括比分板、计时器、暂停菜单、赛后结算画面、倒计时和各种弹出提示。

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>暂停命令类型：无操作、切换暂停、恢复比赛、返回菜单。</summary>
    public enum mlpPauseCommand
    {
        None,
        Toggle,
        Resume,
        Menu
    }

    /// <summary>
    /// 比赛 HUD 界面：管理比赛中所有抬头显示元素——比分板、计时器、暂停菜单、赛后结算、倒计时和各种弹出提示。
    /// </summary>
    public sealed class mlpHudView
    {
        private const float ScreenCenterY = 240f;
        private const float ScoreboardCenterX = mlpConstants.Width2;
        private const float ScoreboardCenterY = 88f;
        private const float ScoreboardTargetWidth = 360f;
        private const float PortraitTargetPixels = 42f;
        private const float PortraitOffsetX = 109f;
        private const float PortraitBaseY = 74f;
        private const float NameOffsetX = 146f;
        private const float NameY = 66f;
        private const float ScoreOffsetX = 30f;
        private const float ScoreY = 68f;
        private const float TimerY = 110f;
        private const int PortraitSortingOrder = 83;
        private const float CountdownY = 172f;
        private const float PauseTitleY = 100f;
        private const float PauseBoardY = 214f;
        private const float PauseNameY = 292f;
        private const float PauseMetaY = 320f;
        private const float PauseActionY = 372f;
        private const float PausePortraitOffsetX = 170f;
        private const float PausePortraitOffsetY = -22f;
        private const float PausePortraitPixels = 84f;
        private const float PauseScoreOffsetX = 47f;
        private const float PauseScoreY = PauseBoardY - 34f;
        private const float PauseMenuButtonX = 304f;
        private const float PauseResumeButtonX = 496f;
        private const float PauseMenuButtonWidth = 156f;
        private const float PauseResumeButtonWidth = 188f;
        private const float PauseActionButtonHeight = 40f;
        private const float PopupCenterY = 236f;
        private const float PopupBackdropWidth = 432f;
        private const float PostMatchCardCenterY = 224f;
        private const float PostMatchCardWidth = 456f;
        private const float PostMatchCardHeight = 236f;
        private const float PostMatchInnerWidth = 414f;
        private const float PostMatchInnerHeight = 192f;
        private const float PostMatchScorePlateWidth = 228f;
        private const float PostMatchScorePlateHeight = 70f;
        private const float PostMatchTitleY = 164f;
        private const float PostMatchSubtitleY = 138f;
        private const float PostMatchScoreY = 218f;
        private const float PostMatchPortraitY = 220f;
        private const float PostMatchPortraitOffsetX = 144f;
        private const float PostMatchPortraitPixels = 46f;
        private const float PostMatchWinnerTagY = 248f;
        private const float PostMatchNameY = 274f;
        private const float PostMatchPromptY = 316f;
        private const float MessageExitWindow = 0.18f;
        private const float CountdownPulseDuration = 0.42f;
        private const float BonusNoticeX = 676f;
        private const float BonusNoticeY = 142f;
        private const float TopRightButtonY = 44f;
        private const float PauseButtonX = 770f;
        private const float MusicButtonX = 706f;
        private const float HelpButtonX = 642f;
        private const float TopRightButtonSize = 60f;
        private const float TopRightIconPixels = 58f;
        private readonly GameObject scoreboardRoot;
        private readonly TextMesh leftScore;
        private readonly TextMesh rightScore;
        private readonly TextMesh leftNameText;
        private readonly TextMesh rightNameText;
        private readonly TextMesh timerText;
        private readonly mlpMenuButton pauseButton;
        private readonly GameObject pauseButtonIcon;
        private readonly mlpIconButton musicButton;
        private readonly mlpIconButton helpButton;
        private readonly GameObject pauseOverlayRoot;
        private readonly GameObject pauseShade;
        private readonly GameObject pausePanel;
        private readonly TextMesh pauseTitleText;
        private readonly TextMesh pauseScoreText;
        private readonly TextMesh pauseLeftNameText;
        private readonly TextMesh pauseRightNameText;
        private readonly TextMesh pauseLeftScoreText;
        private readonly TextMesh pauseRightScoreText;
        private readonly TextMesh pauseScoreDividerText;
        private readonly GameObject pauseLeftPortrait;
        private readonly GameObject pauseRightPortrait;
        private readonly mlpMenuButton pauseMenuButton;
        private readonly mlpMenuButton pauseResumeButton;
        private readonly bool isTraining;
        private readonly GameObject messageRoot;
        private readonly GameObject messageBackdrop;
        private readonly TextMesh messageText;
        private readonly GameObject bonusNoticeRoot;
        private readonly TextMesh bonusNoticeText;
        private readonly TextMesh countdownCaptionText;
        private readonly TextMesh countdownText;
        private readonly Vector3 countdownBaseScale;
        private readonly string leftCharacterLabel;
        private readonly string rightCharacterLabel;
        private readonly bool isTutorial;
        private readonly GameObject postMatchOverlayRoot;
        private readonly GameObject postMatchTopGlow;
        private readonly GameObject postMatchBottomGlow;
        private readonly GameObject postMatchCardRoot;
        private readonly GameObject postMatchCardPanel;
        private readonly GameObject postMatchCardFrame;
        private readonly GameObject postMatchScorePlate;
        private readonly GameObject postMatchLeftAura;
        private readonly GameObject postMatchRightAura;
        private readonly GameObject postMatchLeftPortrait;
        private readonly GameObject postMatchRightPortrait;
        private readonly GameObject postMatchPromptFrame;
        private readonly TextMesh postMatchTitleText;
        private readonly TextMesh postMatchSubtitleText;
        private readonly TextMesh postMatchScoreText;
        private readonly TextMesh postMatchLeftNameText;
        private readonly TextMesh postMatchRightNameText;
        private readonly TextMesh postMatchWinnerTagText;
        private readonly TextMesh postMatchPromptText;
        private readonly Vector3 postMatchLeftAuraBaseScale;
        private readonly Vector3 postMatchRightAuraBaseScale;
        private readonly Vector3 postMatchLeftPortraitBaseScale;
        private readonly Vector3 postMatchRightPortraitBaseScale;
        private float messageTime;
        private float messageDuration;
        private float messageVisualScale = 1f;
        private float bonusNoticeTime;
        private float bonusNoticeDuration;
        private float countdownTime = -1f;
        private float countdownPulseTime;
        private mlpPauseCommand pendingPauseCommand;
        private int lastCountdownTick = int.MinValue;
        private float postMatchAnimTime;
        private int postMatchWinnerSide;
        public bool IsPostMatchVisible => postMatchOverlayRoot != null && postMatchOverlayRoot.activeSelf;
        public bool IsPauseOverlayVisible { get; private set; }

        /// <summary>
        /// 构建整个 HUD 界面：比分板、暂停覆盖层、赛后结果卡片、倒计时和弹出消息。
        /// </summary>
        public mlpHudView(Transform parent, mlpMatchData matchData)
        {
            // 1. 判断当前游戏模式（教程、训练、普通比赛）和角色名称
            var gameMode = mlpInventory.Instance.GameMode;
            isTutorial = gameMode == mlpGameModeIds.Tutorial;
            isTraining = gameMode == mlpGameModeIds.Training || gameMode == mlpGameModeIds.Tutorial;
            leftCharacterLabel = mlpPlayersData.GetCharacterName(matchData.CharacterIds[0]);
            rightCharacterLabel = mlpPlayersData.GetCharacterName(matchData.CharacterIds[1]);

            // 2. 创建比分板（背景图 + 左右角色头像 + 名字 + 分数 + 计时器）
            scoreboardRoot = CreateHudRoot("ScoreboardRoot", parent);
            CreateScoreboardBackdrop(scoreboardRoot.transform);
            CreatePortraitAura("LeftPortraitAura", ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, 0.34f, 80, scoreboardRoot.transform);
            CreatePortraitAura("RightPortraitAura", ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, 0.34f, 80, scoreboardRoot.transform);
            CreateCharacterPortrait("LeftPortrait", matchData.CharacterIds[0], ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, scoreboardRoot.transform);
            CreateCharacterPortrait("RightPortrait", matchData.CharacterIds[1], ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, scoreboardRoot.transform);

            leftNameText = mlpRender.Text(
                "LeftName",
                leftCharacterLabel,
                ScoreboardCenterX - NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleRight,
                85,
                scoreboardRoot.transform,
                mlpTextStyle.HudName);
            rightNameText = mlpRender.Text(
                "RightName",
                rightCharacterLabel,
                ScoreboardCenterX + NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleLeft,
                85,
                scoreboardRoot.transform,
                mlpTextStyle.HudName);

            var scoreColor = new Color32(0xFF, 0xA7, 0x22, 0xFF);
            leftScore = mlpRender.Text(
                "LeftScore",
                "0",
                ScoreboardCenterX - ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                scoreboardRoot.transform,
                mlpTextStyle.HudScore);
            rightScore = mlpRender.Text(
                "RightScore",
                "0",
                ScoreboardCenterX + ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                scoreboardRoot.transform,
                mlpTextStyle.HudScore);
            timerText = mlpRender.Text(
                "Timer",
                "1:00",
                ScoreboardCenterX,
                TimerY,
                18,
                new Color32(0xC6, 0xFF, 0x33, 0xFF),
                TextAnchor.MiddleCenter,
                87,
                scoreboardRoot.transform,
                mlpTextStyle.HudTimer);
            SetScoreboardVisible(true);

            // 3. 创建右上角功能按钮（暂停、音乐开关、帮助）
            pauseButton = new mlpMenuButton(string.Empty, PauseButtonX, TopRightButtonY, TopRightButtonSize, TopRightButtonSize, () => pendingPauseCommand = mlpPauseCommand.Toggle, parent);
            pauseButton.SetBackgroundVisible(false);
            pauseButton.SetLabelVisible(false);
            pauseButtonIcon = CreatePauseButtonIcon(parent);
            musicButton = new mlpIconButton(
                "HudMusicButton",
                MusicButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                ToggleBackgroundMusic,
                parent,
                82,
                TopRightIconPixels,
                mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOn),
                mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOff));
            musicButton.SetActiveIconIndex(GetMusicIconIndex());
            helpButton = new mlpIconButton(
                "HudHelpButton",
                HelpButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                mlpHelpPanel.ShowKeyboardPage,
                parent,
                82,
                TopRightIconPixels,
                mlpAssets.Images.ResourcePath(mlpAssets.Images.HelpButton));

            // 4. 创建倒计时显示（数字脉冲动画 + 标题文字，如 "RESUMING IN"）
            countdownCaptionText = mlpRender.Text(
                "CountdownCaption",
                string.Empty,
                mlpConstants.Width2,
                CountdownY - 28f,
                16,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                mlpTextStyle.TournamentAccent);
            countdownText = mlpRender.Text(
                "Countdown",
                string.Empty,
                mlpConstants.Width2,
                CountdownY,
                58,
                new Color32(0xFF, 0xB8, 0x2E, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                mlpTextStyle.HudPopup);
            countdownBaseScale = countdownText.transform.localScale;

            // 5. 创建屏幕中央的弹出消息（如 "GO!!!"、"BASKET"、"3 POINT"）
            messageRoot = CreateHudAnchor("MessageRoot", mlpConstants.Width2, PopupCenterY, parent);
            messageBackdrop = CreatePopupBackdrop(parent);
            if (messageBackdrop != null)
            {
                messageBackdrop.transform.SetParent(messageRoot.transform, true);
            }

            messageText = mlpRender.Text(
                "Message",
                string.Empty,
                mlpConstants.Width2,
                PopupCenterY + 2f,
                56,
                new Color32(0x8B, 0x2D, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                mlpTextStyle.HudPopup);
            messageText.transform.SetParent(messageRoot.transform, true);
            messageRoot.SetActive(false);

            // 6. 创建右上角加分提示（如 "HELL DASH!"）
            bonusNoticeRoot = CreateHudAnchor("BonusNoticeRoot", BonusNoticeX, BonusNoticeY, parent);
            bonusNoticeText = mlpRender.Text(
                "BonusNotice",
                string.Empty,
                BonusNoticeX,
                BonusNoticeY,
                16,
                new Color32(0xFF, 0x7A, 0x39, 0xFF),
                TextAnchor.MiddleCenter,
                119,
                parent,
                mlpTextStyle.TournamentAccent);
            bonusNoticeText.transform.SetParent(bonusNoticeRoot.transform, true);
            bonusNoticeRoot.SetActive(false);

            // 7. 创建赛后结果卡片（暗色遮罩 + 卡片面板 + 角色头像 + 比分 + 胜者标签 + 提示文字）
            postMatchOverlayRoot = CreateHudRoot("PostMatchOverlayRoot", parent);
            CreatePausePanel("PostMatchShade", mlpConstants.Width2, ScreenCenterY, 800f, 480f, 128, postMatchOverlayRoot.transform, new Color(0.02f, 0.04f, 0.06f, 0.68f));
            postMatchTopGlow = CreatePausePanel("PostMatchTopGlow", mlpConstants.Width2, 120f, 760f, 92f, 129, postMatchOverlayRoot.transform, new Color(0.34f, 0.86f, 0.92f, 0.08f));
            postMatchBottomGlow = CreatePausePanel("PostMatchBottomGlow", mlpConstants.Width2, 358f, 700f, 116f, 129, postMatchOverlayRoot.transform, new Color(1f, 0.72f, 0.34f, 0.06f));
            postMatchCardRoot = CreateHudAnchor("PostMatchCardRoot", mlpConstants.Width2, PostMatchCardCenterY, postMatchOverlayRoot.transform);
            postMatchCardPanel = CreatePausePanel(
                "PostMatchCardPanel",
                mlpConstants.Width2,
                PostMatchCardCenterY,
                PostMatchCardWidth,
                PostMatchCardHeight,
                130,
                postMatchCardRoot.transform,
                new Color(0.03f, 0.06f, 0.1f, 0.94f));
            postMatchCardFrame = CreatePauseFrame(
                "PostMatchCardFrame",
                "MatchBack0002",
                mlpConstants.Width2,
                PostMatchCardCenterY,
                PostMatchCardWidth + 34f,
                PostMatchCardHeight + 16f,
                131,
                postMatchCardRoot.transform,
                new Color(0.86f, 0.96f, 1f, 0.18f));
            CreatePausePanel(
                "PostMatchInnerPanel",
                mlpConstants.Width2,
                PostMatchCardCenterY,
                PostMatchInnerWidth,
                PostMatchInnerHeight,
                132,
                postMatchCardRoot.transform,
                new Color(0.08f, 0.11f, 0.16f, 0.84f));
            CreatePausePanel(
                "PostMatchTopAccent",
                mlpConstants.Width2,
                PostMatchSubtitleY - 14f,
                212f,
                2f,
                133,
                postMatchCardRoot.transform,
                new Color(0.35f, 0.88f, 0.93f, 0.28f));
            CreatePausePanel(
                "PostMatchBottomAccent",
                mlpConstants.Width2,
                PostMatchPromptY - 18f,
                244f,
                2f,
                133,
                postMatchCardRoot.transform,
                new Color(1f, 0.73f, 0.36f, 0.22f));
            postMatchScorePlate = CreatePausePanel(
                "PostMatchScorePlate",
                mlpConstants.Width2,
                PostMatchScoreY + 2f,
                PostMatchScorePlateWidth,
                PostMatchScorePlateHeight,
                133,
                postMatchCardRoot.transform,
                new Color(0.02f, 0.04f, 0.08f, 0.82f));
            CreatePauseFrame(
                "PostMatchScoreFrame",
                "btn_bg0000",
                mlpConstants.Width2,
                PostMatchScoreY + 2f,
                PostMatchScorePlateWidth + 24f,
                PostMatchScorePlateHeight + 14f,
                134,
                postMatchCardRoot.transform,
                new Color(0.3f, 0.82f, 0.9f, 0.26f));

            postMatchLeftAura = CreatePortraitAura(
                "PostMatchLeftAura",
                mlpConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                0.36f,
                132,
                postMatchCardRoot.transform);
            postMatchRightAura = CreatePortraitAura(
                "PostMatchRightAura",
                mlpConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                0.36f,
                132,
                postMatchCardRoot.transform);
            postMatchLeftPortrait = CreateCharacterPortrait(
                "PostMatchLeftPortrait",
                matchData.CharacterIds[0],
                mlpConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                PostMatchPortraitPixels,
                135,
                postMatchCardRoot.transform);
            postMatchRightPortrait = CreateCharacterPortrait(
                "PostMatchRightPortrait",
                matchData.CharacterIds[1],
                mlpConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                PostMatchPortraitPixels,
                135,
                postMatchCardRoot.transform);

            postMatchSubtitleText = mlpRender.Text(
                "PostMatchSubtitle",
                string.Empty,
                mlpConstants.Width2,
                PostMatchSubtitleY,
                13,
                new Color32(0xCB, 0xD9, 0xE4, 0xFF),
                TextAnchor.MiddleCenter,
                135,
                postMatchCardRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f),
                outlinePixels: 0.52f);
            postMatchTitleText = mlpRender.Text(
                "PostMatchTitle",
                string.Empty,
                mlpConstants.Width2,
                PostMatchTitleY,
                30,
                new Color32(0xFF, 0xC7, 0x56, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                mlpTextStyle.DisplayTitle);
            postMatchScoreText = mlpRender.Text(
                "PostMatchScore",
                string.Empty,
                mlpConstants.Width2,
                PostMatchScoreY,
                42,
                new Color32(0xFF, 0xF3, 0xD8, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                mlpTextStyle.HudScore);
            postMatchWinnerTagText = mlpRender.Text(
                "PostMatchWinnerTag",
                string.Empty,
                mlpConstants.Width2,
                PostMatchWinnerTagY,
                12,
                new Color32(0xFF, 0xDE, 0x98, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.02f, 0.05f, 0.08f, 0.92f),
                outlinePixels: 0.56f);
            postMatchLeftNameText = mlpRender.Text(
                "PostMatchLeftName",
                leftCharacterLabel,
                mlpConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchNameY,
                ResolvePostMatchNameFontSize(leftCharacterLabel),
                Color.white,
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                mlpTextStyle.HudName);
            postMatchRightNameText = mlpRender.Text(
                "PostMatchRightName",
                rightCharacterLabel,
                mlpConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchNameY,
                ResolvePostMatchNameFontSize(rightCharacterLabel),
                Color.white,
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                mlpTextStyle.HudName);
            CreatePausePanel(
                "PostMatchPromptTint",
                mlpConstants.Width2,
                PostMatchPromptY,
                216f,
                20f,
                133,
                postMatchCardRoot.transform,
                new Color(0.03f, 0.06f, 0.1f, 0.62f));
            postMatchPromptFrame = CreatePauseFrame(
                "PostMatchPromptFrame",
                "btn_bg0000",
                mlpConstants.Width2,
                PostMatchPromptY,
                248f,
                34f,
                134,
                postMatchCardRoot.transform,
                new Color(0.3f, 0.82f, 0.9f, 0.34f));
            postMatchPromptText = mlpRender.Text(
                "PostMatchPrompt",
                string.Empty,
                mlpConstants.Width2,
                PostMatchPromptY,
                15,
                new Color32(0xEE, 0xF5, 0xD5, 0xFF),
                TextAnchor.MiddleCenter,
                135,
                postMatchCardRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.52f);

            postMatchLeftAuraBaseScale = postMatchLeftAura != null ? postMatchLeftAura.transform.localScale : Vector3.one;
            postMatchRightAuraBaseScale = postMatchRightAura != null ? postMatchRightAura.transform.localScale : Vector3.one;
            postMatchLeftPortraitBaseScale = postMatchLeftPortrait != null ? postMatchLeftPortrait.transform.localScale : Vector3.one;
            postMatchRightPortraitBaseScale = postMatchRightPortrait != null ? postMatchRightPortrait.transform.localScale : Vector3.one;

            // 8. 创建暂停画面（半透明遮罩 + 面板 + 角色头像 + 比分 + 菜单/恢复按钮）
            pauseOverlayRoot = CreateHudRoot("PauseOverlayRoot", parent);
            pauseShade = CreatePausePanel("PauseShade", mlpConstants.Width2, ScreenCenterY, 800f, 480f, 140, pauseOverlayRoot.transform, new Color(0.01f, 0.03f, 0.05f, 0.78f));
            CreatePausePanel("PauseTopGlow", mlpConstants.Width2, 96f, 760f, 104f, 141, pauseOverlayRoot.transform, new Color(0.22f, 0.86f, 0.94f, 0.12f));
            CreatePausePanel("PauseBottomGlow", mlpConstants.Width2, 388f, 760f, 132f, 141, pauseOverlayRoot.transform, new Color(0.56f, 0.22f, 0.94f, 0.1f));
            pausePanel = CreatePausePanel("PausePanel", mlpConstants.Width2, ScreenCenterY, 582f, 308f, 142, pauseOverlayRoot.transform, new Color(0.05f, 0.08f, 0.12f, 0.9f));
            CreatePauseFrame("PauseFrame", "MatchBack0002", mlpConstants.Width2, ScreenCenterY, 632f, 332f, 143, pauseOverlayRoot.transform, new Color(0.9f, 0.98f, 1f, 0.96f));
            CreatePausePanel("PauseBoardTint", mlpConstants.Width2, PauseBoardY, 206f, 72f, 144, pauseOverlayRoot.transform, new Color(0.02f, 0.04f, 0.09f, 0.4f));
            var pauseBoard = CreateHudImage("PauseBoard", mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Scoreboard), mlpConstants.Width2, PauseBoardY, 560f, 145, pauseOverlayRoot.transform);
            if (pauseBoard == null)
            {
                CreatePauseFrame("PauseBoardFallback", "btn_bg0000", mlpConstants.Width2, PauseBoardY, 456f, 150f, 145, pauseOverlayRoot.transform, new Color(0.22f, 0.84f, 0.95f, 0.94f));
            }

            CreatePortraitAura("PauseLeftPortraitAura", mlpConstants.Width2 - PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, 0.66f, 144, pauseOverlayRoot.transform);
            CreatePortraitAura("PauseRightPortraitAura", mlpConstants.Width2 + PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, 0.66f, 144, pauseOverlayRoot.transform);
            pauseLeftPortrait = CreateCharacterPortrait("PauseLeftPortrait", matchData.CharacterIds[0], mlpConstants.Width2 - PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseRightPortrait = CreateCharacterPortrait("PauseRightPortrait", matchData.CharacterIds[1], mlpConstants.Width2 + PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseTitleText = mlpRender.Text(
                "PauseTitle",
                "GAME PAUSED",
                mlpConstants.Width2,
                PauseTitleY,
                28,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpTextStyle.DisplayTitle);
            pauseLeftNameText = mlpRender.Text(
                "PauseLeftName",
                leftCharacterLabel,
                mlpConstants.Width2 - PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseRightNameText = mlpRender.Text(
                "PauseRightName",
                rightCharacterLabel,
                mlpConstants.Width2 + PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseLeftScoreText = mlpRender.Text(
                "PauseLeftScore",
                "0",
                mlpConstants.Width2 - PauseScoreOffsetX,
                PauseScoreY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreDividerText = mlpRender.Text(
                "PauseScoreDivider",
                ":",
                mlpConstants.Width2,
                PauseScoreY - 1f,
                32,
                new Color32(0x8F, 0xFF, 0xF8, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpFontKind.CfCrackBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.95f),
                outlinePixels: 1f);
            pauseRightScoreText = mlpRender.Text(
                "PauseRightScore",
                "0",
                mlpConstants.Width2 + PauseScoreOffsetX,
                PauseScoreY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                mlpFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreText = mlpRender.Text(
                "PauseMeta",
                string.Empty,
                mlpConstants.Width2,
                PauseMetaY,
                15,
                new Color32(0xCC, 0xF6, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                pauseOverlayRoot.transform,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f),
                outlinePixels: 0.62f);
            pauseMenuButton = new mlpMenuButton("MENU", PauseMenuButtonX, PauseActionY, PauseMenuButtonWidth, PauseActionButtonHeight, () => pendingPauseCommand = mlpPauseCommand.Menu, pauseOverlayRoot.transform, 147);
            pauseResumeButton = new mlpMenuButton("RESUME", PauseResumeButtonX, PauseActionY, PauseResumeButtonWidth, PauseActionButtonHeight, () => pendingPauseCommand = mlpPauseCommand.Resume, pauseOverlayRoot.transform, 147);

            SetPauseOverlayVisible(false);
            if (isTraining)
            {
                SetText(pauseScoreText, "FREE PLAY / NO TIMER");
                SetGameObjectVisible(pauseLeftScoreText.gameObject, false);
                SetGameObjectVisible(pauseRightScoreText.gameObject, false);
                SetGameObjectVisible(pauseScoreDividerText.gameObject, false);
            }

            HideMessage();
            HideCountdown();
            HidePostMatch();
            UpdateScore(matchData.MatchScore[0], matchData.MatchScore[1]);
        }

        /// <summary>
        /// 更新比分板和暂停画面上的比分数字。
        /// </summary>
        public void UpdateScore(int left, int right)
        {
            SetText(leftScore, left.ToString());
            SetText(rightScore, right.ToString());
            SetText(pauseLeftScoreText, left.ToString());
            SetText(pauseRightScoreText, right.ToString());
        }

        /// <summary>
        /// 更新计时器显示，并在暂停画面上显示冻结时间。
        /// </summary>
        public void UpdateTimer(float secondsLeft)
        {
            // 1. 将剩余秒数格式化为 "1:00" 或 "04.2" 的显示文本
            var timeText = FormatTime(secondsLeft);
            // 2. 更新比分板上的计时器文字
            SetText(timerText, timeText);
            // 3. 如果不是训练模式，同步更新暂停画面上的冻结时间显示
            if (!isTraining)
            {
                SetText(pauseScoreText, $"TIME FROZEN / {timeText}");
            }
        }

        /// <summary>
        /// 显示或隐藏比赛计时器。
        /// </summary>
        public void SetTimerVisible(bool visible)
        {
            timerText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 读取并清除待处理的暂停指令（切换、恢复或返回菜单）。
        /// </summary>
        public mlpPauseCommand ConsumePauseCommand()
        {
            var command = pendingPauseCommand;
            pendingPauseCommand = mlpPauseCommand.None;
            return command;
        }

        /// <summary>
        /// 切换暂停画面的显示或隐藏。
        /// </summary>
        public void TogglePauseOverlay()
        {
            if (IsPauseOverlayVisible)
            {
                HidePauseOverlay();
            }
            else
            {
                ShowPauseOverlay();
            }
        }

        /// <summary>
        /// 显示暂停画面并隐藏其他 HUD 元素。
        /// </summary>
        public void ShowPauseOverlay()
        {
            // 1. 隐藏屏幕上的弹出消息、加分提示和倒计时（暂停时不需要这些）
            HideMessage();
            HideBonusNotice();
            HideCountdown();
            // 2. 标记暂停状态并显示暂停画面（遮罩 + 面板 + 按钮）
            IsPauseOverlayVisible = true;
            SetPauseOverlayVisible(true);
            // 3. 隐藏右上角的暂停、音乐和帮助按钮（暂停画面有自己的按钮）
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
        }

        /// <summary>
        /// 隐藏暂停画面并恢复右上角按钮。
        /// </summary>
        public void HidePauseOverlay()
        {
            // 1. 清除暂停状态并隐藏暂停画面
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            // 2. 重新显示右上角的暂停、音乐和帮助按钮
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// 隐藏暂停画面并开始"即将恢复"倒计时。
        /// </summary>
        public void BeginResumeCountdown(float duration)
        {
            // 1. 清除暂停状态并隐藏暂停画面
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            // 2. 隐藏所有右上角按钮（倒计时期间不允许操作）
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
            // 3. 启动 3-2-1 倒计时，标题显示 "RESUMING IN"
            StartCountdown(duration, "RESUMING IN");
        }

        /// <summary>
        /// 恢复倒计时结束后重新显示右上角按钮。
        /// </summary>
        public void EndResumeCountdown()
        {
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// 在屏幕中央显示大型弹出消息（例如"GO!!!"、"BASKET"）。
        /// </summary>
        public void ShowMessage(string message, float duration = 1.2f, bool showBackdrop = true)
        {
            // 1. 如果消息根节点不存在（UI 未构建），只设置文字和计时器
            if (messageRoot == null)
            {
                SetText(messageText, message);
                messageTime = duration;
                messageDuration = Mathf.Max(0.01f, duration);
                return;
            }

            // 2. 根据消息内容选择合适的文字颜色（如 "GO!!!" 为绿色，"BASKET" 为金色）
            ApplyMessageTheme(message);
            // 3. 设置消息文字和持续时间
            SetText(messageText, message);
            messageDuration = Mathf.Max(0.01f, duration);
            messageTime = messageDuration;
            // 4. 对较长的文字适当缩小，防止超出屏幕
            messageVisualScale = ResolveMessageScale(message);
            // 5. 设置背景是否显示，定位到屏幕中央，以较小的初始缩放显示（后续帧会播放弹入动画）
            SetGameObjectVisible(messageBackdrop, showBackdrop);
            messageRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PopupCenterY);
            messageRoot.transform.localScale = Vector3.one * (0.78f * messageVisualScale);
            messageRoot.SetActive(true);
        }

        /// <summary>
        /// 在右上角显示小型加分提示（例如"HELL DASH!"）。
        /// </summary>
        public void ShowBonusNotice(string message, float duration = 0.9f)
        {
            // 1. 如果加分提示的根节点不存在，直接返回
            if (bonusNoticeRoot == null)
            {
                return;
            }

            // 2. 根据消息内容选择合适的文字颜色
            ApplyBonusNoticeTheme(message);
            // 3. 设置文字内容和显示持续时间
            SetText(bonusNoticeText, message);
            bonusNoticeDuration = Mathf.Max(0.01f, duration);
            bonusNoticeTime = bonusNoticeDuration;
            // 4. 定位到右上角指定位置，以较小的初始缩放显示（后续帧会播放弹入动画）
            bonusNoticeRoot.transform.position = mlpConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
            bonusNoticeRoot.transform.localScale = Vector3.one * 0.72f;
            bonusNoticeRoot.SetActive(true);
        }

        /// <summary>
        /// 隐藏弹出消息。
        /// </summary>
        public void HideMessage()
        {
            SetText(messageText, string.Empty);
            messageTime = 0f;
            messageDuration = 0f;
            messageVisualScale = 1f;
            if (messageRoot != null)
            {
                messageRoot.transform.localScale = Vector3.one;
                messageRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PopupCenterY);
                messageRoot.SetActive(false);
            }

            SetGameObjectVisible(messageBackdrop, true);
        }

        /// <summary>
        /// 隐藏加分提示。
        /// </summary>
        public void HideBonusNotice()
        {
            SetText(bonusNoticeText, string.Empty);
            bonusNoticeTime = 0f;
            bonusNoticeDuration = 0f;
            if (bonusNoticeRoot != null)
            {
                bonusNoticeRoot.transform.localScale = Vector3.one;
                bonusNoticeRoot.transform.position = mlpConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
                bonusNoticeRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 隐藏倒计时显示并重置其状态。
        /// </summary>
        public void HideCountdown()
        {
            SetText(countdownText, string.Empty);
            SetText(countdownCaptionText, string.Empty);
            countdownTime = -1f;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            countdownText.transform.localScale = countdownBaseScale;
            SetGameObjectVisible(countdownCaptionText.gameObject, false);
        }

        /// <summary>
        /// 启动无标题文字的倒计时。
        /// </summary>
        public void StartCountdown(float duration)
        {
            StartCountdown(duration, string.Empty);
        }

        /// <summary>
        /// 启动倒计时，数字上方可显示可选的标题文字。
        /// </summary>
        public void StartCountdown(float duration, string caption)
        {
            countdownTime = duration;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            SetText(countdownText, string.Empty);
            SetText(countdownCaptionText, caption ?? string.Empty);
            countdownText.color = new Color32(0xFF, 0xB8, 0x2E, 0xFF);
            countdownText.transform.localScale = countdownBaseScale * 0.82f;
            SetGameObjectVisible(countdownCaptionText.gameObject, !string.IsNullOrEmpty(caption));
            HideMessage();
            HideBonusNotice();
            HidePostMatch();
        }

        /// <summary>
        /// 每帧更新倒计时。倒计时仍在运行时返回 true。
        /// </summary>
        public bool UpdateCountdown(float dt)
        {
            if (countdownTime < 0f)
            {
                return false;
            }

            countdownTime -= dt;
            var tick = Mathf.CeilToInt(countdownTime);
            if (tick != lastCountdownTick)
            {
                lastCountdownTick = tick;
                countdownPulseTime = CountdownPulseDuration;
                if (tick > 0)
                {
                    countdownText.color = new Color32(0xFF, 0xB8, 0x2E, 0xFF);
                    SetText(countdownText, tick.ToString());
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.MCountdown, 0.8f);
                }
                else
                {
                    countdownText.color = new Color32(0x9C, 0xFF, 0x4A, 0xFF);
                    SetText(countdownText, "GO!!!");
                }
            }

            if (countdownTime <= -0.45f)
            {
                HideCountdown();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 主更新循环：处理按钮、消息、加分提示、倒计时和赛后动画的每帧更新。
        /// </summary>
        public void Update(float dt)
        {
            // 1. 如果帮助面板打开，暂停 HUD 的所有更新
            if (mlpHelpPanel.IsAnyOpen)
            {
                return;
            }

            // 2. 如果暂停画面可见，只更新暂停画面上的按钮和音乐图标
            if (IsPauseOverlayVisible)
            {
                musicButton?.SetActiveIconIndex(GetMusicIconIndex());
                pauseMenuButton.Update(Camera.main);
                pauseResumeButton.Update(Camera.main);
                return;
            }

            // 3. 更新右上角的暂停、音乐和帮助按钮的鼠标悬停/点击检测
            pauseButton.Update(Camera.main);
            musicButton?.SetActiveIconIndex(GetMusicIconIndex());
            musicButton?.Update(Camera.main);
            helpButton?.Update(Camera.main);

            if (messageTime > 0f)
            {
                messageTime = Mathf.Max(0f, messageTime - dt);
                UpdateMessageVisual();
                if (messageTime <= 0f)
                {
                    HideMessage();
                }
            }
            else if (messageRoot != null && messageRoot.activeSelf)
            {
                HideMessage();
            }

            if (bonusNoticeTime > 0f)
            {
                bonusNoticeTime = Mathf.Max(0f, bonusNoticeTime - dt);
                UpdateBonusNoticeVisual();
                if (bonusNoticeTime <= 0f)
                {
                    HideBonusNotice();
                }
            }
            else if (bonusNoticeRoot != null && bonusNoticeRoot.activeSelf)
            {
                HideBonusNotice();
            }

            if (countdownPulseTime > 0f)
            {
                countdownPulseTime = Mathf.Max(0f, countdownPulseTime - dt);
            }

            if (!string.IsNullOrEmpty(countdownText.text))
            {
                UpdateCountdownVisual();
            }
            else
            {
                countdownText.transform.localScale = countdownBaseScale;
            }

            if (IsPostMatchVisible)
            {
                UpdatePostMatchVisual(dt);
            }

            if (countdownTime < 0f && !string.IsNullOrEmpty(countdownText.text))
            {
                HideCountdown();
            }
        }

        /// <summary>
        /// 显示赛后结果卡片，包含胜者、比分和角色头像。
        /// </summary>
        public void ShowPostMatch(int winner, int leftScoreValue, int rightScoreValue)
        {
            // 1. 获取游戏存档信息，判断当前是哪种游戏模式（锦标赛、冒险、快速匹配等）
            var inventory = mlpInventory.Instance;
            var isPlayerFacingMode = inventory.IsTournamentActive ||
                                     inventory.IsAdventureActive ||
                                     inventory.GameMode == mlpGameModeIds.RandomQuick ||
                                     inventory.GameMode == mlpGameModeIds.QuickMatch ||
                                     inventory.GameMode == mlpGameModeIds.Tutorial;
            var titleUsesWarmAccent = !isPlayerFacingMode || winner < 0;
            var winnerName = winner < 0 ? leftCharacterLabel : rightCharacterLabel;
            postMatchWinnerSide = winner < 0 ? -1 : 1;
            if (isPlayerFacingMode)
            {
                SetText(postMatchTitleText, postMatchWinnerSide == -1 ? "VICTORY" : "DEFEAT");
            }
            else
            {
                SetText(postMatchTitleText, postMatchWinnerSide == -1 ? "PLAYER 1 WINS" : "PLAYER 2 WINS");
            }

            // 2. 设置标题文字颜色（胜利用暖色/金色，失败用冷色/银色）
            postMatchTitleText.color = titleUsesWarmAccent
                ? new Color32(0xFF, 0xC7, 0x56, 0xFF)
                : new Color32(0xDB, 0xE4, 0xF1, 0xFF);
            postMatchSubtitleText.color = new Color32(0xCB, 0xD9, 0xE4, 0xFF);
            postMatchScoreText.color = new Color32(0xFF, 0xF3, 0xD8, 0xFF);
            SetText(postMatchSubtitleText, GetPostMatchSubtitle(inventory, postMatchWinnerSide));
            SetText(postMatchWinnerTagText, string.Empty);
            postMatchLeftNameText.color = postMatchWinnerSide == -1
                ? new Color32(0xFF, 0xDE, 0x99, 0xFF)
                : new Color32(0xD8, 0xE0, 0xEC, 0xFF);
            postMatchRightNameText.color = postMatchWinnerSide == 1
                ? new Color32(0xFF, 0xDE, 0x99, 0xFF)
                : new Color32(0xD8, 0xE0, 0xEC, 0xFF);
            SetSpriteTint(postMatchCardPanel, new Color(0.03f, 0.06f, 0.1f, 0.94f));
            SetSpriteTint(
                postMatchCardFrame,
                titleUsesWarmAccent
                    ? new Color(1f, 0.82f, 0.55f, 0.2f)
                    : new Color(0.86f, 0.96f, 1f, 0.18f));
            SetSpriteTint(postMatchScorePlate, new Color(0.02f, 0.04f, 0.08f, 0.82f));
            SetSpriteTint(
                postMatchLeftAura,
                postMatchWinnerSide == -1
                    ? new Color(1f, 0.79f, 0.37f, 0.6f)
                    : new Color(0.52f, 0.71f, 0.8f, 0.18f));
            SetSpriteTint(
                postMatchRightAura,
                postMatchWinnerSide == 1
                    ? new Color(1f, 0.79f, 0.37f, 0.6f)
                    : new Color(0.52f, 0.71f, 0.8f, 0.18f));
            SetSpriteTint(postMatchLeftPortrait, postMatchWinnerSide == -1 ? Color.white : new Color(0.73f, 0.77f, 0.84f, 0.62f));
            SetSpriteTint(postMatchRightPortrait, postMatchWinnerSide == 1 ? Color.white : new Color(0.73f, 0.77f, 0.84f, 0.62f));
            SetText(postMatchScoreText, $"{leftScoreValue} - {rightScoreValue}");
            SetText(postMatchPromptText, inventory.IsTournamentActive || inventory.IsAdventureActive ? "CLICK TO CONTINUE" : "CLICK OR PRESS ENTER");
            postMatchAnimTime = 0f;
            if (postMatchCardRoot != null)
            {
                postMatchCardRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PostMatchCardCenterY + 12f);
                postMatchCardRoot.transform.localScale = Vector3.one * 0.96f;
            }

            // 3. 隐藏比赛中的比分板和右上角按钮，显示赛后结果卡片
            SetScoreboardVisible(false);
            SetGameObjectVisible(postMatchOverlayRoot, true);
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
            HideMessage();
            HideBonusNotice();
            HideCountdown();
        }

        private static string GetPostMatchSubtitle(mlpInventory inventory, int winnerSide)
        {
            if (inventory.IsTournamentActive)
            {
                return mlpSinglePlayerNarrative.TournamentResultSubtitle;
            }

            if (inventory.IsAdventureActive)
            {
                return winnerSide == -1 ? "LANTERN SIGIL CLAIMED" : "WARDEN GATE HELD";
            }

            return "FINAL SCORE";
        }

        /// <summary>
        /// 隐藏赛后结果卡片并恢复比分板。
        /// </summary>
        public void HidePostMatch()
        {
            SetText(postMatchTitleText, string.Empty);
            SetText(postMatchSubtitleText, string.Empty);
            SetText(postMatchScoreText, string.Empty);
            SetText(postMatchWinnerTagText, string.Empty);
            SetText(postMatchPromptText, string.Empty);
            postMatchAnimTime = 0f;
            postMatchWinnerSide = 0;
            postMatchTitleText.color = new Color32(0xFF, 0xC7, 0x56, 0xFF);
            postMatchSubtitleText.color = new Color32(0xCB, 0xD9, 0xE4, 0xFF);
            postMatchScoreText.color = new Color32(0xFF, 0xF3, 0xD8, 0xFF);
            postMatchWinnerTagText.color = new Color32(0xFF, 0xDE, 0x98, 0xFF);
            postMatchLeftNameText.color = Color.white;
            postMatchRightNameText.color = Color.white;
            SetSpriteTint(postMatchCardPanel, new Color(0.03f, 0.06f, 0.1f, 0.94f));
            SetSpriteTint(postMatchCardFrame, new Color(0.86f, 0.96f, 1f, 0.18f));
            SetSpriteTint(postMatchScorePlate, new Color(0.02f, 0.04f, 0.08f, 0.82f));
            SetSpriteTint(postMatchLeftAura, new Color(0.52f, 0.71f, 0.8f, 0.18f));
            SetSpriteTint(postMatchRightAura, new Color(0.52f, 0.71f, 0.8f, 0.18f));
            SetSpriteTint(postMatchLeftPortrait, Color.white);
            SetSpriteTint(postMatchRightPortrait, Color.white);
            postMatchPromptText.color = new Color32(0xEE, 0xF5, 0xD5, 0xFF);
            SetSpriteTint(postMatchTopGlow, new Color(0.34f, 0.86f, 0.92f, 0.08f));
            SetSpriteTint(postMatchBottomGlow, new Color(1f, 0.72f, 0.34f, 0.06f));
            SetSpriteTint(postMatchPromptFrame, new Color(0.3f, 0.82f, 0.9f, 0.34f));
            if (postMatchLeftAura != null)
            {
                postMatchLeftAura.transform.localScale = postMatchLeftAuraBaseScale;
            }

            if (postMatchRightAura != null)
            {
                postMatchRightAura.transform.localScale = postMatchRightAuraBaseScale;
            }

            if (postMatchLeftPortrait != null)
            {
                postMatchLeftPortrait.transform.localScale = postMatchLeftPortraitBaseScale;
            }

            if (postMatchRightPortrait != null)
            {
                postMatchRightPortrait.transform.localScale = postMatchRightPortraitBaseScale;
            }

            if (postMatchCardRoot != null)
            {
                postMatchCardRoot.transform.localScale = Vector3.one;
                postMatchCardRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PostMatchCardCenterY);
            }

            SetScoreboardVisible(true);
            SetGameObjectVisible(postMatchOverlayRoot, false);
        }

        /// <summary>
        /// 播放赛后卡片入场滑动动画和胜者头像脉冲效果。
        /// </summary>
        private void UpdatePostMatchVisual(float dt)
        {
            // 1. 累加动画时间，计算入场进度（0→1）和持续脉冲值
            postMatchAnimTime += dt;
            var intro = Mathf.Clamp01(postMatchAnimTime / 0.34f);
            var eased = 1f - Mathf.Pow(1f - intro, 3f);
            var pulse = 0.5f + (0.5f * Mathf.Sin(postMatchAnimTime * 2.4f));
            // 2. 卡片从略低位置滑入到最终位置，同时从 0.96 倍放大到 1 倍
            if (postMatchCardRoot != null)
            {
                postMatchCardRoot.transform.position = mlpConstants.PixelToWorldSnapped(
                    mlpConstants.Width2,
                    Mathf.Lerp(PostMatchCardCenterY + 12f, PostMatchCardCenterY, eased));
                postMatchCardRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
            }

            // 3. 胜者头像和光环持续脉冲放大，败者保持缩小状态
            var winnerAuraScale = 1f + (0.035f * pulse);
            var loserAuraScale = 0.92f;
            var winnerPortraitScale = 1f + (0.014f * pulse);
            var loserPortraitScale = 0.98f;
            if (postMatchLeftAura != null)
            {
                postMatchLeftAura.transform.localScale = postMatchLeftAuraBaseScale * (postMatchWinnerSide == -1 ? winnerAuraScale : loserAuraScale);
            }

            if (postMatchRightAura != null)
            {
                postMatchRightAura.transform.localScale = postMatchRightAuraBaseScale * (postMatchWinnerSide == 1 ? winnerAuraScale : loserAuraScale);
            }

            if (postMatchLeftPortrait != null)
            {
                postMatchLeftPortrait.transform.localScale = postMatchLeftPortraitBaseScale * (postMatchWinnerSide == -1 ? winnerPortraitScale : loserPortraitScale);
            }

            if (postMatchRightPortrait != null)
            {
                postMatchRightPortrait.transform.localScale = postMatchRightPortraitBaseScale * (postMatchWinnerSide == 1 ? winnerPortraitScale : loserPortraitScale);
            }

            // 4. 提示文字和发光效果跟随脉冲值变化，营造呼吸灯效果
            postMatchPromptText.color = new Color(0.93f, 0.96f, 0.84f, Mathf.Lerp(0.72f, 1f, pulse));
            SetSpriteTint(
                postMatchPromptFrame,
                new Color(0.3f, 0.82f, 0.9f, Mathf.Lerp(0.24f, 0.42f, pulse)));
            SetSpriteTint(
                postMatchTopGlow,
                new Color(0.34f, 0.86f, 0.92f, Mathf.Lerp(0.05f, 0.09f, pulse)));
            SetSpriteTint(
                postMatchBottomGlow,
                new Color(1f, 0.72f, 0.34f, Mathf.Lerp(0.04f, 0.08f, 1f - pulse)));
        }

        /// <summary>
        /// 播放弹出消息的弹入缩放动画并略微向上漂移。
        /// </summary>
        private void UpdateMessageVisual()
        {
            // 1. 如果消息根节点不存在或未激活，跳过动画
            if (messageRoot == null || !messageRoot.activeSelf)
            {
                return;
            }

            // 2. 计算动画进度（0 = 刚出现，1 = 即将消失）
            var progress = 1f - Mathf.Clamp01(messageTime / Mathf.Max(0.01f, messageDuration));
            // 3. 弹入动画：先从 0.78 放大到 1.08（过冲），再弹回到 1.0
            float scale;
            if (progress < 0.2f)
            {
                scale = Mathf.Lerp(0.78f, 1.08f, progress / 0.2f);
            }
            else if (progress < 0.4f)
            {
                scale = Mathf.Lerp(1.08f, 1f, (progress - 0.2f) / 0.2f);
            }
            else
            {
                scale = 1f;
            }

            // 4. 即将消失时略微缩小，营造退场感
            if (messageTime < MessageExitWindow)
            {
                scale *= Mathf.Lerp(0.92f, 1f, messageTime / MessageExitWindow);
            }

            // 5. 消息出现时从下方微微上浮，然后停在中央
            var lift = Mathf.Lerp(5f, 0f, Mathf.Clamp01(progress * 1.2f));
            messageRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PopupCenterY - lift);
            messageRoot.transform.localScale = Vector3.one * (scale * messageVisualScale);
        }

        /// <summary>
        /// 播放加分提示的缩放进入动画并缓缓向上漂移。
        /// </summary>
        private void UpdateBonusNoticeVisual()
        {
            // 1. 如果加分提示根节点不存在或未激活，跳过动画
            if (bonusNoticeRoot == null || !bonusNoticeRoot.activeSelf)
            {
                return;
            }

            // 2. 计算动画进度（0 = 刚出现，1 = 即将消失）
            var progress = 1f - Mathf.Clamp01(bonusNoticeTime / Mathf.Max(0.01f, bonusNoticeDuration));
            // 3. 缩放动画：先从 0.62 弹入到 0.8，再缓缓缩小到 0.72
            var scale = progress < 0.2f
                ? Mathf.Lerp(0.62f, 0.8f, progress / 0.2f)
                : Mathf.Lerp(0.8f, 0.72f, (progress - 0.2f) / 0.8f);
            // 4. 缓缓向上漂移，营造轻盈感
            var drift = Mathf.Lerp(4f, 0f, Mathf.Clamp01(progress * 1.1f));
            bonusNoticeRoot.transform.position = mlpConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY - drift);
            bonusNoticeRoot.transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// 播放倒计时数字的脉冲（先放大后缩小）动画效果。
        /// </summary>
        private void UpdateCountdownVisual()
        {
            // 1. 计算脉冲动画进度（0 = 刚触发，1 = 动画结束）
            var progress = 1f - Mathf.Clamp01(countdownPulseTime / CountdownPulseDuration);
            // 2. 先从 0.72 快速放大到 1.16（弹出感），再缓慢回到 1.0（稳定）
            float scale;
            if (progress < 0.24f)
            {
                scale = Mathf.Lerp(0.72f, 1.16f, progress / 0.24f);
            }
            else
            {
                scale = Mathf.Lerp(1.16f, 1f, (progress - 0.24f) / 0.76f);
            }

            // 3. 应用缩放到倒计时文字上
            countdownText.transform.localScale = countdownBaseScale * scale;
        }

        /// <summary>
        /// 根据弹出消息的内容选择文字颜色（例如"GO!!!"为绿色）。
        /// </summary>
        private void ApplyMessageTheme(string message)
        {
            // 1. 根据消息内容匹配不同的主题颜色
            switch (message)
            {
                // 2. 三分球：橙色
                case "3 POINT":
                    messageText.color = new Color32(0xFF, 0x98, 0x10, 0xFF);
                    break;
                // 3. 投篮得分：金色
                case "BASKET":
                    messageText.color = new Color32(0xFF, 0xC5, 0x57, 0xFF);
                    break;
                // 4. 比赛开始：绿色
                case "GO!!!":
                    messageText.color = new Color32(0x9C, 0xFF, 0x4A, 0xFF);
                    break;
                // 5. 时间到：暖黄色
                case "TIME!!!":
                    messageText.color = new Color32(0xFF, 0xBA, 0x40, 0xFF);
                    break;
                // 6. 加时赛：青色
                case "OVERTIME":
                    messageText.color = new Color32(0x42, 0xFF, 0xEA, 0xFF);
                    break;
                // 7. 地狱冲刺：橙红色
                case "HELL DASH!":
                    messageText.color = new Color32(0xFF, 0x62, 0x32, 0xFF);
                    break;
                // 8. 地狱护盾：粉红色
                case "HELL SHIELD!":
                    messageText.color = new Color32(0xFF, 0x52, 0x92, 0xFF);
                    break;
                case "FOG WIND ACTIVE":
                    messageText.color = new Color32(0xA8, 0xF7, 0xFF, 0xFF);
                    break;
                // 9. 其他消息：紫色（默认）
                default:
                    messageText.color = new Color32(0x8B, 0x2D, 0xFF, 0xFF);
                    break;
            }
        }

        /// <summary>
        /// 根据加分提示的内容选择文字颜色。
        /// </summary>
        private void ApplyBonusNoticeTheme(string message)
        {
            switch (message)
            {
                case "HELL DASH!":
                    bonusNoticeText.color = new Color32(0xFF, 0x74, 0x2B, 0xFF);
                    break;
                case "HELL SHIELD!":
                    bonusNoticeText.color = new Color32(0xFF, 0x5A, 0xB8, 0xFF);
                    break;
                default:
                    bonusNoticeText.color = new Color32(0xFF, 0xD6, 0x63, 0xFF);
                    break;
            }
        }

        /// <summary>
        /// 缩小较长的消息使其适应屏幕。返回缩放倍数。
        /// </summary>
        private static float ResolveMessageScale(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return 1f;
            }

            if (message.Length >= 10)
            {
                return 0.88f;
            }

            return message.Length >= 8 ? 0.94f : 1f;
        }

        /// <summary>
        /// 为赛后卡片上较长的角色名称选择较小的字号。
        /// </summary>
        private static int ResolvePostMatchNameFontSize(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return 14;
            }

            if (characterName.Length >= 12)
            {
                return 12;
            }

            return characterName.Length >= 9 ? 13 : 14;
        }

        /// <summary>
        /// 创建用于暂停/赛后背景的纯色矩形面板。
        /// </summary>
        private static GameObject CreatePausePanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var standalonePanel = TryCreateStandaloneTintPanel(name, x, y, width, height, sortingOrder, parent, tint);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = mlpRender.Sprite(name, mlpAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / 10f,
                mlpConstants.UnitsPerPixel * height / 10f,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        /// <summary>
        /// 创建用于暂停/赛后卡片轮廓的带边框面板。
        /// </summary>
        private static GameObject CreatePauseFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var standalonePanel = TryCreateStandalonePauseFrame(name, frame, x, y, width, height, sortingOrder, parent, tint);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = mlpRender.Sprite(name, mlpAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = mlpAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    mlpConstants.UnitsPerPixel * width / sourceWidth,
                    mlpConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject TryCreateStandaloneTintPanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.PanelFillSoft));
            if (texture == null)
            {
                return null;
            }

            var panel = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject TryCreateStandalonePauseFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var imageKey = ResolveStandalonePauseFrameImage(frame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            var panel = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static string ResolveStandalonePauseFrameImage(string frame)
        {
            return frame switch
            {
                "MatchBack0001" => mlpAssets.Images.Ui.FrameMatchCardIdle,
                "MatchBack0002" => mlpAssets.Images.Ui.FrameMatchCardActive,
                "btn_bg0000" => mlpAssets.Images.Ui.MenuButtonPlate,
                _ => null
            };
        }

        /// <summary>
        /// 安全地显示或隐藏 GameObject（对象为空时不做任何操作）。
        /// </summary>
        private static void SetGameObjectVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// 更新 TextMesh 标签并请求字体纹理刷新。
        /// </summary>
        private static void SetText(TextMesh target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value;
            target.font?.RequestCharactersInTexture(value, target.fontSize, FontStyle.Normal);
        }

        /// <summary>
        /// 显示或隐藏整个暂停覆盖层，包括菜单和恢复按钮。
        /// </summary>
        private void SetPauseOverlayVisible(bool visible)
        {
            SetGameObjectVisible(pauseOverlayRoot, visible);
            pauseMenuButton.SetVisible(visible);
            pauseResumeButton.SetVisible(visible);
        }

        /// <summary>
        /// 显示或隐藏游戏内比分板（教程模式下始终隐藏）。
        /// </summary>
        private void SetScoreboardVisible(bool visible)
        {
            SetGameObjectVisible(scoreboardRoot, visible && !isTutorial);
        }

        /// <summary>
        /// 创建屏幕右上角的暂停按钮图标。
        /// </summary>
        private GameObject CreatePauseButtonIcon(Transform parent)
        {
            var resourcePath = mlpAssets.Images.ResourcePath(mlpAssets.Images.PauseButton);
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return mlpIconButton.CreateImageIcon("PauseButtonIcon", resourcePath, PauseButtonX, TopRightButtonY, 82, TopRightIconPixels, parent);
            }

            var icon = mlpRender.Sprite("PauseButtonIconFallback", mlpAtlasCache.Instance.Gameplay, "InGamePauseButton0000", PauseButtonX, TopRightButtonY, 0.5f, 0.5f, 82, parent);
            icon.transform.localScale *= 1.2f;
            return icon;
        }

        /// <summary>
        /// 切换背景音乐的开关。
        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            mlpAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// 空占位回调，不执行任何操作。
        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// 音乐播放时返回 0，静音时返回 1（用于选择正确的按钮图标）。
        /// </summary>
        private static int GetMusicIconIndex()
        {
            return mlpAudio.Instance != null && mlpAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// 创建比分板背景图片（资源缺失时回退到图集精灵）。
        /// </summary>
        private static void CreateScoreboardBackdrop(Transform parent)
        {
            var image = CreateHudImage("ScoreboardBackdrop", mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Scoreboard), ScoreboardCenterX, ScoreboardCenterY, ScoreboardTargetWidth, 80, parent);
            if (image != null)
            {
                return;
            }

            mlpRender.Sprite("InfoPanel", mlpAtlasCache.Instance.Gameplay, "infoPanel0000", mlpConstants.Width2, 60f, 0.5f, 0.5f, 80, parent);
        }

        /// <summary>
        /// 创建弹出消息背景（当前未使用——消息不再需要边框即可渲染）。
        /// </summary>
        private static GameObject CreatePopupBackdrop(Transform parent)
        {
            // 弹出边框已从 HUD 中移除。消息文字现在独立渲染，
            // 所以这个素材不会再出现了。
            return null;
        }

        /// <summary>
        /// 从 Resources 加载纹理并创建按目标宽度缩放的精灵。未找到时返回 null。
        /// </summary>
        private static GameObject CreateHudImage(string name, string resourcePath, float x, float y, float targetWidth, int sortingOrder, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var image = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            image.transform.localScale *= targetWidth / Mathf.Max(1f, texture.width);
            return image;
        }

        /// <summary>
        /// 创建一个空的根 GameObject 并将其挂载到指定的 Transform 下。
        /// </summary>
        private static GameObject CreateHudRoot(string name, Transform parent)
        {
            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            return root;
        }

        /// <summary>
        /// 创建一个位于指定像素坐标的空 GameObject。
        /// </summary>
        private static GameObject CreateHudAnchor(string name, float x, float y, Transform parent)
        {
            var anchor = new GameObject(name);
            if (parent != null)
            {
                anchor.transform.SetParent(parent, false);
            }

            anchor.transform.position = mlpConstants.PixelToWorldSnapped(x, y);
            return anchor;
        }

        /// <summary>
        /// 创建位于角色头像后方的发光光环。
        /// </summary>
        private static GameObject CreatePortraitAura(string name, float x, float y, float scale, int sortingOrder, Transform parent)
        {
            const float legacyAuraPixels = 150f;
            var diameterPixels = legacyAuraPixels * scale;
            return mlpRender.PortraitBackplate(
                name,
                x,
                y,
                diameterPixels,
                sortingOrder,
                parent,
                new Color(0.16f, 0.96f, 0.9f, 0.28f),
                new Color(0.01f, 0.025f, 0.06f, 0.92f),
                new Color(0.48f, 1f, 0.94f, 0.95f));
        }

        /// <summary>
        /// 创建按指定像素位置缩放和定位的角色头像精灵。
        /// </summary>
        private static GameObject CreateCharacterPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            var targetSize = targetPixels * mlpPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var sprite = mlpPlayersData.GetCharacterPortraitSprite(characterId, targetSize);
            if (sprite == null)
            {
                return null;
            }

            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetSize / Mathf.Max(1f, spritePixels);
            var adjustedY = y + mlpPlayersData.GetCharacterPortraitOffsetY(characterId, sprite) * scale;
            mlpRender.ApplyPixelTransform(portrait.transform, x, adjustedY, 0f, scale);
            return portrait;
        }

        /// <summary>
        /// 设置 GameObject 的 SpriteRenderer 颜色色调（为空时不做任何操作）。
        /// </summary>
        private static void SetSpriteTint(GameObject target, Color tint)
        {
            if (target == null)
            {
                return;
            }

            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = tint;
            }
        }

        /// <summary>
        /// 将秒数转换为类似"1:00"或"04.2"的显示字符串。
        /// </summary>
        private static string FormatTime(float secondsLeft)
        {
            var clamped = Mathf.Max(0f, secondsLeft);
            if (clamped >= 60f)
            {
                var minuteRemainder = Mathf.FloorToInt(clamped - 60f);
                return $"1:{minuteRemainder:00}";
            }

            var seconds = Mathf.FloorToInt(clamped);
            var tenths = Mathf.Clamp(Mathf.FloorToInt((clamped - seconds) * 10f), 0, 9);
            return clamped >= 10f ? $"{seconds}.{tenths}" : $"0{seconds}.{tenths}";
        }
    }

    /// <summary>
    /// 菜单按钮：暂停菜单中的可点击按钮，支持鼠标悬停高亮和点击回调。
    /// </summary>
    public sealed class mlpMenuButton
    {
        private Rect rect;                              // 按钮的矩形区域（用于鼠标碰撞检测）
        private readonly System.Action action;           // 点击时执行的回调函数
        private readonly GameObject sprite;              // 按钮背景的 GameObject
        private readonly TextMesh label;                 // 按钮文字（旧版 Unity 文字系统）
        private readonly TMP_Text nativeLabel;           // 按钮文字（TextMeshPro 文字系统）
        private readonly Transform labelTransform;       // 文字的 Transform 组件
        private readonly Vector3 baseScale;              // 背景精灵的基础缩放值
        private readonly Vector3 labelBaseScale;         // 文字的基础缩放值
        private bool visible = true;                     // 按钮整体是否可见
        private bool backgroundVisible = true;           // 背景精灵是否可见
        private bool labelVisible = true;                // 文字标签是否可见
        private bool pressed;                            // 当前是否处于按下状态
        public GameObject Root => sprite;                // 公开访问背景 GameObject 的属性

        /// <summary>
        /// 创建一个带背景精灵和文字标签的可点击菜单按钮。
        /// </summary>
        public mlpMenuButton(
            string text,
            float x,
            float y,
            float width,
            float height,
            System.Action action,
            Transform parent,
            int sortingOrder = 50,
            mlpTextStyle labelStyle = mlpTextStyle.ButtonLabel)
        {
            // 1. 保存点击回调函数，计算按钮在屏幕上的碰撞矩形
            this.action = action;
            rect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
            // 2. 加载按钮背景纹理（优先使用独立纹理，否则回退到图集精灵）
            var buttonTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.MenuButtonPlate));
            float sourceWidth;
            float sourceHeight;
            if (buttonTexture != null)
            {
                sprite = mlpRender.Image($"Button_{text}", buttonTexture, x, y, 0.5f, 0.5f, sortingOrder, parent);
                sourceWidth = Mathf.Max(1f, buttonTexture.width);
                sourceHeight = Mathf.Max(1f, buttonTexture.height);
            }
            else
            {
                sprite = mlpRender.Sprite($"Button_{text}", mlpAtlasCache.Instance.Interface, "btn_bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
                var frame = mlpAtlasCache.Instance.Interface.Frame("btn_bg0000");
                sourceWidth = frame != null ? Mathf.Max(1f, frame.W) : 1f;
                sourceHeight = frame != null ? Mathf.Max(1f, frame.H) : 1f;
            }

            // 3. 将背景精灵缩放到指定的像素尺寸
            sprite.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / sourceWidth,
                mlpConstants.UnitsPerPixel * height / sourceHeight,
                1f);

            // 4. 记录基础缩放值（悬停时会在此基础上放大）
            baseScale = sprite.transform.localScale;
            // 5. 根据按钮高度计算字号，创建文字标签（优先使用原生文字层，否则用 TextMesh）
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.55f), 18, 32);
            if (mlpNativeMenuTextLayer.Active != null && mlpNativeMenuTextLayer.Active.Owns(parent))
            {
                nativeLabel = mlpNativeMenuTextLayer.Active.CreateText(
                    $"ButtonText_{text}",
                    text,
                    x,
                    y + 1f,
                    fontSize,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    labelStyle);
                labelTransform = nativeLabel.rectTransform;
            }
            else
            {
                label = mlpRender.Text(
                    $"ButtonText_{text}",
                    text,
                    x,
                    y + 1f,
                    fontSize,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    sortingOrder + 30,
                    parent,
                    labelStyle);
                labelTransform = label.transform;
            }

            // 6. 记录标签的基础缩放值
            labelBaseScale = labelTransform != null ? labelTransform.localScale : Vector3.one;
        }

        /// <summary>
        /// 每帧检测鼠标悬停和点击。悬停时高亮显示，点击时触发动作。
        /// </summary>
        public void Update(Camera camera)
        {
            // 1. 按钮不可见或没有相机时，重置按下状态并跳过
            if (!visible)
            {
                pressed = false;
                return;
            }

            if (camera == null)
            {
                pressed = false;
                return;
            }

            // 2. 获取鼠标位置，转换为游戏像素坐标，判断是否在按钮区域内
            var mouse = Input.mousePosition;
            Vector2 pixel;
            bool inside;
            if (mlpFixedResolutionPresenter.HasActivePresenter)
            {
                inside = mlpFixedResolutionPresenter.TryMapScreenToGamePixel(mouse, out pixel) && rect.Contains(pixel);
            }
            else
            {
                var screenPoint = new Vector2(mouse.x, mouse.y);
                if (!camera.pixelRect.Contains(screenPoint))
                {
                    pixel = default;
                    inside = false;
                }
                else
                {
                    var world = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -camera.transform.position.z));
                    pixel = mlpConstants.WorldToPixel(world);
                    inside = rect.Contains(pixel);
                }
            }

            // 3. 悬停时按钮和标签略微放大（1.035 倍），否则恢复原始大小
            sprite.transform.localScale = inside ? baseScale * 1.035f : baseScale;
            if (labelTransform != null)
            {
                labelTransform.localScale = inside ? labelBaseScale * 1.035f : labelBaseScale;
            }

            // 4. 悬停时文字变为金黄色，否则为白色
            var labelColor = inside ? new Color(1f, 0.92f, 0.25f) : Color.white;
            if (label != null)
            {
                label.color = labelColor;
            }
            else if (nativeLabel != null)
            {
                nativeLabel.color = labelColor;
            }

            // 5. 鼠标按下时记录"已按下"状态
            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            // 6. 鼠标松开时：如果之前按下了且仍在按钮区域内，播放音效并执行回调
            if (pressed && Input.GetMouseButtonUp(0))
            {
                pressed = false;
                if (inside)
                {
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.Button);
                    action?.Invoke();
                }
            }
        }

        /// <summary>
        /// 更改按钮的标签文字。
        /// </summary>
        public void SetText(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
            else if (nativeLabel != null)
            {
                nativeLabel.text = text;
            }
        }

        /// <summary>
        /// 显示或隐藏整个按钮（背景和标签）。
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            sprite.SetActive(isVisible && backgroundVisible);
            if (label != null)
            {
                label.gameObject.SetActive(isVisible && labelVisible);
            }
            else if (nativeLabel != null)
            {
                nativeLabel.gameObject.SetActive(isVisible && labelVisible);
            }
        }

        public void SetPosition(float x, float y)
        {
            rect = new Rect(x - rect.width * 0.5f, y - rect.height * 0.5f, rect.width, rect.height);
            SetTransformPosition(sprite != null ? sprite.transform : null, x, y);

            if (labelTransform != null)
            {
                SetTransformPosition(labelTransform, x, y + 1f);
            }
        }

        /// <summary>
        /// 仅显示或隐藏按钮背景（保持标签可见）。
        /// </summary>
        public void SetBackgroundVisible(bool isVisible)
        {
            backgroundVisible = isVisible;
            sprite.SetActive(visible && backgroundVisible);
        }

        /// <summary>
        /// 仅显示或隐藏按钮标签文字（保持背景可见）。
        /// </summary>
        public void SetLabelVisible(bool isVisible)
        {
            labelVisible = isVisible;
            if (label != null)
            {
                label.gameObject.SetActive(visible && labelVisible);
            }
            else if (nativeLabel != null)
            {
                nativeLabel.gameObject.SetActive(visible && labelVisible);
            }
        }

        private static void SetTransformPosition(Transform transform, float x, float y)
        {
            if (transform == null)
            {
                return;
            }

            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                mlpNativeMenuTextLayer.SetPixelPosition(rectTransform, x, y);
                return;
            }

            transform.position = mlpConstants.PixelToWorldSnapped(x, y, transform.position.z);
        }
    }

    /// <summary>
    /// 图标按钮：小型图标样式的可点击按钮，用于 HUD 上的功能按钮（如帮助、音效开关）。
    /// </summary>
    public sealed class mlpIconButton
    {
        private readonly mlpMenuButton button;
        private readonly GameObject[] icons;
        private bool visible = true;
        private int activeIconIndex;

        /// <summary>
        /// 创建一个可在多个图标图片之间切换的按钮（例如音乐开/关）。
        /// </summary>
        public mlpIconButton(
            string name,
            float x,
            float y,
            float width,
            float height,
            System.Action action,
            Transform parent,
            int sortingOrder,
            float targetPixels,
            params string[] resourcePaths)
        {
            button = new mlpMenuButton(string.Empty, x, y, width, height, action, parent);
            button.SetBackgroundVisible(false);
            button.SetLabelVisible(false);

            icons = new GameObject[resourcePaths.Length];
            for (var i = 0; i < resourcePaths.Length; i++)
            {
                icons[i] = CreateImageIcon($"{name}_Icon{i}", resourcePaths[i], x, y, sortingOrder, targetPixels, parent);
            }

            RefreshIcons();
        }

        /// <summary>
        /// 将鼠标输入处理转发给内部菜单按钮。
        /// </summary>
        public void Update(Camera camera)
        {
            button.Update(camera);
        }

        /// <summary>
        /// 显示或隐藏图标按钮及其当前图标。
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            button.SetVisible(isVisible);
            RefreshIcons();
        }

        /// <summary>
        /// 切换当前显示的图标图片。
        /// </summary>
        public void SetActiveIconIndex(int iconIndex)
        {
            activeIconIndex = Mathf.Clamp(iconIndex, 0, Mathf.Max(0, icons.Length - 1));
            RefreshIcons();
        }

        /// <summary>
        /// 加载纹理并在指定位置创建缩放后的精灵图标。
        /// </summary>
        public static GameObject CreateImageIcon(string name, string resourcePath, float x, float y, int sortingOrder, float targetPixels, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var icon = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var sourcePixels = Mathf.Max(texture.width, texture.height);
            icon.transform.localScale *= targetPixels / Mathf.Max(1f, sourcePixels);
            return icon;
        }

        /// <summary>
        /// 仅显示当前激活的图标，隐藏其余所有图标。
        /// </summary>
        private void RefreshIcons()
        {
            for (var i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                {
                    icons[i].SetActive(visible && i == activeIconIndex);
                }
            }
        }
    }

    /// <summary>
    /// 能量条视图：显示角色大招充能进度的条形 UI，充满后可以释放大招。
    /// </summary>
    public sealed class mlpEnergyBarView
    {
        private const float SoloEnergyX = 45f;
        private const float PlayerOneEnergyX = 45f;
        private const float PlayerTwoEnergyX = 706f;
        private const float EnergyY = 45f;
        private const float PlayerTwoEnergyY = 126f;
        private readonly mlpRadialIconMesh overlay;

        /// <summary>
        /// 为玩家槽位构建能量/技能充能条 UI。
        /// </summary>
        public mlpEnergyBarView(Transform parent, int controllerSlot, mlpCharacterSkillDefinition skillDefinition, float fullTime)
        {
            // 1. 获取玩家控制器配置，根据玩家槽位确定能量条位置
            var profile = mlpControlsData.ProfileForSlot(controllerSlot);
            var x = SoloEnergyX;
            var y = EnergyY;
            if (controllerSlot == 1)
            {
                x = PlayerOneEnergyX;
            }
            else if (controllerSlot == 2)
            {
                x = PlayerTwoEnergyX;
                y = PlayerTwoEnergyY;
            }

            // 2. 创建能量条背景（优先使用独立纹理，否则回退到图集精灵）
            const float legacyEnergyBgWidth = 95f;
            const float legacyEnergyBgHeight = 89f;
            const float standaloneEnergyIconPixels = 76f;
            var energyTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.EnergyButtonPlate));
            GameObject bg;
            if (energyTexture != null)
            {
                bg = mlpRender.Image($"EnergyBg_{controllerSlot}", energyTexture, x, y + 1f, 0.5f, 0.5f, 83, parent);
                bg.transform.localScale = new Vector3(
                    mlpConstants.UnitsPerPixel * legacyEnergyBgWidth * 1.1f / Mathf.Max(1f, energyTexture.width),
                    mlpConstants.UnitsPerPixel * legacyEnergyBgHeight * 1.1f / Mathf.Max(1f, energyTexture.height),
                    1f);
            }
            else
            {
                bg = mlpRender.Sprite($"EnergyBg_{controllerSlot}", mlpAtlasCache.Instance.Gameplay, "btn_bg20000", x, y + 1f, 0.5f, 0.5f, 83, parent);
                bg.transform.localScale *= 1.1f;
            }

            // 3. 加载技能图标的基础图和径向遮罩图
            var baseResourcePath = skillDefinition.HasStandaloneIconArt
                ? mlpAssets.Images.ResourcePath(skillDefinition.IconImageKey)
                : null;
            var maskResourcePath = skillDefinition.HasStandaloneIconArt
                ? mlpAssets.Images.ResourcePath(skillDefinition.ChargeMaskImageKey)
                : null;
            var baseTexture = !string.IsNullOrEmpty(baseResourcePath) ? Resources.Load<Texture2D>(baseResourcePath) : null;
            var maskTexture = !string.IsNullOrEmpty(maskResourcePath) ? Resources.Load<Texture2D>(maskResourcePath) : null;

            // 4. 创建技能图标层和径向填充遮罩层（遮罩层用于显示充能进度）
            if (baseTexture != null && maskTexture != null)
            {
                mlpIconButton.CreateImageIcon($"EnergyBase_{controllerSlot}", baseResourcePath, x, y, 84, standaloneEnergyIconPixels, parent);
                overlay = new mlpRadialIconMesh($"EnergyFill_{controllerSlot}", maskTexture, x, y, 85, parent, standaloneEnergyIconPixels);
            }
            else
            {
                if (baseTexture != null)
                {
                    mlpIconButton.CreateImageIcon($"EnergyBase_{controllerSlot}", baseResourcePath, x, y, 84, standaloneEnergyIconPixels, parent);
                }

                if (maskTexture != null)
                {
                    overlay = new mlpRadialIconMesh($"EnergyFill_{controllerSlot}", maskTexture, x, y, 85, parent, standaloneEnergyIconPixels);
                }
                else
                {
                    Debug.LogWarning($"Missing standalone skill mask art for {skillDefinition.SkillName}; energy fill overlay will stay hidden.");
                }
            }

            // 5. 创建按键提示背景和提示文字（如 "E" 键释放大招）
            mlpRender.Sprite($"EnergyHintBg_{controllerSlot}", mlpAtlasCache.Instance.Gameplay, "key_hint0000", x - 30f, y + 30f, 0.5f, 0.5f, 86, parent);
            mlpRender.Text(
                $"EnergyHint_{controllerSlot}",
                profile.SuperHint,
                x - 30f,
                y + 32f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                87,
                parent,
                mlpTextStyle.TournamentBody);

            // 6. 设置初始充能状态（充能时间为 0 则直接充满，否则从 0 开始）
            SetCharge(fullTime <= 0f ? 1f : 0f);
        }

        /// <summary>
        /// 更新能量条的径向填充，显示技能距离完全充能还有多近。
        /// </summary>
        public void SetCharge(float progress)
        {
            overlay?.SetProgress(progress);
        }

        /// <summary>
        /// 离开比赛时清理能量条的网格资源。
        /// </summary>
        public void ReleaseRuntimeResources()
        {
            overlay?.ReleaseRuntimeResources();
        }
    }

    /// <summary>
    /// 径向图标网格：用扇形网格来显示技能图标的部分填充效果，表示技能冷却或充能进度。
    /// </summary>
    public sealed class mlpRadialIconMesh
    {
        private const int RadialSteps = 36;
        private const float DegreesPerStep = 10f;
        private readonly GameObject graphic;
        private readonly Mesh mesh;
        private readonly float width;
        private readonly float height;
        private readonly Vector2 uvMin;
        private readonly Vector2 uvMax;

        /// <summary>
        /// 使用图集中的精灵创建径向填充图标。
        /// </summary>
        public mlpRadialIconMesh(string name, mlpAtlas atlas, string frameName, float x, float y, int sortingOrder, Transform parent)
        {
            graphic = new GameObject(name);
            graphic.transform.SetParent(parent, false);

            var filter = graphic.AddComponent<MeshFilter>();
            var renderer = graphic.AddComponent<MeshRenderer>();
            var sprite = atlas.Sprite(frameName, 0.5f, 0.5f);
            var frame = atlas.Frame(frameName);
            mesh = new Mesh { name = $"{name}_Mesh" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            renderer.sharedMaterial = mlpSharedMaterialCache.GetSpritesDefault(sprite.texture);
            renderer.sortingOrder = sortingOrder;

            width = frame.W;
            height = frame.H;
            var rect = sprite.rect;
            uvMin = new Vector2(rect.xMin / sprite.texture.width, rect.yMin / sprite.texture.height);
            uvMax = new Vector2(rect.xMax / sprite.texture.width, rect.yMax / sprite.texture.height);

            mlpRender.ApplyPixelTransform(graphic.transform, x, y, 0.13f, 1f);
            SetProgress(0f);
        }

        /// <summary>
        /// 使用独立纹理创建径向填充图标。
        /// </summary>
        public mlpRadialIconMesh(string name, Texture2D texture, float x, float y, int sortingOrder, Transform parent, float targetPixels)
        {
            graphic = new GameObject(name);
            graphic.transform.SetParent(parent, false);

            var filter = graphic.AddComponent<MeshFilter>();
            var renderer = graphic.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = $"{name}_Mesh" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            renderer.sharedMaterial = mlpSharedMaterialCache.GetSpritesDefault(texture);
            renderer.sortingOrder = sortingOrder;

            var sourcePixels = Mathf.Max(1f, Mathf.Max(texture.width, texture.height));
            width = targetPixels * texture.width / sourcePixels;
            height = targetPixels * texture.height / sourcePixels;
            uvMin = Vector2.zero;
            uvMax = Vector2.one;

            mlpRender.ApplyPixelTransform(graphic.transform, x, y, 0.13f, 1f);
            SetProgress(0f);
        }

        /// <summary>
        /// 更新径向图标的可见程度。0 = 空，1 = 完全填充。
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progress >= 0.999f)
            {
                graphic.SetActive(false);
                return;
            }

            graphic.SetActive(true);
            var hiddenSteps = Mathf.Clamp(Mathf.FloorToInt(progress * RadialSteps), 0, RadialSteps);
            BuildSector((RadialSteps - hiddenSteps) * DegreesPerStep);
        }

        /// <summary>
        /// 重建网格三角形以显示指定度数的扇形区域。
        /// </summary>
        private void BuildSector(float degrees)
        {
            // 1. 根据角度计算需要多少个三角形分段（最多 36 段 = 360 度）
            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(RadialSteps * Mathf.Clamp01(degrees / 360f)));
            var radius = Mathf.Min(width, height) * 0.5f;
            // 2. 分配顶点、UV 和三角形索引数组（顶点数 = 中心点 + 扇形边缘点）
            var vertices = new Vector3[segmentCount + 2];
            var uvs = new Vector2[segmentCount + 2];
            var triangles = new int[segmentCount * 3];
            // 3. 中心顶点位于原点，UV 设为纹理中心
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f);

            // 4. 从 12 点钟方向顺时针生成扇形边缘顶点
            for (var i = 0; i <= segmentCount; i++)
            {
                var t = segmentCount == 0 ? 0f : i / (float)segmentCount;
                var angle = (90f - degrees * t) * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                // 5. 设置顶点位置
                vertices[i + 1] = new Vector3(point.x, point.y, 0f);
                // 6. 将顶点位置映射到纹理 UV 坐标（实现扇形裁剪纹理效果）
                var uvX = Mathf.Lerp(uvMin.x, uvMax.x, point.x / width + 0.5f);
                var uvY = Mathf.Lerp(uvMin.y, uvMax.y, point.y / height + 0.5f);
                uvs[i + 1] = new Vector2(uvX, uvY);
                // 7. 为每个分段创建一个三角形（中心点 → 当前边缘点 → 下一个边缘点）
                if (i == segmentCount)
                {
                    continue;
                }

                var tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }

            // 8. 将计算好的数据上传到网格
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// 销毁网格以释放内存，当图标不再需要时调用。
        /// </summary>
        public void ReleaseRuntimeResources()
        {
            if (mesh == null)
            {
                return;
            }

            var filter = graphic != null ? graphic.GetComponent<MeshFilter>() : null;
            if (filter != null && filter.sharedMesh == mesh)
            {
                filter.sharedMesh = null;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}
