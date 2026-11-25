using System;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    [Serializable]
    public struct PlanetLightingSettings
    {
        [SerializeField] private Color _ambientColor;
        [SerializeField, Min(0f)] private float _ambientIntensity;
        [SerializeField] private Color _directionalColor;
        [SerializeField, Min(0f)] private float _directionalIntensity;
        [SerializeField] private Vector3 _directionalEulerAngles;

        public Color AmbientColor => _ambientColor;
        public float AmbientIntensity => _ambientIntensity;
        public Color DirectionalColor => _directionalColor;
        public float DirectionalIntensity => _directionalIntensity;
        public Vector3 DirectionalEulerAngles => _directionalEulerAngles;

        public PlanetLightingSettings(
            Color ambientColor,
            float ambientIntensity,
            Color directionalColor,
            float directionalIntensity,
            Vector3 directionalEulerAngles)
        {
            _ambientColor = ambientColor;
            _ambientIntensity = Mathf.Max(0f, ambientIntensity);
            _directionalColor = directionalColor;
            _directionalIntensity = Mathf.Max(0f, directionalIntensity);
            _directionalEulerAngles = directionalEulerAngles;
        }
    }

    [CreateAssetMenu(
        fileName = "PlanetContentProfile",
        menuName = "MyDefense/Battle/Planet Content Profile")]
    public sealed class PlanetContentProfile : ScriptableObject
    {
        [SerializeField] private string _mapId;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private GameObject _environmentPrefab;
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Color _backgroundColor = Color.black;
        [SerializeField] private Material _environmentMaterial;
        [SerializeField] private PlanetLightingSettings _lighting = new(
            Color.gray,
            1f,
            Color.white,
            1f,
            new Vector3(50f, -30f, 0f));
        [SerializeField] private GameObject[] _environmentEffects = Array.Empty<GameObject>();
        [SerializeField] private bool _requiresBattlefieldTheme;
        [SerializeField] private Texture2D _battlefieldTerrain;
        [SerializeField] private Texture2D _battlefieldSurfaces;
        [SerializeField] private Vector2 _battlefieldAtlasSplit = new(.5f, .5f);

        public string MapId => _mapId;
        public bool Enabled => _enabled;
        public GameObject EnvironmentPrefab => _environmentPrefab;
        public Sprite BackgroundSprite => _backgroundSprite;
        public Color BackgroundColor => _backgroundColor;
        public Material EnvironmentMaterial => _environmentMaterial;
        public PlanetLightingSettings Lighting => _lighting;
        public GameObject[] EnvironmentEffects => _environmentEffects ?? Array.Empty<GameObject>();
        public bool RequiresBattlefieldTheme => _requiresBattlefieldTheme;
        public Texture2D BattlefieldTerrain => _battlefieldTerrain;
        public Texture2D BattlefieldSurfaces => _battlefieldSurfaces;
        public Vector2 BattlefieldAtlasSplit => _battlefieldAtlasSplit;

        public bool ValidateBattlefieldTheme(out string error)
        {
            error = null;
            if (!_requiresBattlefieldTheme) return true;
            if (_battlefieldTerrain == null || _battlefieldSurfaces == null || _environmentMaterial == null)
                error = MapId + " battlefield requires Terrain, Surfaces and a presentation Material; no SF/background fallback is allowed.";
            else if (_battlefieldTerrain.height <= _battlefieldTerrain.width)
                error = MapId + " battlefield Terrain must be portrait ground artwork.";
            else if (_battlefieldSurfaces.width != _battlefieldSurfaces.height || _battlefieldSurfaces.width % 2 != 0)
                error = MapId + " battlefield Surfaces must be a square, evenly divisible 2x2 atlas.";
            else if (_battlefieldAtlasSplit.x < .1f || _battlefieldAtlasSplit.x > .9f
                || _battlefieldAtlasSplit.y < .1f || _battlefieldAtlasSplit.y > .9f)
                error = MapId + " battlefield atlas split must preserve four usable cells.";
            return error == null;
        }

#if UNITY_EDITOR
        public void ConfigureBattlefieldForEditor(bool required, Texture2D terrain, Texture2D surfaces, Vector2? split = null)
        {
            _requiresBattlefieldTheme = required; _battlefieldTerrain = terrain; _battlefieldSurfaces = surfaces;
            _battlefieldAtlasSplit = split ?? new Vector2(.5f, .5f);
        }

        public void ConfigureForEditor(
            string mapId,
            bool enabled,
            GameObject environmentPrefab,
            Sprite backgroundSprite,
            Color backgroundColor,
            Material environmentMaterial,
            PlanetLightingSettings lighting,
            GameObject[] environmentEffects)
        {
            _mapId = mapId;
            _enabled = enabled;
            _environmentPrefab = environmentPrefab;
            _backgroundSprite = backgroundSprite;
            _backgroundColor = backgroundColor;
            _environmentMaterial = environmentMaterial;
            _lighting = lighting;
            _environmentEffects = environmentEffects ?? Array.Empty<GameObject>();
        }
#endif
    }

}
