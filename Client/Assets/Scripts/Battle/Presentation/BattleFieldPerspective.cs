using System;
using UnityEngine;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Applies the local player's perspective to the two battle boards.
    /// The local board keeps its authored orientation; the remote board is
    /// rotated 180 degrees around Y so it faces the local player.
    /// </summary>
    public sealed class BattleFieldPerspective : MonoBehaviour
    {
        [SerializeField] private Transform _player1Field;
        [SerializeField] private Transform _player2Field;
        [SerializeField] private BattleRunnerLifecycle _runnerLifecycle;
        [SerializeField] private BattleWaveStateAuthority _stateAuthority;

        private Quaternion _player1BaseRotation;
        private Quaternion _player2BaseRotation;
        private Vector3 _player1BasePosition;
        private Vector3 _player2BasePosition;
        private bool _capturedBaseRotations;

        public int LocalPlayerSlot { get; private set; }
        public bool IsApplied { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseRotations();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (_runnerLifecycle != null)
                _runnerLifecycle.PlayerRoster.PlayersChanged += ApplyFromRoster;
            ApplyFromRoster();
        }

        private void OnDisable()
        {
            if (_runnerLifecycle != null)
                _runnerLifecycle.PlayerRoster.PlayersChanged -= ApplyFromRoster;
            RestoreBaseRotations();
            IsApplied = false;
            LocalPlayerSlot = 0;
        }

        private void Update()
        {
            if (_runnerLifecycle == null
                || _runnerLifecycle.Runner == null
                || _stateAuthority == null
                || _stateAuthority.Object == null
                || !_stateAuthority.Object.IsValid)
                return;

            // The client roster initially contains only its local player. Once
            // the authoritative PlayerRef-to-slot mapping arrives through
            // replication, re-apply the presentation for the real slot.
            int networkedSlot = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (networkedSlot != 0 && networkedSlot != LocalPlayerSlot)
                ApplySlot(networkedSlot);
        }

        private void ResolveReferences()
        {
            if (_player1Field == null)
                _player1Field = FindTransform("GridManager");
            if (_player2Field == null)
                _player2Field = FindTransform("EnemyGridParent");
            if (_runnerLifecycle == null)
                _runnerLifecycle = FindFirstObjectByType<BattleRunnerLifecycle>();
            if (_stateAuthority == null)
                _stateAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
        }

        private Transform FindTransform(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            return target != null ? target.transform : null;
        }

        private void CaptureBaseRotations()
        {
            if (_capturedBaseRotations)
                return;
            if (_player1Field == null || _player2Field == null)
                return;
            _player1BaseRotation = _player1Field.localRotation;
            _player2BaseRotation = _player2Field.localRotation;
            _player1BasePosition = _player1Field.localPosition;
            _player2BasePosition = _player2Field.localPosition;
            _capturedBaseRotations = true;
        }

        private void ApplyFromRoster()
        {
            CaptureBaseRotations();

            if (_runnerLifecycle == null || _runnerLifecycle.Runner == null)
                return;
            int localPlayerSlot = _stateAuthority != null
                && _stateAuthority.Object != null
                && _stateAuthority.Object.IsValid
                ? _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer)
                : 0;
            if (localPlayerSlot == 0
                && _runnerLifecycle.PlayerRoster.TryGet(
                    _runnerLifecycle.Runner.LocalPlayer,
                    out BattlePlayerIdentity localIdentity))
                localPlayerSlot = localIdentity.PlayerSlot;
            if (localPlayerSlot != 1 && localPlayerSlot != 2)
                return;

            ApplySlot(localPlayerSlot);
        }

        private void ApplySlot(int localPlayerSlot)
        {
            LocalPlayerSlot = localPlayerSlot;
            Transform localField = localPlayerSlot == 1 ? _player1Field : _player2Field;
            Transform remoteField = localPlayerSlot == 1 ? _player2Field : _player1Field;
            Quaternion localBase = localPlayerSlot == 1 ? _player1BaseRotation : _player2BaseRotation;
            Quaternion remoteBase = localPlayerSlot == 1 ? _player2BaseRotation : _player1BaseRotation;
            // The local player's field is always presented in the lower lane;
            // the remote field remains in the upper lane on every client.
            Vector3 localPosition = _player1BasePosition;
            Vector3 remotePosition = _player2BasePosition;

            if (localField == null || remoteField == null)
                return;

            // Keep the local field at the authored lower position and the
            // remote field at the authored upper position. The field objects
            // themselves still carry their P1/P2 identity for synchronized
            // kidnap placement.
            localField.localPosition = localPosition;
            remoteField.localPosition = remotePosition;
            localField.localRotation = localBase;
            remoteField.localRotation = remoteBase * Quaternion.Euler(0f, 180f, 0f);
            IsApplied = true;
            Debug.LogFormat("[BattleFieldPerspective] Applied local slot {0}: remote field mirrored.", LocalPlayerSlot);
        }

        private void RestoreBaseRotations()
        {
            if (!_capturedBaseRotations)
                return;
            if (_player1Field != null)
            {
                _player1Field.localPosition = _player1BasePosition;
                _player1Field.localRotation = _player1BaseRotation;
            }
            if (_player2Field != null)
            {
                _player2Field.localPosition = _player2BasePosition;
                _player2Field.localRotation = _player2BaseRotation;
            }
        }
    }
}
