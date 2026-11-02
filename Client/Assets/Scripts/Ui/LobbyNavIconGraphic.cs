using UnityEngine;
using UnityEngine.UI;

/// <summary>Small vector-like SF navigation symbols; no embedded labels or interaction.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class LobbyNavIconGraphic : MaskableGraphic
{
    [SerializeField] private int tabIndex;
    public int TabIndex => tabIndex;
    public void Configure(int index) { tabIndex=index; raycastTarget=false; SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(tabIndex==0)
        {
            Path(mesh,new[]{P(.15f,.25f),P(.15f,.65f),P(.26f,.82f),P(.74f,.82f),P(.85f,.65f),P(.85f,.25f),P(.15f,.25f)});
            Line(mesh,P(.15f,.61f),P(.85f,.61f)); Line(mesh,P(.36f,.80f),P(.32f,.61f));
            Line(mesh,P(.64f,.80f),P(.68f,.61f)); Line(mesh,P(.48f,.54f),P(.52f,.54f),.08f);
            Line(mesh,P(.23f,.34f),P(.39f,.34f)); Line(mesh,P(.61f,.34f),P(.77f,.34f));
        }
        else if(tabIndex==2)
        {
            Sword(mesh,false); Sword(mesh,true);
        }
        else if(tabIndex==3)
        {
            Path(mesh,new[]{P(.5f,.88f),P(.82f,.75f),P(.78f,.42f),P(.66f,.25f),P(.5f,.13f),P(.34f,.25f),P(.22f,.42f),P(.18f,.75f),P(.5f,.88f)});
            foreach(float x in new[]{.35f,.50f,.65f})
            { Circle(mesh,P(x,x==.50f?.64f:.58f),.043f); Line(mesh,P(x,x==.50f?.54f:.48f),P(x,x==.50f?.36f:.34f),.075f); }
        }
        else if(tabIndex==4)
        {
            Line(mesh,P(.5f,.80f),P(.20f,.27f)); Line(mesh,P(.20f,.27f),P(.80f,.27f)); Line(mesh,P(.80f,.27f),P(.5f,.80f));
            Circle(mesh,P(.5f,.80f),.10f); Circle(mesh,P(.20f,.27f),.10f); Circle(mesh,P(.80f,.27f),.10f);
        }
    }
    private Vector2 P(float x,float y)
    { var r=GetPixelAdjustedRect(); float s=Mathf.Min(r.width,r.height); return r.center+new Vector2(x-.5f,y-.5f)*s; }
    private void Sword(VertexHelper mesh,bool flip)
    {
        float sign=flip?-1:1;
        float size=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
        Vector2 tip=P(.5f+sign*.29f,.82f),guard=P(.5f-sign*.16f,.37f);
        Vector2 axis=(tip-guard).normalized;
        Vector2 normal=new Vector2(-axis.y,axis.x);
        Vector2 shoulder=tip-axis*size*.10f;
        Vector2 edge=normal*size*.040f;
        // A pointed blade, narrow grip and perpendicular crossguard read as swords,
        // rather than two identical diagonal strokes forming an X.
        int start=mesh.currentVertCount;
        V(mesh,guard-edge);V(mesh,shoulder-edge);V(mesh,tip);V(mesh,shoulder+edge);V(mesh,guard+edge);
        mesh.AddTriangle(start,start+1,start+2);
        mesh.AddTriangle(start,start+2,start+3);
        mesh.AddTriangle(start,start+3,start+4);
        Line(mesh,guard,guard-axis*size*.18f,.041f);
        Vector2 crossguard=normal*size*.12f;
        Line(mesh,guard-crossguard,guard+crossguard,.045f);
        Circle(mesh,guard-axis*size*.20f,.029f);
    }
    private void Circle(VertexHelper m,Vector2 c,float radius)
    {
        float scale=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
        Vector2 prev=c+Vector2.right*radius*scale;
        for(int i=1;i<=24;i++) { float a=i*Mathf.PI*2/24; Vector2 next=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*scale; Line(m,prev,next,.027f); prev=next; }
    }
    private void Path(VertexHelper m,Vector2[] p) { for(int i=1;i<p.Length;i++) Line(m,p[i-1],p[i]); }
    private void Line(VertexHelper m,Vector2 a,Vector2 b,float width=.033f)
    {
        Vector2 delta=(b-a).normalized; Vector2 n=new Vector2(-delta.y,delta.x)*width*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        int s=m.currentVertCount; V(m,a-n);V(m,a+n);V(m,b+n);V(m,b-n);m.AddTriangle(s,s+1,s+2);m.AddTriangle(s,s+2,s+3);
    }
    private void V(VertexHelper m,Vector2 p) {var v=UIVertex.simpleVert;v.position=p;v.color=color;m.AddVert(v);}
}
