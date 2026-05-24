using UnityEngine;

namespace rimrush
{
    public sealed class rimrushTournamentBracketSceneView : MonoBehaviour
    {
        [SerializeField] private GameObject regularSeasonBoardRoot;
        [SerializeField] private GameObject playoffBoardRoot;
        [SerializeField] private GameObject completedBoardRoot;
        [SerializeField] private rimrushTournamentBracketPreviewState previewState;

        public GameObject RegularSeasonBoardRoot => regularSeasonBoardRoot;
        public GameObject PlayoffBoardRoot => playoffBoardRoot;
        public GameObject CompletedBoardRoot => completedBoardRoot;
        public rimrushTournamentBracketPreviewState PreviewState => previewState;

        public string GetPreviewBackgroundFrame()
        {
            return previewState == rimrushTournamentBracketPreviewState.RegularSeason
                ? "bg2blue0000"
                : "bg10000";
        }

        public void SetPreviewState(rimrushTournamentBracketPreviewState state)
        {
            previewState = state;
            ApplyPreviewState();
        }

        public void ApplyPreviewState()
        {
            if (Application.isPlaying)
            {
                return;
            }

            SetVisible(regularSeasonBoardRoot, previewState == rimrushTournamentBracketPreviewState.RegularSeason);
            SetVisible(playoffBoardRoot, previewState == rimrushTournamentBracketPreviewState.Playoffs);
            SetVisible(completedBoardRoot, previewState == rimrushTournamentBracketPreviewState.Completed);
        }

        private void OnEnable()
        {
            ApplyPreviewState();
        }

        private void OnValidate()
        {
            ApplyPreviewState();
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
