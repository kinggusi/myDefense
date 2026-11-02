using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyDefense.Auth
{
    [Serializable] public sealed class AuthUser { public long userId; public string username; public bool isGuest; }
    [Serializable] public sealed class AuthTokens
    {
        public string accessToken, refreshToken, tokenType;
        public long expiresIn, refreshExpiresIn;
        public AuthUser user;
    }
    [Serializable] public sealed class GuestRequest { public string guestSecret; }
    [Serializable] public sealed class DevelopmentLoginRequest { public string username, password; }
    [Serializable] public sealed class RefreshRequest { public string refreshToken; }
    [Serializable] public sealed class ProviderInfo { public string provider, reason; public bool available; }
    [Serializable] public sealed class ProvidersResponse { public ProviderInfo[] providers; }
    [Serializable] public sealed class ChallengeRequest { public string provider, purpose; }
    [Serializable] public sealed class AuthChallenge { public string challengeId, nonce; public long expiresIn; }
    [Serializable] public sealed class ExternalAuthRequest { public string provider, idToken, challengeId; }
    [Serializable] public sealed class StoredCredential
    {
        public int version = 1;
        public long userId;
        public string apiBaseUrl, guestSecret, refreshToken;
    }
    public interface ISecureCredentialStore : IDisposable
    {
        StoredCredential Load();
        void Save(StoredCredential credential);
    }
    // FUTURE_AUTH_REPLACEMENT: native Google/Apple SDK adapters return proof, never a trusted user ID.
    public interface IExternalIdentityProvider
    {
        string Provider { get; }
        bool IsAvailable { get; }
        IEnumerator GetIdToken(string nonce, Action<string, string> completed);
    }
    public static class ExternalIdentityProviders
    {
        private static readonly Dictionary<string, IExternalIdentityProvider> Providers = new Dictionary<string, IExternalIdentityProvider>();
        public static void Register(IExternalIdentityProvider provider) { Providers[provider.Provider] = provider; }
        public static IExternalIdentityProvider Find(string name) { Providers.TryGetValue(name, out var value); return value; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Providers.Clear(); }
    }
    public sealed class AuthHttpResult
    {
        public long Status;
        public string Body, Error;
        public bool Success => Status >= 200 && Status < 300 && string.IsNullOrEmpty(Error);
    }
}
