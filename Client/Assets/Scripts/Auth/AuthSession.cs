using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MyDefense.Auth
{
    public sealed class AuthSession : MonoBehaviour
    {
        public static AuthSession Instance { get; private set; }
        public AuthUser User { get; private set; }
        public string ApiBaseUrl { get; private set; }
        public string DevelopmentAccount { get; private set; }
        public bool IsDevelopmentSession => !string.IsNullOrEmpty(DevelopmentAccount);
        public bool IsAuthenticated => User != null && !string.IsNullOrEmpty(accessToken);
        public int Generation { get; private set; }
        public string LastError { get; private set; }
        public bool HasCredential => credential != null;
        private string accessToken;
        private double expiresAt;
        private bool refreshing;
        private bool lastRefreshSucceeded;
        private ISecureCredentialStore store;
        private StoredCredential credential;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }
        public static AuthSession GetOrCreate(string endpoint)
        {
            endpoint = NormalizeEndpoint(endpoint);
            string developmentAccount = DevelopmentLoginPolicy.SelectedAccount;
            if (!string.IsNullOrEmpty(developmentAccount) && !DevelopmentLoginPolicy.IsAllowed(endpoint, developmentAccount))
                throw new InvalidOperationException("Development login requires explicit local/dev Editor loopback configuration.");
            if (Instance != null)
            {
                if (Instance.ApiBaseUrl != endpoint) throw new InvalidOperationException("Account server changed; restart required.");
                if ((Instance.DevelopmentAccount ?? "") != developmentAccount) throw new InvalidOperationException("Account selection changed; stop Play first.");
                return Instance;
            }
            string profile = "default";
            if (Application.isEditor || Debug.isDebugBuild)
                profile = Environment.GetEnvironmentVariable("MYDEFENSE_AUTH_PROFILE") ?? profile;
            var credentialStore = new SecureCredentialStore(DevelopmentLoginPolicy.CredentialDirectory(Application.persistentDataPath, profile, developmentAccount), endpoint);
            var session = new GameObject("AuthSession").AddComponent<AuthSession>();
            session.ApiBaseUrl = endpoint;
            session.DevelopmentAccount = developmentAccount;
            session.store = credentialStore;
            Instance = session;
            DontDestroyOnLoad(session.gameObject);
            return session;
        }
        private void OnDestroy()
        {
            store?.Dispose();
            if (Instance == this) Instance = null;
            accessToken = null; User = null; Generation++;
        }
        public static string NormalizeEndpoint(string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
                || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback && (Application.isEditor || Debug.isDebugBuild))))
                throw new InvalidOperationException("HTTPS API endpoint required (development loopback excepted).");
            return uri.AbsoluteUri.TrimEnd('/');
        }
        public bool TryLoadCredential()
        {
            try
            {
                var loaded = store.Load();
                if (IsDevelopmentSession && loaded != null && !string.IsNullOrEmpty(loaded.guestSecret))
                    throw new InvalidDataException("Guest credential found in development account scope.");
                credential = loaded; LastError = null; return true;
            }
            catch (Exception) { LastError = "기기에 저장된 계정을 열 수 없습니다. 기존 계정 정보는 보존했습니다. 보안 저장소 설정을 확인해 주세요."; return false; }
        }
        public IEnumerator SignInGuest(Action<bool> completed)
        {
            if (IsDevelopmentSession)
            { LastError = "개발 계정 창에서 비밀번호로 로그인해 주세요. 게스트 계정은 변경하지 않았습니다."; completed(false); yield break; }
            if (credential != null && string.IsNullOrEmpty(credential.guestSecret))
            { LastError = "연동한 계정 제공자로 다시 로그인해 주세요. 새 게스트를 만들지 않았습니다."; completed(false); yield break; }
            if (credential == null)
            {
                var created = new StoredCredential { apiBaseUrl = ApiBaseUrl, guestSecret = SecureCredentialStore.NewGuestSecret() };
                if (!TrySave(created)) { completed(false); yield break; }
                credential = created;
            }
            AuthHttpResult response = null;
            yield return SendRaw("/auth/guest", "POST", JsonUtility.ToJson(new GuestRequest { guestSecret = credential.guestSecret }), null, r => response = r);
            completed(Accept(response));
        }
        public IEnumerator SignInDevelopment(string password, Action<bool> completed)
        {
            if (!IsDevelopmentSession || !DevelopmentLoginPolicy.IsAllowed(ApiBaseUrl, DevelopmentAccount)
                || DevelopmentLoginPolicy.SelectedAccount != DevelopmentAccount || IsAuthenticated || refreshing)
            { password = null; LastError = "개발 계정 선택 또는 실행 환경을 확인해 주세요. 계정 전환은 Play 종료 후 가능합니다."; completed(false); yield break; }
            if (string.IsNullOrEmpty(password) || password.Length > 256)
            { password = null; LastError = "비밀번호를 확인해 주세요."; completed(false); yield break; }
            string body = JsonUtility.ToJson(new DevelopmentLoginRequest { username = DevelopmentAccount, password = password });
            password = null;
            AuthHttpResult response = null;
            yield return SendRaw("/dev/auth/login", "POST", body, null, r => response = r);
            body = null;
            completed(Accept(response));
        }
        public IEnumerator Restore(Action<bool> completed)
        {
            if (credential == null) { completed(false); yield break; }
            if (string.IsNullOrEmpty(credential.refreshToken))
            {
                if (IsDevelopmentSession) { LastError = "개발 계정 비밀번호를 다시 입력해 주세요."; completed(false); yield break; }
                yield return SignInGuest(completed); yield break;
            }
            yield return Refresh(completed, true);
        }
        public IEnumerator EnsureFresh(Action<bool> completed)
        {
            if (!IsAuthenticated) { LastError = "로그인이 필요합니다."; completed(false); yield break; }
            if (Time.realtimeSinceStartupAsDouble < expiresAt - 45) { completed(true); yield break; }
            yield return Refresh(completed, true);
        }
        private IEnumerator Refresh(Action<bool> completed, bool recoverGuest)
        {
            if (refreshing)
            {
                while (refreshing) yield return null;
                completed(lastRefreshSucceeded && IsAuthenticated && Time.realtimeSinceStartupAsDouble < expiresAt - 5);
                yield break;
            }
            refreshing = true;
            bool ok = false;
            try
            {
                AuthHttpResult response = null;
                yield return SendRaw("/auth/refresh", "POST", JsonUtility.ToJson(new RefreshRequest { refreshToken = credential.refreshToken }), null, r => response = r);
                ok = Accept(response);
                // Expired/replayed refresh can be recovered with the same protected device credential.
                if (!ok && response.Status == 401 && recoverGuest && !string.IsNullOrEmpty(credential.guestSecret))
                    yield return SignInGuest(value => ok = value);
            }
            finally { lastRefreshSucceeded = ok; refreshing = false; }
            completed(ok);
        }
        private bool Accept(AuthHttpResult response)
        {
            if (response == null || !response.Success) { LastError = FriendlyError(response); return false; }
            AuthTokens tokens;
            try { tokens = JsonUtility.FromJson<AuthTokens>(response.Body); }
            catch (Exception) { tokens = null; }
            if (tokens == null || tokens.user == null || tokens.user.userId <= 0 || string.IsNullOrWhiteSpace(tokens.user.username)
                || tokens.tokenType != "Bearer" || string.IsNullOrEmpty(tokens.accessToken)
                || string.IsNullOrEmpty(tokens.refreshToken) || tokens.expiresIn <= 0)
            { LastError = "계정 응답을 확인하지 못했습니다. 다시 시도해 주세요."; return false; }
            if (IsDevelopmentSession && !DevelopmentLoginPolicy.MatchesAccount(tokens.user, DevelopmentAccount))
            { LastError = "선택한 개발 계정과 응답이 일치하지 않습니다. 저장된 계정은 변경하지 않았습니다."; return false; }
            if ((User != null && User.userId != tokens.user.userId)
                || (credential != null && credential.userId > 0 && credential.userId != tokens.user.userId))
            { LastError = "계정이 변경되어 연결을 중단했습니다. 게임을 다시 시작해 주세요."; return false; }
            var next = new StoredCredential { apiBaseUrl = ApiBaseUrl, userId = tokens.user.userId, guestSecret = credential?.guestSecret, refreshToken = tokens.refreshToken };
            if (!TrySave(next)) return false;
            credential = next;
            if (User == null) Generation++;
            User = tokens.user;
            accessToken = tokens.accessToken;
            expiresAt = Time.realtimeSinceStartupAsDouble + tokens.expiresIn;
            LastError = null;
            return true;
        }
        private bool TrySave(StoredCredential value)
        {
            try { store.Save(value); return true; }
            catch (Exception) { LastError = "계정을 안전하게 저장하지 못했습니다. 저장 공간과 보안 저장소를 확인한 뒤 다시 시도해 주세요."; return false; }
        }
        public IEnumerator SendAccount(string path, string method, string json, WWWForm form, Action<AuthHttpResult> completed)
        {
            bool ready = false;
            yield return EnsureFresh(ok => ready = ok);
            if (!ready) { completed(new AuthHttpResult { Status = 401, Error = LastError }); yield break; }
            int generation = Generation;
            string usedToken = accessToken;
            AuthHttpResult response = null;
            yield return SendRaw(path, method, json, usedToken, r => response = r, form);
            if (generation != Generation) { completed(new AuthHttpResult { Status = 409, Error = "계정이 변경되었습니다." }); yield break; }
            // Never blindly replay spending POSTs. Their existing requestId retry UX remains authoritative.
            if (response.Status == 401 && method == "GET")
            {
                bool refreshed = usedToken != accessToken;
                if (!refreshed) yield return Refresh(ok => refreshed = ok, true);
                if (refreshed) yield return SendRaw(path, method, json, accessToken, r => response = r, form);
            }
            if (generation != Generation) { completed(new AuthHttpResult { Status = 409, Error = "계정이 변경되었습니다." }); yield break; }
            completed(response);
        }
        public IEnumerator Providers(Action<ProvidersResponse> completed)
        {
            AuthHttpResult response = null;
            yield return SendRaw("/auth/providers", "GET", null, null, r => response = r);
            ProvidersResponse providers = null;
            if (response.Success) { try { providers = JsonUtility.FromJson<ProvidersResponse>(response.Body); } catch (Exception) { } }
            completed(providers);
        }
        public IEnumerator External(string provider, bool link, Action<bool> completed)
        {
            if (IsDevelopmentSession)
            { LastError = "개발 계정은 외부 계정 연동 대상이 아닙니다."; completed(false); yield break; }
            if (!link && credential != null && !string.IsNullOrEmpty(credential.guestSecret))
            { LastError = "이 기기의 게스트를 먼저 복구한 뒤 계정을 연동해 주세요. 기존 진행은 변경하지 않았습니다."; completed(false); yield break; }
            var adapter = ExternalIdentityProviders.Find(provider);
            if (adapter == null || !adapter.IsAvailable) { LastError = "이 기기의 계정 연동 모듈이 준비되지 않았습니다."; completed(false); yield break; }
            bool ready = !link;
            if (link) yield return EnsureFresh(ok => ready = ok);
            if (!ready) { completed(false); yield break; }
            AuthHttpResult response = null;
            yield return SendRaw("/auth/challenges", "POST", JsonUtility.ToJson(new ChallengeRequest { provider = provider, purpose = link ? "LINK" : "LOGIN" }), link ? accessToken : null, r => response = r);
            if (!response.Success) { LastError = FriendlyError(response); completed(false); yield break; }
            AuthChallenge challenge = null;
            try { challenge = JsonUtility.FromJson<AuthChallenge>(response.Body); } catch (Exception) { }
            if (challenge == null || string.IsNullOrEmpty(challenge.nonce) || string.IsNullOrEmpty(challenge.challengeId))
            { LastError = "계정 연동 요청을 확인하지 못했습니다."; completed(false); yield break; }
            string proof = null, error = null;
            yield return adapter.GetIdToken(challenge.nonce, (token, failure) => { proof = token; error = failure; });
            if (string.IsNullOrEmpty(proof)) { LastError = string.IsNullOrEmpty(error) ? "계정 연동이 취소되었습니다." : "계정 제공자 확인에 실패했습니다."; completed(false); yield break; }
            yield return SendRaw(link ? "/auth/links" : "/auth/external", "POST", JsonUtility.ToJson(new ExternalAuthRequest { provider = provider, idToken = proof, challengeId = challenge.challengeId }), link ? accessToken : null, r => response = r);
            proof = null;
            if (link)
            {
                if (response.Success) User.isGuest = false;
                else LastError = response.Status == 409 ? "이미 다른 게임 계정에 연결되어 있습니다. 현재 진행은 변경하지 않았습니다." : FriendlyError(response);
                completed(response.Success);
            }
            else completed(Accept(response));
        }
        private IEnumerator SendRaw(string path, string method, string json, string bearer, Action<AuthHttpResult> completed, WWWForm form = null)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("/") || path.StartsWith("//") || path.Contains("://") || path.Contains("..") || path.Contains("\\"))
            { completed(new AuthHttpResult { Error = "잘못된 API 경로입니다." }); yield break; }
            using (var request = form != null ? UnityWebRequest.Post(ApiBaseUrl + path, form) : new UnityWebRequest(ApiBaseUrl + path, method))
            {
                if (form == null && method != "GET")
                { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json ?? "{}")); request.SetRequestHeader("Content-Type", "application/json"); }
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 15;
                request.redirectLimit = 0;
                if (!string.IsNullOrEmpty(bearer)) request.SetRequestHeader("Authorization", "Bearer " + bearer);
                yield return request.SendWebRequest();
                completed(new AuthHttpResult { Status = request.responseCode, Body = request.downloadHandler.text,
                    Error = request.result == UnityWebRequest.Result.Success ? null : "요청을 완료하지 못했습니다." });
            }
        }
        public static string FriendlyError(AuthHttpResult response)
        {
            if (response == null || response.Status == 0) return "서버에 연결할 수 없습니다. 연결을 확인한 뒤 다시 시도해 주세요.";
            if (response.Status == 503) return "로그인 서버 또는 계정 연동 설정이 아직 준비되지 않았습니다.";
            if (response.Status == 429) return "요청이 많습니다. 잠시 후 다시 시도해 주세요.";
            if (response.Status == 401) return "계정 확인이 필요합니다. 다시 시도해 주세요.";
            return "요청을 완료하지 못했습니다. 다시 시도해 주세요. (" + response.Status + ")";
        }
    }
}
