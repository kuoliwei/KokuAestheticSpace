using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ImageStyleTransferHandler : MonoBehaviour
{
    public string ipAddress = "http://ipaddress"; // API 的 IP 地址
    private string taskId = null;
    private string imageUrl = null;
    private const float RequestTimeout = 10f; // 超時秒數

    // 1. 發送風格化請求
    public IEnumerator SendStyleRequest(string imageBase64, string style, Action<string> onComplete)
    {
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

            Debug.Log($"[SendStyleRequest] 回應原始文字: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var responseJson = JsonConvert.DeserializeObject<TaskResponse>(request.downloadHandler.text);
                    taskId = responseJson.task_id;
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

    // 2. 檢查進度
    public IEnumerator CheckProgress(Action<float> onProgressUpdate, Action onComplete)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            Debug.LogError("[CheckProgress] Task ID 無效，無法檢查進度。");
            yield break;
        }

        string url = $"{ipAddress}/comfyui/uploadimagecheck";
        Debug.Log($"[CheckProgress] 開始檢查進度，URL={url}, TaskID={taskId}");

        while (true)
        {
            var requestBody = new { task_id = taskId };
            string jsonData = JsonConvert.SerializeObject(requestBody);

            Debug.Log($"[CheckProgress] 發送查詢 Body={jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return SendRequestWithTimeout(request, RequestTimeout);

                Debug.Log($"[CheckProgress] 原始回應: {request.downloadHandler.text}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var progressResponse = JsonConvert.DeserializeObject<ProgressResponse>(request.downloadHandler.text);
                        Debug.Log($"[CheckProgress] Progress={progressResponse.progress}");
                        onProgressUpdate?.Invoke(progressResponse.progress);

                        if (progressResponse.progress >= 100f)
                        {
                            Debug.Log("[CheckProgress] 已達 100%，進度完成");
                            onComplete?.Invoke();
                            break;
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
            yield return new WaitForSeconds(1);
        }
    }

    // 3. 下載生成的圖片
    public IEnumerator DownloadImage(Action<Texture2D> onImageDownloaded)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            Debug.LogError("Task ID 無效，無法下載圖片。");
            yield break;
        }

        string url = $"{ipAddress}/comfyui/getimage";
        var requestBody = new { task_id = taskId };
        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[DownloadImage] 發送請求到 {url}，Body={jsonData}");

            yield return SendRequestWithTimeout(request, RequestTimeout);

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 打印原始回應
                Debug.Log($"[DownloadImage] 原始回應: {request.downloadHandler.text}");

                var downloadResponse = JsonConvert.DeserializeObject<DownloadResponse>(request.downloadHandler.text);

                if (downloadResponse == null)
                {
                    Debug.LogError("[DownloadImage] 無法解析回應 JSON！");
                    yield break;
                }

                imageUrl = downloadResponse.url;
                Debug.Log($"[DownloadImage] 後端回傳圖片網址: {imageUrl}");

                if (string.IsNullOrEmpty(imageUrl))
                {
                    Debug.LogError("[DownloadImage] 後端回傳的 URL 為空！");
                    yield break;
                }

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
                        Debug.LogError($"[DownloadImage] 嘗試存取的 URL: {imageUrl}");
                        onImageDownloaded?.Invoke(null);
                    }
                }
            }
            else
            {
                Debug.LogError($"[DownloadImage] 獲取圖片 URL 失敗: {request.error}");
                Debug.LogError($"[DownloadImage] 原始回應: {request.downloadHandler.text}");
                onImageDownloaded?.Invoke(null);
            }
        }
    }

    // 超時處理的協程
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

    [Serializable]
    private class TaskResponse { public string task_id; }
    [Serializable]
    private class ProgressResponse { public float progress; public string type; }
    [Serializable]
    private class DownloadResponse { public string filename; public string url; }
}
