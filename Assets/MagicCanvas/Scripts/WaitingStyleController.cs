using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaitingStyleController : MonoBehaviour
{
    [Header("Deps")]
    [SerializeField] private ImageStyleTransferHandler styleHandler;
    [SerializeField] private RawImage resultRawImage;

    [Header("UI")]
    [SerializeField] private Text progressText;
    [SerializeField] private ProgressCircleController progressCircleController;

    private Coroutine progressRoutine;
    private Coroutine downloadRoutine;

    private string currentTaskId;
    private bool progressCompleted;

    // 事件：外部（PanelFlowController）可依這些事件決定下一步
    public event Action OnProgressCompleted;        // 只代表進度到 100%
    public event Action OnProgressNotCompleted;       // 新增：尚未完成（請外部再呼叫 BeginTracking）
    public event Action<string> OnProgressFailed;   // 查進度失敗/中止
    public event Action OnDownloadSucceeded;
    public event Action<string> OnDownloadFailed;

    // 由狀態機在進入 Waiting 面板後呼叫：只開始「查進度」
    public void BeginTracking(string taskId)
    {
        currentTaskId = taskId;
        progressCompleted = false;

        if (progressRoutine != null) StopCoroutine(progressRoutine);
        progressRoutine = StartCoroutine(Co_TrackProgressOnly());
    }

    // 由外部在「進度完成」後再呼叫：只負責下載
    public void BeginDownload()
    {
        if (!progressCompleted)
        {
            OnDownloadFailed?.Invoke("尚未完成，不能下載");
            return;
        }
        if (downloadRoutine != null) StopCoroutine(downloadRoutine);
        downloadRoutine = StartCoroutine(Co_DownloadOnly());
    }

    public void CancelAll()
    {
        if (progressRoutine != null) { StopCoroutine(progressRoutine); progressRoutine = null; }
        if (downloadRoutine != null) { StopCoroutine(downloadRoutine); downloadRoutine = null; }
        SetProgressText(string.Empty);
    }

    // ---------- coroutines ----------
    private int fakeProgress = 0;
    private IEnumerator Co_TrackProgressOnly()
    {
        bool completed = false;

        // 只輪詢進度，不下載
        {  //  呼叫檢查轉換進度的API
            yield return styleHandler.CheckProgress(
                p =>
                {
                    // p: 0~100
                    SetProgressText($"{Mathf.RoundToInt(p)}%");
                    progressCircleController.SetByPercentage(Mathf.RoundToInt(p));
                },
                () =>
                {
                    completed = true;
                    SetProgressText("100%");
                    progressCircleController.SetByPercentage(100);
                });
        }

        //{  //  用於測試百分比顯示
        //    if (fakeProgress < 100)
        //    {
        //        SetProgressText($"{fakeProgress.ToString()}%");
        //        progressCircleController.SetByPercentage(fakeProgress);
        //        OnProgressNotCompleted?.Invoke();
        //        fakeProgress = Mathf.Clamp(fakeProgress + 29, 0, 100);
        //        yield return new WaitForSeconds(1);
        //        yield break;
        //    }
        //    else if (fakeProgress >= 100)
        //    {
        //        SetProgressText($"{100.ToString()}%");
        //        progressCircleController.SetByPercentage(100);
        //        progressRoutine = null;
        //        progressCompleted = true;
        //        OnProgressCompleted?.Invoke();
        //        fakeProgress = 0;
        //        yield return new WaitForSeconds(1);
        //        yield break;
        //    }
        //}

        // 結束輪詢
        progressRoutine = null;

        // 你的期望：未達 100% 就安靜結束，不視為失敗、不發事件
        if (!completed)
        {
            OnProgressNotCompleted?.Invoke();  // 讓 PanelFlowController 再呼叫 BeginTracking
            yield break;
        }

        // 只有真正完成才標記 & 通知
        progressCompleted = true;
        OnProgressCompleted?.Invoke();
    }

    private IEnumerator Co_DownloadOnly()
    {
        Texture2D result = null;

        //{  //  測試用，直接讀取圖片並放至resultRawImage
        //    string filePath = Path.Combine(Application.dataPath, "test.jpg");
        //    result = LoadJPGAsTexture(filePath);
        //    if (resultRawImage != null) resultRawImage.texture = result;
        //    OnDownloadSucceeded?.Invoke(result);
        //    yield break;
        //}

        yield return styleHandler.DownloadImage(tex => result = tex);
        downloadRoutine = null;

        if (result == null)
        {
            OnDownloadFailed?.Invoke("下載風格化圖片失敗");
            yield break;
        }

        if (resultRawImage != null) resultRawImage.texture = result;
        OnDownloadSucceeded?.Invoke();
    }

    // ---------- helpers ----------
    private void SetProgressText(string t)
    {
        if (progressText != null) progressText.text = t;
    }
    /// <summary>
    /// 從本地路徑讀取 JPG 檔，轉換成 Texture2D
    /// </summary>
    /// <param name="filePath">檔案完整路徑，例如 Application.persistentDataPath + "/test.jpg"</param>
    /// <returns>成功返回 Texture2D，失敗返回 null</returns>
    public static Texture2D LoadJPGAsTexture(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogWarning($"LoadJPGAsTexture: 檔案不存在 {filePath}");
            return null;
        }

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // 建立一個空的 Texture2D，大小會在 LoadImage 時自動調整
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(fileData))
            {
                Debug.Log($"成功載入 JPG → Texture2D: {filePath}");
                return tex;
            }
            else
            {
                Debug.LogError("LoadImage 失敗，檔案可能損壞或格式錯誤");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"讀取 JPG 發生錯誤: {ex.Message}");
            return null;
        }
    }
}
