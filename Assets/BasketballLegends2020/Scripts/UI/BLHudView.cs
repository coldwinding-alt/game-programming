using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLHudView
    {
        private readonly TextMesh leftScore;
        private readonly TextMesh rightScore;
        private readonly TextMesh timerText;
        private readonly TextMesh messageText;
        private readonly TextMesh countdownText;
        private readonly TextMesh postMatchTitleText;
        private readonly TextMesh postMatchScoreText;
        private readonly TextMesh postMatchPromptText;
        private float messageTime;
        private float countdownTime = -1f;
        private int lastCountdownTick = int.MinValue;
        public bool IsPostMatchVisible => !string.IsNullOrEmpty(postMatchTitleText.text);

        public BLHudView(Transform parent, BLMatchData matchData)
        {
            BLRender.Sprite("InfoPanel", BLAtlasCache.Instance.Gameplay, "infoPanel0000", BLConstants.Width2, 60f, 0.5f, 0.5f, 80, parent);
            BLRender.Sprite("HudEmblemBgLeft", BLAtlasCache.Instance.Interface, "EmblemsBg0000", BLConstants.Width2 - 103f, 42f, 0.5f, 0.5f, 81, parent).transform.localScale *= 0.31f;
            BLRender.Sprite("HudEmblemBgRight", BLAtlasCache.Instance.Interface, "EmblemsBg0000", BLConstants.Width2 + 103f, 42f, 0.5f, 0.5f, 81, parent).transform.localScale *= 0.31f;
            var emblemLeftFrame = "Emblems00" + (matchData.Teams[0] - 1 < 10 ? "0" : "") + (matchData.Teams[0] - 1);
            var emblemRightFrame = "Emblems00" + (matchData.Teams[1] - 1 < 10 ? "0" : "") + (matchData.Teams[1] - 1);
            BLRender.Sprite("HudEmblemLeft", BLAtlasCache.Instance.Interface, emblemLeftFrame, BLConstants.Width2 - 101f, 42f, 0.5f, 0.5f, 82, parent).transform.localScale *= 0.3f;
            BLRender.Sprite("HudEmblemRight", BLAtlasCache.Instance.Interface, emblemRightFrame, BLConstants.Width2 + 103f, 42f, 0.5f, 0.5f, 82, parent).transform.localScale *= 0.3f;

            var scoreColor = new Color(1f, 0.6f, 0f);
            leftScore = BLRender.Text(
                "LeftScore",
                "0",
                BLConstants.Width2 - 10f,
                65f,
                46,
                scoreColor,
                TextAnchor.LowerRight,
                100,
                parent,
                BLFontKind.CfCrackBold,
                outlineColor: Color.black,
                outlinePixels: 2f);
            rightScore = BLRender.Text(
                "RightScore",
                "0",
                BLConstants.Width2 + 14f,
                65f,
                46,
                scoreColor,
                TextAnchor.LowerLeft,
                100,
                parent,
                BLFontKind.CfCrackBold,
                outlineColor: Color.black,
                outlinePixels: 2f);
            timerText = BLRender.Text(
                "Timer",
                "1:00",
                BLConstants.Width2,
                82f,
                24,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                100,
                parent,
                BLFontKind.CfCrackBold,
                shadowColor: new Color(0f, 0f, 0f, 0.5f),
                shadowOffset: new Vector2(3f, 3f));
            countdownText = BLRender.Text(
                "Countdown",
                "",
                BLConstants.Width2,
                170f,
                64,
                new Color32(0xFF, 0x99, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                120,
                parent,
                BLFontKind.Impact,
                outlineColor: Color.white,
                outlinePixels: 3f);
            messageText = BLRender.Text(
                "Message",
                "",
                BLConstants.Width2,
                240f,
                64,
                new Color32(0xFF, 0x99, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                119,
                parent,
                BLFontKind.Impact,
                outlineColor: Color.white,
                outlinePixels: 3f);
            postMatchTitleText = BLRender.Text(
                "PostMatchTitle",
                "",
                BLConstants.Width2,
                188f,
                42,
                new Color32(0xFF, 0xA3, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLFontKind.Impact,
                outlineColor: Color.white,
                outlinePixels: 2f,
                shadowColor: new Color(0f, 0f, 0f, 0.35f),
                shadowOffset: new Vector2(2f, 2f));
            postMatchScoreText = BLRender.Text(
                "PostMatchScore",
                "",
                BLConstants.Width2,
                234f,
                28,
                Color.white,
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0f, 0f, 0f, 0.85f),
                outlinePixels: 2f);
            postMatchPromptText = BLRender.Text(
                "PostMatchPrompt",
                "",
                BLConstants.Width2,
                276f,
                18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                130,
                parent,
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.8f),
                outlinePixels: 1f);
            UpdateScore(matchData.MatchScore[0], matchData.MatchScore[1]);
        }

        public void UpdateScore(int left, int right)
        {
            leftScore.text = left.ToString();
            rightScore.text = right.ToString();
        }

        public void UpdateTimer(float secondsLeft)
        {
            timerText.text = FormatTime(secondsLeft);
        }

        public void ShowMessage(string message, float duration = 1.2f)
        {
            messageText.text = message;
            messageTime = duration;
        }

        public void HideMessage()
        {
            messageText.text = "";
            messageTime = 0f;
        }

        public void HideCountdown()
        {
            countdownText.text = "";
            countdownTime = -1f;
            lastCountdownTick = int.MinValue;
        }

        public void StartCountdown(float duration)
        {
            countdownTime = duration;
            lastCountdownTick = int.MinValue;
            countdownText.text = "";
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
                if (tick > 0)
                {
                    countdownText.text = tick.ToString();
                    BLAudio.Instance?.Play(BLAssets.Sounds.MCountdown, 0.8f);
                }
                else
                {
                    countdownText.text = "GO!!!";
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
            if (messageTime > 0f)
            {
                messageTime -= dt;
                if (messageTime <= 0f)
                {
                    HideMessage();
                }
            }
            else if (!string.IsNullOrEmpty(messageText.text))
            {
                HideMessage();
            }

            if (countdownTime < 0f && !string.IsNullOrEmpty(countdownText.text))
            {
                HideCountdown();
            }
        }

        public void ShowPostMatch(int winner, int leftScore, int rightScore)
        {
            postMatchTitleText.text = winner == -1 ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
            postMatchScoreText.text = $"{leftScore} - {rightScore}";
            postMatchPromptText.text = "CLICK OR PRESS ENTER";
            HideMessage();
            HideCountdown();
        }

        public void HidePostMatch()
        {
            postMatchTitleText.text = "";
            postMatchScoreText.text = "";
            postMatchPromptText.text = "";
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
        private bool pressed;

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
            label = BLRender.Text(
                $"ButtonText_{text}",
                text,
                x,
                y + 1f,
                40,
                Color.white,
                TextAnchor.MiddleCenter,
                80,
                parent,
                BLFontKind.Impact,
                outlineColor: new Color(0f, 0f, 0f, 0.9f),
                outlinePixels: 2f,
                shadowColor: new Color(0f, 0f, 0f, 0.2f),
                shadowOffset: new Vector2(1f, 1f));
        }

        public void Update(Camera camera)
        {
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
    }
}
