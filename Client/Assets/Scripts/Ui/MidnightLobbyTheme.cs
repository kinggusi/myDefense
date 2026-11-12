using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in runtime presentation. No serialized asset, account or gameplay state is changed.</summary>
public static class MidnightLobbyTheme
{
    public const string PreferenceKey = "mydefense.ui.midnight.enabled";
    public const string BackgroundResource = "Lobby/Midnight/observatory-background";
    public const string GachaResource = "Lobby/Midnight/gacha-prism";
    public const string PlanetResource = "Lobby/Midnight/neptune-observatory";
    public const string MainResource = "Lobby/Midnight/main-observatory-v4";
    public const string GachaCutoutResource = "Lobby/Midnight/gacha-prism-cutout-v5";
    public const string DisplayFontResource = "Lobby/Midnight/Fonts/BlackHanSans-Regular";
    public const float NavigationHeight=225f;
    public const float ContentBottom=245f;
    private static Font displayFont;
    public static readonly Color Ink = new Color(.93f,.96f,1f);
    public static readonly Color Muted = new Color(.58f,.66f,.78f);
    public static readonly Color Accent = new Color(.30f,.88f,.97f);
    public static readonly Color Surface = new Color(.075f,.105f,.17f,.96f);
    private static bool? sessionEnabled;
    private static Canvas lobbyCanvas;
    public static bool Enabled
    {
        get
        {
            if(!Application.isPlaying)return false;
            if(!sessionEnabled.HasValue)sessionEnabled=PlayerPrefs.GetInt(PreferenceKey,1)!=0;
            return sessionEnabled.Value;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { sessionEnabled = null;lobbyCanvas=null; }
    public static void RegisterCanvas(Canvas canvas) { lobbyCanvas=canvas; }
    public static bool IsPresentationTarget(Transform target) => Enabled&&lobbyCanvas!=null&&target.IsChildOf(lobbyCanvas.transform)&&!IsProtectedProfile(target);
    public static Sprite CurrencySprite(string name)
    {
        if(lobbyCanvas==null)return null;
        var bar=lobbyCanvas.transform.Find("tab_bar");if(bar==null)return null;
        foreach(var image in bar.GetComponentsInChildren<Image>(true))if(image.name==name&&image.sprite!=null)return image.sprite;
        return null;
    }
    public static void Typography(Text text,int size,FontStyle style=FontStyle.Bold)
    {
        if(text==null)return;text.fontSize=size;text.resizeTextForBestFit=true;
        text.resizeTextMinSize=Mathf.Max(22,size-8);text.resizeTextMaxSize=size;text.fontStyle=style;
    }
    public static void DisplayTypography(Text text,int size,bool italic=true)
    {
        if(text==null)return;
        if(displayFont==null)displayFont=Resources.Load<Font>(DisplayFontResource);
        if(displayFont!=null)text.font=displayFont;
        Typography(text,size,italic?FontStyle.Italic:FontStyle.Normal);
    }

    public static void SelectForNextSession(bool enabled)
    {
        PlayerPrefs.SetInt(PreferenceKey,enabled?1:0);
        PlayerPrefs.Save();
    }

    public static bool IsProtectedProfile(Transform target)
    {
        // Generic popup/frame passes must skip authored profile children. The explicit compact
        // header presentation now owns their style; this is not a profile-immutability policy.
        for(var node=target;node!=null;node=node.parent)
            if(node.name=="LobbyProfileStatus") return true;
        return false;
    }

    public static RawImage Art(Transform parent,string name,string resource,float x0,float y0,float x1,float y1)
    {
        var rect=LobbyFinalPresentation.Child(parent,name);
        LobbyFinalPresentation.Place(rect,x0,y0,x1,y1);
        var image=LobbyFinalPresentation.Get<RawImage>(rect.gameObject);
        image.texture=Resources.Load<Texture2D>(resource);
        image.color=image.texture!=null?Color.white:new Color(.025f,.04f,.085f,1f);
        image.raycastTarget=false;
        var crop=LobbyFinalPresentation.Get<MidnightArtworkCrop>(rect.gameObject);
        crop.Refresh();
        return image;
    }

    public static void Frame(GameObject owner)
    {
        var frame=owner.GetComponent<LobbyNeonGraphic>();
        if(frame!=null&&!IsProtectedProfile(owner.transform)) frame.UseMidnightStyle();
    }

    public static void StyleCard(UnitCardUI card)
    {
        if(card==null)return;
        if(card.neonFrame!=null)card.neonFrame.UseMidnightAccent(card.neonFrame.IsOwned?new Color(.085f,.125f,.20f,.97f):new Color(.045f,.065f,.105f,.97f));
        Place(card.text_Grade,.07f,.88f,.73f,.98f,23,TextAnchor.MiddleLeft);
        if(card.text_Grade!=null){card.text_Grade.color=Color.white;card.text_Grade.fontStyle=FontStyle.BoldAndItalic;}
        Place(card.text_Name,.07f,.15f,.94f,.29f,28,TextAnchor.MiddleLeft);
        Place(card.text_Level,.07f,.025f,.49f,.14f,25,TextAnchor.MiddleLeft);
        Place(card.text_Pieces,.53f,.025f,.94f,.14f,25,TextAnchor.MiddleCenter);
        var countPlate=LobbyFinalPresentation.Child(card.transform,"MidnightQuantityPlate");
        LobbyFinalPresentation.Place(countPlate,.51f,.028f,.95f,.13f);
        var plate=LobbyFinalPresentation.Get<Image>(countPlate.gameObject);plate.color=new Color(.36f,.44f,.56f,.30f);plate.raycastTarget=false;
        countPlate.SetAsLastSibling();if(card.text_Pieces!=null)card.text_Pieces.transform.SetAsLastSibling();
        var well=card.transform.Find("PortraitWell") as RectTransform;
        if(well!=null)
        {
            LobbyFinalPresentation.Place(well,.06f,.30f,.94f,.88f);
            foreach(var frame in well.GetComponentsInChildren<LobbyNeonGraphic>(true))frame.enabled=false;
        }
        if(card.portraitImage!=null) { card.portraitImage.preserveAspect=true;card.portraitImage.raycastTarget=false; }
        if(card.ownershipStatusText!=null)card.ownershipStatusText.gameObject.SetActive(false);
    }

    private static void Place(Text text,float x0,float y0,float x1,float y1,int size,TextAnchor alignment)
    {
        if(text==null)return;
        LobbyFinalPresentation.Place(text.rectTransform,x0,y0,x1,y1);
        Typography(text,size);
        text.alignment=alignment;text.raycastTarget=false;
    }
}
