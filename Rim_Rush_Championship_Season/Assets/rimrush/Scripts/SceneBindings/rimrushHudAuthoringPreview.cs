using UnityEngine;

namespace rimrush
{
    [ExecuteAlways]
    public sealed class rimrushHudAuthoringPreview : MonoBehaviour
    {
        [SerializeField] private rimrushHudSceneView hudView;
        [SerializeField] private rimrushHudPreviewState previewState;

        private void OnEnable()
        {
            ApplyPreview();
        }

        private void OnValidate()
        {
            ApplyPreview();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyPreview();
            }
        }

        private void ApplyPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (hudView == null)
            {
                hudView = GetComponent<rimrushHudSceneView>();
            }

            if (hudView == null)
            {
                return;
            }

            var showPause = previewState == rimrushHudPreviewState.Pause;
            var showCountdown = previewState == rimrushHudPreviewState.Countdown;
            var showPostMatch = previewState == rimrushHudPreviewState.PostMatch;

            SetVisible(hudView.PauseOverlayRoot, showPause);
            SetVisible(hudView.CountdownBackdrop, showCountdown);
            SetVisible(hudView.CountdownCaptionText != null ? hudView.CountdownCaptionText.gameObject : null, showCountdown);
            SetVisible(hudView.CountdownText != null ? hudView.CountdownText.gameObject : null, showCountdown);
            SetVisible(hudView.MessageRoot, false);
            SetVisible(hudView.BonusNoticeRoot, false);
            SetVisible(hudView.PostMatchRoot, showPostMatch);
            SetVisible(hudView.PostMatchTitleText != null ? hudView.PostMatchTitleText.gameObject : null, showPostMatch);
            SetVisible(hudView.PostMatchScoreText != null ? hudView.PostMatchScoreText.gameObject : null, showPostMatch);
            SetVisible(hudView.PostMatchPromptText != null ? hudView.PostMatchPromptText.gameObject : null, showPostMatch);

            var topButtonsVisible = previewState != rimrushHudPreviewState.Pause;
            SetVisible(hudView.PauseButtonView != null ? hudView.PauseButtonView.Root : null, topButtonsVisible);
            SetVisible(hudView.PauseButtonIcon, topButtonsVisible);
            SetVisible(hudView.MusicButtonView != null ? hudView.MusicButtonView.Root : null, topButtonsVisible);
            SetVisible(hudView.HelpButtonView != null ? hudView.HelpButtonView.Root : null, topButtonsVisible);

            if (showCountdown)
            {
                if (hudView.CountdownCaptionText != null)
                {
                    hudView.CountdownCaptionText.text = "RESUMING IN";
                }

                if (hudView.CountdownText != null)
                {
                    hudView.CountdownText.text = "3";
                }
            }
            else
            {
                if (hudView.CountdownCaptionText != null)
                {
                    hudView.CountdownCaptionText.text = string.Empty;
                }

                if (hudView.CountdownText != null)
                {
                    hudView.CountdownText.text = string.Empty;
                }
            }

            if (showPostMatch)
            {
                if (hudView.PostMatchTitleText != null)
                {
                    hudView.PostMatchTitleText.text = "PLAYER 1 WINS";
                }

                if (hudView.PostMatchScoreText != null)
                {
                    hudView.PostMatchScoreText.text = "11 - 8";
                }

                if (hudView.PostMatchPromptText != null)
                {
                    hudView.PostMatchPromptText.text = "CLICK OR PRESS ENTER";
                }
            }
            else
            {
                if (hudView.PostMatchTitleText != null)
                {
                    hudView.PostMatchTitleText.text = string.Empty;
                }

                if (hudView.PostMatchScoreText != null)
                {
                    hudView.PostMatchScoreText.text = string.Empty;
                }

                if (hudView.PostMatchPromptText != null)
                {
                    hudView.PostMatchPromptText.text = string.Empty;
                }
            }
        }

        private static void SetVisible(GameObject target, bool isVisible)
        {
            if (target != null)
            {
                target.SetActive(isVisible);
            }
        }
    }
}
