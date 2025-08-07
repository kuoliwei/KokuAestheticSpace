using UnityEngine;

public class EscapeToReset : MonoBehaviour
{
    public WebSocketClient webSocketClient;
    public GameObject connectPanel;
    public ScratchCard scratchCard;
    public GameObject reconnectPanel; // ReconnectPanel（整個面板）
    public ReconnectPanelController reconnectUI; // 可選，用來關閉 coroutine 等

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("按下 ESC：強制返回主畫面");

            // 1. 關閉任何自動重連機制
            webSocketClient.allowReconnect = false;
            webSocketClient.isReconnectAttempt = false;
            webSocketClient.CloseConnection();

            // 2. 隱藏 ReconnectPanel（如果還在顯示）
            if (reconnectPanel != null && reconnectPanel.activeSelf)
            {
                reconnectPanel.SetActive(false);
            }

            // 3. 停止閃爍協程
            if (reconnectUI != null)
            {
                reconnectUI.CancelAndHideImmediately(); // 正確地停止閃爍與 pending 關閉協程
            }

            // 4. 顯示 ConnectPanel
            connectPanel.SetActive(true);

            // 5. 重置 ScratchCard 畫面
            scratchCard.ResetScratch();
        }
    }
}
