#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using MyDefense.Tutorial;

public static class TutorialAssetBuilder
{
    public const string FixturePath="Assets/Scenes/Tests/UserSystem/P2_07_Tutorial_Test.unity";
    [MenuItem("Tools/MyDefense/Tutorial/Rebuild UI Fixture")]
    public static void Rebuild()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Stop Play Mode and preserve current scene changes first.");
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var canvas=new GameObject("Tutorial UI Fixture - NO HTTP",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvas,scene);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var view=TutorialView.Build(canvas.transform);canvas.AddComponent<TutorialUiFixture>().view=view;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(events,scene);
            var camera=new GameObject("Main Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camera,scene);camera.GetComponent<Camera>().backgroundColor=new Color(.01f,.025f,.05f);
            var light=new GameObject("Directional Light",typeof(Light));SceneManager.MoveGameObjectToScene(light,scene);light.GetComponent<Light>().type=LightType.Directional;
            EditorSceneManager.SaveScene(scene,FixturePath);
        }
        finally {EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
        AssetDatabase.SaveAssets();
    }
}
#endif
