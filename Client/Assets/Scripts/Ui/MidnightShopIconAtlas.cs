using UnityEngine;
using UnityEngine.UI;

/// <summary>Shop-only material artwork; never changes the preserved status atlas or unit mapping.</summary>
public static class MidnightShopIconAtlas
{
    public enum Icon { Coin,Dna,Cells,Shards }
    public const string Resource="Lobby/Midnight/shop-item-icons-v4";
    private static Texture2D texture;
    private static readonly Sprite[] sprites=new Sprite[4];
    public static Rect TileRect(Icon icon,int width,int height)
    {int index=(int)icon;return new Rect(index%2*width*.5f,(index<2?1:0)*height*.5f,width*.5f,height*.5f);}
    public static Sprite Get(Icon icon)
    {
        int i=(int)icon;if(sprites[i]!=null)return sprites[i];if(texture==null)texture=Resources.Load<Texture2D>(Resource);if(texture==null)return null;
        sprites[i]=Sprite.Create(texture,TileRect(icon,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprites[i].name="MidnightShop"+icon;sprites[i].hideFlags=HideFlags.HideAndDontSave;return sprites[i];
    }
    public static Image Place(Transform parent,Icon icon)
    {
        var rect=LobbyFinalPresentation.Child(parent,"ShopAtlasIcon");LobbyFinalPresentation.Place(rect,0,0,1,1);
        var image=LobbyFinalPresentation.Get<Image>(rect.gameObject);image.sprite=Get(icon);image.enabled=image.sprite!=null;
        image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){Clear(!Application.isPlaying);Application.quitting-=Quit;Application.quitting+=Quit;}
    private static void Quit()=>Clear(false);
    private static void Clear(bool immediate)
    {for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null){if(immediate)Object.DestroyImmediate(sprites[i]);else Object.Destroy(sprites[i]);sprites[i]=null;}texture=null;}
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void Register(){UnityEditor.EditorApplication.playModeStateChanged-=Changed;UnityEditor.EditorApplication.playModeStateChanged+=Changed;}
    private static void Changed(UnityEditor.PlayModeStateChange state){if(state==UnityEditor.PlayModeStateChange.ExitingPlayMode)Clear(true);}
#endif
}
