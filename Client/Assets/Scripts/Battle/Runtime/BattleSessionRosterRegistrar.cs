using System;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    public interface IBattleSessionRosterRegistration
    {
        bool IsRegistered { get; }
        bool IsRequestInFlight { get; }
        string LastError { get; }
        event Action Registered;
        void Configure(BattleSessionContext session, IBattlePlayerIdentityProvider identities);
        void EnsureRegistered();
        bool RetryRegistration();
    }

    public static class BattleSessionRosterRegistrationFactory
    {
        /// <summary>
        /// FUTURE_AUTH_REPLACEMENT: a JWT/matchmaking bootstrap registers its
        /// own IBattleSessionRosterRegistration component before this call.
        /// The factory always prefers that implementation over local fallback.
        /// </summary>
        public static IBattleSessionRosterRegistration ResolveOrCreate(
            GameObject host,
            string environmentName = null)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            foreach (MonoBehaviour component in host.GetComponents<MonoBehaviour>())
            {
                if (component is IBattleSessionRosterRegistration registration)
                    return registration;
            }

            string environment = environmentName ?? RuntimeEnvironmentConfig.EnvironmentName;
            return BattleSessionRosterRegistrar.IsLocalOrDev(environment)
                ? host.AddComponent<BattleSessionRosterRegistrar>()
                : host.AddComponent<MissingAuthenticatedRosterRegistrar>();
        }
    }

    /// <summary>
    /// Local/dev-only bridge from the Fusion State Authority roster to Spring.
    ///
    /// FUTURE_AUTH_REPLACEMENT: replace this component with a registrar backed
    /// by verified JWT principals and production matchmaking. Settlement only
    /// depends on IBattleSessionRosterRegistration, so its payload and reward
    /// transaction do not change when authentication is introduced.
    /// </summary>
    public sealed class BattleSessionRosterRegistrar : MonoBehaviour, IBattleSessionRosterRegistration
    {
        private const string LocalRegistrationPath = "/dev/battle/session-rosters";
        private BattleSessionRosterRegisterRequest _request;

        public bool IsRegistered { get; private set; }
        public bool IsRequestInFlight { get; private set; }
        public string LastError { get; private set; }
        public event Action Registered;

        public void Configure(BattleSessionContext session, IBattlePlayerIdentityProvider identities)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (identities == null) throw new ArgumentNullException(nameof(identities));
            if (!identities.TryGetPlayerId(LaneType.Player1Lane, out string playerOne)
                || !identities.TryGetPlayerId(LaneType.Player2Lane, out string playerTwo))
                throw new InvalidOperationException("Both Fusion player identities are required for roster registration.");

            _request = BuildRequest(session, playerOne, playerTwo);
            IsRegistered = false;
            IsRequestInFlight = false;
            LastError = null;
        }

        public void EnsureRegistered()
        {
            if (IsRegistered || IsRequestInFlight || _request == null)
                return;
            if (!IsLocalOrDev(RuntimeEnvironmentConfig.EnvironmentName))
            {
                LastError = "Production JWT matchmaking roster adapter is not configured.";
                Debug.LogError("[BattleRoster] FUTURE_AUTH_REPLACEMENT: " + LastError);
                return;
            }
            if (NetworkManager.Instance == null)
            {
                LastError = "NetworkManager is not available for roster registration.";
                Debug.LogError("[BattleRoster] " + LastError);
                return;
            }

            IsRequestInFlight = true;
            NetworkManager.Instance.PostJson(
                LocalRegistrationPath,
                JsonUtility.ToJson(_request),
                HandleSuccess,
                error =>
                {
                    IsRequestInFlight = false;
                    LastError = error;
                    Debug.LogError("[BattleRoster] registration failed: " + error);
                });
        }

        public bool RetryRegistration()
        {
            if (IsRegistered || IsRequestInFlight || _request == null)
                return false;
            LastError = null;
            EnsureRegistered();
            return IsRequestInFlight;
        }

        public static BattleSessionRosterRegisterRequest BuildRequest(
            BattleSessionContext session,
            string playerOne,
            string playerTwo)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            playerOne = BattleSessionContext.RequireText(playerOne, nameof(playerOne));
            playerTwo = BattleSessionContext.RequireText(playerTwo, nameof(playerTwo));
            if (string.Equals(playerOne, playerTwo, StringComparison.Ordinal))
                throw new ArgumentException("Roster player identities must be distinct.", nameof(playerTwo));

            return new BattleSessionRosterRegisterRequest
            {
                battleSessionId = session.BattleSessionId,
                mapId = string.IsNullOrWhiteSpace(session.MapId) ? BattleRunnerLifecycle.DefaultMapId : session.MapId,
                balanceVersion = session.CanonicalBalanceVersion,
                contentHash = session.CanonicalContentHash,
                players = new[]
                {
                    new BattleSessionRosterPlayer { playerSlot = 1, playerId = playerOne },
                    new BattleSessionRosterPlayer { playerSlot = 2, playerId = playerTwo }
                }
            };
        }

        public static bool IsLocalOrDev(string environmentName)
            => string.Equals(environmentName, "local", StringComparison.OrdinalIgnoreCase)
               || string.Equals(environmentName, "dev", StringComparison.OrdinalIgnoreCase);

        private void HandleSuccess(string json)
        {
            try
            {
                BattleSessionRosterRegisterResponse response = JsonUtility.FromJson<BattleSessionRosterRegisterResponse>(json);
                if (response == null
                    || !string.Equals(response.battleSessionId, _request.battleSessionId, StringComparison.Ordinal)
                    || !string.Equals(response.status, "REGISTERED", StringComparison.Ordinal)
                    || response.playerCount != 2)
                    throw new InvalidOperationException("Roster registration response is invalid.");

                IsRequestInFlight = false;
                IsRegistered = true;
                LastError = null;
                Debug.Log("[BattleRoster] registered trusted local roster for session=" + response.battleSessionId);
                Registered?.Invoke();
            }
            catch (Exception exception)
            {
                IsRequestInFlight = false;
                LastError = exception.Message;
                Debug.LogError("[BattleRoster] invalid registration response: " + exception.Message);
            }
        }
    }

    /// <summary>
    /// Fail-closed production placeholder. It keeps Wave and Settlement
    /// blocked until a JWT/matchmaking registrar is supplied by production
    /// bootstrap code.
    /// </summary>
    public sealed class MissingAuthenticatedRosterRegistrar : MonoBehaviour, IBattleSessionRosterRegistration
    {
        public bool IsRegistered => false;
        public bool IsRequestInFlight => false;
        public string LastError { get; private set; }
        public event Action Registered { add { } remove { } }

        public void Configure(BattleSessionContext session, IBattlePlayerIdentityProvider identities)
        {
            LastError = "JWT matchmaking roster adapter is required in this environment.";
        }

        public void EnsureRegistered()
        {
            Debug.LogError("[BattleRoster] FUTURE_AUTH_REPLACEMENT: " + LastError);
        }

        public bool RetryRegistration()
        {
            EnsureRegistered();
            return false;
        }
    }
}
