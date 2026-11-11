using UnityEngine;
using UnityEngine.UI;

/// <summary>Simple layered account-gold icon; not a currency calculation or balance source.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MidnightCoinGraphic : MaskableGraphic
{
    protected override void OnEnable(){base.OnEnable();raycastTarget=false;}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=GetPixelAdjustedRect();float s=Mathf.Min(r.width,r.height);
        Coin(mesh,r.center+new Vector2(-.13f,-.11f)*s,s*.25f);
        Coin(mesh,r.center+new Vector2(.16f,.07f)*s,s*.29f);
    }
    private void Coin(VertexHelper mesh,Vector2 center,float radius)
    {
        Disc(mesh,center+Vector2.down*radius*.17f,radius,new Color(.59f,.32f,.055f));
        Disc(mesh,center,radius,new Color(1f,.78f,.27f));
        Disc(mesh,center,radius*.77f,new Color(.91f,.58f,.10f));
        Disc(mesh,center+new Vector2(-.10f,.12f)*radius,radius*.65f,new Color(1f,.82f,.34f));
        int start=mesh.currentVertCount;float w=radius*.13f,h=radius*.43f;
        mesh.AddVert(center+new Vector2(-w,-h),new Color(.75f,.44f,.06f),Vector2.zero);
        mesh.AddVert(center+new Vector2(-w,h),new Color(.75f,.44f,.06f),Vector2.zero);
        mesh.AddVert(center+new Vector2(w,h),new Color(.75f,.44f,.06f),Vector2.zero);
        mesh.AddVert(center+new Vector2(w,-h),new Color(.75f,.44f,.06f),Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
    private static void Disc(VertexHelper mesh,Vector2 center,float radius,Color tint)
    {
        int start=mesh.currentVertCount;mesh.AddVert(center,tint,Vector2.zero);
        for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);}
        for(int i=0;i<32;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%32);
    }
}
