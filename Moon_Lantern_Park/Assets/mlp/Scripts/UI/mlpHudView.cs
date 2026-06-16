// In-match HUD interface (heads-up display)

// Including scoreboard, timer, pause menu, post-match settlement screen, countdown and various pop-up prompts.

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>Pause command type: no operation, switch pause, resume game, return to menu. </summary>
    public enum mlpPauseCommand
    {
        None,
        Toggle,
        Resume,
        Menu
    }

    /// <summary>
    /// Game HUD Interface: Manage all heads-up display elements in the game - scoreboard, timer, pause menu, post-game settlement, countdown and various pop-up prompts.

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
        /// Build the entire HUD interface: scoreboard, timeout overlay, post-match results card, countdown and pop-up messages.
        /// </summary>
        public mlpHudView(Transform parent, mlpMatchData matchData)
        {
            // 1. Determine the current game mode (tutorial, training, normal competition) and character name

            var gameMode = mlpInventory.Instance.GameMode;
            isTutorial = gameMode == mlpGameModeIds.Tutorial;
            isTraining = gameMode == mlpGameModeIds.Training || gameMode == mlpGameModeIds.Tutorial;
            leftCharacterLabel = mlpPlayersData.GetCharacterName(matchData.CharacterIds[0]);
            rightCharacterLabel = mlpPlayersData.GetCharacterName(matchData.CharacterIds[1]);

            // 2. Create a scoreboard (background image + left and right character avatars + name + score + timer)

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

            // 3. Create function buttons in the upper right corner (pause, music switch, help)

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

            // 4. Create a countdown display (digital pulse animation + title text, such as "RESUMING IN")

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

            // 5. Create pop-up messages in the center of the screen (e.g. "GO!!!", "BASKET", "3 POINT")

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

            // 6. Create a bonus point prompt in the upper right corner (such as "HELL DASH!")
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

            // 7. Create post-match result card (dark mask + card panel + character avatar + score + winner label + prompt text)

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

            // 8. Create pause screen (semi-transparent mask + panel + character avatar + score + menu/resume button)

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
        /// Updated score numbers on the scoreboard and timeout screen.

        /// </summary>
        public void UpdateScore(int left, int right)
        {
            SetText(leftScore, left.ToString());
            SetText(rightScore, right.ToString());
            SetText(pauseLeftScoreText, left.ToString());
            SetText(pauseRightScoreText, right.ToString());
        }

        /// <summary>
        /// Update timer display and display freeze time on pause screen.

        /// </summary>
        public void UpdateTimer(float secondsLeft)
        {
            // 1. Format remaining seconds as display text of "1:00" or "04.2"

            var timeText = FormatTime(secondsLeft);
            // 2. Update the timer text on the scoreboard

            SetText(timerText, timeText);
            // 3. If it is not training mode, the freeze time display on the pause screen will be updated simultaneously.

            if (!isTraining)
            {
                SetText(pauseScoreText, $"TIME FROZEN / {timeText}");
            }
        }

        /// <summary>
        /// Show or hide the match timer.

        /// </summary>
        public void SetTimerVisible(bool visible)
        {
            timerText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Read and clear pending pause commands (toggle, resume or return to menu).

        /// </summary>
        public mlpPauseCommand ConsumePauseCommand()
        {
            var command = pendingPauseCommand;
            pendingPauseCommand = mlpPauseCommand.None;
            return command;
        }

        /// <summary>
        /// Toggles the display or hiding of the pause screen.

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
        /// Shows the pause screen and hides other HUD elements.

        /// </summary>
        public void ShowPauseOverlay()
        {
            // 1. Hide on-screen pop-up messages, extra score tips, and countdowns (you don’t need these when paused)
            HideMessage();
            HideBonusNotice();
            HideCountdown();
            // 2. Mark the pause state and display the pause screen (mask + panel + button)

            IsPauseOverlayVisible = true;
            SetPauseOverlayVisible(true);
            // 3. Hide the pause, music and help buttons in the upper right corner (the pause screen has its own button)

            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
        }

        /// <summary>
        /// Hide the pause screen and restore the upper right button.

        /// </summary>
        public void HidePauseOverlay()
        {
            // 1. Clear pause status and hide pause screen

            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            // 2. Redisplay the pause, music and help buttons in the upper right corner

            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// Hide the pause screen and start the "Resume soon" countdown.

        /// </summary>
        public void BeginResumeCountdown(float duration)
        {
            // 1. Clear pause status and hide pause screen

            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            // 2. Hide all buttons in the upper right corner (no operations allowed during countdown)

            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
            musicButton?.SetVisible(false);
            helpButton?.SetVisible(false);
            // 3. Start the 3-2-1 countdown, and the title will display "RESUMING IN"

            StartCountdown(duration, "RESUMING IN");
        }

        /// <summary>
        /// The button in the upper right corner will reappear after the recovery countdown is over.

        /// </summary>
        public void EndResumeCountdown()
        {
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        /// <summary>
        /// Display a large pop-up message in the center of the screen (e.g. "GO!!!", "BASKET").

        /// </summary>
        public void ShowMessage(string message, float duration = 1.2f, bool showBackdrop = true)
        {
            // 1. If the message root node does not exist (UI is not built), only set text and timer

            if (messageRoot == null)
            {
                SetText(messageText, message);
                messageTime = duration;
                messageDuration = Mathf.Max(0.01f, duration);
                return;
            }

            // 2. Choose the appropriate text color according to the message content (for example, "GO!!!" is green, "BASKET" is gold)

            ApplyMessageTheme(message);
            // 3. Set message text and duration

            SetText(messageText, message);
            messageDuration = Mathf.Max(0.01f, duration);
            messageTime = messageDuration;
            // 4. Reduce long text appropriately to prevent it from exceeding the screen.

            messageVisualScale = ResolveMessageScale(message);
            // 5. Set whether the background is displayed, position it in the center of the screen, and display it with a small initial zoom (the pop-in animation will be played in subsequent frames)

            SetGameObjectVisible(messageBackdrop, showBackdrop);
            messageRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PopupCenterY);
            messageRoot.transform.localScale = Vector3.one * (0.78f * messageVisualScale);
            messageRoot.SetActive(true);
        }

        /// <summary>
        /// Display a small bonus point prompt in the upper right corner (e.g. "HELL DASH!").

        /// </summary>
        public void ShowBonusNotice(string message, float duration = 0.9f)
        {
            // 1. If the root node of the bonus point tip does not exist, return directly

            if (bonusNoticeRoot == null)
            {
                return;
            }

            // 2. Choose the appropriate text color according to the message content

            ApplyBonusNoticeTheme(message);
            // 3. Set text content and display duration

            SetText(bonusNoticeText, message);
            bonusNoticeDuration = Mathf.Max(0.01f, duration);
            bonusNoticeTime = bonusNoticeDuration;
            // 4. Locate to the specified position in the upper right corner and display it with a smaller initial zoom (the pop-in animation will be played in subsequent frames)

            bonusNoticeRoot.transform.position = mlpConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
            bonusNoticeRoot.transform.localScale = Vector3.one * 0.72f;
            bonusNoticeRoot.SetActive(true);
        }

        /// <summary>
        /// Hide pop-up messages.

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
        /// Hide extra points tips.

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
        /// Hide the countdown display and reset its state.

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
        /// Start countdown of untitled text.
        /// </summary>
        public void StartCountdown(float duration)
        {
            StartCountdown(duration, string.Empty);
        }

        /// <summary>
        /// Starts a countdown with optional title text displayed above the number.
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
        /// Countdown is updated every frame. Returns true while the countdown is still running.
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
        /// Main update loop: handles frame-by-frame updates for buttons, messages, bonus point prompts, countdowns, and post-match animations.
        /// </summary>
        public void Update(float dt)
        {
            // 1. If the help panel is open, pause all updates to the HUD

            if (mlpHelpPanel.IsAnyOpen)
            {
                return;
            }

            // 2. If the pause screen is visible, only the buttons and music icons on the pause screen are updated.

            if (IsPauseOverlayVisible)
            {
                musicButton?.SetActiveIconIndex(GetMusicIconIndex());
                pauseMenuButton.Update(Camera.main);
                pauseResumeButton.Update(Camera.main);
                return;
            }

            // 3. Update mouseover/click detection for pause, music and help buttons in the upper right corner
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
        /// Displays the post-match result card, including the winner, score, and character portrait.

        /// </summary>
        public void ShowPostMatch(int winner, int leftScoreValue, int rightScoreValue)
        {
            // 1. Obtain game archive information and determine which game mode is currently (tournament, adventure, quick match, etc.)

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

            // 2. Set the title text color (warm/gold for victory, cool/silver for failure)

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

            // 3. Hide the scoreboard and the button in the upper right corner during the game and display the post-match result card

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
        /// Hide the post-match results card and restore the scoreboard.

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
        /// Play the post-game card entry sliding animation and winner avatar pulse effect.
        /// </summary>
        private void UpdatePostMatchVisual(float dt)
        {
            // 1. Accumulate animation time, calculate entry progress (0→1) and continuous pulse value

            postMatchAnimTime += dt;
            var intro = Mathf.Clamp01(postMatchAnimTime / 0.34f);
            var eased = 1f - Mathf.Pow(1f - intro, 3f);
            var pulse = 0.5f + (0.5f * Mathf.Sin(postMatchAnimTime * 2.4f));
            // 2. Slide the card from a slightly lower position to the final position, while simultaneously zooming in from 0.96x to 1x

            if (postMatchCardRoot != null)
            {
                postMatchCardRoot.transform.position = mlpConstants.PixelToWorldSnapped(
                    mlpConstants.Width2,
                    Mathf.Lerp(PostMatchCardCenterY + 12f, PostMatchCardCenterY, eased));
                postMatchCardRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
            }

            // 3. The winner's avatar and halo continue to pulse and enlarge, while the loser remains in a reduced state.

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

            // 4. The prompt text and luminous effect follow the change of pulse value to create a breathing light effect.

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
        /// Play the pop-up zoom animation of the pop-up message and drift upward slightly.

        /// </summary>
        private void UpdateMessageVisual()
        {
            // 1. If the message root node does not exist or is not activated, skip the animation

            if (messageRoot == null || !messageRoot.activeSelf)
            {
                return;
            }

            // 2. Calculate animation progress (0 = just appeared, 1 = about to disappear)

            var progress = 1f - Mathf.Clamp01(messageTime / Mathf.Max(0.01f, messageDuration));
            // 3. Pop-in animation: first zoom in from 0.78 to 1.08 (overshoot), then bounce back to 1.0

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

            // 4. Slightly shrink when it is about to disappear to create a sense of exit.

            if (messageTime < MessageExitWindow)
            {
                scale *= Mathf.Lerp(0.92f, 1f, messageTime / MessageExitWindow);
            }

            // 5. When the message appears, it will float slightly from the bottom and then stop in the center.

            var lift = Mathf.Lerp(5f, 0f, Mathf.Clamp01(progress * 1.2f));
            messageRoot.transform.position = mlpConstants.PixelToWorldSnapped(mlpConstants.Width2, PopupCenterY - lift);
            messageRoot.transform.localScale = Vector3.one * (scale * messageVisualScale);
        }

        /// <summary>
        /// The zoom that plays the bonus point prompt enters the animation and slowly drifts upward.

        /// </summary>
        private void UpdateBonusNoticeVisual()
        {
            // 1. If the extra point prompt root node does not exist or is not activated, skip the animation.
            if (bonusNoticeRoot == null || !bonusNoticeRoot.activeSelf)
            {
                return;
            }

            // 2. Calculate animation progress (0 = just appeared, 1 = about to disappear)

            var progress = 1f - Mathf.Clamp01(bonusNoticeTime / Mathf.Max(0.01f, bonusNoticeDuration));
            // 3. Zoom animation: first bounce from 0.62 to 0.8, and then slowly shrink to 0.72

            var scale = progress < 0.2f
                ? Mathf.Lerp(0.62f, 0.8f, progress / 0.2f)
                : Mathf.Lerp(0.8f, 0.72f, (progress - 0.2f) / 0.8f);
            // 4. Drift upward slowly to create a sense of lightness

            var drift = Mathf.Lerp(4f, 0f, Mathf.Clamp01(progress * 1.1f));
            bonusNoticeRoot.transform.position = mlpConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY - drift);
            bonusNoticeRoot.transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Play the pulse (first zoom in and then zoom out) animation effect of the countdown number.

        /// </summary>
        private void UpdateCountdownVisual()
        {
            // 1. Calculate pulse animation progress (0 = just triggered, 1 = animation end)

            var progress = 1f - Mathf.Clamp01(countdownPulseTime / CountdownPulseDuration);
            // 2. First quickly zoom in from 0.72 to 1.16 (pop-up feeling), and then slowly return to 1.0 (stable)

            float scale;
            if (progress < 0.24f)
            {
                scale = Mathf.Lerp(0.72f, 1.16f, progress / 0.24f);
            }
            else
            {
                scale = Mathf.Lerp(1.16f, 1f, (progress - 0.24f) / 0.76f);
            }

            // 3. Apply zoom to the countdown text

            countdownText.transform.localScale = countdownBaseScale * scale;
        }

        /// <summary>
        /// Choose the text color based on the content of the pop-up message (e.g. "GO!!!" is green).

        /// </summary>
        private void ApplyMessageTheme(string message)
        {
            // 1. Match different theme colors according to message content

            switch (message)
            {
                // 2. Three-pointers: Orange

                case "3 POINT":
                    messageText.color = new Color32(0xFF, 0x98, 0x10, 0xFF);
                    break;
                // 3. Field goal: gold

                case "BASKET":
                    messageText.color = new Color32(0xFF, 0xC5, 0x57, 0xFF);
                    break;
                // 4. Game starts: green

                case "GO!!!":
                    messageText.color = new Color32(0x9C, 0xFF, 0x4A, 0xFF);
                    break;
                // 5. Time’s up: warm yellow
                case "TIME!!!":
                    messageText.color = new Color32(0xFF, 0xBA, 0x40, 0xFF);
                    break;
                // 6. Overtime: Cyan

                case "OVERTIME":
                    messageText.color = new Color32(0x42, 0xFF, 0xEA, 0xFF);
                    break;
                // 7. Hell Rush: Orange-Red

                case "HELL DASH!":
                    messageText.color = new Color32(0xFF, 0x62, 0x32, 0xFF);
                    break;
                // 8. Hell Shield: Pink
                case "HELL SHIELD!":
                    messageText.color = new Color32(0xFF, 0x52, 0x92, 0xFF);
                    break;
                case "FOG WIND ACTIVE":
                    messageText.color = new Color32(0xA8, 0xF7, 0xFF, 0xFF);
                    break;
                // 9. Other messages: purple (default)
                default:
                    messageText.color = new Color32(0x8B, 0x2D, 0xFF, 0xFF);
                    break;
            }
        }

        /// <summary>
        /// Choose the text color according to the content of the extra points prompt.
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
        /// Shrink longer messages to fit the screen. Returns the zoom factor.
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
        /// Choose a smaller font size for the longer character names on the post-game cards.

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
        /// Create a solid color rectangular panel for pause/post-game background.

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
        /// Create a bordered panel for pause/post-game card outlines.

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
        /// Safely show or hide the GameObject (does nothing if the object is empty).

        /// </summary>
        private static void SetGameObjectVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// Update the TextMesh tag and request a font texture refresh.

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
        /// Show or hide the entire pause overlay, including the menu and resume button.
        /// </summary>
        private void SetPauseOverlayVisible(bool visible)
        {
            SetGameObjectVisible(pauseOverlayRoot, visible);
            pauseMenuButton.SetVisible(visible);
            pauseResumeButton.SetVisible(visible);
        }

        /// <summary>
        /// Show or hide the in-game scoreboard (always hidden in tutorial mode).

        /// </summary>
        private void SetScoreboardVisible(bool visible)
        {
            SetGameObjectVisible(scoreboardRoot, visible && !isTutorial);
        }

        /// <summary>
        /// Create a pause button icon in the upper right corner of the screen.

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
        /// Toggle the background music switch.

        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            mlpAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// Empty stub callback, does not perform any operation.

        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// Returns 0 when music is playing and 1 when muted (used to select the correct button icon).

        /// </summary>
        private static int GetMusicIconIndex()
        {
            return mlpAudio.Instance != null && mlpAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// Create a scoreboard background image (fallback to the album wizard when resources are missing).

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
        /// Create popup message background (currently not used - messages no longer need a border to render).
        /// </summary>
        private static GameObject CreatePopupBackdrop(Transform parent)
        {
            // Pop-up borders have been removed from the HUD. Message text is now rendered independently,
            // So this material will not appear again.
            return null;
        }

        /// <summary>
        /// Loads a texture from Resources and creates a sprite scaled to the target width. Returns null if not found.
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
        /// Create an empty root GameObject and mount it under the specified Transform.

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
        /// Creates an empty GameObject located at the specified pixel coordinates.

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
        /// Create a glowing halo behind your character's portrait.
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
        /// Creates a character avatar sprite scaled and positioned at a specified pixel position.

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
        /// Sets the GameObject's SpriteRenderer color tint (does nothing when empty).

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
        /// Convert seconds to a display string like "1:00" or "04.2".

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
    /// Menu button: A clickable button in the pause menu that supports mouseover highlighting and click callbacks.

    /// </summary>
    public sealed class mlpMenuButton
    {
        private Rect rect;                              // The rectangular area of the button (used for mouse collision detection)

        private readonly System.Action action;           // callback function executed when clicked

        private readonly GameObject sprite;              // GameObject for the button background

        private readonly TextMesh label;                 // Button text (old Unity text system)

        private readonly TMP_Text nativeLabel;           // Button text (TextMeshPro text system)

        private readonly Transform labelTransform;       // Text Transform component

        private readonly Vector3 baseScale;              // The base scaling value of the background sprite

        private readonly Vector3 labelBaseScale;         // The base scaling value of the text
        private bool visible = true;                     // Is the entire button visible?

        private bool backgroundVisible = true;           // Is the background sprite visible?

        private bool labelVisible = true;                // Is the text label visible?

        private bool pressed;                            // Whether it is currently pressed

        public GameObject Root => sprite;                // Publicly access properties of the background GameObject


        /// <summary>
        /// Create a clickable menu button with a background sprite and text label.

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
            // 1. Save the click callback function and calculate the collision rectangle of the button on the screen

            this.action = action;
            rect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
            // 2. Load the button background texture (use independent texture first, otherwise fall back to the atlas sprite)

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

            // 3. Scale the background sprite to the specified pixel size

            sprite.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / sourceWidth,
                mlpConstants.UnitsPerPixel * height / sourceHeight,
                1f);

            // 4. Record the basic zoom value (it will zoom in on this basis when hovering)

            baseScale = sprite.transform.localScale;
            // 5. Calculate the font size based on the button height and create a text label (preferably use the native text layer, otherwise use TextMesh)

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

            // 6. Record the base scaling value of the label
            labelBaseScale = labelTransform != null ? labelTransform.localScale : Vector3.one;
        }

        /// <summary>
        /// Detect mouseovers and clicks every frame. Highlight on hover and trigger action on click.
        /// </summary>
        public void Update(Camera camera)
        {
            // 1. When the button is invisible or there is no camera, reset the pressed state and skip

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

            // 2. Get the mouse position, convert it to game pixel coordinates, and determine whether it is within the button area.
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

            // 3. Buttons and labels are slightly enlarged (1.035x) on hover, otherwise restored to original size

            sprite.transform.localScale = inside ? baseScale * 1.035f : baseScale;
            if (labelTransform != null)
            {
                labelTransform.localScale = inside ? labelBaseScale * 1.035f : labelBaseScale;
            }

            // 4. The text turns golden when hovered, otherwise it is white

            var labelColor = inside ? new Color(1f, 0.92f, 0.25f) : Color.white;
            if (label != null)
            {
                label.color = labelColor;
            }
            else if (nativeLabel != null)
            {
                nativeLabel.color = labelColor;
            }

            // 5. Record the "pressed" state when the mouse is pressed
            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            // 6. When the mouse is released: If it was previously pressed and is still within the button area, play the sound effect and execute the callback

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
        /// Change the button's label text.

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
        /// Show or hide the entire button (background and label).

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
        /// Show or hide only the button background (keeping the label visible).

        /// </summary>
        public void SetBackgroundVisible(bool isVisible)
        {
            backgroundVisible = isVisible;
            sprite.SetActive(visible && backgroundVisible);
        }

        /// <summary>
        /// Show or hide only the button label text (keeping the background visible).

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
    /// Icon button: A small icon-style clickable button used for function buttons on the HUD (such as help, sound effects switches).
    /// </summary>
    public sealed class mlpIconButton
    {
        private readonly mlpMenuButton button;
        private readonly GameObject[] icons;
        private bool visible = true;
        private int activeIconIndex;

        /// <summary>
        /// Create a button that toggles between multiple icon images (e.g. music on/off).

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
        /// Forward mouse input handling to internal menu buttons.

        /// </summary>
        public void Update(Camera camera)
        {
            button.Update(camera);
        }

        /// <summary>
        /// Shows or hides the icon button and its current icon.

        /// </summary>
        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            button.SetVisible(isVisible);
            RefreshIcons();
        }

        /// <summary>
        /// Switch the currently displayed icon image.

        /// </summary>
        public void SetActiveIconIndex(int iconIndex)
        {
            activeIconIndex = Mathf.Clamp(iconIndex, 0, Mathf.Max(0, icons.Length - 1));
            RefreshIcons();
        }

        /// <summary>
        /// Loads a texture and creates a scaled sprite icon at the specified location.

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
        /// Shows only the currently active icons and hides all other icons.

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
    /// Energy bar view: A bar UI that displays the charging progress of the character's ultimate move. Once full, the ultimate move can be released.

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
        /// Build energy/skill charge bar UI for player slots.

        /// </summary>
        public mlpEnergyBarView(Transform parent, int controllerSlot, mlpCharacterSkillDefinition skillDefinition, float fullTime)
        {
            // 1. Obtain the player controller configuration and determine the energy bar position according to the player slot.

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

            // 2. Create an energy bar background (prioritize using independent textures, otherwise fall back to the atlas sprite)

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

            // 3. Load the base image and radial mask image of the skill icon

            var baseResourcePath = skillDefinition.HasStandaloneIconArt
                ? mlpAssets.Images.ResourcePath(skillDefinition.IconImageKey)
                : null;
            var maskResourcePath = skillDefinition.HasStandaloneIconArt
                ? mlpAssets.Images.ResourcePath(skillDefinition.ChargeMaskImageKey)
                : null;
            var baseTexture = !string.IsNullOrEmpty(baseResourcePath) ? Resources.Load<Texture2D>(baseResourcePath) : null;
            var maskTexture = !string.IsNullOrEmpty(maskResourcePath) ? Resources.Load<Texture2D>(maskResourcePath) : null;

            // 4. Create a skill icon layer and a radial fill mask layer (the mask layer is used to show charging progress)
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

            // 5. Create key prompt background and prompt text (such as the "E" key to release the ultimate move)

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

            // 6. Set the initial charging state (if the charging time is 0, it will be fully charged directly, otherwise it will start from 0)

            SetCharge(fullTime <= 0f ? 1f : 0f);
        }

        /// <summary>
        /// Updated the radial fill of the energy bar to show how close the ability is to being fully charged.

        /// </summary>
        public void SetCharge(float progress)
        {
            overlay?.SetProgress(progress);
        }

        /// <summary>
        /// Cleans up the energy bar's grid resource when leaving a match.

        /// </summary>
        public void ReleaseRuntimeResources()
        {
            overlay?.ReleaseRuntimeResources();
        }
    }

    /// <summary>
    /// Radial icon grid: Use a fan-shaped grid to display the partial filling effect of skill icons, indicating skill cooling or charging progress.

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
        /// Create a radial fill icon using sprites from the gallery.
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
        /// Create a radial fill icon using an independent texture.
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
        /// Update the visibility of the radial icon. 0 = empty, 1 = completely filled.
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
        /// Rebuilds the mesh triangles to reveal a sector of the specified degree.
        /// </summary>
        private void BuildSector(float degrees)
        {
            // 1. Calculate how many triangle segments are needed based on the angle (max 36 segments = 360 degrees)

            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(RadialSteps * Mathf.Clamp01(degrees / 360f)));
            var radius = Mathf.Min(width, height) * 0.5f;
            // 2. Allocate vertex, UV and triangle index arrays (number of vertices = center point + sector edge point)

            var vertices = new Vector3[segmentCount + 2];
            var uvs = new Vector2[segmentCount + 2];
            var triangles = new int[segmentCount * 3];
            // 3. The center vertex is located at the origin, and the UV is set to the texture center.
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f);

            // 4. Generate scalloped edge vertices clockwise from the 12 o'clock direction

            for (var i = 0; i <= segmentCount; i++)
            {
                var t = segmentCount == 0 ? 0f : i / (float)segmentCount;
                var angle = (90f - degrees * t) * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                // 5. Set the vertex position

                vertices[i + 1] = new Vector3(point.x, point.y, 0f);
                // 6. Map vertex positions to texture UV coordinates (to achieve fan-shaped clipping texture effect)

                var uvX = Mathf.Lerp(uvMin.x, uvMax.x, point.x / width + 0.5f);
                var uvY = Mathf.Lerp(uvMin.y, uvMax.y, point.y / height + 0.5f);
                uvs[i + 1] = new Vector2(uvX, uvY);
                // 7. Create a triangle for each segment (center point → current edge point → next edge point)

                if (i == segmentCount)
                {
                    continue;
                }

                var tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }

            // 8. Upload the calculated data to the grid

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Destroy the grid to free up memory, called when the icon is no longer needed.
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
