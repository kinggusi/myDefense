#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Presentation;
using MyDefense.Battle.Runtime;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    public sealed class BattleShowroomCharacter : BattleShowroomWindow
    {
        private const string PreferenceId = "Character";
        private static readonly string[] Grades = { "NORMAL", "EPIC", "UNIQUE", "LEGEND", "MYTHIC" };
        private static readonly string[] MutationStates = { "Pending DNA", "Active", "Sealed" };
        private static readonly string[] PreviewChoices = { "Canonical", "Diad", "Mecharon", "Puppeteer 1", "Puppeteer 2" };
        private static readonly string[] Facings = { "Front", "Side", "Rear" };
        private static readonly string[] Looks = { "Original", "Dark + Bold" };
        private static readonly string[] Modes = { "Single", "Board placement" };
        private static readonly string[] PlacementChoices = { "Diad", "Mecharon", "Puppeteer 1", "Puppeteer 2" };

        [SerializeField] private Camera _previewCamera;
        [SerializeField] private Quaternion _battleCameraRotation;
        [SerializeField] private MeshFilter _commonBattleMesh;
        [SerializeField] private MeshRenderer _commonBattleRenderer;
        [SerializeField] private BattleShowroomConceptPreview _conceptPreview;

        [Serializable]
        public sealed class Settings
        {
            public string alienId = "29";
            public string level = "1";
            public int grade = 4;
            public int mutation = 4;
            public int mutationState = 1;
            public float zoom = 1f;
            public float rotation;
            public bool battleAngle;
            public BattleShowroomConcept concept;
            public bool conceptLoop;
            public bool conceptAutomatic = true;
            public float conceptZoom = 1f;
            public float conceptElevation = 60f;
            public BattleShowroomFacing conceptFacing;
            public BattleShowroomLook conceptLook;
            public bool boardPlacement;
            public int selectedPlacementSlot = 14;
            public BattleShowroomConcept placementBrush = BattleShowroomConcept.Diad;
            public BattleShowroomFacing placementFacing;
            public bool placementAll;
            public bool placementLoop = true;
            public bool placementAutomatic;
            public List<BattleShowroomPlacement> placements = new();
        }

        private Settings _settings;
        private CanonicalBattleAlienIdProvider _alienIds;
        private string _alienLoadError;
        private string _status;
        private Transform _preview;
        private MutationAuraView _mutationAura;

        protected override string Title => "Battle Showroom | Character";
        protected override string SessionDescription => "Local appearance preview";

        private void Start()
        {
            CanonicalBattleAlienIdProvider.TryCreate(out _alienIds, out _alienLoadError);
            _settings = BattleShowroomPreferences.Load<Settings>(PreferenceId);
            _settings.concept = (BattleShowroomConcept)Mathf.Clamp((int)_settings.concept, 0, PreviewChoices.Length - 1);
            if (_settings.conceptElevation < 50f)
                _settings.conceptElevation = 60f;
            _settings.selectedPlacementSlot = Mathf.Clamp(_settings.selectedPlacementSlot, 0, 23);
            if (!BattleShowroomPlacement.IsSupported(_settings.placementBrush))
                _settings.placementBrush = BattleShowroomConcept.Diad;
            _settings.placementFacing = (BattleShowroomFacing)Mathf.Clamp((int)_settings.placementFacing, 0, 2);
            _settings.placements = BattleShowroomPlacement.Normalize(_settings.placements, out _);
            ApplySelectedPreview();
            if (_conceptPreview != null)
            {
                _conceptPreview.SetPlacedPlaybackOptions(_settings.placementLoop, _settings.placementAutomatic);
                _conceptPreview.RestorePlacements(_settings.placements);
                _conceptPreview.SetSelectedPlacementSlot(_settings.selectedPlacementSlot);
                _conceptPreview.SetBoardPlacementMode(_settings.boardPlacement);
                if (_settings.boardPlacement)
                {
                    ClearCanonicalPreview();
                    _status = null;
                }
            }
        }

        protected override void DrawControls()
        {
            if (_settings == null)
                return;

            float controlWidth = Mathf.Max(85f, Screen.width * PreviewStart - 62f);
            int mode = GUILayout.SelectionGrid(_settings.boardPlacement ? 1 : 0, Modes, 2, GUILayout.Width(controlWidth));
            if ((mode == 1) != _settings.boardPlacement && _conceptPreview != null)
            {
                _settings.boardPlacement = mode == 1;
                _conceptPreview.SetBoardPlacementMode(_settings.boardPlacement);
                if (_settings.boardPlacement)
                {
                    ClearCanonicalPreview();
                    _status = null;
                }
                else if (_settings.concept == BattleShowroomConcept.Canonical)
                    ApplyPreview();
                SavePlacementSettings();
            }
            if (_settings.boardPlacement)
            {
                DrawPlacementControls(controlWidth);
                return;
            }
            GUILayout.Label("Preview", GUILayout.Width(controlWidth));
            int selected = GUILayout.SelectionGrid((int)_settings.concept, PreviewChoices, 2, GUILayout.Width(controlWidth));
            if (selected != (int)_settings.concept)
            {
                _settings.concept = (BattleShowroomConcept)selected;
                ApplySelectedPreview();
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            }
            if (_settings.concept != BattleShowroomConcept.Canonical)
            {
                DrawConceptControls(controlWidth);
                return;
            }

            GUILayout.Label("Canonical Alien ID");
            _settings.alienId = GUILayout.TextField(_settings.alienId ?? string.Empty, 10);
            GUILayout.Label("Preview grade");
            _settings.grade = GUILayout.SelectionGrid(_settings.grade, Grades, 3);
            GUILayout.Label("Preview level");
            _settings.level = GUILayout.TextField(_settings.level ?? string.Empty, 4);
            GUILayout.Label("Mutation");
            _settings.mutation = GUILayout.SelectionGrid(_settings.mutation,
                DevelopmentMythicFixtureRules.Mutations.ToArray(), 3);
            _settings.mutationState = GUILayout.SelectionGrid(_settings.mutationState, MutationStates, 3);
            if (GUILayout.Button("Compare original and Mutation"))
            {
                if (ApplyPreview())
                    BattleShowroomPreferences.Save(PreferenceId, _settings);
            }

            GUILayout.Space(12);
            GUILayout.Label("View");
            float zoom = GUILayout.HorizontalSlider(_settings.zoom, 0.6f, 2f);
            GUILayout.Label("Rotation");
            float rotation = GUILayout.HorizontalSlider(_settings.rotation, -180f, 180f);
            bool battleAngle = GUILayout.Toggle(_settings.battleAngle, "Use Battle camera angle");
            if (zoom != _settings.zoom || rotation != _settings.rotation || battleAngle != _settings.battleAngle)
            {
                _settings.zoom = zoom;
                _settings.rotation = rotation;
                _settings.battleAngle = battleAngle;
                UpdateCamera();
            }
            if (GUILayout.Button("Save view and selection"))
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            if (_mutationAura != null && GUILayout.Button("Replay active Mutation Effect"))
                _mutationAura.Apply(_mutationAura.ActiveMutationType);

            GUILayout.Space(12);
            GUILayout.Label(_status ?? string.Empty, GUI.skin.box);
            GUILayout.Label("Missing assets: per-Alien Model/Sprite/Prefab and Idle/Move/Attack/Skill clips.");
        }

        private void DrawConceptControls(float width)
        {
            if (_conceptPreview == null || !_conceptPreview.IsArtAvailable)
            {
                GUILayout.Label("Concept art is missing. Refresh the Character scene after importing the texture.",
                    GUI.skin.box, GUILayout.Width(width));
                return;
            }

            GUILayout.Space(10f);
            DrawLookControls(width);
            GUILayout.Label("Concept animation", GUI.skin.box, GUILayout.Width(width));
            float actionWidth = (width - 12f) / 4f;
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            DrawConceptButton("Idle", BattleShowroomConceptAction.Idle, actionWidth);
            DrawConceptButton("Move", BattleShowroomConceptAction.Move, actionWidth);
            DrawConceptButton("Basic", BattleShowroomConceptAction.Basic, actionWidth);
            DrawConceptButton("Skill", BattleShowroomConceptAction.Skill, actionWidth);
            GUILayout.EndHorizontal();
            if (_settings.concept == BattleShowroomConcept.PuppeteerPlan1)
            {
                if (GUILayout.Button("Deploy dolls", GUILayout.Width(width)))
                    _conceptPreview.Play(BattleShowroomConceptAction.Deploy);
            }
            _settings.conceptLoop = GUILayout.Toggle(_settings.conceptLoop, "Loop selected action", GUILayout.Width(width));
            if (_conceptPreview.Definition.DollCount > 0)
            {
                _settings.conceptAutomatic = GUILayout.Toggle(_settings.conceptAutomatic, "Automatic Puppet preview (10 s)", GUILayout.Width(width));
                if (_settings.conceptAutomatic)
                    GUILayout.Label($"Next automatic cue: {_conceptPreview.NextAutomaticIn:0.0} s", GUILayout.Width(width));
                if (_settings.concept == BattleShowroomConcept.PuppeteerPlan1)
                {
                    GUILayout.Label($"Field dolls: {_conceptPreview.ActiveDollCount}/{_conceptPreview.Definition.MaximumDollCount}", GUILayout.Width(width));
                    if (_conceptPreview.ActiveDollCount == 0 && !_settings.conceptLoop)
                        GUILayout.Label("Use Deploy dolls to summon another group.", GUILayout.Width(width));
                }
            }
            _conceptPreview.SetPlaybackOptions(_settings.conceptLoop, _settings.conceptAutomatic);
            GUILayout.Label("Current action: " + _conceptPreview.Action
                + (_conceptPreview.IsAutomatic ? " (automatic)" : string.Empty), GUILayout.Width(width));
            if (!_conceptPreview.IsAttackPoseAvailable)
                GUILayout.Label("Windup, attack or recovery artwork is missing. Refresh this Character scene after importing the pose sheets.", GUILayout.Width(width));
            if (!_conceptPreview.AreEffectAssetsAvailable)
                GUILayout.Label("Effect artwork is missing. Import the effect atlas and refresh this scene.", GUILayout.Width(width));
            if (!_conceptPreview.IsBoardArtAvailable)
                GUILayout.Label("SF board artwork is missing. Refresh the Character scene after importing SFBoardTiles.png.", GUILayout.Width(width));
            GUILayout.Label(ConceptDescription(), GUI.skin.box, GUILayout.Width(width), GUILayout.Height(39f));

            GUILayout.Space(9f);
            GUILayout.Label("Direction", GUILayout.Width(width));
            _settings.conceptFacing = (BattleShowroomFacing)GUILayout.SelectionGrid(
                Mathf.Clamp((int)_settings.conceptFacing, 0, Facings.Length - 1), Facings, 3, GUILayout.Width(width));
            _conceptPreview.SetFacing(_settings.conceptFacing);
            GUILayout.Label("Camera elevation", GUILayout.Width(width));
            _settings.conceptElevation = GUILayout.HorizontalSlider(_settings.conceptElevation, 50f, 80f, GUILayout.Width(width));
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button("Front overhead"))
                _settings.conceptElevation = 60f;
            if (GUILayout.Button("Top down"))
                _settings.conceptElevation = 75f;
            GUILayout.EndHorizontal();
            GUILayout.Label("Board camera zoom", GUILayout.Width(width));
            _settings.conceptZoom = GUILayout.HorizontalSlider(_settings.conceptZoom, 0.7f, 2.6f, GUILayout.Width(width));
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button("Full board"))
                _settings.conceptZoom = 1f;
            if (GUILayout.Button("Unit detail"))
                _settings.conceptZoom = 2.6f;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Restart preview", GUILayout.Width(width)))
                _conceptPreview.Select(_settings.concept);
            if (GUILayout.Button("Save preview settings", GUILayout.Width(width)))
                BattleShowroomPreferences.Save(PreferenceId, _settings);
        }

        private void DrawConceptButton(string label, BattleShowroomConceptAction action, float width)
        {
            if (GUILayout.Button(label, GUILayout.Width(width)))
                _conceptPreview.Play(action);
        }

        private void DrawLookControls(float width)
        {
            GUILayout.Label("Look", GUILayout.Width(width));
            int selected = GUILayout.SelectionGrid((int)_conceptPreview.Look, Looks, 2, GUILayout.Width(width));
            if (selected != (int)_conceptPreview.Look && _conceptPreview.SetLook((BattleShowroomLook)selected))
            {
                _settings.conceptLook = _conceptPreview.Look;
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            }
            if (!_conceptPreview.IsDarkBoldAvailable)
                GUILayout.Label(_conceptPreview.LookUnavailableReason, GUI.skin.box, GUILayout.Width(width));
        }

        private void DrawPlacementControls(float width)
        {
            if (_conceptPreview == null)
            {
                GUILayout.Label("The Character preview is missing.", GUILayout.Width(width));
                return;
            }
            DrawLookControls(width);
            GUILayout.Label("Appearance setup", GUILayout.Width(width));
            int brush = GUILayout.SelectionGrid((int)_settings.placementBrush - 1, PlacementChoices, 2, GUILayout.Width(width));
            if (brush != (int)_settings.placementBrush - 1)
            {
                _settings.placementBrush = (BattleShowroomConcept)(brush + 1);
                SavePlacementSettings();
            }
            string[] cells = new string[24];
            for (int slot = 0; slot < cells.Length; slot++)
            {
                BattleShowroomConceptPreview actor = _conceptPreview.GetPlacedUnit(slot);
                string code = actor == null ? "-" : actor.Concept switch
                {
                    BattleShowroomConcept.Diad => "D",
                    BattleShowroomConcept.Mecharon => "M",
                    BattleShowroomConcept.PuppeteerPlan1 => "P1",
                    _ => "P2"
                };
                cells[slot] = (slot + 1).ToString("00") + " " + code;
            }
            int selectedSlot = GUILayout.SelectionGrid(_settings.selectedPlacementSlot, cells, 6,
                GUILayout.Width(width), GUILayout.Height(112f));
            if (selectedSlot != _settings.selectedPlacementSlot)
            {
                _settings.selectedPlacementSlot = selectedSlot;
                _conceptPreview.SetSelectedPlacementSlot(selectedSlot);
                BattleShowroomConceptPreview actor = _conceptPreview.GetPlacedUnit(selectedSlot);
                if (actor != null)
                    _settings.placementFacing = actor.Facing;
                SavePlacementSettings();
            }
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button("Place / replace"))
            {
                bool placed = _conceptPreview.PlaceUnit(new BattleShowroomPlacement
                {
                    slot = _settings.selectedPlacementSlot, concept = _settings.placementBrush,
                    facing = _settings.placementFacing
                });
                _status = placed ? null : "The selected concept art or tile is unavailable.";
                SavePlacementSettings();
            }
            if (GUILayout.Button("Remove selected"))
            {
                _conceptPreview.RemovePlacedUnit(_settings.selectedPlacementSlot);
                SavePlacementSettings();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button("4-unit preset"))
            {
                _conceptPreview.ClearPlacedUnits();
                int[] slots = { 7, 10, 13, 16 };
                for (int index = 0; index < slots.Length; index++)
                    _conceptPreview.PlaceUnit(new BattleShowroomPlacement
                    {
                        slot = slots[index], concept = (BattleShowroomConcept)(index + 1),
                        facing = _settings.placementFacing
                    });
                _settings.selectedPlacementSlot = slots[0];
                _conceptPreview.SetSelectedPlacementSlot(slots[0]);
                BattleShowroomConceptPreview first = _conceptPreview.GetPlacedUnit(slots[0]);
                if (first != null)
                    _settings.placementFacing = first.Facing;
                SavePlacementSettings();
            }
            if (GUILayout.Button("Clear all"))
            {
                _conceptPreview.ClearPlacedUnits();
                SavePlacementSettings();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label($"Slot {_settings.selectedPlacementSlot + 1} | {_conceptPreview.PlacedUnitCount}/24 units", GUILayout.Width(width));
            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label(_status, GUI.skin.box, GUILayout.Width(width));
            GUILayout.Label("Preview placement only. Canonical assets use Single.", GUILayout.Width(width));

            _settings.placementAll = GUILayout.Toggle(_settings.placementAll, "Actions / direction: all placed units", GUILayout.Width(width));
            GUILayout.Label(_settings.placementAll ? "Target: all placed units" : "Target: selected slot", GUILayout.Width(width));
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            foreach (BattleShowroomConceptAction action in new[]
                     { BattleShowroomConceptAction.Idle, BattleShowroomConceptAction.Move,
                         BattleShowroomConceptAction.Basic, BattleShowroomConceptAction.Skill })
            {
                if (GUILayout.Button(action.ToString()))
                    _conceptPreview.PlayPlacedAction(_settings.selectedPlacementSlot, action, _settings.placementAll);
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Deploy P1 dolls", GUILayout.Width(width)))
                _conceptPreview.PlayPlacedAction(_settings.selectedPlacementSlot, BattleShowroomConceptAction.Deploy, _settings.placementAll);
            int facing = GUILayout.SelectionGrid((int)_settings.placementFacing, Facings, 3, GUILayout.Width(width));
            if (facing != (int)_settings.placementFacing)
            {
                _settings.placementFacing = (BattleShowroomFacing)facing;
                _conceptPreview.SetPlacedFacing(_settings.selectedPlacementSlot, _settings.placementFacing, _settings.placementAll);
                SavePlacementSettings();
            }
            bool loop = GUILayout.Toggle(_settings.placementLoop, "Loop action for placed units", GUILayout.Width(width));
            bool automatic = GUILayout.Toggle(_settings.placementAutomatic, "Automatic Puppet cue for placed units", GUILayout.Width(width));
            if (loop != _settings.placementLoop || automatic != _settings.placementAutomatic)
            {
                _settings.placementLoop = loop;
                _settings.placementAutomatic = automatic;
                _conceptPreview.SetPlacedPlaybackOptions(loop, automatic);
                SavePlacementSettings();
            }
            GUILayout.Label("Camera elevation", GUILayout.Width(width));
            _settings.conceptElevation = GUILayout.HorizontalSlider(_settings.conceptElevation, 50f, 80f, GUILayout.Width(width));
            GUILayout.Label("Board camera zoom", GUILayout.Width(width));
            _settings.conceptZoom = GUILayout.HorizontalSlider(_settings.conceptZoom, 0.7f, 2.6f, GUILayout.Width(width));
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            if (GUILayout.Button("Full board"))
                _settings.conceptZoom = 1f;
            if (GUILayout.Button("Selected detail"))
                _settings.conceptZoom = 2.6f;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Save layout and view", GUILayout.Width(width)))
                SavePlacementSettings();
        }

        private void SavePlacementSettings()
        {
            _settings.placements = _conceptPreview.SnapshotPlacements();
            BattleShowroomPreferences.Save(PreferenceId, _settings);
        }

        private string ConceptDescription() => _settings.concept switch
        {
            BattleShowroomConcept.Diad => "Ritual Cut: alternating beam / slash.\nSkill: unified Ritual Cut.",
            BattleShowroomConcept.Mecharon => "Basic: cannon burst.\nSkill: missile fan.",
            BattleShowroomConcept.PuppeteerPlan1 => "Basic: each doll strikes its own target.\nMove: reposition. Skill: detonate.",
            _ => "Basic: curved attack and return.\nSkill: throw and burst at the apex."
        };

        protected override void ResetControls()
        {
            if (_conceptPreview != null)
            {
                _conceptPreview.SetBoardPlacementMode(false);
                _conceptPreview.ClearPlacedUnits();
                _conceptPreview.SetPlacedPlaybackOptions(true, false);
            }
            BattleShowroomPreferences.Clear(PreferenceId);
            _settings = new Settings();
            ApplySelectedPreview();
            _conceptPreview?.SetSelectedPlacementSlot(_settings.selectedPlacementSlot);
        }

        private void ApplySelectedPreview()
        {
            if (_conceptPreview != null)
            {
                if (!_conceptPreview.SetLook(_settings.conceptLook))
                    _settings.conceptLook = _conceptPreview.Look;
                _conceptPreview.Select(_settings.concept);
                _conceptPreview.SetPlaybackOptions(_settings.conceptLoop, _settings.conceptAutomatic);
                _conceptPreview.SetFacing(_settings.conceptFacing);
            }
            if (_settings.concept == BattleShowroomConcept.Canonical)
                ApplyPreview();
            else
                ClearCanonicalPreview();
        }

        private void ClearCanonicalPreview()
        {
            if (_preview != null)
            {
                _preview.gameObject.SetActive(false);
                Destroy(_preview.gameObject);
                _preview = null;
                _mutationAura = null;
            }
        }

        private bool ApplyPreview()
        {
            ClearCanonicalPreview();

            _settings.grade = Mathf.Clamp(_settings.grade, 0, Grades.Length - 1);
            _settings.mutation = Mathf.Clamp(_settings.mutation, 0, DevelopmentMythicFixtureRules.Mutations.Count - 1);
            _settings.mutationState = Mathf.Clamp(_settings.mutationState, 0, MutationStates.Length - 1);
            _settings.zoom = Mathf.Clamp(_settings.zoom, 0.6f, 2f);
            _settings.rotation = Mathf.Clamp(_settings.rotation, -180f, 180f);
            if (_alienIds == null)
                return Fail(_alienLoadError);
            if (!long.TryParse(_settings.alienId, NumberStyles.None, CultureInfo.InvariantCulture, out long id)
                || !_alienIds.Contains(id))
                return Fail("Alien ID is missing from canonical Alien data.");
            if (!int.TryParse(_settings.level, NumberStyles.None, CultureInfo.InvariantCulture, out int level) || level < 1)
                return Fail("Enter a positive preview level.");

            string mutation = DevelopmentMythicFixtureRules.Mutations[_settings.mutation];
            bool active = _settings.mutationState == 1 && mutation != DevelopmentMythicFixtureRules.NoneMutation;
            if (active && _settings.grade != 4)
                return Fail("Active Mutation is available on a Mythic preview.");
            if (active && !DevelopmentMythicFixtureRules.TryNormalize(id, mutation, out _, out string reason))
                return Fail(reason);
            if (_commonBattleMesh == null || _commonBattleMesh.sharedMesh == null
                || _commonBattleRenderer == null || _commonBattleRenderer.sharedMaterials.Length == 0)
                return Fail("The common Battle prefab or its mesh/material is missing.");
            if (_previewCamera == null)
                return Fail("The preview camera is missing.");

            _preview = new GameObject("Appearance Comparison").transform;
            _preview.SetParent(transform, false);
            _preview.localPosition = new Vector3(3f, 0f, 0f);
            CreateView("Original", -1.15f);
            GameObject altered = CreateView("Mutation", 1.15f);
            if (active)
            {
                _mutationAura = altered.AddComponent<MutationAuraView>();
                _mutationAura.Apply(mutation);
            }
            _status = $"Left: Original. Right: {MutationStates[_settings.mutationState]} {mutation}.\n"
                + $"Alien {id} | {Grades[_settings.grade]} | Level {level}\n"
                + "Rendering: common Battle prefab mesh and materials.";
            UpdateCamera();
            return true;
        }

        private bool Fail(string message)
        {
            _status = message;
            return false;
        }

        private GameObject CreateView(string label, float x)
        {
            var item = new GameObject(label, typeof(UnitData));
            item.transform.SetParent(_preview, false);
            item.transform.localPosition = new Vector3(x, 0f, 0f);
            var model = new GameObject("Common Battle mesh", typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(item.transform, false);
            model.transform.localScale = _commonBattleMesh.transform.localScale * 0.8f;
            model.GetComponent<MeshFilter>().sharedMesh = _commonBattleMesh.sharedMesh;
            model.GetComponent<MeshRenderer>().sharedMaterials = _commonBattleRenderer.sharedMaterials;
            item.GetComponent<UnitData>().grade = Grades[_settings.grade];
            item.AddComponent<AlienGradeView>();
            return item;
        }

        private void UpdateCamera()
        {
            if (_preview == null || _previewCamera == null)
                return;

            foreach (Transform item in _preview)
                item.localRotation = Quaternion.Euler(0f, _settings.rotation, 0f);

            Quaternion rotation = _settings.battleAngle ? _battleCameraRotation : Quaternion.Euler(27f, 0f, 0f);
            _previewCamera.transform.SetPositionAndRotation(
                _preview.position - rotation * Vector3.forward * 10f, rotation);
            _previewCamera.orthographic = true;
            UpdateViewport();
        }

        private void LateUpdate() => UpdateViewport();

        private void UpdateViewport()
        {
            if (_previewCamera == null || _settings == null)
                return;
            var area = new Rect(PreviewStart, 0f, 1f - PreviewStart, 1f);
            if (_previewCamera.rect != area)
                _previewCamera.rect = area;
            if ((_settings.boardPlacement || _settings.concept != BattleShowroomConcept.Canonical) && _conceptPreview != null)
            {
                _conceptPreview.FrameTileCamera(area.width * Screen.width / Mathf.Max(1, Screen.height),
                    _settings.conceptZoom, _settings.conceptElevation);
                return;
            }
            float aspect = area.width * Screen.width / Mathf.Max(1, Screen.height);
            _previewCamera.orthographicSize = Mathf.Max(1.8f, 1.7f / Mathf.Max(0.15f, aspect)) / _settings.zoom;
        }

        protected override void ReturnToHub()
        {
            if (_settings != null)
                BattleShowroomPreferences.Save(PreferenceId, _settings);
            base.ReturnToHub();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            Camera previewCamera, Quaternion battleCameraRotation, MeshFilter mesh, MeshRenderer renderer,
            BattleShowroomConceptPreview conceptPreview)
        {
            _previewCamera = previewCamera;
            _battleCameraRotation = battleCameraRotation;
            _commonBattleMesh = mesh;
            _commonBattleRenderer = renderer;
            _conceptPreview = conceptPreview;
        }
#endif
    }
}
#endif
