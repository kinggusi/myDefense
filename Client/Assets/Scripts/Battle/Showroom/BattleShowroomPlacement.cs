#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Battle.Showroom
{
    [Serializable]
    public sealed class BattleShowroomPlacement
    {
        public const int SlotCount = 24;
        public int slot;
        public BattleShowroomConcept concept = BattleShowroomConcept.Diad;
        public BattleShowroomFacing facing;

        public bool IsValid => slot >= 0 && slot < SlotCount && IsSupported(concept)
            && facing >= BattleShowroomFacing.Front && facing <= BattleShowroomFacing.Rear;

        public static bool IsSupported(BattleShowroomConcept concept)
            => concept >= BattleShowroomConcept.Diad && concept <= BattleShowroomConcept.PuppeteerPlan2;

        public static List<BattleShowroomPlacement> Normalize(IEnumerable<BattleShowroomPlacement> source, out int rejected)
        {
            var slots = new BattleShowroomPlacement[SlotCount];
            rejected = 0;
            if (source != null)
            {
                foreach (BattleShowroomPlacement item in source)
                {
                    if (item == null || !item.IsValid)
                    {
                        rejected++;
                        continue;
                    }
                    if (slots[item.slot] != null)
                        rejected++;
                    slots[item.slot] = new BattleShowroomPlacement
                        { slot = item.slot, concept = item.concept, facing = item.facing };
                }
            }
            var result = new List<BattleShowroomPlacement>();
            foreach (BattleShowroomPlacement item in slots)
            {
                if (item != null)
                    result.Add(item);
            }
            return result;
        }
    }

    public sealed partial class BattleShowroomConceptPreview
    {
        private BattleShowroomConceptPreview _artOwner;
        private bool _actorOnly;
        private bool _boardPlacementMode;
        private bool _placedLoop;
        private bool _placedAutomatic;
        private int _selectedPlacementSlot = 14;
        private readonly Dictionary<int, BattleShowroomConceptPreview> _placedActors = new();
        private readonly Dictionary<MeshRenderer, Mesh> _paintedTileMeshes = new();

        private BattleShowroomConceptPreview ArtOwner => _artOwner != null ? _artOwner : this;
        public bool IsBoardPlacementMode => _boardPlacementMode;
        public int PlacedUnitCount => _placedActors.Count;
        public BattleShowroomConcept Concept => _playback.Concept;
        public BattleShowroomFacing Facing => _facing;

        public MeshRenderer GetPlacementTile(int slot)
        {
            if (_actorOnly || slot < 0 || slot >= BattleShowroomPlacement.SlotCount || _boardTiles == null
                || _boardTiles.Length != BattleShowroomPlacement.SlotCount)
                return null;
            return _boardTiles[(3 - slot / 6) * 6 + slot % 6];
        }

        public BattleShowroomConceptPreview GetPlacedUnit(int slot)
            => _placedActors.TryGetValue(slot, out BattleShowroomConceptPreview actor) ? actor : null;

        public void SetSelectedPlacementSlot(int slot)
        {
            if (GetPlacementTile(slot) == null)
                return;
            _selectedPlacementSlot = slot;
            RefreshBoardSelection();
        }

        public void SetBoardPlacementMode(bool enabled)
        {
            if (_actorOnly || _boardPlacementMode == enabled)
                return;
            _boardPlacementMode = enabled;
            if (_stage == null)
                return;
            if (enabled)
            {
                _stage.SetActive(true);
                EnsureStageObjects();
                ResetEffects();
                _body?.Hide();
                if (_shadow != null)
                    _shadow.enabled = false;
            }
            else
            {
                _stage.SetActive(Concept != BattleShowroomConcept.Canonical);
            }
            foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
                actor.gameObject.SetActive(enabled);
            if (_camera != null)
                _camera.backgroundColor = enabled || Concept != BattleShowroomConcept.Canonical
                    ? Background : _canonicalBackground;
            RefreshBoardSelection();
        }

        public bool PlaceUnit(BattleShowroomPlacement placement)
        {
            if (_actorOnly || placement == null || !placement.IsValid || _stage == null
                || _camera == null || _reliefShader == null)
                return false;
            MeshRenderer tile = GetPlacementTile(placement.slot);
            if (tile == null)
                return false;
            BattleShowroomConceptArt art = BattleShowroomConceptDefinition.For(placement.concept).Art;
            if (ResolveAtlas(art, BattleShowroomConceptPose.Idle)?.IsAvailable != true
                || art == BattleShowroomConceptArt.Puppeteer && ArtOwner._doll?.IsAvailable != true)
                return false;
            RemovePlacedUnit(placement.slot);
            var root = new GameObject("Showroom slot " + (placement.slot + 1) + " " + placement.concept);
            root.transform.SetParent(_stage.transform, false);
            var actor = root.AddComponent<BattleShowroomConceptPreview>();
            actor._actorOnly = true;
            actor._artOwner = this;
            actor._camera = _camera;
            actor._stage = root;
            actor._tile = tile;
            actor._reliefShader = _reliefShader;
            actor._diadEffects = _diadEffects;
            actor._mecharonEffects = _mecharonEffects;
            actor._puppeteerEffects = _puppeteerEffects;
            actor._darkBoldUnitTextures = _darkBoldUnitTextures;
            actor._darkBoldValidationError = _darkBoldValidationError;
            actor.SetLook(_look);
            actor.SetFacing(placement.facing);
            actor.Select(placement.concept);
            actor.SetPlaybackOptions(_placedLoop, _placedAutomatic);
            root.SetActive(_boardPlacementMode);
            _placedActors.Add(placement.slot, actor);
            return true;
        }

        public bool RemovePlacedUnit(int slot)
        {
            if (!_placedActors.Remove(slot, out BattleShowroomConceptPreview actor))
                return false;
            if (actor != null)
            {
                actor.gameObject.SetActive(false);
                actor.ReleaseOwnedResources();
                DestroyOwnedObject(actor.gameObject);
            }
            return true;
        }

        public void ClearPlacedUnits()
        {
            foreach (BattleShowroomConceptPreview actor in new List<BattleShowroomConceptPreview>(_placedActors.Values))
            {
                if (actor == null)
                    continue;
                actor.gameObject.SetActive(false);
                actor.ReleaseOwnedResources();
                DestroyOwnedObject(actor.gameObject);
            }
            _placedActors.Clear();
        }

        public List<BattleShowroomPlacement> SnapshotPlacements()
        {
            var result = new List<BattleShowroomPlacement>();
            for (int slot = 0; slot < BattleShowroomPlacement.SlotCount; slot++)
            {
                BattleShowroomConceptPreview actor = GetPlacedUnit(slot);
                if (actor != null)
                    result.Add(new BattleShowroomPlacement { slot = slot, concept = actor.Concept, facing = actor.Facing });
            }
            return result;
        }

        public int RestorePlacements(IEnumerable<BattleShowroomPlacement> source)
        {
            List<BattleShowroomPlacement> layout = BattleShowroomPlacement.Normalize(source, out int rejected);
            ClearPlacedUnits();
            foreach (BattleShowroomPlacement item in layout)
            {
                if (!PlaceUnit(item))
                    rejected++;
            }
            return rejected;
        }

        public void SetPlacedPlaybackOptions(bool loop, bool automatic)
        {
            _placedLoop = loop;
            _placedAutomatic = automatic;
            foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
                actor.SetPlaybackOptions(loop, automatic);
        }

        public void PlayPlacedAction(int slot, BattleShowroomConceptAction action, bool all)
        {
            if (all)
            {
                foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
                    actor.Play(action);
                return;
            }
            GetPlacedUnit(slot)?.Play(action);
        }

        public void SetPlacedFacing(int slot, BattleShowroomFacing facing, bool all)
        {
            if (facing < BattleShowroomFacing.Front || facing > BattleShowroomFacing.Rear)
                return;
            if (all)
            {
                foreach (BattleShowroomConceptPreview actor in _placedActors.Values)
                    actor.SetFacing(facing);
                return;
            }
            GetPlacedUnit(slot)?.SetFacing(facing);
        }

        private void RefreshBoardSelection()
        {
            MeshRenderer selected = _boardPlacementMode ? GetPlacementTile(_selectedPlacementSlot) : _tile;
            if (_boardTiles == null)
                return;
            for (int index = 0; index < _boardTiles.Length; index++)
            {
                MeshRenderer tile = _boardTiles[index];
                if (tile != null && _paintedTileMeshes.TryGetValue(tile, out Mesh mesh))
                    SetBoardSurfaceUV(mesh, tile == selected ? 2 : (index + index / 6) % 2);
            }
        }

        private Bounds ProjectedActorBounds(Matrix4x4 viewRotation)
        {
            Vector3 foot = _tile != null ? _tile.bounds.center
                + Vector3.up * (_tile.bounds.extents.y + 0.012f) : Vector3.zero;
            float lift = Definition.Art == BattleShowroomConceptArt.Mecharon ? 0f : 0.025f;
            Matrix4x4 actorView = viewRotation * Matrix4x4.TRS(foot + Vector3.up * lift,
                CharacterRotation, Vector3.one);
            Bounds artBounds = _actorArtBounds;
            if (_actorOnly)
                artBounds.Expand(new Vector3(0.48f, 0f, 0.48f));
            return TransformBounds(artBounds, actorView);
        }

        private static void DestroyOwnedObject(UnityEngine.Object item)
        {
            if (item == null)
                return;
            if (Application.isPlaying)
                Destroy(item);
            else
                DestroyImmediate(item);
        }
    }
}
#endif
