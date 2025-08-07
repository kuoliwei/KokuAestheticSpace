using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

public class WebSocketMessageReceiver : MonoBehaviour
{
    [SerializeField] WebSocketConnectUI webSocketConnectUI;
    [SerializeField] private GameObject connectPanel; // 拖入你的 UI 物件
    [SerializeField] private ReconnectPanelController reconnectUI; // 記得在 Inspector 指派

    [SerializeField] private WebSocketClient webSocketClient;
    public bool CanReceiveBrushMessage = true;

    [System.Serializable]
    public class BrushUpdateEvent : UnityEvent<List<BrushData>> { }
    public BrushUpdateEvent OnBrushDataReceived = new();

    private void Start()
    {
        webSocketClient.OnMessageReceive.AddListener(ReceiveMessage);
        webSocketClient.OnConnected.AddListener(OnWebSocketConnected);
        webSocketClient.OnConnectionError.AddListener(webSocketConnectUI.OnConnectionFaild);
        webSocketClient.OnDisconnected.AddListener(OnWebSocketDisconnected);
    }
    private void OnWebSocketDisconnected()
    {
        if (!connectPanel.activeSelf) // 只有在遊戲中才啟動 reconnect
        {
            reconnectUI.ShowFlicker(); // 顯示斷線提示
            webSocketClient.allowReconnect = true;
            webSocketClient.isReconnectAttempt = true;
            Debug.Log("掉線中，自動啟用重連機制");
        }
        else
        {
            Debug.Log("ConnectPanel 開啟中，不自動重連");
        }
    }

    private void OnWebSocketConnected()
    {
        reconnectUI.ShowSuccessAndHide(); // 顯示成功提示並自動收起

        if (connectPanel != null)
        {
            connectPanel.SetActive(false);
        }

        webSocketClient.allowReconnect = false;

        if (webSocketClient.isReconnectAttempt)
        {
            Debug.Log("重新連線成功");
            webSocketClient.isReconnectAttempt = false; // reset flag
        }
    }

    private void ReceiveMessage(string messageContent)
    {
        if (!CanReceiveBrushMessage) return;

        try
        {
            var newBrushMessage = JsonConvert.DeserializeObject<BrushPositionJson>(messageContent);
            if (newBrushMessage?.data != null)
            {
                OnBrushDataReceived.Invoke(newBrushMessage.data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Can't deserialize message: {messageContent}. Error: {e.Message}");
        }
    }

    /// <summary>
    /// 提供 UI 連線輸入用，格式：IP:Port
    /// </summary>
    public void ConnectToServer(string ip, string port)
    {
        string address = $"ws://{ip}:{port}";
        Debug.Log($"[WebSocketReceiver] Connecting to: {address}");

        webSocketClient.CloseConnection();         // 先關閉任何現有連線
        webSocketClient.StartConnection(address);  // 再建立新連線
    }

    // 測試手動送入資料用
    public void SendMessageManually(string json)
    {
        ReceiveMessage(json);
    }
}
