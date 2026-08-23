using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattlePlayerIdentityCallbacks : INetworkRunnerCallbacks
    {
        private readonly BattlePlayerRoster _roster;
        private readonly string _localUserId;
        private readonly Action<BattlePlayerIdentity> _playerJoined;
        private readonly Action<BattlePlayerIdentity> _playerLeft;
        private readonly HashSet<string> _pendingUserIds = new(StringComparer.Ordinal);

        public BattlePlayerIdentityCallbacks(
            BattlePlayerRoster roster,
            string localUserId,
            Action<BattlePlayerIdentity> playerJoined = null,
            Action<BattlePlayerIdentity> playerLeft = null)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _localUserId = string.IsNullOrWhiteSpace(localUserId) ? null : localUserId.Trim();
            _playerJoined = playerJoined;
            _playerLeft = playerLeft;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            string userId = null;
            if (player == runner.LocalPlayer)
                userId = _localUserId;
            if (userId == null && runner.IsServer)
                BattlePlayerIdentityToken.TryDecode(runner.GetPlayerConnectionToken(player), out userId);
            if (userId != null)
            {
                _pendingUserIds.Remove(userId);
                if (_roster.TryAdd(player, userId, out BattlePlayerIdentity identity))
                    _playerJoined?.Invoke(identity);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_roster.TryGet(player, out BattlePlayerIdentity identity))
            {
                _roster.Disconnect(player);
                _playerLeft?.Invoke(identity);
            }
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            if (!runner.IsServer || !BattlePlayerIdentityToken.TryDecode(token, out string userId))
            {
                request.Refuse();
                return;
            }
            if (!_roster.CanAddUser(userId)
                || _pendingUserIds.Contains(userId)
                || _roster.Count + _pendingUserIds.Count >= 2)
                request.Refuse();
            else
            {
                _pendingUserIds.Add(userId);
                request.Accept();
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) => _pendingUserIds.Clear();
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => _pendingUserIds.Clear();
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => _pendingUserIds.Clear();
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
