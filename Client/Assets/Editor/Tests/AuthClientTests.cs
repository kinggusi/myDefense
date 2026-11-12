using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MyDefense.Auth;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AuthClientTests
{
    [Test] public void RejectedPostIsNotReplayedAndExplicitRetryRefreshesSamePayload()
    {
        using (var fixture = new SessionFixture())
        {
            var calls = new List<string>();
            const string payload = "{\"requestId\":\"same-purchase\"}";
            fixture.Transport((path, method, json, bearer) =>
            {
                calls.Add(path + "|" + method + "|" + bearer + "|" + json);
                if (path == "/auth/refresh") return fixture.Tokens("renewed");
                Assert.AreEqual(payload, json);
                return new AuthHttpResult { Status = bearer == "initial" ? 401 : 200, Body = "{}" };
            });
            AuthHttpResult response = null;
            Drain(fixture.Session.SendAccount("/shop/purchases", "POST", payload, null, r => response = r));
            Assert.AreEqual(401, response.Status);
            Assert.AreEqual(1, calls.Count, "Spending POST must never be automatically replayed.");
            Drain(fixture.Session.SendAccount("/shop/purchases", "POST", payload, null, r => response = r));
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(3, calls.Count);
            Assert.That(calls[1], Does.StartWith("/auth/refresh|POST|"));
            Assert.That(calls[2], Does.Contain("|renewed|" + payload));
        }
    }

    [Test] public void LateRejectionOfOldTokenDoesNotInvalidateNewToken()
    {
        using (var fixture = new SessionFixture())
        {
            fixture.Transport((path, method, json, bearer) =>
            {
                fixture.Set("accessToken", "already-refreshed");
                return new AuthHttpResult { Status = 401 };
            });
            Drain(fixture.Session.SendAccount("/shop/purchases", "POST", "{}", null, _ => { }));
            Assert.Greater((double)fixture.Get("expiresAt"), Time.realtimeSinceStartupAsDouble);
        }
    }

    [Test] public void RejectedGetRefreshesOnceAndDoesNotLoop()
    {
        using (var fixture = new SessionFixture())
        {
            int reads = 0, refreshes = 0;
            fixture.Transport((path, method, json, bearer) =>
            {
                if (path == "/auth/refresh") { refreshes++; return fixture.Tokens("renewed"); }
                reads++; return new AuthHttpResult { Status = 401 };
            });
            AuthHttpResult response = null;
            Drain(fixture.Session.SendAccount("/shop/catalog", "GET", null, null, r => response = r));
            Assert.AreEqual(401, response.Status); Assert.AreEqual(2, reads); Assert.AreEqual(1, refreshes);
            Assert.AreEqual(0d, fixture.Get("expiresAt"));
        }
    }

    [Test] public void SessionReplacementDuringRefreshPreventsAccountPostAndCredentialWrite()
    {
        using (var fixture = new SessionFixture())
        {
            fixture.Set("expiresAt", 0d);
            int sends = 0;
            fixture.Transport((path, method, json, bearer) =>
            {
                sends++; Assert.AreEqual("/auth/refresh", path);
                typeof(AuthSession).GetProperty("Instance").SetValue(null, null);
                return fixture.Tokens("stale-response");
            });
            AuthHttpResult response = null;
            Drain(fixture.Session.SendAccount("/shop/purchases", "POST", "{}", null, r => response = r));
            Assert.AreEqual(409, response.Status); Assert.AreEqual(1, sends);
            Assert.AreEqual("initial", fixture.Get("accessToken"));
        }
    }

    [Test] public void SessionReplacementDuringGuestRecoveryCannotSaveStaleCredentials()
    {
        using (var fixture = new SessionFixture())
        {
            fixture.Set("expiresAt", 0d);
            fixture.Set("credential", new StoredCredential { apiBaseUrl = fixture.Endpoint, userId = 7, refreshToken = "rejected", guestSecret = "existing-secret" });
            int sends = 0;
            fixture.Transport((path, method, json, bearer) =>
            {
                sends++;
                if (path == "/auth/refresh") return new AuthHttpResult { Status = 401 };
                Assert.AreEqual("/auth/guest", path);
                typeof(AuthSession).GetProperty("Instance").SetValue(null, null);
                return fixture.Tokens("stale-guest-response");
            });
            AuthHttpResult response = null;
            Drain(fixture.Session.SendAccount("/shop/purchases", "POST", "{}", null, r => response = r));
            Assert.AreEqual(409, response.Status); Assert.AreEqual(2, sends);
            Assert.AreEqual("initial", fixture.Get("accessToken"));
            Assert.AreEqual("rejected", ((StoredCredential)fixture.Get("credential")).refreshToken);
        }
    }

    [Test] public void SessionReplacementDuringDevelopmentLoginCannotSaveResponse()
    {
        string previousEnvironment = Environment.GetEnvironmentVariable("MYDEFENSE_ENV");
        string previousSelection = DevelopmentLoginPolicy.SelectedAccount;
        try
        {
            Environment.SetEnvironmentVariable("MYDEFENSE_ENV", "local");
            SessionState.SetString(DevelopmentLoginPolicy.SelectionKey, "jjangash");
            using (var fixture = new SessionFixture())
            {
                typeof(AuthSession).GetProperty("DevelopmentAccount").SetValue(fixture.Session, "jjangash");
                typeof(AuthSession).GetProperty("User").SetValue(fixture.Session, null);
                fixture.Set("accessToken", null);
                fixture.Transport((path, method, json, bearer) =>
                {
                    Assert.AreEqual("/dev/auth/login", path);
                    typeof(AuthSession).GetProperty("Instance").SetValue(null, null);
                    return new AuthHttpResult { Status = 200, Body = JsonUtility.ToJson(new AuthTokens {
                        tokenType = "Bearer", accessToken = "stale-development", refreshToken = "stale-refresh", expiresIn = 3600,
                        user = new AuthUser { userId = 7, username = "jjangash" } }) };
                });
                bool? accepted = null;
                Drain(fixture.Session.SignInDevelopment("fixture-only", ok => accepted = ok));
                Assert.AreEqual(false, accepted); Assert.IsNull(fixture.Session.User);
                Assert.AreEqual("refresh", ((StoredCredential)fixture.Get("credential")).refreshToken);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("MYDEFENSE_ENV", previousEnvironment);
            SessionState.SetString(DevelopmentLoginPolicy.SelectionKey, previousSelection);
        }
    }

    [Test] public void AccountScopeRejectsGenerationEndpointAndSameIdSessionReplacement()
    {
        using (var fixture = new SessionFixture())
        {
            var scope = new AuthAccountScope(fixture.Session, fixture.Endpoint + "/");
            Assert.IsTrue(scope.IsCurrent(fixture.Endpoint));
            fixture.Set("accessToken", "normal-refresh");
            Assert.IsTrue(scope.IsCurrent(fixture.Endpoint), "Normal token rotation keeps the account scope.");
            Assert.IsFalse(scope.IsCurrent("http://127.0.0.1:18085/api"));
            typeof(AuthSession).GetProperty("Generation").SetValue(fixture.Session, 1);
            Assert.IsFalse(scope.IsCurrent(fixture.Endpoint));
            typeof(AuthSession).GetProperty("Generation").SetValue(fixture.Session, 0);
            using (var replacement = new SessionFixture())
                Assert.IsFalse(scope.IsCurrent(fixture.Endpoint), "Matching numeric userId cannot authorize a replacement session.");
        }
    }

    [Test] public void StaleShopAndBreedingControllersCannotDispatchOrClearPendingIntent()
    {
        var previousNetwork = NetworkManager.Instance;
        NetworkManager.Instance = null;
        var networkRoot = new GameObject("IsolatedScopeNetwork");
        var shopRoot = new GameObject("IsolatedScopeShop");
        var breedingRoot = new GameObject("IsolatedScopeBreeding");
        try
        {
            var network = networkRoot.AddComponent<NetworkManager>();
            using (var fixture = new SessionFixture())
            {
                network.BaseUrl = fixture.Endpoint;
                var scope = new AuthAccountScope(fixture.Session, fixture.Endpoint);
                var shop = shopRoot.AddComponent<ShopCatalogController>();
                var breeding = breedingRoot.AddComponent<MythicBreedingController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(ShopCatalogController).GetField("accountScope", flags).SetValue(shop, scope);
                typeof(MythicBreedingController).GetField("accountScope", flags).SetValue(breeding, scope);
                var pending = new MyDefense.Shop.ShopPendingRequest { kind = "purchase", json = "original-payload" };
                typeof(ShopCatalogController).GetField("pending", flags).SetValue(shop, pending);
                var requestId = typeof(MythicBreedingController).GetMethod("RequestIdFor", flags);
                string originalId = (string)requestId.Invoke(breeding, new object[] { "accelerate:1:1" });
                int sent = 0;
                fixture.Transport((path, method, json, bearer) => { sent++; return new AuthHttpResult { Status = 200 }; });
                network.BaseUrl = "http://127.0.0.1:18085/api";
                shop.Retry(); breeding.RefreshStatus();
                Assert.IsTrue(shop.HasPending);
                Assert.AreSame(pending, typeof(ShopCatalogController).GetField("pending", flags).GetValue(shop));
                Assert.AreEqual(originalId, requestId.Invoke(breeding, new object[] { "accelerate:1:1" }));
                Assert.AreEqual(0, sent);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(shopRoot); UnityEngine.Object.DestroyImmediate(breedingRoot);
            UnityEngine.Object.DestroyImmediate(networkRoot); NetworkManager.Instance = previousNetwork;
        }
    }

    private static void Drain(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested) Drain(nested);
            else Assert.Fail("Deterministic auth test unexpectedly yielded a real async operation.");
        }
    }
    private sealed class SessionFixture : IDisposable
    {
        public readonly string Endpoint = "http://127.0.0.1:18084/api";
        public readonly AuthSession Session;
        private readonly AuthSession previous;
        public SessionFixture()
        {
            previous = AuthSession.Instance;
            Session = new GameObject("IsolatedAuthRegression").AddComponent<AuthSession>();
            typeof(AuthSession).GetProperty("Instance").SetValue(null, Session);
            typeof(AuthSession).GetProperty("ApiBaseUrl").SetValue(Session, Endpoint);
            typeof(AuthSession).GetProperty("User").SetValue(Session, new AuthUser { userId = 7, username = "test-account" });
            Set("accessToken", "initial"); Set("expiresAt", Time.realtimeSinceStartupAsDouble + 3600);
            Set("credential", new StoredCredential { apiBaseUrl = Endpoint, userId = 7, refreshToken = "refresh" });
            Set("store", new MemoryCredentialStore());
        }
        public void Set(string field, object value) => typeof(AuthSession).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Session, value);
        public object Get(string field) => typeof(AuthSession).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Session);
        public void Transport(Func<string, string, string, string, AuthHttpResult> send)
        {
            Func<string, string, string, string, Action<AuthHttpResult>, WWWForm, IEnumerator> transport =
                (path, method, json, bearer, completed, form) => Respond(send(path, method, json, bearer), completed);
            Set("testTransport", transport);
        }
        private static IEnumerator Respond(AuthHttpResult response, Action<AuthHttpResult> completed) { completed(response); yield break; }
        public AuthHttpResult Tokens(string access) => new AuthHttpResult { Status = 200, Body = JsonUtility.ToJson(new AuthTokens {
            tokenType = "Bearer", accessToken = access, refreshToken = "rotated", expiresIn = 3600,
            user = new AuthUser { userId = 7, username = "test-account" } }) };
        public void Dispose() { UnityEngine.Object.DestroyImmediate(Session.gameObject); typeof(AuthSession).GetProperty("Instance").SetValue(null, previous); }
        private sealed class MemoryCredentialStore : ISecureCredentialStore
        {
            public StoredCredential Load() => null;
            public void Save(StoredCredential value) { }
            public void Dispose() { }
        }
    }

    [Test] public void GuestProfileNameTracksActualServerAccountId()
    {
        var user = new AuthUser { userId = 12, username = "guest-internal", isGuest = true };
        Assert.That(LobbyManager.FormatProfileName(user, user.username), Is.EqualTo("Guest-12"));
        user.userId = 9876543210L;
        Assert.That(LobbyManager.FormatProfileName(user, user.username), Is.EqualTo("Guest-9876543210"));
        Assert.That(user.username, Is.EqualTo("guest-internal"));
    }
    [Test] public void LinkedProfileNamePreservesLobbyUsername()
    {
        var user = new AuthUser { userId = 12, username = "auth-username", isGuest = false };
        Assert.That(LobbyManager.FormatProfileName(user, "lobby-username"), Is.EqualTo("lobby-username"));
    }
    [Test] public void LegacyProfileNamePreservesExistingUsername()
    {
        Assert.That(LobbyManager.FormatProfileName(null, "sh1"), Is.EqualTo("sh1"));
    }
    [TestCase(0L)]
    [TestCase(-1L)]
    public void InvalidGuestAccountIdPreservesExistingUsername(long userId)
    {
        var user = new AuthUser { userId = userId, isGuest = true };
        Assert.That(LobbyManager.FormatProfileName(user, "existing-username"), Is.EqualTo("existing-username"));
    }
    [Test] public void ExistingProfileFontSupportsGuestAccountLabel()
    {
        var preview = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        try
        {
            var lobby = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyManager>(true)).Single();
            Assert.That(lobby.text_UserName, Is.Not.Null);
            Assert.That(lobby.text_UserName.font, Is.Not.Null);
            Assert.That(lobby.text_UserName.font.HasCharacters("Guest-0123456789"), Is.True);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
    }
    [Test] public void GuestSecretsAreStrongDistinctCanonicalBase64Url()
    {
        string first = SecureCredentialStore.NewGuestSecret(), second = SecureCredentialStore.NewGuestSecret();
        Assert.That(first.Length, Is.EqualTo(43)); Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(first.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'), Is.True);
    }
    [TestCase("http://example.com/api")]
    [TestCase("https://user:password@example.com/api")]
    [TestCase("https://example.com/api?token=x")]
    [TestCase("https://example.com/api#fragment")]
    public void UnsafeEndpointsAreRejected(string endpoint) => Assert.Throws<InvalidOperationException>(() => AuthSession.NormalizeEndpoint(endpoint));
    [Test] public void LocalAndProductionCredentialsHaveSeparateScopes()
    {
        Assert.That(SecureCredentialStore.ScopeName("http://localhost:8080/api"), Is.Not.EqualTo(SecureCredentialStore.ScopeName("http://localhost:18080/api")));
        Assert.That(AuthSession.NormalizeEndpoint("https://example.com/api/"), Is.EqualTo("https://example.com/api"));
    }
    [Test] public void WindowsCredentialRoundtripIsEncryptedAndTamperFailsWithoutDeleting()
    {
#if UNITY_EDITOR_WIN
        string directory = Path.Combine(Path.GetTempPath(), "MyDefenseAuthTest-" + Guid.NewGuid().ToString("N"));
        SecureCredentialStore store = null;
        try
        {
            store = new SecureCredentialStore(directory, "http://localhost:18080/api");
            Assert.Throws<IOException>(() => new SecureCredentialStore(directory, "http://localhost:18080/api"));
            Assert.That(store.Load(), Is.Null);
            var original = new StoredCredential { apiBaseUrl = "http://localhost:18080/api", guestSecret = SecureCredentialStore.NewGuestSecret(), refreshToken = "first-refresh" };
            store.Save(original);
            Assert.That(store.Load().guestSecret, Is.EqualTo(original.guestSecret));
            original.refreshToken = "rotated-refresh"; store.Save(original);
            Assert.That(store.Load().refreshToken, Is.EqualTo("rotated-refresh"));
            string file = Directory.GetFiles(directory, "*.bin").Single();
            Assert.That(System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(file)), Does.Not.Contain(original.guestSecret));
            File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => store.Load());
            Assert.That(File.Exists(file), Is.True);
        }
        finally { store?.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
#else
        Assert.Ignore("Windows DPAPI platform test.");
#endif
    }
    [Test] public void StartupPrefabHasReplaceableThemeAndNoReadyProviderClaims()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthStartupAssetBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        var view = prefab.GetComponent<AuthStartupView>();
        Assert.That(view.theme, Is.Not.Null); Assert.That(view.theme.background, Is.Not.Null);
        Assert.That(view.theme.entryPolicy, Is.EqualTo(AccountEntryPolicy.GuestFirst));
        Assert.That(view.googleButton.interactable, Is.False); Assert.That(view.appleButton.interactable, Is.False);
        Assert.That(view.background.raycastTarget, Is.False);
        Assert.That(prefab.GetComponent<CanvasGroup>().ignoreParentGroups, Is.True);
        Assert.That(view.stages.Length, Is.EqualTo(3));
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == AuthStartupAssetBuilder.FixturePath), Is.False);
    }
    [Test] public void LegacyLobbyRequiresExplicitDevelopmentEnvironmentAndFlag()
    {
        string env = Environment.GetEnvironmentVariable("MYDEFENSE_ENV"), flag = Environment.GetEnvironmentVariable("MYDEFENSE_LEGACY_LOBBY");
        try
        {
            Environment.SetEnvironmentVariable("MYDEFENSE_ENV", null); Environment.SetEnvironmentVariable("MYDEFENSE_LEGACY_LOBBY", "1");
            Assert.That(NetworkManager.AllowLegacyLobby, Is.False);
            Environment.SetEnvironmentVariable("MYDEFENSE_ENV", "production"); Assert.That(NetworkManager.AllowLegacyLobby, Is.False);
            Environment.SetEnvironmentVariable("MYDEFENSE_ENV", "local"); Assert.That(NetworkManager.AllowLegacyLobby, Is.True);
            Environment.SetEnvironmentVariable("MYDEFENSE_LEGACY_LOBBY", null); Assert.That(NetworkManager.AllowLegacyLobby, Is.False);
        }
        finally { Environment.SetEnvironmentVariable("MYDEFENSE_ENV", env); Environment.SetEnvironmentVariable("MYDEFENSE_LEGACY_LOBBY", flag); }
    }
}
