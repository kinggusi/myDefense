using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reference-shaped navigation only; does not own callbacks or unit portraits.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MidnightNavGraphic : MaskableGraphic
{
    public int TabIndex {get;private set;}
    public bool Selected {get;private set;}
    private static readonly Color Cutout=new Color(.035f,.055f,.095f,1);
    public void Configure(int index){TabIndex=index;raycastTarget=false;SetVerticesDirty();}
    public void SetSelected(bool selected)
    {Selected=selected;color=selected?new Color(.39f,.94f,.98f):new Color(.52f,.59f,.70f);SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        switch(TabIndex){case 0:Crate(vh);break;case 1:Alien(vh);break;case 2:Sword(vh,false);Sword(vh,true);break;case 3:Shield(vh);break;case 4:Nodes(vh);break;case 5:Clipboard(vh);break;case 6:Calendar(vh);break;case 7:GraduationCap(vh);break;case 8:PlaySquare(vh);break;case 9:Refresh(vh);break;}
        if(!Selected)return;
        var stream=new List<UIVertex>();vh.GetUIVertexStream(stream);vh.Clear();
        var glow=new List<UIVertex>(stream.Count);Vector2 center=rectTransform.rect.center;
        foreach(var original in stream){var v=original;v.position=center+((Vector2)v.position-center)*1.11f;v.color=new Color(color.r,color.g,color.b,.075f*color.a);glow.Add(v);}
        vh.AddUIVertexTriangleStream(glow);vh.AddUIVertexTriangleStream(stream);
    }
    private void Crate(VertexHelper vh)
    {
        Poly(vh,new[]{P(.13f,.57f),P(.23f,.82f),P(.39f,.82f),P(.35f,.57f)});
        Poly(vh,new[]{P(.40f,.57f),P(.43f,.82f),P(.59f,.82f),P(.63f,.57f)});
        Poly(vh,new[]{P(.68f,.57f),P(.64f,.82f),P(.79f,.82f),P(.89f,.57f)});
        Poly(vh,new[]{P(.18f,.51f),P(.84f,.51f),P(.80f,.22f),P(.73f,.15f),P(.29f,.15f),P(.22f,.22f)});
        Bar(vh,P(.40f,.53f),P(.40f,.34f),.055f,Cutout);Bar(vh,P(.62f,.53f),P(.62f,.34f),.055f,Cutout);Bar(vh,P(.40f,.34f),P(.62f,.34f),.055f,Cutout);
    }
    private void Alien(VertexHelper vh)
    {
        var head=new Vector2[40];for(int i=0;i<head.Length;i++){float a=i*Mathf.PI*2/head.Length;head[i]=P(.48f+Mathf.Cos(a)*.35f,.48f+Mathf.Sin(a)*.27f+Mathf.Cos(a)*.09f);}
        Poly(vh,head);Poly(vh,new[]{P(.72f,.69f),P(.87f,.88f),P(.84f,.65f)});Poly(vh,new[]{P(.18f,.55f),P(.09f,.65f),P(.13f,.39f)});
        Eye(vh,new Vector2(.34f,.47f),-.50f);Eye(vh,new Vector2(.62f,.54f),.50f);
    }
    private void Eye(VertexHelper vh,Vector2 center,float slope)
    {
        var points=new Vector2[16];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length,x=Mathf.Cos(a)*.105f,y=Mathf.Sin(a)*.055f;points[i]=P(center.x+x,center.y+y+slope*x);}
        Poly(vh,points,Cutout);
    }
    private void Shield(VertexHelper vh)
    {
        var outer=new[]{P(.50f,.91f),P(.86f,.76f),P(.79f,.38f),P(.66f,.22f),P(.50f,.10f),P(.34f,.22f),P(.21f,.38f),P(.14f,.76f)};Ring(vh,outer,.79f);
        var inner=new Vector2[outer.Length];Vector2 center=P(.50f,.52f);for(int i=0;i<inner.Length;i++)inner[i]=Vector2.Lerp(center,outer[i],.65f);Poly(vh,inner);
    }
    private void Nodes(VertexHelper vh)
    {
        Bar(vh,P(.5f,.78f),P(.23f,.28f),.035f,color);Bar(vh,P(.23f,.28f),P(.77f,.28f),.035f,color);Bar(vh,P(.77f,.28f),P(.5f,.78f),.035f,color);
        Circle(vh,P(.5f,.78f),.115f);Circle(vh,P(.23f,.28f),.115f);Circle(vh,P(.77f,.28f),.115f);
    }
    private void Clipboard(VertexHelper vh)
    {
        Ring(vh,new[]{P(.24f,.81f),P(.76f,.81f),P(.76f,.16f),P(.24f,.16f)},.78f);
        Poly(vh,new[]{P(.36f,.88f),P(.64f,.88f),P(.64f,.72f),P(.36f,.72f)});
        Bar(vh,P(.34f,.55f),P(.66f,.55f),.055f,color);Bar(vh,P(.34f,.39f),P(.62f,.39f),.055f,color);
    }
    private void Calendar(VertexHelper vh)
    {
        Ring(vh,new[]{P(.15f,.78f),P(.85f,.78f),P(.85f,.15f),P(.15f,.15f)},.79f);
        Bar(vh,P(.18f,.62f),P(.82f,.62f),.07f,color);Bar(vh,P(.33f,.89f),P(.33f,.70f),.08f,color);Bar(vh,P(.67f,.89f),P(.67f,.70f),.08f,color);
        for(int row=0;row<2;row++)for(int col=0;col<3;col++){float x=.30f+col*.19f,y=.46f-row*.16f;Poly(vh,new[]{P(x-.04f,y-.04f),P(x+.04f,y-.04f),P(x+.04f,y+.04f),P(x-.04f,y+.04f)});}
    }
    private void GraduationCap(VertexHelper vh)
    {
        Poly(vh,new[]{P(.08f,.65f),P(.50f,.85f),P(.92f,.65f),P(.50f,.44f)});
        Poly(vh,new[]{P(.27f,.45f),P(.50f,.33f),P(.73f,.45f),P(.73f,.27f),P(.50f,.19f),P(.27f,.27f)});
        Bar(vh,P(.87f,.63f),P(.87f,.27f),.05f,color);
    }
    private void PlaySquare(VertexHelper vh)
    {
        Ring(vh,new[]{P(.12f,.84f),P(.88f,.84f),P(.88f,.16f),P(.12f,.16f)},.77f);
        Poly(vh,new[]{P(.41f,.70f),P(.69f,.50f),P(.41f,.30f)});
    }
    private void Refresh(VertexHelper vh)
    {
        Vector2 previous=P(.77f,.64f);
        for(int i=1;i<=24;i++){float a=(28+290f*i/24)*Mathf.Deg2Rad;var point=P(.50f+Mathf.Cos(a)*.30f,.50f+Mathf.Sin(a)*.30f);Bar(vh,previous,point,.065f,color);previous=point;}
        Poly(vh,new[]{P(.66f,.86f),P(.87f,.64f),P(.61f,.63f)});
    }
    private void Sword(VertexHelper vh,bool flip)
    {
        float sign=flip?-1:1;Vector2 guard=P(.5f-sign*.16f,.35f),tip=P(.5f+sign*.33f,.88f),axis=(tip-guard).normalized,n=new Vector2(-axis.y,axis.x)*Scale;
        Poly(vh,new[]{guard-n*.055f,tip-axis*Scale*.15f-n*.07f,tip,tip-axis*Scale*.15f+n*.07f,guard+n*.055f});
        Bar(vh,guard-n*.14f,guard+n*.14f,.065f,color);Bar(vh,guard,guard-axis*Scale*.23f,.07f,color);Bar(vh,guard-axis*Scale*.22f-n*.055f,guard-axis*Scale*.22f+n*.055f,.06f,color);
    }
    private Vector2 P(float x,float y){var r=GetPixelAdjustedRect();float s=Mathf.Min(r.width,r.height);return r.center+new Vector2(x-.5f,y-.5f)*s;}
    private float Scale=>Mathf.Min(rectTransform.rect.width,rectTransform.rect.height);
    private void Bar(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
    {Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*Scale*width*.5f;Poly(vh,new[]{a-n,a+n,b+n,b-n},tint);}
    private void Circle(VertexHelper vh,Vector2 center,float radius)
    {
        var points=new Vector2[40];for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Scale*radius;}Poly(vh,points,Cutout);Ring(vh,points,.61f);
    }
    private void Ring(VertexHelper vh,Vector2[] points,float inner)
    {
        Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
        int start=vh.currentVertCount;foreach(var p in points){vh.AddVert(p,color,Vector2.zero);vh.AddVert(Vector2.Lerp(center,p,inner),color,Vector2.zero);}
        for(int i=0;i<points.Length;i++){int j=(i+1)%points.Length;vh.AddTriangle(start+i*2,start+j*2,start+i*2+1);vh.AddTriangle(start+i*2+1,start+j*2,start+j*2+1);}
    }
    private void Poly(VertexHelper vh,Vector2[] points)=>Poly(vh,points,color);
    private static void Poly(VertexHelper vh,Vector2[] points,Color tint)
    {int start=vh.currentVertCount;foreach(var p in points)vh.AddVert(p,tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
}
