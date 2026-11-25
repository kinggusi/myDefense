#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.Linq;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Presentation;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public sealed class BattleShowroomContent : BattleShowroomWindow
    {
        private const string PreferenceId = "Content";
        private static readonly string[] PlayerViews = { "Player 1 view", "Player 2 view" };
        private static readonly string[] StageLabels = { "1", "2", "3", "4", "5" };
        private static readonly Color LocalColor = new(0.24f, 0.91f, 0.83f);
        private static readonly Color RemoteColor = new(1f, 0.48f, 0.59f);
        private static readonly Color BossColor = new(1f, 0.75f, 0.3f);

        [SerializeField] private Camera _battleCamera;
        [SerializeField] private PlanetContentApplicator _environment;
        [SerializeField] private Vector3[] _player1Slots;
        [SerializeField] private Vector3[] _player2Slots;
        [SerializeField] private Vector3[] _player2LocalSlots;
        [SerializeField] private Vector3[] _player1RemoteSlots;
        [SerializeField] private Vector3[] _player1Path;
        [SerializeField] private Vector3[] _player2Path;
        [SerializeField] private Vector3[] _bossPath;

        [Serializable]
        public sealed class Settings
        {
            public string mapId = "NEPTUNE";
            public int stage = 1;
            public int playerView;
            public bool showGeometry = true;
            public bool showComparisonUnits = true;
            public string wave = "1";
            public int dailyWave = 1;
        }

        private Settings _settings;
        private CanonicalBalanceBundle _balance;
        private string[] _maps = Array.Empty<string>();
        private string[] _mapLabels = Array.Empty<string>();
        private string _status;
        private readonly System.Collections.Generic.List<BattleUnitArtView> _comparisonUnits = new();
        private bool IsDaily => DailyBattleContentCatalog.RequiredMapIds.Contains(_settings.mapId, StringComparer.Ordinal);
        protected override string Title => "Battle Showroom | Map & Content";
        protected override string SessionDescription => "Local environment and authored Battle layout";

        private void Start()
        {
            _settings = BattleShowroomPreferences.Load<Settings>(PreferenceId);
            if (_environment != null)
                _environment.SetBattlefieldLayout(new PlanetBattlefieldLayout(_player1Slots, _player2Slots, _player1Path, _player2Path, _bossPath));
            CanonicalBalanceLoadResult result = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(), new ExistingMonsterPrefabRuntimeMapping());
            if (!result.IsValid)
            {
                _status = string.Join("\n", result.Errors);
                return;
            }
            _balance = result.Bundle;
            _maps = _balance.PlanetBattles.All.Where(planet => planet.Enabled).Select(planet => planet.MapId)
                .Concat(DailyBattleContentCatalog.RequiredMapIds).ToArray();
            _mapLabels = _maps.Select(map => map == DailyBattleExecutionPlanBuilder.CultivationMapId ? "Cultivation Zone"
                : map == DailyBattleExecutionPlanBuilder.MutationLabMapId ? "Mutation Lab" : map).ToArray();
            if (!_maps.Contains(_settings.mapId, StringComparer.Ordinal))
                _settings.mapId = new Settings().mapId;
            ApplyMap();
        }

        protected override void DrawControls()
        {
            if (_settings == null)
                return;
            if (_balance == null)
            {
                GUILayout.Label(_status ?? "Canonical Battle content is unavailable.", GUI.skin.box);
                return;
            }

            GUILayout.Label("Environment Profile");
            int previous = Array.IndexOf(_maps, _settings.mapId);
            int selection = GUILayout.SelectionGrid(previous, _mapLabels, 2);
            if (selection != previous && selection >= 0 && selection < _maps.Length)
            {
                _settings.mapId = _maps[selection];
                UpdateSelection();
            }
            if (IsDaily)
            {
                GUILayout.Label("Daily Stage");
                int stage = GUILayout.SelectionGrid(_settings.stage - 1, StageLabels, 5) + 1;
                if (stage != _settings.stage)
                {
                    _settings.stage = stage;
                    UpdateSelection();
                }
                GUILayout.Label("Daily uses Player 1 board and Lane, including its final Boss.");
            }
            else
            {
                int view = GUILayout.SelectionGrid(_settings.playerView, PlayerViews, 2);
                if (view != _settings.playerView)
                {
                    _settings.playerView = view;
                    BattleShowroomPreferences.Save(PreferenceId, _settings);
                }
            }
            bool geometry = GUILayout.Toggle(_settings.showGeometry, "Show board slots, lanes and spawn points");
            if (geometry != _settings.showGeometry)
            {
                _settings.showGeometry = geometry;
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            }
            bool units = GUILayout.Toggle(_settings.showComparisonUnits, "Show four comparison units (visual only)");
            if (units != _settings.showComparisonUnits)
            {
                _settings.showComparisonUnits = units;
                RefreshComparisonUnits(); BattleShowroomPreferences.Save(PreferenceId, _settings);
            }
            GUILayout.Space(10);
            GUILayout.Label(_status ?? string.Empty, GUI.skin.box);
            DrawWaveLaunch();
        }

        private void DrawWaveLaunch()
        {
            GUILayout.Space(10);
            GUILayout.Label("Fresh offline validation session", GUI.skin.box);
            if (IsDaily)
            {
                if (!_balance.DailyBattleStages.TryGet(SelectedDailyType(), _settings.stage, out var rows) || rows.Count == 0)
                    return;
                GUILayout.Label("Initial Daily Wave");
                string[] labels = rows.Select(row => $"W{row.Wave}" + (row.Boss ? " Boss" : string.Empty)).ToArray();
                int previous = Mathf.Clamp(_settings.dailyWave - 1, 0, labels.Length - 1);
                int selection = GUILayout.SelectionGrid(previous, labels, 4);
                if (selection >= 0 && selection < labels.Length)
                    _settings.dailyWave = rows[selection].Wave;
                if (GUILayout.Button("Open selected Daily Wave"))
                {
                    BattleShowroomPreferences.Save(PreferenceId, _settings);
                    new BattleShowroomLaunch(_settings.mapId, _settings.dailyWave, SelectedDailyType(), _settings.stage)
                        .OpenCombat();
                }
                return;
            }

            GUILayout.Label("Initial planet Wave (1–80)");
            _settings.wave = GUILayout.TextField(_settings.wave ?? string.Empty, 2);
            bool validWave = int.TryParse(_settings.wave, NumberStyles.None, CultureInfo.InvariantCulture, out int wave)
                && _balance.RuntimeWaves.Items.Any(item => item.Enabled && item.RoundNumber == wave);
            if (GUILayout.Button("Prepare selected Wave / Boss"))
            {
                if (!validWave)
                {
                    _status = "Select an enabled canonical Wave from 1–80.";
                    return;
                }
                BattleShowroomPreferences.Save(PreferenceId, _settings);
                new BattleShowroomLaunch(_settings.mapId, wave).OpenCombat();
            }
        }

        protected override void OnGUI()
        {
            if (_settings != null && _settings.showGeometry && _battleCamera != null)
            {
                bool remoteViewer = !IsDaily && _settings.playerView == 1;
                DrawSlots(remoteViewer ? _player2LocalSlots : _player1Slots, LocalColor);
                DrawPath(_player1Path, remoteViewer ? "P2 local" : "P1 local", LocalColor, true);
                if (!IsDaily)
                {
                    DrawSlots(remoteViewer ? _player1RemoteSlots : _player2Slots, RemoteColor);
                    DrawPath(_player2Path, remoteViewer ? "P1 remote" : "P2 remote", RemoteColor, true);
                    DrawPath(_bossPath, "Boss", BossColor, false);
                }
            }
            base.OnGUI();
        }

        private void LateUpdate()
        {
            if (_battleCamera == null)
                return;
            var area = new Rect(PreviewStart, 0f, 1f - PreviewStart, 1f);
            if (_battleCamera.rect != area)
                _battleCamera.rect = area;
            FramePreview(area);
        }

        private void FramePreview(Rect viewport)
        {
            if (_player1Slots == null || _player1Slots.Length != 24) return;
            Bounds bounds;
            if (_environment != null && _environment.ActiveBattlefield != null)
                bounds = _environment.ActiveBattlefield.FocusBounds;
            else
            {
                bounds = new Bounds(_player1Slots[0], Vector3.zero);
                foreach (Vector3 point in _player1Slots) bounds.Encapsulate(point);
                if (_settings != null && !IsDaily)
                    foreach (Vector3 point in _player2Slots) bounds.Encapsulate(point);
                bounds.Expand(new Vector3(3f, 1.7f, 3f));
                bounds.Encapsulate(new Vector3(bounds.center.x, _player1Slots[0].y + 1.7f, bounds.center.z));
            }
            Quaternion rotation = Quaternion.Euler(60f, 0f, 0f);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 min = bounds.min, max = bounds.max;
            Bounds projected = new(inverse * min, Vector3.zero);
            for (int i = 0; i < 8; i++)
                projected.Encapsulate(inverse * new Vector3((i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
            float aspect = Mathf.Max(.1f, Screen.width * viewport.width / Mathf.Max(1f, Screen.height * viewport.height));
            _battleCamera.orthographic = true; _battleCamera.aspect = aspect;
            _battleCamera.transform.SetPositionAndRotation(rotation * projected.center - rotation * Vector3.forward * 30f, rotation);
            _battleCamera.orthographicSize = Mathf.Max(projected.extents.y, projected.extents.x / aspect) * 1.05f;
        }

        protected override void ResetControls()
        {
            BattleShowroomPreferences.Clear(PreferenceId);
            _settings = new Settings();
            if (_balance != null)
                ApplyMap();
        }

        protected override void ReturnToHub()
        {
            if (_settings != null)
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            base.ReturnToHub();
        }

        private void UpdateSelection()
        {
            if (ApplyMap())
                BattleShowroomPreferences.Save(PreferenceId, _settings);
        }

        private bool ApplyMap()
        {
            if (_environment == null)
            {
                _status = "Battle environment applicator is missing.";
                return false;
            }

            _settings.stage = Mathf.Clamp(_settings.stage, 1, 5);
            _settings.playerView = Mathf.Clamp(_settings.playerView, 0, 1);
            _environment.Clear();
            RefreshComparisonUnits();
            if (!IsDaily)
            {
                if (!_environment.TryApply(_settings.mapId, _balance.PlanetBattles, out string error))
                {
                    _status = error;
                    return false;
                }
                _status = _settings.mapId + (_environment.ActiveBattlefield != null
                    ? "\nShared Battle Profile: terrain, tiles, Lane and border."
                    : "\nLegacy environment Profile loaded.") + "\nPreview 60 degrees. No network/account commands.";
                return true;
            }

            var catalog = Resources.Load<DailyBattleContentCatalog>(DailyBattleContentCatalog.ResourcesPath);
            if (catalog == null)
            {
                _status = "Daily environment catalog is missing.";
                return false;
            }
            if (!catalog.TryResolve(_settings.mapId, out PlanetContentProfile profile, out string dailyError)
                || !_environment.TryApplyResolvedProfile(_settings.mapId, profile, out dailyError))
            {
                _status = dailyError;
                return false;
            }
            if (!_balance.DailyBattleStages.TryGet(SelectedDailyType(), _settings.stage, out var rows) || rows.Count == 0)
            {
                _status = "Daily Stage is missing from canonical data.";
                return false;
            }
            _status = $"{_settings.mapId} | Stage {_settings.stage} | {rows[0].TimeLimitSeconds} sec\n"
                + string.Join("\n", rows.Where(row => row.Enabled).Select(row =>
                    $"W{row.Wave:D2} {row.MonsterSpecId} x{row.SpawnCount}"
                    + (row.Boss ? " | Boss" : string.Empty)
                    + $" | {row.StatusEffectType}"));
            return true;
        }

        private string SelectedDailyType()
            => _settings.mapId == DailyBattleExecutionPlanBuilder.CultivationMapId
                ? DailyBattleExecutionPlanBuilder.CultivationContentType
                : DailyBattleExecutionPlanBuilder.MutationLabContentType;

        private void RefreshComparisonUnits()
        {
            ClearComparisonUnits();
            if (!_settings.showComparisonUnits || _player1Slots == null || _player1Slots.Length != 24) return;
            BattleUnitArtCatalog art = Resources.Load<BattleUnitArtCatalog>(BattleUnitArtCatalog.ResourcePath);
            if (art == null) { _status = "Comparison unit artwork unavailable."; return; }
            int[] slots = { 7, 10, 19, 22 };
            for (int index = 0; index < 4; index++)
            {
                var root = new GameObject("Planet comparison unit " + index, typeof(UnitData));
                root.transform.SetParent(transform, false);
                root.transform.position = _player1Slots[slots[index]] + Vector3.up * .80f;
                // No canonical species assignment, UnitAttack, Runner, or commands.
                var view = root.AddComponent<BattleUnitArtView>();
                if (!view.Configure(art, (BattleUnitVisual)index, true))
                { BattleSpriteGeometry.Release(root); _status = "Comparison unit artwork unavailable: " + index; continue; }
                _comparisonUnits.Add(view);
            }
        }

        private void ClearComparisonUnits()
        {
            foreach (BattleUnitArtView view in _comparisonUnits)
                if (view != null) { view.ReleaseArtwork(); BattleSpriteGeometry.Release(view.gameObject); }
            _comparisonUnits.Clear();
        }
        private void OnDestroy() => ClearComparisonUnits();

        private void DrawSlots(Vector3[] positions, Color color)
        {
            if (positions == null)
                return;
            for (int index = 0; index < positions.Length; index++)
            {
                if (!TryProject(positions[index], out Vector2 gui))
                    continue;
                DrawSquare(gui, 5f, color);
                if (index == 0 || index == positions.Length - 1)
                    GUI.Label(new Rect(gui + new Vector2(4f, -14f), new Vector2(40f, 24f)), (index + 1).ToString());
            }
        }

        private void DrawPath(Vector3[] positions, string label, Color color, bool loop)
        {
            if (positions == null || positions.Length == 0)
                return;
            for (int index = 0; index < positions.Length; index++)
            {
                if (!TryProject(positions[index], out Vector2 gui))
                    continue;
                int next = (index + 1) % positions.Length;
                if ((loop || next != 0) && TryProject(positions[next], out Vector2 to))
                    DrawLine(gui, to, color);
                DrawSquare(gui, index == 0 ? 11f : 6f, color);
                GUI.Label(new Rect(gui + new Vector2(6f, -21f), new Vector2(135f, 24f)),
                    index == 0 ? label + " spawn" : index.ToString());
            }
        }

        private bool TryProject(Vector3 world, out Vector2 gui)
        {
            Vector3 screen = _battleCamera.WorldToScreenPoint(world);
            gui = new Vector2(screen.x, Screen.height - screen.y);
            return screen.z > 0f;
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            Vector2 delta = to - from;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - 1f, delta.magnitude, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.matrix = matrix;
        }

        private static void DrawSquare(Vector2 center, float size, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), Texture2D.whiteTexture);
            GUI.color = previous;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Camera battleCamera,
            PlanetContentApplicator environment,
            Vector3[] player1Slots,
            Vector3[] player2Slots,
            Vector3[] player2LocalSlots,
            Vector3[] player1RemoteSlots,
            Vector3[] player1Path,
            Vector3[] player2Path,
            Vector3[] bossPath)
        {
            _battleCamera = battleCamera;
            _environment = environment;
            _player1Slots = player1Slots;
            _player2Slots = player2Slots;
            _player2LocalSlots = player2LocalSlots;
            _player1RemoteSlots = player1RemoteSlots;
            _player1Path = player1Path;
            _player2Path = player2Path;
            _bossPath = bossPath;
        }
#endif
    }
}
#endif
