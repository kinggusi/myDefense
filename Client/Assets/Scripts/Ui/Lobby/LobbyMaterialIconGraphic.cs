using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Lobby
{
    /// <summary>Small resolution-independent material symbols; no imported art dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LobbyMaterialIconGraphic : MaskableGraphic
    {
        public enum MaterialKind { WakjeoDna, GrowthCell }
        [SerializeField] private MaterialKind kind;
        public MaterialKind Kind => kind;

        public void Configure(MaterialKind value)
        {
            kind = value;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width < 1f || r.height < 1f) return;
            Vector2 c = r.center;
            float s = Mathf.Min(r.width, r.height);
            if (kind == MaterialKind.WakjeoDna)
            {
                Color a = new Color(0.58f, 0.84f, 0.91f);
                Color b = new Color(0.69f, 0.62f, 0.87f);
                for (int i = 0; i < 24; i++)
                {
                    float t0 = i / 24f, t1 = (i + 1) / 24f;
                    Line(vh, Dna(c,s,t0,1f), Dna(c,s,t1,1f), s * .055f, a);
                    Line(vh, Dna(c,s,t0,-1f), Dna(c,s,t1,-1f), s * .055f, b);
                    if (i % 4 == 0) Line(vh, Dna(c,s,t0,1f), Dna(c,s,t0,-1f), s*.035f, new Color(.48f,.72f,.81f,.65f));
                }
            }
            else
            {
                Disc(vh,c,s*.40f,new Color(.27f,.56f,.47f,.35f));
                for (int i=0;i<32;i++)
                {
                    float a=i*Mathf.PI*2/32, b=(i+1)*Mathf.PI*2/32;
                    Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*s*.38f,
                        c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*s*.38f,s*.045f,new Color(.52f,.82f,.66f));
                }
                Disc(vh,c+new Vector2(-.04f,.03f)*s,s*.15f,new Color(.70f,.86f,.60f));
                Disc(vh,c+new Vector2(.19f,-.14f)*s,s*.05f,new Color(.41f,.72f,.59f));
                Disc(vh,c+new Vector2(-.19f,-.15f)*s,s*.04f,new Color(.41f,.72f,.59f));
            }
        }

        private static Vector2 Dna(Vector2 c,float s,float t,float side) =>
            c+new Vector2(Mathf.Sin(t*Mathf.PI*2)*s*.23f*side,(t-.5f)*s*.82f);

        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 d=(b-a).normalized;Vector2 n=new Vector2(-d.y,d.x)*width*.5f;
            int start=vh.currentVertCount;
            Vertex(vh,a-n,tint);Vertex(vh,a+n,tint);Vertex(vh,b+n,tint);Vertex(vh,b-n,tint);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }

        private void Disc(VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int start=vh.currentVertCount;Vertex(vh,center,tint);
            for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;Vertex(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint);}
            for(int i=0;i<24;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%24);
        }

        private void Vertex(VertexHelper vh,Vector2 p,Color tint)
        {
            UIVertex v=UIVertex.simpleVert;v.position=p;v.color=tint*color;vh.AddVert(v);
        }
    }
}
