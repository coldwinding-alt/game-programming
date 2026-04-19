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
            var inventory = BLInventory.Instance;
            if (inventory.IsTournamentActive || inventory.GameMode == 1 || inventory.GameMode == 2)
            {
                postMatchTitleText.text = winner == -1 ? "YOU WIN!" : "YOU LOSE";
            }
            else if (inventory.GameMode == 4 && inventory.MatchData.MatchMode == 2)
            {
                postMatchTitleText.text = winner == -1 ? "TEAM WINS!" : "TEAM LOSES";
            }
            else
            {
                postMatchTitleText.text = winner == -1 ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
            }

            postMatchScoreText.text = $"{leftScore} - {rightScore}";
            postMatchPromptText.text = inventory.IsTournamentActive ? "CLICK TO CONTINUE" : "CLICK OR PRESS ENTER";
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
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.8f),
                outlinePixels: 1f);

            SetCharge(fullTime <= 0f ? 1f : 0f);
        }

        public void SetCharge(float progress)
        {
            overlay.SetProgress(progress);
        }
    }

    public sealed class BLRadialIconMesh
    {
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
            BuildSector(360f * (1f - progress));
        }

        private void BuildSector(float degrees)
        {
            const int maxSegments = 36;
            var segmentCount = Mathf.Max(3, Mathf.CeilToInt(maxSegments * Mathf.Clamp01(degrees / 360f)));
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
