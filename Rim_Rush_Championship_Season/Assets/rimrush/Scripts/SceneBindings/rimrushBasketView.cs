using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushBasketView : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private SpriteRenderer basketRenderer;
        [SerializeField] private SpriteRenderer frontEarRenderer;
        [SerializeField] private LineRenderer[] netLines;

        public Transform Root => root != null ? root : transform;
        public SpriteRenderer BasketRenderer => basketRenderer;
        public SpriteRenderer FrontEarRenderer => frontEarRenderer;
        public IReadOnlyList<LineRenderer> NetLines => netLines;

        public static rimrushBasketView CreateRuntimeFallback(int side, Transform parent)
        {
            var container = new GameObject(side == -1 ? "BasketLeftRuntimeView" : "BasketRightRuntimeView");
            container.transform.SetParent(parent, false);
            var view = container.AddComponent<rimrushBasketView>();

            var center = side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;
            var graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(container.transform, false);
            rimrushRender.ApplyPixelTransform(graphic.transform, center, rimrushObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var basket = new GameObject("BasketGraphic");
            basket.transform.SetParent(graphic.transform, false);
            var basketSpriteRenderer = basket.AddComponent<SpriteRenderer>();
            basketSpriteRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BasketGraphic0000", 0.7f, 0.93f);
            basketSpriteRenderer.sortingOrder = 4;

            var frontEar = new GameObject(side == -1 ? "FrontEarLeft" : "FrontEarRight");
            frontEar.transform.SetParent(container.transform, false);
            var frontEarSpriteRenderer = frontEar.AddComponent<SpriteRenderer>();
            frontEarSpriteRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("FrontEar0000", 0.5f, 0.5f);
            frontEarSpriteRenderer.sortingOrder = 60;
            rimrushRender.ApplyPixelTransform(frontEar.transform, center, rimrushObjectsData.BasketHeight);
            frontEar.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var lineList = new List<LineRenderer>();
            for (var i = 0; i < 10; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(container.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault(Texture2D.whiteTexture);
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                lineList.Add(line);
            }

            view.root = graphic.transform;
            view.basketRenderer = basketSpriteRenderer;
            view.frontEarRenderer = frontEarSpriteRenderer;
            view.netLines = lineList.ToArray();
            return view;
        }
    }
}
