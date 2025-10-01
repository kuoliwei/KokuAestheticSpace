using System;
using UnityEngine;
using UnityEngine.UI;

public class WebSocketMessageReceiveTest : MonoBehaviour
{
    [Header("WebSocket �Ȥ��")]
    [SerializeField] private WebSocketClient webSocketClient;
    public Text message;
    private void Start()
    {
        if (webSocketClient != null)
        {
            webSocketClient.OnMessageReceive.AddListener(OnMessageReceived);
            webSocketClient.OnConnected.AddListener(() => Debug.Log("[Test] WebSocket �w�s�u"));
            webSocketClient.OnConnectionError.AddListener(() => Debug.LogError("[Test] WebSocket �s�u����"));
            webSocketClient.OnDisconnected.AddListener(() => Debug.LogWarning("[Test] WebSocket �w�_�u"));
        }
    }

    private void OnMessageReceived(string messageContent)
    {
        DateTime now = DateTime.Now;

        // �ɤ���� DateTime�A���p�Ƴ����ۤv���W�h
        string timeStr = $"{now:HH:mm}:{now.Second + now.Millisecond / 1000.0:F6}";

        Debug.Log(
            $"[Time] ����ɶ� {timeStr}" +
            $"[Test] ����T��:\n{messageContent}\n\n"
        );
        message.text = $"[Time] ����ɶ� {timeStr}" +
            $"[Test] ����T��:\n{messageContent}\n\n";
    }

    public void ConnectToServer(string ip, string port)
    {
        string address = $"ws://{ip}:{port}";
        Debug.Log($"[Test] ���ճs�u {address}");
        webSocketClient.CloseConnection();
        webSocketClient.StartConnection(address);
    }
}
