using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Development visualization for the authoritative Kidnap board state.
    /// It intentionally uses primitive geometry until the networked Alien
    /// object/prefab path is introduced by the next Battle task.
    /// </summary>
    public sealed class FusionKidnapBoardView : MonoBehaviour
    {
        public static FusionKidnapBoardView Active { get; private set; }

        private BattleWaveStateAuthority _authority;
        private Transform _player1Grid;
        private Transform _player2Grid;
        private GameObject _unitPrefab;
        private bool _unitPrefabResolved;
        private readonly Dictionary<int, Dictionary<long, BattleAlienAttackSnapshotJson>> _attackCatalogs = new();
        private readonly HashSet<int> _loadingAttackCatalogs = new();
        private readonly HashSet<int> _failedAttackCatalogs = new();
        private readonly Dictionary<int, string> _loadedPlayerIds = new();
        private readonly Dictionary<int, string> _requestedPlayerIds = new();
        private int _selectedLocalMythicSlot = -1;
        private bool _soloPlayerOneMode;

        public int SelectedLocalMythicSlot => _selectedLocalMythicSlot;

        public void SetSoloPlayerOneMode(bool enabled)
        {
            _soloPlayerOneMode = enabled;
            _player2Grid ??= GameObject.Find("EnemyGridParent")?.transform;
            if (_player2Grid != null)
                _player2Grid.gameObject.SetActive(!enabled);
        }

        private void OnEnable()
        {
            Active = this;
            _authority = FindFirstObjectByType<BattleWaveStateAuthority>();
            if (_authority == null) return;
            _player1Grid = GameObject.Find("GridManager")?.transform;
            _player2Grid = GameObject.Find("EnemyGridParent")?.transform;
            _unitPrefab = ResolveUnitPrefab();
            _unitPrefabResolved = true;
            _authority.KidnapApplied += ApplyKidnap;
            _authority.InjectorApplied += ApplyInjector;
            _authority.MutationApplied += ApplyMutation;
            _authority.BoardChanged += ApplyBoardChange;
            _authority.BoardSwapped += ApplyBoardSwap;
            _authority.ResonanceUpgraded += ApplyResonanceUpgrade;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
            if (_authority != null)
            {
                _authority.KidnapApplied -= ApplyKidnap;
                _authority.InjectorApplied -= ApplyInjector;
                _authority.MutationApplied -= ApplyMutation;
                _authority.BoardChanged -= ApplyBoardChange;
                _authority.BoardSwapped -= ApplyBoardSwap;
                _authority.ResonanceUpgraded -= ApplyResonanceUpgrade;
            }
        }

        private void Update()
        {
            if (_authority == null || !_authority.Object || !_authority.Object.IsValid)
                return;

            EnsureAttackCatalog(1, _authority.Player1UserId.ToString());
            if (!_soloPlayerOneMode)
                EnsureAttackCatalog(2, _authority.Player2UserId.ToString());

            // Reconcile from the replicated occupancy snapshot so late joins,
            // scene reloads, and reconnects do not depend on past RPC events.
            for (int slotIndex = 0; slotIndex < 24; slotIndex++)
            {
                ReconcileSlot(1, slotIndex);
                if (!_soloPlayerOneMode)
                    ReconcileSlot(2, slotIndex);
            }
        }

        private void ReconcileSlot(int playerSlot, int slotIndex)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            Transform tile = ResolveTile(grid, slotIndex);
            if (tile == null) return;
            Transform unit = tile.Find("FusionKidnapUnit");
            bool occupied = _authority.IsBoardOccupied(playerSlot, slotIndex);
            // During a drag the source object is temporarily reparented to the
            // grid, so its source tile is intentionally empty. Do not create a
            // second visual while the pointer is holding the original unit.
            if (occupied && unit == null && !HasDraggingUnit(playerSlot, slotIndex))
                if (_authority.IsBoardInjector(playerSlot, slotIndex))
                    ApplyInjector(playerSlot, slotIndex, _authority.GetBoardMutationType(playerSlot, slotIndex));
                else
                    ApplyKidnap(playerSlot, slotIndex, _authority.GetBoardAlienId(playerSlot, slotIndex), _authority.GetBoardGrade(playerSlot, slotIndex));
            else if (!occupied && unit != null)
                Object.Destroy(unit.gameObject);
            else if (occupied && unit != null && !_authority.IsBoardInjector(playerSlot, slotIndex))
                SyncUnitRuntimeState(unit.gameObject, playerSlot, slotIndex, _authority.GetBoardAlienId(playerSlot, slotIndex));
        }

        private static bool HasDraggingUnit(int playerSlot, int slotIndex)
        {
            FusionKidnapUnitDrag[] drags = Object.FindObjectsByType<FusionKidnapUnitDrag>(FindObjectsSortMode.None);
            foreach (FusionKidnapUnitDrag drag in drags)
            {
                if (drag != null && drag.IsDragging && drag.PlayerSlot == playerSlot && drag.SlotIndex == slotIndex)
                    return true;
            }
            return false;
        }

        private void ApplyKidnap(int playerSlot, int slotIndex, long alienId)
        {
            ApplyKidnap(playerSlot, slotIndex, alienId, _authority != null ? _authority.GetBoardGrade(playerSlot, slotIndex) : (byte)0);
        }

        private void ApplyKidnap(int playerSlot, int slotIndex, long alienId, byte grade)
        {
            if (slotIndex < 0 || slotIndex >= 24) return;
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            if (grid == null) return;
            int row = slotIndex / 6;
            int column = slotIndex % 6;
            // GridManager authors rows from the bottom (z=0). Present the
            // logical sequence from the top row on screen. A remote field is
            // rotated 180 degrees by BattleFieldPerspective, so its authored
            // row order is already visually inverted.
            bool isMirroredField = Mathf.Abs(Mathf.DeltaAngle(grid.localEulerAngles.y, 180f)) < 45f;
            int authoredColumn = isMirroredField ? 5 - column : column;
            int authoredRow = 3 - row;
            Transform tile = ResolveTile(grid, slotIndex);
            if (tile == null || tile.Find("FusionKidnapUnit") != null) return;

            GameObject unit = CreateUnitVisual(playerSlot, slotIndex, alienId, grade);
            unit.name = "FusionKidnapUnit";
            unit.transform.SetParent(tile, false);
            // Grid tiles are authored with a flattened Y scale (0.1). Compensate
            // the child transform so the temporary visual is not embedded in
            // the floor and keeps a round world-space shape.
            Vector3 tileScale = tile.lossyScale;
            unit.transform.localPosition = new Vector3(
                0f,
                SafeDivide(0.8f, tileScale.y),
                0f);
            unit.transform.localScale = new Vector3(
                SafeDivide(0.55f, tileScale.x),
                SafeDivide(0.55f, tileScale.y),
                SafeDivide(0.55f, tileScale.z));
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = playerSlot == 1 ? Color.cyan : Color.magenta;
            ConfigureUnitData(unit, playerSlot, slotIndex, alienId, grade);
            ApplyAttackSnapshot(unit, playerSlot, alienId);
            ApplyGradeVisual(unit.transform, grade, alienId);
            ApplyInitialActiveMutationVisual(unit);
            FusionKidnapUnitDrag drag = unit.AddComponent<FusionKidnapUnitDrag>();
            drag.Initialize(this, _authority, playerSlot, slotIndex);
            Debug.Log($"[FusionKidnapBoardView] slot={playerSlot} alienId={alienId} grid={grid.name} tile={tile.name} row={row} column={column} mirrored={isMirroredField} index={slotIndex}.");
        }

        private void ApplyInjector(int playerSlot, int slotIndex, string mutationType)
        {
            ApplyKidnapVisual(playerSlot, slotIndex, 0, byte.MaxValue, mutationType);
        }

        private void ApplyMutation(int playerSlot, int sourceSlotIndex, int targetSlotIndex, string mutationType)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            Transform source = ResolveTile(grid, sourceSlotIndex);
            if (source != null && source.Find("FusionKidnapUnit") != null) Object.Destroy(source.Find("FusionKidnapUnit").gameObject);
            Transform target = ResolveTile(grid, targetSlotIndex);
            if (target != null && target.Find("FusionKidnapUnit") != null)
            {
                GameObject targetUnit = target.Find("FusionKidnapUnit").gameObject;
                SyncUnitRuntimeState(targetUnit, playerSlot, targetSlotIndex, _authority.GetBoardAlienId(playerSlot, targetSlotIndex));
            }
        }

        private void ApplyKidnapVisual(int playerSlot, int slotIndex, long alienId, byte grade, string mutationType)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            Transform tile = ResolveTile(grid, slotIndex);
            if (tile == null || tile.Find("FusionKidnapUnit") != null) return;
            GameObject unit = CreateUnitVisual(playerSlot, slotIndex, alienId, grade);
            unit.name = "FusionKidnapUnit";
            unit.transform.SetParent(tile, false);
            ApplyUnitTransform(unit.transform, tile);
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(1f, 0.55f, 0.1f);
            ConfigureUnitData(unit, playerSlot, slotIndex, alienId, grade);
            ApplyMutationVisual(unit, mutationType);
            FusionKidnapUnitDrag drag = unit.AddComponent<FusionKidnapUnitDrag>();
            drag.Initialize(this, _authority, playerSlot, slotIndex);
        }

        private static void ApplyMutationVisual(GameObject unit, string mutationType)
        {
            if (unit == null) return;
            string normalized = string.IsNullOrWhiteSpace(mutationType)
                ? "NONE"
                : mutationType.Trim().ToUpperInvariant();
            string marker = MutationAuraView.ResolveMarker(normalized);
            bool active = !string.IsNullOrEmpty(marker);
            TextMesh label = FindUnitLabel(unit.transform, "MutationLabel");
            if (active)
            {
                label ??= CreateUnitLabel(
                    unit.transform,
                    "MutationLabel",
                    new Vector3(0f, 0f, -0.86f),
                    36,
                    0.14f);
                label.text = "M:" + marker;
                label.gameObject.SetActive(true);
            }
            else if (label != null)
            {
                label.text = string.Empty;
                label.gameObject.SetActive(false);
            }

            MutationAuraView aura = unit.GetComponent<MutationAuraView>();
            if (active)
            {
                aura ??= unit.AddComponent<MutationAuraView>();
                aura.Apply(normalized);
            }
            else if (aura != null)
            {
                aura.Apply(null);
            }
        }

        public bool TryGetUnitTransform(long serverId, out Transform unit)
        {
            unit = null;
            if (!TryDecodeUnitServerId(serverId, out int playerSlot, out int slotIndex))
                return false;

            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            Transform tile = ResolveTile(grid, slotIndex);
            unit = tile == null ? null : tile.Find("FusionKidnapUnit");
            return unit != null;
        }

        public static bool TryDecodeUnitServerId(long serverId, out int playerSlot, out int slotIndex)
        {
            playerSlot = (int)(serverId >> 32);
            slotIndex = (int)((uint)serverId - 1);
            return (playerSlot == 1 || playerSlot == 2) && slotIndex >= 0 && slotIndex < 24;
        }

        private static bool ApplyInitialActiveMutationVisual(GameObject unit)
        {
            UnitData data = unit == null ? null : unit.GetComponent<UnitData>();
            if (data == null)
                return false;
            if (string.IsNullOrWhiteSpace(data.activeMutationType))
            {
                ApplyMutationVisual(unit, null);
                return false;
            }
            ApplyMutationVisual(unit, data.activeMutationType);
            return true;
        }

        public Transform GetGrid(int playerSlot) => playerSlot == 1 ? _player1Grid : _player2Grid;

        public int EnsureSelectedLocalMythicSlot(int playerSlot)
        {
            if (IsSelectableMythic(playerSlot, _selectedLocalMythicSlot))
                return _selectedLocalMythicSlot;
            return SelectNextLocalMythicSlot(playerSlot, 1);
        }

        public int SelectNextLocalMythicSlot(int playerSlot, int direction)
        {
            if (_authority == null || (playerSlot != 1 && playerSlot != 2))
                return _selectedLocalMythicSlot = -1;
            int step = direction < 0 ? -1 : 1;
            int start = IsValidBoardSlot(_selectedLocalMythicSlot)
                ? _selectedLocalMythicSlot
                : (step > 0 ? -1 : 24);
            for (int offset = 1; offset <= 24; offset++)
            {
                int candidate = (start + step * offset) % 24;
                if (candidate < 0) candidate += 24;
                if (IsSelectableMythic(playerSlot, candidate))
                    return _selectedLocalMythicSlot = candidate;
            }
            return _selectedLocalMythicSlot = -1;
        }

        private bool IsSelectableMythic(int playerSlot, int slotIndex)
            => IsValidBoardSlot(slotIndex)
                && _authority.IsBoardOccupied(playerSlot, slotIndex)
                && !_authority.IsBoardInjector(playerSlot, slotIndex)
                && _authority.GetBoardGrade(playerSlot, slotIndex) == 4;

        private static bool IsValidBoardSlot(int slotIndex) => slotIndex >= 0 && slotIndex < 24;

        private GameObject ResolveUnitPrefab()
        {
            if (_unitPrefabResolved)
                return _unitPrefab;

            GameManager manager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            _unitPrefab = manager == null ? null : manager.unitPrefab;
            _unitPrefabResolved = true;
            if (_unitPrefab == null)
                Debug.LogWarning("[FusionKidnapBoardView] GameManager.unitPrefab is not available; battle units will be visual-only.");
            return _unitPrefab;
        }

        private GameObject CreateUnitVisual(int playerSlot, int slotIndex, long alienId, byte grade)
        {
            GameObject prefab = ResolveUnitPrefab();
            if (prefab != null)
            {
                GameObject unit = Object.Instantiate(prefab);
                AlienMergeHintView legacyMergeHint = unit.GetComponent<AlienMergeHintView>();
                if (legacyMergeHint != null)
                    legacyMergeHint.enabled = false;
                return unit;
            }

            // Keep the old primitive fallback for development scenes that do not
            // contain GameManager, but make the limitation explicit in the log.
            return GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        private void ConfigureUnitData(GameObject unit, int playerSlot, int slotIndex, long alienId, byte grade)
        {
            UnitData data = unit == null ? null : unit.GetComponent<UnitData>();
            if (data == null)
                return;

            // The board slot is the runtime identity for this temporary Fusion
            // presentation. It must be unique across both player fields so the
            // damage audit can distinguish two copies of the same Alien species.
            data.serverId = ((long)playerSlot << 32) | (uint)(slotIndex + 1);
            data.specId = alienId;
            data.grade = GradeName(grade);
            data.unitName = "Alien-" + alienId;
            ApplyMutationState(data, playerSlot, slotIndex);
        }

        private void SyncUnitRuntimeState(GameObject unit, int playerSlot, int slotIndex, long alienId)
        {
            UnitData data = unit == null ? null : unit.GetComponent<UnitData>();
            if (data == null)
                return;
            string previousActiveMutation = data.activeMutationType;
            bool snapshotChanged = ApplyAuthoritativeUnitState(
                data,
                playerSlot,
                slotIndex,
                alienId,
                _authority.GetBoardGrade(playerSlot, slotIndex),
                _authority.GetBoardMutationState(playerSlot, slotIndex),
                _authority.GetBoardMutationType(playerSlot, slotIndex));
            bool activeMutationChanged = !string.Equals(
                previousActiveMutation,
                data.activeMutationType,
                StringComparison.Ordinal);
            if (activeMutationChanged)
                ApplyMutationVisual(unit, data.activeMutationType);
            if (snapshotChanged)
                ApplyAttackSnapshot(unit, playerSlot, alienId);
        }

        private static bool ApplyAuthoritativeUnitState(
            UnitData data,
            int playerSlot,
            int slotIndex,
            long alienId,
            byte grade,
            byte mutationState,
            string mutationType)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            long serverId = ((long)playerSlot << 32) | (uint)(slotIndex + 1);
            string gradeName = GradeName(grade);
            string previousActiveMutation = data.activeMutationType;
            bool metadataChanged = data.serverId != serverId
                || data.specId != alienId
                || !string.Equals(data.grade, gradeName, StringComparison.Ordinal);

            if (metadataChanged)
            {
                data.serverId = serverId;
                data.specId = alienId;
                data.grade = gradeName;
                data.unitName = "Alien-" + alienId;
            }

            ApplyMutationState(data, mutationState, mutationType);
            return metadataChanged
                || !string.Equals(previousActiveMutation, data.activeMutationType, StringComparison.Ordinal);
        }

        private void ApplyMutationState(UnitData data, int playerSlot, int slotIndex)
        {
            if (data == null || _authority == null)
                return;
            ApplyMutationState(
                data,
                _authority.GetBoardMutationState(playerSlot, slotIndex),
                _authority.GetBoardMutationType(playerSlot, slotIndex));
        }

        public static void ApplyMutationState(UnitData data, byte state, string mutationType)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.pendingMutationType = state == 2 || state == 4 ? mutationType : null;
            data.activeMutationType = state == 3 ? mutationType : null;
        }

        private static string GradeName(byte grade)
        {
            return grade switch
            {
                1 => "EPIC",
                2 => "UNIQUE",
                3 => "LEGEND",
                4 => "MYTHIC",
                _ => "NORMAL"
            };
        }

        public bool TryFindNearestSlot(int playerSlot, Vector3 worldPosition, int excludeSlotIndex, out int slotIndex)
        {
            slotIndex = -1;
            Transform grid = GetGrid(playerSlot);
            if (grid == null) return false;

            float bestDistance = float.MaxValue;
            for (int candidate = 0; candidate < 24; candidate++)
            {
                if (candidate == excludeSlotIndex) continue;
                Transform tile = ResolveTile(grid, candidate);
                if (tile == null) continue;
                float distance = Vector2.Distance(
                    new Vector2(worldPosition.x, worldPosition.z),
                    new Vector2(tile.position.x, tile.position.z));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    slotIndex = candidate;
                }
            }

            return slotIndex >= 0 && bestDistance < 2.5f;
        }

        public void SnapUnitToSlot(GameObject unit, int playerSlot, int slotIndex)
        {
            if (unit == null) return;
            Transform tile = ResolveTile(GetGrid(playerSlot), slotIndex);
            if (tile == null) return;
            ApplyUnitTransform(unit.transform, tile);
        }

        private void ApplyBoardChange(int playerSlot, int fromSlotIndex, int toSlotIndex, bool merged, long resultAlienId, byte resultGrade)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            if (grid == null) return;
            Transform sourceTile = ResolveTile(grid, fromSlotIndex);
            Transform targetTile = ResolveTile(grid, toSlotIndex);
            if (sourceTile == null || targetTile == null) return;
            Transform sourceUnit = sourceTile.Find("FusionKidnapUnit");

            if (merged)
            {
                if (fromSlotIndex != toSlotIndex && sourceUnit != null)
                    Object.Destroy(sourceUnit.gameObject);
                Transform targetUnit = targetTile.Find("FusionKidnapUnit");
                if (targetUnit != null && resultAlienId > 0)
                {
                    // The target visual is reused for the merge result. Keep
                    // its runtime UnitData in sync with the authoritative
                    // result so grade/spec-based UI and subsequent merges do
                    // not continue to treat an Epic result as the old Normal.
                    ConfigureUnitData(targetUnit.gameObject, playerSlot, toSlotIndex, resultAlienId, resultGrade);
                    ApplyAttackSnapshot(targetUnit.gameObject, playerSlot, resultAlienId);
                    ApplyGradeVisual(targetUnit, resultGrade, resultAlienId);
                    ApplyInitialActiveMutationVisual(targetUnit.gameObject);
                }
                Debug.Log($"[FusionKidnapBoardView] merge slot={playerSlot} source={fromSlotIndex} target={toSlotIndex} resultAlienId={resultAlienId} resultGrade={resultGrade}.");
                return;
            }

            if (sourceUnit == null) return;

            if (targetTile.Find("FusionKidnapUnit") != null) return;
            ApplyUnitTransform(sourceUnit, targetTile);
            FusionKidnapUnitDrag drag = sourceUnit.GetComponent<FusionKidnapUnitDrag>();
            if (drag != null)
                drag.UpdateSlotIndex(toSlotIndex);
            SyncUnitRuntimeState(
                sourceUnit.gameObject,
                playerSlot,
                toSlotIndex,
                _authority.GetBoardAlienId(playerSlot, toSlotIndex));
            Debug.Log($"[FusionKidnapBoardView] move slot={playerSlot} source={fromSlotIndex} target={toSlotIndex}.");
        }

        private void ApplyBoardSwap(int playerSlot, int sourceSlotIndex, int targetSlotIndex)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            Transform sourceTile = ResolveTile(grid, sourceSlotIndex);
            Transform targetTile = ResolveTile(grid, targetSlotIndex);
            if (sourceTile == null || targetTile == null) return;
            Transform sourceUnit = sourceTile.Find("FusionKidnapUnit");
            Transform targetUnit = targetTile.Find("FusionKidnapUnit");
            if (sourceUnit == null || targetUnit == null) return;

            ApplyUnitTransform(sourceUnit, targetTile);
            ApplyUnitTransform(targetUnit, sourceTile);
            FusionKidnapUnitDrag sourceDrag = sourceUnit.GetComponent<FusionKidnapUnitDrag>();
            FusionKidnapUnitDrag targetDrag = targetUnit.GetComponent<FusionKidnapUnitDrag>();
            if (sourceDrag != null) sourceDrag.UpdateSlotIndex(targetSlotIndex);
            if (targetDrag != null) targetDrag.UpdateSlotIndex(sourceSlotIndex);
            SyncUnitRuntimeState(
                sourceUnit.gameObject,
                playerSlot,
                targetSlotIndex,
                _authority.GetBoardAlienId(playerSlot, targetSlotIndex));
            SyncUnitRuntimeState(
                targetUnit.gameObject,
                playerSlot,
                sourceSlotIndex,
                _authority.GetBoardAlienId(playerSlot, sourceSlotIndex));
            Debug.Log($"[FusionKidnapBoardView] swap slot={playerSlot} source={sourceSlotIndex} target={targetSlotIndex}.");
        }

        private static void ApplyUnitTransform(Transform unit, Transform tile)
        {
            unit.SetParent(tile, false);
            Vector3 tileScale = tile.lossyScale;
            unit.localPosition = new Vector3(0f, SafeDivide(0.8f, tileScale.y), 0f);
            unit.localScale = new Vector3(
                SafeDivide(0.55f, tileScale.x),
                SafeDivide(0.55f, tileScale.y),
                SafeDivide(0.55f, tileScale.z));
        }

        private static void ApplyGradeVisual(Transform unit, byte grade, long alienId)
        {
            if (unit == null) return;
            Renderer renderer = unit.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Color color = ColorForAlien(alienId);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", color);
                properties.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(properties);
            }
            TextMesh label = FindUnitLabel(unit, "GradeLabel")
                ?? CreateUnitLabel(unit, "GradeLabel", new Vector3(0f, 0f, -0.58f), 48, 0.18f);
            label.gameObject.SetActive(true);
            label.text = grade switch { 1 => "E", 2 => "U", 3 => "L", 4 => "M", _ => "N" };
        }

        private static TextMesh FindUnitLabel(Transform unit, string labelName)
        {
            Transform labelTransform = unit == null ? null : unit.Find(labelName);
            return labelTransform == null ? null : labelTransform.GetComponent<TextMesh>();
        }

        private static TextMesh CreateUnitLabel(
            Transform unit,
            string labelName,
            Vector3 localPosition,
            int fontSize,
            float localScale)
        {
            GameObject labelObject = new GameObject(labelName);
            labelObject.transform.SetParent(unit, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * localScale;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 1f;
            label.fontSize = fontSize;
            label.color = Color.white;
            return label;
        }

        private static Color ColorForAlien(long alienId)
        {
            long normalizedAlienId = alienId > 0L ? alienId : 1L;
            int speciesIndex = (int)((normalizedAlienId - 1L) % 7L);
            return speciesIndex switch
            {
                0 => new Color(0.90f, 0.15f, 0.15f), // red
                1 => new Color(1.00f, 0.45f, 0.05f), // orange
                2 => new Color(0.95f, 0.82f, 0.08f), // yellow
                3 => new Color(0.15f, 0.75f, 0.25f), // green
                4 => new Color(0.12f, 0.45f, 0.95f), // blue
                5 => new Color(0.25f, 0.18f, 0.65f), // indigo
                _ => new Color(0.62f, 0.20f, 0.82f)  // purple
            };
        }

        private static Transform ResolveTile(Transform grid, int slotIndex)
        {
            if (grid == null || slotIndex < 0 || slotIndex >= 24) return null;
            int row = slotIndex / 6;
            int column = slotIndex % 6;
            bool isMirroredField = Mathf.Abs(Mathf.DeltaAngle(grid.localEulerAngles.y, 180f)) < 45f;
            int authoredColumn = isMirroredField ? 5 - column : column;
            int authoredRow = 3 - row;
            return grid.Find($"Tile_{authoredColumn}_{authoredRow}");
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        private void EnsureAttackCatalog(int playerSlot, string playerId)
        {
            if (!_authority.IsAuthoritative || string.IsNullOrWhiteSpace(playerId))
                return;
            if (!_requestedPlayerIds.TryGetValue(playerSlot, out string requestedPlayerId)
                || !string.Equals(requestedPlayerId, playerId, StringComparison.Ordinal))
            {
                _requestedPlayerIds[playerSlot] = playerId;
                _failedAttackCatalogs.Remove(playerSlot);
            }
            if (_loadedPlayerIds.TryGetValue(playerSlot, out string loadedPlayerId)
                && string.Equals(loadedPlayerId, playerId, StringComparison.Ordinal))
                return;
            if (_failedAttackCatalogs.Contains(playerSlot))
                return;
            if (_loadingAttackCatalogs.Contains(playerSlot))
                return;

            _loadingAttackCatalogs.Add(playerSlot);
            StartCoroutine(LoadAttackCatalog(playerSlot, playerId));
        }

        private IEnumerator LoadAttackCatalog(int playerSlot, string playerId)
        {
            string url = RuntimeEnvironmentConfig.ApiBaseUrl
                + "/battle/entry/attack-snapshots?playerId="
                + UnityWebRequest.EscapeURL(playerId);
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;
            yield return request.SendWebRequest();

            _loadingAttackCatalogs.Remove(playerSlot);
            if (request.result != UnityWebRequest.Result.Success)
            {
                _failedAttackCatalogs.Add(playerSlot);
                Debug.LogError($"[FusionKidnapBoardView] Attack snapshot load failed for player {playerSlot}: {request.error}");
                yield break;
            }

            BattleAttackSnapshotCatalogJson response;
            try
            {
                response = JsonUtility.FromJson<BattleAttackSnapshotCatalogJson>(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                _failedAttackCatalogs.Add(playerSlot);
                Debug.LogError($"[FusionKidnapBoardView] Attack snapshot JSON is invalid: {exception.Message}");
                yield break;
            }

            BattleWaveExecutor executor = _authority.Executor;
            if (!IsCompatibleCatalog(response, executor)
                || !string.Equals(response.playerId, playerId, StringComparison.Ordinal))
            {
                _failedAttackCatalogs.Add(playerSlot);
                Debug.LogError($"[FusionKidnapBoardView] Attack snapshot balance mismatch for player {playerSlot}.");
                yield break;
            }

            var catalog = new Dictionary<long, BattleAlienAttackSnapshotJson>();
            foreach (BattleAlienAttackSnapshotJson entry in response.aliens ?? Array.Empty<BattleAlienAttackSnapshotJson>())
            {
                if (entry != null && entry.alienId > 0
                    && IsFinitePositive(entry.damage)
                    && IsFinitePositive(entry.attackRate)
                    && IsFinitePositive(entry.range))
                    catalog[entry.alienId] = entry;
            }
            if (catalog.Count == 0)
            {
                _failedAttackCatalogs.Add(playerSlot);
                Debug.LogError($"[FusionKidnapBoardView] Attack snapshot catalog is empty for player {playerSlot}.");
                yield break;
            }

            _attackCatalogs[playerSlot] = catalog;
            _loadedPlayerIds[playerSlot] = playerId;
            _failedAttackCatalogs.Remove(playerSlot);
            ApplyCatalogToExistingUnits(playerSlot);
            Debug.Log($"[FusionKidnapBoardView] Applied {catalog.Count} server-calculated attack snapshots for player {playerSlot}.");
        }

        internal static bool IsCompatibleCatalog(BattleAttackSnapshotCatalogJson response, BattleWaveExecutor executor)
        {
            return executor != null && IsCompatibleCatalog(
                response,
                executor.CanonicalBalanceVersion,
                executor.CanonicalContentHash);
        }

        public static bool IsCompatibleCatalog(
            BattleAttackSnapshotCatalogJson response,
            string canonicalBalanceVersion,
            string canonicalContentHash)
        {
            return response != null
                && string.Equals(response.balanceVersion, canonicalBalanceVersion, StringComparison.Ordinal)
                && string.Equals(response.contentHash, canonicalContentHash, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyCatalogToExistingUnits(int playerSlot)
        {
            Transform grid = GetGrid(playerSlot);
            if (grid == null)
                return;
            for (int slot = 0; slot < 24; slot++)
            {
                Transform unit = ResolveTile(grid, slot)?.Find("FusionKidnapUnit");
                UnitData data = unit == null ? null : unit.GetComponent<UnitData>();
                if (data != null && data.specId > 0)
                    ApplyAttackSnapshot(unit.gameObject, playerSlot, data.specId);
            }
        }

        private void ApplyResonanceUpgrade(int playerSlot, CanonicalResonanceTrack track, int level, int remainingGold)
        {
            ApplyCatalogToExistingUnits(playerSlot);
        }

        private bool ApplyAttackSnapshot(GameObject unit, int playerSlot, long alienId)
        {
            if (unit == null
                || !_attackCatalogs.TryGetValue(playerSlot, out Dictionary<long, BattleAlienAttackSnapshotJson> catalog)
                || !catalog.TryGetValue(alienId, out BattleAlienAttackSnapshotJson entry))
                return false;
            UnitData data = unit.GetComponent<UnitData>();
            UnitAttack attack = unit.GetComponent<UnitAttack>();
            if (data == null || attack == null)
                return false;

            AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(
                data.serverId,
                entry.damage,
                entry.attackRate,
                entry.range,
                data.activeMutationType);
            byte grade = _authority.GetBoardGrade(playerSlot, (int)((uint)data.serverId - 1));
            if (_authority?.Executor != null)
                _authority.Executor.TryApplyCanonicalResonance(
                    grade,
                    _authority.GetResonanceLevel(playerSlot, CanonicalResonanceTrack.NORMAL),
                    _authority.GetResonanceLevel(playerSlot, CanonicalResonanceTrack.MYTHIC),
                    snapshot,
                    out snapshot);
            if (!string.IsNullOrWhiteSpace(data.activeMutationType)
                && _authority?.Executor != null
                && _authority.Executor.TryGetCanonicalMutationSpec(data.activeMutationType, out var mutationSpec))
                snapshot = MutationAttackSnapshotCalculator.Apply(snapshot, mutationSpec);
            attack.ApplyAttackSnapshot(snapshot);
            return true;
        }

        private static bool IsFinitePositive(float value)
            => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        [Serializable]
        public sealed class BattleAttackSnapshotCatalogJson
        {
            public string playerId;
            public string balanceVersion;
            public string contentHash;
            public BattleAlienAttackSnapshotJson[] aliens;
        }

        [Serializable]
        public sealed class BattleAlienAttackSnapshotJson
        {
            public long alienId;
            public int level;
            public float damage;
            public float attackRate;
            public float range;
        }
    }
}
