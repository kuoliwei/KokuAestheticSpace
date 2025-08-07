using UnityEngine;
using System.Collections.Generic;

public class FakeBrushSender : MonoBehaviour
{
    [SerializeField] private WebSocketMessageReceiverAsync receiver;

    [Range(0f, 1f)] public float testX = 0.5f;
    [Range(0f, 1f)] public float testY = 0.5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            string fakeJson = CreateFakeJson(testX, testY);
            receiver.SendMessageManually(fakeJson);
        }
    }

    private string CreateFakeJson(float x, float y)
    {
        return $"{{\"data\":[{{\"roller_id\":1,\"point\":[{x},{y}]}}]}}";
    }
}
