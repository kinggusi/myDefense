using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Combat;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Showroom;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyDefenseGame.Editor.FeatureTesting
{
    public static class BattleShowroomSceneBuilder
    {
        private const string BattlePath = "Assets/Scenes/Battle.unity";
        private const string CommonAlienPath = "Assets/Prefabs/Units/AlienUnit.prefab";
        private const string ConceptArtDirectory = "Assets/Art/Battle/ShowroomConcepts/";
        private const string DarkBoldArtDirectory = ConceptArtDirectory + "VisualVariants/DarkBold/";
        private static readonly string[] BuildScenes =
        {
            BattleShowroomScenes.Hub,
            BattleShowroomScenes.Character,
            BattleShowroomScenes.Combat,
            BattleShowroomScenes.Content
        };

        [MenuItem("Tools/MyDefense/Battle Showroom/Generate Missing Scenes")]
        public static void GenerateMissingScenes() => Generate(rebuildCombat: false, rebuildContent: false);

        public static void RefreshGeneratedCombatScene() => Generate(rebuildCombat: true, rebuildContent: false);

        public static void RefreshGeneratedScenes() => Generate(rebuildCombat: true, rebuildContent: true);

        [MenuItem("Tools/MyDefense/Battle Showroom/Refresh Character Scene")]
        public static void RefreshGeneratedCharacterScene()
            => Generate(rebuildCombat: false, rebuildContent: false, rebuildCharacter: true);

        private static void Generate(bool rebuildCombat, bool rebuildContent, bool rebuildCharacter = false)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before generating Battle Showroom scenes.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BattlePath) == null)
                throw new InvalidOperationException("The authored Battle scene is missing.");
            if (!AssetDatabase.IsValidFolder(BattleShowroomScenes.Directory))
                AssetDatabase.CreateFolder("Assets/Scenes/Tests", "Battle");
            if (!rebuildCombat && !rebuildContent && !rebuildCharacter
                && BuildScenes.Skip(1).All(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null))
                return;

            Scene previousScene = SceneManager.GetActiveScene();
            Scene source = SceneManager.GetSceneByPath(BattlePath);
            bool openedSource = !source.IsValid() || !source.isLoaded;
            bool replacingUntitled = openedSource && SceneManager.sceneCount == 1
                && string.IsNullOrEmpty(previousScene.path) && !previousScene.isDirty;
            if (openedSource)
                source = EditorSceneManager.OpenScene(BattlePath, replacingUntitled ? OpenSceneMode.Single : OpenSceneMode.Additive);
            try
            {
                if (source.isDirty)
                    throw new InvalidOperationException("Save authored Battle scene changes before generating test scenes.");
                Camera sourceCamera = FindInScene<Camera>(source).Single(camera => camera.CompareTag("MainCamera"));
                Light sourceLight = FindInScene<Light>(source).FirstOrDefault(light => light.type == LightType.Directional);
                CreateMissing(BattleShowroomScenes.Character, () => BuildCharacter(source, sourceCamera, sourceLight), rebuildCharacter);
                CreateMissing(BattleShowroomScenes.Combat, () => BuildCombat(source, sourceCamera, sourceLight), rebuildCombat);
                CreateMissing(BattleShowroomScenes.Content, () => BuildContent(source, sourceCamera, sourceLight), rebuildContent);
            }
            finally
            {
                if (replacingUntitled)
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                else if (previousScene.IsValid() && previousScene.isLoaded)
                    SceneManager.SetActiveScene(previousScene);
                if (openedSource && !replacingUntitled)
                    EditorSceneManager.CloseScene(source, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleShowroom] Isolated test scenes are available.");
        }

        [MenuItem("Tools/MyDefense/Battle Showroom/Open Feature Test Hub")]
        public static void OpenHub()
        {
            GenerateMissingScenes();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(BattleShowroomScenes.Hub, OpenSceneMode.Single);
        }

        [MenuItem("Tools/MyDefense/Battle Showroom/Build Windows Development Client")]
        public static void BuildWindowsDevelopmentClient()
        {
            string path = EditorUtility.SaveFilePanel("Build Battle Showroom Development Client", "", "BattleShowroom", "exe");
            if (!string.IsNullOrEmpty(path))
                BuildWindowsDevelopmentClientAt(path);
        }

        public static void BuildWindowsDevelopmentClientAt(string path)
        {
            GenerateMissingScenes();
            if (!string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Windows Development build output must end in .exe.", nameof(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException("Battle Showroom Development build failed: " + report.summary.result);
        }

        public static void BuildWindowsDevelopmentClientFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(arguments, "-battleShowroomBuildOutput");
            if (output < 0 || output + 1 == arguments.Length)
                throw new ArgumentException("Supply -battleShowroomBuildOutput <absolute Windows .exe path>.");
            BuildWindowsDevelopmentClientAt(arguments[output + 1]);
        }

        private static void CreateMissing(string path, Action createRoots, bool rebuild = false)
        {
            if (!rebuild && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return;
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException("Close the existing generated scene before replacing it: " + path);
            Scene previous = SceneManager.GetActiveScene();
            Scene generated = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(generated);
            try
            {
                createRoots();
                if (!EditorSceneManager.SaveScene(generated, path))
                    throw new InvalidOperationException("Could not save Battle Showroom test scene " + path);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(generated, true);
            }
        }

        private static void BuildCharacter(Scene source, Camera sourceCamera, Light sourceLight)
        {
            Camera camera = CopyCamera(sourceCamera);
            CopyLight(sourceLight);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonAlienPath);
            if (prefab == null)
                Debug.LogWarning("[BattleShowroom] Common AlienUnit prefab is missing: " + CommonAlienPath);
            var root = new GameObject("Battle Showroom Character");
            Transform board = FindBoard(FindInScene<Transform>(source), "GridManager");
            var stage = new GameObject("Character Board Stage");
            stage.transform.SetParent(root.transform, false);
            Transform anchor = board.Find("Tile_2_1");
            if (anchor == null)
                throw new InvalidOperationException("Authored Battle Tile_2_1 is missing.");
            var tiles = new MeshRenderer[24];
            MeshRenderer actorTile = null;
            Bounds boardBounds = new(Vector3.zero, Vector3.zero);
            for (int row = 0; row < 4; row++)
            for (int column = 0; column < 6; column++)
            {
                string tileName = $"Tile_{column}_{row}";
                Transform original = board.Find(tileName);
                if (original == null || !original.TryGetComponent(out MeshFilter originalMesh)
                    || !original.TryGetComponent(out MeshRenderer originalRenderer))
                    throw new InvalidOperationException($"Authored Battle {tileName} mesh or renderer is missing.");
                MeshRenderer tile = CopyBoardMesh(stage.transform, tileName, originalMesh.sharedMesh,
                    originalRenderer.sharedMaterials, original.localPosition - anchor.localPosition,
                    original.localRotation, original.localScale);
                tiles[row * 6 + column] = tile;
                boardBounds.Encapsulate(tile.bounds);
                if (original == anchor)
                    actorTile = tile;
            }
            Vector3 backingCenter = boardBounds.center;
            backingCenter.y = boardBounds.min.y - 0.085f;
            Vector3 backingSize = boardBounds.size + new Vector3(0.36f, 0f, 0.36f);
            backingSize.y = 0.24f;
            MeshRenderer frame = CopyBoardMesh(stage.transform, "Board Frame", tiles[0].GetComponent<MeshFilter>().sharedMesh,
                tiles[0].sharedMaterials, backingCenter, Quaternion.identity, backingSize);
            stage.SetActive(false);
            var concept = root.AddComponent<BattleShowroomConceptPreview>();
            Shader relief = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Battle/Showroom/BattleShowroomRelief.shader");
            concept.ConfigureForEditor(camera, stage, actorTile, tiles, frame, LoadBoardTexture(), relief,
                LoadConceptAtlas("DiadBoardViews"), LoadConceptAtlas("MecharonBoardViews"),
                LoadConceptAtlas("PuppeteerBoardViews"), LoadConceptAtlas("PuppeteerDollBoardViews"),
                LoadConceptAtlas("DiadBoardAttack"), LoadConceptAtlas("MecharonBoardAttack"),
                LoadConceptAtlas("PuppeteerBoardAttack"), LoadConceptAtlas("PuppeteerDollBoardAttack"),
                LoadEffectAtlas("MecharonBoardEffects"), LoadEffectAtlas("PuppeteerBoardEffects"),
                LoadConceptAtlas("DiadBoardWindup"), LoadConceptAtlas("MecharonBoardWindup"),
                LoadConceptAtlas("PuppeteerBoardWindup"), LoadConceptAtlas("PuppeteerDollBoardWindup"),
                LoadConceptAtlas("DiadBoardRecover"), LoadConceptAtlas("MecharonBoardRecover"),
                LoadConceptAtlas("PuppeteerBoardRecover"), LoadConceptAtlas("PuppeteerDollBoardRecover"),
                LoadEffectAtlas("DiadBoardEffects"));
            ConfigureCharacterLookVariants(concept);
            root.AddComponent<BattleShowroomCharacter>().ConfigureForEditor(
                camera, sourceCamera.transform.rotation, prefab != null ? prefab.GetComponent<MeshFilter>() : null,
                prefab != null ? prefab.GetComponent<MeshRenderer>() : null, concept);
        }

        private static MeshRenderer CopyBoardMesh(Transform parent, string name, Mesh mesh, Material[] materials,
            Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var copy = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            copy.transform.SetParent(parent, false);
            copy.transform.SetLocalPositionAndRotation(position, rotation);
            copy.transform.localScale = scale;
            copy.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = copy.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private static BattleShowroomEffectAtlas LoadEffectAtlas(string fileName)
        {
            string path = ConceptArtDirectory + fileName + ".png";
            Texture2D texture = ReadShowroomTexture(path);
            if (texture == null)
                return null;
            RectInt[] frames;
            try
            {
                frames = BattleShowroomEffectAtlas.FindFrames(texture.GetPixels32(), texture.width, texture.height);
            }
            finally
            {
                EndReadingShowroomTexture(path);
            }
            return new BattleShowroomEffectAtlas(AssetDatabase.LoadAssetAtPath<Texture2D>(path), frames);
        }

        private static Texture2D LoadBoardTexture()
        {
            string path = ConceptArtDirectory + "SFBoardTiles.png";
            if (ReadShowroomTexture(path) == null)
                return null;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ConfigureCharacterLookVariants(BattleShowroomConceptPreview concept)
        {
            var errors = new List<string>();
            Texture2D originalBoard = AssetDatabase.LoadAssetAtPath<Texture2D>(ConceptArtDirectory + "SFBoardTiles.png");
            Texture2D boardVariant = LoadVisualVariant(originalBoard, errors);
            Texture2D[] unitTextures = typeof(BattleShowroomConceptPreview)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Where(field => field.FieldType == typeof(BattleShowroomTileAtlas))
                .Select(field => field.GetValue(concept) as BattleShowroomTileAtlas)
                .Where(atlas => atlas?.Texture != null)
                .Select(atlas => atlas.Texture)
                .Distinct()
                .OrderBy(texture => texture.name, StringComparer.Ordinal)
                .Select(texture => LoadVisualVariant(texture, errors))
                .Where(texture => texture != null)
                .ToArray();
            string issue = errors.Count == 0 ? string.Empty : errors[0];
            if (errors.Count > 0)
                Debug.LogWarning("[BattleShowroom] Dark + Bold artwork is unavailable.\n" + string.Join("\n", errors));
            concept.ConfigureLookVariantsForEditor(boardVariant, unitTextures, issue);
        }

        private static Texture2D LoadVisualVariant(Texture2D original, List<string> errors)
        {
            if (original == null)
            {
                errors.Add("Dark + Bold source artwork is missing.");
                return null;
            }
            string variantPath = DarkBoldArtDirectory + original.name + ".png";
            if (AssetImporter.GetAtPath(variantPath) is not TextureImporter variantImporter)
            {
                errors.Add("Dark + Bold artwork is missing: " + variantPath);
                return null;
            }
            string originalPath = AssetDatabase.GetAssetPath(original);
            var originalPixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var variantPixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string originalFile = Path.Combine(Application.dataPath, originalPath.Substring("Assets/".Length));
                string variantFile = Path.Combine(Application.dataPath, variantPath.Substring("Assets/".Length));
                if (!originalPixels.LoadImage(File.ReadAllBytes(originalFile))
                    || !variantPixels.LoadImage(File.ReadAllBytes(variantFile)))
                {
                    errors.Add("Dark + Bold artwork cannot be read: " + variantPath);
                    return null;
                }
                if (!ValidateVisualVariant(originalPixels, variantPixels, out string issue))
                {
                    errors.Add(variantPath + ": " + issue);
                    return null;
                }
            }
            catch (IOException exception)
            {
                errors.Add(variantPath + ": " + exception.Message);
                return null;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalPixels);
                UnityEngine.Object.DestroyImmediate(variantPixels);
            }

            var originalImporter = (TextureImporter)AssetImporter.GetAtPath(originalPath);
            var settings = new TextureImporterSettings();
            originalImporter.ReadTextureSettings(settings);
            variantImporter.SetTextureSettings(settings);
            variantImporter.maxTextureSize = originalImporter.maxTextureSize;
            variantImporter.textureCompression = originalImporter.textureCompression;
            variantImporter.crunchedCompression = originalImporter.crunchedCompression;
            variantImporter.compressionQuality = originalImporter.compressionQuality;
            variantImporter.isReadable = false;
            variantImporter.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(variantPath);
        }

        public static bool ValidateVisualVariant(Texture2D original, Texture2D variant, out string issue)
        {
            if (original.width != variant.width || original.height != variant.height)
            {
                issue = "Canvas dimensions differ from the original.";
                return false;
            }
            Color32[] sourcePixels = original.GetPixels32();
            Color32[] variantPixels = variant.GetPixels32();
            for (int index = 0; index < sourcePixels.Length; index++)
            {
                if (sourcePixels[index].a != variantPixels[index].a)
                {
                    issue = "Alpha differs from the original at pixel " + index + ".";
                    return false;
                }
            }
            issue = string.Empty;
            return true;
        }

        private static BattleShowroomTileAtlas LoadConceptAtlas(string fileName)
        {
            string path = ConceptArtDirectory + fileName + ".png";
            Texture2D texture = ReadShowroomTexture(path);
            if (texture == null)
                return null;
            RectInt[] frames;
            float[] feet;
            try
            {
                Color32[] pixels = texture.GetPixels32();
                frames = BattleShowroomTileAtlas.FindAlphaFrames(pixels, texture.width, texture.height);
                feet = BattleShowroomTileAtlas.FindFootPivots(pixels, texture.width, frames);
            }
            finally
            {
                EndReadingShowroomTexture(path);
            }
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var atlas = new BattleShowroomTileAtlas(texture, frames, feet);
            if (!atlas.IsAvailable)
                Debug.LogWarning("[BattleShowroom] Direction atlas needs three opaque, equal-width frames: " + path);
            return atlas;
        }

        private static Texture2D ReadShowroomTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogWarning("[BattleShowroom] Artwork is missing: " + path);
                return null;
            }
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            bool unitPose=path.EndsWith("BoardViews.png",StringComparison.Ordinal)||path.EndsWith("BoardWindup.png",StringComparison.Ordinal)
                ||path.EndsWith("BoardAttack.png",StringComparison.Ordinal)||path.EndsWith("BoardRecover.png",StringComparison.Ordinal);
            importer.mipmapEnabled = unitPose;
            importer.mipMapsPreserveCoverage=unitPose;importer.alphaTestReferenceValue=.27f;
            importer.filterMode=unitPose?FilterMode.Trilinear:FilterMode.Bilinear;importer.anisoLevel=unitPose?4:1;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EndReadingShowroomTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void BuildCombat(Scene source, Camera sourceCamera, Light sourceLight)
        {
            CopyCamera(sourceCamera);
            CopyLight(sourceLight);
            var pathRoot = new GameObject("Battle Waypoints");
            pathRoot.SetActive(false);
            foreach (WaypointGroup original in FindInScene<WaypointGroup>(source))
            {
                var groupRoot = new GameObject(original.name);
                groupRoot.transform.SetParent(pathRoot.transform, false);
                EditorUtility.CopySerialized(original, groupRoot.AddComponent<WaypointGroup>());
                foreach (Transform point in original.Waypoints)
                {
                    var copy = new GameObject(point.name);
                    copy.transform.SetParent(groupRoot.transform, false);
                    copy.transform.SetPositionAndRotation(point.position, point.rotation);
                }
            }
            pathRoot.AddComponent<PathManager>();
            pathRoot.SetActive(true);

            var template = new GameObject("Offline Battle Session Template");
            template.SetActive(false);
            template.AddComponent<NetworkObject>();
            template.AddComponent<BattleRunnerLifecycle>();
            var executor = template.AddComponent<BattleWaveExecutor>();
            template.AddComponent<BattleWaveStateAuthority>();
            var projectileSpawner = template.AddComponent<BattleProjectileSpawner>();
            EditorUtility.CopySerialized(FindInScene<BattleProjectileSpawner>(source).Single(), projectileSpawner);
            BattleWaveExecutor sourceExecutor = FindInScene<BattleWaveExecutor>(source).Single();
            EditorUtility.CopySerialized(sourceExecutor, executor);
            var originalConfig = new SerializedObject(sourceExecutor);
            var config = new SerializedObject(executor);
            config.FindProperty("_autoStartOnPlay").boolValue = false;
            var originalSpawn = originalConfig.FindProperty("_spawnPoint").objectReferenceValue as Transform;
            if (originalSpawn == null)
                throw new InvalidOperationException("Authored Battle spawn reference is missing.");
            var spawn = new GameObject(originalSpawn.name);
            spawn.transform.SetParent(template.transform, false);
            spawn.transform.SetPositionAndRotation(originalSpawn.position, originalSpawn.rotation);
            config.FindProperty("_spawnPoint").objectReferenceValue = spawn.transform;
            config.ApplyModifiedPropertiesWithoutUndo();
            var showroom = new GameObject("Battle Showroom Combat");
            var probe = showroom.AddComponent<BattleShowroomCombatProbe>();
            GameObject alien = AssetDatabase.LoadAssetAtPath<GameObject>(CommonAlienPath);
            Transform localBoard = FindBoard(FindInScene<Transform>(source), "GridManager");
            probe.ConfigureForEditor(alien, Slots(localBoard, localBoard.position, mirrored: false));
            showroom.AddComponent<BattleShowroomCombat>().ConfigureForEditor(template, probe);
        }

        private static void BuildContent(Scene source, Camera sourceCamera, Light sourceLight)
        {
            Camera camera = CopyCamera(sourceCamera);
            Light light = CopyLight(sourceLight);
            var root = new GameObject("Battle Showroom Content");
            var applicator = root.AddComponent<PlanetContentApplicator>();
            applicator.ConfigureForTests(null, root.transform, camera, light);
            WaypointGroup[] groups = FindInScene<WaypointGroup>(source);
            Transform[] authored = source.GetRootGameObjects().SelectMany(rootObject =>
                rootObject.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform player1Board = FindBoard(authored, "GridManager");
            Transform player2Board = FindBoard(authored, "EnemyGridParent");
            root.AddComponent<BattleShowroomContent>().ConfigureForEditor(
                camera, applicator,
                Slots(player1Board, player1Board.position, mirrored: false),
                Slots(player2Board, player2Board.position, mirrored: true),
                Slots(player2Board, player1Board.position, mirrored: false),
                Slots(player1Board, player2Board.position, mirrored: true),
                Lane(groups, LaneType.Player1Lane), Lane(groups, LaneType.Player2Lane), Lane(groups, LaneType.BossSharedLane));
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static Transform FindBoard(Transform[] transforms, string name)
        {
            Transform board = transforms.SingleOrDefault(item => item.name == name);
            if (board == null)
                throw new InvalidOperationException("Authored Battle board is missing: " + name);
            return board;
        }

        private static Vector3[] Slots(Transform board, Vector3 position, bool mirrored)
        {
            Quaternion rotation = mirrored ? board.rotation * Quaternion.Euler(0f, 180f, 0f) : board.rotation;
            Matrix4x4 pose = Matrix4x4.TRS(position, rotation, board.lossyScale);
            return Enumerable.Range(0, 24).Select(index =>
            {
                int row = 3 - index / 6;
                int column = mirrored ? 5 - index % 6 : index % 6;
                Transform tile = board.Find($"Tile_{column}_{row}");
                if (tile == null)
                    throw new InvalidOperationException($"Authored Battle board '{board.name}' is missing Tile_{column}_{row}.");
                return pose.MultiplyPoint3x4(tile.localPosition);
            }).ToArray();
        }

        private static Vector3[] Lane(WaypointGroup[] groups, LaneType lane)
        {
            WaypointGroup group = groups.SingleOrDefault(item => item.Lane == lane);
            if (group == null || group.Waypoints.Count < 2)
                throw new InvalidOperationException("Authored Battle path is missing: " + lane);
            return group.Waypoints.Select(point => point.position).ToArray();
        }

        private static Camera CopyCamera(Camera original)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
            var copy = cameraObject.GetComponent<Camera>();
            copy.CopyFrom(original);
            copy.targetTexture = null;
            return copy;
        }

        private static Light CopyLight(Light original)
        {
            if (original == null)
                return null;
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
            var copy = lightObject.GetComponent<Light>();
            EditorUtility.CopySerialized(original, copy);
            return copy;
        }
    }

    internal sealed class BattleShowroomBuildGuard : BuildPlayerProcessor
    {
        public override void PrepareForBuild(BuildPlayerContext context)
        {
            BuildPlayerOptions options = context.BuildPlayerOptions;
            if ((options.options & BuildOptions.Development) != 0)
                return;
            if ((options.scenes ?? Array.Empty<string>()).Any(path => BattleShowroomScenes.IsShowroomScene(path)
                || string.Equals(path, BattleShowroomScenes.Hub, StringComparison.OrdinalIgnoreCase)))
            {
                throw new BuildFailedException("Battle Showroom and Feature Test Hub require a Development build.");
            }
        }
    }
}
