using UnityEngine;

namespace rimrush
{
    public sealed class rimrushCharacterSelectorView : MonoBehaviour
    {
        [SerializeField] private TextMesh headerText;
        [SerializeField] private rimrushMenuButtonView previousButtonView;
        [SerializeField] private rimrushMenuButtonView nextButtonView;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private Transform previewMount;
        [SerializeField] private TextMesh nameText;

        public TextMesh HeaderText => headerText;
        public rimrushMenuButtonView PreviousButtonView => previousButtonView;
        public rimrushMenuButtonView NextButtonView => nextButtonView;
        public SpriteRenderer ShadowRenderer => shadowRenderer;
        public Transform PreviewMount => previewMount != null ? previewMount : transform;
        public TextMesh NameText => nameText;
    }
}
