using UnityEngine;
using UnityEngine.UI;

/// <summary>Cover-crops a non-interactive RawImage without changing its containing layout.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class MidnightArtworkCrop : MonoBehaviour
{
    public Vector2 focalPoint=new Vector2(.5f,.5f);
    private void OnEnable()=>Refresh();
    private void OnRectTransformDimensionsChange()=>Refresh();
    public static Rect CoverUv(float sourceAspect,float targetAspect)
        => CoverUv(sourceAspect,targetAspect,new Vector2(.5f,.5f));
    public static Rect CoverUv(float sourceAspect,float targetAspect,Vector2 focal)
    {
        if(sourceAspect<=0||targetAspect<=0)return new Rect(0,0,1,1);
        if(sourceAspect>targetAspect) {float w=targetAspect/sourceAspect;return new Rect((1-w)*Mathf.Clamp01(focal.x),0,w,1);}
        float h=sourceAspect/targetAspect;return new Rect(0,(1-h)*Mathf.Clamp01(focal.y),1,h);
    }
    public void Refresh()
    {
        var image=GetComponent<RawImage>();if(image==null)return;
        image.raycastTarget=false;var rect=((RectTransform)transform).rect;
        if(image.texture==null||rect.height<=0)return;
        image.uvRect=CoverUv((float)image.texture.width/image.texture.height,rect.width/rect.height,focalPoint);
    }
}
