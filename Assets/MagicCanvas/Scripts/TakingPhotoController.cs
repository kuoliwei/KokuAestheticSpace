using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TakingPhotoController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WebCamController webCam;   // 指到你現有的 WebCamController
    [SerializeField] private RawImage previewRawImage;  // TakingPhotoPanel 上顯示預覽的 RawImage

    // TakingPhotoController.cs（在 CapturePhotoToPreview() 結尾）
    public event Action OnCaptureCompleted;  // ← 新增：無參數事件

    [Header("Countdown")]
    [SerializeField] private Text countdownText;     // 指到面板上的 Text（可選）
    [SerializeField] private int countdownSeconds = 5;
    // 既有欄位略…
    [SerializeField] private int aftershotSeconds = 2;   // 拍完後停留秒數（預設 1 秒）
    private Coroutine aftershotRoutine;                  // 專用：拍後倒數
    private Texture2D lastPhoto;
    public Texture2D LastPhoto => lastPhoto;  // 需要時可取用
    private Coroutine countdownRoutine;

    // 由 PanelFlowController 在「進入 TakingPhoto 狀態」時呼叫
    public void Enter()
    {
        if (webCam == null) return;
        // 開相機並綁預覽
        webCam.OpenCamera(webCam.selectedDeviceName);
        if (previewRawImage != null) previewRawImage.texture = webCam.PreviewTexture;
        SetCountdownText(""); // 清空顯示
        //StartCaptureCountdown();
    }

    // 由 PanelFlowController 在「離開 TakingPhoto 狀態」時呼叫
    public void Exit()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
        if (aftershotRoutine != null) { StopCoroutine(aftershotRoutine); aftershotRoutine = null; }
        if (webCam == null) return;
        webCam.CloseCamera();
        SetCountdownText("");
    }

    // ====== 給 UI 按鈕用（無參數適合 Inspector 綁定） ======

    // 手動開鏡頭（若你想用按鈕控制，而不是由狀態機進面板時自動開）
    public void UI_OnOpenPreview()
    {
        Enter();
    }
    // PanelFlowController 呼叫：開始倒數，結束後自動拍照並顯示於 previewRawImage
    public void StartCaptureCountdown()
    {
        //Debug.Log("呼叫StartCaptureCountdown()");
        if (countdownRoutine != null) return;           // 防止重複觸發
        if (webCam == null || previewRawImage == null) return;

        countdownRoutine = StartCoroutine(Co_CountdownThenCapture());
    }
    //（可選）提供取消倒數的按鈕
    public void CancelCaptureCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
            SetCountdownText("");
        }
    }
    IEnumerator Co_CountdownThenCapture()
    {
        int t = Mathf.Max(1, countdownSeconds);
        while (t > 0)
        {
            SetCountdownText(t.ToString());
            yield return new WaitForSeconds(1f);
            t--;
        }
        SetCountdownText("");           // 清掉數字
        countdownRoutine = null;

        // 倒數結束 → 擷取並顯示
        CapturePhotoToPreview();
    }
    private void SetCountdownText(string s)
    {
        if (countdownText != null) countdownText.text = s;
    }
    // 拍照
    public void CapturePhotoToPreview()
    {
        //Debug.Log("呼叫CapturePhotoToPreview()");
        if (webCam == null || previewRawImage == null) return;

        var photo = webCam.CaptureFrame();
        if (photo == null) return;

        webCam.CloseCamera();

        if (lastPhoto != null) Destroy(lastPhoto);
        lastPhoto = photo;

        previewRawImage.texture = lastPhoto;
        SaveTextureAsJPG(lastPhoto);
        StartAftershotDelay();
    }
    public void StartAftershotDelay(int seconds = -1)
    {
        if (aftershotRoutine != null) return;                                // 防重入
        int wait = seconds > 0 ? seconds : Mathf.Max(0, aftershotSeconds);
        aftershotRoutine = StartCoroutine(Co_AftershotDelay(wait));
    }

    private IEnumerator Co_AftershotDelay(int seconds)
    {
        if (seconds > 0) yield return new WaitForSeconds(seconds);
        aftershotRoutine = null;
        OnCaptureCompleted?.Invoke();                                        // 現在才通知外部切下一步
    }
    //（可留作備用）
    public void UI_OnCapture() => CapturePhotoToPreview();

    // 手動關鏡頭（若你想用按鈕控制）
    public void UI_OnClosePreview()
    {
        Exit();
    }
    // 儲存輸入的 Texture2D 成 .jpg 檔
    public async void SaveTextureAsJPG(Texture2D tex)
    {
        string filePath = Path.Combine(Application.dataPath, "test.jpg");
        if (tex == null)
        {
            Debug.LogWarning("SaveTextureAsJPG: 輸入的 Texture2D 為 null，無法存檔。");
            return;
        }

        try
        {
            byte[] bytes = tex.EncodeToJPG();
            await File.WriteAllBytesAsync(filePath, bytes);
            Debug.Log($"照片已存成 JPG：{filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"存檔失敗：{ex.Message}");
        }
    }
}
