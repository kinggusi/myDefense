using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Clans
{
    /// <summary>Clan-only low-contrast surfaces. No shared lobby theme or material is modified.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ClanSurfaceGraphic : MaskableGraphic
    {
        public enum Shape { Panel, Shield, Circle }
        public Shape shape;
        public Color border = new Color(.22f, .36f, .49f, .65f);
        public float thickness = 1.6f;
        public float corner = 16;

        public Color ResolveFill(bool midnight) => midnight&&shape==Shape.Panel
            ? new Color(MidnightLobbyTheme.Surface.r,MidnightLobbyTheme.Surface.g,MidnightLobbyTheme.Surface.b,color.a):color;
        public Color ResolveBorder(bool midnight) => midnight&&shape==Shape.Panel?new Color(.36f,.44f,.58f,.5f):border;
        public float ResolveCorner(bool midnight) => midnight&&shape==Shape.Panel?10:corner;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            bool midnight=MidnightLobbyTheme.IsPresentationTarget(transform);
            if(midnight&&shape==Shape.Panel)
            {
                DrawMidnightPanel(vh,r);
                return;
            }
            Vector2[] outer = Points(r, 0,ResolveCorner(midnight));
            Vector2[] inner = Points(r, Mathf.Clamp(thickness, 0, Mathf.Min(r.width, r.height) * .2f),ResolveCorner(midnight));
            Color32 edge = ResolveBorder(midnight);
            edge.a = (byte)(edge.a * color.a);
            for (int i = 0; i < outer.Length; i++)
            {
                int next = (i + 1) % outer.Length;
                int start = vh.currentVertCount;
                vh.AddVert(outer[i], edge, Vector2.zero); vh.AddVert(outer[next], edge, Vector2.zero);
                vh.AddVert(inner[next], edge, Vector2.zero); vh.AddVert(inner[i], edge, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
            int center = vh.currentVertCount;
            Color fill=ResolveFill(midnight);
            vh.AddVert(r.center, fill, Vector2.zero);
            for (int i = 0; i < inner.Length; i++) vh.AddVert(inner[i], fill, Vector2.zero);
            for (int i = 0; i < inner.Length; i++) vh.AddTriangle(center, center + 1 + i, center + 1 + (i + 1) % inner.Length);
        }

        private void DrawMidnightPanel(VertexHelper mesh,Rect bounds)
        {
            // The authored Graphic alpha is a group opacity for every surface layer.
            // Keep fill opaque here, then multiply once through tint (also rim/shadow/highlight).
            Color fill=ResolveFill(true);fill.a=1f;
            Color tint=Color.white;tint.a=color.a;
            MidnightSurfaceMesh.Draw(mesh,bounds,fill,ResolveBorder(true),tint,ResolveCorner(true),canvas!=null?canvas.scaleFactor:1);
        }

        private Vector2[] Points(Rect r, float inset,float resolvedCorner)
        {
            float x0 = r.xMin + inset, x1 = r.xMax - inset, y0 = r.yMin + inset, y1 = r.yMax - inset;
            if (shape == Shape.Shield)
                return new[] { new Vector2(r.center.x, y1), new Vector2(x1, Mathf.Lerp(y0, y1, .8f)), new Vector2(x1, Mathf.Lerp(y0, y1, .3f)), new Vector2(r.center.x, y0), new Vector2(x0, Mathf.Lerp(y0, y1, .3f)), new Vector2(x0, Mathf.Lerp(y0, y1, .8f)) };
            if (shape == Shape.Circle)
            {
                var result = new Vector2[32];
                for (int i = 0; i < result.Length; i++)
                {
                    float a = i * Mathf.PI * 2 / result.Length;
                    result[i] = r.center + new Vector2(Mathf.Cos(a) * (r.width * .5f - inset), Mathf.Sin(a) * (r.height * .5f - inset));
                }
                return result;
            }
            float c = Mathf.Clamp(resolvedCorner - inset, 0, Mathf.Min(x1 - x0, y1 - y0) * .25f);
            return new[] { new Vector2(x0 + c, y1), new Vector2(x1 - c, y1), new Vector2(x1, y1 - c), new Vector2(x1, y0 + c), new Vector2(x1 - c, y0), new Vector2(x0 + c, y0), new Vector2(x0, y0 + c), new Vector2(x0, y1 - c) };
        }
    }
}
