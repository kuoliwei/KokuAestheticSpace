using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamTest : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;
    string selectedDeviceName => dropdown.options[dropdown.value].text;
    //string selectedDeviceName;
    private WebCamTexture webCamTexture;
    public WebCamTexture PreviewTexture => webCamTexture; // 讓外部綁到 RawImage.texture

    //[SerializeField] private RawImage previewRawImage;
    // Start is called before the first frame update
    void Start()
    {
        SetDropdoenOptions(GetDevices(), dropdown);
    }
    List<string> GetDevices()
    {
        List<string> names = new List<string>();
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            names.Add(device.name);
        }
        return names;
    }
    void SetDropdoenOptions(List<string> deviceNeams, Dropdown dropdown)
    {
        dropdown.ClearOptions();
        List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();
        foreach (string name in deviceNeams)
        {
            optionDatas.Add(new Dropdown.OptionData(name));
            Debug.Log(name);
        }
        dropdown.AddOptions(optionDatas);
    }
    public void SelectWebcam()
    {
        //selectedDeviceName = dropdown.options[dropdown.value].text;
    }
    public void OpenCamera()
    {
        Debug.Log("呼叫OpenCamera()");
        //若已在同一台裝置上播放，不重複開
        if (webCamTexture != null && webCamTexture.isPlaying &&
            (string.IsNullOrEmpty(selectedDeviceName) || webCamTexture.deviceName == selectedDeviceName)) return;

        if (webCamTexture != null && webCamTexture.isPlaying) webCamTexture.Stop();

        if (string.IsNullOrEmpty(selectedDeviceName))
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) { Debug.LogError("No webcam found."); return; }
        }

        webCamTexture = new WebCamTexture(selectedDeviceName, 1920, 1080);
        //webCamTexture = new WebCamTexture("Intel(R) RealSense(TM) Depth Camera 455  RGB", 1920, 1080);
        //previewRawImage.texture = webCamTexture;

        webCamTexture.Play();
        dropdown.gameObject.SetActive(false);
    }
    //public void DisplayOut()
    //{
    //    webCamTexture.Play();
    //}
    public void CloseCamera()
    {
        Debug.Log("呼叫CloseCamera()");
        //if (webCamTexture != null && webCamTexture.isPlaying) webCamTexture.Stop();

        //webCamTexture.Stop();
    }
    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    OpenCamera();
        //}
        //if (Input.GetKeyDown(KeyCode.D))
        //{
        //    DisplayOut();
        //}
        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    CloseCamera();
        //}
    }
}
