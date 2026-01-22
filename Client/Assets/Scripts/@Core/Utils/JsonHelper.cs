using System;
using UnityEngine;
using System.Collections.Generic;

public static class JsonHelper
{
    public static List<T> FromJsonList<T>(string json)
    {
        // 서버에서 온 [...] 배열을 { "items": [...] } 형태로 감싸서 파싱하는 꼼수
        string newJson = "{ \"items\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public List<T> items;
    }
}