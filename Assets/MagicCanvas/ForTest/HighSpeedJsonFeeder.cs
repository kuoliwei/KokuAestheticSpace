using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

public class MouseTriggeredJsonFeeder : MonoBehaviour
{
    public WebSocketMessageReceiverAsync receiver;
    public int simulatedMessagesPerSecond = 100;

    private CancellationTokenSource cts;

    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    StartFeeding();
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    StopFeeding();
        //}
    }

    public void StartFeeding()
    {
        Debug.Log("StartFeeding 執行");
        if (receiver == null || cts != null) return;

        cts = new CancellationTokenSource();
        var token = cts.Token;
        //Debug.Log($"[Feeder] 開始灌入測試資料，每秒 {simulatedMessagesPerSecond} 筆");
        Debug.Log("token.IsCancellationRequested : " + token.IsCancellationRequested);
        Task.Run(async () =>
        {
            Debug.Log("非同步 執行");
            int intervalMS = Mathf.Max(1, 1000 / simulatedMessagesPerSecond);

            while (!token.IsCancellationRequested)
            {
                //System.Random rand = new System.Random();
                //Debug.Log("while 執行");
                //float x = (float)rand.NextDouble();
                //Debug.Log("x 取亂數");
                //float y = (float)rand.NextDouble();
                //Debug.Log("y 取亂數");
                string json = $"{{\"data\":[{{\"roller_id\":0,\"point\":[{0.5f},{0.5f}]}}]}}";
                //Debug.Log("產生 json");
                //Debug.Log($"[Feeder] SendMessageManually(json) 呼叫中：{json}");
                //Debug.Log("SendMessageManually 之前");
                receiver.SendMessageManually(json);
                //Debug.Log("SendMessageManually 之後");
                await Task.Delay(intervalMS, token);
            }
            //Debug.Log("[Feeder] 已停止灌入");
        }, token);
        Debug.Log("StartFeeding 結束");
    }

    public void StopFeeding()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts = null;
        }
    }
}
