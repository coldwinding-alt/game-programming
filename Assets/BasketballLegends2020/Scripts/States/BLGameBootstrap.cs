using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLGameBootstrap : MonoBehaviour
    {
        private readonly List<BLMenuButton> menuButtons = new List<BLMenuButton>();
        private Transform runtimeRoot;
        private BLGameCore gameCore;
        private Camera mainCamera;
        private BLMenuButton difficultyButton;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = BLConstants.GameH / (2f * BLConstants.PixelsPerUnit);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = Color.black;

            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform);
            ShowMenu();
        }

        private void Update()
        {
            if (gameCore != null)
            {
                gameCore.Update(Time.deltaTime);
                if (gameCore.ReturnToMenuRequested || Input.GetKeyDown(KeyCode.Escape))
                {
                    ClearRuntime();
                    ShowMenu();
                }

                return;
            }

            foreach (var button in menuButtons)
            {
                button.Update(mainCamera);
            }
        }

        private void ShowMenu()
        {
            ClearRuntime();
            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform).PlayMusic(BLAssets.Sounds.MenuMusic);

            var bg = Random.value > 0.5f ? "bg10000" : "bg2blue0000";
            BLRender.Sprite("MenuBackground", BLAtlasCache.Instance.Interface, bg, BLConstants.Width2, 240f, 0.5f, 0.5f, 0, runtimeRoot);

            var logoTexture = Resources.Load<Texture2D>("BL2020/Images/logo");
            if (logoTexture != null)
            {
                BLRender.Image("Logo", logoTexture, BLConstants.Width2, 72f, 0.5f, 0.5f, 20, runtimeRoot);
            }

            BLRender.Text(
                "Controls",
                "1P/TRAINING  A/D MOVE  W JUMP  S BLOCK  B SHOOT  Z SUPER\n2P  P1 A/D MOVE  W JUMP  S BLOCK  B SHOOT  V SUPER\n2P  P2 LEFT/RIGHT MOVE  UP JUMP  DOWN BLOCK  L SHOOT  K SUPER",
                BLConstants.Width2,
                436f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                30,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.9f),
                outlinePixels: 1f,
                shadowColor: new Color(0f, 0f, 0f, 0.2f),
                shadowOffset: new Vector2(1f, 1f));

            menuButtons.Clear();
            difficultyButton = new BLMenuButton(BLInventory.Instance.DifficultyLabel, BLConstants.Width2, 130f, 220f, 40f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                difficultyButton.SetText(BLInventory.Instance.DifficultyLabel);
            }, runtimeRoot);
            menuButtons.Add(difficultyButton);

            menuButtons.Add(new BLMenuButton("1 PLAYER", BLConstants.Width2, 185f, 250f, 58f, () =>
            {
                BLInventory.Instance.StartOnePlayer();
                StartGameplay();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("2 PLAYERS", BLConstants.Width2, 250f, 250f, 58f, () =>
            {
                BLInventory.Instance.StartTwoPlayers();
                StartGameplay();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("QUICK MATCH", BLConstants.Width2, 315f, 250f, 58f, () =>
            {
                BLInventory.Instance.StartQuickGame();
                StartGameplay();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("TRAINING", BLConstants.Width2, 380f, 250f, 58f, () =>
            {
                BLInventory.Instance.StartTraining();
                StartGameplay();
            }, runtimeRoot));
        }

        private void StartGameplay()
        {
            ClearRuntime();
            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform).PlayMusic(BLAssets.Sounds.MenuMusic);
            menuButtons.Clear();
            gameCore = new BLGameBuilder().Build(runtimeRoot);
        }

        private void ClearRuntime()
        {
            gameCore = null;
            difficultyButton = null;
            menuButtons.Clear();
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
        }
    }
}
