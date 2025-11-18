using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class HolidayImageLoader
{
    // GitHub Raw Base URL
    private const string githubRawBaseUrl =
        "https://raw.githubusercontent.com/fei268/Holidaypic/main/";

    private const string calendarJsonUrl =
        githubRawBaseUrl + "calendarpic.json";

    // 本地缓存目录
    private static readonly string localCachePath = Path.Combine(Application.persistentDataPath, "holidaypic");

    private JObject cachedJson;

    // =========================
    // 主方法：按月加载所有 Texture
    // =========================
    public async Task<Dictionary<int, List<Texture2D>>> LoadMonthTexturesAsync(int year, int month)
    {
        // 确保本地缓存文件夹存在
        Directory.CreateDirectory(Path.Combine(localCachePath, "holiday"));
        Directory.CreateDirectory(Path.Combine(localCachePath, "game"));
        Directory.CreateDirectory(Path.Combine(localCachePath, "ad"));

        // 1. 加载 JSON
        if (cachedJson == null)
            cachedJson = await LoadCalendarJsonAsync();
        if (cachedJson == null)
            return new Dictionary<int, List<Texture2D>>();

        Dictionary<int, List<Texture2D>> result = new Dictionary<int, List<Texture2D>>();

        // 2. 遍历 JSON 条目
        foreach (var prop in cachedJson.Properties())
        {
            string key = prop.Name;

            if (key == "holiday" || key == "lastUpdate")
                continue;

            // key 是日期 MM-dd
            if (!DateTime.TryParse($"{year}-{key}", out DateTime date))
                continue;
            if (date.Month != month)
                continue;

            int day = date.Day;

            List<string> urls = new List<string>();

            // 节假日匹配
            var holidaysList = cachedJson["holiday"] as JArray;
            if (holidaysList != null)
            {
                foreach (var h in holidaysList)
                {
                    string hStr = h.ToString();
                    if (hStr.Contains(key)) // 可改为更灵活匹配规则
                        urls.Add(githubRawBaseUrl + "holiday/" + hStr);
                }
            }

            // 当天 game/ad
            if (prop.Value["game"] is JArray games)
                urls.AddRange(games.Select(g => githubRawBaseUrl + "game/" + g));

            if (prop.Value["ad"] is JArray ads)
                urls.AddRange(ads.Select(a => githubRawBaseUrl + "ad/" + a));

            // 3. 下载或读取本地 Texture2D
            List<Texture2D> textures = new List<Texture2D>();
            foreach (var url in urls)
            {
                string localFile = GetLocalPath(url);
                if (!File.Exists(localFile))
                {
                    await DownloadFileAsync(url, localFile);
                }

                if (File.Exists(localFile))
                {
                    byte[] bytes = File.ReadAllBytes(localFile);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    textures.Add(tex);
                }
            }

            if (textures.Count > 0)
                result[day] = textures;
        }

        return result;
    }

    // -------------------------
    // 辅助：本地缓存路径
    // -------------------------
    private string GetLocalPath(string url)
    {
        string fileName = Path.GetFileName(url);
        if (url.Contains("/holiday/"))
            return Path.Combine(localCachePath, "holiday", fileName);
        else if (url.Contains("/game/"))
            return Path.Combine(localCachePath, "game", fileName);
        else if (url.Contains("/ad/"))
            return Path.Combine(localCachePath, "ad", fileName);
        else
            return Path.Combine(localCachePath, fileName);
    }

    // -------------------------
    // 辅助：下载文件
    // -------------------------
    private async Task DownloadFileAsync(string url, string savePath)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(savePath, www.downloadHandler.data);
            }
            else
            {
                Debug.LogWarning("Download failed: " + url);
            }
        }
    }

    // -------------------------
    // 辅助：加载 JSON
    // -------------------------
    private async Task<JObject> LoadCalendarJsonAsync()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(calendarJsonUrl))
        {
            await www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Load calendarpic.json failed: " + www.error);
                return null;
            }

            string json = www.downloadHandler.text;

            // 去掉 BOM 和零宽空格
            json = json.TrimStart('\uFEFF').Replace("\u200B", "");

            try
            {
                return JObject.Parse(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Parse calendarpic.json failed: " + e.Message);
                Debug.Log("Content: " + json.Substring(0, Mathf.Min(100, json.Length)));
                return null;
            }
        }
    }

}
