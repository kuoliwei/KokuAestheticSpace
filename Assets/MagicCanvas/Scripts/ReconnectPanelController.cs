using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ReconnectPanelController : MonoBehaviour
{
    public GameObject reconnectPanel;
    public Text statusText;

    private Coroutine flickerCoroutine;

    public void ShowFlicker()
    {
        reconnectPanel.SetActive(true);
        statusText.text = "已斷線，嘗試重新連線中";
        flickerCoroutine = StartCoroutine(FlickerText());
    }

    public void ShowSuccessAndHide()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        statusText.text = "重新連線成功";
        StartCoroutine(AutoHideAfterDelay(1f)); // 1秒後自動隱藏
    }

    private IEnumerator FlickerText()
    {
        while (true)
        {
            statusText.enabled = !statusText.enabled;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        reconnectPanel.SetActive(false);
    }
    public void CancelAndHideImmediately()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        StopAllCoroutines(); // 保險也把 AutoHide 協程停掉
        reconnectPanel.SetActive(false);
    }
}
