using System;
using System.Collections.Generic;
using MyDefense.Battle.Balance.Canonical;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Applies presentation-only planet content to the common Battle scene.
    /// The authoritative map ID is resolved exactly; there is no fallback.
    /// </summary>
    public sealed class PlanetContentApplicator : MonoBehaviour
    {
        [SerializeField] private PlanetContentCatalog _catalog;
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private Camera _backgroundCamera;
        [SerializeField] private Light _directionalLight;

        private GameObject _activeEnvironment;
        private string _activeMapId;
        private PlanetContentProfile _activeProfile;
        private bool _globalStateCaptured;
        private Color _previousAmbientColor;
        private float _previousAmbientIntensity;
        private Camera _capturedCamera;
        private Color _previousCameraColor;
        private Light _capturedDirectionalLight;
        private Color _previousDirectionalColor;
        private float _previousDirectionalIntensity;
        private Quaternion _previousDirectionalRotation;

        public string ActiveMapId => _activeMapId;
        public PlanetContentProfile ActiveProfile => _activeProfile;
        public GameObject ActiveEnvironment => _activeEnvironment;
        public string LastError { get; private set; }

        public bool TryApply(
            string authoritativeMapId,
            CanonicalPlanetBattleRegistry canonicalPlanets,
            out string error)
        {
            PlanetContentCatalog catalog = _catalog != null
                ? _catalog
                : Resources.Load<PlanetContentCatalog>(PlanetContentCatalog.ResourcesPath);
            if (catalog == null)
            {
                error = "PlanetContentCatalog was not found at Resources/"
                    + PlanetContentCatalog.ResourcesPath + ".asset.";
                LastError = error;
                return false;
            }

            IReadOnlyList<string> catalogErrors =
                PlanetContentValidator.ValidateCatalogAgainstCanonical(catalog, canonicalPlanets);
            if (catalogErrors.Count > 0)
            {
                error = string.Join(Environment.NewLine + " - ", catalogErrors);
                LastError = error;
                return false;
            }

            if (!catalog.TryResolve(authoritativeMapId, out PlanetContentProfile profile, out error))
            {
                LastError = error;
                return false;
            }

            if (_activeEnvironment != null
                && ReferenceEquals(_activeProfile, profile)
                && string.Equals(_activeMapId, authoritativeMapId, StringComparison.Ordinal))
            {
                LastError = null;
                error = null;
                return true;
            }

            Transform parent = ResolveContentRoot();
            GameObject replacement = null;
            try
            {
                CaptureGlobalState();
                replacement = Instantiate(profile.EnvironmentPrefab, parent, false);
                replacement.name = "PlanetEnvironment_" + authoritativeMapId;
                InstantiateEffects(profile, replacement.transform);
                ApplyPresentation(profile, replacement);
            }
            catch (Exception exception)
            {
                DestroyOwnedObject(replacement);
                if (_activeProfile != null && _activeEnvironment != null)
                    ApplyPresentation(_activeProfile, _activeEnvironment);
                else
                    RestoreGlobalState();
                error = "Failed to instantiate PlanetContent for mapId '" + authoritativeMapId
                    + "': " + exception.Message;
                LastError = error;
                return false;
            }

            DestroyOwnedObject(_activeEnvironment);
            _activeEnvironment = replacement;
            _activeMapId = authoritativeMapId;
            _activeProfile = profile;
            LastError = null;
            error = null;
            return true;
        }

        public void Clear()
        {
            DestroyOwnedObject(_activeEnvironment);
            _activeEnvironment = null;
            _activeMapId = null;
            _activeProfile = null;
            LastError = null;
            RestoreGlobalState();
        }

        private Transform ResolveContentRoot()
        {
            if (_contentRoot != null)
                return _contentRoot;

            Transform existing = transform.Find("PlanetContentRuntime");
            if (existing != null)
            {
                _contentRoot = existing;
                return _contentRoot;
            }

            var root = new GameObject("PlanetContentRuntime");
            root.transform.SetParent(transform, false);
            _contentRoot = root.transform;
            return _contentRoot;
        }

        private static void InstantiateEffects(PlanetContentProfile profile, Transform parent)
        {
            GameObject[] effects = profile.EnvironmentEffects;
            for (int index = 0; index < effects.Length; index++)
            {
                GameObject effect = Instantiate(effects[index], parent, false);
                effect.name = effects[index].name + "_Runtime";
            }
        }

        private void ApplyPresentation(PlanetContentProfile profile, GameObject environment)
        {
            Camera targetCamera = _capturedCamera != null
                ? _capturedCamera
                : (_backgroundCamera != null ? _backgroundCamera : Camera.main);
            if (targetCamera != null)
                targetCamera.backgroundColor = profile.BackgroundColor;

            if (profile.BackgroundSprite != null)
            {
                Transform existingBackground = environment.transform.Find("PlanetBackgroundSprite");
                GameObject backgroundObject = existingBackground != null
                    ? existingBackground.gameObject
                    : new GameObject("PlanetBackgroundSprite");
                backgroundObject.transform.SetParent(environment.transform, false);
                backgroundObject.transform.localPosition = new Vector3(0f, 0f, 5f);
                SpriteRenderer spriteRenderer = backgroundObject.GetComponent<SpriteRenderer>()
                    ?? backgroundObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = profile.BackgroundSprite;
                spriteRenderer.sortingOrder = -1000;
            }

            PlanetLightingSettings lighting = profile.Lighting;
            RenderSettings.ambientLight = lighting.AmbientColor;
            RenderSettings.ambientIntensity = lighting.AmbientIntensity;

            Light targetLight = _capturedDirectionalLight != null
                ? _capturedDirectionalLight
                : (_directionalLight != null ? _directionalLight : FindDirectionalLight());
            if (targetLight != null)
            {
                targetLight.color = lighting.DirectionalColor;
                targetLight.intensity = lighting.DirectionalIntensity;
                targetLight.transform.rotation = Quaternion.Euler(lighting.DirectionalEulerAngles);
            }

            Material material = profile.EnvironmentMaterial;
            if (material == null)
                return;
            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] is not ParticleSystemRenderer)
                    renderers[index].sharedMaterial = material;
            }
        }

        private void CaptureGlobalState()
        {
            if (_globalStateCaptured)
                return;

            _previousAmbientColor = RenderSettings.ambientLight;
            _previousAmbientIntensity = RenderSettings.ambientIntensity;
            _capturedCamera = _backgroundCamera != null ? _backgroundCamera : Camera.main;
            if (_capturedCamera != null)
                _previousCameraColor = _capturedCamera.backgroundColor;
            _capturedDirectionalLight = _directionalLight != null
                ? _directionalLight
                : FindDirectionalLight();
            if (_capturedDirectionalLight != null)
            {
                _previousDirectionalColor = _capturedDirectionalLight.color;
                _previousDirectionalIntensity = _capturedDirectionalLight.intensity;
                _previousDirectionalRotation = _capturedDirectionalLight.transform.rotation;
            }
            _globalStateCaptured = true;
        }

        private void RestoreGlobalState()
        {
            if (!_globalStateCaptured)
                return;

            RenderSettings.ambientLight = _previousAmbientColor;
            RenderSettings.ambientIntensity = _previousAmbientIntensity;
            if (_capturedCamera != null)
                _capturedCamera.backgroundColor = _previousCameraColor;
            if (_capturedDirectionalLight != null)
            {
                _capturedDirectionalLight.color = _previousDirectionalColor;
                _capturedDirectionalLight.intensity = _previousDirectionalIntensity;
                _capturedDirectionalLight.transform.rotation = _previousDirectionalRotation;
            }
            _capturedCamera = null;
            _capturedDirectionalLight = null;
            _globalStateCaptured = false;
        }

        private static Light FindDirectionalLight()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < lights.Length; index++)
            {
                if (lights[index].type == LightType.Directional)
                    return lights[index];
            }
            return null;
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                Destroy(value);
            else
                DestroyImmediate(value);
        }

        private void OnDestroy()
        {
            Clear();
        }

#if UNITY_EDITOR
        public void ConfigureForTests(
            PlanetContentCatalog catalog,
            Transform contentRoot = null,
            Camera backgroundCamera = null,
            Light directionalLight = null)
        {
            _catalog = catalog;
            _contentRoot = contentRoot;
            _backgroundCamera = backgroundCamera;
            _directionalLight = directionalLight;
        }
#endif
    }
}
