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
        private const float PopupCenterY = 236f;
        private const float PopupBackdropWidth = 432f;
        private const float MessageExitWindow = 0.18f;
        private const float CountdownPulseDuration = 0.42f;
        private readonly TextMesh leftScore;
        private readonly TextMesh rightScore;
        private readonly TextMesh leftNameText;
        private readonly TextMesh rightNameText;
        private readonly TextMesh timerText;
        private readonly BLMenuButton pauseButton;
        private readonly GameObject pauseButtonIcon;
        private readonly GameObject pauseShade;
        private readonly GameObject pausePanel;
        private readonly TextMesh pauseTitleText;
        private readonly TextMesh pauseMatchupText;
        private readonly TextMesh pauseScoreText;
        private readonly BLMenuButton pauseMenuButton;
        private readonly BLMenuButton pauseResumeButton;
        private readonly bool isTraining;
        private readonly GameObject messageRoot;
        private readonly TextMesh messageText;
        private readonly TextMesh countdownText;
        private readonly Vector3 countdownBaseScale;
        private readonly TextMesh postMatchTitleText;
        private readonly TextMesh postMatchScoreText;
        private readonly TextMesh postMatchPromptText;
        private float messageTime;
        private float messageDuration;
        private float messageVisualScale = 1f;
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

            pauseButton = new BLMenuButton(string.Empty, 770f, 44f, 60f, 60f, () => pendingPauseCommand = BLPauseCommand.Toggle, parent);
            pauseButton.SetBackgroundVisible(false);
            pauseButton.SetLabelVisible(false);
            pauseButtonIcon = CreatePauseButtonIcon(parent);

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

            pauseShade = CreatePausePanel("PauseShade", BLConstants.Width2, ScreenCenterY, 800f, 480f, 140, parent, new Color(0f, 0f, 0f, 0.68f));
            pausePanel = CreatePausePanel("PausePanel", BLConstants.Width2, ScreenCenterY, 420f, 272f, 141, parent, new Color(0.13f, 0.1f, 0.23f, 0.96f));
            pauseTitleText = BLRender.Text(
                "PauseTitle",
                "GAME PAUSED",
                BLConstants.Width2,
                158f,
                30,
                new Color32(0xFF, 0xA3, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                145,
                parent,
                BLFontKind.CfCrackBold,
                outlineColor: Color.white,
                outlinePixels: 1.4f);
            pauseMatchupText = BLRender.Text(
                "PauseMatchup",
                $"{leftCharacterName}  VS  {rightCharacterName}",
                BLConstants.Width2,
                200f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                145,
                parent,
                BLTextStyle.TournamentBody);
            pauseScoreText = BLRender.Text(
                "PauseScore",
                "0 : 0",
                BLConstants.Width2,
                236f,
                28,
                Color.white,
                TextAnchor.MiddleCenter,
                145,
                parent,
                BLTextStyle.HudScore);
            pauseMenuButton = new BLMenuButton("MENU", 314f, 322f, 154f, 44f, () => pendingPauseCommand = BLPauseCommand.Menu, parent);
            pauseResumeButton = new BLMenuButton("RESUME", 486f, 322f, 174f, 44f, () => pendingPauseCommand = BLPauseCommand.Resume, parent);

            SetPauseOverlayVisible(false);
            if (isTraining)
            {
                SetGameObjectVisible(leftNameText.gameObject, false);
                SetGameObjectVisible(rightNameText.gameObject, false);
                SetGameObjectVisible(pauseMatchupText.gameObject, false);
                SetGameObjectVisible(pauseScoreText.gameObject, false);
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
            SetText(pauseScoreText, $"{left} : {right}");
        }

        public void UpdateTimer(float secondsLeft)
        {
            SetText(timerText, FormatTime(secondsLeft));
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
            IsPauseOverlayVisible = true;
            SetPauseOverlayVisible(true);
            pauseButton.SetVisible(false);
            SetGameObjectVisible(pauseButtonIcon, false);
        }

        public void HidePauseOverlay()
        {
            IsPauseOverlayVisible = false;
            SetPauseOverlayVisible(false);
            pauseButton.SetVisible(true);
            SetGameObjectVisible(pauseButtonIcon, true);
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

        public void HideCountdown()
        {
            SetText(countdownText, string.Empty);
            countdownTime = -1f;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            countdownText.transform.localScale = countdownBaseScale;
        }

        public void StartCountdown(float duration)
        {
            countdownTime = duration;
            countdownPulseTime = 0f;
            lastCountdownTick = int.MinValue;
            SetText(countdownText, string.Empty);
            countdownText.color = new Color32(0xFF, 0xB8, 0x2E, 0xFF);
            countdownText.transform.localScale = countdownBaseScale * 0.82f;
            HideMessage();
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
                pauseMenuButton.Update(Camera.main);
                pauseResumeButton.Update(Camera.main);
                return;
            }

            pauseButton.Update(Camera.main);

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
                default:
                    messageText.color = new Color32(0x8B, 0x2D, 0xFF, 0xFF);
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
            SetGameObjectVisible(pauseShade, visible);
            SetGameObjectVisible(pausePanel, visible);
            SetGameObjectVisible(pauseTitleText.gameObject, visible);
            pauseMenuButton.SetVisible(visible);
            pauseResumeButton.SetVisible(visible);
            if (!isTraining)
            {
                SetGameObjectVisible(pauseMatchupText.gameObject, visible);
                SetGameObjectVisible(pauseScoreText.gameObject, visible);
            }
        }

        private GameObject CreatePauseButtonIcon(Transform parent)
        {
            const float x = 770f;
            const float y = 44f;
            var texture = Resources.Load<Texture2D>("BL2020/Images/pause_button");
            GameObject icon;
            if (texture != null)
            {
                icon = BLRender.Image("PauseButtonIcon", texture, x, y, 0.5f, 0.5f, 82, parent);
                var targetPixels = 58f;
                var sourcePixels = Mathf.Max(texture.width, texture.height);
                icon.transform.localScale *= targetPixels / Mathf.Max(1f, sourcePixels);
            }
            else
            {
                icon = BLRender.Sprite("PauseButtonIconFallback", BLAtlasCache.Instance.Gameplay, "InGamePauseButton0000", x, y, 0.5f, 0.5f, 82, parent);
                icon.transform.localScale *= 1.2f;
            }

            return icon;
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

        public BLMenuButton(string text, float x, float y, float width, float height, System.Action action, Transform parent)
        {
            this.action = action;
            rect = new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
            sprite = BLRender.Sprite($"Button_{text}", BLAtlasCache.Instance.Interface, "btn_bg0000", x, y, 0.5f, 0.5f, 50, parent);
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
                80,
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
