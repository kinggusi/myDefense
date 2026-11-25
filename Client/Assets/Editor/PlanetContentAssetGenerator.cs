using System;
using System.Collections.Generic;
using System.IO;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyDefense.Battle.Editor
{
    public static class PlanetContentAssetGenerator
    {
        private const string ResourcesFolder = "Assets/Resources/Battle/PlanetContent";
        private const string ProfilesFolder = ResourcesFolder + "/Profiles";
        private const string PrefabsFolder = "Assets/Prefabs/Battle/PlanetContent";
        private const string MaterialsFolder = "Assets/Materials/Battle/PlanetContent";
        private const string CatalogPath = ResourcesFolder + "/PlanetContentCatalog.asset";

        private static readonly Vector3 PlanetCenter = new(7.55f, 0.08f, 1.35f);

        private static readonly PlanetVisualRecipe[] Recipes =
        {
            new("NEPTUNE", new Color(0.04f, 0.18f, 0.62f), new Color(0.10f, 0.82f, 1f),
                new Color(0.20f, 0.58f, 1f), 3.2f, 3f),
            new("URANUS", new Color(0.42f, 0.86f, 0.90f), new Color(0.80f, 1f, 1f),
                new Color(0.45f, 0.95f, 1f), 2.8f, 2f),
            new("SATURN", new Color(0.78f, 0.59f, 0.27f), new Color(1f, 0.88f, 0.55f),
                new Color(1f, 0.72f, 0.24f), 3.05f, 2f),
            new("JUPITER", new Color(0.72f, 0.43f, 0.24f), new Color(0.95f, 0.74f, 0.48f),
                new Color(1f, 0.48f, 0.24f), 3.7f, 3f),
            new("MARS", new Color(0.67f, 0.15f, 0.07f), new Color(0.26f, 0.06f, 0.035f),
                new Color(1f, 0.34f, 0.12f), 2.55f, 4f),
            new("EARTH", new Color(0.05f, 0.34f, 0.70f), new Color(0.12f, 0.62f, 0.25f),
                new Color(0.50f, 0.86f, 1f), 3.0f, 3f),
            new("VENUS", new Color(0.88f, 0.61f, 0.20f), new Color(1f, 0.88f, 0.58f),
                new Color(1f, 0.68f, 0.25f), 2.9f, 6f),
            new("MERCURY", new Color(0.48f, 0.45f, 0.42f), new Color(0.20f, 0.18f, 0.17f),
                new Color(0.75f, 0.72f, 0.68f), 1.85f, 0.5f),
            new("SUN", new Color(1f, 0.35f, 0.025f), new Color(1f, 0.82f, 0.12f),
                new Color(1f, 0.52f, 0.04f), 3.4f, 10f)
        };

        [MenuItem("MyDefense/Battle/Generate P2 Planet Placeholder Content")]
        public static void GeneratePlaceholderContent()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ProfilesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);

            if (Recipes.Length != PlanetContentCatalog.CanonicalMapIds.Count)
                throw new InvalidOperationException("Planet visual recipe count must match canonical map IDs.");

            var profiles = new List<PlanetContentProfile>();
            for (int index = 0; index < Recipes.Length; index++)
            {
                PlanetVisualRecipe recipe = Recipes[index];
                string canonicalMapId = PlanetContentCatalog.CanonicalMapIds[index];
                if (!string.Equals(recipe.MapId, canonicalMapId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Planet visual recipe order drifted from canonical map IDs. Expected '"
                        + canonicalMapId + "', got '" + recipe.MapId + "'.");

                Material background = CreateOrUpdateMaterial(
                    recipe.MapId, "Environment", Color.Lerp(Color.black, recipe.BaseColor, 0.16f), false);
                Material body = CreateOrUpdateMaterial(recipe.MapId, "Body", recipe.BaseColor, false);
                Material accent = CreateOrUpdateMaterial(recipe.MapId, "Accent", recipe.AccentColor, false);
                Material glow = CreateOrUpdateMaterial(recipe.MapId, "Glow", recipe.GlowColor, false);
                Material particle = CreateOrUpdateMaterial(
                    recipe.MapId, "Particle", WithAlpha(recipe.GlowColor, 0.75f), true);

                GameObject environmentPrefab = CreateOrUpdateEnvironmentPrefab(
                    recipe, background, body, accent, glow);
                GameObject effectPrefab = CreateOrUpdateEffectPrefab(recipe, particle);
                PlanetContentProfile profile = CreateOrUpdateProfile(
                    recipe, background, environmentPrefab, effectPrefab);
                profiles.Add(profile);
            }

            PlanetContentCatalog catalog = AssetDatabase.LoadAssetAtPath<PlanetContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlanetContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.ConfigureForEditor(profiles);
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateGeneratedCatalog(catalog);
            Debug.Log(
                "[PlanetContent] Generated nine camera-safe primitive planet placeholders. Catalog="
                + CatalogPath);
        }

        [MenuItem("MyDefense/Battle/Apply Painted Planet Backgrounds")]
        public static void GeneratePaintedContent()
        {
            AssetDatabase.Refresh();
            EnsureFolder(ResourcesFolder); EnsureFolder(ProfilesFolder); EnsureFolder(PrefabsFolder);
            // Validate all files before touching an existing Profile or Prefab.
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (PlanetVisualRecipe recipe in Recipes)
            {
                string artPath = "Assets/Art/Battle/Planets/" + recipe.MapId + "_Background.png";
                if (AssetImporter.GetAtPath(artPath) is not TextureImporter importer)
                    throw new FileNotFoundException("Required planet background is missing", artPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 100f;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
                importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = false; importer.SaveAndReimport();
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                if (sprite == null) throw new InvalidOperationException("Cannot import planet background: " + artPath);
                sprites.Add(recipe.MapId, sprite);
            }
            var profiles = new List<PlanetContentProfile>();
            foreach (PlanetVisualRecipe recipe in Recipes)
            {
                string prefabPath = PrefabsFolder + "/" + recipe.MapId + "_Environment.prefab";
                var root = new GameObject(recipe.MapId + "_Environment");
                GameObject environment;
                try
                {
                    root.AddComponent<PlanetEnvironmentContent>();
                    // Profile.BackgroundSprite supplies the complete painted
                    // environment. No old primitive planet/ambient FX remain.
                    environment = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                if (environment == null) throw new InvalidOperationException("Could not save " + prefabPath);
                string profilePath = ProfilesFolder + "/" + recipe.MapId + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(profilePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
                    AssetDatabase.CreateAsset(profile, profilePath);
                }
                profile.ConfigureForEditor(recipe.MapId, true, environment, sprites[recipe.MapId],
                    new Color(.025f, .035f, .065f), null,
                    new PlanetLightingSettings(Color.gray, .8f, Color.white, 1f, new Vector3(50f, -30f, 0f)),
                    Array.Empty<GameObject>());
                profile.ConfigureBattlefieldForEditor(false, null, null);
                EditorUtility.SetDirty(profile); profiles.Add(profile);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<PlanetContentCatalog>(CatalogPath);
            if (catalog == null)
            { catalog = ScriptableObject.CreateInstance<PlanetContentCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.ConfigureForEditor(profiles); EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets(); ValidateGeneratedCatalog(catalog);
            Debug.Log("[PlanetContent] Applied all nine painted backgrounds. Board/Lane/Waypoint/Wave/Boss unchanged.");
        }

        [MenuItem("MyDefense/Battle/Apply Planet Battlefield Surface Themes")]
        public static void GenerateBattlefieldContent()
            =>GenerateBattlefieldContentInternal(false);

        [MenuItem("MyDefense/Battle/Apply Matte Planet Battlefield Themes")]
        public static void GenerateMatteBattlefieldContent()=>GenerateBattlefieldContentInternal(true);

        [MenuItem("MyDefense/Battle/Apply Detailed SF Planet Battlefield Themes")]
        public static void GenerateDetailedBattlefieldContent()=>GenerateBattlefieldContentInternal(true,true);

        private static void GenerateBattlefieldContentInternal(bool matte,bool detailed=false)
        {
            string art = detailed?"Assets/Art/Battle/Planets/SFDetailBattlefields/":matte?"Assets/Art/Battle/Planets/MatteBattlefields/":"Assets/Art/Battle/Planets/Battlefields/";
            string commonPath="Assets/Art/Battle/Planets/MatteBattlefields/SF_MatteSurfaces.png";
            AssetDatabase.Refresh();
            // All eighteen files must exist before any Profile/Prefab is changed.
            foreach (PlanetVisualRecipe recipe in Recipes)
            foreach (string suffix in new[] { "_Terrain.png", "_Surfaces.png" })
            {
                string path=matte&&suffix=="_Surfaces.png"?commonPath:art+recipe.MapId+suffix;
                if (!File.Exists(Path.Combine(Application.dataPath,path.Substring(7))))throw new FileNotFoundException("Required battlefield artwork missing",path);
            }
            var terrain = new Dictionary<string, Texture2D>();
            var surfaces = new Dictionary<string, Texture2D>();
            Texture2D common=matte?ImportBattlefieldTexture(commonPath):null;
            foreach (PlanetVisualRecipe recipe in Recipes)
            {
                Texture2D floor = ImportBattlefieldTexture(art + recipe.MapId + "_Terrain.png");
                Texture2D atlas = matte?common:ImportBattlefieldTexture(art + recipe.MapId + "_Surfaces.png");
                if (floor.height <= floor.width || atlas.width != atlas.height || atlas.width % 2 != 0)
                    throw new InvalidOperationException("Invalid battlefield Terrain/2x2 Surfaces shape: " + recipe.MapId);
                terrain.Add(recipe.MapId, floor); surfaces.Add(recipe.MapId, atlas);
            }
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Battle/Presentation/BattleBoardSurface.shader");
            if (shader == null) throw new InvalidOperationException("Battlefield surface shader missing.");
            EnsureFolder(ResourcesFolder); EnsureFolder(ProfilesFolder); EnsureFolder(PrefabsFolder); EnsureFolder(MaterialsFolder);
            var profiles = new List<PlanetContentProfile>();
            foreach (PlanetVisualRecipe recipe in Recipes)
            {
                string materialPath = MaterialsFolder + "/" + recipe.MapId + "_Battlefield.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
                material.shader = shader; material.mainTexture = terrain[recipe.MapId]; EditorUtility.SetDirty(material);
                string prefabPath = PrefabsFolder + "/" + recipe.MapId + "_Environment.prefab";
                var root = new GameObject(recipe.MapId + "_Environment");
                GameObject environment;
                try { root.AddComponent<PlanetEnvironmentContent>(); environment = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                if (environment == null) throw new InvalidOperationException("Cannot save " + prefabPath);
                string profilePath = ProfilesFolder + "/" + recipe.MapId + ".asset";
                PlanetContentProfile profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(profilePath);
                if (profile == null) { profile = ScriptableObject.CreateInstance<PlanetContentProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
                profile.ConfigureForEditor(recipe.MapId, true, environment, null, new Color(.025f, .035f, .065f), material,
                    new PlanetLightingSettings(Color.gray, .8f, Color.white, 1f, new Vector3(50f, -30f, 0f)), Array.Empty<GameObject>());
                // Curated crop for the delivered Venus artwork: top-origin row
                // boundary 589 of 1254. Keep the original PNG entirely unchanged.
                Vector2 split = !matte && recipe.MapId == "VENUS" && surfaces[recipe.MapId].height == 1254
                    ? new Vector2(.5f, 665f / 1254f) : new Vector2(.5f, .5f);
                profile.ConfigureBattlefieldForEditor(true, terrain[recipe.MapId], surfaces[recipe.MapId], split);
                EditorUtility.SetDirty(profile); profiles.Add(profile);
            }
            PlanetContentCatalog catalog = AssetDatabase.LoadAssetAtPath<PlanetContentCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<PlanetContentCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.ConfigureForEditor(profiles); EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets(); ValidateGeneratedCatalog(catalog);
            Debug.Log("[PlanetContent] Applied nine required battlefield themes. Existing boards/paths/physics and old PNGs preserved.");
        }

        private static Texture2D ImportBattlefieldTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.isReadable = false;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateOrUpdateMaterial(
            string mapId,
            string suffix,
            Color color,
            bool particle)
        {
            string path = MaterialsFolder + "/" + mapId + "_" + suffix + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = FindSupportedShader(particle);

            if (material == null)
            {
                material = new Material(shader) { name = mapId + "_" + suffix };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            SetMaterialColor(material, color);
            if (particle)
                ConfigureParticleTransparency(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindSupportedShader(bool particle)
        {
            string[] names = particle
                ? new[]
                {
                    "Universal Render Pipeline/Particles/Unlit",
                    "Particles/Standard Unlit",
                    "Legacy Shaders/Particles/Alpha Blended",
                    "Sprites/Default"
                }
                : new[]
                {
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default"
                };

            for (int index = 0; index < names.Length; index++)
            {
                Shader shader = Shader.Find(names[index]);
                if (shader != null && shader.isSupported)
                    return shader;
            }

            throw new InvalidOperationException(
                "No supported " + (particle ? "particle" : "unlit")
                + " shader was found for planet placeholders.");
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 0.35f);
                material.EnableKeyword("_EMISSION");
            }
        }

        private static void ConfigureParticleTransparency(Material material)
        {
            bool isUniversalRenderPipeline = material.shader.name.StartsWith(
                "Universal Render Pipeline/",
                StringComparison.Ordinal);

            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_Mode"))
                material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat(
                    "_SrcBlend",
                    (float)(isUniversalRenderPipeline ? BlendMode.SrcAlpha : BlendMode.One));
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.DisableKeyword("_ALPHATEST_ON");
            if (isUniversalRenderPipeline)
            {
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static GameObject CreateOrUpdateEnvironmentPrefab(
            PlanetVisualRecipe recipe,
            Material backgroundMaterial,
            Material bodyMaterial,
            Material accentMaterial,
            Material glowMaterial)
        {
            string path = PrefabsFolder + "/" + recipe.MapId + "_Environment.prefab";
            var root = new GameObject(recipe.MapId + "_Environment");
            try
            {
                root.AddComponent<PlanetEnvironmentContent>();
                CreatePrimitive(
                    root.transform, PrimitiveType.Quad, "PlanetBackground",
                    new Vector3(0f, -1f, -2f), new Vector3(30f, 18f, 1f),
                    new Vector3(90f, 0f, 0f), backgroundMaterial);
                CreatePrimitive(
                    root.transform, PrimitiveType.Sphere, "PlanetBody", PlanetCenter,
                    new Vector3(recipe.BodyDiameter, 0.32f, recipe.BodyDiameter),
                    Vector3.zero, bodyMaterial);

                AddPlanetFeatures(root.transform, recipe, accentMaterial, glowMaterial);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save environment prefab: " + path);
                IReadOnlyList<string> validation =
                    PlanetContentValidator.ValidatePresentationPrefab(prefab, recipe.MapId + " environmentPrefab");
                if (validation.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validation));
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddPlanetFeatures(
            Transform root,
            PlanetVisualRecipe recipe,
            Material accent,
            Material glow)
        {
            switch (recipe.MapId)
            {
                case "NEPTUNE":
                    CreateBand(root, "StormBand_1", -0.38f, -11f, 2.7f, 0.16f, accent);
                    CreateBand(root, "StormBand_2", 0.34f, -11f, 2.45f, 0.12f, accent);
                    CreateRing(root, "AtmosphereSegment", 1.72f, 1.72f, 16, 0.11f, glow, 0f);
                    break;
                case "URANUS":
                    CreateBand(root, "IceBand_1", 0f, 78f, 2.25f, 0.14f, accent);
                    CreateRing(root, "PolarRingSegment", 1.72f, 0.58f, 16, 0.10f, glow, 78f);
                    break;
                case "SATURN":
                    CreateBand(root, "EquatorialBand_1", 0f, 0f, 2.5f, 0.14f, accent);
                    CreateRing(root, "RingSegment", 2.18f, 0.78f, 18, 0.16f, glow, 18f);
                    CreateRing(root, "InnerRingSegment", 1.82f, 0.62f, 16, 0.09f, accent, 18f);
                    break;
                case "JUPITER":
                    CreateBand(root, "Band_1", -0.66f, 0f, 3.15f, 0.18f, accent);
                    CreateBand(root, "Band_2", -0.20f, 0f, 3.45f, 0.12f, glow);
                    CreateBand(root, "Band_3", 0.32f, 0f, 3.25f, 0.16f, accent);
                    CreateBand(root, "Band_4", 0.76f, 0f, 2.75f, 0.10f, glow);
                    CreateSpot(root, "GreatRedSpot", new Vector3(0.72f, 0f, -0.53f),
                        new Vector3(0.58f, 0.05f, 0.34f), glow);
                    break;
                case "MARS":
                    CreateSpot(root, "Crater_1", new Vector3(-0.45f, 0f, -0.32f),
                        new Vector3(0.42f, 0.05f, 0.34f), accent);
                    CreateSpot(root, "Crater_2", new Vector3(0.48f, 0f, 0.34f),
                        new Vector3(0.30f, 0.05f, 0.24f), accent);
                    CreateSpot(root, "PolarCap", new Vector3(0f, 0f, 0.93f),
                        new Vector3(0.80f, 0.05f, 0.22f), glow);
                    break;
                case "EARTH":
                    CreateSpot(root, "Land_1", new Vector3(-0.48f, 0f, -0.18f),
                        new Vector3(0.76f, 0.05f, 0.52f), accent);
                    CreateSpot(root, "Land_2", new Vector3(0.58f, 0f, 0.38f),
                        new Vector3(0.62f, 0.05f, 0.44f), accent);
                    CreateBand(root, "CloudBand_1", 0.18f, -18f, 2.45f, 0.08f, glow);
                    CreateRing(root, "AtmosphereSegment", 1.62f, 1.62f, 16, 0.08f, glow, 0f);
                    break;
                case "VENUS":
                    CreateBand(root, "CloudBand_1", -0.52f, 16f, 2.35f, 0.16f, accent);
                    CreateBand(root, "CloudBand_2", 0f, 16f, 2.65f, 0.14f, glow);
                    CreateBand(root, "CloudBand_3", 0.52f, 16f, 2.30f, 0.16f, accent);
                    CreateRing(root, "AtmosphereSegment", 1.58f, 1.58f, 16, 0.10f, glow, 0f);
                    break;
                case "MERCURY":
                    CreateSpot(root, "Crater_1", new Vector3(-0.34f, 0f, -0.20f),
                        new Vector3(0.34f, 0.05f, 0.28f), accent);
                    CreateSpot(root, "Crater_2", new Vector3(0.31f, 0f, 0.36f),
                        new Vector3(0.26f, 0.05f, 0.22f), accent);
                    CreateSpot(root, "Crater_3", new Vector3(0.42f, 0f, -0.32f),
                        new Vector3(0.20f, 0.05f, 0.16f), accent);
                    break;
                case "SUN":
                    CreateRing(root, "CoronaSegment", 1.95f, 1.95f, 20, 0.16f, glow, 0f);
                    CreateRing(root, "OuterCoronaSegment", 2.17f, 2.17f, 20, 0.08f, accent, 0f);
                    CreateBand(root, "SolarBand_1", -0.28f, -12f, 2.65f, 0.12f, accent);
                    break;
                default:
                    throw new InvalidOperationException("No visual recipe implementation for " + recipe.MapId + ".");
            }
        }

        private static void CreateBand(
            Transform root, string name, float zOffset, float yaw,
            float width, float depth, Material material)
        {
            CreatePrimitive(
                root, PrimitiveType.Cube, name,
                PlanetCenter + new Vector3(0f, 0.20f, zOffset),
                new Vector3(width, 0.05f, depth), new Vector3(0f, yaw, 0f), material);
        }

        private static void CreateSpot(
            Transform root, string name, Vector3 offset, Vector3 scale, Material material)
        {
            CreatePrimitive(
                root, PrimitiveType.Sphere, name,
                PlanetCenter + offset + new Vector3(0f, 0.21f, 0f),
                scale, Vector3.zero, material);
        }

        private static void CreateRing(
            Transform root, string prefix, float radiusX, float radiusZ,
            int segmentCount, float thickness, Material material, float rotationDegrees)
        {
            float rotationRadians = rotationDegrees * Mathf.Deg2Rad;
            float segmentLength = Mathf.Max(0.22f,
                2f * Mathf.PI * Mathf.Max(radiusX, radiusZ) / segmentCount * 0.82f);
            for (int index = 0; index < segmentCount; index++)
            {
                float angle = 2f * Mathf.PI * index / segmentCount;
                float x = Mathf.Cos(angle) * radiusX;
                float z = Mathf.Sin(angle) * radiusZ;
                float rotatedX = x * Mathf.Cos(rotationRadians) - z * Mathf.Sin(rotationRadians);
                float rotatedZ = x * Mathf.Sin(rotationRadians) + z * Mathf.Cos(rotationRadians);
                float tangentYaw = -(angle * Mathf.Rad2Deg + rotationDegrees + 90f);
                CreatePrimitive(
                    root, PrimitiveType.Cube,
                    prefix + "_" + (index + 1).ToString("00"),
                    PlanetCenter + new Vector3(rotatedX, 0.16f, rotatedZ),
                    new Vector3(segmentLength, 0.045f, thickness),
                    new Vector3(0f, tangentYaw, 0f), material);
            }
        }

        private static GameObject CreatePrimitive(
            Transform parent, PrimitiveType primitiveType, string name,
            Vector3 position, Vector3 scale, Vector3 eulerAngles, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(primitiveType);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localRotation = Quaternion.Euler(eulerAngles);
            value.transform.localScale = scale;

            Collider collider = value.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return value;
        }

        private static GameObject CreateOrUpdateEffectPrefab(
            PlanetVisualRecipe recipe,
            Material particleMaterial)
        {
            string path = PrefabsFolder + "/" + recipe.MapId + "_AmbientEffect.prefab";
            var root = new GameObject(recipe.MapId + "_AmbientEffect");
            try
            {
                root.transform.localPosition = PlanetCenter + new Vector3(0f, 0.28f, 0f);
                root.AddComponent<PlanetEnvironmentContent>();
                ParticleSystem particles = root.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particles.main;
                main.loop = true;
                main.playOnAwake = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = recipe.MapId == "SUN" ? 2.2f : 5.5f;
                main.startSpeed = recipe.MapId == "SUN" ? 0.55f : 0.08f;
                main.startSize = recipe.MapId == "SUN" ? 0.16f : 0.10f;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    WithAlpha(recipe.GlowColor, 0.35f),
                    WithAlpha(Color.white, 0.75f));
                main.maxParticles = recipe.MapId == "SUN" ? 70 : 45;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = recipe.ParticleRate;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = recipe.BodyDiameter * 0.58f;
                shape.radiusThickness = 0.12f;

                ParticleSystemRenderer particleRenderer = root.GetComponent<ParticleSystemRenderer>();
                particleRenderer.sharedMaterial = particleMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
                particleRenderer.receiveShadows = false;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save effect prefab: " + path);
                IReadOnlyList<string> validation =
                    PlanetContentValidator.ValidatePresentationPrefab(prefab, recipe.MapId + " environmentEffect");
                if (validation.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validation));
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static PlanetContentProfile CreateOrUpdateProfile(
            PlanetVisualRecipe recipe,
            Material backgroundMaterial,
            GameObject environmentPrefab,
            GameObject effectPrefab)
        {
            string path = ProfilesFolder + "/" + recipe.MapId + ".asset";
            PlanetContentProfile profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
                profile.name = recipe.MapId;
                AssetDatabase.CreateAsset(profile, path);
            }

            Color ambient = Color.Lerp(recipe.BaseColor, Color.gray, 0.65f);
            profile.ConfigureForEditor(
                recipe.MapId, true, environmentPrefab, null,
                Color.Lerp(Color.black, recipe.BaseColor, 0.25f),
                backgroundMaterial,
                new PlanetLightingSettings(
                    ambient, 0.8f, Color.Lerp(recipe.BaseColor, Color.white, 0.65f),
                    1f, new Vector3(50f, -30f, 0f)),
                new[] { effectPrefab });
            profile.ConfigureBattlefieldForEditor(false, null, null);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ValidateGeneratedCatalog(PlanetContentCatalog catalog)
        {
            CanonicalBalanceLoadResult canonical = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                new ExistingMonsterPrefabRuntimeMapping());
            if (!canonical.IsValid || canonical.Bundle?.PlanetBattles == null)
                throw new InvalidOperationException(
                    "Canonical PlanetBattle data is invalid: " + string.Join(Environment.NewLine, canonical.Errors));

            IReadOnlyList<string> errors = PlanetContentValidator.ValidateCatalogAgainstCanonical(
                catalog, canonical.Bundle.PlanetBattles);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "Generated PlanetContentCatalog is invalid:" + Environment.NewLine
                    + " - " + string.Join(Environment.NewLine + " - ", errors));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Invalid AssetDatabase folder path: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct PlanetVisualRecipe
        {
            public PlanetVisualRecipe(
                string mapId, Color baseColor, Color accentColor, Color glowColor,
                float bodyDiameter, float particleRate)
            {
                MapId = mapId;
                BaseColor = baseColor;
                AccentColor = accentColor;
                GlowColor = glowColor;
                BodyDiameter = bodyDiameter;
                ParticleRate = particleRate;
            }

            public string MapId { get; }
            public Color BaseColor { get; }
            public Color AccentColor { get; }
            public Color GlowColor { get; }
            public float BodyDiameter { get; }
            public float ParticleRate { get; }
        }
    }
}
