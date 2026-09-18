using System;
using System.IO;
using System.Linq;
using MyDefense.Auth;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AuthClientTests
{
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
