using UnityEngine;
using MyDefense.Shared.Contracts;
using MyDefense.Battle.Runtime;

namespace MyDefense.Battle.Presentation
{
    /// <summary>
    /// Switches the local camera to the surviving player's field after local
    /// elimination. The state authority remains the source of truth; this
    /// component only changes presentation and never mutates battle state.
    /// </summary>
    public sealed class BattleSpectatorCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private BattleWaveStateAuthority _stateAuthority;
        [SerializeField] private BattleRunnerLifecycle _runnerLifecycle;
        [SerializeField] private Transform _player1Field;
        [SerializeField] private Transform _player2Field;
        [SerializeField] private Vector3 _spectatorOffset = new Vector3(0f, 12f, -10f);
        [SerializeField] private float _transitionSeconds = 0.35f;

        private Vector3 _normalPosition;
        private Quaternion _normalRotation;
        private bool _capturedNormalTransform;
        private bool _isSpectating;
        private Transform _spectatorTarget;

        public bool IsSpectating => _isSpectating;
        public int SpectatorTargetSlot { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            CaptureNormalTransform();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureNormalTransform();
        }

        private void Start()
        {
            ResolveReferences();
            CaptureNormalTransform();
        }

        private void OnDisable()
        {
            RestoreNormalCamera();
        }

        private void Update()
        {
            if (_camera == null || _stateAuthority == null || _runnerLifecycle?.Runner == null)
                return;

            // Fusion Networked properties are only valid after the scene object
            // has been spawned by the runner. Keep presentation polling inert
            // during scene load and before network attachment.
            if (!_stateAuthority.IsSpawnedForAccess)
                return;

            int localSlot = _stateAuthority.GetNetworkedPlayerSlot(_runnerLifecycle.Runner.LocalPlayer);
            if (localSlot != 1 && localSlot != 2)
                return;

            PlayerBattleState state = localSlot == 1
                ? _stateAuthority.Player1BattleState
                : _stateAuthority.Player2BattleState;
            bool shouldSpectate = state == PlayerBattleState.ELIMINATED
                || state == PlayerBattleState.SPECTATING;
            Transform target = localSlot == 1 ? _player2Field : _player1Field;

            if (shouldSpectate)
            {
                if (!_isSpectating || _spectatorTarget != target)
                    EnterSpectatorMode(target, localSlot == 1 ? 2 : 1);
                return;
            }

            if (_isSpectating)
                RestoreNormalCamera();
        }

        private void ResolveReferences()
        {
            _camera ??= GetComponent<Camera>();
            _camera ??= Camera.main;
            _stateAuthority ??= FindFirstObjectByType<BattleWaveStateAuthority>();
            _runnerLifecycle ??= FindFirstObjectByType<BattleRunnerLifecycle>();
            if (_player1Field == null || _player2Field == null)
            {
                GridManager[] grids = FindObjectsByType<GridManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int index = 0; index < grids.Length; index++)
                {
                    if (_player1Field == null && grids[index].name == "GridManager")
                        _player1Field = grids[index].transform;
                    else if (_player2Field == null && grids[index].name == "EnemyGridParent")
                        _player2Field = grids[index].transform;
                }
            }
        }

        private void CaptureNormalTransform()
        {
            if (_capturedNormalTransform || _camera == null)
                return;

            _normalPosition = _camera.transform.position;
            _normalRotation = _camera.transform.rotation;
            _capturedNormalTransform = true;
        }

        private void EnterSpectatorMode(Transform target, int targetSlot)
        {
            if (target == null)
                return;

            CaptureNormalTransform();
            _isSpectating = true;
            _spectatorTarget = target;
            SpectatorTargetSlot = targetSlot;
            Vector3 targetPosition = target.position;
            targetPosition.x += _spectatorOffset.x;
            targetPosition.y += _spectatorOffset.y;
            targetPosition.z += _spectatorOffset.z;
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPosition,
                _transitionSeconds <= 0f ? 1f : Mathf.Clamp01(Time.deltaTime / _transitionSeconds));
            _camera.transform.LookAt(target.position);
            Debug.LogFormat(
                "[BattleSpectator] Local player eliminated; spectating player {0} field.",
                targetSlot);
        }

        private void RestoreNormalCamera()
        {
            if (_camera != null && _capturedNormalTransform)
            {
                _camera.transform.position = _normalPosition;
                _camera.transform.rotation = _normalRotation;
            }

            _isSpectating = false;
            _spectatorTarget = null;
            SpectatorTargetSlot = 0;
        }

        private void OnGUI()
        {
            if (!_isSpectating)
                return;

            GUIStyle style = GUI.skin.GetStyle("box");
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = Mathf.Max(16, Screen.height / 45);
            GUI.Box(
                new Rect(20f, 20f, 300f, 48f),
                SpectatorTargetSlot == 1 ? "SPECTATING · P1" : "SPECTATING · P2",
                style);
        }
    }
}
