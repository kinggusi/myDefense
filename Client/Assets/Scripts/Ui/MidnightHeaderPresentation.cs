using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime-only compact status row. Reuses bound texts/buttons; never owns account values.</summary>
public static class MidnightHeaderPresentation
{
    public const float PlusSize=44f;
    public const float PlusHitHeight=88f;
    public static void Apply(LobbyManager lobby)
    {
        if(lobby==null||lobby.text_UserName==null)return;
        var canvas=lobby.text_UserName.GetComponentInParent<Canvas>();
        var bar=canvas!=null?canvas.transform.Find("tab_bar"):null;if(bar==null)return;
        var profile=bar.Find("LobbyProfileStatus") as RectTransform;
        if(profile!=null)
        {
            HideFrame(profile);LobbyFinalPresentation.Place(profile,.025f,.04f,.35f,.96f);
            var avatar=LobbyFinalPresentation.Child(profile,"MidnightAvatar");
            avatar.anchorMin=avatar.anchorMax=new Vector2(0,.5f);avatar.pivot=new Vector2(0,.5f);avatar.sizeDelta=new Vector2(96,96);avatar.anchoredPosition=Vector2.zero;
            MidnightIconAtlas.Place(avatar,MidnightIconAtlas.Icon.Avatar);
            lobby.text_UserName.transform.SetParent(profile,false);
            LobbyFinalPresentation.Place(lobby.text_UserName.rectTransform,.255f,.48f,.98f,.94f);TextStyle(lobby.text_UserName,28,18,TextAlignmentOptions.MidlineLeft);
            if(lobby.text_UserLevel!=null)
            {
                Transform old=lobby.text_UserLevel.transform.parent;
                lobby.text_UserLevel.transform.SetParent(profile,false);
                if(old!=profile)old.gameObject.SetActive(false);
                LobbyFinalPresentation.Place(lobby.text_UserLevel.rectTransform,.335f,.08f,.98f,.48f);TextStyle(lobby.text_UserLevel,23,17,TextAlignmentOptions.MidlineLeft);
                var prefix=LobbyFinalPresentation.TextChild(profile,"MidnightLevelPrefix","Lv.",23);
                LobbyFinalPresentation.Place(prefix.rectTransform,.255f,.08f,.335f,.48f);prefix.color=MidnightLobbyTheme.Muted;prefix.alignment=TextAnchor.MiddleLeft;
            }
            // Lobby DTO has no XP fields. Preserve the existing object/value/listeners but do
            // not present the authored 75/500 or slider fraction as live account data.
            var slider=profile.GetComponentInChildren<Slider>(true);if(slider!=null)slider.gameObject.SetActive(false);
            var experience=profile.GetComponent<MidnightExperiencePlaceholder>();
            if(experience==null)experience=profile.gameObject.AddComponent<MidnightExperiencePlaceholder>();
            experience.Bind(lobby.text_UserLevel);
        }
        TMP_Text[] values={lobby.text_Heart,lobby.text_Gold,lobby.text_Diamond};
        string[] icons={"Heart","Coin","Gem"};
        for(int i=0;i<values.Length;i++)
        {
            var value=values[i];if(value==null)continue;
            var panel=value.transform.parent as RectTransform;if(panel==null)continue;
            HideFrame(panel);float left=.365f+i*.208f;
            LobbyFinalPresentation.Place(panel,left,.20f,left+.202f,.80f);
            var icon=panel.Find(icons[i]) as RectTransform;
            if(icon!=null)
            {
                var oldImage=icon.GetComponent<Image>();if(oldImage!=null)oldImage.enabled=false;
                icon.anchorMin=icon.anchorMax=new Vector2(0,.5f);icon.pivot=new Vector2(0,.5f);icon.anchoredPosition=new Vector2(-7,0);icon.sizeDelta=new Vector2(62,62);
                MidnightIconAtlas.Place(icon,(MidnightIconAtlas.Icon)i);
            }
            var r=value.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(47,0);r.offsetMax=new Vector2(-51,0);
            TextStyle(value,30,18,TextAlignmentOptions.MidlineRight);
            var plus=panel.Find("Plus Button") as RectTransform;if(plus==null)continue;
            var aspect=plus.GetComponent<AspectRatioFitter>();if(aspect!=null)aspect.enabled=false;
            plus.anchorMin=plus.anchorMax=new Vector2(1,.5f);plus.pivot=new Vector2(1,.5f);plus.sizeDelta=new Vector2(PlusSize,PlusHitHeight);plus.anchoredPosition=Vector2.zero;
            var frame=LobbyFinalPresentation.Frame(plus.gameObject);frame.UseMidnightStyle();
            var visual=(RectTransform)frame.transform;visual.anchorMin=visual.anchorMax=visual.pivot=new Vector2(.5f,.5f);visual.sizeDelta=new Vector2(PlusSize,PlusSize);visual.anchoredPosition=Vector2.zero;
            frame.Configure(LobbyNeonGraphic.FrameStyle.Panel);frame.raycastTarget=false;
            frame.UseMidnightButtonGloss();
            var hit=LobbyFinalPresentation.Get<Image>(plus.gameObject);hit.enabled=true;hit.color=Color.clear;hit.raycastTarget=true;
            var button=plus.GetComponent<Button>();if(button!=null)button.targetGraphic=frame;
            MidnightLobbyPresentation.StyleCurrencyPlus(plus);
        }
    }
    private static void TextStyle(TMP_Text text,float max,float min,TextAlignmentOptions alignment)
    {
        text.color=MidnightLobbyTheme.Ink;text.alignment=alignment;text.enableAutoSizing=true;text.fontSizeMax=max;text.fontSizeMin=min;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;
    }
    private static void HideFrame(Transform root)
    {
        var graphic=root.GetComponent<LobbyNeonGraphic>();if(graphic!=null)graphic.enabled=false;
        var image=root.GetComponent<Image>();if(image!=null)image.enabled=false;
        var frame=root.Find("LobbyNeonFrame");if(frame!=null)frame.gameObject.SetActive(false);
    }
}
