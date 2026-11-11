using UnityEngine;
using UnityEngine.UI;

/// <summary>Six cached runtime sprites. No per-frame creation and no imported Sprite dependency.</summary>
public static class MidnightIconAtlas
{
    public enum Icon { Heart,Coin,Diamond,Dna,Cells,Avatar }
    public const string Resource="Lobby/Midnight/status-material-icons-v3";
    private static Texture2D texture;
    private static readonly Sprite[] sprites=new Sprite[6];
    public static Rect TileRect(Icon icon,int width,int height)
    {int index=(int)icon;return new Rect(index%3*(width/3f),(index<3?1:0)*(height/2f),width/3f,height/2f);}
    public static Sprite Get(Icon icon)
    {
        int index=(int)icon;if(sprites[index]!=null)return sprites[index];
        if(texture==null)texture=Resources.Load<Texture2D>(Resource);
        if(texture==null)return null;
        sprites[index]=Sprite.Create(texture,TileRect(icon,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprites[index].name="Midnight"+icon;sprites[index].hideFlags=HideFlags.HideAndDontSave;return sprites[index];
    }
    public static Image Place(Transform owner,Icon icon)
    {
        var child=LobbyFinalPresentation.Child(owner,"MidnightAtlasIcon");LobbyFinalPresentation.Place(child,0,0,1,1);
        var image=LobbyFinalPresentation.Get<Image>(child.gameObject);image.sprite=Get(icon);image.enabled=image.sprite!=null;image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
        return image;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Clear(!Application.isPlaying);
        Application.quitting-=OnQuit;Application.quitting+=OnQuit;
    }
    private static void OnQuit()=>Clear(false);
    private static void Clear(bool immediate)
    {
        for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null)
        {if(immediate)Object.DestroyImmediate(sprites[i]);else Object.Destroy(sprites[i]);sprites[i]=null;}
        texture=null;
    }
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditorCleanup()
    {UnityEditor.EditorApplication.playModeStateChanged-=OnPlayModeChanged;UnityEditor.EditorApplication.playModeStateChanged+=OnPlayModeChanged;}
    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)Clear(true);}
#endif
}
