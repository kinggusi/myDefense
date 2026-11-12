#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using MyDefense.Auth;
using MyDefense.Shop;
using TMPro;
using UnityEngine;

/// <summary>Real catalog controller and HTTP purchases; no production credentials or database.</summary>
public sealed class ShopHttpUiFixture : MonoBehaviour
{
    public const string Endpoint = "http://127.0.0.1:18084/api";
    public ShopCatalogView view;
    public LobbyManager lobby;
    private AuthSession ownedSession;
    private NetworkManager ownedNetwork;
    private string previousEnvironment;
    private bool changedEnvironment;
    private bool ready;
    private IEnumerator Start()
    {
        if (AuthSession.Instance != null || NetworkManager.Instance != null)
        { view.statusText.text = "기존 계정을 보호하려면 Play를 종료한 뒤 격리 씬만 실행해 주세요."; yield break; }
        previousEnvironment = Environment.GetEnvironmentVariable("MYDEFENSE_ENV");
        Environment.SetEnvironmentVariable("MYDEFENSE_ENV", "local"); changedEnvironment = true;
        ownedNetwork = new GameObject("ShopFixtureNetwork").AddComponent<NetworkManager>();
        ownedNetwork.BaseUrl = Endpoint;
        ownedSession = new GameObject("ShopFixtureAuth").AddComponent<AuthSession>();
        var type = typeof(AuthSession);
        type.GetProperty("Instance").SetValue(null, ownedSession);
        type.GetProperty("ApiBaseUrl").SetValue(ownedSession, Endpoint);
        type.GetField("store", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ownedSession, new RamCredentials());
        type.GetField("credential", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ownedSession,
            new StoredCredential { apiBaseUrl = Endpoint, guestSecret = new string('D', 42) + "A" });
        bool authenticated = false;
        yield return ownedSession.SignInGuest(ok => authenticated = ok);
        if (!authenticated) { view.statusText.text = "격리 ShopUiSmokeServer 18084를 실행해 주세요. 실제 계정은 변경하지 않았습니다."; yield break; }
        typeof(LobbyManager).GetField("currentUsername", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lobby, ownedSession.User.username);
        var controller = lobby.gameObject.AddComponent<ShopCatalogController>();
        controller.Initialize(lobby);
        lobby.LoadLobbyData(_ => controller.SetLobbyTab(0));
        ready = true;
    }
    public void RejectCurrentAccessToken()
    {
        if (!ready || !ReferenceEquals(AuthSession.Instance, ownedSession)) return;
        typeof(AuthSession).GetField("accessToken", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ownedSession, "fixture-rejected-access-token");
        typeof(AuthSession).GetField("expiresAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ownedSession, Time.realtimeSinceStartupAsDouble + 3600);
        view.statusText.text = "JWT 거부 준비: 상품 구매 → 결과 확인 필요 → 같은 요청 재시도 (자동 POST 재전송 없음)";
    }
    private void OnDestroy()
    {
        if (ownedSession != null)
        {
            if (ownedSession.User != null) new ShopPendingRequestStore(Endpoint, ownedSession.User.userId).Clear();
            Destroy(ownedSession.gameObject);
        }
        if (ownedNetwork != null) Destroy(ownedNetwork.gameObject);
        if (changedEnvironment) Environment.SetEnvironmentVariable("MYDEFENSE_ENV", previousEnvironment);
    }
    private sealed class RamCredentials : ISecureCredentialStore
    {
        private StoredCredential value;
        public StoredCredential Load() => value;
        public void Save(StoredCredential credential) { value = credential; }
        public void Dispose() { value = null; }
    }
}
#endif
