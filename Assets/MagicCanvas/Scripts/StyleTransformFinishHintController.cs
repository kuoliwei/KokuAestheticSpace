using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class StyleTransformFinishHintController : MonoBehaviour
{
    [SerializeField] private Text countdownText;  // UI 顯示剩餘秒數（可選）
    //private int staySeconds = 5; // 預設停留時間

    private Coroutine routine;

    // 當倒數完成，通知外部（PanelFlowController）切換
    public event Action OnFinishCountdown;

    public void BeginCountdown(int seconds)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Co_Countdown(seconds));
    }

    public void CancelCountdown()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (countdownText != null) countdownText.text = "";
    }

    private IEnumerator Co_Countdown(int seconds)
    {
        int t = seconds;
        while (t > 0)
        {
            if (countdownText != null) countdownText.text = t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }
        if (countdownText != null) countdownText.text = "";
        routine = null;
        OnFinishCountdown?.Invoke();
    }
}
