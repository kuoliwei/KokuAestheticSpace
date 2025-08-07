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
                Debug.Log("�w�g�b�ϥΦ���v���G" + deviceName);
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
            Debug.LogWarning("�|���Ұʥ�����v���I");
            return;
        }

        // �إ� Texture2D �ӱ����e��
        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();

        // �ন JPG byte[]
        byte[] bytes = photo.EncodeToJPG();
        // �� Base64 �� API


        // �إߦs�ɧ�����|
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

        Debug.Log("��Ӧ��\�A�s�ɸ��|�G" + path);

        Destroy(photo); // ����Ȧs

        string image64 = Convert.ToBase64String(bytes);
        // �p�G�w�����Ȧb�]�A������
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(UploadAndProcess(image64, currebtPaintingStyle.ToString()));
    }

    private IEnumerator UploadAndProcess(string image64, string style)
    {
        bool isRequestSuccess = false;

        // �o�e�����ഫ�ШD
        yield return imageStyleTransferHandler.SendStyleRequest(image64, style, taskId =>
        {
            if (!string.IsNullOrEmpty(taskId))
            {
                isRequestSuccess = true;
                Debug.Log($"���\�إߥ��� TaskID={taskId}");
            }
            else
            {
                Debug.LogError("�����ഫ�ШD����");
            }
        });

        if (!isRequestSuccess) yield break;

        // �ˬd�i��
        yield return imageStyleTransferHandler.CheckProgress(
            progress => Debug.Log($"�����ഫ�i�� {progress}%"),
            () => Debug.Log("�����ഫ����")
        );

        // �U���Ϥ�
        yield return imageStyleTransferHandler.DownloadImage(texture =>
        {
            if (texture != null)
            {
                displayImage.texture = texture; // �M�ε��G
                Debug.Log("�����ഫ�Ϥ��U�����������");
            }
            else
            {
                Debug.LogError("�U���Ϥ�����");
            }
        });
    }
}
