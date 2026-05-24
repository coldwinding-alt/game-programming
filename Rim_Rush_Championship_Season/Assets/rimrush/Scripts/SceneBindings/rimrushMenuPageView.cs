using UnityEngine;

namespace rimrush
{
    public sealed class rimrushMenuPageView : MonoBehaviour
    {
        [SerializeField] private rimrushMenuPageKind pageKind;
        [SerializeField] private string backgroundFrame = "bg10000";
        [SerializeField] private bool showLogo;
        [SerializeField] private GameObject root;
        [SerializeField] private rimrushCharacterSelectorView[] characterSelectors;
        [SerializeField] private rimrushBallSelectorView[] ballSelectors;
        [SerializeField] private rimrushTournamentBracketSceneView tournamentBracketView;
        [SerializeField] private rimrushTournamentAwardsSceneView tournamentAwardsView;

        public rimrushMenuPageKind PageKind => pageKind;
        public string BackgroundFrame => string.IsNullOrEmpty(backgroundFrame) ? "bg10000" : backgroundFrame;
        public bool ShowLogo => showLogo;
        public GameObject Root => root != null ? root : gameObject;
        public rimrushCharacterSelectorView[] CharacterSelectors => characterSelectors;
        public rimrushBallSelectorView[] BallSelectors => ballSelectors;
        public rimrushTournamentBracketSceneView TournamentBracketView => tournamentBracketView;
        public rimrushTournamentAwardsSceneView TournamentAwardsView => tournamentAwardsView;

        public void SetVisible(bool isVisible)
        {
            Root.SetActive(isVisible);
        }
    }
}
