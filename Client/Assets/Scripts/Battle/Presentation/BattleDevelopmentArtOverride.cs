#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using MyDefense.Battle.Combat;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Explicit local visual substitution on existing real battle units. Does
    /// not create units, set canonical IDs, issue commands, or alter snapshots.
    /// Both peers must select the same option to compare their presentation.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class BattleDevelopmentArtOverride : MonoBehaviour
    {
        public enum Selection { Off, Mixed, Diad, Mecharon, Puppeteer1, Puppeteer2 }
        private readonly Dictionary<UnitData, BattleUnitArtView> _views = new();
        private readonly Dictionary<ulong, ShotArt> _shots = new();
        private readonly List<BattleProjectileNetworkState> _pendingProjectiles = new();
        private readonly List<UnitData> _staleUnits=new();
        private readonly List<ulong> _expiredShots=new();
        private FusionKidnapBoardView _board;
        private BattleUnitArtCatalog _catalog;
        private Selection _selection;
        private bool _darkBold = true;
        private bool _expanded;
        private Vector2 _panelScroll;
        private float _nextRefresh;
        private Camera _camera;
        private Vector3 _previousCameraPosition;
        private Quaternion _previousCameraRotation;
        private float _previousCameraSize;
        private bool _cameraFramed;
        private BattleFieldPerspective _fieldPerspective;
        private string _error;
        public int ObservedShots { get; private set; }
        public int ObservedImpacts { get; private set; }
        public int SkippedUnstampedImpacts { get; private set; }
        public Selection CurrentSelection => _selection;

        private void OnEnable()
        {
            _catalog = Resources.Load<BattleUnitArtCatalog>(BattleUnitArtCatalog.ResourcePath);
            _board=GetComponent<FusionKidnapBoardView>();
            if(_board!=null){_board.UnitViewChanged-=UnitChanged;_board.UnitViewChanged+=UnitChanged;}
            BattleProjectileNetworkState.PresentationSpawned += ObserveSpawn;
            BattleProjectileSpawner.PresentationPrimaryHit += ObserveHit;
            BattleProjectileSpawner.PresentationSessionReset += ResetForSession;
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith("-battleArt=", StringComparison.OrdinalIgnoreCase)) continue;
                string value = argument.Substring("-battleArt=".Length);
                if (TryParseSelection(value, out Selection selection)) SetSelection(selection);
                else _error = "Unknown -battleArt value: " + value;
            }
        }

        public static bool TryParseSelection(string value, out Selection selection)
        {
            if (Enum.TryParse(value, true, out selection) && Enum.IsDefined(typeof(Selection), selection)) return true;
            selection = Selection.Off; return false;
        }

        public static BattleUnitVisual VisualFor(Selection selection, long alienSpecId)
        {
            // A development alias, not a canonical character assignment. The
            // replicated species identity survives move/swap and late joins.
            if (selection == Selection.Mixed) return (BattleUnitVisual)((Math.Max(1L, alienSpecId) - 1L) % 4L);
            return selection == Selection.Mecharon ? BattleUnitVisual.Mecharon
                : selection == Selection.Puppeteer1 ? BattleUnitVisual.Puppeteer1
                : selection == Selection.Puppeteer2 ? BattleUnitVisual.Puppeteer2 : BattleUnitVisual.Diad;
        }

        public void SetSelection(Selection selection)
        {
            if (!Enum.IsDefined(typeof(Selection), selection)) return;
            ReleaseViews();
            _selection = selection; _error = null; _nextRefresh = 0f;
            if (selection == Selection.Off) RestoreCamera();
            Debug.Log("[BattleArt] Development local visual override=" + selection
                + "; canonical IDs/statistics/commands are unchanged.");
        }

        private void Update()
        {
            if (_selection == Selection.Off || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .15f;
            if (_catalog == null) { _error = "Battle art catalog missing. Run Generate Battle Presentation Assets."; return; }
            if (!_cameraFramed) FrameBattleCamera();
            _staleUnits.Clear();_expiredShots.Clear();
            foreach (var pair in _views) if (pair.Key == null || pair.Value == null) _staleUnits.Add(pair.Key);
            foreach (UnitData unit in _staleUnits) _views.Remove(unit);
            foreach (var shot in _shots) if (Time.unscaledTime - shot.Value.started > 12f) _expiredShots.Add(shot.Key);
            foreach (ulong id in _expiredShots) _shots.Remove(id);
            if(_board!=null)foreach(GameObject unit in _board.VisibleUnits)UnitChanged(unit);
        }
        private void UnitChanged(GameObject unit){if(unit!=null)EnsureView(unit.GetComponent<UnitData>());}

        private void LateUpdate()
        {
            if (_selection == Selection.Off) return;
            for (int i = _pendingProjectiles.Count - 1; i >= 0; i--)
            {
                BattleProjectileNetworkState pending = _pendingProjectiles[i];
                if (pending == null || pending.Object == null || !pending.Object.IsValid || TryObserveSpawn(pending))
                    _pendingProjectiles.RemoveAt(i);
            }
        }

        private BattleUnitArtView EnsureView(UnitData unit)
        {
            if (_selection == Selection.Off || _catalog == null || unit == null || unit.gameObject.scene != gameObject.scene
                || unit.name != "FusionKidnapUnit" || unit.specId <= 0) return null;
            BattleUnitVisual kind = VisualFor(_selection, unit.specId);
            if (_views.TryGetValue(unit, out BattleUnitArtView current) && current != null && current.enabled)
            {
                if ((current.Visual != kind || current.BoundSpecId != unit.specId || current.DarkBold != _darkBold)
                    && !current.Configure(_catalog, kind, _darkBold))
                { _error = "Artwork unavailable for merged unit: " + kind; return null; }
                return current;
            }
            var art = unit.gameObject.AddComponent<BattleUnitArtView>();
            if (!art.Configure(_catalog, kind, _darkBold))
            {
                _error = "Artwork unavailable for " + kind + (_darkBold ? " / Dark + Bold" : " / Original");
                BattleSpriteGeometry.Release(art); return null;
            }
            _views[unit] = art;
            return art;
        }

        private void ObserveSpawn(BattleProjectileNetworkState projectile)
        {
            if (_selection == Selection.Off || projectile == null || FusionKidnapBoardView.Active == null) return;
            if (projectile.gameObject.scene != gameObject.scene) return;
            if (!TryObserveSpawn(projectile) && !_pendingProjectiles.Contains(projectile))
                _pendingProjectiles.Add(projectile);
        }

        private bool TryObserveSpawn(BattleProjectileNetworkState projectile)
        {
            if (FusionKidnapBoardView.Active == null) return false;
            if (_shots.ContainsKey(projectile.RuntimeProjectileId)) return true;
            if (!projectile.HasStateAuthority)
            {
                _fieldPerspective ??= FindFirstObjectByType<BattleFieldPerspective>();
                if (_fieldPerspective != null && !_fieldPerspective.IsApplied) return false;
                if (!projectile.TryGetLocalPresentationPosition(out _)) return false;
            }
            if (!FusionKidnapBoardView.Active.TryGetUnitTransform(projectile.AttackerServerId, out Transform unit)) return false;
            // The first real shot can precede the periodic board scan.
            BattleUnitArtView art = EnsureView(unit.GetComponent<UnitData>());
            if (art == null || !art.enabled) return false;
            BattleUnitArtShot shot = art.CaptureShot(projectile.RuntimeProjectileId);
            var painted = projectile.GetComponent<BattlePaintedProjectileView>()
                ?? projectile.gameObject.AddComponent<BattlePaintedProjectileView>();
            painted.Configure(projectile, art, shot);
            _shots[projectile.RuntimeProjectileId] = new ShotArt
                { artwork = shot, source = art, started = Time.unscaledTime };
            ObservedShots++;
            return true;
        }

        private void ObserveHit(BattleProjectileSpawner sender, ulong projectileId, long attackerId, Vector3 position)
        {
            if (_selection == Selection.Off || sender.gameObject.scene != gameObject.scene) return;
            if (!_shots.TryGetValue(projectileId, out ShotArt shot))
            {
                // The old slot might now contain a merge result or another
                // species. Without a launch stamp, no cosmetic identity is known.
                SkippedUnstampedImpacts++;
                return;
            }
            int effect = shot.artwork.Visual >= BattleUnitVisual.Puppeteer1 ? 2 : 4;
            BattleArtImpact.Play(shot.artwork.Catalog.Effects(shot.artwork.Visual), effect, position + Vector3.up * .1f, .72f, .30f);
            if (shot.artwork.Matches(shot.source))
            { shot.source.ObserveShot(projectileId, position); shot.source.EndFlight(projectileId); }
            _shots.Remove(projectileId);
            ObservedImpacts++;
        }

        private void ResetForSession(BattleProjectileSpawner sender)
        {
            if (sender == null || sender.gameObject.scene != gameObject.scene) return;
            _shots.Clear(); _pendingProjectiles.Clear();
            foreach (BattleUnitArtView view in _views.Values)
                if (view != null) view.ResetShotHistory();
            ObservedShots = 0; ObservedImpacts = 0; SkippedUnstampedImpacts = 0;
            _nextRefresh = 0f; _fieldPerspective = null;
        }

        private void FrameBattleCamera()
        {
            Transform p1 = GameObject.Find("GridManager")?.transform;
            Transform p2 = GameObject.Find("EnemyGridParent")?.transform;
            _camera = Camera.main;
            if (p1 == null || p2 == null || _camera == null || !_camera.orthographic) return;
            _previousCameraPosition = _camera.transform.position; _previousCameraRotation = _camera.transform.rotation;
            _previousCameraSize = _camera.orthographicSize;
            Quaternion rotation = Quaternion.Euler(60f, 0f, 0f);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Bounds projected = new(); bool any = false;
            foreach (Transform grid in new[] { p1, p2 })
            {
                if (!grid.gameObject.activeInHierarchy) continue;
                for (int row = 0; row < 4; row++)
                for (int col = 0; col < 6; col++)
                {
                    Transform tile = grid.Find($"Tile_{col}_{row}");
                    if (tile == null) continue;
                    foreach (Vector3 corner in new[] { new Vector3(-.60f, .05f, -.60f),
                        new Vector3(.60f, .05f, .60f), new Vector3(-.60f, 1.65f, -.60f), new Vector3(.60f, 1.65f, .60f) })
                    {
                        Vector3 point = inverse * (tile.position + corner);
                        if (!any) { projected = new Bounds(point, Vector3.zero); any = true; } else projected.Encapsulate(point);
                    }
                }
            }
            if (!any) return;
            _camera.transform.SetPositionAndRotation(rotation * projected.center - rotation * Vector3.forward * 30f, rotation);
            _camera.orthographicSize = Mathf.Max(projected.extents.y, projected.extents.x / Mathf.Max(.1f, _camera.aspect)) * 1.12f;
            _cameraFramed = true;
        }

        private void RestoreCamera()
        {
            if (_cameraFramed && _camera != null)
            { _camera.transform.SetPositionAndRotation(_previousCameraPosition, _previousCameraRotation); _camera.orthographicSize = _previousCameraSize; }
            _cameraFramed = false;
        }

        private void OnGUI()
        {
            if(BattleHudPresentation.HasActiveModal)return;
            float width = 246f;
            bool hud = BattleHudPresentation.HasActiveHud;
            if (hud)
            {
                _expanded = BattleHudPresentation.IsDevelopmentDrawerOpen(1);
                if(GUI.Button(BattleHudPresentation.DevelopmentToggleRect(1),"Dev Art: "+_selection)) _expanded=BattleHudPresentation.ToggleDevelopmentDrawer(1);
                if(!_expanded)return;
            }
            Rect panel = hud ? BattleHudPresentation.DevelopmentDrawerRect(width+20,335) : new Rect(Screen.width-width-8,8,width,_expanded?335:32);
            if(hud) BattleHudPresentation.RegisterDevelopmentInput(panel);
            GUILayout.BeginArea(panel, GUI.skin.box);
            if (!hud && GUILayout.Button("Dev Battle Art: " + _selection + (_expanded ? " [-]" : " [+]"))) _expanded = !_expanded;
            if (_expanded)
            {
                _panelScroll=GUILayout.BeginScrollView(_panelScroll);
                GUILayout.Label("LOCAL visuals / existing real units\nNo canonical character mapping");
                Selection[] options = { Selection.Off, Selection.Mixed, Selection.Diad, Selection.Mecharon, Selection.Puppeteer1, Selection.Puppeteer2 };
                int selected = GUILayout.SelectionGrid((int)_selection,
                    new[] { "Off", "Mixed dev aliases", "Diad", "Mecharon", "Puppet 1", "Puppet 2" }, 2);
                if (selected != (int)_selection) SetSelection(options[selected]);
                bool look = GUILayout.Toggle(_darkBold, "Dark + Bold (clean edge candidate)");
                if (look != _darkBold) { _darkBold = look; SetSelection(_selection); }
                GUILayout.Label("Both peers: select same local option.\nReal basic attack only; no new skills.");
                GUILayout.Label("Shots " + ObservedShots + " / accepted impacts " + ObservedImpacts);
                GUILayout.Label("Unstamped instant FX skipped: " + SkippedUnstampedImpacts);
                if (!string.IsNullOrEmpty(_error)) GUILayout.Label(_error);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void ReleaseViews()
        {
            foreach (var pair in _views)
                if (pair.Value != null) { pair.Value.ReleaseArtwork(); pair.Value.enabled = false; BattleSpriteGeometry.Release(pair.Value); }
            _views.Clear();
            _shots.Clear();
            _pendingProjectiles.Clear();
            foreach (BattlePaintedProjectileView projectile in FindObjectsByType<BattlePaintedProjectileView>(FindObjectsSortMode.None))
                if (projectile.gameObject.scene == gameObject.scene) projectile.ReleaseArtwork();
        }
        private sealed class ShotArt
        {
            public BattleUnitArtShot artwork;
            public BattleUnitArtView source;
            public float started;
        }
        private void OnDisable()
        {
            if(_board!=null)_board.UnitViewChanged-=UnitChanged;
            BattleProjectileNetworkState.PresentationSpawned -= ObserveSpawn;
            BattleProjectileSpawner.PresentationPrimaryHit -= ObserveHit;
            BattleProjectileSpawner.PresentationSessionReset -= ResetForSession;
            ReleaseViews(); RestoreCamera();
        }
    }
}
#endif
