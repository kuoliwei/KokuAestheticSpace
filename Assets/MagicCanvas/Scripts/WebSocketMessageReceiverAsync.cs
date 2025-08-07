using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WebSocketMessageReceiverAsync : MonoBehaviour
{
    [Header("連接 UI 元件")]
    [SerializeField] private WebSocketConnectUI webSocketConnectUI;
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private ReconnectPanelController reconnectUI;

    [Header("WebSocket 客戶端")]
    [SerializeField] private WebSocketClient webSocketClient;

    [Header("是否允許處理 Brush 資料")]
    public bool CanReceiveBrushMessage = true;

    [System.Serializable]
    public class BrushUpdateEvent : UnityEvent<List<BrushData>> { }
    [Header("接收 Brush 資料事件")]
    public BrushUpdateEvent OnBrushDataReceived = new();
    private readonly ConcurrentQueue<List<BrushData>> mainThreadQueue = new();
    private int messagesProcessedThisSecond = 0;
    private float messageTimer = 0f;

    private void Start()
    {
        if (webSocketClient != null)
        {
            webSocketClient.OnMessageReceive.AddListener(message =>
            {
                //receivedCountThisSecond++;
                ReceiveMessage(message);
            });
            webSocketClient.OnConnected.AddListener(OnWebSocketConnected);
            webSocketClient.OnConnectionError.AddListener(webSocketConnectUI.OnConnectionFaild);
            webSocketClient.OnDisconnected.AddListener(OnWebSocketDisconnected);
        }
    }

    private void Update()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    // 1. 建立模擬 BrushData 的 JSON 字串
        //    Vector2 mousePos = Input.mousePosition;
        //    Vector2 normalized = new Vector2(
        //        mousePos.x / Screen.width,
        //        1f - (mousePos.y / Screen.height)
        //    );

        //    string json = $"{{\"data\":[{{\"roller_id\":0,\"point\":[{normalized.x},{normalized.y}]}}]}}";
        //    Debug.Log("normalized.y :" + normalized.y);
        //    // 2. 呼叫 WebSocketMessageReceiverAsync 處理 JSON
        //    SendMessageManually(json);

        //    //scratchCount++;
        //}
        int processed = 0;
        {//限制每幀資料筆數
            //int maxProcessPerFrame = 128;
            //while (processed < maxProcessPerFrame && mainThreadQueue.TryDequeue(out var brushList))
            //{
            //    OnBrushDataReceived.Invoke(brushList);

            //    processed++;
            //}
        }

        {//不限制每幀資料筆數
            while (mainThreadQueue.TryDequeue(out var brushList))
            {
                OnBrushDataReceived.Invoke(brushList);

                processed++;
            }
        }

        // 監控
        messageTimer += Time.deltaTime;
        if (messageTimer >= 1f)
        {
            //Debug.Log($"[監控] 本幀處理 {processed} 組 brushList。Queue 剩餘：{mainThreadQueue.Count}");
            messageTimer = 0f;
        }
    }

    private void ReceiveMessage(string messageContent)
    {
        //Debug.Log("[ReceiveMessage] 函式有被呼叫");

        if (!CanReceiveBrushMessage)
        {
            Debug.LogWarning("忽略訊息：CanReceiveBrushMessage 為 false");
            return;
        }

        //Debug.Log($"[ReceiveMessage] 收到訊息內容：{messageContent}");

        try
        {
            var newBrushMessage = JsonConvert.DeserializeObject<BrushPositionJson>(messageContent);
            if (newBrushMessage == null)
            {
                Debug.LogError("[ReceiveMessage] JSON 解析失敗！");
                return;
            }

            if (newBrushMessage.data == null || newBrushMessage.data.Count == 0)
            {
                Debug.LogWarning("[ReceiveMessage] JSON 解析成功但 data 為空");
                return;
            }

            //Debug.Log($"[ReceiveMessage] 成功解析，共 {newBrushMessage.data.Count} 筆 BrushData");
            mainThreadQueue.Enqueue(newBrushMessage.data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Can't deserialize message: {messageContent}. Error: {e.Message}");
        }
    }

    private void OnWebSocketDisconnected()
    {
        if (!connectPanel.activeSelf)
        {
            reconnectUI?.ShowFlicker();
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
        reconnectUI?.ShowSuccessAndHide();

        if (connectPanel != null)
        {
            connectPanel.SetActive(false);
        }

        webSocketClient.allowReconnect = false;

        if (webSocketClient.isReconnectAttempt)
        {
            Debug.Log("重新連線成功");
            webSocketClient.isReconnectAttempt = false;
        }
    }

    /// <summary>
    /// 提供 UI 輸入連線資訊的接口
    /// </summary>
    public void ConnectToServer(string ip, string port)
    {
        string address = $"ws://{ip}:{port}";
        Debug.Log($"[WebSocketReceiverAsync] Connecting to: {address}");

        webSocketClient.CloseConnection();
        webSocketClient.StartConnection(address);
    }

    public void SendMessageManually(string json)
    {
        Debug.Log($"[Receiver] SendMessageManually 呼叫中");

        if (!CanReceiveBrushMessage)
        {
            //Debug.LogWarning("[Receiver] 忽略訊息：CanReceiveBrushMessage 為 false");
            return;
        }

        ReceiveMessage(json);
    }
}

// class BrushPositionJson { public List<BrushData> data; }
// class BrushData { public int roller_id; public List<float> point; }
