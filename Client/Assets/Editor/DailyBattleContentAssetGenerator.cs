using System;
using System.Collections.Generic;
using System.IO;
using MyDefense.Battle.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyDefense.Battle.Editor
{
    /// <summary>
    /// Generates Development-only placeholder presentation Assets through Unity's
    /// serialization APIs. Final art replaces Profile references without changing
    /// Battle runtime or Daily Session contracts.
    /// </summary>
    public static class DailyBattleContentAssetGenerator
    {
        private const string ResourcesFolder = "Assets/Resources/Battle/DailyContent";
        private const string ProfilesFolder = ResourcesFolder + "/Profiles";
        private const string PrefabsFolder = "Assets/Prefabs/Battle/DailyContent";
        private const string MaterialsFolder = "Assets/Materials/Battle/DailyContent";
        private const string CatalogPath = ResourcesFolder + "/DailyBattleContentCatalog.asset";

        private static readonly DailyVisualRecipe[] Recipes =
        {
            new(
                DailyBattleExecutionPlanBuilder.CultivationMapId,
                new Color(0.08f, 0.28f, 0.13f),
                new Color(0.38f, 0.94f, 0.32f),
                new Color(0.96f, 0.74f, 0.22f),
                new Color(0.015f, 0.055f, 0.025f)),
            new(
                DailyBattleExecutionPlanBuilder.MutationLabMapId,
                new Color(0.23f, 0.055f, 0.34f),
                new Color(0.32f, 0.94f, 1f),
                new Color(0.94f, 0.22f, 0.86f),
                new Color(0.045f, 0.015f, 0.075f))
        };

        [MenuItem("MyDefense/Battle/Generate Daily Battle Placeholder Content")]
        public static void GeneratePlaceholderContent()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ProfilesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);

            if (Recipes.Length != DailyBattleContentCatalog.RequiredMapIds.Count)
                throw new InvalidOperationException("Daily visual recipes must match the two required map IDs.");

            var profiles = new List<PlanetContentProfile>(Recipes.Length);
            for (int index = 0; index < Recipes.Length; index++)
            {
                DailyVisualRecipe recipe = Recipes[index];
                if (!string.Equals(
                        recipe.MapId,
                        DailyBattleContentCatalog.RequiredMapIds[index],
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("Daily visual recipe order drifted from required map IDs.");

                Material baseMaterial = CreateOrUpdateMaterial(recipe.MapId, "Base", recipe.BaseColor);
                Material accentMaterial = CreateOrUpdateMaterial(recipe.MapId, "Accent", recipe.AccentColor);
                Material effectMaterial = CreateOrUpdateMaterial(recipe.MapId, "Effect", recipe.EffectColor);
                GameObject environment = CreateOrUpdateEnvironment(
                    recipe, baseMaterial, accentMaterial, effectMaterial);
                GameObject effect = CreateOrUpdateEffect(recipe, effectMaterial);
                profiles.Add(CreateOrUpdateProfile(recipe, environment, effect, baseMaterial));
            }

            DailyBattleContentCatalog catalog =
                AssetDatabase.LoadAssetAtPath<DailyBattleContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DailyBattleContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.ConfigureForEditor(profiles);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> errors = DailyBattleContentValidator.ValidateCatalog(catalog);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "Generated DailyBattleContentCatalog is invalid:" + Environment.NewLine
                    + " - " + string.Join(Environment.NewLine + " - ", errors));
            Debug.Log("[DailyBattleContent] Generated two isolated placeholder Profiles. Catalog=" + CatalogPath);
        }

        private static Material CreateOrUpdateMaterial(string mapId, string suffix, Color color)
        {
            string path = MaterialsFolder + "/" + mapId + "_" + suffix + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException("No supported unlit Shader is available for Daily placeholders.");
            if (material == null)
            {
                material = new Material(shader) { name = mapId + "_" + suffix };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 1.6f);
                material.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateEnvironment(
            DailyVisualRecipe recipe,
            Material baseMaterial,
            Material accentMaterial,
            Material effectMaterial)
        {
            var root = new GameObject(recipe.MapId + "_Environment");
            root.AddComponent<PlanetEnvironmentContent>();
            try
            {
                CreatePrimitive(
                    PrimitiveType.Quad,
                    "PlanetBackground",
                    root.transform,
                    new Vector3(0f, -1f, -2f),
                    new Vector3(30f, 18f, 1f),
                    new Vector3(90f, 0f, 0f),
                    baseMaterial);

                if (string.Equals(
                        recipe.MapId,
                        DailyBattleExecutionPlanBuilder.CultivationMapId,
                        StringComparison.Ordinal))
                    AddCultivationSilhouette(root.transform, accentMaterial, effectMaterial);
                else
                    AddMutationLabSilhouette(root.transform, accentMaterial, effectMaterial);

                string path = PrefabsFolder + "/" + recipe.MapId + "_Environment.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save Daily environment prefab: " + path);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddCultivationSilhouette(
            Transform root,
            Material mint,
            Material growth)
        {
            Vector3 capsuleCenter = new(-9.8f, 0.15f, 2.7f);
            CreatePrimitive(
                PrimitiveType.Sphere, "CultivationCapsuleCore", root, capsuleCenter,
                new Vector3(4.4f, 0.16f, 1.65f), Vector3.zero, mint);
            CreatePrimitive(
                PrimitiveType.Cube, "CultivationCapsuleFluid", root,
                capsuleCenter + new Vector3(0f, 0.04f, -0.18f),
                new Vector3(3.55f, 0.08f, 0.52f), Vector3.zero, growth);
            CreateRing(
                root, "CultivationCapsuleShell", capsuleCenter + new Vector3(0f, 0.08f, 0f),
                2.45f, 1.18f, 22, 0.17f, growth, 0f);

            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI * 2f / 6f;
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "CultivationCell_" + (index + 1).ToString("00"),
                    root,
                    capsuleCenter + new Vector3(
                        Mathf.Cos(angle) * 1.45f,
                        0.10f,
                        Mathf.Sin(angle) * 0.46f),
                    new Vector3(0.42f, 0.10f, 0.42f),
                    Vector3.zero,
                    index % 2 == 0 ? growth : mint);
            }

            Vector3 ringCenter = new(9.8f, 0.14f, 3.65f);
            CreateRing(root, "CultivationGrowthRingOuter", ringCenter, 1.95f, 1.35f, 20, 0.19f, mint, 0f);
            CreateRing(root, "CultivationGrowthRingInner", ringCenter, 1.28f, 0.82f, 16, 0.12f, growth, 12f);
            CreatePrimitive(
                PrimitiveType.Sphere, "CultivationGrowthSeed", root, ringCenter,
                new Vector3(0.78f, 0.13f, 0.78f), Vector3.zero, growth);

            Vector3[] organicNodes =
            {
                new(11.9f, 0.12f, 2.2f), new(11.1f, 0.12f, 1.1f),
                new(11.8f, 0.12f, -0.1f), new(10.9f, 0.12f, -1.35f),
                new(11.6f, 0.12f, -2.55f), new(10.8f, 0.12f, -3.8f)
            };
            for (int index = 0; index < organicNodes.Length; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "CultivationOrganicNode_" + (index + 1).ToString("00"),
                    root,
                    organicNodes[index],
                    new Vector3(0.62f + index * 0.06f, 0.10f, 0.42f),
                    Vector3.zero,
                    index % 2 == 0 ? mint : growth);
                if (index > 0)
                {
                    CreateBeam(
                        root,
                        "CultivationOrganicCurve_" + index.ToString("00"),
                        organicNodes[index - 1],
                        organicNodes[index],
                        0.22f,
                        mint);
                }
            }
        }

        private static void AddMutationLabSilhouette(
            Transform root,
            Material cyan,
            Material magenta)
        {
            CreatePrimitive(
                PrimitiveType.Cube, "MutationWarningFrame_Top", root,
                new Vector3(0f, 0.10f, 5.25f), new Vector3(24.8f, 0.08f, 0.22f),
                Vector3.zero, magenta);
            CreatePrimitive(
                PrimitiveType.Cube, "MutationWarningFrame_Left", root,
                new Vector3(-12.45f, 0.10f, -1.55f), new Vector3(0.22f, 0.08f, 13.4f),
                Vector3.zero, magenta);
            CreatePrimitive(
                PrimitiveType.Cube, "MutationWarningFrame_Right", root,
                new Vector3(12.45f, 0.10f, -1.55f), new Vector3(0.22f, 0.08f, 13.4f),
                Vector3.zero, cyan);

            CreateRing(
                root, "MutationCoilLeft", new Vector3(-10.3f, 0.15f, 2.35f),
                1.45f, 2.15f, 24, 0.18f, cyan, 0f);
            CreateRing(
                root, "MutationCoilRight", new Vector3(10.35f, 0.15f, 0.25f),
                1.45f, 2.15f, 24, 0.18f, magenta, 18f);

            for (int index = 0; index < 5; index++)
            {
                float x = 6.9f + index * 1.18f;
                float z = 4.2f + (index % 2 == 0 ? 0.22f : -0.18f);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "MutationCrystal_" + (index + 1).ToString("00"),
                    root,
                    new Vector3(x, 0.17f, z),
                    new Vector3(0.58f, 0.12f, 1.65f + index * 0.12f),
                    new Vector3(0f, 32f + index * 13f, 0f),
                    index % 2 == 0 ? cyan : magenta);
            }

            Vector3[] arcPoints =
            {
                new(-11.6f, 0.22f, 0.2f), new(-10.5f, 0.22f, -0.45f),
                new(-11.45f, 0.22f, -1.2f), new(-10.35f, 0.22f, -2.0f),
                new(-11.35f, 0.22f, -2.8f), new(-10.2f, 0.22f, -3.65f)
            };
            for (int index = 1; index < arcPoints.Length; index++)
            {
                CreateBeam(
                    root,
                    "MutationElectricArc_" + index.ToString("00"),
                    arcPoints[index - 1],
                    arcPoints[index],
                    0.20f,
                    index % 2 == 0 ? cyan : magenta);
            }

            for (int index = 0; index < 4; index++)
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    "MutationWarningChevron_" + (index + 1).ToString("00"),
                    root,
                    new Vector3(-3.6f + index * 2.4f, 0.13f, 4.92f),
                    new Vector3(1.25f, 0.09f, 0.32f),
                    new Vector3(0f, index % 2 == 0 ? 28f : -28f, 0f),
                    index % 2 == 0 ? magenta : cyan);
            }
        }

        private static void CreateRing(
            Transform root,
            string prefix,
            Vector3 center,
            float radiusX,
            float radiusZ,
            int segmentCount,
            float thickness,
            Material material,
            float phaseDegrees)
        {
            float phase = phaseDegrees * Mathf.Deg2Rad;
            float segmentLength = Mathf.Max(
                0.25f,
                2f * Mathf.PI * Mathf.Max(radiusX, radiusZ) / segmentCount * 0.82f);
            for (int index = 0; index < segmentCount; index++)
            {
                float angle = 2f * Mathf.PI * index / segmentCount + phase;
                CreatePrimitive(
                    PrimitiveType.Cube,
                    prefix + "_" + (index + 1).ToString("00"),
                    root,
                    center + new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ),
                    new Vector3(segmentLength, 0.07f, thickness),
                    new Vector3(0f, -(angle * Mathf.Rad2Deg + 90f), 0f),
                    material);
            }
        }

        private static void CreateBeam(
            Transform root,
            string name,
            Vector3 start,
            Vector3 end,
            float thickness,
            Material material)
        {
            Vector3 delta = end - start;
            float length = new Vector2(delta.x, delta.z).magnitude;
            float yaw = -Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
            CreatePrimitive(
                PrimitiveType.Cube,
                name,
                root,
                (start + end) * 0.5f,
                new Vector3(length, 0.07f, thickness),
                new Vector3(0f, yaw, 0f),
                material);
        }

        private static GameObject CreateOrUpdateEffect(DailyVisualRecipe recipe, Material material)
        {
            var root = new GameObject(recipe.MapId + "_Effect");
            root.AddComponent<PlanetEnvironmentContent>();
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            try
            {
                ParticleSystem.MainModule main = particles.main;
                main.loop = true;
                main.startLifetime = 4f;
                main.startSpeed = 0.35f;
                main.startSize = 0.16f;
                main.startColor = recipe.EffectColor;
                main.maxParticles = 60;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = 4f;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(14f, 6f, 0.5f);
                particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

                string path = PrefabsFolder + "/" + recipe.MapId + "_Effect.prefab";
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static PlanetContentProfile CreateOrUpdateProfile(
            DailyVisualRecipe recipe,
            GameObject environment,
            GameObject effect,
            Material environmentMaterial)
        {
            string path = ProfilesFolder + "/" + recipe.MapId + ".asset";
            PlanetContentProfile profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
                profile.name = recipe.MapId;
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.ConfigureForEditor(
                recipe.MapId,
                true,
                environment,
                null,
                recipe.BackgroundColor,
                environmentMaterial,
                new PlanetLightingSettings(
                    Color.Lerp(recipe.BaseColor, Color.gray, 0.65f),
                    0.8f,
                    Color.Lerp(recipe.AccentColor, Color.white, 0.55f),
                    1f,
                    new Vector3(50f, -30f, 0f)),
                new[] { effect });
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Vector3 eulerAngles,
            Material material)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            value.transform.localRotation = Quaternion.Euler(eulerAngles);
            Collider collider = value.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Invalid AssetDatabase folder path: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct DailyVisualRecipe
        {
            public DailyVisualRecipe(
                string mapId,
                Color baseColor,
                Color accentColor,
                Color effectColor,
                Color backgroundColor)
            {
                MapId = mapId;
                BaseColor = baseColor;
                AccentColor = accentColor;
                EffectColor = effectColor;
                BackgroundColor = backgroundColor;
            }

            public string MapId { get; }
            public Color BaseColor { get; }
            public Color AccentColor { get; }
            public Color EffectColor { get; }
            public Color BackgroundColor { get; }
        }
    }
}
