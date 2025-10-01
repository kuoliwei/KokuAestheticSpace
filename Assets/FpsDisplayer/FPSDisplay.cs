using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public Text fpsText; // 指向 UI Text
    private int frameCount;
    private float elapsedTime;
    private float refreshTime = 1f; // 每隔幾秒刷新一次

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        }

        if (fpsText.gameObject.activeSelf)
        {
            // 累計 frame 與時間
            frameCount++;
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= refreshTime)
            {
                // 計算平均 FPS
                float fps = frameCount / elapsedTime;

                // 顯示到 Text
                fpsText.text = $"FPS: {fps:F1}";

                // 重置計數器
                frameCount = 0;
                elapsedTime = 0f;
            }
        }

    }
}
