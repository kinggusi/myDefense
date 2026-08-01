using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle
{
    public class BattleWaveHudView : MonoBehaviour
    {
        [SerializeField] private BattleWaveExecutor _waveExecutor;
        [SerializeField] private BattleWaveStateAuthority _stateAuthority;
        [SerializeField] private Text _waveText;
        [SerializeField] private TMP_Text _player1MonsterCountText;
        [SerializeField] private TMP_Text _player2MonsterCountText;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _warningColor = new Color(1f, 0.84f, 0.31f, 1f);
        [SerializeField] private Color _dangerColor = new Color(1f, 0.54f, 0.24f, 1f);
        [SerializeField] private Color _eliminatedColor = new Color(1f, 0.30f, 0.35f, 1f);

        private BattleWaveExecutor _subscribedExecutor;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            Subscribe(_waveExecutor);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe(BattleWaveExecutor executor)
        {
            Unsubscribe();
            if (executor == null) return;

            _subscribedExecutor = executor;
            _subscribedExecutor.OnRoundChanged += UpdateWaveText;
            _subscribedExecutor.OnPlayerMonsterCountChanged += UpdatePlayerMonsterCount;
            _subscribedExecutor.OnPlayerBattleStateChanged += UpdatePlayerBattleState;
            _subscribedExecutor.OnMatchStateChanged += UpdateMatchState;
            RefreshAll();
        }

        private void Unsubscribe()
        {
            if (_subscribedExecutor == null) return;

            _subscribedExecutor.OnRoundChanged -= UpdateWaveText;
            _subscribedExecutor.OnPlayerMonsterCountChanged -= UpdatePlayerMonsterCount;
            _subscribedExecutor.OnPlayerBattleStateChanged -= UpdatePlayerBattleState;
            _subscribedExecutor.OnMatchStateChanged -= UpdateMatchState;
            _subscribedExecutor = null;
        }

        private void RefreshAll()
        {
            if (_subscribedExecutor == null) return;

            UpdateWaveText(_subscribedExecutor.CurrentRound);
            UpdatePlayerDisplay(
                LaneType.Player1Lane,
                _subscribedExecutor.Player1AliveMonsterCount,
                _subscribedExecutor.Player1BattleState);
            UpdatePlayerDisplay(
                LaneType.Player2Lane,
                _subscribedExecutor.Player2AliveMonsterCount,
                _subscribedExecutor.Player2BattleState);
        }

        private void UpdatePlayerMonsterCount(LaneType lane, int count, int limit)
        {
            UpdatePlayerDisplay(lane, count, GetPlayerState(lane), limit);
        }

        private void Update()
        {
            ResolveReferences();
            if (_stateAuthority == null || !_stateAuthority.IsSpawnedForAccess)
                return;

            // The executor is authoritative only on the host. Every peer must
            // render the replicated counts so P2 does not drift from the host.
            UpdateWaveText(_stateAuthority.CurrentWave);
            UpdatePlayerDisplay(
                LaneType.Player1Lane,
                _stateAuthority.Player1AliveMonsterCount,
                _stateAuthority.Player1BattleState,
                _stateAuthority.PlayerMonsterLimit);
            UpdatePlayerDisplay(
                LaneType.Player2Lane,
                _stateAuthority.Player2AliveMonsterCount,
                _stateAuthority.Player2BattleState,
                _stateAuthority.PlayerMonsterLimit);
        }

        private void ResolveReferences()
        {
            if (_waveExecutor == null)
                _waveExecutor = BattleWaveExecutor.Instance;
            if (_stateAuthority == null)
                _stateAuthority = FindFirstObjectByType<BattleWaveStateAuthority>();
        }

        private void UpdatePlayerBattleState(LaneType lane, PlayerBattleState state)
        {
            UpdatePlayerDisplay(lane, GetPlayerCount(lane), state);
        }

        private void UpdateMatchState(MatchState _)
        {
            RefreshAll();
        }

        private int GetPlayerCount(LaneType lane)
        {
            if (_subscribedExecutor == null) return 0;

            return lane == LaneType.Player2Lane
                ? _subscribedExecutor.Player2AliveMonsterCount
                : _subscribedExecutor.Player1AliveMonsterCount;
        }

        private PlayerBattleState GetPlayerState(LaneType lane)
        {
            if (_subscribedExecutor == null) return PlayerBattleState.ACTIVE;

            return lane == LaneType.Player2Lane
                ? _subscribedExecutor.Player2BattleState
                : _subscribedExecutor.Player1BattleState;
        }

        private void UpdatePlayerDisplay(
            LaneType lane,
            int count,
            PlayerBattleState state,
            int limit = -1)
        {
            if (lane == LaneType.BossSharedLane) return;

            TMP_Text target = lane == LaneType.Player1Lane
                ? _player1MonsterCountText
                : _player2MonsterCountText;
            if (target == null) return;

            if (limit < 0)
            {
                if (_subscribedExecutor == null) return;
                limit = _subscribedExecutor.MonsterLimit;
            }

            bool eliminated = state == PlayerBattleState.ELIMINATED || count >= limit;
            string playerLabel = lane == LaneType.Player1Lane ? "P1" : "P2";
            target.text = eliminated
                ? $"{playerLabel} ELIMINATED \u00B7 {count} / {limit}"
                : $"{playerLabel} {count} / {limit}";
            target.color = GetCountColor(count, limit, eliminated);
        }

        private Color GetCountColor(int count, int limit, bool eliminated)
        {
            if (eliminated || count >= limit) return _eliminatedColor;
            if (_subscribedExecutor != null && count >= _subscribedExecutor.MonsterDangerThreshold) return _dangerColor;
            if (_subscribedExecutor != null && count >= _subscribedExecutor.MonsterWarningThreshold) return _warningColor;
            return _normalColor;
        }

        private void UpdateWaveText(int round)
        {
            if (_waveText != null)
            {
                _waveText.text = $"WAVE {round}";
            }
        }
    }
}
