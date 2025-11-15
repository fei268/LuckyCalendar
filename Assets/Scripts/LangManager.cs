using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public static class LangManager
{
    private static JObject _langData;
    private static JObject _fallbackLangData;
    public static string DebugLogs = "";

    /// <summary>
    /// 用这个可以将日志直接显示在终端，方便查看
    /// 调用方式 label.text= LangManager.DebugLogs;
    /// </summary>
    /// <param name="msg"></param>
    private static void Log(string msg)
    {
        Debug.Log(msg);
        DebugLogs += msg + "\n";
    }

    public static async Task LoadAsync(string langCode)
    {
        _langData = await LoadLangJsonAsync($"lang_{langCode}.json");
        _fallbackLangData = await LoadLangJsonAsync("lang_zh.json");
    }

    private static async Task<JObject> LoadLangJsonAsync(string fileName)
    {
        try
        {
            string path = Application.streamingAssetsPath + "/Lang/" + fileName;
            string json = "";

            bool useRequest =
                Application.platform == RuntimePlatform.Android ||
                path.StartsWith("jar:") ||
                path.StartsWith("file://");

            if (useRequest)
            {
                using (var request = UnityWebRequest.Get(path))
                {
                    var op = request.SendWebRequest();
                    while (!op.isDone)
                        await Task.Yield();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Log($"[Load Error] {path} : {request.error}");
                        return new JObject();
                    }

                    json = request.downloadHandler.text;
                }
            }
            else
            {
                if (!File.Exists(path))
                {
                    Log($"[File Missing] {path}");
                    return new JObject();
                }

                json = await File.ReadAllTextAsync(path);
            }

            json = json.Trim('\uFEFF', '\u200B', '\0');

            return JObject.Parse(json);
        }
        catch (Exception e)
        {
            Log($"[Lang Load Error] {fileName} : {e.Message}");
            return new JObject();
        }
    }


    // ✅ 普通键值读取（主语言 + 中文兜底）
    public static string Get(string category, string key)
    {
        if (TryGetValue(_langData, category, key, out var value))
            return value;
        if (TryGetValue(_fallbackLangData, category, key, out var fallback))
            return fallback;
        return string.Empty;
    }

    private static bool TryGetValue(JObject data, string category, string key, out string value)
    {
        value = string.Empty;
        if (data == null) return false;

        if (data.TryGetValue(category, out var catToken) && catToken is JObject catObj)
        {
            if (catObj.TryGetValue(key, out var token))
            {
                value = token.ToString();
                return true;
            }
        }
        return false;
    }

    // ==================== 读取简单数组 ["a","b","c"] ====================
    public static string GetArrayValue(string category, int index)
    {
        var list = GetArrayInternal(_langData, category);
        if (list.Count == 0)
            list = GetArrayInternal(_fallbackLangData, category);

        if (index < 0 || index >= list.Count)
            return "未知";

        return list[index]?.ToString() ?? "未知";
    }

    private static List<object> GetArrayInternal(JObject data, string category)
    {
        var result = new List<object>();
        if (data == null) return result;

        if (data.TryGetValue(category, out var token) && token is JArray array)
        {
            foreach (var item in array)
                result.Add(item.ToObject<object>());
        }
        return result;
    }

    // ==================== 读取对象数组 [{"a":"b"}] ====================
    public static List<Dictionary<string, string>> GetArray(string category)
    {
        var result = GetArrayObjectInternal(_langData, category);
        if (result.Count == 0)
            result = GetArrayObjectInternal(_fallbackLangData, category);

        return result;
    }

    private static List<Dictionary<string, string>> GetArrayObjectInternal(JObject data, string category)
    {
        var result = new List<Dictionary<string, string>>();
        if (data == null) return result;

        if (data.TryGetValue(category, out var token) && token is JArray array)
        {
            foreach (var item in array)
            {
                if (item is JObject obj)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var prop in obj.Properties())
                        dict[prop.Name] = prop.Value.ToString();
                    result.Add(dict);
                }
            }
        }
        return result;
    }

    // ==================== 读取二维数组 [["旺","生"], ["死","退"]] ====================
    public static List<List<string>> Get2DArray(string category)
    {
        var result = Get2DArrayInternal(_langData, category);
        if (result.Count == 0)
            result = Get2DArrayInternal(_fallbackLangData, category);

        return result;
    }

    private static List<List<string>> Get2DArrayInternal(JObject data, string category)
    {
        var result = new List<List<string>>();
        if (data == null) return result;

        if (data.TryGetValue(category, out var token) && token is JArray outerArray)
        {
            foreach (var inner in outerArray)
            {
                if (inner is JArray innerArray)
                {
                    var row = new List<string>();
                    foreach (var val in innerArray)
                        row.Add(val.ToString());
                    result.Add(row);
                }
            }
        }

        return result;
    }

}
