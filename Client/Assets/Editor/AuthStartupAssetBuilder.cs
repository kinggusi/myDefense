#if UNITY_EDITOR
using MyDefense.Auth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AuthStartupAssetBuilder
{
    public const string ThemePath = "Assets/Resources/Auth/StartupTheme.asset";
    public const string PrefabPath = "Assets/Resources/Auth/AuthStartupView.prefab";
    public const string FixturePath = "Assets/Scenes/Tests/AuthStartupUiTest.unity";
    [MenuItem("Tools/MyDefense/Auth/Rebuild Startup UI")]
    public static void Rebuild()
    {
        Folder("Assets/Resources/Auth");
        var theme = AssetDatabase.LoadAssetAtPath<StartupTheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<StartupTheme>();
            theme.background = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Lobby/Final/SpaceshipBackground.png");
            theme.mascot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Lobby/Final/WakjeoMascot.png");
            theme.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        var root = Box("AuthStartupView", null, new Color(.005f, .02f, .04f));
        Stretch((RectTransform)root.transform, 0, 0, 1, 1);
        root.AddComponent<CanvasGroup>().ignoreParentGroups = true;
        var view = root.AddComponent<AuthStartupView>(); view.theme = theme;
        var background = new GameObject("SpaceshipBackground", typeof(RectTransform), typeof(RawImage));
        background.transform.SetParent(root.transform, false);
        view.background = background.GetComponent<RawImage>();
        view.background.gameObject.AddComponent<AspectRatioFitter>();
        Stretch(view.background.rectTransform, 0, 0, 1, 1);
        var shade = Box("ReadabilityShade", root.transform, new Color(.003f, .014f, .028f, .60f));
        shade.GetComponent<Image>().raycastTarget = false; Stretch((RectTransform)shade.transform, 0, 0, 1, 1);
        view.safeArea = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        view.safeArea.SetParent(root.transform, false); Stretch(view.safeArea, 0, 0, 1, 1);
        Text("ShipLabel", view.safeArea, "WAKJEO  /  DEFENSE", 28, .12f, .88f, .88f, .93f).color = theme.muted;
        view.title = Text("Title", view.safeArea, theme.title, 92, .08f, .72f, .92f, .83f);
        view.logo = Box("ReplaceableLogo", view.safeArea, Color.white).GetComponent<Image>();
        Stretch(view.logo.rectTransform, .12f, .71f, .88f, .85f); view.logo.preserveAspect = true; view.logo.raycastTarget = false;
        view.mascot = Box("Mascot", view.safeArea, Color.white).GetComponent<Image>();
        Stretch(view.mascot.rectTransform, .30f, .47f, .70f, .70f); view.mascot.preserveAspect = true; view.mascot.raycastTarget = false;
        view.subtitle = Text("Subtitle", view.safeArea, theme.subtitle, 29, .08f, .43f, .92f, .48f);
        var panel = new GameObject("EntryPanel", typeof(RectTransform), typeof(LobbyNeonGraphic));
        panel.transform.SetParent(view.safeArea, false); Stretch((RectTransform)panel.transform, .07f, .115f, .93f, .415f);
        panel.GetComponent<LobbyNeonGraphic>().Configure(LobbyNeonGraphic.FrameStyle.Panel);
        panel.GetComponent<LobbyNeonGraphic>().UseFinalStyle();
        view.status = Text("Status", panel.transform, "탑승 준비 완료", 31, .06f, .70f, .94f, .96f);
        view.stages = new[] {
            Text("AccountStage", panel.transform, "01  계정 확인", 23, .04f, .59f, .34f, .68f),
            Text("DataStage", panel.transform, "02  유닛 정보", 23, .35f, .59f, .65f, .68f),
            Text("LobbyStage", panel.transform, "03  로비 준비", 23, .66f, .59f, .96f, .68f) };
        view.startButton = Button("StartButton", panel.transform, theme.startLabel, .06f, .32f, .94f, .55f, out view.startLabel);
        view.googleButton = Button("GoogleButton", panel.transform, "Google · 준비 중", .06f, .09f, .48f, .27f, out view.googleLabel);
        view.appleButton = Button("AppleButton", panel.transform, "Apple · 준비 중", .52f, .09f, .94f, .27f, out view.appleLabel);
        view.notice = Text("GuestNotice", view.safeArea, theme.guestNotice, 24, .08f, .035f, .92f, .105f);
        view.ApplyTheme(); view.SetState("탑승 준비 완료", false); view.SetProviderAvailability(false, false);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FixturePath) == null) BuildFixture();
        AssetDatabase.SaveAssets();
    }
    private static void BuildFixture()
    {
        Folder("Assets/Scenes/Tests");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var canvas = new GameObject("Auth UI Fixture — no account requests", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            SceneManager.MoveGameObjectToScene(canvas, scene);
            var view = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            view.transform.SetParent(canvas.transform, false);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(events, scene);
            var camera = new GameObject("Main Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(camera, scene);
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = Color.black;
            var light = new GameObject("Directional Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
            SceneManager.MoveGameObjectToScene(light, scene);
            EditorSceneManager.SaveScene(scene, FixturePath);
        }
        finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
    }
    private static GameObject Box(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI"); if (parent != null) obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color; return obj;
    }
    private static Text Text(string name, Transform parent, string content, int size, float x0, float y0, float x1, float y1)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(18, size - 8); text.resizeTextMaxSize = size;
        text.raycastTarget = false; Stretch(text.rectTransform, x0, y0, x1, y1); return text;
    }
    private static Button Button(string name, Transform parent, string label, float x0, float y0, float x1, float y1, out Text text)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(LobbyNeonGraphic), typeof(Button)); obj.transform.SetParent(parent, false);
        Stretch((RectTransform)obj.transform, x0, y0, x1, y1);
        var frame = obj.GetComponent<LobbyNeonGraphic>(); frame.Configure(LobbyNeonGraphic.FrameStyle.Button); frame.UseFinalStyle();
        var button = obj.GetComponent<Button>(); button.targetGraphic = frame;
        var colors = button.colors; colors.disabledColor = new Color(.40f, .44f, .48f); button.colors = colors;
        text = Text("Label", obj.transform, label, name == "StartButton" ? 37 : 25, .04f, .04f, .96f, .96f); return button;
    }
    private static void Stretch(RectTransform rect, float x0, float y0, float x1, float y1)
    { rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); string parent = path.Substring(0, slash); Folder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
#endif
