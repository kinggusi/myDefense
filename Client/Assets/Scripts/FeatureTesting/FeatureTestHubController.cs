#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using MyDefense.Battle.Showroom;
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

            var panel = new GameObject("CatalogPanel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            panel.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.19f, 0.96f);
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewport = (RectTransform)viewportObject.transform;
            viewport.SetParent(panel.transform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            var contentObject = new GameObject("CatalogEntries", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var content = (RectTransform)contentObject.transform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var heading = new GameObject("Heading", typeof(RectTransform), typeof(LayoutElement));
            heading.transform.SetParent(content, false);
            heading.GetComponent<LayoutElement>().preferredHeight = 80f;
            AddLabel(heading.transform, "Feature Test Hub");
            foreach (var entry in ReadCatalogEntries())
            {
                var buttonObject = new GameObject(entry.TaskId + "_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                buttonObject.transform.SetParent(content, false);
                buttonObject.GetComponent<LayoutElement>().preferredHeight = 72;
                buttonObject.GetComponent<Image>().color = new Color(0.19f, 0.26f, 0.37f);
                AddLabel(buttonObject.transform, entry.TaskId + "  " + entry.Title + entry.LaunchInstructions);
                var path = entry.ScenePath;
                buttonObject.GetComponent<Button>().onClick.AddListener(() => OpenScene(path));
            }
        }

        private static IEnumerable<CatalogEntry> ReadCatalogEntries()
        {
#if UNITY_EDITOR
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
#else
            yield return new CatalogEntry(BattleShowroomScenes.CharacterId, "Battle Showroom | Character", BattleShowroomScenes.Character, string.Empty);
            yield return new CatalogEntry(BattleShowroomScenes.CombatId, "Battle Showroom | Combat", BattleShowroomScenes.Combat, string.Empty);
            yield return new CatalogEntry(BattleShowroomScenes.ContentId, "Battle Showroom | Map & Content", BattleShowroomScenes.Content, string.Empty);
#endif
        }

        private static void AddLabel(Transform parent, string value)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 4f);
            rect.offsetMax = new Vector2(-12f, -4f);
            var text = labelObject.GetComponent<Text>();
            text.text = value;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void OpenScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                return;
            }
#endif
            BattleShowroomScenes.OpenInPlayMode(scenePath);
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
#endif
