using System;
using System.IO;
using System.Reflection;
using MyDefense.Auth;
using NUnit.Framework;
using UnityEngine;

public sealed class DevelopmentLoginTests
{
    [TestCase(true, "local", "http://localhost:8080/api", "jjangash", true)]
    [TestCase(true, "dev", "http://127.0.0.1:18080/api", "kingusi", true)]
    [TestCase(true, "local", "https://localhost/api", "jjangash", true)]
    [TestCase(false, "local", "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, null, "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, "production", "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, "prod", "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, "local,prod", "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, "dev,production", "http://localhost:8080/api", "jjangash", false)]
    [TestCase(true, "local", "https://example.com/api", "jjangash", false)]
    [TestCase(true, "local", "http://localhost.evil.example/api", "jjangash", false)]
    [TestCase(true, "local", "http://user:password@localhost/api", "jjangash", false)]
    [TestCase(true, "local", "http://localhost/api?token=x", "jjangash", false)]
    [TestCase(true, "local", "http://localhost/api#fragment", "jjangash", false)]
    [TestCase(true, "local", "ftp://localhost/api", "jjangash", false)]
    [TestCase(true, "local", "http://localhost/api", "admin", false)]
    [TestCase(true, "local", "http://localhost/api", "", false)]
    public void DevelopmentLoginRequiresExplicitEditorLoopbackSelection(bool editor, string environment, string endpoint, string account, bool allowed)
        => Assert.That(DevelopmentLoginPolicy.Evaluate(editor, environment, endpoint, account), Is.EqualTo(allowed));

    [Test] public void DevelopmentScopesCannotReplaceRegularGuestScope()
    {
        string root = Path.GetTempPath();
        string original = Path.Combine(root, "Auth", SecureCredentialStore.ScopeName("default"));
        Assert.That(DevelopmentLoginPolicy.CredentialDirectory(root, "default", null), Is.EqualTo(original));
        string first = DevelopmentLoginPolicy.CredentialDirectory(root, "default", "jjangash");
        string second = DevelopmentLoginPolicy.CredentialDirectory(root, "default", "kingusi");
        Assert.That(first, Is.Not.EqualTo(original)); Assert.That(second, Is.Not.EqualTo(original));
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.Throws<InvalidOperationException>(() => DevelopmentLoginPolicy.CredentialDirectory(root, "default", "../default"));
    }

    [Test] public void DevelopmentRefreshStorageLeavesOriginalGuestBytesUntouched()
    {
#if UNITY_EDITOR_WIN
        string root = Path.Combine(Path.GetTempPath(), "MyDefenseDevAuth-" + Guid.NewGuid().ToString("N"));
        const string endpoint = "http://localhost:18080/api";
        try
        {
            string guestDirectory = DevelopmentLoginPolicy.CredentialDirectory(root, "default", null);
            string developerDirectory = DevelopmentLoginPolicy.CredentialDirectory(root, "default", "jjangash");
            using (var guest = new SecureCredentialStore(guestDirectory, endpoint))
            using (var developer = new SecureCredentialStore(developerDirectory, endpoint))
            {
                string secret = SecureCredentialStore.NewGuestSecret();
                guest.Save(new StoredCredential { apiBaseUrl = endpoint, userId = 11, guestSecret = secret, refreshToken = "guest-refresh" });
                string guestFile = Path.Combine(guestDirectory, SecureCredentialStore.ScopeName(endpoint) + ".bin");
                byte[] originalBytes = File.ReadAllBytes(guestFile);
                developer.Save(new StoredCredential { apiBaseUrl = endpoint, userId = 22, refreshToken = "developer-refresh" });
                developer.Save(new StoredCredential { apiBaseUrl = endpoint, userId = 22, refreshToken = "rotated-developer-refresh" });
                Assert.That(File.ReadAllBytes(guestFile), Is.EqualTo(originalBytes));
                Assert.That(guest.Load().guestSecret, Is.EqualTo(secret));
                Assert.That(guest.Load().userId, Is.EqualTo(11));
                Assert.That(developer.Load().guestSecret, Is.Null.Or.Empty);
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
#else
        Assert.Ignore("Windows secure storage test.");
#endif
    }

    [TestCase("kingusi", false, false)]
    [TestCase("jjangash", true, false)]
    [TestCase("jjangash", false, true)]
    public void AccountResponseMustMatchSelectedDeveloperBeforeSaving(string responseUsername, bool guest, bool expected)
    {
        var go = new GameObject("DevelopmentAuthTest"); var store = new RecordingStore();
        try
        {
            var session = go.AddComponent<AuthSession>();
            SetProperty(session, "ApiBaseUrl", "http://localhost:18080/api");
            SetProperty(session, "DevelopmentAccount", "jjangash");
            typeof(AuthSession).GetField("store", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(session, store);
            var response = new AuthHttpResult { Status = 200, Body = JsonUtility.ToJson(new AuthTokens {
                tokenType = "Bearer", accessToken = "test-access", refreshToken = "test-refresh", expiresIn = 900,
                user = new AuthUser { userId = 22, username = responseUsername, isGuest = guest } }) };
            bool accepted = (bool)typeof(AuthSession).GetMethod("Accept", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(session, new object[] { response });
            Assert.That(accepted, Is.EqualTo(expected)); Assert.That(store.SaveCount, Is.EqualTo(expected ? 1 : 0));
            if (expected) Assert.That(store.Value.guestSecret, Is.Null.Or.Empty);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test] public void DevelopmentSessionNeverCreatesGuestOrLinksExternalIdentity()
    {
        var go = new GameObject("DevelopmentAuthTest");
        try
        {
            var session = go.AddComponent<AuthSession>(); SetProperty(session, "DevelopmentAccount", "jjangash");
            bool? guest = null, external = null;
            Assert.That(session.SignInGuest(ok => guest = ok).MoveNext(), Is.False);
            Assert.That(session.External("GOOGLE", false, ok => external = ok).MoveNext(), Is.False);
            Assert.That(guest, Is.False); Assert.That(external, Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    [Test] public void PersistedCredentialHasNoPasswordField()
        => Assert.That(typeof(StoredCredential).GetField("password"), Is.Null);
    private static void SetProperty(AuthSession session, string property, object value)
        => typeof(AuthSession).GetProperty(property).SetValue(session, value);
    private sealed class RecordingStore : ISecureCredentialStore
    {
        public int SaveCount; public StoredCredential Value;
        public StoredCredential Load() => Value;
        public void Save(StoredCredential value) { SaveCount++; Value = value; }
        public void Dispose() { }
    }
}
