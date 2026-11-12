using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>Small rounded satin surfaces, using the regular masked UI mesh pipeline.</summary>
public static class MidnightSurfaceMesh
{
    public static void Draw(VertexHelper mesh,Rect bounds,Color surface,Color edge,Color tint,float radius,float scale,bool slanted=false)
    {
        if(bounds.width<8||bounds.height<8)return;
        float pixel=1f/Mathf.Max(.01f,scale);
        Rect body=Inset(bounds,Mathf.Max(2,pixel));
        Rect shadow=body;shadow.y-=Mathf.Min(2,body.height*.04f);
        Fill(mesh,shadow,radius,new Color(0,0,0,.22f),tint,false,slanted);
        Fill(mesh,body,radius,surface,tint,true,slanted);
        Stroke(mesh,body,radius,Mathf.Max(.85f,pixel*.8f),edge,tint,slanted);
        Color inner=new Color(.79f,.87f,1f,.075f);
        Stroke(mesh,Inset(body,Mathf.Max(2,pixel*1.5f)),Mathf.Max(0,radius-2),Mathf.Max(.65f,pixel*.55f),inner,tint,slanted);
    }
    /// <summary>Opt-in bevel/gloss for interactive controls only. Default satin Draw is unchanged.</summary>
    public static void DrawButton(VertexHelper mesh,Rect bounds,Color surface,Color edge,Color tint,float radius,float scale,bool primary,bool slanted)
    {
        Draw(mesh,bounds,surface,edge,tint,radius,scale,slanted);
        if(bounds.width<8||bounds.height<8)return;
        float pixel=1f/Mathf.Max(.01f,scale);
        Rect body=Inset(bounds,Mathf.Max(2,pixel));
        var outline=Outline(body,radius,slanted);
        float h=body.height;
        // Visible upper-third reflection fades before the label's quiet central area.
        float glossBottom=body.yMin+h*.69f;
        AddClipped(mesh,new[]{new Vector2(body.xMin,glossBottom),new Vector2(body.xMax,glossBottom),new Vector2(body.xMax,body.yMax),new Vector2(body.xMin,body.yMax)},outline,
            p=>new Color(.82f,.93f,1f,Mathf.Lerp(0,primary?.35f:.24f,Mathf.InverseLerp(glossBottom,body.yMax,p.y)))*tint);
        float strip=Mathf.Min(h*.085f,Mathf.Max(2.2f,pixel*1.3f));
        AddClipped(mesh,new[]{new Vector2(body.xMin,body.yMax-strip),new Vector2(body.xMax,body.yMax-strip),new Vector2(body.xMax,body.yMax),new Vector2(body.xMin,body.yMax)},outline,
            p=>new Color(.87f,.97f,1f,primary?.78f:.57f)*tint);
        float depth=Mathf.Min(h*.13f,Mathf.Max(4f,pixel*2.5f));
        AddClipped(mesh,new[]{new Vector2(body.xMin,body.yMin),new Vector2(body.xMax,body.yMin),new Vector2(body.xMax,body.yMin+depth),new Vector2(body.xMin,body.yMin+depth)},outline,
            p=>new Color(.005f,.018f,.045f,Mathf.Lerp(.50f,.18f,Mathf.InverseLerp(body.yMin,body.yMin+depth,p.y)))*tint);
        // Static diagonal facet confined above the text. Its alpha is independent of Draw's
        // base fill but receives the same group tint exactly once, including disabled controls.
        float x=body.xMin+body.width*.15f,w=body.width*.22f;
        AddClipped(mesh,new[]{new Vector2(x,body.yMax),new Vector2(x+w,body.yMax),new Vector2(x+w+h*.31f,glossBottom),new Vector2(x+h*.31f,glossBottom)},outline,
            p=>new Color(.92f,.98f,1f,(primary?.12f:.10f)*Mathf.InverseLerp(glossBottom,body.yMax,p.y))*tint);
    }
    /// <summary>Reference-blue soft finish. Header controls continue to use DrawButton above.</summary>
    public static void DrawReferenceButton(VertexHelper mesh,Rect bounds,Color tint,float radius,float scale,bool primary,bool slanted,Color? accent=null,bool symmetricHex=false,bool shopGacha=false)
    {
        if(bounds.width<8||bounds.height<8)return;
        float pixel=1f/Mathf.Max(.01f,scale);var body=Inset(bounds,Mathf.Max(1.5f,pixel*.7f));
        var outline=shopGacha?ShopGachaOutline(body):symmetricHex?MainHexOutline(body):slanted?ReferenceButtonOutline(body):Outline(body,radius,false);
        Color top=primary?new Color(.46f,.94f,1f,1f):new Color(.28f,.42f,.60f,.98f);
        Color bottom=primary?new Color(.11f,.68f,.86f,1f):new Color(.10f,.18f,.30f,.98f);
        if(!primary&&accent.HasValue)
        {
            top=Color.Lerp(accent.Value,Color.white,.23f);bottom=Color.Lerp(accent.Value,Color.black,.12f);
            top.a=bottom.a=accent.Value.a;
        }
        int first=mesh.currentVertCount;Add(mesh,body.center,Color.Lerp(bottom,top,.5f)*tint);
        foreach(var p in outline)Add(mesh,p,Color.Lerp(bottom,top,Mathf.InverseLerp(body.yMin,body.yMax,p.y))*tint);
        for(int i=0;i<outline.Length;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%outline.Length);
        Color edge=primary?new Color(.76f,.98f,1f,.70f):new Color(.61f,.75f,.94f,.55f);
        if(!primary&&accent.HasValue){edge=Color.Lerp(accent.Value,Color.white,.55f);edge.a=.55f*accent.Value.a;}
        StrokePath(mesh,outline,body,Mathf.Max(.8f,pixel*.75f),edge*tint);
        float bevel=Mathf.Min(body.height*.035f,Mathf.Max(1.2f,pixel));
        AddClipped(mesh,new[]{new Vector2(body.xMin,body.yMax-bevel),new Vector2(body.xMax,body.yMax-bevel),new Vector2(body.xMax,body.yMax),new Vector2(body.xMin,body.yMax)},outline,
            p=>new Color(.78f,.94f,1f,.48f)*tint);
        AddClipped(mesh,new[]{new Vector2(body.xMin,body.yMin),new Vector2(body.xMax,body.yMin),new Vector2(body.xMax,body.yMin+bevel),new Vector2(body.xMin,body.yMin+bevel)},outline,
            p=>new Color(.015f,.04f,.08f,.33f)*tint);
        // A small translucent corner reflection, not the old full-width metallic cap.
        float y=body.yMin+body.height*.82f;
        AddClipped(mesh,new[]{new Vector2(body.xMin,body.yMax),new Vector2(body.xMin+body.width*.40f,body.yMax),new Vector2(body.xMin+body.width*.24f,y),new Vector2(body.xMin,y)},outline,
            p=>new Color(.65f,.90f,1f,.10f*Mathf.InverseLerp(y,body.yMax,p.y))*tint);
    }
    public static Vector2[] ReferenceButtonOutline(Rect body)
    {
        float cut=Mathf.Min(body.height*.30f,body.width*.085f);
        return new[]{new Vector2(body.xMin+cut,body.yMax),new Vector2(body.xMax-cut*.18f,body.yMax),new Vector2(body.xMax,body.yMax-cut*.35f),new Vector2(body.xMax-cut,body.yMin),new Vector2(body.xMin+cut*.18f,body.yMin),new Vector2(body.xMin,body.yMin+cut*.35f)};
    }
    public static Vector2[] ShopGachaOutline(Rect r)
    {
        // Two long parallel slanted ends, each capped with two short chamfers.
        float slant=Mathf.Min(r.height*.20f,r.width*.06f),chamfer=Mathf.Min(r.height*.12f,r.width*.035f);
        return new[]{new Vector2(r.xMin+slant+chamfer,r.yMax),new Vector2(r.xMax-chamfer,r.yMax),
            new Vector2(r.xMax,r.yMax-chamfer),new Vector2(r.xMax-slant,r.yMin+chamfer),
            new Vector2(r.xMax-slant-chamfer,r.yMin),new Vector2(r.xMin+chamfer,r.yMin),
            new Vector2(r.xMin,r.yMin+chamfer),new Vector2(r.xMin+slant,r.yMax-chamfer)};
    }
    public static Vector2[] DiscountChipOutline(Rect r)
    {
        float cut=Mathf.Min(r.height*.38f,r.width*.12f);
        return new[]{new Vector2(r.xMin+cut,r.yMax),new Vector2(r.xMax,r.yMax),
            new Vector2(r.xMax-cut,r.yMin),new Vector2(r.xMin,r.yMin)};
    }
    /// <summary>Decorative shop discount chip only; no Button owner or input target required.</summary>
    public static void DrawDiscountChip(VertexHelper mesh,Rect bounds,Color surface,Color tint,float scale)
    {
        if(bounds.width<8||bounds.height<8)return;
        float pixel=1f/Mathf.Max(.01f,scale);var body=Inset(bounds,Mathf.Max(1.5f,pixel*.7f));
        var outline=DiscountChipOutline(body);
        Color top=Color.Lerp(surface,Color.white,.075f),bottom=Color.Lerp(surface,Color.black,.12f);
        top.a=bottom.a=surface.a;
        int start=mesh.currentVertCount;Add(mesh,body.center,Color.Lerp(bottom,top,.5f)*tint);
        foreach(var point in outline)Add(mesh,point,Color.Lerp(bottom,top,Mathf.InverseLerp(body.yMin,body.yMax,point.y))*tint);
        for(int i=0;i<outline.Length;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%outline.Length);
        StrokePath(mesh,outline,body,Mathf.Max(.65f,pixel*.55f),new Color(.45f,.65f,.85f,.26f*surface.a)*tint);
    }
    public static Vector2[] MainHexOutline(Rect body)
    {
        float cut=Mathf.Min(body.height*.25f,body.width*.06f);
        return new[]{new Vector2(body.xMin+cut,body.yMax),new Vector2(body.xMax-cut,body.yMax),new Vector2(body.xMax,body.center.y),new Vector2(body.xMax-cut,body.yMin),new Vector2(body.xMin+cut,body.yMin),new Vector2(body.xMin,body.center.y)};
    }
    private static void StrokePath(VertexHelper mesh,Vector2[] outline,Rect body,float thickness,Color color)
    {
        int start=mesh.currentVertCount;Vector2 scale=new Vector2(1-2*thickness/body.width,1-2*thickness/body.height);
        foreach(var p in outline){Add(mesh,p,color);Add(mesh,body.center+Vector2.Scale(p-body.center,scale),color);}
        for(int i=0;i<outline.Length;i++){int j=(i+1)%outline.Length;mesh.AddTriangle(start+i*2,start+j*2,start+i*2+1);mesh.AddTriangle(start+i*2+1,start+j*2,start+j*2+1);}
    }
    private static void AddClipped(VertexHelper mesh,Vector2[] polygon,Vector2[] boundary,System.Func<Vector2,Color> color)
    {
        var points=new List<Vector2>(polygon);
        // Both public outlines are clockwise; interior is on the right of each edge.
        for(int edge=0;edge<boundary.Length&&points.Count>0;edge++)
        {
            Vector2 a=boundary[edge],b=boundary[(edge+1)%boundary.Length],direction=b-a;
            var result=new List<Vector2>();Vector2 previous=points[points.Count-1];float pd=Cross(direction,previous-a);
            foreach(var current in points)
            {
                float cd=Cross(direction,current-a);bool inside=cd<=.0001f,wasInside=pd<=.0001f;
                if(inside!=wasInside){float divisor=pd-cd;if(Mathf.Abs(divisor)>.000001f)result.Add(Vector2.LerpUnclamped(previous,current,pd/divisor));}
                if(inside)result.Add(current);previous=current;pd=cd;
            }
            points=result;
        }
        if(points.Count<3)return;
        // Clipping at a polygon vertex can repeat adjacent points. Skip zero-area triangles.
        int start=mesh.currentVertCount;foreach(var p in points)Add(mesh,p,color(p));
        for(int i=1;i<points.Count-1;i++)if(Mathf.Abs(Cross(points[i]-points[0],points[i+1]-points[0]))>.01f)mesh.AddTriangle(start,start+i,start+i+1);
    }
    private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    public static Vector2[] Outline(Rect r,float radius,bool slanted)
    {
        float c=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)*.45f);
        if(slanted)return new[]{new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c*.25f,r.yMin+c),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMin,r.yMin+c),new Vector2(r.xMin+c*.25f,r.yMax-c)};
        const int steps=5;var points=new Vector2[4*(steps+1)];
        Vector2[] centers={new Vector2(r.xMax-c,r.yMax-c),new Vector2(r.xMax-c,r.yMin+c),new Vector2(r.xMin+c,r.yMin+c),new Vector2(r.xMin+c,r.yMax-c)};
        for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
        {
            float angle=(90-corner*90-j*90f/steps)*Mathf.Deg2Rad;
            points[corner*(steps+1)+j]=centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*c;
        }
        return points;
    }
    private static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,Mathf.Max(1,r.width-2*n),Mathf.Max(1,r.height-2*n));
    private static Color SurfaceColor(Color baseColor,Vector2 point,Rect rect)
    {
        float x=Mathf.InverseLerp(rect.xMin,rect.xMax,point.x),y=Mathf.InverseLerp(rect.yMin,rect.yMax,point.y);
        float light=.025f+.065f*y+.018f*Mathf.Sin(x*3.1f+y*1.7f);
        Color result=Color.Lerp(baseColor,Color.white,light);
        result=Color.Lerp(result,Color.black,.10f*(1-y));result.a=baseColor.a;return result;
    }
    private static void Fill(VertexHelper mesh,Rect r,float radius,Color color,Color tint,bool shade,bool slanted)
    {
        var outline=Outline(r,radius,slanted);int first=mesh.currentVertCount;
        Add(mesh,r.center,(shade?SurfaceColor(color,r.center,r):color)*tint);
        foreach(var p in outline)Add(mesh,p,(shade?SurfaceColor(color,p,r):color)*tint);
        for(int i=0;i<outline.Length;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%outline.Length);
    }
    private static void Stroke(VertexHelper mesh,Rect r,float radius,float width,Color edge,Color tint,bool slanted)
    {
        var outer=Outline(r,radius,slanted);var inner=Outline(Inset(r,width),Mathf.Max(0,radius-width),slanted);int first=mesh.currentVertCount;
        for(int i=0;i<outer.Length;i++)
        {
            float y=Mathf.InverseLerp(r.yMin,r.yMax,outer[i].y);Color shade=edge;
            shade.a*=Mathf.Lerp(.30f,1f,y*y);Add(mesh,outer[i],shade*tint);Add(mesh,inner[i],shade*tint);
        }
        for(int i=0;i<outer.Length;i++){int next=(i+1)%outer.Length;mesh.AddTriangle(first+i*2,first+next*2,first+i*2+1);mesh.AddTriangle(first+i*2+1,first+next*2,first+next*2+1);}
    }
    private static void Add(VertexHelper mesh,Vector2 position,Color color){var vertex=UIVertex.simpleVert;vertex.position=position;vertex.color=color;mesh.AddVert(vertex);}
}
