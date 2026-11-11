using UnityEngine;
using UnityEngine.UI;

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
