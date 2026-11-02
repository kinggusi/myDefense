#if UNITY_EDITOR
using System;
using MyDefense.Clans;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ClanAssetBuilder
{
    public const string PrefabPath="Assets/Resources/Prefabs/Lobby/Clan/ClanScreen.prefab";
    public const string FixturePath="Assets/Scenes/Tests/UserSystem/P3_01_03_Clan_Test.unity";
    public const string HttpFixturePath="Assets/Scenes/Tests/UserSystem/P3_01_03_Clan_Http_Test.unity";
    [MenuItem("Tools/MyDefense/Clan/Rebuild UI Assets")]
    public static void Rebuild()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play Mode and preserve current scene changes first.");
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            string path=SceneManager.GetSceneAt(i).path;
            if(path==FixturePath||path==HttpFixturePath)throw new InvalidOperationException("Open a clean SampleScene and close both Clan fixture scenes before rebuilding their assets.");
        }
        Folder("Assets/Resources/Prefabs/Lobby/Clan");Folder("Assets/Scenes/Tests/UserSystem");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var temporary=new GameObject("Temporary");SceneManager.MoveGameObjectToScene(temporary,preview);
            var view=ClanView.Build(temporary.transform);PrefabUtility.SaveAsPrefabAsset(view.gameObject,PrefabPath);
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        BuildScene(FixturePath,false);BuildScene(HttpFixturePath,true);AssetDatabase.SaveAssets();
    }
    private static void BuildScene(string path,bool http)
    {
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var canvas=new GameObject(http?"Clan isolated HTTP fixture":"Clan UI fixture - NO HTTP",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var view=ClanView.Build(canvas.transform);
            if(http)canvas.AddComponent<ClanHttpUiFixture>().view=view;else canvas.AddComponent<ClanUiFixture>().view=view;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(events,scene);
            var camera=new GameObject("Main Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camera,scene);camera.GetComponent<Camera>().backgroundColor=new Color(.008f,.025f,.045f);
            var light=new GameObject("Directional Light",typeof(Light));SceneManager.MoveGameObjectToScene(light,scene);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=.7f;
            EditorSceneManager.SaveScene(scene,path);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
    }
    private static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;int separator=path.LastIndexOf('/');string parent=path.Substring(0,separator);Folder(parent);AssetDatabase.CreateFolder(parent,path.Substring(separator+1));
    }
}
#endif
