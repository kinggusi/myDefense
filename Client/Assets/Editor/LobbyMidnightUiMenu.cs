#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Preference-only switch. Never saves/rebuilds scenes, prefabs or account data.</summary>
public static class LobbyMidnightUiMenu
{
    private const string Root="Tools/MyDefense/Lobby/Theme/";
    private const string ReimportMenu="Tools/MyDefense/Lobby/Reimport Midnight Artwork";
    [MenuItem(ReimportMenu)]
    public static void ReimportMidnightArtwork()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        int count=0;
        foreach(string path in MidnightArtworkImporter.ArtworkPaths)
        {
            string absolute=System.IO.Path.Combine(Application.dataPath,"..",path);
            if(!System.IO.File.Exists(absolute)){Debug.LogWarning("[LobbyTheme] Missing artwork: "+path);continue;}
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);count++;
        }
        Debug.Log("[LobbyTheme] Reimported "+count+" whitelisted artwork textures. No Scene/Prefab/SaveAssets operation.");
    }
    [MenuItem(ReimportMenu,true)]
    public static bool CanReimportMidnightArtwork()=>!EditorApplication.isPlayingOrWillChangePlaymode;
    [MenuItem(Root+"Midnight (next Play)")]
    public static void Midnight() { MidnightLobbyTheme.SelectForNextSession(true);Debug.Log("[LobbyTheme] 다음 Play부터 Midnight. 현재 실행은 종료 후 다시 시작하세요."); }
    [MenuItem(Root+"Legacy (next Play)")]
    public static void Legacy() { MidnightLobbyTheme.SelectForNextSession(false);Debug.Log("[LobbyTheme] 다음 Play부터 기존 UI. 씬/프리팹/계정은 변경하지 않았습니다."); }
    private const string CaptureMenu="Tools/MyDefense/Lobby/Capture Game View";
    [MenuItem(CaptureMenu)]
    public static void CaptureGameView()
    {
        if(!EditorApplication.isPlaying)return;
        string[] tabs={"shop","collection","main","clan","content"};string tab="game";
        var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyManager>();
        if(lobby!=null&&lobby.viewObjects!=null)
            for(int i=0;i<lobby.viewObjects.Length&&i<tabs.Length;i++)if(lobby.viewObjects[i]!=null&&lobby.viewObjects[i].activeInHierarchy){tab=tabs[i];break;}
        string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../_localbuild/MidnightValidation/screenshots-v2"));
        System.IO.Directory.CreateDirectory(folder);
        string path=System.IO.Path.Combine(folder,tab+"-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff",System.Globalization.CultureInfo.InvariantCulture)+".png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[LobbyTheme] Game View capture requested: "+path);
    }
    [MenuItem(CaptureMenu,true)]
    public static bool CanCaptureGameView()=>EditorApplication.isPlaying;
}
#endif
