using Fusion;
using UnityEngine;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Development visualization for the authoritative Kidnap board state.
    /// It intentionally uses primitive geometry until the networked Alien
    /// object/prefab path is introduced by the next Battle task.
    /// </summary>
    public sealed class FusionKidnapBoardView : MonoBehaviour
    {
        private BattleWaveStateAuthority _authority;
        private Transform _player1Grid;
        private Transform _player2Grid;

        private void OnEnable()
        {
            _authority = FindFirstObjectByType<BattleWaveStateAuthority>();
            if (_authority == null) return;
            _player1Grid = GameObject.Find("GridManager")?.transform;
            _player2Grid = GameObject.Find("EnemyGridParent")?.transform;
            _authority.KidnapApplied += ApplyKidnap;
            _authority.BoardChanged += ApplyBoardChange;
        }

        private void OnDisable()
        {
            if (_authority != null)
            {
                _authority.KidnapApplied -= ApplyKidnap;
                _authority.BoardChanged -= ApplyBoardChange;
            }
        }

        private void Update()
        {
            if (_authority == null || !_authority.Object || !_authority.Object.IsValid)
                return;

            // Reconcile from the replicated occupancy snapshot so late joins,
            // scene reloads, and reconnects do not depend on past RPC events.
            for (int slotIndex = 0; slotIndex < 24; slotIndex++)
            {
                ReconcileSlot(1, slotIndex);
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
                ApplyKidnap(playerSlot, slotIndex);
            else if (!occupied && unit != null)
                Object.Destroy(unit.gameObject);
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

        private void ApplyKidnap(int playerSlot, int slotIndex)
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

            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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
            FusionKidnapUnitDrag drag = unit.AddComponent<FusionKidnapUnitDrag>();
            drag.Initialize(this, _authority, playerSlot, slotIndex);
            Debug.Log($"[FusionKidnapBoardView] slot={playerSlot} grid={grid.name} tile={tile.name} row={row} column={column} mirrored={isMirroredField} index={slotIndex}.");
        }

        public Transform GetGrid(int playerSlot) => playerSlot == 1 ? _player1Grid : _player2Grid;

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

        private void ApplyBoardChange(int playerSlot, int fromSlotIndex, int toSlotIndex, bool merged)
        {
            Transform grid = playerSlot == 1 ? _player1Grid : _player2Grid;
            if (grid == null) return;
            Transform sourceTile = ResolveTile(grid, fromSlotIndex);
            Transform targetTile = ResolveTile(grid, toSlotIndex);
            if (sourceTile == null || targetTile == null) return;
            Transform sourceUnit = sourceTile.Find("FusionKidnapUnit");
            if (sourceUnit == null) return;

            if (merged)
            {
                Object.Destroy(sourceUnit.gameObject);
                Debug.Log($"[FusionKidnapBoardView] merge slot={playerSlot} source={fromSlotIndex} target={toSlotIndex}.");
                return;
            }

            if (targetTile.Find("FusionKidnapUnit") != null) return;
            ApplyUnitTransform(sourceUnit, targetTile);
            FusionKidnapUnitDrag drag = sourceUnit.GetComponent<FusionKidnapUnitDrag>();
            if (drag != null)
                drag.UpdateSlotIndex(toSlotIndex);
            Debug.Log($"[FusionKidnapBoardView] move slot={playerSlot} source={fromSlotIndex} target={toSlotIndex}.");
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
    }
}
