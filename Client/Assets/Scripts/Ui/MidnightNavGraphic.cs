using UnityEngine;
using UnityEngine.UI;

/// <summary>Filled lobby-only navigation symbols; existing buttons keep their callbacks.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MidnightNavGraphic : MaskableGraphic
{
    public int TabIndex {get;private set;}
    public void Configure(int index){TabIndex=index;raycastTarget=false;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(TabIndex==0)
        {
            Polygon(vh,new[]{P(.13f,.58f),P(.25f,.84f),P(.75f,.84f),P(.87f,.58f)});
            Polygon(vh,new[]{P(.17f,.51f),P(.83f,.51f),P(.78f,.19f),P(.22f,.19f)});
            Bar(vh,P(.5f,.48f),P(.5f,.33f),.075f,new Color(.04f,.07f,.12f,color.a));
        }
        else if(TabIndex==2){Sword(vh,false);Sword(vh,true);}
        else if(TabIndex==3)
        {
            var points=new[]{P(.5f,.91f),P(.86f,.75f),P(.79f,.36f),P(.5f,.10f),P(.21f,.36f),P(.14f,.75f)};
            Ring(vh,points,.72f);
            Disc(vh,P(.5f,.61f),.07f);Bar(vh,P(.5f,.49f),P(.5f,.31f),.13f,color);
        }
        else if(TabIndex==4)
        {
            Bar(vh,P(.5f,.79f),P(.2f,.25f),.065f,color);Bar(vh,P(.2f,.25f),P(.8f,.25f),.065f,color);Bar(vh,P(.8f,.25f),P(.5f,.79f),.065f,color);
            CircleRing(vh,P(.5f,.79f),.12f);CircleRing(vh,P(.2f,.25f),.12f);CircleRing(vh,P(.8f,.25f),.12f);
        }
    }
    private Vector2 P(float x,float y){var r=GetPixelAdjustedRect();float s=Mathf.Min(r.width,r.height);return r.center+new Vector2(x-.5f,y-.5f)*s;}
    private float Scale=>Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
    private void Sword(VertexHelper vh,bool flip)
    {
        float sign=flip?-1:1;Vector2 guard=P(.5f-sign*.16f,.36f),tip=P(.5f+sign*.32f,.88f),axis=(tip-guard).normalized,n=new Vector2(-axis.y,axis.x)*Scale;
        Polygon(vh,new[]{guard-n*.055f,tip-axis*Scale*.14f-n*.055f,tip,tip-axis*Scale*.14f+n*.055f,guard+n*.055f});
        Bar(vh,guard-n*.14f,guard+n*.14f,.065f,color);Bar(vh,guard,guard-axis*Scale*.23f,.075f,color);
    }
    private void Bar(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
    {
        Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*Scale*width*.5f;
        Polygon(vh,new[]{a-n,a+n,b+n,b-n},tint);
    }
    private void Disc(VertexHelper vh,Vector2 center,float radius)
    {var p=new Vector2[24];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Scale*radius;}Polygon(vh,p);}
    private void CircleRing(VertexHelper vh,Vector2 center,float radius)
    {
        var p=new Vector2[24];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Scale*radius;}
        Ring(vh,p,.48f);
    }
    private void Ring(VertexHelper vh,Vector2[] points,float inner)
    {
        Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
        int start=vh.currentVertCount;foreach(var p in points){vh.AddVert(p,color,Vector2.zero);vh.AddVert(Vector2.Lerp(center,p,inner),color,Vector2.zero);}
        for(int i=0;i<points.Length;i++){int j=(i+1)%points.Length;vh.AddTriangle(start+i*2,start+j*2,start+i*2+1);vh.AddTriangle(start+i*2+1,start+j*2,start+j*2+1);}
    }
    private void Polygon(VertexHelper vh,Vector2[] points)=>Polygon(vh,points,color);
    private static void Polygon(VertexHelper vh,Vector2[] points,Color tint)
    {int start=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
}
