using System;
using System.Collections.Generic;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Presentation;
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
        [SerializeField] private MeshRenderer _legacyBattleGround;

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
        private MeshRenderer _hiddenLegacyGround;
        private bool _previousLegacyGroundEnabled;
        private PlanetBattlefieldView _activeBattlefield;
        private PlanetBattlefieldLayout _battlefieldLayout;

        public string ActiveMapId => _activeMapId;
        public PlanetContentProfile ActiveProfile => _activeProfile;
        public GameObject ActiveEnvironment => _activeEnvironment;
        public string LastError { get; private set; }
        public PlanetBattlefieldView ActiveBattlefield => _activeBattlefield;
        public void SetBattlefieldLayout(PlanetBattlefieldLayout layout) => _battlefieldLayout = layout;

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

            return TryApplyResolvedProfile(authoritativeMapId, profile, out error);
        }

        /// <summary>
        /// Applies an already fail-fast-resolved presentation Profile. Daily content
        /// uses this entry point so its two-map Catalog remains isolated from the
        /// canonical nine-planet Catalog.
        /// </summary>
        public bool TryApplyResolvedProfile(
            string authoritativeMapId,
            PlanetContentProfile profile,
            out string error)
        {
            if (profile == null)
            {
                error = "Resolved PlanetContentProfile is required.";
                LastError = error;
                return false;
            }
            if (!profile.Enabled)
            {
                error = "Resolved PlanetContentProfile is disabled: '" + profile.MapId + "'.";
                LastError = error;
                return false;
            }
            if (!profile.ValidateBattlefieldTheme(out error)) { LastError = error; return false; }
            if (string.IsNullOrWhiteSpace(authoritativeMapId)
                || !string.Equals(authoritativeMapId, profile.MapId, StringComparison.Ordinal))
            {
                error = "Resolved PlanetContentProfile mapId does not match the authoritative mapId.";
                LastError = error;
                return false;
            }
            IReadOnlyList<string> presentationErrors =
                PlanetContentValidator.ValidatePresentationPrefab(profile.EnvironmentPrefab, profile.MapId + " environmentPrefab");
            for (int index = 0; index < profile.EnvironmentEffects.Length; index++)
            {
                IReadOnlyList<string> effectErrors = PlanetContentValidator.ValidatePresentationPrefab(
                    profile.EnvironmentEffects[index],
                    profile.MapId + " environmentEffect[" + index + "]");
                if (effectErrors.Count > 0)
                {
                    var combined = new List<string>(presentationErrors);
                    combined.AddRange(effectErrors);
                    presentationErrors = combined;
                }
            }
            if (presentationErrors.Count > 0)
            {
                error = string.Join(Environment.NewLine + " - ", presentationErrors);
                LastError = error;
                return false;
            }

            if (_activeEnvironment != null
                && ReferenceEquals(_activeProfile, profile)
                && string.Equals(_activeMapId, authoritativeMapId, StringComparison.Ordinal))
            {
                SetLegacyGroundHidden((profile.RequiresBattlefieldTheme || profile.BackgroundSprite != null)
                    && (_capturedCamera != null || _backgroundCamera != null || Camera.main != null));
                LastError = null;
                error = null;
                return true;
            }

            Transform parent = ResolveContentRoot();
            GameObject replacement = null;
            PlanetBattlefieldView battlefield = null;
            try
            {
                CaptureGlobalState();
                replacement = Instantiate(profile.EnvironmentPrefab, parent, false);
                replacement.name = "PlanetEnvironment_" + authoritativeMapId;
                InstantiateEffects(profile, replacement.transform);
                if (profile.RequiresBattlefieldTheme)
                    battlefield = new PlanetBattlefieldView(replacement.transform, profile,
                        _battlefieldLayout ?? PlanetBattlefieldLayout.FromScene(gameObject.scene));
                ApplyPresentation(profile, replacement);
            }
            catch (Exception exception)
            {
                battlefield?.Dispose();
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

            _activeBattlefield?.Dispose();
            _activeBattlefield = battlefield;
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
            _activeBattlefield?.Dispose(); _activeBattlefield = null;
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

            if (!profile.RequiresBattlefieldTheme && profile.BackgroundSprite != null)
            {
                Transform existingBackground = environment.transform.Find("PlanetBackgroundSprite");
                GameObject backgroundObject = existingBackground != null
                    ? existingBackground.gameObject
                    : new GameObject("PlanetBackgroundSprite");
                backgroundObject.transform.SetParent(environment.transform, false);
                SpriteRenderer spriteRenderer = backgroundObject.GetComponent<SpriteRenderer>();
                // Unity's Editor may return a missing-component fake null.
                // Use Unity equality rather than the C# null-coalescing operator.
                if (spriteRenderer == null) spriteRenderer = backgroundObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = profile.BackgroundSprite;
                spriteRenderer.sortingOrder = -1000;
                FitBackdrop(targetCamera, spriteRenderer);
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

            SetLegacyGroundHidden((profile.RequiresBattlefieldTheme || profile.BackgroundSprite != null) && targetCamera != null);

            Material material = profile.EnvironmentMaterial;
            if (material == null)
                return;
            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer is ParticleSystemRenderer)
                    continue;

                bool isBackground = string.Equals(
                    renderer.gameObject.name,
                    "PlanetBackground",
                    StringComparison.Ordinal);
                if (isBackground || renderer.sharedMaterial == null)
                    renderer.sharedMaterial = material;
            }
        }

        private MeshRenderer ResolveLegacyGround()
        {
            if (_legacyBattleGround != null)
                return _legacyBattleGround.gameObject.scene == gameObject.scene ? _legacyBattleGround : null;
            // This exact root is the original opaque 30x30 laboratory plane in
            // Battle.unity. Do not search children, other scenes, or substrings.
            if (!string.Equals(gameObject.scene.path, "Assets/Scenes/Battle.unity", StringComparison.Ordinal)) return null;
            MeshRenderer found = null;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (!string.Equals(root.name, "Lab_Ground", StringComparison.Ordinal)
                    || !root.TryGetComponent(out MeshRenderer renderer)
                    || root.GetComponent<MeshFilter>() == null || root.GetComponent<MeshCollider>() == null) continue;
                if (found != null)
                {
                    Debug.LogWarning("[PlanetContent] Ambiguous exact Battle Lab_Ground roots; no legacy ground renderer was selected.");
                    return null;
                }
                found = renderer;
            }
            return found;
        }

        private void SetLegacyGroundHidden(bool hidden)
        {
            MeshRenderer target = hidden ? ResolveLegacyGround() : null;
            if (target != null && _hiddenLegacyGround == target)
            { target.enabled = false; return; }
            RestoreLegacyGround();
            if (target == null) return;
            _hiddenLegacyGround = target;
            _previousLegacyGroundEnabled = target.enabled;
            target.enabled = false;
        }

        private void RestoreLegacyGround()
        {
            if (_hiddenLegacyGround != null) _hiddenLegacyGround.enabled = _previousLegacyGroundEnabled;
            _hiddenLegacyGround = null;
        }

        private void LateUpdate()
        {
            if (_activeEnvironment == null || _activeProfile == null || _activeProfile.RequiresBattlefieldTheme || _activeProfile.BackgroundSprite == null) return;
            Transform backdrop = _activeEnvironment.transform.Find("PlanetBackgroundSprite");
            if (backdrop != null)
                FitBackdrop(_capturedCamera != null ? _capturedCamera : Camera.main, backdrop.GetComponent<SpriteRenderer>());
        }

        public static void FitBackdrop(Camera camera, SpriteRenderer renderer)
        {
            if (camera == null || renderer == null || renderer.sprite == null) return;
            float distance = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, .90f);
            float halfHeight = camera.orthographic ? camera.orthographicSize
                : distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            Bounds artwork = renderer.sprite.bounds;
            Vector3 size = artwork.size;
            float scale = Mathf.Max(halfHeight * 2f / Mathf.Max(.001f, size.y),
                halfHeight * 2f * camera.aspect / Mathf.Max(.001f, size.x)) * 1.01f;
            float excessWidth = Mathf.Max(0f, size.x * scale - halfHeight * 2f * camera.aspect);
            float excessHeight = Mathf.Max(0f, size.y * scale - halfHeight * 2f);
            // The painted planets occupy the upper-right of each source image.
            // Keep cover sizing, but crop excess from the left/bottom rather
            // than losing that focal subject on a portrait or very wide screen.
            Vector3 position = camera.transform.position + camera.transform.forward * distance
                - camera.transform.right * (excessWidth * .5f)
                - camera.transform.up * (excessHeight * .5f)
                - camera.transform.rotation * (artwork.center * scale);
            renderer.transform.SetPositionAndRotation(position, camera.transform.rotation);
            // Environment roots are presentation-only and authored at scale 1.
            // Compensate a parent scale as well so framing stays camera-owned.
            Vector3 parentScale = renderer.transform.parent != null ? renderer.transform.parent.lossyScale : Vector3.one;
            renderer.transform.localScale = new Vector3(scale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                scale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f);
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
            RestoreLegacyGround();
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

        private void OnDisable() => Clear();

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
