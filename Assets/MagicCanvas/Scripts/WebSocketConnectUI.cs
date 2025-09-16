using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System;

public class WebSocketConnectUI : MonoBehaviour
{
    [Header("UI 元件")]
    public Text message;
    public GameObject connectPanel;
    public InputField ipInput;
    public InputField portInput;
    public Button connectButton;

    private string ip = "127.0.0.1";
    private string port = "9999";
    //private string ip = "10.66.66.57";
    //private string port = "8765";
    //private string ip = "10.66.66.51";
    //private string port = "8765";
    [Header("連線接收器")]
    public WebSocketMessageReceiverAsync receiver;
    public event Action<bool, string> OnConnectResult; // true=成功；false=失敗；string=訊息
    private void Start()
    {
        // 若要預設可填在這裡（目前已註解）
        ipInput.text = ip;
        portInput.text = port;

        //connectButton.onClick.AddListener(OnClickConnect);
    }
    // 供 PanelFlowController 呼叫
    public void TryConnect()
    {
        //Debug.Log("呼叫TryConnect()");
        message.text = "";
        //string ip = this.ip;
        //string portText = this.port;
        string ip = ipInput.text;
        string portText = portInput.text;

        if (!System.Net.IPAddress.TryParse(ip, out _))
        {
            message.text += "IP 格式不正確\n";
            OnConnectResult?.Invoke(false, "IP 格式不正確");
            return;
        }
        if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
        {
            message.text += "Port 格式不正確（有效範圍：1~65535）";
            OnConnectResult?.Invoke(false, "Port 格式不正確");
            return;
        }

        // 交給接收器去連線；成功/失敗請回呼到下方兩個方法
        receiver.ConnectToServer(ip, portText);
    }
    // 讓接收器在「連線成功」時呼叫
    public void OnConnectionSucceeded()
    {
        //Debug.Log("呼叫OnConnectionSucceeded()");
        message.text = "連線成功";
        OnConnectResult?.Invoke(true, "連線成功");
    }
    // （原本就有的）連線失敗回呼：擴充成事件回報
    public void OnConnectionFaild(string reason = "連線失敗")
    {
        Debug.Log("呼叫OnConnectionFaild()");
        if (connectPanel.activeSelf)
        {
            message.text = reason;
        }
        OnConnectResult?.Invoke(false, reason);
    }
    public void OnInputFieldValueChanged()
    {
        ip = ipInput.text;
        port = portInput.text;
    }
}
