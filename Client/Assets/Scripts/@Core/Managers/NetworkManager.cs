using System;
using System.Collections;
using System.Text;
using MyDefense.Auth;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    public string BaseUrl = RuntimeEnvironmentConfig.DefaultApiBaseUrl;
    public static bool AllowLegacyLobby => (Application.isEditor || Debug.isDebugBuild)
        && (Environment.GetEnvironmentVariable("MYDEFENSE_ENV") == "local" || Environment.GetEnvironmentVariable("MYDEFENSE_ENV") == "dev")
        && Environment.GetEnvironmentVariable("MYDEFENSE_LEGACY_LOBBY") == "1";
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (RuntimeEnvironmentConfig.HasApiBaseUrlOverride) BaseUrl = RuntimeEnvironmentConfig.ApiBaseUrl;
        else if (string.IsNullOrWhiteSpace(BaseUrl)) BaseUrl = RuntimeEnvironmentConfig.DefaultApiBaseUrl;
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void Post(string uri, WWWForm form, Action<string> onSuccess, Action<string> onError)
        => StartCoroutine(Send(uri, "POST", null, form, r => Dispatch(r, onSuccess, onError)));
    public void PostJson(string uri, string json, Action<string> onSuccess, Action<string> onError)
        => StartCoroutine(Send(uri, "POST", json, null, r => Dispatch(r, onSuccess, onError)));
    public void Get(string uri, Action<string> onSuccess, Action<string> onError)
        => StartCoroutine(Send(uri, "GET", null, null, r => Dispatch(r, onSuccess, onError)));
    private static void Dispatch(AuthHttpResult result, Action<string> success, Action<string> failure)
    {
        if (result.Success) success?.Invoke(result.Body);
        else failure?.Invoke(string.IsNullOrWhiteSpace(result.Body) ? result.Error ?? AuthSession.FriendlyError(result) : result.Body);
    }
    public void PostJsonAsync<TRequest, TResponse>(string uri, TRequest body, Action<ApiResult<TResponse>> callback)
    {
        StartCoroutine(Send(uri, "POST", JsonUtility.ToJson(body), null, response =>
        {
            var result = new ApiResult<TResponse> { StatusCode = response.Status, IsSuccess = response.Success };
            if (response.Success)
            {
                try { result.Data = JsonUtility.FromJson<TResponse>(response.Body); }
                catch (Exception) { result.IsSuccess = false; result.NetworkError = "JSON_PARSE_ERROR"; }
            }
            else
            {
                try { if (!string.IsNullOrEmpty(response.Body)) result.Error = JsonUtility.FromJson<ApiErrorResponse>(response.Body); }
                catch (Exception) { }
                if (result.Error == null) result.NetworkError = response.Error ?? AuthSession.FriendlyError(response);
            }
            callback?.Invoke(result);
        }));
    }
    private IEnumerator Send(string uri, string method, string json, WWWForm form, Action<AuthHttpResult> completed)
    {
        var session = AuthSession.Instance;
        if (session != null)
        {
            string normalized = null;
            try { normalized = AuthSession.NormalizeEndpoint(BaseUrl); } catch (Exception) { }
            if (normalized != session.ApiBaseUrl)
            { completed(new AuthHttpResult { Error = "계정 서버가 변경되었습니다. 다시 시작해 주세요." }); yield break; }
            yield return session.SendAccount(uri, method, json, form, completed);
            yield break;
        }
        if (!AllowLegacyLobby || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out var endpoint) || !endpoint.IsLoopback)
        { completed(new AuthHttpResult { Status = 401, Error = "로그인이 필요합니다." }); yield break; }
        // Explicit local fixtures only. Never a production authentication fallback.
        if (string.IsNullOrEmpty(uri) || !uri.StartsWith("/") || uri.StartsWith("//"))
        { completed(new AuthHttpResult { Error = "잘못된 요청입니다." }); yield break; }
        using (var request = form != null ? UnityWebRequest.Post(BaseUrl + uri, form) : new UnityWebRequest(BaseUrl + uri, method))
        {
            if (form == null && method != "GET")
            { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json ?? "{}")); request.SetRequestHeader("Content-Type", "application/json"); }
            request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 15; request.redirectLimit = 0;
            yield return request.SendWebRequest();
            completed(new AuthHttpResult { Status = request.responseCode, Body = request.downloadHandler.text,
                Error = request.result == UnityWebRequest.Result.Success ? null : "서버 요청 실패" });
        }
    }
}
