using UnityEngine;

public class HandTrailController : MonoBehaviour
{
    private TrailRenderer trail;

    void Awake()
    {
        // 取得 TrailRenderer
        trail = GetComponent<TrailRenderer>();

        // 預設關閉（你可以改成 true）
        //trail.enabled = false;
    }

    void Update()
    {
        //// 範例：按下滑鼠左鍵才啟動殘影
        //if (Input.GetMouseButtonDown(0))
        //{
        //    trail.Clear();      // 清空舊的殘影
        //    trail.enabled = true;
        //}

        //// 放開滑鼠左鍵就關閉
        //if (Input.GetMouseButtonUp(0))
        //{
        //    trail.enabled = false;
        //}
    }
}
