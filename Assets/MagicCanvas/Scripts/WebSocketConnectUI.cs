using UnityEngine;
using UnityEngine.UI;
using System.Net;

public class WebSocketConnectUI : MonoBehaviour
{
    [Header("UI ����")]
    public Text message;
    public GameObject connectPanel;
    public InputField ipInput;
    public InputField portInput;
    public Button connectButton;

    private string ip = "127.0.0.1";
    private string port = "9999";
    [Header("�s�u������")]
    public WebSocketMessageReceiverAsync receiver;

    private void Start()
    {
        // �Y�n�w�]�i��b�o�̡]�ثe�w���ѡ^
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

        // IP �X�k���ˬd
        if (!IPAddress.TryParse(ip, out _))
        {
            Debug.LogWarning("IP �榡�����T");
            message.text += "IP �榡�����T\n";
            return;
        }

        // Port �X�k���ˬd
        if (!int.TryParse(portText, out int port) || port < 1 || port > 65535)
        {
            Debug.LogWarning("Port �榡�����T�]���Ľd��G1~65535�^");
            message.text += "Port �榡�����T�]���Ľd��G1~65535�^";
            return;
        }

        receiver.ConnectToServer(ip, portText);
    }
    public void OnConnectionFaild()
    {
        Debug.LogWarning("�s�u����");
        if (connectPanel.activeSelf)
        {
            message.text = "�s�u����";
        }
    }
}
