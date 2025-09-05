using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChooseStyleController : MonoBehaviour
{
    [Header("Deps")]
    [SerializeField] private ImageStyleTransferHandler styleHandler; // 後端呼叫

    [Header("Source")]
    private Texture2D sourcePhoto;
    public Texture2D SourcePhoto => sourcePhoto;  // 唯讀屬性，對外公開

    public event Action<string> OnStyleFailed;                       // 通知外部：失敗訊息
                                                                     // 只回報任務建立的結果
    public event Action<string> OnStyleTaskCreated;     // 成功：回傳 taskId
    public event Action<string> OnStyleTaskFailed;      // 失敗：回傳原因

    private bool isSending;
    [Header("UI")]
    [SerializeField] private Text resultText;   // 指到你新增的錯誤訊息 Text
    // 對外方法，讓外部可以指定照片
    public void SetSourcePhoto(Texture2D tex)
    {
        sourcePhoto = tex;
    }
    // 給風格按鈕綁定（字串可由 Button 的 OnClick 傳入）
    // 風格按鈕 OnClick 綁這個（傳入 style 名稱）
    public void ChooseStyle(string styleName)
    {
        Debug.Log("呼叫ChooseStyle()");
        if (isSending) return;
        if (SourcePhoto == null) { OnStyleTaskFailed?.Invoke("沒有來源照片"); return; }
        StartCoroutine(Co_SendOnly(styleName, SourcePhoto));
    }

    private IEnumerator Co_SendOnly(string styleName, Texture2D photo)
    {
        Debug.Log("呼叫Co_SendOnly()");
        isSending = true;

        // 1) 壓 JPG → base64
        byte[] jpg = photo.EncodeToJPG(85);
        string image64 = Convert.ToBase64String(jpg);

        // 2) 送出請求，只取回 taskId
        string taskId = null;
        yield return styleHandler.SendStyleRequest(image64, styleName, tid => taskId = tid);

        if (string.IsNullOrEmpty(taskId))
        {
            OnStyleTaskFailed?.Invoke("建立風格化任務失敗");
            isSending = false;

            yield break;
        }

        // 成功：把 taskId 丟給外部（PanelFlowController / Waiting 面板）
        OnStyleTaskCreated?.Invoke(taskId);
        isSending = false;
    }
    public void ShowResult(string result)
    {
        if (resultText != null)
            resultText.text = result;           // 在面板上顯示
    }
}
