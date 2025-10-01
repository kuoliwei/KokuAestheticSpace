using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public Text fpsText; // ���V UI Text
    private int frameCount;
    private float elapsedTime;
    private float refreshTime = 1f; // �C�j�X���s�@��

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        }

        if (fpsText.gameObject.activeSelf)
        {
            // �֭p frame �P�ɶ�
            frameCount++;
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= refreshTime)
            {
                // �p�⥭�� FPS
                float fps = frameCount / elapsedTime;

                // ��ܨ� Text
                fpsText.text = $"FPS: {fps:F1}";

                // ���m�p�ƾ�
                frameCount = 0;
                elapsedTime = 0f;
            }
        }

    }
}
