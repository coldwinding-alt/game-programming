using UnityEngine;

namespace rimrush
{
    public sealed class rimrushBallSelectorView : MonoBehaviour
    {
        [SerializeField] private TextMesh headerText;
        [SerializeField] private rimrushMenuButtonView previousButtonView;
        [SerializeField] private rimrushMenuButtonView nextButtonView;
        [SerializeField] private SpriteRenderer previewRenderer;
        [SerializeField] private TextMesh labelText;

        public TextMesh HeaderText => headerText;
        public rimrushMenuButtonView PreviousButtonView => previousButtonView;
        public rimrushMenuButtonView NextButtonView => nextButtonView;
        public SpriteRenderer PreviewRenderer => previewRenderer;
        public TextMesh LabelText => labelText;
    }
}
