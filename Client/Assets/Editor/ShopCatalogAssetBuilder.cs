#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopCatalogAssetBuilder
{
    public const string PrefabPath="Assets/Resources/Prefabs/Shop/ShopCatalog.prefab";
    public const string FixturePath="Assets/Scenes/Tests/UserSystem/P2_06_01_Shop_Test.unity";
    [MenuItem("Tools/MyDefense/Shop/Rebuild Catalog Assets")]
    public static void Rebuild()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first.");
        var active=SceneManager.GetActiveScene();
        if(active.isDirty)throw new System.InvalidOperationException("Save current scene changes before rebuilding.");
        Folder("Assets/Resources/Prefabs/Shop");Folder("Assets/Scenes/Tests/UserSystem");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var temporary=new GameObject("Temporary");SceneManager.MoveGameObjectToScene(temporary,preview);
            var view=ShopCatalogView.Build(temporary.transform);
            PrefabUtility.SaveAsPrefabAsset(view.gameObject,PrefabPath);
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var canvas=new GameObject("Shop UI Fixture — NO HTTP",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);root.transform.SetParent(canvas.transform,false);
            var fixture=canvas.AddComponent<ShopCatalogFixture>();fixture.view=root.GetComponent<ShopCatalogView>();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(events,scene);
            var camera=new GameObject("Main Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camera,scene);camera.GetComponent<Camera>().backgroundColor=new Color(.01f,.025f,.05f);
            var light=new GameObject("Directional Light",typeof(Light));SceneManager.MoveGameObjectToScene(light,scene);light.GetComponent<Light>().type=LightType.Directional;
            EditorSceneManager.SaveScene(scene,FixturePath);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
        AssetDatabase.SaveAssets();
    }
    private static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;var i=path.LastIndexOf('/');var parent=path.Substring(0,i);Folder(parent);AssetDatabase.CreateFolder(parent,path.Substring(i+1));
    }
}
#endif
