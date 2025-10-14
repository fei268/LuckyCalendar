using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

public static class LangManager
{
    private static Dictionary<string, object> _langData;           // 当前语言（如英文）
    private static Dictionary<string, object> _fallbackLangData;   // 默认中文语言

    public static void Load(string langCode)
    {
        _langData = LoadLangFile($"lang_{langCode}.json");
        _fallbackLangData = LoadLangFile("lang_zh.json");
    }

    private static Dictionary<string, object> LoadLangFile(string fileName)
    {
        try
        {
            string path = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Lang", fileName);
            if (!File.Exists(path)) return new Dictionary<string, object>();

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    // ✅ 普通键值读取（主语言 + 中文兜底）
    public static string Get(string category, string key)
    {
        if (TryGetValue(_langData, category, key, out var value))
        {
            return value;
        }
        if (TryGetValue(_fallbackLangData, category, key, out var fallbackValue))
        {
            return fallbackValue;
        }
            
        return string.Empty;
    }

    private static bool TryGetValue(Dictionary<string, object> data, string category, string key, out string value)
    {
        value = string.Empty;
        if (data == null) return false;

        if (data.TryGetValue(category, out var obj))
        {
            if (obj is JObject jObj)
            {
                if (jObj.TryGetValue(key, out var token))
                {
                    value = token.ToString();
                    return true;
                }
            }
            else if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(key, out var val))
                {
                    value = val?.ToString() ?? string.Empty;
                    return true;
                }
            }
        }
        return false;
    }

    // ✅ 读取 ["1", "2", ...] 形式的数组（主语言 + 中文兜底）
    public static string GetArrayValue(string category, int index)
    {
        var list = GetArrayInternal(_langData, category);
        if (list == null || list.Count == 0)
            list = GetArrayInternal(_fallbackLangData, category);

        if (list == null || index < 0 || index >= list.Count)
            return "未知";

        return list[index]?.ToString() ?? "未知";
    }

    // ✅ 读取 [{"1":"1","2":"1"}, {...}] 形式的数组（主语言 + 中文兜底）
    public static List<Dictionary<string, object>> GetArray(string category)
    {
        var result = GetArrayObjectInternal(_langData, category);
        if (result.Count == 0)
            result = GetArrayObjectInternal(_fallbackLangData, category);

        return result;
    }

    // 🔹 内部方法：读取简单数组 ["a","b","c"]
    private static List<object> GetArrayInternal(Dictionary<string, object> data, string category)
    {
        if (data == null) return new List<object>();

        if (data.TryGetValue(category, out var obj))
        {
            if (obj is JArray jArray)
                return jArray.ToObject<List<object>>() ?? new List<object>();
            if (obj is List<object> list)
                return list;
        }

        return new List<object>();
    }

    // 🔹 内部方法：读取对象数组 [{"a":"b"}]
    private static List<Dictionary<string, object>> GetArrayObjectInternal(Dictionary<string, object> data, string category)
    {
        var result = new List<Dictionary<string, object>>();
        if (data == null) return result;

        if (data.TryGetValue(category, out var token))
        {
            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject obj)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in obj.Properties())
                        {
                            dict[prop.Name] = prop.Value.ToString();
                        }
                        result.Add(dict);
                    }
                }
            }
        }

        return result;
    }

    // 读取 [["旺","生"], ["死","退"]] 形式的二维数组
    public static List<List<string>> Get2DArray(string category)
    {
        var result = new List<List<string>>();
        if (_langData != null && _langData.TryGetValue(category, out var token))
        {
            if (token is JArray array)
            {
                foreach (var inner in array)
                {
                    if (inner is JArray innerArray)
                    {
                        var row = innerArray.ToObject<List<string>>();
                        if (row != null) result.Add(row);
                    }
                }
            }
        }

        // 如果主语言没有，再读取中文兜底
        if (result.Count == 0 && _fallbackLangData != null && _fallbackLangData.TryGetValue(category, out var fallbackToken))
        {
            if (fallbackToken is JArray array)
            {
                foreach (var inner in array)
                {
                    if (inner is JArray innerArray)
                    {
                        var row = innerArray.ToObject<List<string>>();
                        if (row != null) result.Add(row);
                    }
                }
            }
        }

        return result;
    }

}
