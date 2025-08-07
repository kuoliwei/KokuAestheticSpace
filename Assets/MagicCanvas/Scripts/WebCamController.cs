using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class WebCamController : MonoBehaviour
{
    [SerializeField] Dropdown dropdown;
    WebCamTexture webCamTexture;
    [SerializeField] RawImage displayImage;
    [SerializeField] string photoFolder;
    [SerializeField] string photoFileName;
    [SerializeField] ImageStyleTransferHandler imageStyleTransferHandler;
    private Coroutine currentCoroutine;
    public enum PaintingStyle { Style1 , Style2 , Style3 }
    PaintingStyle currebtPaintingStyle = PaintingStyle.Style1;
    // Start is called before the first frame update
    void Start()
    {
        SetDropdoenOptions(GetDevices(), dropdown);
        OnDropdownValueChange();
    }

    // Update is called once per frame
    void Update()
    {
        
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
        foreach(string name in deviceNeams)
        {
            optionDatas.Add(new Dropdown.OptionData(name));
        }
        dropdown.AddOptions(optionDatas);
    }
    public void OnDropdownValueChange()
    {
        ActivateDevice(dropdown.options[dropdown.value].text, displayImage);
        Debug.Log($"Webcam {dropdown.options[dropdown.value].text} has been activated.");
    }
    void ActivateDevice(string deviceName, RawImage displayImage)
    {
        try
        {
            if (webCamTexture != null && webCamTexture.isPlaying && webCamTexture.deviceName == deviceName)
            {
                Debug.Log("已經在使用此攝影機：" + deviceName);
                return;
            }
            if (webCamTexture.isPlaying || webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        webCamTexture = new WebCamTexture(deviceName);
        displayImage.texture = webCamTexture;
        webCamTexture.Play();
    }

    public void CapturePhoto()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            Debug.LogWarning("尚未啟動任何攝影機！");
            return;
        }

        // 建立 Texture2D 來接收畫面
        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();

        // 轉成 JPG byte[]
        byte[] bytes = photo.EncodeToJPG();
        // 轉 Base64 給 API


        // 建立存檔完整路徑
        string folderPath = Path.Combine(Application.dataPath, photoFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        //else
        //{
        //    Directory.GetFiles(folderPath);
        //}
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(folderPath, $"{photoFileName}_{timestamp}.jpg");

        File.WriteAllBytes(path, bytes);

        Debug.Log("拍照成功，存檔路徑：" + path);

        Destroy(photo); // 釋放暫存

        string image64 = Convert.ToBase64String(bytes);
        // 如果已有任務在跑，先停掉
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(UploadAndProcess(image64, currebtPaintingStyle.ToString()));
    }

    private IEnumerator UploadAndProcess(string image64, string style)
    {
        bool isRequestSuccess = false;

        // 發送風格轉換請求
        yield return imageStyleTransferHandler.SendStyleRequest(image64, style, taskId =>
        {
            if (!string.IsNullOrEmpty(taskId))
            {
                isRequestSuccess = true;
                Debug.Log($"成功建立任務 TaskID={taskId}");
            }
            else
            {
                Debug.LogError("風格轉換請求失敗");
            }
        });

        if (!isRequestSuccess) yield break;

        // 檢查進度
        yield return imageStyleTransferHandler.CheckProgress(
            progress => Debug.Log($"風格轉換進度 {progress}%"),
            () => Debug.Log("風格轉換完成")
        );

        // 下載圖片
        yield return imageStyleTransferHandler.DownloadImage(texture =>
        {
            if (texture != null)
            {
                displayImage.texture = texture; // 套用結果
                Debug.Log("風格轉換圖片下載完成並顯示");
            }
            else
            {
                Debug.LogError("下載圖片失敗");
            }
        });
    }
}
