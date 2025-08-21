using UnityEngine;
using UnityEngine.UI;
using System.Net;

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
    [Header("連線接收器")]
    public WebSocketMessageReceiverAsync receiver;

    private void Start()
    {
        // 若要預設可填在這裡（目前已註解）
        ipInput.text = "10.66.66.57";
        portInput.text = "8765";
        ip = ipInput.text;
        port = portInput.text;

        //connectButton.onClick.AddListener(OnClickConnect);
    }
    public void OnInputFieldValueChanged()
    {
        ip = ipInput.text;
        port = portInput.text;
    }
    public void OnClickConnect()
    {
        message.text = "";
        string ip = this.ip;
        string portText = this.port;

        // IP 合法性檢查
        if (!IPAddress.TryParse(ip, out _))
        {
            Debug.LogWarning("IP 格式不正確");
            message.text += "IP 格式不正確\n";
            return;
        }

        // Port 合法性檢查
        if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
        {
            Debug.LogWarning("Port 格式不正確（有效範圍：1~65535）");
            message.text += "Port 格式不正確（有效範圍：1~65535）";
            return;
        }

        receiver.ConnectToServer(ip, portText);
    }
    public void OnConnectionFaild()
    {
        Debug.LogWarning("連線失敗");
        if (connectPanel.activeSelf)
        {
            message.text = "連線失敗";
        }
    }
}
