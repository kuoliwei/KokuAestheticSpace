using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;

public class ImageStyleTransferHandler : MonoBehaviour
{
    [Header("Server")]
    //public string ipAddress = "https://d0bb5976a900.ngrok-free.app"; // 後端 Base URL（不要結尾斜線）
    //private string ipAddress = "http://10.66.66.54:3000";
    private string ipAddress = "http://192.168.50.160:3000";
    [Header("Polling")]
    [Tooltip("是否使用同事版協議（不帶 task_id 的全域輪詢）")]
    public bool pollWithoutTaskId = true;
    [Tooltip("輪詢間隔秒數")]
    public float pollIntervalSeconds = 0.2f;
    [Tooltip("進度 100% 後延遲取得圖片的秒數")]
    public float getImageDelaySeconds = 1.0f;

    [SerializeField] private InputField photoUrlInput;
    private string taskId = null;
    private string imageUrl = null;
    private const float RequestTimeout = 10f;
    private void Start()
    {
        photoUrlInput.text = ipAddress;
    }
    public void OnInputFieldValueChanged()
    {
        ipAddress = photoUrlInput.text;
        Debug.Log($"ipAddress:{ipAddress}");
    }
    // ===== 1) 發送風格化請求 =====
    // 與你原本一致：傳 Base64 + style，後端若同時也支援 prompt，可再自行擴充
    public IEnumerator SendStyleRequest(string imageBase64, string style, Action<string> onComplete)
    {
        Debug.Log("呼叫SendStyleRequest()");
        string url = $"{ipAddress}/comfyui/uploadimage2style";
        var requestBody = new { image64 = imageBase64, style = style };
        string jsonData = JsonConvert.SerializeObject(requestBody);

        Debug.Log($"[SendStyleRequest] 發送到: {url}");
        Debug.Log($"[SendStyleRequest] Body JSON 長度={jsonData.Length}, Style={style}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return SendRequestWithTimeout(request, RequestTimeout);

            var rawText = request.downloadHandler != null ? request.downloadHandler.text : null;
            Debug.Log($"[SendStyleRequest] 回應原始文字: {rawText}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseJson = JsonConvert.DeserializeObject<TaskResponse>(rawText);
                    taskId = responseJson?.task_id;
                    Debug.Log($"[SendStyleRequest] 任務建立成功，TaskID={taskId}");
                    onComplete?.Invoke(taskId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SendStyleRequest] 回應解析錯誤: {ex}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[SendStyleRequest] 發送失敗: {request.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    // ===== 2) 檢查進度（改成同事版協議：不帶 task_id）=====
    public IEnumerator CheckProgress(Action<float> onProgressUpdate, Action onComplete)
    {
        string url = $"{ipAddress}/comfyui/uploadimagecheck";
        Debug.Log($"[CheckProgress] 開始檢查進度，URL={url}, 當前 TaskID={taskId}");

        while (true)
        {
            string jsonData =
                pollWithoutTaskId
                ? "{}"
                : JsonConvert.SerializeObject(new { task_id = taskId });

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                Debug.Log($"[CheckProgress] 發送查詢 Body={jsonData}");
                yield return SendRequestWithTimeout(request, RequestTimeout);

                var raw = request.downloadHandler != null ? request.downloadHandler.text : null;
                Debug.Log($"[CheckProgress] 原始回應: {raw}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var progressResponse = JsonConvert.DeserializeObject<ProgressResponse>(raw);
                        // 同事版協議：以 type == "Style-Transfer" 為目標事件
                        if (progressResponse != null &&
                            (string.IsNullOrEmpty(progressResponse.type) || progressResponse.type == "Style-Transfer"))
                        {
                            float p = progressResponse.progress;
                            onProgressUpdate?.Invoke(p);
                            Debug.Log($"[CheckProgress] Progress={p}");

                            if (p >= 100f)
                            {
                                Debug.Log("[CheckProgress] 已達 100%，進度完成");
                                onComplete?.Invoke();
                                yield break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CheckProgress] 回應解析錯誤: {ex}");
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"[CheckProgress] 查詢失敗: {request.error}");
                    yield break;
                }
            }

            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    // ===== 3) 下載生成的圖片（同事版：status/url，且延遲 getImageDelaySeconds）=====
    public IEnumerator DownloadImage(Action<Texture2D> onImageDownloaded)
    {
        if (getImageDelaySeconds > 0f)
            yield return new WaitForSeconds(getImageDelaySeconds);

        if (string.IsNullOrEmpty(taskId))
        {
            Debug.LogWarning("[DownloadImage] Task ID 為空，仍會嘗試依同事版協議取得圖片（部分後端不強制使用 task_id）");
        }

        string url = $"{ipAddress}/comfyui/getimage";
        var requestBody = new { task_id = taskId }; // 同事版仍保留 task_id 欄位
        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[DownloadImage] 發送請求到 {url}，Body={jsonData}");
            yield return SendRequestWithTimeout(request, RequestTimeout);

            var raw = request.downloadHandler != null ? request.downloadHandler.text : null;
            Debug.Log($"[DownloadImage] 原始回應: {raw}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                GetImageResponse res = null;
                try { res = JsonConvert.DeserializeObject<GetImageResponse>(raw); }
                catch (Exception ex)
                {
                    Debug.LogError($"[DownloadImage] 無法解析回應 JSON：{ex}");
                    onImageDownloaded?.Invoke(null);
                    yield break;
                }

                if (res != null && res.status == "Finish" && !string.IsNullOrEmpty(res.url))
                {
                    imageUrl = res.url;

                    // 手動替換內網域名為 ngrok 外網域名
                    imageUrl = imageUrl.Replace(
                        "http://192.168.50.160:3000",
                        ipAddress
                    );
                    Debug.Log($"[DownloadImage] 下載圖片 URL: {imageUrl}");

                    using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
                    {
                        Debug.Log($"[DownloadImage] 嘗試下載圖片: {imageUrl}");
                        yield return imageRequest.SendWebRequest();

                        if (imageRequest.result == UnityWebRequest.Result.Success)
                        {
                            Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);
                            Debug.Log("[DownloadImage] 成功下載圖片並轉換為 Texture2D");
                            onImageDownloaded?.Invoke(texture);
                        }
                        else
                        {
                            Debug.LogError($"[DownloadImage] 下載圖片失敗，錯誤: {imageRequest.error}");
                            onImageDownloaded?.Invoke(null);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[DownloadImage] 狀態不正確或缺少 url，status={res?.status}, url={res?.url}");
                    onImageDownloaded?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[DownloadImage] 取圖 API 請求失敗: {request.error}");
                onImageDownloaded?.Invoke(null);
            }
        }
    }

    // ===== 共用：帶逾時的送出 =====
    private IEnumerator SendRequestWithTimeout(UnityWebRequest request, float timeout)
    {
        var operation = request.SendWebRequest();
        float startTime = Time.time;

        while (!operation.isDone)
        {
            if (Time.time - startTime > timeout)
            {
                Debug.LogError("請求超時！");
                request.Abort();
                break;
            }
            yield return null;
        }
    }

    // ===== 回應資料模型 =====
    [Serializable] private class TaskResponse { public string task_id; }

    // 同事版進度：{ "type": "Style-Transfer", "progress": 100 }
    [Serializable] private class ProgressResponse { public float progress; public string type; }

    // 同事版取圖：{ "status": "Finish", "url": "http://..." }
    [Serializable] private class GetImageResponse { public string status; public string url; }
}
