using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace MyDefense.Auth
{
    public sealed class AuthStartupController : MonoBehaviour
    {
        public const string ViewResource = "Auth/AuthStartupView";
        private LobbyManager lobby;
        private AuthStartupView view;
        private AuthSession session;
        private CanvasGroup lobbyInteraction;
        private bool previousInteractable, busy, googleAvailable, appleAvailable, credentialReady;
        public bool CanSubmitDevelopmentLogin => credentialReady && session != null && session.IsDevelopmentSession && !busy;
        public static void Begin(LobbyManager lobby)
        {
            var controller = lobby.gameObject.AddComponent<AuthStartupController>();
            controller.lobby = lobby; controller.StartCoroutine(controller.Prepare());
        }
        private IEnumerator Prepare()
        {
            var canvas = lobby.viewObjects[0].GetComponentInParent<Canvas>();
            var prefab = Resources.Load<GameObject>(ViewResource);
            if (canvas == null || prefab == null) { Debug.LogError("[Auth] Startup UI asset missing. Lobby requests remain blocked."); yield break; }
            lobbyInteraction = canvas.GetComponent<CanvasGroup>();
            if (lobbyInteraction == null) lobbyInteraction = canvas.gameObject.AddComponent<CanvasGroup>();
            previousInteractable = lobbyInteraction.interactable; lobbyInteraction.interactable = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            view = Instantiate(prefab, canvas.transform, false).GetComponent<AuthStartupView>();
            view.name = "AuthStartupView"; view.transform.SetAsLastSibling();
            view.startButton.onClick.AddListener(StartGuest);
            view.googleButton.onClick.AddListener(() => StartProvider("GOOGLE"));
            view.appleButton.onClick.AddListener(() => StartProvider("APPLE"));
            view.SetState("기기 계정 확인 중", true, 0);
            yield return null;
            try { session = AuthSession.GetOrCreate(NetworkManager.Instance.BaseUrl); }
            catch (Exception) { view.SetState("계정 저장소를 열지 못했습니다. 같은 계정의 다른 실행 창 또는 서버 주소 설정을 확인해 주세요.", true, -1, true); yield break; }
            if (!session.TryLoadCredential()) { view.SetState(session.LastError, true, -1, true); yield break; }
            credentialReady = true;
            if (session.IsDevelopmentSession)
            {
                if (session.IsAuthenticated) { yield return LoadLobby(); yield break; }
                if (session.HasCredential)
                {
                    busy = true; bool restored = false;
                    yield return session.Restore(ok => restored = ok);
                    busy = false;
                    if (restored) { yield return LoadLobby(); yield break; }
                }
                ShowError("개발 계정 " + session.DevelopmentAccount + " · Unity의 Development Login 창에서 비밀번호를 입력해 주세요.");
                yield break;
            }
            if (session.IsAuthenticated && CanEnter()) { yield return LoadLobby(); yield break; }
            if (session.HasCredential)
            {
                busy = true; bool ok = false;
                yield return session.Restore(value => ok = value);
                if (ok && CanEnter()) { yield return LoadLobby(); yield break; }
                busy = false; ShowError(ok ? "계정 연동 후 입장할 수 있습니다." : session.LastError);
            }
            else view.SetState("탑승 준비 완료", false);
            yield return session.Providers(ConfigureProviders);
            if (view != null && !busy) view.SetProviderAvailability(googleAvailable, appleAvailable);
        }
        private bool CanEnter() => view.theme.entryPolicy == AccountEntryPolicy.GuestFirst || !session.User.isGuest;
        private void ConfigureProviders(ProvidersResponse response)
        { googleAvailable = IsAvailable(response, "GOOGLE"); appleAvailable = IsAvailable(response, "APPLE"); }
        private static bool IsAvailable(ProvidersResponse response, string name)
        {
            return response != null && response.providers != null && response.providers.Any(p => p.provider == name && p.available)
                && ExternalIdentityProviders.Find(name) != null && ExternalIdentityProviders.Find(name).IsAvailable;
        }
        private void StartGuest() { if (!busy) StartCoroutine(Guest()); }
        public bool SubmitDevelopmentLogin(string password)
        {
            if (!CanSubmitDevelopmentLogin) return false;
            StartCoroutine(DevelopmentLogin(password));
            return true;
        }
        private IEnumerator DevelopmentLogin(string password)
        {
            busy = true; view.SetState("개발 계정 확인 중", true, 0); bool ok = session.IsAuthenticated;
            if (!ok) yield return session.SignInDevelopment(password, value => ok = value);
            password = null;
            if (ok) yield return LoadLobby();
            else { busy = false; ShowError(session.LastError); }
        }
        private IEnumerator Guest()
        {
            busy = true; view.SetState("계정 연결 중", true, 0); bool ok = false;
            if (session.IsAuthenticated) ok = true;
            else if (session.HasCredential) yield return session.Restore(value => ok = value);
            else yield return session.SignInGuest(value => ok = value);
            if (ok) yield return LoadLobby();
            else { busy = false; ShowError(session.LastError); }
        }
        private void StartProvider(string provider) { if (!busy) StartCoroutine(Provider(provider)); }
        private IEnumerator Provider(string provider)
        {
            busy = true; view.SetState("계정 제공자 확인 중", true, 0); bool ok = false;
            yield return session.External(provider, session.IsAuthenticated, value => ok = value);
            if (ok) yield return LoadLobby();
            else { busy = false; ShowError(session.LastError); }
        }
        private IEnumerator LoadLobby()
        {
            busy = true; view.SetState("유닛 정보 불러오는 중", true, 1);
            bool finished = false, ok = false;
            lobby.InitializeForAccount(session.User.username, value => { finished = true; ok = value; });
            double deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!finished && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (!finished || !ok) { busy = false; ShowError("로비 정보를 불러오지 못했습니다. 같은 계정으로 다시 시도합니다."); yield break; }
            view.SetState("로비 준비 완료", true, 2); yield return null;
            lobbyInteraction.interactable = previousInteractable;
            Destroy(view.gameObject); view = null; Destroy(this);
        }
        private void ShowError(string message)
        {
            if (session != null && session.IsDevelopmentSession)
            { view.SetState(message, true, -1, true); view.startLabel.text = "개발 계정 인증 대기"; return; }
            view.SetState(message, false, -1, true); view.SetProviderAvailability(googleAvailable, appleAvailable);
        }
        private void LateUpdate() { if (view != null) view.transform.SetAsLastSibling(); }
        private void OnDestroy()
        {
            if (lobbyInteraction != null) lobbyInteraction.interactable = previousInteractable;
            if (view != null) Destroy(view.gameObject);
        }
    }
}
