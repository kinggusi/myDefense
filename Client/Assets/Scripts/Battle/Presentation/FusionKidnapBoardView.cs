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
        }

        private void OnDisable()
        {
            if (_authority != null) _authority.KidnapApplied -= ApplyKidnap;
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
            Transform tile = grid.Find($"Tile_{authoredColumn}_{authoredRow}");
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
            Debug.Log($"[FusionKidnapBoardView] slot={playerSlot} grid={grid.name} tile={tile.name} row={row} column={column} mirrored={isMirroredField} index={slotIndex}.");
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }
    }
}
