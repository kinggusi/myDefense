using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Battle.Presentation
{
    /// <summary>Battle-local metal chamfer frame. No dependency on lobby presentation or raster assets.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleHudGraphic : MaskableGraphic
    {
        public bool Accent;
        public bool OutlineOnly;
        public bool UseDirectOutlineColor;
        public bool Flat;
        public float OutlineWidth=1.2f;
        public static readonly Color Ink = new(.018f, .044f, .073f, .98f);
        public static readonly Color Cyan = new(.30f, .82f, .86f, 1f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            if(Flat&&!OutlineOnly)
            {
                Quad(vh,r,new Color(.018f,.044f,.073f,.92f)*color);
                Quad(vh,new Rect(r.xMin,r.yMin,r.width,1),new Color(.22f,.56f,.62f,.35f)*color);return;
            }
            float cut = Mathf.Min(12f, Mathf.Min(r.width, r.height) * .14f);
            float stroke = Mathf.Max(OutlineWidth, OutlineWidth / Mathf.Max(.01f, canvas == null ? 1 : canvas.scaleFactor));
            Vector2[] outer = Points(r, cut);
            Rect innerRect = Rect.MinMaxRect(r.xMin + stroke, r.yMin + stroke, r.xMax - stroke, r.yMax - stroke);
            Vector2[] inner = Points(innerRect, Mathf.Max(0, cut - stroke));
            Color rim = OutlineOnly&&UseDirectOutlineColor?Color.white:Accent ? Cyan : new Color(.22f, .40f, .47f, .95f);
            if (!OutlineOnly)
            {
                Color fill = Accent ? new Color(.23f, .78f, .89f, 1f) : Ink;
                vh.AddVert(r.center, fill * color, Vector2.zero);
                for (int i = 0; i < inner.Length; i++)
                {
                    Color tint = Accent ? Color.Lerp(new Color(.15f, .72f, .87f), new Color(.40f, .93f, .96f), Mathf.InverseLerp(r.yMin, r.yMax, inner[i].y)) : fill;
                    vh.AddVert(inner[i], tint * color, Vector2.zero);
                }
                for (int i = 0; i < 8; i++) vh.AddTriangle(0, i + 1, (i + 1) % 8 + 1);
            }
            int offset = vh.currentVertCount;
            for (int i = 0; i < 8; i++) { vh.AddVert(outer[i], rim * color, Vector2.zero); vh.AddVert(inner[i], rim * color, Vector2.zero); }
            for (int i = 0; i < 8; i++)
            {
                int a = offset + i * 2, b = offset + (i + 1) % 8 * 2;
                vh.AddTriangle(a, b, a + 1); vh.AddTriangle(b, b + 1, a + 1);
            }
        }
        private static void Quad(VertexHelper vh,Rect r,Color tint)
        {
            int offset=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,r.yMin),tint,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),tint,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),tint,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),tint,Vector2.zero);
            vh.AddTriangle(offset,offset+1,offset+2);vh.AddTriangle(offset,offset+2,offset+3);
        }

        private static Vector2[] Points(Rect r, float cut) => new[]
        {
            new Vector2(r.xMin + cut,r.yMin), new Vector2(r.xMax - cut,r.yMin),
            new Vector2(r.xMax,r.yMin + cut), new Vector2(r.xMax,r.yMax - cut),
            new Vector2(r.xMax - cut,r.yMax), new Vector2(r.xMin + cut,r.yMax),
            new Vector2(r.xMin,r.yMax - cut), new Vector2(r.xMin,r.yMin + cut)
        };
    }
}
