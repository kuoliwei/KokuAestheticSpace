using System;
using UnityEngine;
using UnityEngine.UI;

public class WebSocketMessageReceiveTest : MonoBehaviour
{
    [Header("WebSocket 客戶端")]
    [SerializeField] private WebSocketClient webSocketClient;
    public Text message;
    private void Start()
    {
        if (webSocketClient != null)
        {
            webSocketClient.OnMessageReceive.AddListener(OnMessageReceived);
            webSocketClient.OnConnected.AddListener(() => Debug.Log("[Test] WebSocket 已連線"));
            webSocketClient.OnConnectionError.AddListener(() => Debug.LogError("[Test] WebSocket 連線失敗"));
            webSocketClient.OnDisconnected.AddListener(() => Debug.LogWarning("[Test] WebSocket 已斷線"));
        }
    }

    private void OnMessageReceived(string messageContent)
    {
        DateTime now = DateTime.Now;

        // 時分秒用 DateTime，秒的小數部分自己拼上去
        string timeStr = $"{now:HH:mm}:{now.Second + now.Millisecond / 1000.0:F6}";

        Debug.Log(
            $"[Time] 收到時間 {timeStr}" +
            $"[Test] 收到訊息:\n{messageContent}\n\n"
        );
        message.text = $"[Time] 收到時間 {timeStr}" +
            $"[Test] 收到訊息:\n{messageContent}\n\n";
    }

    public void ConnectToServer(string ip, string port)
    {
        string address = $"ws://{ip}:{port}";
        Debug.Log($"[Test] 嘗試連線 {address}");
        webSocketClient.CloseConnection();
        webSocketClient.StartConnection(address);
    }
}
