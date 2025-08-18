using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ComfyUIClientUnity : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("後端 Base URL，不要結尾斜線")]
    public string baseUrl = "http://localhost:3000";

    [Header("UI")]
    public TMP_Dropdown styleDropdown;      // 選 Style1 / Style2 / Style3
    public TMP_InputField promptInput;      // 預設 "photo of a human"
    public TMP_InputField imagePathInput;   // 你現在先傳 'none'，可留作未來擴充
    public Button sendButton;
    public Button stopWatchButton;
    public Slider progressBar;              // 建議 maxValue = 100
    public TMP_Text statusText;
    public RawImage resultImage;            // 顯示生成圖

    [Header("Polling")]
    public float pollIntervalSeconds = 0.2f;  // 200ms
    public float getImageDelaySeconds = 1.0f; // 進度 100% 後延遲取圖

    // ---- runtime state ----
    private string _taskId = null;
    private bool _waiting = false;
    private Coroutine _pollRoutine = null;

    [Serializable]
    private class UploadReq
    {
        public string style;
        public string image;
        public string prompt;
    }

    [Serializable]
    private class UploadRes
    {
        public string task_id;
    }

    [Serializable]
    private class CheckRes
    {
        public string type;          // 期待 "Style-Transfer"
        public float progress;       // 0~100
        // public CheckData data;     // 若後端還有 data 可擴充
    }

    [Serializable]
    private class GetImageReq
    {
        public string task_id;
    }

    [Serializable]
    private class GetImageRes
    {
        public string status;   // 期待 "Finish"
        public string url;      // 圖片 URL
    }

    private void Awake()
    {
        if (sendButton != null) sendButton.onClick.AddListener(SendImageStyle);
        if (stopWatchButton != null) stopWatchButton.onClick.AddListener(StopWatchGenImage);

        if (promptInput != null && string.IsNullOrWhiteSpace(promptInput.text))
            promptInput.text = "photo of a human";

        if (styleDropdown != null && styleDropdown.options.Count == 0)
        {
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("Style1"));
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("Style2"));
            styleDropdown.options.Add(new TMP_Dropdown.OptionData("Style3"));
            styleDropdown.value = 0;
        }

        if (progressBar != null)
        {
            if (progressBar.maxValue != 100) progressBar.maxValue = 100;
            progressBar.value = 0;
        }

        SetStatus("Idle");
    }

    private void OnDisable()
    {
        StopWatchGenImage();
    }

    // === 1) 送出風格化任務 ===
    public void SendImageStyle()
    {
        if (_waiting)
        {
            SetStatus("Already running…");
            return;
        }

        string style = SafeGetCurrentStyle();
        string prompt = promptInput != null ? promptInput.text : "photo of a human";
        // 目前前端傳 "image":"none"
        string image = (imagePathInput != null && !string.IsNullOrWhiteSpace(imagePathInput.text))
            ? imagePathInput.text
            : "none";

        var req = new UploadReq { style = style, image = image, prompt = prompt };
        string url = $"{baseUrl}/comfyui/uploadimage2style";
        StartCoroutine(PostJson(url, JsonUtility.ToJson(req), (ok, json) =>
        {
            if (!ok)
            {
                SetStatus("Upload failed.");
                return;
            }

            UploadRes res = null;
            try { res = JsonUtility.FromJson<UploadRes>(json); }
            catch { /* ignore */ }

            if (res != null && !string.IsNullOrEmpty(res.task_id))
            {
                _taskId = res.task_id;
                _waiting = true;
                SetStatus($"Task queued: {_taskId}");
                StartWatchGenImage();
            }
            else
            {
                SetStatus("No task_id in response.");
            }
        }));
    }

    private string SafeGetCurrentStyle()
    {
        if (styleDropdown != null && styleDropdown.options.Count > 0)
            return styleDropdown.options[styleDropdown.value].text;
        return "Style1";
    }

    // === 2) 啟動輪詢 ===
    private void StartWatchGenImage()
    {
        StopWatchGenImage(); // 保險先停
        _pollRoutine = StartCoroutine(PollRoutine());
    }

    private IEnumerator PollRoutine()
    {
        string url = $"{baseUrl}/comfyui/uploadimagecheck";

        while (true)
        {
            yield return PostJsonYield(url, "{}", (ok, json) =>
            {
                if (!ok)
                {
                    SetStatus("Check failed.");
                    return;
                }

                CheckRes res = null;
                try { res = JsonUtility.FromJson<CheckRes>(json); }
                catch { /* ignore */ }

                if (res != null && res.type == "Style-Transfer")
                {
                    if (progressBar != null) progressBar.value = res.progress;
                    SetStatus($"Progress: {res.progress:0}%");

                    if (Mathf.Approximately(res.progress, 100f))
                    {
                        // 進度滿，延遲取圖
                        StartCoroutine(DelayedGetImage(getImageDelaySeconds));
                        StopWatchGenImage();
                    }
                }
                else
                {
                    // 不是我們要的事件就靜默；可在此擴充其他 type
                }
            });

            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    // === 3) 取得圖片 ===
    private IEnumerator DelayedGetImage(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!string.IsNullOrEmpty(_taskId))
            yield return GetImageByPromptId(_taskId);
    }

    private IEnumerator GetImageByPromptId(string taskId)
    {
        string url = $"{baseUrl}/comfyui/getimage";
        var req = new GetImageReq { task_id = taskId };

        yield return PostJsonYield(url, JsonUtility.ToJson(req), (ok, json) =>
        {
            if (!ok)
            {
                SetStatus("Get image failed.");
                return;
            }

            GetImageRes res = null;
            try { res = JsonUtility.FromJson<GetImageRes>(json); }
            catch { /* ignore */ }

            if (res != null && res.status == "Finish" && !string.IsNullOrEmpty(res.url))
            {
                if (progressBar != null) progressBar.value = 100;
                SetStatus("Finished. Downloading image…");
                StartCoroutine(DownloadAndShowImage(res.url));
            }
            else
            {
                SetStatus($"Status: {res?.status ?? "null"}");
            }
        });
    }

    // 下載圖片並顯示到 RawImage
    private IEnumerator DownloadAndShowImage(string imageUrl)
    {
        using (var uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return uwr.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                SetStatus($"Image download error: {uwr.error}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(uwr);
            if (resultImage != null)
            {
                resultImage.texture = tex;
                // 讓 RawImage 自適應長寬（可選）
                var r = resultImage.rectTransform;
                if (tex != null && r != null)
                {
                    float aspect = (float)tex.width / tex.height;
                    // 簡單等比：固定高度，調寬度
                    float h = r.sizeDelta.y;
                    if (h <= 0) h = 256;
                    r.sizeDelta = new Vector2(h * aspect, h);
                }
            }
            SetStatus("Image shown.");
            _waiting = false;
        }
    }

    // === 停止輪詢 ===
    public void StopWatchGenImage()
    {
        if (_pollRoutine != null)
        {
            StopCoroutine(_pollRoutine);
            _pollRoutine = null;
        }
    }

    // === 小工具：POST JSON ===
    private IEnumerator PostJsonYield(string url, string json, Action<bool, string> callback)
    {
        using (var req = MakeJsonRequest(url, json))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !(req.isNetworkError || req.isHttpError);
#endif
            string text = null;
            try { text = req.downloadHandler != null ? req.downloadHandler.text : null; } catch { }
            callback?.Invoke(ok, text);
        }
    }

    private IEnumerator PostJson(string url, string json, Action<bool, string> callback)
        => PostJsonYield(url, json, callback);

    private UnityWebRequest MakeJsonRequest(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        // Debug.Log(msg);
    }
}
