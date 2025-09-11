using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class StyleTransformFinishHintController : MonoBehaviour
{
    [SerializeField] private Text countdownText;  // UI ��ܳѾl��ơ]�i��^
    [SerializeField] private int staySeconds = 5; // �w�]���d�ɶ�

    private Coroutine routine;

    // ��˼Ƨ����A�q���~���]PanelFlowController�^����
    public event Action OnFinishCountdown;

    public void BeginCountdown(int seconds = -1)
    {
        if (routine != null) StopCoroutine(routine);
        int wait = seconds > 0 ? seconds : staySeconds;
        routine = StartCoroutine(Co_Countdown(wait));
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
