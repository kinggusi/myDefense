using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Unknown XP presentation only. No account XP contract exists; never invent progress.</summary>
public sealed class MidnightExperiencePlaceholder : MonoBehaviour
{
    private TMP_Text level;
    private RectTransform track;
    private Text unknown;
    private string lastLevel;
    private float lastWidth=-1;
    public void Bind(TMP_Text boundLevel)
    {
        level=boundLevel;
        if(level==null)
        {
            if(track!=null)track.gameObject.SetActive(false);
            if(unknown!=null)unknown.gameObject.SetActive(false);
            return;
        }
        if(track==null)
        {
            track=LobbyFinalPresentation.Child(transform,"MidnightExperienceTrack");
            var image=track.gameObject.AddComponent<Image>();image.color=new Color(.055f,.105f,.16f,.95f);image.raycastTarget=false;
            var edge=track.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.29f,.49f,.64f,.6f);edge.effectDistance=new Vector2(1,-1);edge.useGraphicAlpha=true;
            unknown=LobbyFinalPresentation.TextChild(transform,"MidnightExperienceUnknown","—/—",18);
            unknown.color=MidnightLobbyTheme.Muted;unknown.raycastTarget=false;
        }
        Refresh();
    }
    private void LateUpdate()
    {
        if(level==null)return;
        float width=((RectTransform)transform).rect.width;
        if(lastLevel!=level.text||Mathf.Abs(lastWidth-width)>.01f)Refresh();
    }
    public void Refresh()
    {
        if(level==null||track==null)return;
        var profile=(RectTransform)transform;lastWidth=profile.rect.width;lastLevel=level.text;
        // The number remains in its original bound TMP. Position after its measured text, not
        // after a guessed two-digit level width. Very long levels never overlap an XP widget.
        float textWidth=level.GetPreferredValues(level.text,float.PositiveInfinity,float.PositiveInfinity).x;
        float start=level.rectTransform.anchorMin.x*lastWidth+level.rectTransform.offsetMin.x+textWidth+12;
        float end=lastWidth*.98f;
        bool fits=end-start>=28;
        track.gameObject.SetActive(fits);unknown.gameObject.SetActive(fits);
        if(!fits)return;
        track.anchorMin=new Vector2(0,.32f);track.anchorMax=new Vector2(0,.40f);
        track.offsetMin=new Vector2(start,0);track.offsetMax=new Vector2(end,0);
        var label=unknown.rectTransform;label.anchorMin=new Vector2(0,.04f);label.anchorMax=new Vector2(0,.30f);
        label.offsetMin=new Vector2(start,0);label.offsetMax=new Vector2(end,0);
    }
}
