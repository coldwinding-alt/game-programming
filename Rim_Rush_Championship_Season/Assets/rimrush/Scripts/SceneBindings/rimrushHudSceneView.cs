using UnityEngine;

namespace rimrush
{
    public sealed class rimrushHudSceneView : MonoBehaviour
    {
        [SerializeField] private GameObject scoreboardBackdrop;
        [SerializeField] private SpriteRenderer leftPortraitAura;
        [SerializeField] private SpriteRenderer rightPortraitAura;
        [SerializeField] private SpriteRenderer leftPortraitRenderer;
        [SerializeField] private SpriteRenderer rightPortraitRenderer;
        [SerializeField] private TextMesh leftNameText;
        [SerializeField] private TextMesh rightNameText;
        [SerializeField] private TextMesh leftScoreText;
        [SerializeField] private TextMesh rightScoreText;
        [SerializeField] private TextMesh timerText;
        [SerializeField] private rimrushMenuButtonView pauseButtonView;
        [SerializeField] private GameObject pauseButtonIcon;
        [SerializeField] private rimrushIconButtonView musicButtonView;
        [SerializeField] private rimrushIconButtonView helpButtonView;
        [SerializeField] private GameObject countdownBackdrop;
        [SerializeField] private TextMesh countdownCaptionText;
        [SerializeField] private TextMesh countdownText;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private TextMesh messageText;
        [SerializeField] private GameObject bonusNoticeRoot;
        [SerializeField] private TextMesh bonusNoticeText;
        [SerializeField] private GameObject postMatchRoot;
        [SerializeField] private TextMesh postMatchTitleText;
        [SerializeField] private TextMesh postMatchScoreText;
        [SerializeField] private TextMesh postMatchPromptText;
        [SerializeField] private GameObject pauseOverlayRoot;
        [SerializeField] private GameObject pauseShade;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private TextMesh pauseTitleText;
        [SerializeField] private TextMesh pauseScoreText;
        [SerializeField] private TextMesh pauseLeftNameText;
        [SerializeField] private TextMesh pauseRightNameText;
        [SerializeField] private TextMesh pauseLeftScoreText;
        [SerializeField] private TextMesh pauseRightScoreText;
        [SerializeField] private TextMesh pauseScoreDividerText;
        [SerializeField] private SpriteRenderer pauseLeftPortraitRenderer;
        [SerializeField] private SpriteRenderer pauseRightPortraitRenderer;
        [SerializeField] private rimrushMenuButtonView pauseMenuButtonView;
        [SerializeField] private rimrushMenuButtonView pauseResumeButtonView;

        public GameObject ScoreboardBackdrop => scoreboardBackdrop;
        public SpriteRenderer LeftPortraitAura => leftPortraitAura;
        public SpriteRenderer RightPortraitAura => rightPortraitAura;
        public SpriteRenderer LeftPortraitRenderer => leftPortraitRenderer;
        public SpriteRenderer RightPortraitRenderer => rightPortraitRenderer;
        public TextMesh LeftNameText => leftNameText;
        public TextMesh RightNameText => rightNameText;
        public TextMesh LeftScoreText => leftScoreText;
        public TextMesh RightScoreText => rightScoreText;
        public TextMesh TimerText => timerText;
        public rimrushMenuButtonView PauseButtonView => pauseButtonView;
        public GameObject PauseButtonIcon => pauseButtonIcon;
        public rimrushIconButtonView MusicButtonView => musicButtonView;
        public rimrushIconButtonView HelpButtonView => helpButtonView;
        public GameObject CountdownBackdrop => countdownBackdrop;
        public TextMesh CountdownCaptionText => countdownCaptionText;
        public TextMesh CountdownText => countdownText;
        public GameObject MessageRoot => messageRoot;
        public TextMesh MessageText => messageText;
        public GameObject BonusNoticeRoot => bonusNoticeRoot;
        public TextMesh BonusNoticeText => bonusNoticeText;
        public GameObject PostMatchRoot => postMatchRoot;
        public TextMesh PostMatchTitleText => postMatchTitleText;
        public TextMesh PostMatchScoreText => postMatchScoreText;
        public TextMesh PostMatchPromptText => postMatchPromptText;
        public GameObject PauseOverlayRoot => pauseOverlayRoot;
        public GameObject PauseShade => pauseShade;
        public GameObject PausePanel => pausePanel;
        public TextMesh PauseTitleText => pauseTitleText;
        public TextMesh PauseScoreText => pauseScoreText;
        public TextMesh PauseLeftNameText => pauseLeftNameText;
        public TextMesh PauseRightNameText => pauseRightNameText;
        public TextMesh PauseLeftScoreText => pauseLeftScoreText;
        public TextMesh PauseRightScoreText => pauseRightScoreText;
        public TextMesh PauseScoreDividerText => pauseScoreDividerText;
        public SpriteRenderer PauseLeftPortraitRenderer => pauseLeftPortraitRenderer;
        public SpriteRenderer PauseRightPortraitRenderer => pauseRightPortraitRenderer;
        public rimrushMenuButtonView PauseMenuButtonView => pauseMenuButtonView;
        public rimrushMenuButtonView PauseResumeButtonView => pauseResumeButtonView;
    }
}
