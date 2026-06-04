// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushHudView 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using TMPro;
using UnityEngine;

namespace rimrush
{
    public enum rimrushPauseCommand
    {
        None,
        Toggle,
        Resume,
        Menu
    }

    public sealed class rimrushHudView
    {
        private const float ScreenCenterY = 240f;
        private const float ScoreboardCenterX = rimrushConstants.Width2;
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
        private readonly rimrushMenuButton pauseButton;
        private readonly GameObject pauseButtonIcon;
        private readonly rimrushIconButton musicButton;
        private readonly rimrushIconButton helpButton;
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
        private readonly rimrushMenuButton pauseMenuButton;
        private readonly rimrushMenuButton pauseResumeButton;
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
        private rimrushPauseCommand pendingPauseCommand;
        private int lastCountdownTick = int.MinValue;
        private float postMatchAnimTime;
        private int postMatchWinnerSide;
        public bool IsPostMatchVisible => postMatchOverlayRoot != null && postMatchOverlayRoot.activeSelf;
        public bool IsPauseOverlayVisible { get; private set; }

        /// <summary>
        /// Executes rimrush Hud View for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="matchData">Input value used by this step of the workflow.</param>
        public rimrushHudView(Transform parent, rimrushMatchData matchData)
        {
            var gameMode = rimrushInventory.Instance.GameMode;
            isTutorial = gameMode == rimrushGameModeIds.Tutorial;
            isTraining = gameMode == rimrushGameModeIds.Training || gameMode == rimrushGameModeIds.Tutorial;
            leftCharacterLabel = rimrushPlayersData.GetCharacterName(matchData.CharacterIds[0]);
            rightCharacterLabel = rimrushPlayersData.GetCharacterName(matchData.CharacterIds[1]);

            scoreboardRoot = CreateHudRoot("ScoreboardRoot", parent);
            CreateScoreboardBackdrop(scoreboardRoot.transform);
            CreatePortraitAura("LeftPortraitAura", ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, 0.34f, 80, scoreboardRoot.transform);
            CreatePortraitAura("RightPortraitAura", ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, 0.34f, 80, scoreboardRoot.transform);
            CreateCharacterPortrait("LeftPortrait", matchData.CharacterIds[0], ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, scoreboardRoot.transform);
            CreateCharacterPortrait("RightPortrait", matchData.CharacterIds[1], ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, scoreboardRoot.transform);

            leftNameText = rimrushRender.Text(
                "LeftName",
                leftCharacterLabel,
                ScoreboardCenterX - NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleRight,
                85,
                scoreboardRoot.transform,
                rimrushTextStyle.HudName);
            rightNameText = rimrushRender.Text(
                "RightName",
                rightCharacterLabel,
                ScoreboardCenterX + NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleLeft,
                85,
                scoreboardRoot.transform,
                rimrushTextStyle.HudName);

            var scoreColor = new Color32(0xFF, 0xA7, 0x22, 0xFF);
            leftScore = rimrushRender.Text(
                "LeftScore",
                "0",
                ScoreboardCenterX - ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                scoreboardRoot.transform,
                rimrushTextStyle.HudScore);
            rightScore = rimrushRender.Text(
                "RightScore",
                "0",
                ScoreboardCenterX + ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                scoreboardRoot.transform,
                rimrushTextStyle.HudScore);
            timerText = rimrushRender.Text(
                "Timer",
                "1:00",
                ScoreboardCenterX,
                TimerY,
                18,
                new Color32(0xC6, 0xFF, 0x33, 0xFF),
                TextAnchor.MiddleCenter,
                87,
                scoreboardRoot.transform,
                rimrushTextStyle.HudTimer);
            SetScoreboardVisible(true);

            pauseButton = new rimrushMenuButton(string.Empty, PauseButtonX, TopRightButtonY, TopRightButtonSize, TopRightButtonSize, () => pendingPauseCommand = rimrushPauseCommand.Toggle, parent);
            pauseButton.SetBackgroundVisible(false);
            pauseButton.SetLabelVisible(false);
            pauseButtonIcon = CreatePauseButtonIcon(parent);
            musicButton = new rimrushIconButton(
                "HudMusicButton",
                MusicButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                ToggleBackgroundMusic,
                parent,
                82,
                TopRightIconPixels,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));
            musicButton.SetActiveIconIndex(GetMusicIconIndex());
            helpButton = new rimrushIconButton(
                "HudHelpButton",
                HelpButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                rimrushHelpPanel.ShowKeyboardPage,
                parent,
                82,
                TopRightIconPixels,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));

            countdownCaptionText = rimrushRender.Text(
                "CountdownCaption",
                string.Empty,
                rimrushConstants.Width2,
                CountdownY - 28f,
                16,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                rimrushTextStyle.TournamentAccent);
            countdownText = rimrushRender.Text(
                "Countdown",
                string.Empty,
                rimrushConstants.Width2,
                CountdownY,
                58,
                new Color32(0xFF, 0xB8, 0x2E, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                rimrushTextStyle.HudPopup);
            countdownBaseScale = countdownText.transform.localScale;

            messageRoot = CreateHudAnchor("MessageRoot", rimrushConstants.Width2, PopupCenterY, parent);
            messageBackdrop = CreatePopupBackdrop(parent);
            if (messageBackdrop != null)
            {
                messageBackdrop.transform.SetParent(messageRoot.transform, true);
            }

            messageText = rimrushRender.Text(
                "Message",
                string.Empty,
                rimrushConstants.Width2,
                PopupCenterY + 2f,
                56,
                new Color32(0x8B, 0x2D, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                rimrushTextStyle.HudPopup);
            messageText.transform.SetParent(messageRoot.transform, true);
            messageRoot.SetActive(false);

            bonusNoticeRoot = CreateHudAnchor("BonusNoticeRoot", BonusNoticeX, BonusNoticeY, parent);
            bonusNoticeText = rimrushRender.Text(
                "BonusNotice",
                string.Empty,
                BonusNoticeX,
                BonusNoticeY,
                16,
                new Color32(0xFF, 0x7A, 0x39, 0xFF),
                TextAnchor.MiddleCenter,
                119,
                parent,
                rimrushTextStyle.TournamentAccent);
            bonusNoticeText.transform.SetParent(bonusNoticeRoot.transform, true);
            bonusNoticeRoot.SetActive(false);

            postMatchOverlayRoot = CreateHudRoot("PostMatchOverlayRoot", parent);
            CreatePausePanel("PostMatchShade", rimrushConstants.Width2, ScreenCenterY, 800f, 480f, 128, postMatchOverlayRoot.transform, new Color(0.02f, 0.04f, 0.06f, 0.68f));
            postMatchTopGlow = CreatePausePanel("PostMatchTopGlow", rimrushConstants.Width2, 120f, 760f, 92f, 129, postMatchOverlayRoot.transform, new Color(0.34f, 0.86f, 0.92f, 0.08f));
            postMatchBottomGlow = CreatePausePanel("PostMatchBottomGlow", rimrushConstants.Width2, 358f, 700f, 116f, 129, postMatchOverlayRoot.transform, new Color(1f, 0.72f, 0.34f, 0.06f));
            postMatchCardRoot = CreateHudAnchor("PostMatchCardRoot", rimrushConstants.Width2, PostMatchCardCenterY, postMatchOverlayRoot.transform);
            postMatchCardPanel = CreatePausePanel(
                "PostMatchCardPanel",
                rimrushConstants.Width2,
                PostMatchCardCenterY,
                PostMatchCardWidth,
                PostMatchCardHeight,
                130,
                postMatchCardRoot.transform,
                new Color(0.03f, 0.06f, 0.1f, 0.94f));
            postMatchCardFrame = CreatePauseFrame(
                "PostMatchCardFrame",
                "MatchBack0002",
                rimrushConstants.Width2,
                PostMatchCardCenterY,
                PostMatchCardWidth + 34f,
                PostMatchCardHeight + 16f,
                131,
                postMatchCardRoot.transform,
                new Color(0.86f, 0.96f, 1f, 0.18f));
            CreatePausePanel(
                "PostMatchInnerPanel",
                rimrushConstants.Width2,
                PostMatchCardCenterY,
                PostMatchInnerWidth,
                PostMatchInnerHeight,
                132,
                postMatchCardRoot.transform,
                new Color(0.08f, 0.11f, 0.16f, 0.84f));
            CreatePausePanel(
                "PostMatchTopAccent",
                rimrushConstants.Width2,
                PostMatchSubtitleY - 14f,
                212f,
                2f,
                133,
                postMatchCardRoot.transform,
                new Color(0.35f, 0.88f, 0.93f, 0.28f));
            CreatePausePanel(
                "PostMatchBottomAccent",
                rimrushConstants.Width2,
                PostMatchPromptY - 18f,
                244f,
                2f,
                133,
                postMatchCardRoot.transform,
                new Color(1f, 0.73f, 0.36f, 0.22f));
            postMatchScorePlate = CreatePausePanel(
                "PostMatchScorePlate",
                rimrushConstants.Width2,
                PostMatchScoreY + 2f,
                PostMatchScorePlateWidth,
                PostMatchScorePlateHeight,
                133,
                postMatchCardRoot.transform,
                new Color(0.02f, 0.04f, 0.08f, 0.82f));
            CreatePauseFrame(
                "PostMatchScoreFrame",
                "btn_bg0000",
                rimrushConstants.Width2,
                PostMatchScoreY + 2f,
                PostMatchScorePlateWidth + 24f,
                PostMatchScorePlateHeight + 14f,
                134,
                postMatchCardRoot.transform,
                new Color(0.3f, 0.82f, 0.9f, 0.26f));

            postMatchLeftAura = CreatePortraitAura(
                "PostMatchLeftAura",
                rimrushConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                0.36f,
                132,
                postMatchCardRoot.transform);
            postMatchRightAura = CreatePortraitAura(
                "PostMatchRightAura",
                rimrushConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                0.36f,
                132,
                postMatchCardRoot.transform);
            postMatchLeftPortrait = CreateCharacterPortrait(
                "PostMatchLeftPortrait",
                matchData.CharacterIds[0],
                rimrushConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                PostMatchPortraitPixels,
                135,
                postMatchCardRoot.transform);
            postMatchRightPortrait = CreateCharacterPortrait(
                "PostMatchRightPortrait",
                matchData.CharacterIds[1],
                rimrushConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchPortraitY,
                PostMatchPortraitPixels,
                135,
                postMatchCardRoot.transform);

            postMatchSubtitleText = rimrushRender.Text(
                "PostMatchSubtitle",
                string.Empty,
                rimrushConstants.Width2,
                PostMatchSubtitleY,
                13,
                new Color32(0xCB, 0xD9, 0xE4, 0xFF),
                TextAnchor.MiddleCenter,
                135,
                postMatchCardRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f),
                outlinePixels: 0.52f);
            postMatchTitleText = rimrushRender.Text(
                "PostMatchTitle",
                string.Empty,
                rimrushConstants.Width2,
                PostMatchTitleY,
                30,
                new Color32(0xFF, 0xC7, 0x56, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                rimrushTextStyle.DisplayTitle);
            postMatchScoreText = rimrushRender.Text(
                "PostMatchScore",
                string.Empty,
                rimrushConstants.Width2,
                PostMatchScoreY,
                42,
                new Color32(0xFF, 0xF3, 0xD8, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                rimrushTextStyle.HudScore);
            postMatchWinnerTagText = rimrushRender.Text(
                "PostMatchWinnerTag",
                string.Empty,
                rimrushConstants.Width2,
                PostMatchWinnerTagY,
                12,
                new Color32(0xFF, 0xDE, 0x98, 0xFF),
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.02f, 0.05f, 0.08f, 0.92f),
                outlinePixels: 0.56f);
            postMatchLeftNameText = rimrushRender.Text(
                "PostMatchLeftName",
                leftCharacterLabel,
                rimrushConstants.Width2 - PostMatchPortraitOffsetX,
                PostMatchNameY,
                ResolvePostMatchNameFontSize(leftCharacterLabel),
                Color.white,
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                rimrushTextStyle.HudName);
            postMatchRightNameText = rimrushRender.Text(
                "PostMatchRightName",
                rightCharacterLabel,
                rimrushConstants.Width2 + PostMatchPortraitOffsetX,
                PostMatchNameY,
                ResolvePostMatchNameFontSize(rightCharacterLabel),
                Color.white,
                TextAnchor.MiddleCenter,
                136,
                postMatchCardRoot.transform,
                rimrushTextStyle.HudName);
            CreatePausePanel(
                "PostMatchPromptTint",
                rimrushConstants.Width2,
                PostMatchPromptY,
                216f,
                20f,
                133,
                postMatchCardRoot.transform,
                new Color(0.03f, 0.06f, 0.1f, 0.62f));
            postMatchPromptFrame = CreatePauseFrame(
                "PostMatchPromptFrame",
                "btn_bg0000",
                rimrushConstants.Width2,
                PostMatchPromptY,
                248f,
                34f,
                134,
                postMatchCardRoot.transform,
                new Color(0.3f, 0.82f, 0.9f, 0.34f));
            postMatchPromptText = rimrushRender.Text(
                "PostMatchPrompt",
                string.Empty,
                rimrushConstants.Width2,
                PostMatchPromptY,
                15,
                new Color32(0xEE, 0xF5, 0xD5, 0xFF),
                TextAnchor.MiddleCenter,
                135,
                postMatchCardRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.52f);

            postMatchLeftAuraBaseScale = postMatchLeftAura != null ? postMatchLeftAura.transform.localScale : Vector3.one;
            postMatchRightAuraBaseScale = postMatchRightAura != null ? postMatchRightAura.transform.localScale : Vector3.one;
            postMatchLeftPortraitBaseScale = postMatchLeftPortrait != null ? postMatchLeftPortrait.transform.localScale : Vector3.one;
            postMatchRightPortraitBaseScale = postMatchRightPortrait != null ? postMatchRightPortrait.transform.localScale : Vector3.one;

            pauseOverlayRoot = CreateHudRoot("PauseOverlayRoot", parent);
            pauseShade = CreatePausePanel("PauseShade", rimrushConstants.Width2, ScreenCenterY, 800f, 480f, 140, pauseOverlayRoot.transform, new Color(0.01f, 0.03f, 0.05f, 0.78f));
            CreatePausePanel("PauseTopGlow", rimrushConstants.Width2, 96f, 760f, 104f, 141, pauseOverlayRoot.transform, new Color(0.22f, 0.86f, 0.94f, 0.12f));
            CreatePausePanel("PauseBottomGlow", rimrushConstants.Width2, 388f, 760f, 132f, 141, pauseOverlayRoot.transform, new Color(0.56f, 0.22f, 0.94f, 0.1f));
            pausePanel = CreatePausePanel("PausePanel", rimrushConstants.Width2, ScreenCenterY, 582f, 308f, 142, pauseOverlayRoot.transform, new Color(0.05f, 0.08f, 0.12f, 0.9f));
            CreatePauseFrame("PauseFrame", "MatchBack0002", rimrushConstants.Width2, ScreenCenterY, 632f, 332f, 143, pauseOverlayRoot.transform, new Color(0.9f, 0.98f, 1f, 0.96f));
            CreatePausePanel("PauseBoardTint", rimrushConstants.Width2, PauseBoardY, 206f, 72f, 144, pauseOverlayRoot.transform, new Color(0.02f, 0.04f, 0.09f, 0.4f));
            var pauseBoard = CreateHudImage("PauseBoard", rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Scoreboard), rimrushConstants.Width2, PauseBoardY, 560f, 145, pauseOverlayRoot.transform);
            if (pauseBoard == null)
            {
                CreatePauseFrame("PauseBoardFallback", "btn_bg0000", rimrushConstants.Width2, PauseBoardY, 456f, 150f, 145, pauseOverlayRoot.transform, new Color(0.22f, 0.84f, 0.95f, 0.94f));
            }

            CreatePortraitAura("PauseLeftPortraitAura", rimrushConstants.Width2 - PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, 0.66f, 144, pauseOverlayRoot.transform);
            CreatePortraitAura("PauseRightPortraitAura", rimrushConstants.Width2 + PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, 0.66f, 144, pauseOverlayRoot.transform);
            pauseLeftPortrait = CreateCharacterPortrait("PauseLeftPortrait", matchData.CharacterIds[0], rimrushConstants.Width2 - PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseRightPortrait = CreateCharacterPortrait("PauseRightPortrait", matchData.CharacterIds[1], rimrushConstants.Width2 + PausePortraitOffsetX, PauseBoardY + PausePortraitOffsetY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseTitleText = rimrushRender.Text(
                "PauseTitle",
                "GAME PAUSED",
                rimrushConstants.Width2,
                PauseTitleY,
                28,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushTextStyle.DisplayTitle);
            pauseLeftNameText = rimrushRender.Text(
                "PauseLeftName",
                leftCharacterLabel,
                rimrushConstants.Width2 - PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseRightNameText = rimrushRender.Text(
                "PauseRightName",
                rightCharacterLabel,
                rimrushConstants.Width2 + PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseLeftScoreText = rimrushRender.Text(
                "PauseLeftScore",
                "0",
                rimrushConstants.Width2 - PauseScoreOffsetX,
                PauseScoreY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreDividerText = rimrushRender.Text(
                "PauseScoreDivider",
                ":",
                rimrushConstants.Width2,
                PauseScoreY - 1f,
                32,
                new Color32(0x8F, 0xFF, 0xF8, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushFontKind.CfCrackBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.95f),
                outlinePixels: 1f);
            pauseRightScoreText = rimrushRender.Text(
                "PauseRightScore",
                "0",
                rimrushConstants.Width2 + PauseScoreOffsetX,
                PauseScoreY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                rimrushFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreText = rimrushRender.Text(
                "PauseMeta",
                string.Empty,
                rimrushConstants.Width2,
                PauseMetaY,
                15,
                new Color32(0xCC, 0xF6, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                pauseOverlayRoot.transform,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f),
                outlinePixels: 0.62f);
            pauseMenuButton = new rimrushMenuButton("MENU", PauseMenuButtonX, PauseActionY, PauseMenuButtonWidth, PauseActionButtonHeight, () => pendingPauseCommand = rimrushPauseCommand.Menu, pauseOverlayRoot.transform, 147);
            pauseResumeButton = new rimrushMenuButton("RESUME", PauseResumeButtonX, PauseActionY, PauseResumeButtonWidth, PauseActionButtonHeight, () => pendingPauseCommand = rimrushPauseCommand.Resume, pauseOverlayRoot.transform, 147);

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
        /// Executes Update Score for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="left">Input value used by this step of the workflow.</param>
        /// <param name="right">Input value used by this step of the workflow.</param>
        public void UpdateScore(int left, int right)
        {
            SetText(leftScore, left.ToString());
            SetText(rightScore, right.ToString());
            SetText(pauseLeftScoreText, left.ToString());
            SetText(pauseRightScoreText, right.ToString());
        }

        /// <summary>
        /// Executes Update Timer for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="secondsLeft">Input value used by this step of the workflow.</param>
        public void UpdateTimer(float secondsLeft)
        {
            var timeText = FormatTime(secondsLeft);
            SetText(timerText, timeText);
            if (!isTraining)
            {
                SetText(pauseScoreText, $"TIME FROZEN / {timeText}");
            }
        }

        /// <summary>
        /// Executes Set Timer Visible for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="visible">Input value used by this step of the workflow.</param>
        public void SetTimerVisible(bool visible)
        {
            timerText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Executes Consume Pause Command for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public rimrushPauseCommand ConsumePauseCommand()
        {
            var command = pendingPauseCommand;
            pendingPauseCommand = rimrushPauseCommand.None;
            return command;
        }

        /// <summary>
        /// Executes Toggle Pause Overlay for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Show Pause Overlay for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ShowPauseOverlay()
        {
            HideMessage();
            HideBonusNotice();
            HideCountdown();
            IsPauseOverlayVisible = true;
            SetPauseOverlayVisible(true);
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
        }

        /// <summary>
        /// Executes Hide Pause Overlay for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void HidePauseOverlay()
        {
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// Executes Begin Resume Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void BeginResumeCountdown(float duration)
        {
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
            StartCountdown(duration, "RESUMING IN");
        }

        /// <summary>
        /// Executes End Resume Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void EndResumeCountdown()
        {
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// Executes Show Message for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void ShowMessage(string message, float duration = 1.2f, bool showBackdrop = true)
        {
            if (messageRoot == null)
            {
                SetText(messageText, message);
                messageTime = duration;
                messageDuration = Mathf.Max(0.01f, duration);
                return;
            }

            ApplyMessageTheme(message);
            SetText(messageText, message);
            messageDuration = Mathf.Max(0.01f, duration);
            messageTime = messageDuration;
            messageVisualScale = ResolveMessageScale(message);
            SetGameObjectVisible(messageBackdrop, showBackdrop);
            messageRoot.transform.position = rimrushConstants.PixelToWorldSnapped(rimrushConstants.Width2, PopupCenterY);
            messageRoot.transform.localScale = Vector3.one * (0.78f * messageVisualScale);
            messageRoot.SetActive(true);
        }

        /// <summary>
        /// Executes Show Bonus Notice for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void ShowBonusNotice(string message, float duration = 0.9f)
        {
            if (bonusNoticeRoot == null)
            {
                return;
            }

            ApplyBonusNoticeTheme(message);
            SetText(bonusNoticeText, message);
            bonusNoticeDuration = Mathf.Max(0.01f, duration);
            bonusNoticeTime = bonusNoticeDuration;
            bonusNoticeRoot.transform.position = rimrushConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
            bonusNoticeRoot.transform.localScale = Vector3.one * 0.72f;
            bonusNoticeRoot.SetActive(true);
        }

        /// <summary>
        /// Executes Hide Message for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
                messageRoot.transform.position = rimrushConstants.PixelToWorldSnapped(rimrushConstants.Width2, PopupCenterY);
                messageRoot.SetActive(false);
            }

            SetGameObjectVisible(messageBackdrop, true);
        }

        /// <summary>
        /// Executes Hide Bonus Notice for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void HideBonusNotice()
        {
            SetText(bonusNoticeText, string.Empty);
            bonusNoticeTime = 0f;
            bonusNoticeDuration = 0f;
            if (bonusNoticeRoot != null)
            {
                bonusNoticeRoot.transform.localScale = Vector3.one;
                bonusNoticeRoot.transform.position = rimrushConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
                bonusNoticeRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Executes Hide Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Start Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void StartCountdown(float duration)
        {
            StartCountdown(duration, string.Empty);
        }

        /// <summary>
        /// Executes Start Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        /// <param name="caption">Input value used by this step of the workflow.</param>
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
        /// Executes Update Countdown for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
                    rimrushAudio.Instance?.Play(rimrushAssets.Sounds.MCountdown, 0.8f);
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
        /// Executes Update for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        public void Update(float dt)
        {
            if (rimrushHelpPanel.IsAnyOpen)
            {
                return;
            }

            if (IsPauseOverlayVisible)
            {
                musicButton?.SetActiveIconIndex(GetMusicIconIndex());
                pauseMenuButton.Update(Camera.main);
                pauseResumeButton.Update(Camera.main);
                return;
            }

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
        /// Executes Show Post Match for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="winner">Input value used by this step of the workflow.</param>
        /// <param name="leftScoreValue">Input value used by this step of the workflow.</param>
        /// <param name="rightScoreValue">Input value used by this step of the workflow.</param>
        public void ShowPostMatch(int winner, int leftScoreValue, int rightScoreValue)
        {
            var inventory = rimrushInventory.Instance;
            var isPlayerFacingMode = inventory.IsTournamentActive ||
                                     inventory.IsAdventureActive ||
                                     inventory.GameMode == rimrushGameModeIds.RandomQuick ||
                                     inventory.GameMode == rimrushGameModeIds.QuickMatch ||
                                     inventory.GameMode == rimrushGameModeIds.Tutorial;
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
                postMatchCardRoot.transform.position = rimrushConstants.PixelToWorldSnapped(rimrushConstants.Width2, PostMatchCardCenterY + 12f);
                postMatchCardRoot.transform.localScale = Vector3.one * 0.96f;
            }

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

        private static string GetPostMatchSubtitle(rimrushInventory inventory, int winnerSide)
        {
            if (inventory.IsTournamentActive)
            {
                return rimrushSinglePlayerNarrative.TournamentResultSubtitle;
            }

            if (inventory.IsAdventureActive)
            {
                return winnerSide == -1 ? "LANTERN SIGIL CLAIMED" : "WARDEN GATE HELD";
            }

            return "FINAL SCORE";
        }

        /// <summary>
        /// Executes Hide Post Match for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
                postMatchCardRoot.transform.position = rimrushConstants.PixelToWorldSnapped(rimrushConstants.Width2, PostMatchCardCenterY);
            }

            SetScoreboardVisible(true);
            SetGameObjectVisible(postMatchOverlayRoot, false);
        }

        /// <summary>
        /// Executes Update Post Match Visual for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        private void UpdatePostMatchVisual(float dt)
        {
            postMatchAnimTime += dt;
            var intro = Mathf.Clamp01(postMatchAnimTime / 0.34f);
            var eased = 1f - Mathf.Pow(1f - intro, 3f);
            var pulse = 0.5f + (0.5f * Mathf.Sin(postMatchAnimTime * 2.4f));
            if (postMatchCardRoot != null)
            {
                postMatchCardRoot.transform.position = rimrushConstants.PixelToWorldSnapped(
                    rimrushConstants.Width2,
                    Mathf.Lerp(PostMatchCardCenterY + 12f, PostMatchCardCenterY, eased));
                postMatchCardRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
            }

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
        /// Executes Update Message Visual for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateMessageVisual()
        {
            if (messageRoot == null || !messageRoot.activeSelf)
            {
                return;
            }

            var progress = 1f - Mathf.Clamp01(messageTime / Mathf.Max(0.01f, messageDuration));
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

            if (messageTime < MessageExitWindow)
            {
                scale *= Mathf.Lerp(0.92f, 1f, messageTime / MessageExitWindow);
            }

            var lift = Mathf.Lerp(5f, 0f, Mathf.Clamp01(progress * 1.2f));
            messageRoot.transform.position = rimrushConstants.PixelToWorldSnapped(rimrushConstants.Width2, PopupCenterY - lift);
            messageRoot.transform.localScale = Vector3.one * (scale * messageVisualScale);
        }

        /// <summary>
        /// Executes Update Bonus Notice Visual for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateBonusNoticeVisual()
        {
            if (bonusNoticeRoot == null || !bonusNoticeRoot.activeSelf)
            {
                return;
            }

            var progress = 1f - Mathf.Clamp01(bonusNoticeTime / Mathf.Max(0.01f, bonusNoticeDuration));
            var scale = progress < 0.2f
                ? Mathf.Lerp(0.62f, 0.8f, progress / 0.2f)
                : Mathf.Lerp(0.8f, 0.72f, (progress - 0.2f) / 0.8f);
            var drift = Mathf.Lerp(4f, 0f, Mathf.Clamp01(progress * 1.1f));
            bonusNoticeRoot.transform.position = rimrushConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY - drift);
            bonusNoticeRoot.transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Executes Update Countdown Visual for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateCountdownVisual()
        {
            var progress = 1f - Mathf.Clamp01(countdownPulseTime / CountdownPulseDuration);
            float scale;
            if (progress < 0.24f)
            {
                scale = Mathf.Lerp(0.72f, 1.16f, progress / 0.24f);
            }
            else
            {
                scale = Mathf.Lerp(1.16f, 1f, (progress - 0.24f) / 0.76f);
            }

            countdownText.transform.localScale = countdownBaseScale * scale;
        }

        /// <summary>
        /// Executes Apply Message Theme for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        private void ApplyMessageTheme(string message)
        {
            switch (message)
            {
                case "3 POINT":
                    messageText.color = new Color32(0xFF, 0x98, 0x10, 0xFF);
                    break;
                case "BASKET":
                    messageText.color = new Color32(0xFF, 0xC5, 0x57, 0xFF);
                    break;
                case "GO!!!":
                    messageText.color = new Color32(0x9C, 0xFF, 0x4A, 0xFF);
                    break;
                case "TIME!!!":
                    messageText.color = new Color32(0xFF, 0xBA, 0x40, 0xFF);
                    break;
                case "OVERTIME":
                    messageText.color = new Color32(0x42, 0xFF, 0xEA, 0xFF);
                    break;
                case "HELL DASH!":
                    messageText.color = new Color32(0xFF, 0x62, 0x32, 0xFF);
                    break;
                case "HELL SHIELD!":
                    messageText.color = new Color32(0xFF, 0x52, 0x92, 0xFF);
                    break;
                default:
                    messageText.color = new Color32(0x8B, 0x2D, 0xFF, 0xFF);
                    break;
            }
        }

        /// <summary>
        /// Executes Apply Bonus Notice Theme for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
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
        /// Executes Resolve Message Scale for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Resolve Post Match Name Font Size for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterName">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Create Pause Panel for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="tint">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreatePausePanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var standalonePanel = TryCreateStandaloneTintPanel(name, x, y, width, height, sortingOrder, parent, tint);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / 10f,
                rimrushConstants.UnitsPerPixel * height / 10f,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        /// <summary>
        /// Executes Create Pause Frame for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="frame">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="tint">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreatePauseFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var standalonePanel = TryCreateStandalonePauseFrame(name, frame, x, y, width, height, sortingOrder, parent, tint);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = rimrushAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * width / sourceWidth,
                    rimrushConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject TryCreateStandaloneTintPanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.PanelFillSoft));
            if (texture == null)
            {
                return null;
            }

            var panel = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
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

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            var panel = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static string ResolveStandalonePauseFrameImage(string frame)
        {
            return frame switch
            {
                "MatchBack0001" => rimrushAssets.Images.Ui.FrameMatchCardIdle,
                "MatchBack0002" => rimrushAssets.Images.Ui.FrameMatchCardActive,
                "btn_bg0000" => rimrushAssets.Images.Ui.MenuButtonPlate,
                _ => null
            };
        }

        /// <summary>
        /// Executes Set Game Object Visible for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="target">Input value used by this step of the workflow.</param>
        /// <param name="visible">Input value used by this step of the workflow.</param>
        private static void SetGameObjectVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// Executes Set Text for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="target">Input value used by this step of the workflow.</param>
        /// <param name="value">Input value used by this step of the workflow.</param>
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
        /// Executes Set Pause Overlay Visible for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="visible">Input value used by this step of the workflow.</param>
        private void SetPauseOverlayVisible(bool visible)
        {
            SetGameObjectVisible(pauseOverlayRoot, visible);
            pauseMenuButton.SetVisible(visible);
            pauseResumeButton.SetVisible(visible);
        }

        /// <summary>
        /// Executes Set Scoreboard Visible for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="visible">Input value used by this step of the workflow.</param>
        private void SetScoreboardVisible(bool visible)
        {
            SetGameObjectVisible(scoreboardRoot, visible && !isTutorial);
        }

        /// <summary>
        /// Executes Create Pause Button Icon for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private GameObject CreatePauseButtonIcon(Transform parent)
        {
            var resourcePath = rimrushAssets.Images.ResourcePath(rimrushAssets.Images.PauseButton);
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return rimrushIconButton.CreateImageIcon("PauseButtonIcon", resourcePath, PauseButtonX, TopRightButtonY, 82, TopRightIconPixels, parent);
            }

            var icon = rimrushRender.Sprite("PauseButtonIconFallback", rimrushAtlasCache.Instance.Gameplay, "InGamePauseButton0000", PauseButtonX, TopRightButtonY, 0.5f, 0.5f, 82, parent);
            icon.transform.localScale *= 1.2f;
            return icon;
        }

        /// <summary>
        /// Executes Toggle Background Music for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            rimrushAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// Executes No Op Action for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// Executes Get Music Icon Index for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static int GetMusicIconIndex()
        {
            return rimrushAudio.Instance != null && rimrushAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// Executes Create Scoreboard Backdrop for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        private static void CreateScoreboardBackdrop(Transform parent)
        {
            var image = CreateHudImage("ScoreboardBackdrop", rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Scoreboard), ScoreboardCenterX, ScoreboardCenterY, ScoreboardTargetWidth, 80, parent);
            if (image != null)
            {
                return;
            }

            rimrushRender.Sprite("InfoPanel", rimrushAtlasCache.Instance.Gameplay, "infoPanel0000", rimrushConstants.Width2, 60f, 0.5f, 0.5f, 80, parent);
        }

        /// <summary>
        /// Executes Create Popup Backdrop for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreatePopupBackdrop(Transform parent)
        {
            // The popup frame has been retired from the HUD. Message text now
            // renders on its own so this artwork never appears again.
            return null;
        }

        /// Executes Create Hud Image for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="resourcePath">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="targetWidth">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreateHudImage(string name, string resourcePath, float x, float y, float targetWidth, int sortingOrder, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var image = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            image.transform.localScale *= targetWidth / Mathf.Max(1f, texture.width);
            return image;
        }

        /// <summary>
        /// Executes Create Hud Root for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Create Hud Anchor for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreateHudAnchor(string name, float x, float y, Transform parent)
        {
            var anchor = new GameObject(name);
            if (parent != null)
            {
                anchor.transform.SetParent(parent, false);
            }

            anchor.transform.position = rimrushConstants.PixelToWorldSnapped(x, y);
            return anchor;
        }

        /// <summary>
        /// Executes Create Portrait Aura for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="scale">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreatePortraitAura(string name, float x, float y, float scale, int sortingOrder, Transform parent)
        {
            const float legacyAuraPixels = 150f;
            var diameterPixels = legacyAuraPixels * scale;
            return rimrushRender.PortraitBackplate(
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
        /// Executes Create Character Portrait for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="targetPixels">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static GameObject CreateCharacterPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            var targetSize = targetPixels * rimrushPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var sprite = rimrushPlayersData.GetCharacterPortraitSprite(characterId, targetSize);
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
            var adjustedY = y + rimrushPlayersData.GetCharacterPortraitOffsetY(characterId, sprite) * scale;
            rimrushRender.ApplyPixelTransform(portrait.transform, x, adjustedY, 0f, scale);
            return portrait;
        }

        /// <summary>
        /// Executes Set Sprite Tint for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="target">Input value used by this step of the workflow.</param>
        /// <param name="tint">Input value used by this step of the workflow.</param>
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
        /// Executes Format Time for the rimrushHudView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="secondsLeft">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

    public sealed class rimrushMenuButton
    {
        private Rect rect;
        private readonly System.Action action;
        private readonly GameObject sprite;
        private readonly TextMesh label;
        private readonly TMP_Text nativeLabel;
        private readonly Transform labelTransform;
        private readonly Vector3 baseScale;
        private readonly Vector3 labelBaseScale;
        private bool visible = true;
        private bool backgroundVisible = true;
        private bool labelVisible = true;
        private bool pressed;
        public GameObject Root => sprite;

        /// <summary>
        /// Executes rimrush Menu Button for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="text">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="action">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        public rimrushMenuButton(
            string text,
            float x,
            float y,
            float width,
            float height,
            System.Action action,
            Transform parent,
            int sortingOrder = 50,
            rimrushTextStyle labelStyle = rimrushTextStyle.ButtonLabel)
        {
            this.action = action;
            rect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
            var buttonTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.MenuButtonPlate));
            float sourceWidth;
            float sourceHeight;
            if (buttonTexture != null)
            {
                sprite = rimrushRender.Image($"Button_{text}", buttonTexture, x, y, 0.5f, 0.5f, sortingOrder, parent);
                sourceWidth = Mathf.Max(1f, buttonTexture.width);
                sourceHeight = Mathf.Max(1f, buttonTexture.height);
            }
            else
            {
                sprite = rimrushRender.Sprite($"Button_{text}", rimrushAtlasCache.Instance.Interface, "btn_bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
                var frame = rimrushAtlasCache.Instance.Interface.Frame("btn_bg0000");
                sourceWidth = frame != null ? Mathf.Max(1f, frame.W) : 1f;
                sourceHeight = frame != null ? Mathf.Max(1f, frame.H) : 1f;
            }

            sprite.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / sourceWidth,
                rimrushConstants.UnitsPerPixel * height / sourceHeight,
                1f);

            baseScale = sprite.transform.localScale;
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.55f), 18, 32);
            if (rimrushNativeMenuTextLayer.Active != null && rimrushNativeMenuTextLayer.Active.Owns(parent))
            {
                nativeLabel = rimrushNativeMenuTextLayer.Active.CreateText(
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
                label = rimrushRender.Text(
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

            labelBaseScale = labelTransform != null ? labelTransform.localScale : Vector3.one;
        }

        /// <summary>
        /// Executes Update for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="camera">Input value used by this step of the workflow.</param>
        public void Update(Camera camera)
        {
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

            var mouse = Input.mousePosition;
            Vector2 pixel;
            bool inside;
            if (rimrushFixedResolutionPresenter.HasActivePresenter)
            {
                inside = rimrushFixedResolutionPresenter.TryMapScreenToGamePixel(mouse, out pixel) && rect.Contains(pixel);
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
                    pixel = rimrushConstants.WorldToPixel(world);
                    inside = rect.Contains(pixel);
                }
            }

            sprite.transform.localScale = inside ? baseScale * 1.035f : baseScale;
            if (labelTransform != null)
            {
                labelTransform.localScale = inside ? labelBaseScale * 1.035f : labelBaseScale;
            }

            var labelColor = inside ? new Color(1f, 0.92f, 0.25f) : Color.white;
            if (label != null)
            {
                label.color = labelColor;
            }
            else if (nativeLabel != null)
            {
                nativeLabel.color = labelColor;
            }

            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            if (pressed && Input.GetMouseButtonUp(0))
            {
                pressed = false;
                if (inside)
                {
                    rimrushAudio.Instance?.Play(rimrushAssets.Sounds.Button);
                    action?.Invoke();
                }
            }
        }

        /// <summary>
        /// Executes Set Text for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="text">Input value used by this step of the workflow.</param>
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
        /// Executes Set Visible for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="isVisible">Input value used by this step of the workflow.</param>
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
        /// Executes Set Background Visible for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="isVisible">Input value used by this step of the workflow.</param>
        public void SetBackgroundVisible(bool isVisible)
        {
            backgroundVisible = isVisible;
            sprite.SetActive(visible && backgroundVisible);
        }

        /// <summary>
        /// Executes Set Label Visible for the rimrushMenuButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="isVisible">Input value used by this step of the workflow.</param>
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
                rimrushNativeMenuTextLayer.SetPixelPosition(rectTransform, x, y);
                return;
            }

            transform.position = rimrushConstants.PixelToWorldSnapped(x, y, transform.position.z);
        }
    }

    public sealed class rimrushIconButton
    {
        private readonly rimrushMenuButton button;
        private readonly GameObject[] icons;
        private bool visible = true;
        private int activeIconIndex;

        /// <summary>
        /// Executes rimrush Icon Button for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <param name="action">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="targetPixels">Input value used by this step of the workflow.</param>
        /// <param name="resourcePaths">Input value used by this step of the workflow.</param>
        public rimrushIconButton(
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
            button = new rimrushMenuButton(string.Empty, x, y, width, height, action, parent);
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
        /// Executes Update for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="camera">Input value used by this step of the workflow.</param>
        public void Update(Camera camera)
        {
            button.Update(camera);
        }

        /// <summary>
        /// Executes Set Visible for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="isVisible">Input value used by this step of the workflow.</param>
        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            button.SetVisible(isVisible);
            RefreshIcons();
        }

        /// <summary>
        /// Executes Set Active Icon Index for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="iconIndex">Input value used by this step of the workflow.</param>
        public void SetActiveIconIndex(int iconIndex)
        {
            activeIconIndex = Mathf.Clamp(iconIndex, 0, Mathf.Max(0, icons.Length - 1));
            RefreshIcons();
        }

        /// <summary>
        /// Executes Create Image Icon for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="resourcePath">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="targetPixels">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static GameObject CreateImageIcon(string name, string resourcePath, float x, float y, int sortingOrder, float targetPixels, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var icon = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var sourcePixels = Mathf.Max(texture.width, texture.height);
            icon.transform.localScale *= targetPixels / Mathf.Max(1f, sourcePixels);
            return icon;
        }

        /// <summary>
        /// Executes Refresh Icons for the rimrushIconButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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

    public sealed class rimrushEnergyBarView
    {
        private const float SoloEnergyX = 45f;
        private const float PlayerOneEnergyX = 45f;
        private const float PlayerTwoEnergyX = 706f;
        private const float EnergyY = 45f;
        private const float PlayerTwoEnergyY = 126f;
        private readonly rimrushRadialIconMesh overlay;

        /// <summary>
        /// Executes rimrush Energy Bar View for the rimrushEnergyBarView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="controllerSlot">Input value used by this step of the workflow.</param>
        /// <param name="skillDefinition">Input value used by this step of the workflow.</param>
        /// <param name="fullTime">Input value used by this step of the workflow.</param>
        public rimrushEnergyBarView(Transform parent, int controllerSlot, rimrushCharacterSkillDefinition skillDefinition, float fullTime)
        {
            var profile = rimrushControlsData.ProfileForSlot(controllerSlot);
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

            const float legacyEnergyBgWidth = 95f;
            const float legacyEnergyBgHeight = 89f;
            const float standaloneEnergyIconPixels = 76f;
            var energyTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.EnergyButtonPlate));
            GameObject bg;
            if (energyTexture != null)
            {
                bg = rimrushRender.Image($"EnergyBg_{controllerSlot}", energyTexture, x, y + 1f, 0.5f, 0.5f, 83, parent);
                bg.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * legacyEnergyBgWidth * 1.1f / Mathf.Max(1f, energyTexture.width),
                    rimrushConstants.UnitsPerPixel * legacyEnergyBgHeight * 1.1f / Mathf.Max(1f, energyTexture.height),
                    1f);
            }
            else
            {
                bg = rimrushRender.Sprite($"EnergyBg_{controllerSlot}", rimrushAtlasCache.Instance.Gameplay, "btn_bg20000", x, y + 1f, 0.5f, 0.5f, 83, parent);
                bg.transform.localScale *= 1.1f;
            }

            var baseResourcePath = skillDefinition.HasStandaloneIconArt
                ? rimrushAssets.Images.ResourcePath(skillDefinition.IconImageKey)
                : null;
            var maskResourcePath = skillDefinition.HasStandaloneIconArt
                ? rimrushAssets.Images.ResourcePath(skillDefinition.ChargeMaskImageKey)
                : null;
            var baseTexture = !string.IsNullOrEmpty(baseResourcePath) ? Resources.Load<Texture2D>(baseResourcePath) : null;
            var maskTexture = !string.IsNullOrEmpty(maskResourcePath) ? Resources.Load<Texture2D>(maskResourcePath) : null;

            if (baseTexture != null && maskTexture != null)
            {
                rimrushIconButton.CreateImageIcon($"EnergyBase_{controllerSlot}", baseResourcePath, x, y, 84, standaloneEnergyIconPixels, parent);
                overlay = new rimrushRadialIconMesh($"EnergyFill_{controllerSlot}", maskTexture, x, y, 85, parent, standaloneEnergyIconPixels);
            }
            else
            {
                var superId = skillDefinition.IconSuperId;
                rimrushRender.Sprite($"EnergyBase_{controllerSlot}", rimrushAtlasCache.Instance.Interface, $"icon_ball000{superId}", x, y, 0.5f, 0.5f, 84, parent);
                overlay = new rimrushRadialIconMesh($"EnergyFill_{controllerSlot}", rimrushAtlasCache.Instance.Interface, $"icon_ball2000{superId}", x, y, 85, parent);
            }

            rimrushRender.Sprite($"EnergyHintBg_{controllerSlot}", rimrushAtlasCache.Instance.Gameplay, "key_hint0000", x - 30f, y + 30f, 0.5f, 0.5f, 86, parent);
            rimrushRender.Text(
                $"EnergyHint_{controllerSlot}",
                profile.SuperHint,
                x - 30f,
                y + 32f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                87,
                parent,
                rimrushTextStyle.TournamentBody);

            SetCharge(fullTime <= 0f ? 1f : 0f);
        }

        /// <summary>
        /// Executes Set Charge for the rimrushEnergyBarView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="progress">Input value used by this step of the workflow.</param>
        public void SetCharge(float progress)
        {
            overlay.SetProgress(progress);
        }

        /// <summary>
        /// Executes Release Runtime Resources for the rimrushEnergyBarView workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ReleaseRuntimeResources()
        {
            overlay?.ReleaseRuntimeResources();
        }
    }

    public sealed class rimrushRadialIconMesh
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
        /// Executes rimrush Radial Icon Mesh for the rimrushRadialIconMesh workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="atlas">Input value used by this step of the workflow.</param>
        /// <param name="frameName">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushRadialIconMesh(string name, rimrushAtlas atlas, string frameName, float x, float y, int sortingOrder, Transform parent)
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

            renderer.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault(sprite.texture);
            renderer.sortingOrder = sortingOrder;

            width = frame.W;
            height = frame.H;
            var rect = sprite.rect;
            uvMin = new Vector2(rect.xMin / sprite.texture.width, rect.yMin / sprite.texture.height);
            uvMax = new Vector2(rect.xMax / sprite.texture.width, rect.yMax / sprite.texture.height);

            rimrushRender.ApplyPixelTransform(graphic.transform, x, y, 0.13f, 1f);
            SetProgress(0f);
        }

        /// <summary>
        /// Executes rimrush Radial Icon Mesh for the standalone texture workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="texture">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="targetPixels">Input value used by this step of the workflow.</param>
        public rimrushRadialIconMesh(string name, Texture2D texture, float x, float y, int sortingOrder, Transform parent, float targetPixels)
        {
            graphic = new GameObject(name);
            graphic.transform.SetParent(parent, false);

            var filter = graphic.AddComponent<MeshFilter>();
            var renderer = graphic.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = $"{name}_Mesh" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;

            renderer.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault(texture);
            renderer.sortingOrder = sortingOrder;

            var sourcePixels = Mathf.Max(1f, Mathf.Max(texture.width, texture.height));
            width = targetPixels * texture.width / sourcePixels;
            height = targetPixels * texture.height / sourcePixels;
            uvMin = Vector2.zero;
            uvMax = Vector2.one;

            rimrushRender.ApplyPixelTransform(graphic.transform, x, y, 0.13f, 1f);
            SetProgress(0f);
        }

        /// <summary>
        /// Executes Set Progress for the rimrushRadialIconMesh workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="progress">Input value used by this step of the workflow.</param>
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
        /// Executes Build Sector for the rimrushRadialIconMesh workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="degrees">Input value used by this step of the workflow.</param>
        private void BuildSector(float degrees)
        {
            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(RadialSteps * Mathf.Clamp01(degrees / 360f)));
            var radius = Mathf.Min(width, height) * 0.5f;
            var vertices = new Vector3[segmentCount + 2];
            var uvs = new Vector2[segmentCount + 2];
            var triangles = new int[segmentCount * 3];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f);

            for (var i = 0; i <= segmentCount; i++)
            {
                var t = segmentCount == 0 ? 0f : i / (float)segmentCount;
                var angle = (90f - degrees * t) * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertices[i + 1] = new Vector3(point.x, point.y, 0f);
                var uvX = Mathf.Lerp(uvMin.x, uvMax.x, point.x / width + 0.5f);
                var uvY = Mathf.Lerp(uvMin.y, uvMax.y, point.y / height + 0.5f);
                uvs[i + 1] = new Vector2(uvX, uvY);
                if (i == segmentCount)
                {
                    continue;
                }

                var tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Executes Release Runtime Resources for the rimrushRadialIconMesh workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
