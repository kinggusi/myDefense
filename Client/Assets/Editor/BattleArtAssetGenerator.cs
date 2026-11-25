using System;
using System.IO;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Showroom;
using MyDefenseGame.Editor.FeatureTesting;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MyDefense.Battle.Editor
{
    public static class BattleArtAssetGenerator
    {
        private const string Source = "Assets/Art/Battle/ShowroomConcepts/";
        private const string Variants = Source + "VisualVariants/DarkBold/";
        private const string Folder = "Assets/Resources/Battle/Presentation";
        public const string CatalogAssetPath = Folder + "/BattleUnitArtCatalog.asset";

        public static void BuildBattleClientFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string output = null; bool production = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-battleArtProductionCheck", StringComparison.OrdinalIgnoreCase)) production = true;
                if (string.Equals(args[i], "-battleArtBuildOutput", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length) output = args[++i];
            }
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output)
                || !string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Supply -battleArtBuildOutput with an absolute Client.exe path.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // The scene list is scoped to this invocation. Never change the
            // project's production Build Settings or Player/Project settings.
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Battle.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = production ? BuildOptions.None : BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Battle-only build failed: " + report.summary.result);
            Debug.Log("[BattleArt] Battle-only " + (production ? "production check" : "Development") + " build: " + output);
        }

        [MenuItem("MyDefense/Battle/Generate Battle Presentation Assets")]
        public static void GeneratePresentationAssets()
        {
            AssetDatabase.Refresh();
            EnsureFolder(Folder);
            ValidateRawPair(Source + "SFBoardTiles.png", Variants + "SFBoardTiles.png");
            BattleUnitPoseArt diad = Unit("Diad"), mecharon = Unit("Mecharon"), puppeteer = Unit("Puppeteer"), doll = Unit("PuppeteerDoll");
            var catalog = AssetDatabase.LoadAssetAtPath<BattleUnitArtCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BattleUnitArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }
            Shader relief = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Battle/Presentation/BattleUnitRelief.shader");
            Shader board = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Battle/Presentation/BattleBoardSurface.shader");
            if (relief == null || board == null) throw new InvalidOperationException("Battle presentation shaders missing.");
            catalog.Configure(diad, mecharon, puppeteer, doll, Effect("Diad"), Effect("Mecharon"), Effect("Puppeteer"),
                Import(Variants + "SFBoardTiles.png", false), relief, board);
            foreach (BattleUnitVisual kind in Enum.GetValues(typeof(BattleUnitVisual)))
                if (!catalog.Available(kind, false) || !catalog.Available(kind, true))
                    throw new InvalidOperationException("Incomplete Battle artwork for " + kind);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("[BattleArt] Generated production-safe presentation catalog. No canonical Alien IDs were assigned.");
        }

        private static BattleUnitPoseArt Unit(string name)
        {
            string[] suffixes = { "Views", "Windup", "Attack", "Recover" };
            var poses = new BattleDirectionArt[4];
            for (int i = 0; i < poses.Length; i++)
            {
                string originalPath = Source + name + "Board" + suffixes[i] + ".png";
                string candidatePath = Variants + name + "Board" + suffixes[i] + ".png";
                ValidateRawPair(originalPath, candidatePath);
                Texture2D original = Import(originalPath, true,true);
                Color32[] pixels = original.GetPixels32();
                RectInt[] frames = BattleShowroomTileAtlas.FindAlphaFrames(pixels, original.width, original.height);
                float[] feet = BattleShowroomTileAtlas.FindFootPivots(pixels, original.width, frames);
                Texture2D candidate = Import(candidatePath, false,true);
                poses[i] = new BattleDirectionArt(original, candidate, frames, feet);
                SetReadable(originalPath, false);
            }
            return new BattleUnitPoseArt(poses);
        }

        private static BattleEffectArt Effect(string name)
        {
            string path = Source + name + "BoardEffects.png";
            Texture2D texture = Import(path, true);
            RectInt[] frames = BattleShowroomEffectAtlas.FindFrames(texture.GetPixels32(), texture.width, texture.height);
            SetReadable(path, false);
            return new BattleEffectArt(texture, frames);
        }

        private static void ValidateRawPair(string originalPath, string candidatePath)
        {
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string originalFile = Path.Combine(Application.dataPath, originalPath.Substring("Assets/".Length));
                string candidateFile = Path.Combine(Application.dataPath, candidatePath.Substring("Assets/".Length));
                if (!original.LoadImage(File.ReadAllBytes(originalFile)) || !candidate.LoadImage(File.ReadAllBytes(candidateFile))
                    || !BattleShowroomSceneBuilder.ValidateVisualVariant(original, candidate, out _))
                    throw new InvalidOperationException("Original/candidate dimensions or alpha differ: " + candidatePath);
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(candidate); }
        }

        private static Texture2D Import(string path, bool readable,bool unitPose=false)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new FileNotFoundException("Required Battle artwork missing", path);
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.mipmapEnabled = unitPose; importer.alphaIsTransparency = true;
            importer.mipMapsPreserveCoverage=unitPose;importer.alphaTestReferenceValue=.27f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = unitPose?FilterMode.Trilinear:FilterMode.Bilinear;
            importer.anisoLevel=unitPose?4:1;
            importer.isReadable = readable; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void SetReadable(string path, bool value)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = value; importer.SaveAndReimport();
        }
        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string folder = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = folder + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(folder, parts[i]);
                folder = next;
            }
        }
    }
}
