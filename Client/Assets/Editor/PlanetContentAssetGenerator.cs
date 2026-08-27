using System;
using System.Collections.Generic;
using System.IO;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using UnityEditor;
using UnityEngine;

namespace MyDefense.Battle.Editor
{
    public static class PlanetContentAssetGenerator
    {
        private const string ResourcesFolder = "Assets/Resources/Battle/PlanetContent";
        private const string ProfilesFolder = ResourcesFolder + "/Profiles";
        private const string PrefabsFolder = "Assets/Prefabs/Battle/PlanetContent";
        private const string MaterialsFolder = "Assets/Materials/Battle/PlanetContent";
        private const string CatalogPath = ResourcesFolder + "/PlanetContentCatalog.asset";

        private static readonly Color[] PlanetColors =
        {
            new(0.08f, 0.28f, 0.55f, 1f),
            new(0.25f, 0.75f, 0.82f, 1f),
            new(0.78f, 0.64f, 0.34f, 1f),
            new(0.72f, 0.46f, 0.28f, 1f),
            new(0.72f, 0.20f, 0.12f, 1f),
            new(0.18f, 0.50f, 0.30f, 1f),
            new(0.82f, 0.55f, 0.22f, 1f),
            new(0.55f, 0.50f, 0.46f, 1f),
            new(1.00f, 0.48f, 0.05f, 1f)
        };

        [MenuItem("MyDefense/Battle/Generate P2 Planet Placeholder Content")]
        public static void GeneratePlaceholderContent()
        {
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ProfilesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);

            var profiles = new List<PlanetContentProfile>();
            for (int index = 0; index < PlanetContentCatalog.CanonicalMapIds.Count; index++)
            {
                string mapId = PlanetContentCatalog.CanonicalMapIds[index];
                Color color = PlanetColors[index];
                Material material = CreateOrUpdateMaterial(mapId, color);
                GameObject environmentPrefab = CreateOrUpdateEnvironmentPrefab(mapId, material, color);
                GameObject effectPrefab = CreateOrUpdateEffectPrefab(mapId, color);
                PlanetContentProfile profile = CreateOrUpdateProfile(
                    mapId,
                    color,
                    material,
                    environmentPrefab,
                    effectPrefab);
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
                "[PlanetContent] Generated nine placeholder profiles, environment prefabs and effects. "
                + "Catalog=" + CatalogPath);
        }

        private static Material CreateOrUpdateMaterial(string mapId, Color color)
        {
            string path = MaterialsFolder + "/" + mapId + "_Environment.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException("No supported unlit shader was found for planet placeholders.");

            if (material == null)
            {
                material = new Material(shader) { name = mapId + "_Environment" };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateEnvironmentPrefab(
            string mapId,
            Material material,
            Color color)
        {
            string path = PrefabsFolder + "/" + mapId + "_Environment.prefab";
            var root = new GameObject(mapId + "_Environment");
            try
            {
                root.AddComponent<PlanetEnvironmentContent>();
                GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
                background.name = "PlaceholderBackground";
                background.transform.SetParent(root.transform, false);
                background.transform.localPosition = new Vector3(0f, 0f, 8f);
                background.transform.localScale = new Vector3(28f, 16f, 1f);
                Collider collider = background.GetComponent<Collider>();
                if (collider != null)
                    UnityEngine.Object.DestroyImmediate(collider);
                background.GetComponent<MeshRenderer>().sharedMaterial = material;

                for (int ornamentIndex = 0; ornamentIndex < 3; ornamentIndex++)
                {
                    GameObject ornament = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    ornament.name = "PlaceholderOrnament_" + (ornamentIndex + 1);
                    ornament.transform.SetParent(root.transform, false);
                    ornament.transform.localPosition = new Vector3(
                        -8f + ornamentIndex * 8f,
                        4.8f - ornamentIndex * 0.3f,
                        7.5f);
                    ornament.transform.localScale = new Vector3(2.5f, 0.35f, 1f);
                    Collider ornamentCollider = ornament.GetComponent<Collider>();
                    if (ornamentCollider != null)
                        UnityEngine.Object.DestroyImmediate(ornamentCollider);
                    ornament.GetComponent<MeshRenderer>().sharedMaterial = material;
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save environment prefab: " + path);
                IReadOnlyList<string> validation =
                    PlanetContentValidator.ValidatePresentationPrefab(prefab, mapId + " environmentPrefab");
                if (validation.Count > 0)
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validation));
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateOrUpdateEffectPrefab(string mapId, Color color)
        {
            string path = PrefabsFolder + "/" + mapId + "_AmbientEffect.prefab";
            var root = new GameObject(mapId + "_AmbientEffect");
            try
            {
                root.AddComponent<PlanetEnvironmentContent>();
                ParticleSystem particles = root.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particles.main;
                main.loop = true;
                main.playOnAwake = true;
                main.startLifetime = 8f;
                main.startSpeed = 0.15f;
                main.startSize = 0.12f;
                main.startColor = new ParticleSystem.MinMaxGradient(color * 0.8f, Color.white);
                main.maxParticles = 80;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = 4f;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(18f, 9f, 1f);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null)
                    throw new InvalidOperationException("Failed to save effect prefab: " + path);
                IReadOnlyList<string> validation =
                    PlanetContentValidator.ValidatePresentationPrefab(prefab, mapId + " environmentEffect");
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
            string mapId,
            Color color,
            Material material,
            GameObject environmentPrefab,
            GameObject effectPrefab)
        {
            string path = ProfilesFolder + "/" + mapId + ".asset";
            PlanetContentProfile profile = AssetDatabase.LoadAssetAtPath<PlanetContentProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlanetContentProfile>();
                profile.name = mapId;
                AssetDatabase.CreateAsset(profile, path);
            }

            Color ambient = Color.Lerp(color, Color.gray, 0.65f);
            profile.ConfigureForEditor(
                mapId,
                true,
                environmentPrefab,
                null,
                color * 0.25f,
                material,
                new PlanetLightingSettings(
                    ambient,
                    0.8f,
                    Color.Lerp(color, Color.white, 0.65f),
                    1f,
                    new Vector3(50f, -30f, 0f)),
                new[] { effectPrefab });
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
                catalog,
                canonical.Bundle.PlanetBattles);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "Generated PlanetContentCatalog is invalid:" + Environment.NewLine
                    + " - " + string.Join(Environment.NewLine + " - ", errors));
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
    }
}
