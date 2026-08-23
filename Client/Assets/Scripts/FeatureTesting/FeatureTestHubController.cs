using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MyDefenseGame.FeatureTesting
{
    /// <summary>
    /// Test-only Hub controller. The canonical catalog lives in the Editor assembly;
    /// reflection keeps this runtime component independent from editor-only code.
    /// FeatureTestHub is intentionally excluded from Production Build Settings.
    /// </summary>
    public sealed class FeatureTestHubController : MonoBehaviour
    {
        private readonly List<GameObject> generated = new List<GameObject>();

        private void Awake()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("FeatureTestHubCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            var panel = new GameObject("CatalogPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            AddLabel(panel.transform, "Feature Test Hub\nTask별 격리 Scene을 선택하세요.");
            foreach (var entry in ReadCatalogEntries())
            {
                var buttonObject = new GameObject(entry.TaskId + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonObject.transform.SetParent(panel.transform, false);
                buttonObject.GetComponent<LayoutElement>().preferredHeight = 72;
                                AddLabel(buttonObject.transform, entry.TaskId + "  " + entry.Title + "\n" + entry.ScenePath + entry.LaunchInstructions);
                var path = entry.ScenePath;
                buttonObject.GetComponent<Button>().onClick.AddListener(() => OpenScene(path));
                generated.Add(buttonObject);
            }
        }

        private static IEnumerable<CatalogEntry> ReadCatalogEntries()
        {
            var catalogType = Type.GetType("MyDefenseGame.Editor.FeatureTesting.FeatureTestCatalog, Assembly-CSharp-Editor");
            var createDefault = catalogType?.GetMethod("CreateDefault");
            var catalog = createDefault?.Invoke(null, null);
            var cases = catalog?.GetType().GetProperty("Cases")?.GetValue(catalog, null) as IEnumerable;
            if (cases == null)
                yield break;

            foreach (var testCase in cases)
            {
                                var type = testCase.GetType();
                var launch = type.GetProperty("LaunchProfile")?.GetValue(testCase, null);
                var launchText = string.Empty;
                if (launch != null)
                {
                    var launchType = launch.GetType();
                    launchText = "\nHost: " + launchType.GetProperty("HostUserId")?.GetValue(launch, null)
                        + "  Client: " + launchType.GetProperty("ClientUserId")?.GetValue(launch, null)
                        + "  Session: " + launchType.GetProperty("SessionName")?.GetValue(launch, null);
                }
                yield return new CatalogEntry(
                    type.GetProperty("TaskId")?.GetValue(testCase, null) as string ?? "UNKNOWN",
                    type.GetProperty("Title")?.GetValue(testCase, null) as string ?? "Untitled",
                    type.GetProperty("ScenePath")?.GetValue(testCase, null) as string ?? string.Empty,
                    launchText);
            }
        }

        private static void AddLabel(Transform parent, string value)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var text = labelObject.GetComponent<Text>();
            text.text = value;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void OpenScene(string scenePath)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
#if UNITY_EDITOR
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
#else
                SceneManager.LoadScene(scenePath);
#endif
        }

        private readonly struct CatalogEntry
        {
            public readonly string TaskId;
            public readonly string Title;
                        public readonly string ScenePath;
            public readonly string LaunchInstructions;

            public CatalogEntry(string taskId, string title, string scenePath, string launchInstructions)
            {
                TaskId = taskId;
                Title = title;
                ScenePath = scenePath;
                LaunchInstructions = launchInstructions ?? string.Empty;
            }
        }
    }
}
