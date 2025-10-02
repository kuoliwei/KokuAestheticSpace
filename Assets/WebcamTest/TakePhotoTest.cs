using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

public class TakePhotoTest : MonoBehaviour
{
    [SerializeField] private WebcamTest webcamTest;
    [SerializeField] private RawImage previewRawImage;  // TakingPhotoPanel �W��ܹw���� RawImage
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Enter()
    {
        Debug.Log("����Enter");
        webcamTest.OpenCamera();
        previewRawImage.texture = webcamTest.PreviewTexture;
    }
    public void Exit()
    {
        Debug.Log("����Exit");
        webcamTest.CloseCamera();
    }
}
