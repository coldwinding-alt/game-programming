using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushMenuShellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer logoRenderer;
        [SerializeField] private Transform dynamicContentRoot;
        [SerializeField] private Transform pageCatalog;
        [SerializeField] private rimrushMenuButtonView[] buttonPool;
        [SerializeField] private rimrushMenuPageView[] pages;
        [SerializeField] private rimrushIconButtonView musicButton;
        [SerializeField] private rimrushIconButtonView helpButton;

        public SpriteRenderer BackgroundRenderer => backgroundRenderer;
        public SpriteRenderer LogoRenderer => logoRenderer;
        public Transform DynamicContentRoot => dynamicContentRoot != null ? dynamicContentRoot : transform;
        public Transform PageCatalog => pageCatalog != null ? pageCatalog : transform;
        public rimrushIconButtonView MusicButton => musicButton;
        public rimrushIconButtonView HelpButton => helpButton;
        public IReadOnlyList<rimrushMenuButtonView> ButtonPool => buttonPool;
        public IReadOnlyList<rimrushMenuPageView> Pages => pages;

        public rimrushMenuButtonView GetButtonView(int index)
        {
            if (buttonPool == null || index < 0 || index >= buttonPool.Length)
            {
                return null;
            }

            return buttonPool[index];
        }

        public rimrushMenuPageView GetPage(rimrushMenuPageKind pageKind)
        {
            if (pages == null)
            {
                return null;
            }

            for (var i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null && pages[i].PageKind == pageKind)
                {
                    return pages[i];
                }
            }

            return null;
        }
    }
}
