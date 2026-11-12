using UnityEngine;

/// <summary>One decorative hero follows its scrolling placeholder inside a separate bounded mask.</summary>
public sealed class MidnightHeroArtworkFollower : MonoBehaviour
{
    public RectTransform artworkBox;
    public CanvasGroup visibility;
    private RectTransform target;
    private readonly Vector3[] corners=new Vector3[4];
    public void Bind(RectTransform placeholder){target=placeholder;Refresh();}
    private void OnEnable(){Canvas.willRenderCanvases+=Refresh;}
    private void OnDisable(){Canvas.willRenderCanvases-=Refresh;}
    private void LateUpdate(){Refresh();}
    public void Refresh()
    {
        bool visible=target!=null&&target.gameObject.activeInHierarchy;
        if(visibility!=null)visibility.alpha=visible?1:0;
        if(!visible||artworkBox==null)return;
        target.GetWorldCorners(corners);var parent=(RectTransform)transform;
        Vector2 min=parent.InverseTransformPoint(corners[0]),max=min;
        for(int i=1;i<4;i++){Vector2 point=parent.InverseTransformPoint(corners[i]);min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
        artworkBox.anchorMin=artworkBox.anchorMax=new Vector2(.5f,.5f);artworkBox.pivot=new Vector2(.5f,.5f);
        artworkBox.anchoredPosition=(min+max)*.5f;artworkBox.sizeDelta=max-min;
    }
}
