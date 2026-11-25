using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Showroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyDefense.Battle.Editor
{
    /// <summary>Actual Unity rendering of the existing offline Content showroom, without IMGUI.</summary>
    public static class PlanetBattlefieldPreviewExporter
    {
        private const int Width = 720;
        private const int Height = 960;
        private const string PreferenceKey = "MyDefense.BattleShowroom.v1.Content";

        public static void ExportFromCommandLine()
        {
            string output = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-planetPreviewOutput" && i + 1 < args.Length) output = args[++i];
                else if (args[i].StartsWith("-planetPreviewOutput=", StringComparison.Ordinal))
                    output = args[i].Substring("-planetPreviewOutput=".Length);
            }
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                throw new ArgumentException("Supply -planetPreviewOutput with an absolute output directory.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Planet preview requires a real graphics device. Run batchmode without -nographics.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Export the offline preview outside Play Mode.");
            Directory.CreateDirectory(output);

            Scene previousScene = SceneManager.GetActiveScene();
            RenderTexture previousActive = RenderTexture.active;
            bool hadPreference = PlayerPrefs.HasKey(PreferenceKey);
            string previousPreference = PlayerPrefs.GetString(PreferenceKey);
            Scene preview = default;
            BattleShowroomContent content = null;
            PlanetContentApplicator applicator = null;
            Camera camera = null;
            RenderTexture target = null, resolved=null, previousTarget = null;
            Texture2D readback = null;
            Rect previousRect = default;
            float previousAspect = 1f;
            ulong previousCulling = 0;
            bool previousEnabled = false;
            bool previousMsaa=false;
            try
            {
                BattleShowroomPreferences.Save("Content", new BattleShowroomContent.Settings
                    { mapId = "NEPTUNE", showComparisonUnits = true, showGeometry = false });
                preview = EditorSceneManager.OpenPreviewScene(BattleShowroomScenes.Content);
                content = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleShowroomContent>(true)).Single();
                applicator = content.GetComponent<PlanetContentApplicator>();
                Invoke(content, "Start");
                camera = (Camera)typeof(BattleShowroomContent).GetField("_battleCamera", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(content);
                if (camera == null) throw new InvalidOperationException("Content showroom camera is missing.");
                previousTarget = camera.targetTexture; previousRect = camera.rect; previousAspect = camera.aspect;
                previousCulling = camera.overrideSceneCullingMask; previousEnabled = camera.enabled;
                previousMsaa=camera.allowMSAA;camera.allowMSAA=true;
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
                    { name = "Planet battlefield renderer evidence", antiAliasing = 4 };
                if (!target.Create()) throw new InvalidOperationException("Cannot create the planet preview RenderTexture.");
                resolved=new RenderTexture(Width,Height,0,RenderTextureFormat.ARGB32){antiAliasing=1};
                if(!resolved.Create())throw new InvalidOperationException("Cannot create the planet preview resolve texture.");
                readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                camera.enabled = false;
                camera.targetTexture = target;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
                var settings = (BattleShowroomContent.Settings)typeof(BattleShowroomContent)
                    .GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(content);

                foreach (string mapId in PlanetContentCatalog.CanonicalMapIds)
                {
                    settings.mapId = mapId;
                    if (!(bool)Invoke(content, "ApplyMap") || applicator.ActiveBattlefield == null)
                        throw new InvalidOperationException("Actual battlefield Profile failed to apply: " + mapId + ": " + applicator.LastError);
                    Invoke(content, "LateUpdate");
                    FrameForTexture(camera, applicator.ActiveBattlefield.FocusBounds);
                    BattleUnitArtView[] units = content.GetComponentsInChildren<BattleUnitArtView>();
                    if (units.Length != 4 || units.Any(unit => unit.GetComponent<UnitAttack>() != null))
                        throw new InvalidOperationException("The Content preview must have four passive comparison units.");
                    foreach (BattleUnitArtView unit in units) Invoke(unit, "LateUpdate");
                    camera.Render();
                    Graphics.Blit(target,resolved);
                    RenderTexture.active = resolved;
                    readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                    readback.Apply(false, false);
                    string file = Path.Combine(output, mapId + "_720x960.png");
                    File.WriteAllBytes(file, readback.EncodeToPNG());
                    Debug.Log("[PlanetPreview] Rendered actual Profile + four units: " + mapId + " -> " + file);
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                try
                {
                    if (camera != null)
                    {
                        camera.targetTexture = previousTarget; camera.rect = previousRect; camera.aspect = previousAspect;
                        camera.overrideSceneCullingMask = previousCulling; camera.enabled = previousEnabled;
                        camera.allowMSAA=previousMsaa;
                    }
                    try { if (content != null) Invoke(content, "ClearComparisonUnits"); }
                    finally { if (applicator != null) applicator.Clear(); }
                }
                finally
                {
                    try
                    {
                        try { if (preview.IsValid() && preview.isLoaded) EditorSceneManager.ClosePreviewScene(preview); }
                        finally { if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene); }
                    }
                    finally
                    {
                        try
                        {
                            if (readback != null) Object.DestroyImmediate(readback);
                            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                            if (resolved != null) { resolved.Release(); Object.DestroyImmediate(resolved); }
                        }
                        finally
                        {
                            if (hadPreference) PlayerPrefs.SetString(PreferenceKey, previousPreference); else PlayerPrefs.DeleteKey(PreferenceKey);
                            PlayerPrefs.Save();
                        }
                    }
                }
            }
        }

        private static void FrameForTexture(Camera camera, Bounds bounds)
        {
            // The same front-overhead projection as Content.FramePreview, using
            // the output dimensions instead of the editor window/sidebar size.
            Quaternion rotation = Quaternion.Euler(60f, 0f, 0f), inverse = Quaternion.Inverse(rotation);
            Vector3 min = bounds.min, max = bounds.max;
            Bounds projected = new(inverse * min, Vector3.zero);
            for (int i = 0; i < 8; i++)
                projected.Encapsulate(inverse * new Vector3((i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
            camera.rect = new Rect(0f, 0f, 1f, 1f); camera.aspect = (float)Width / Height; camera.orthographic = true;
            camera.transform.SetPositionAndRotation(rotation * projected.center - rotation * Vector3.forward * 30f, rotation);
            camera.orthographicSize = Mathf.Max(projected.extents.y, projected.extents.x / camera.aspect) * 1.05f;
        }

        private static object Invoke(object target, string method)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
