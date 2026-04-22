using UnityEngine;

namespace BasketballLegends2020
{
    public enum BLPauseCommand
    {
        None,
        Toggle,
        Resume,
        Menu
    }

    public sealed class BLHudView
    {
        private const float ScreenCenterY = 240f;
        private const float ScoreboardCenterX = BLConstants.Width2;
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
        private const float PauseTitleY = 78f;
        private const float PauseMatchupY = 112f;
        private const float PauseBoardY = 214f;
        private const float PauseNameY = 292f;
        private const float PauseMetaY = 330f;
        private const float PauseActionY = 392f;
        private const float PausePortraitOffsetX = 210f;
        private const float PausePortraitPixels = 84f;
        private const float PauseScoreOffsetX = 40f;
        private const float PopupCenterY = 236f;
        private const float PopupBackdropWidth = 432f;
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
        private readonly TextMesh leftScore;
        private readonly TextMesh rightScore;
        private readonly TextMesh leftNameText;
        private readonly TextMesh rightNameText;
        private readonly TextMesh timerText;
        private readonly BLMenuButton pauseButton;
        private readonly GameObject pauseButtonIcon;
        private readonly BLIconButton musicButton;
        private readonly BLIconButton helpButton;
        private readonly GameObject pauseOverlayRoot;
        private readonly GameObject pauseShade;
        private readonly GameObject pausePanel;
        private readonly TextMesh pauseTitleText;
        private readonly TextMesh pauseMatchupText;
        private readonly TextMesh pauseScoreText;
        private readonly TextMesh pauseLeftNameText;
        private readonly TextMesh pauseRightNameText;
        private readonly TextMesh pauseLeftScoreText;
        private readonly TextMesh pauseRightScoreText;
        private readonly TextMesh pauseScoreDividerText;
        private readonly GameObject pauseLeftPortrait;
        private readonly GameObject pauseRightPortrait;
        private readonly BLMenuButton pauseMenuButton;
        private readonly BLMenuButton pauseResumeButton;
        private readonly bool isTraining;
        private readonly GameObject messageRoot;
        private readonly TextMesh messageText;
        private readonly GameObject bonusNoticeRoot;
        private readonly TextMesh bonusNoticeText;
        private readonly GameObject countdownBackdrop;
        private readonly TextMesh countdownCaptionText;
        private readonly TextMesh countdownText;
        private readonly Vector3 countdownBaseScale;
        private readonly TextMesh postMatchTitleText;
        private readonly TextMesh postMatchScoreText;
        private readonly TextMesh postMatchPromptText;
        private float messageTime;
        private float messageDuration;
        private float messageVisualScale = 1f;
        private float bonusNoticeTime;
        private float bonusNoticeDuration;
        private float countdownTime = -1f;
        private float countdownPulseTime;
        private BLPauseCommand pendingPauseCommand;
        private int lastCountdownTick = int.MinValue;
        public bool IsPostMatchVisible => !string.IsNullOrEmpty(postMatchTitleText.text);
        public bool IsPauseOverlayVisible { get; private set; }

        public BLHudView(Transform parent, BLMatchData matchData)
        {
            isTraining = BLInventory.Instance.GameMode == 3;
            var leftCharacterName = BLPlayersData.GetCharacterName(matchData.CharacterIds[0]);
            var rightCharacterName = BLPlayersData.GetCharacterName(matchData.CharacterIds[1]);

            CreateScoreboardBackdrop(parent);
            CreatePortraitAura("LeftPortraitAura", ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, 0.235f, 81, parent);
            CreatePortraitAura("RightPortraitAura", ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, 0.235f, 81, parent);
            CreateCharacterPortrait("LeftPortrait", matchData.CharacterIds[0], ScoreboardCenterX - PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, parent);
            CreateCharacterPortrait("RightPortrait", matchData.CharacterIds[1], ScoreboardCenterX + PortraitOffsetX, PortraitBaseY, PortraitTargetPixels, PortraitSortingOrder, parent);

            leftNameText = BLRender.Text(
                "LeftName",
                leftCharacterName,
                ScoreboardCenterX - NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleRight,
                85,
                parent,
                BLTextStyle.HudName);
            rightNameText = BLRender.Text(
                "RightName",
                rightCharacterName,
                ScoreboardCenterX + NameOffsetX,
                NameY,
                18,
                Color.white,
                TextAnchor.MiddleLeft,
                85,
                parent,
                BLTextStyle.HudName);

            var scoreColor = new Color32(0xFF, 0xA7, 0x22, 0xFF);
            leftScore = BLRender.Text(
                "LeftScore",
                "0",
                ScoreboardCenterX - ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                parent,
                BLTextStyle.HudScore);
            rightScore = BLRender.Text(
                "RightScore",
                "0",
                ScoreboardCenterX + ScoreOffsetX,
                ScoreY,
                34,
                scoreColor,
                TextAnchor.MiddleCenter,
                86,
                parent,
                BLTextStyle.HudScore);
            timerText = BLRender.Text(
                "Timer",
                "1:00",
                ScoreboardCenterX,
                TimerY,
                18,
                new Color32(0xC6, 0xFF, 0x33, 0xFF),
                TextAnchor.MiddleCenter,
                87,
                parent,
                BLTextStyle.HudTimer);

            pauseButton = new BLMenuButton(string.Empty, PauseButtonX, TopRightButtonY, TopRightButtonSize, TopRightButtonSize, () => pendingPauseCommand = BLPauseCommand.Toggle, parent);
            pauseButton.SetBackgroundVisible(false);
            pauseButton.SetLabelVisible(false);
            pauseButtonIcon = CreatePauseButtonIcon(parent);
            musicButton = new BLIconButton(
                "HudMusicButton",
                MusicButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                ToggleBackgroundMusic,
                parent,
                82,
                TopRightIconPixels,
                "BL2020/Images/music_button_on",
                "BL2020/Images/music_button_off");
            musicButton.SetActiveIconIndex(GetMusicIconIndex());
            helpButton = new BLIconButton(
                "HudHelpButton",
                HelpButtonX,
                TopRightButtonY,
                TopRightButtonSize,
                TopRightButtonSize,
                NoOpAction,
                parent,
                82,
                TopRightIconPixels,
                "BL2020/Images/help_button");

            countdownBackdrop = CreateHudImage("CountdownBackdrop", "BL2020/Hud/popup_halloween", BLConstants.Width2, CountdownY + 4f, 360f, 119, parent);
            countdownCaptionText = BLRender.Text(
                "CountdownCaption",
                string.Empty,
                BLConstants.Width2,
                CountdownY - 28f,
                16,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                BLTextStyle.TournamentAccent);
            countdownText = BLRender.Text(
                "Countdown",
                string.Empty,
                BLConstants.Width2,
                CountdownY,
                58,
                new Color32(0xFF, 0xB8, 0x2E, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                BLTextStyle.HudPopup);
            countdownBaseScale = countdownText.transform.localScale;

            messageRoot = CreateHudAnchor("MessageRoot", BLConstants.Width2, PopupCenterY, parent);
            var messageBackdrop = CreatePopupBackdrop(parent);
            if (messageBackdrop != null)
            {
                messageBackdrop.transform.SetParent(messageRoot.transform, true);
            }

            messageText = BLRender.Text(
                "Message",
                string.Empty,
                BLConstants.Width2,
                PopupCenterY + 2f,
                56,
                new Color32(0x8B, 0x2D, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                BLTextStyle.HudPopup);
            messageText.transform.SetParent(messageRoot.transform, true);
            messageRoot.SetActive(false);

            bonusNoticeRoot = CreateHudAnchor("BonusNoticeRoot", BonusNoticeX, BonusNoticeY, parent);
            bonusNoticeText = BLRender.Text(
                "BonusNotice",
                string.Empty,
                BonusNoticeX,
                BonusNoticeY,
                16,
                new Color32(0xFF, 0x7A, 0x39, 0xFF),
                TextAnchor.MiddleCenter,
                119,
                parent,
                BLTextStyle.TournamentAccent);
            bonusNoticeText.transform.SetParent(bonusNoticeRoot.transform, true);
            bonusNoticeRoot.SetActive(false);

            postMatchTitleText = BLRender.Text(
                "PostMatchTitle",
                string.Empty,
                BLConstants.Width2,
                188f,
                40,
                new Color32(0xFF, 0x9C, 0x12, 0xFF),
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLTextStyle.HudPopup);
            postMatchScoreText = BLRender.Text(
                "PostMatchScore",
                string.Empty,
                BLConstants.Width2,
                236f,
                24,
                Color.white,
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLTextStyle.HudScore);
            postMatchPromptText = BLRender.Text(
                "PostMatchPrompt",
                string.Empty,
                BLConstants.Width2,
                276f,
                18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLTextStyle.TournamentAccent);

            pauseOverlayRoot = CreateHudRoot("PauseOverlayRoot", parent);
            pauseShade = CreatePausePanel("PauseShade", BLConstants.Width2, ScreenCenterY, 800f, 480f, 140, pauseOverlayRoot.transform, new Color(0.01f, 0.03f, 0.05f, 0.78f));
            CreatePausePanel("PauseTopGlow", BLConstants.Width2, 96f, 760f, 104f, 141, pauseOverlayRoot.transform, new Color(0.22f, 0.86f, 0.94f, 0.12f));
            CreatePausePanel("PauseBottomGlow", BLConstants.Width2, 388f, 760f, 132f, 141, pauseOverlayRoot.transform, new Color(0.56f, 0.22f, 0.94f, 0.1f));
            pausePanel = CreatePausePanel("PausePanel", BLConstants.Width2, ScreenCenterY, 582f, 308f, 142, pauseOverlayRoot.transform, new Color(0.05f, 0.08f, 0.12f, 0.9f));
            CreatePauseFrame("PauseFrame", "MatchBack0002", BLConstants.Width2, ScreenCenterY, 632f, 332f, 143, pauseOverlayRoot.transform, new Color(0.9f, 0.98f, 1f, 0.96f));
            CreatePausePanel("PauseBoardTint", BLConstants.Width2, PauseBoardY, 206f, 72f, 144, pauseOverlayRoot.transform, new Color(0.02f, 0.04f, 0.09f, 0.4f));
            var pauseBoard = CreateHudImage("PauseBoard", "BL2020/Hud/scoreboard_halloween", BLConstants.Width2, PauseBoardY, 560f, 145, pauseOverlayRoot.transform);
            if (pauseBoard == null)
            {
                CreatePauseFrame("PauseBoardFallback", "btn_bg0000", BLConstants.Width2, PauseBoardY, 456f, 150f, 145, pauseOverlayRoot.transform, new Color(0.22f, 0.84f, 0.95f, 0.94f));
            }

            CreatePortraitAura("PauseLeftPortraitAura", BLConstants.Width2 - PausePortraitOffsetX, PauseBoardY, 0.46f, 146, pauseOverlayRoot.transform);
            CreatePortraitAura("PauseRightPortraitAura", BLConstants.Width2 + PausePortraitOffsetX, PauseBoardY, 0.46f, 146, pauseOverlayRoot.transform);
            pauseLeftPortrait = CreateCharacterPortrait("PauseLeftPortrait", matchData.CharacterIds[0], BLConstants.Width2 - PausePortraitOffsetX, PauseBoardY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseRightPortrait = CreateCharacterPortrait("PauseRightPortrait", matchData.CharacterIds[1], BLConstants.Width2 + PausePortraitOffsetX, PauseBoardY, PausePortraitPixels, 147, pauseOverlayRoot.transform);
            pauseTitleText = BLRender.Text(
                "PauseTitle",
                "GAME PAUSED",
                BLConstants.Width2,
                PauseTitleY,
                40,
                new Color32(0xC8, 0xFF, 0x55, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                pauseOverlayRoot.transform,
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0.04f, 0.08f, 0.02f, 0.95f),
                outlinePixels: 1.85f,
                shadowColor: new Color(0f, 0f, 0f, 0.45f),
                shadowOffset: new Vector2(1.4f, -1.4f));
            pauseMatchupText = BLRender.Text(
                "PauseMatchup",
                $"{leftCharacterName}  VS  {rightCharacterName}",
                BLConstants.Width2,
                PauseMatchupY,
                18,
                new Color32(0x92, 0xFA, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                pauseOverlayRoot.transform,
                BLFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.75f);
            pauseLeftNameText = BLRender.Text(
                "PauseLeftName",
                leftCharacterName,
                BLConstants.Width2 - PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                BLFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseRightNameText = BLRender.Text(
                "PauseRightName",
                rightCharacterName,
                BLConstants.Width2 + PausePortraitOffsetX,
                PauseNameY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                BLFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f),
                outlinePixels: 0.68f);
            pauseLeftScoreText = BLRender.Text(
                "PauseLeftScore",
                "0",
                BLConstants.Width2 - PauseScoreOffsetX,
                PauseBoardY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreDividerText = BLRender.Text(
                "PauseScoreDivider",
                ":",
                BLConstants.Width2,
                PauseBoardY - 1f,
                32,
                new Color32(0x8F, 0xFF, 0xF8, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.95f),
                outlinePixels: 1f);
            pauseRightScoreText = BLRender.Text(
                "PauseRightScore",
                "0",
                BLConstants.Width2 + PauseScoreOffsetX,
                PauseBoardY,
                42,
                new Color32(0xFF, 0xC2, 0x42, 0xFF),
                TextAnchor.MiddleCenter,
                146,
                pauseOverlayRoot.transform,
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f),
                outlinePixels: 1.4f);
            pauseScoreText = BLRender.Text(
                "PauseMeta",
                string.Empty,
                BLConstants.Width2,
                PauseMetaY,
                15,
                new Color32(0xCC, 0xF6, 0xFF, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                pauseOverlayRoot.transform,
                BLFontKind.RajdhaniBold,
                outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f),
                outlinePixels: 0.62f);
            pauseMenuButton = new BLMenuButton("MENU", 310f, PauseActionY, 168f, 48f, () => pendingPauseCommand = BLPauseCommand.Menu, pauseOverlayRoot.transform, 147);
            pauseResumeButton = new BLMenuButton("RESUME", 490f, PauseActionY, 196f, 48f, () => pendingPauseCommand = BLPauseCommand.Resume, pauseOverlayRoot.transform, 147);

            SetPauseOverlayVisible(false);
            if (isTraining)
            {
                SetText(pauseMatchupText, "TRAINING SESSION");
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

        public void UpdateScore(int left, int right)
        {
            SetText(leftScore, left.ToString());
            SetText(rightScore, right.ToString());
            SetText(pauseLeftScoreText, left.ToString());
            SetText(pauseRightScoreText, right.ToString());
        }

        public void UpdateTimer(float secondsLeft)
        {
            var timeText = FormatTime(secondsLeft);
            SetText(timerText, timeText);
            if (!isTraining)
            {
                SetText(pauseScoreText, $"TIME FROZEN / {timeText}");
            }
        }

        public void SetTimerVisible(bool visible)
        {
            timerText.gameObject.SetActive(visible);
        }

        public BLPauseCommand ConsumePauseCommand()
        {
            var command = pendingPauseCommand;
            pendingPauseCommand = BLPauseCommand.None;
            return command;
        }

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

        public void HidePauseOverlay()
        {
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

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

        public void EndResumeCountdown()
        {
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
            musicButton?.SetVisible(true);
            helpButton?.SetVisible(true);
        }

        public void ShowMessage(string message, float duration = 1.2f)
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
            messageRoot.transform.position = BLConstants.PixelToWorldSnapped(BLConstants.Width2, PopupCenterY);
            messageRoot.transform.localScale = Vector3.one * (0.78f * messageVisualScale);
            messageRoot.SetActive(true);
        }

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
            bonusNoticeRoot.transform.position = BLConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
            bonusNoticeRoot.transform.localScale = Vector3.one * 0.72f;
            bonusNoticeRoot.SetActive(true);
        }

        public void HideMessage()
        {
            SetText(messageText, string.Empty);
            messageTime = 0f;
            messageDuration = 0f;
            messageVisualScale = 1f;
            if (messageRoot != null)
            {
                messageRoot.transform.localScale = Vector3.one;
                messageRoot.transform.position = BLConstants.PixelToWorldSnapped(BLConstants.Width2, PopupCenterY);
                messageRoot.SetActive(false);
            }
        }

        public void HideBonusNotice()
        {
            SetText(bonusNoticeText, string.Empty);
            bonusNoticeTime = 0f;
            bonusNoticeDuration = 0f;
            if (bonusNoticeRoot != null)
            {
                bonusNoticeRoot.transform.localScale = Vector3.one;
                bonusNoticeRoot.transform.position = BLConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY);
                bonusNoticeRoot.SetActive(false);
            }
        }

        public void HideCountdown()
        {
            SetText(countdownText, string.Empty);
            SetText(countdownCaptionText, string.Empty);
            countdownTime = -1f;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            countdownText.transform.localScale = countdownBaseScale;
            SetGameObjectVisible(countdownBackdrop, false);
            SetGameObjectVisible(countdownCaptionText.gameObject, false);
        }

        public void StartCountdown(float duration)
        {
            StartCountdown(duration, string.Empty);
        }

        public void StartCountdown(float duration, string caption)
        {
            countdownTime = duration;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            SetText(countdownText, string.Empty);
            SetText(countdownCaptionText, caption ?? string.Empty);
            countdownText.color = new Color32(0xFF, 0xB8, 0x2E, 0xFF);
            countdownText.transform.localScale = countdownBaseScale * 0.82f;
            SetGameObjectVisible(countdownBackdrop, true);
            SetGameObjectVisible(countdownCaptionText.gameObject, !string.IsNullOrEmpty(caption));
            HideMessage();
            HideBonusNotice();
            HidePostMatch();
        }

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
                    BLAudio.Instance?.Play(BLAssets.Sounds.MCountdown, 0.8f);
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

        public void Update(float dt)
        {
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

            if (countdownTime < 0f && !string.IsNullOrEmpty(countdownText.text))
            {
                HideCountdown();
            }
        }

        public void ShowPostMatch(int winner, int leftScoreValue, int rightScoreValue)
        {
            var inventory = BLInventory.Instance;
            if (inventory.IsTournamentActive || inventory.GameMode == 1 || inventory.GameMode == 2)
            {
                SetText(postMatchTitleText, winner == -1 ? "YOU WIN!" : "YOU LOSE");
            }
            else
            {
                SetText(postMatchTitleText, winner == -1 ? "PLAYER 1 WINS" : "PLAYER 2 WINS");
            }

            postMatchTitleText.color = winner == -1
                ? new Color32(0xFF, 0xB3, 0x2A, 0xFF)
                : new Color32(0x9C, 0x4B, 0xFF, 0xFF);
            SetText(postMatchScoreText, $"{leftScoreValue} - {rightScoreValue}");
            SetText(postMatchPromptText, inventory.IsTournamentActive ? "CLICK TO CONTINUE" : "CLICK OR PRESS ENTER");
            HideMessage();
            HideBonusNotice();
            HideCountdown();
        }

        public void HidePostMatch()
        {
            SetText(postMatchTitleText, string.Empty);
            SetText(postMatchScoreText, string.Empty);
            SetText(postMatchPromptText, string.Empty);
        }

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
            messageRoot.transform.position = BLConstants.PixelToWorldSnapped(BLConstants.Width2, PopupCenterY - lift);
            messageRoot.transform.localScale = Vector3.one * (scale * messageVisualScale);
        }

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
            bonusNoticeRoot.transform.position = BLConstants.PixelToWorldSnapped(BonusNoticeX, BonusNoticeY - drift);
            bonusNoticeRoot.transform.localScale = Vector3.one * scale;
        }

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

        private void ApplyMessageTheme(string message)
        {
            switch (message)
            {
                case "3 POINTS!":
                    messageText.color = new Color32(0xFF, 0x98, 0x10, 0xFF);
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

        private static GameObject CreatePausePanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var panel = BLRender.Sprite(name, BLAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                BLConstants.UnitsPerPixel * width / 10f,
                BLConstants.UnitsPerPixel * height / 10f,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject CreatePauseFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var panel = BLRender.Sprite(name, BLAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = BLAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    BLConstants.UnitsPerPixel * width / sourceWidth,
                    BLConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static void SetGameObjectVisible(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        private static void SetText(TextMesh target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value;
            target.font?.RequestCharactersInTexture(value, target.fontSize, FontStyle.Normal);
        }

        private void SetPauseOverlayVisible(bool visible)
        {
            SetGameObjectVisible(pauseOverlayRoot, visible);
            pauseMenuButton.SetVisible(visible);
            pauseResumeButton.SetVisible(visible);
        }

        private GameObject CreatePauseButtonIcon(Transform parent)
        {
            const string resourcePath = "BL2020/Images/pause_button";
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return BLIconButton.CreateImageIcon("PauseButtonIcon", resourcePath, PauseButtonX, TopRightButtonY, 82, TopRightIconPixels, parent);
            }

            var icon = BLRender.Sprite("PauseButtonIconFallback", BLAtlasCache.Instance.Gameplay, "InGamePauseButton0000", PauseButtonX, TopRightButtonY, 0.5f, 0.5f, 82, parent);
            icon.transform.localScale *= 1.2f;
            return icon;
        }

        private static void ToggleBackgroundMusic()
        {
            BLAudio.Instance?.ToggleMusic();
        }

        private static void NoOpAction()
        {
        }

        private static int GetMusicIconIndex()
        {
            return BLAudio.Instance != null && BLAudio.Instance.MusicEnabled ? 0 : 1;
        }

        private static void CreateScoreboardBackdrop(Transform parent)
        {
            var image = CreateHudImage("ScoreboardBackdrop", "BL2020/Hud/scoreboard_halloween", ScoreboardCenterX, ScoreboardCenterY, ScoreboardTargetWidth, 80, parent);
            if (image != null)
            {
                return;
            }

            BLRender.Sprite("InfoPanel", BLAtlasCache.Instance.Gameplay, "infoPanel0000", BLConstants.Width2, 60f, 0.5f, 0.5f, 80, parent);
        }

        private static GameObject CreatePopupBackdrop(Transform parent)
        {
            return CreateHudImage("MessageBackdrop", "BL2020/Hud/popup_halloween", BLConstants.Width2, PopupCenterY + 1f, PopupBackdropWidth, 118, parent);
        }

        private static GameObject CreateHudImage(string name, string resourcePath, float x, float y, float targetWidth, int sortingOrder, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var image = BLRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            image.transform.localScale *= targetWidth / Mathf.Max(1f, texture.width);
            return image;
        }

        private static GameObject CreateHudRoot(string name, Transform parent)
        {
            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            return root;
        }

        private static GameObject CreateHudAnchor(string name, float x, float y, Transform parent)
        {
            var anchor = new GameObject(name);
            if (parent != null)
            {
                anchor.transform.SetParent(parent, false);
            }

            anchor.transform.position = BLConstants.PixelToWorldSnapped(x, y);
            return anchor;
        }

        private static void CreatePortraitAura(string name, float x, float y, float scale, int sortingOrder, Transform parent)
        {
            var interfaceAtlas = BLAtlasCache.Instance.Interface;
            if (interfaceAtlas == null || !interfaceAtlas.HasFrame("EmblemsBg0000"))
            {
                return;
            }

            var aura = BLRender.Sprite(name, interfaceAtlas, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            aura.transform.localScale *= scale;
            aura.GetComponent<SpriteRenderer>().color = new Color32(0x46, 0xFF, 0xF0, 0x95);
        }

        private static GameObject CreateCharacterPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            var sprite = BLPlayersData.GetCharacterPortraitSprite(characterId);
            if (sprite == null)
            {
                return null;
            }

            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var targetSize = targetPixels * BLPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetSize / Mathf.Max(1f, spritePixels);
            var adjustedY = y + BLPlayersData.GetCharacterPortraitOffsetY(characterId);
            BLRender.ApplyPixelTransform(portrait.transform, x, adjustedY, 0f, scale);
            return portrait;
        }

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

    public sealed class BLMenuButton
    {
        private readonly Rect rect;
        private readonly System.Action action;
        private readonly GameObject sprite;
        private readonly TextMesh label;
        private readonly Vector3 baseScale;
        private bool visible = true;
        private bool backgroundVisible = true;
        private bool labelVisible = true;
        private bool pressed;
        public GameObject Root => sprite;

        public BLMenuButton(string text, float x, float y, float width, float height, System.Action action, Transform parent, int sortingOrder = 50)
        {
            this.action = action;
            rect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
            sprite = BLRender.Sprite($"Button_{text}", BLAtlasCache.Instance.Interface, "btn_bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            var frame = BLAtlasCache.Instance.Interface.Frame("btn_bg0000");
            if (frame != null)
            {
                sprite.transform.localScale = new Vector3(
                    BLConstants.UnitsPerPixel * width / frame.W,
                    BLConstants.UnitsPerPixel * height / frame.H,
                    1f);
            }

            baseScale = sprite.transform.localScale;
            var fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.55f), 18, 32);
            label = BLRender.Text(
                $"ButtonText_{text}",
                text,
                x,
                y + 1f,
                fontSize,
                Color.white,
                TextAnchor.MiddleCenter,
                sortingOrder + 30,
                parent,
                BLTextStyle.ButtonLabel);
        }

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
            var world = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -camera.transform.position.z));
            var pixel = BLConstants.WorldToPixel(world);
            var inside = rect.Contains(pixel);
            sprite.transform.localScale = inside ? baseScale * 1.035f : baseScale;
            label.color = inside ? new Color(1f, 0.92f, 0.25f) : Color.white;

            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            if (pressed && Input.GetMouseButtonUp(0))
            {
                pressed = false;
                if (inside)
                {
                    BLAudio.Instance?.Play(BLAssets.Sounds.Button);
                    action?.Invoke();
                }
            }
        }

        public void SetText(string text)
        {
            label.text = text;
        }

        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            sprite.SetActive(isVisible && backgroundVisible);
            label.gameObject.SetActive(isVisible && labelVisible);
        }

        public void SetBackgroundVisible(bool isVisible)
        {
            backgroundVisible = isVisible;
            sprite.SetActive(visible && backgroundVisible);
        }

        public void SetLabelVisible(bool isVisible)
        {
            labelVisible = isVisible;
            label.gameObject.SetActive(visible && labelVisible);
        }
    }

    public sealed class BLIconButton
    {
        private readonly BLMenuButton button;
        private readonly GameObject[] icons;
        private bool visible = true;
        private int activeIconIndex;

        public BLIconButton(
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
            button = new BLMenuButton(string.Empty, x, y, width, height, action, parent);
            button.SetBackgroundVisible(false);
            button.SetLabelVisible(false);

            icons = new GameObject[resourcePaths.Length];
            for (var i = 0; i < resourcePaths.Length; i++)
            {
                icons[i] = CreateImageIcon($"{name}_Icon{i}", resourcePaths[i], x, y, sortingOrder, targetPixels, parent);
            }

            RefreshIcons();
        }

        public void Update(Camera camera)
        {
            button.Update(camera);
        }

        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            button.SetVisible(isVisible);
            RefreshIcons();
        }

        public void SetActiveIconIndex(int iconIndex)
        {
            activeIconIndex = Mathf.Clamp(iconIndex, 0, Mathf.Max(0, icons.Length - 1));
            RefreshIcons();
        }

        public static GameObject CreateImageIcon(string name, string resourcePath, float x, float y, int sortingOrder, float targetPixels, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var icon = BLRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var sourcePixels = Mathf.Max(texture.width, texture.height);
            icon.transform.localScale *= targetPixels / Mathf.Max(1f, sourcePixels);
            return icon;
        }

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

    public sealed class BLEnergyBarView
    {
        private readonly BLRadialIconMesh overlay;

        public BLEnergyBarView(Transform parent, int controllerSlot, int superId, float fullTime)
        {
            var profile = BLControlsData.ProfileForSlot(controllerSlot);
            var x = 45f;
            if (controllerSlot == 1)
            {
                x = 185f;
            }
            else if (controllerSlot == 2)
            {
                x = 614f;
            }

            var y = 45f;
            var bg = BLRender.Sprite($"EnergyBg_{controllerSlot}", BLAtlasCache.Instance.Gameplay, "btn_bg20000", x, y + 1f, 0.5f, 0.5f, 83, parent);
            bg.transform.localScale *= 1.1f;
            BLRender.Sprite($"EnergyBase_{controllerSlot}", BLAtlasCache.Instance.Interface, $"icon_ball000{superId}", x, y, 0.5f, 0.5f, 84, parent);
            overlay = new BLRadialIconMesh($"EnergyFill_{controllerSlot}", BLAtlasCache.Instance.Interface, $"icon_ball2000{superId}", x, y, 85, parent);

            BLRender.Sprite($"EnergyHintBg_{controllerSlot}", BLAtlasCache.Instance.Gameplay, "key_hint0000", x - 30f, y + 30f, 0.5f, 0.5f, 86, parent);
            BLRender.Text(
                $"EnergyHint_{controllerSlot}",
                profile.SuperHint,
                x - 30f,
                y + 32f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                87,
                parent,
                BLTextStyle.TournamentBody);

            SetCharge(fullTime <= 0f ? 1f : 0f);
        }

        public void SetCharge(float progress)
        {
            overlay.SetProgress(progress);
        }
    }

    public sealed class BLRadialIconMesh
    {
        private const int RadialSteps = 36;
        private const float DegreesPerStep = 10f;
        private readonly GameObject graphic;
        private readonly Mesh mesh;
        private readonly float width;
        private readonly float height;
        private readonly Vector2 uvMin;
        private readonly Vector2 uvMax;

        public BLRadialIconMesh(string name, BLAtlas atlas, string frameName, float x, float y, int sortingOrder, Transform parent)
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

            var material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = sprite.texture;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;

            width = frame.W;
            height = frame.H;
            var rect = sprite.rect;
            uvMin = new Vector2(rect.xMin / sprite.texture.width, rect.yMin / sprite.texture.height);
            uvMax = new Vector2(rect.xMax / sprite.texture.width, rect.yMax / sprite.texture.height);

            BLRender.ApplyPixelTransform(graphic.transform, x, y, 0.13f, 1f);
            SetProgress(0f);
        }

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
    }
}
