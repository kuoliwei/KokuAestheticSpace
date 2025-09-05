using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WebCamController : MonoBehaviour
{
    [SerializeField] private Dropdown dropdown;
    [SerializeField] private ImageStyleTransferHandler imageStyleTransferHandler;
    private WebCamTexture webCamTexture;
    public string selectedDeviceName => dropdown.options[dropdown.value].text;
    public WebCamTexture PreviewTexture => webCamTexture; // 讓外部綁到 RawImage.texture
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
        }
        dropdown.AddOptions(optionDatas);
    }
    //public void OnDropdownValueChange()
    //{
    //    ActivateDevice(dropdown.options[dropdown.value].text, displayImage);
    //    Debug.Log($"Webcam {dropdown.options[dropdown.value].text} has been activated.");
    //}
    //void ActivateDevice(string deviceName, RawImage displayImage)
    //{
    //    try
    //    {
    //        if (webCamTexture != null && webCamTexture.isPlaying && webCamTexture.deviceName == deviceName)
    //        {
    //            Debug.Log("已經在使用此攝影機：" + deviceName);
    //            return;
    //        }
    //        if (webCamTexture.isPlaying || webCamTexture != null)
    //        {
    //            webCamTexture.Stop();
    //            Destroy(webCamTexture);
    //            webCamTexture = null;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.Log(ex);
    //    }

    //    webCamTexture = new WebCamTexture(deviceName);
    //    displayImage.texture = webCamTexture;
    //    webCamTexture.Play();
    //}
    // 開相機（可傳 deviceName，不傳則用第一個）
    public void OpenCamera(string selectedDeviceName)
    {
        Debug.Log("呼叫OpenCamera()");
        // 若已在同一台裝置上播放，不重複開
        if (webCamTexture != null && webCamTexture.isPlaying &&
            (string.IsNullOrEmpty(selectedDeviceName) || webCamTexture.deviceName == selectedDeviceName)) return;

        if (webCamTexture != null && webCamTexture.isPlaying) webCamTexture.Stop();

        if (string.IsNullOrEmpty(selectedDeviceName))
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) { Debug.LogError("No webcam found."); return; }
        }

        webCamTexture = new WebCamTexture(selectedDeviceName, 1920, 1080);
        webCamTexture.Play();
    }
    //public void OpenCamera(string deviceName = null, int width = 1280, int height = 720, int fps = 30)
    //{
    //    // 若已在同一台裝置上播放，不重複開
    //    if (webCamTexture != null && webCamTexture.isPlaying &&
    //        (string.IsNullOrEmpty(deviceName) || webCamTexture.deviceName == deviceName)) return;

    //    if (webCamTexture != null && webCamTexture.isPlaying) webCamTexture.Stop();

    //    if (string.IsNullOrEmpty(deviceName))
    //    {
    //        var devices = WebCamTexture.devices;
    //        if (devices.Length == 0) { Debug.LogError("No webcam found."); return; }
    //        deviceName = devices[0].name;
    //    }

    //    webCamTexture = new WebCamTexture(deviceName, width, height, fps);
    //    webCamTexture.Play();
    //}

    public void CloseCamera()
    {
        Debug.Log("呼叫CloseCamera()");
        if (webCamTexture != null && webCamTexture.isPlaying) webCamTexture.Stop();
    }

    // 同步取得目前一張影像（Texture2D）
    public Texture2D CaptureFrame()
    {
        Debug.Log("呼叫CaptureFrame()");
        if (webCamTexture == null || !webCamTexture.isPlaying) return null;
        var tex = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        tex.SetPixels(webCamTexture.GetPixels());
        tex.Apply();
        return tex;
    }

    // 一條龍：拍照→上傳→輪詢→下載；結果透過回呼回傳 Texture2D
    public IEnumerator CaptureAndStylize(string style, Action<Texture2D> onDone, int jpgQuality = 85)
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogWarning("Camera not running.");
            onDone?.Invoke(null);
            yield break;
        }

        Texture2D photo = null;
        try
        {
            // 1) 拍照
            photo = CaptureFrame();
            if (photo == null) { onDone?.Invoke(null); yield break; }

            byte[] bytes = photo.EncodeToJPG(Mathf.Clamp(jpgQuality, 1, 100));
            string image64 = Convert.ToBase64String(bytes);

            // 2) 送請求
            bool ok = false;
            yield return imageStyleTransferHandler.SendStyleRequest(image64, style, tid =>
            {
                ok = !string.IsNullOrEmpty(tid);
            });
            if (!ok) { onDone?.Invoke(null); yield break; }

            // 3) 輪詢進度
            yield return imageStyleTransferHandler.CheckProgress(_ => { }, () => { });

            // 4) 下載圖片
            Texture2D result = null;
            yield return imageStyleTransferHandler.DownloadImage(tex => result = tex);
            onDone?.Invoke(result);
        }
        finally
        {
            if (photo != null) Destroy(photo);  // Unity 物件用 Destroy 釋放
        }
    }

}
