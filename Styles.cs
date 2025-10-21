using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.Diagnostics;
using System.Numerics;
using Debug = UnityEngine.Debug;

public class Styles : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private GameObject quad;
    [SerializeField] private float imageDisplayTime = 10f;

    private string[] mediaFiles;
    private int currentIndex;
    private readonly Dictionary<string, Texture2D> textureCache = new();

    // holiday 为节日名称（如“春节”），为空表示普通日
    public string InitializeEffect(string imageFolder, string holiday)
    {
        if (quad == null) quad = GameObject.Find("Quad");
        if (videoPlayer == null) videoPlayer = GameObject.Find("VideoAD")?.GetComponent<VideoPlayer>();
        if (quad == null || targetMaterial == null || videoPlayer == null)
            return "缺少必要组件（Quad / VideoAD / Material）";

        SetupQuad();
        SetupVideoPlayer();

        string path = Path.Combine(Application.streamingAssetsPath, imageFolder);
        if (!Directory.Exists(path)) return $"路径不存在: {path}";

        var allFiles = Directory.GetFiles(path)
            .Where(f => IsSupportedMedia(f))
            .ToArray();

        if (allFiles.Length == 0) return "未找到任何可播放媒体文件";

        // ✅ 简化筛选逻辑
        if (!string.IsNullOrEmpty(holiday))
        {
            // 1. 节日文件（名称包含节日关键字）
            var holidayFiles = allFiles
                .Where(f => Path.GetFileNameWithoutExtension(f).Contains(holiday))
                .ToList();

            // 2. 普通文件（名称包含 normal 或 common）
            var normalFiles = allFiles
                .Where(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("normal")
                         || Path.GetFileNameWithoutExtension(f).ToLower().Contains("common"))
                .ToList();

            // 3. 合并播放
            mediaFiles = holidayFiles.Concat(normalFiles).OrderBy(_ => Guid.NewGuid()).ToArray();
        }
        else
        {
            // 普通日只播放普通文件
            mediaFiles = allFiles
                .Where(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("normal")
                         || Path.GetFileNameWithoutExtension(f).ToLower().Contains("common"))
                .OrderBy(_ => Guid.NewGuid())
                .ToArray();

            // 如果没有 normal/common，就播放全部
            if (mediaFiles.Length == 0)
                mediaFiles = allFiles;
        }

        currentIndex = 0;
        PlayCurrent();
        quad.SetActive(true);
        return "播放初始化完成";
    }

    private void SetupQuad()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float distance = 5f;
        float height = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        quad.transform.localScale = new UnityEngine.Vector3(width, height, 1);
        quad.transform.position = cam.transform.position + cam.transform.forward * distance;
        quad.transform.rotation = cam.transform.rotation;
    }

    private void SetupVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = Camera.main;
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private static bool IsSupportedMedia(string file)
    {
        string ext = Path.GetExtension(file).ToLower();
        return ext is ".mp4" or ".mov" or ".png" or ".jpg";
    }

    public void ResetEffect()
    {
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying) videoPlayer.Stop();
            videoPlayer.enabled = false;
        }

        if (quad != null) quad.SetActive(false);

        textureCache.Clear();
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    #region 播放逻辑
    private void PlayCurrent()
    {
        if (mediaFiles == null || mediaFiles.Length == 0) return;

        string file = mediaFiles[currentIndex];
        string ext = Path.GetExtension(file).ToLower();

        CancelInvoke(nameof(NextMedia));

        if (ext is ".mp4" or ".mov")
        {
            StopAllCoroutines();
            quad.SetActive(false);
            videoPlayer.url = file;
            videoPlayer.enabled = true;
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Stop();
            StartCoroutine(ShowImage(file));
        }
    }

    private void OnVideoFinished(VideoPlayer vp) => NextMedia();

    private void NextMedia()
    {
        currentIndex = (currentIndex + 1) % mediaFiles.Length;
        PlayCurrent();
    }

    private IEnumerator ShowImage(string file)
    {
        quad.SetActive(true);
        Texture2D tex = GetOrLoadTexture(file);
        if (tex != null) targetMaterial.mainTexture = tex;

        yield return new WaitForSeconds(imageDisplayTime);
        NextMedia();
    }

    private Texture2D GetOrLoadTexture(string path)
    {
        if (textureCache.TryGetValue(path, out var cached)) return cached;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            textureCache[path] = tex;
            return tex;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载图片失败: {path}\n{e}");
            return null;
        }
    }
    #endregion
}
