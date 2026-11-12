using UnityEngine;
using UnityEngine.UI;

public sealed class QuestUiView : MonoBehaviour
{
    public const float CycleScrollTop = 450f;
    public const float AchievementScrollTop = 210f;
    public CanvasGroup inputCanvasGroup;
    public Button closeButton;
    public Button dailyTabButton;
    public Button weeklyTabButton;
    public Button achievementTabButton;
    public Text titleText;
    public Text resetText;
    public Text activityText;
    public Slider activitySlider;
    public Transform milestoneRoot;
    public Button milestoneTemplate;
    public Transform questRoot;
    public RectTransform questScrollRect;
    public Button questTemplate;
    public Text statusText;
    public bool IsMidnightPresentation {get;private set;}
    private Canvas presentationCanvas;
    private float lastSafeBottom=-1;
    private bool cycleVisible=true;
    private void OnEnable()
    {
        if(MidnightLobbyTheme.IsPresentationTarget(transform))ApplyMidnightPresentation();
    }
    private void LateUpdate()
    {
        if(!IsMidnightPresentation)return;
        if(presentationCanvas==null)presentationCanvas=GetComponentInParent<Canvas>();
        float bottom=Screen.safeArea.yMin/Mathf.Max(.01f,presentationCanvas==null?1:presentationCanvas.scaleFactor);
        if(Mathf.Abs(bottom-lastSafeBottom)>.1f){lastSafeBottom=bottom;var r=(RectTransform)transform;r.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom+Mathf.Max(0,bottom));}
    }
    public void ApplyMidnightPresentation()
    {
        IsMidnightPresentation=true;
        var root=(RectTransform)transform;root.anchorMin=new Vector2(.035f,0);root.anchorMax=new Vector2(.965f,1);root.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom+Mathf.Max(0,lastSafeBottom));root.offsetMax=new Vector2(0,-160);
        var frame=LobbyFinalPresentation.Frame(gameObject);frame.UseMidnightAccent(new Color(.045f,.07f,.12f,1f));
        // A modal must occlude the lobby, not merely catch input. The opaque root image also
        // covers the inset/corners of the decorative mesh without altering HUD/nav bounds.
        var rootHit=LobbyFinalPresentation.Get<Image>(gameObject);rootHit.enabled=true;rootHit.sprite=null;rootHit.type=Image.Type.Simple;rootHit.color=new Color(.045f,.07f,.12f,1f);rootHit.raycastTarget=true;
        var header=closeButton.transform.parent as RectTransform;
        MidnightLobbyPresentation.Top(header,0,1,0,180);
        var headerImage=header.GetComponent<Image>();if(headerImage!=null)headerImage.color=Color.clear;
        var headerFrame=header.Find("LobbyNeonFrame")?.GetComponent<LobbyNeonGraphic>();if(headerFrame!=null)headerFrame.enabled=false;
        MidnightLobbyPresentation.Top(titleText.rectTransform,.055f,.76f,25,125);MidnightLobbyTheme.DisplayTypography(titleText,72);titleText.alignment=TextAnchor.MiddleLeft;
        MidnightLobbyPresentation.Top((RectTransform)closeButton.transform,.79f,.95f,40,130);StyleButton(closeButton,32);
        MidnightLobbyPresentation.Top(resetText.rectTransform,.06f,.94f,125,180);MidnightLobbyTheme.Typography(resetText,25,FontStyle.Normal);resetText.alignment=TextAnchor.MiddleLeft;resetText.color=MidnightLobbyTheme.Muted;
        Button[] tabs={dailyTabButton,weeklyTabButton,achievementTabButton};
        for(int i=0;i<tabs.Length;i++){MidnightLobbyPresentation.Top((RectTransform)tabs[i].transform,.055f+i*.30f,.335f+i*.30f,190,275);StyleButton(tabs[i],36);}
        MidnightLobbyPresentation.Top(activityText.rectTransform,.06f,.94f,310,365);MidnightLobbyTheme.Typography(activityText,32);activityText.alignment=TextAnchor.MiddleLeft;
        MidnightLobbyPresentation.Top((RectTransform)activitySlider.transform,.065f,.935f,380,396);activitySlider.interactable=false;
        var fill=activitySlider.fillRect?.GetComponent<Image>();if(fill!=null){fill.color=MidnightLobbyTheme.Accent;fill.raycastTarget=false;}
        MidnightLobbyPresentation.Top((RectTransform)milestoneRoot,.045f,.955f,425,595);
        var milestoneLayout=milestoneRoot.GetComponent<HorizontalLayoutGroup>();if(milestoneLayout!=null){milestoneLayout.spacing=18;milestoneLayout.padding=new RectOffset(0,0,0,0);}
        var layout=questRoot.GetComponent<VerticalLayoutGroup>();if(layout!=null){layout.spacing=18;layout.padding=new RectOffset(6,6,6,24);}
        LobbyFinalPresentation.Get<LayoutElement>(questTemplate.gameObject).preferredHeight=215;
        MidnightLobbyPresentation.BottomBox(statusText.rectTransform,.055f,.945f,8,55);MidnightLobbyTheme.Typography(statusText,25,FontStyle.Normal);statusText.color=MidnightLobbyTheme.Muted;
        SetMidnightCycleLayout(cycleVisible);header.SetAsLastSibling();
    }
    public void SetMidnightCycleLayout(bool visible)
    {
        cycleVisible=visible;if(!IsMidnightPresentation||questScrollRect==null)return;
        questScrollRect.anchorMin=new Vector2(.045f,0);questScrollRect.anchorMax=new Vector2(.955f,1);
        questScrollRect.offsetMin=new Vector2(0,75);questScrollRect.offsetMax=new Vector2(0,visible?-620:-310);
    }
    public void StyleMidnightTabs(int selected)
    {
        if(!IsMidnightPresentation)return;
        Button[] tabs={dailyTabButton,weeklyTabButton,achievementTabButton};
        for(int i=0;i<tabs.Length;i++)
        {
            var button=tabs[i];StyleButton(button,36);
            var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;
            var label=button.GetComponentInChildren<Text>();if(label!=null)label.color=i==selected?MidnightLobbyTheme.Accent:MidnightLobbyTheme.Ink;
            var line=LobbyFinalPresentation.Child(button.transform,"MidnightSelectedLine");LobbyFinalPresentation.Place(line,.14f,.02f,.86f,.065f);
            var image=LobbyFinalPresentation.Get<Image>(line.gameObject);image.color=MidnightLobbyTheme.Accent;image.raycastTarget=false;line.gameObject.SetActive(i==selected);
        }
    }
    public void StyleMidnightQuest(Button button,string title,string description,string progress,string reward,bool claimed)
    {
        if(!IsMidnightPresentation)return;
        // Preserve the original first Text object for the controller's legacy write contract.
        var original=button.GetComponentInChildren<Text>(true);if(original!=null&&!original.name.StartsWith("Midnight")){original.enabled=false;original.raycastTarget=false;}
        var image=button.GetComponent<Image>();if(image!=null){image.color=new Color(0,0,0,.001f);image.raycastTarget=true;}
        LobbyFinalPresentation.Get<LayoutElement>(button.gameObject).preferredHeight=215;
        var surface=LobbyFinalPresentation.Child(button.transform,"MidnightRowSurface");LobbyFinalPresentation.Place(surface,0,0,1,1);surface.SetAsFirstSibling();
        var panel=LobbyFinalPresentation.Get<LobbyNeonGraphic>(surface.gameObject);panel.Configure(LobbyNeonGraphic.FrameStyle.Panel);panel.UseMidnightAccent(new Color(.09f,.13f,.20f,.97f));panel.raycastTarget=false;
        var action=LobbyFinalPresentation.Frame(button.gameObject);LobbyFinalPresentation.Place((RectTransform)action.transform,.76f,.24f,.96f,.72f);action.UseMidnightPrimary();action.UseMidnightButtonGloss();action.transform.SetAsLastSibling();
        button.targetGraphic=action;ButtonColors(button);
        TextAt(button.transform,"MidnightRowTitle",title,35,.045f,.70f,.72f,.94f,TextAnchor.MiddleLeft);
        TextAt(button.transform,"MidnightRowDescription",description,25,.045f,.43f,.72f,.70f,TextAnchor.MiddleLeft).color=MidnightLobbyTheme.Muted;
        TextAt(button.transform,"MidnightRowProgress",progress,26,.045f,.25f,.72f,.45f,TextAnchor.MiddleLeft).color=MidnightLobbyTheme.Accent;
        TextAt(button.transform,"MidnightRowReward",reward,26,.045f,.035f,.72f,.25f,TextAnchor.MiddleLeft);
        var state=TextAt(button.transform,"MidnightRowState",claimed?"수령 완료":button.interactable?"받기":"진행 중",28,.77f,.25f,.95f,.71f,TextAnchor.MiddleCenter);
        state.color=button.interactable?new Color(.025f,.065f,.10f):MidnightLobbyTheme.Muted;
    }
    public void StyleMidnightMilestone(Button button,int points,string reward,bool claimed)
    {
        if(!IsMidnightPresentation)return;
        var original=button.GetComponentInChildren<Text>(true);if(original!=null&&!original.name.StartsWith("Midnight")){original.enabled=false;original.raycastTarget=false;}
        var frame=LobbyFinalPresentation.Frame(button.gameObject);frame.UseMidnightPrimary();frame.UseMidnightButtonGloss();button.targetGraphic=frame;ButtonColors(button);
        Color ink=button.interactable?new Color(.025f,.065f,.10f):MidnightLobbyTheme.Ink;
        TextAt(button.transform,"MidnightPoints",points.ToString(),32,.05f,.69f,.95f,.96f,TextAnchor.MiddleCenter).color=ink;
        TextAt(button.transform,"MidnightReward",reward,23,.045f,.29f,.955f,.70f,TextAnchor.MiddleCenter).color=ink;
        TextAt(button.transform,"MidnightState",claimed?"수령 완료":button.interactable?"받기":"잠김",24,.04f,.035f,.96f,.29f,TextAnchor.MiddleCenter).color=button.interactable?new Color(.025f,.065f,.10f):MidnightLobbyTheme.Muted;
    }
    private static Text TextAt(Transform parent,string name,string value,int size,float x0,float y0,float x1,float y1,TextAnchor alignment)
    {var text=LobbyFinalPresentation.TextChild(parent,name,value,size);LobbyFinalPresentation.Place(text.rectTransform,x0,y0,x1,y1);MidnightLobbyTheme.Typography(text,size);text.alignment=alignment;text.supportRichText=false;return text;}
    private static void StyleButton(Button button,int size)
    {
        var frame=LobbyFinalPresentation.Frame(button.gameObject);frame.UseMidnightStyle();frame.UseMidnightButtonGloss();button.targetGraphic=frame;ButtonColors(button);
        var label=button.GetComponentInChildren<Text>();if(label!=null){MidnightLobbyTheme.Typography(label,size);label.color=MidnightLobbyTheme.Ink;}
    }
    private static void ButtonColors(Button button)
    {
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.91f,.97f,1);colors.pressedColor=new Color(.63f,.78f,.88f);colors.disabledColor=new Color(.30f,.38f,.48f,.70f);colors.fadeDuration=.09f;button.colors=colors;
    }
}
