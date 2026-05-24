using UnityEngine;

namespace rimrush
{
    [ExecuteAlways]
    public sealed class rimrushMenuAuthoringPreview : MonoBehaviour
    {
        [SerializeField] private rimrushMenuShellView menuShell;
        [SerializeField] private rimrushMenuPreviewPage previewPage = rimrushMenuPreviewPage.PlayerCount;

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

            if (menuShell == null)
            {
                menuShell = GetComponent<rimrushMenuShellView>();
            }

            if (menuShell == null)
            {
                return;
            }

            rimrushMenuPageView activePage = null;
            var pages = menuShell.Pages;
            if (pages != null)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    if (page == null)
                    {
                        continue;
                    }

                    var visible = page.PageKind == (rimrushMenuPageKind)previewPage;
                    page.SetVisible(visible);
                    if (visible)
                    {
                        activePage = page;
                    }
                }
            }

            if (menuShell.BackgroundRenderer != null)
            {
                var frame = activePage != null ? activePage.BackgroundFrame : "bg10000";
                if (activePage != null && activePage.TournamentBracketView != null)
                {
                    frame = activePage.TournamentBracketView.GetPreviewBackgroundFrame();
                }

                menuShell.BackgroundRenderer.sprite = rimrushAtlasCache.Instance.Interface.Sprite(frame, 0.5f, 0.5f);
                menuShell.BackgroundRenderer.sortingOrder = 0;
                rimrushRender.ApplyPixelTransform(menuShell.BackgroundRenderer.transform, rimrushConstants.Width2, 240f, menuShell.BackgroundRenderer.transform.localPosition.z);
            }

            if (menuShell.LogoRenderer != null)
            {
                menuShell.LogoRenderer.gameObject.SetActive(activePage != null && activePage.ShowLogo);
            }

            if (activePage != null && activePage.TournamentBracketView != null)
            {
                activePage.TournamentBracketView.ApplyPreviewState();
            }
        }
    }
}
