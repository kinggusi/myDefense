using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;
    
    // 🔥 자신의 서버 주소로 수정 (localhost는 유니티 에디터 기준)
    public string BaseUrl = RuntimeEnvironmentConfig.DefaultApiBaseUrl;

    void Awake()
    {
        Instance = this;
        if (RuntimeEnvironmentConfig.HasApiBaseUrlOverride)
            BaseUrl = RuntimeEnvironmentConfig.ApiBaseUrl;
        else if (string.IsNullOrWhiteSpace(BaseUrl))
            BaseUrl = RuntimeEnvironmentConfig.DefaultApiBaseUrl;
        DontDestroyOnLoad(gameObject);
    }

    // 일반적인 POST (소환 등)
    public void Post(string uri, WWWForm form, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostRequest(BaseUrl + uri, form, onSuccess, onError));
    }

    // JSON POST (머지 등 @RequestBody용)
    public void PostJson(string uri, string json, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostJsonRequest(BaseUrl + uri, json, onSuccess, onError));
    }

    IEnumerator PostRequest(string url, WWWForm form, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) onError?.Invoke(www.error);
            else onSuccess?.Invoke(www.downloadHandler.text);
        }
    }

    IEnumerator PostJsonRequest(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        using (var www = new UnityWebRequest(url, "POST")) 
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            onError?.Invoke(string.IsNullOrWhiteSpace(www.downloadHandler.text) ? www.error : www.downloadHandler.text);
        else onSuccess?.Invoke(www.downloadHandler.text);
    }
    }

    // ✅ 로비 데이터 조회용 GET (Action<string>으로 결과를 돌려줌)
    public void Get(string uri, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetRequest(BaseUrl + uri, onSuccess, onError));
    }

    private IEnumerator GetRequest(string url, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GET ERROR] {url} : {www.error}");
                onError?.Invoke(www.error);
            }
            else
            {
                onSuccess?.Invoke(www.downloadHandler.text);
            }
        }
    }

    // 타임아웃 기본값
    private const int DefaultTimeoutSeconds = 10;

    // 제네릭 POST 공통 처리 API
    public void PostJsonAsync<TRequest, TResponse>(string uri, TRequest requestBody, Action<ApiResult<TResponse>> callback)
    {
        StartCoroutine(PostJsonCoroutine(BaseUrl + uri, requestBody, callback));
    }

    private IEnumerator PostJsonCoroutine<TRequest, TResponse>(string url, TRequest requestBody, Action<ApiResult<TResponse>> callback)
    {
        string json = JsonUtility.ToJson(requestBody);
        using (var www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = DefaultTimeoutSeconds;

            yield return www.SendWebRequest();

            var result = new ApiResult<TResponse>();
            result.StatusCode = www.responseCode;

            if (www.result == UnityWebRequest.Result.Success)
            {
                result.IsSuccess = true;
                try
                {
                    result.Data = JsonUtility.FromJson<TResponse>(www.downloadHandler.text);
                }
                catch (Exception e)
                {
                    result.IsSuccess = false;
                    result.NetworkError = "JSON_PARSE_ERROR: " + e.Message;
                }
            }
            else
            {
                result.IsSuccess = false;
                string errorBody = www.downloadHandler.text;

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    result.StatusCode = 0; // 연결 자체가 실패한 경우
                    result.NetworkError = "CONNECTION_FAILED: " + www.error;
                }
                else if (www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // HTTP 4xx/5xx 에러 본문 파싱
                    if (!string.IsNullOrEmpty(errorBody) && errorBody.Trim().StartsWith("{") && errorBody.Trim().EndsWith("}"))
                    {
                        try
                        {
                            result.Error = JsonUtility.FromJson<ApiErrorResponse>(errorBody);
                        }
                        catch (Exception ex)
                        {
                            result.NetworkError = "ERROR_JSON_PARSE_FAILED: Failed to parse ErrorResponse. " + ex.Message;
                        }
                    }
                    else
                    {
                        result.NetworkError = "HTTP_PROTOCOL_ERROR: " + www.error;
                    }
                }
                else if (www.result == UnityWebRequest.Result.DataProcessingError)
                {
                    result.NetworkError = "DATA_PROCESSING_ERROR: " + www.error;
                }
                else
                {
                    // 타임아웃 등 기타 네트워크 장애
                    if (www.error != null && www.error.Contains("Request timeout"))
                    {
                        result.StatusCode = 0;
                        result.NetworkError = "TIMEOUT_ERROR: Request timed out.";
                    }
                    else
                    {
                        result.NetworkError = "UNKNOWN_NETWORK_ERROR: " + www.error;
                    }
                }
            }

            callback?.Invoke(result);
        }
    }
}
