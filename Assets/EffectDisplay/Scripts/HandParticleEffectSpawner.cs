using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandParticleEffectSpawner : MonoBehaviour
{
    public GameObject particlePrefab;
    //public RectTransform canvasRect;

    //private List<GameObject> currentEffects = new List<GameObject>();
    private Dictionary<Canvas, List<GameObject>> effectsByCanvas = new();
    private float noUpdateTimer = 0f;
    private float noUpdateThreshold = 0.1f; // 0.5秒無更新就自動清空
    // =========================
    // UV 版本需要的欄位
    // =========================
    [Header("UV 對應設定")]                             // [NEW]
    private RectTransform uiRectTransform; // [NEW] UV 所對應的那塊互動 UI（InteractiveQuad）
    private Canvas targetCanvas;           // [NEW] 要把特效生在哪個 Canvas 底下（若空會自動找）
    void Syart() // [NEW]
    {
        // 若未指定 Canvas，從 uiRectTransform 往上找
        if (targetCanvas == null && uiRectTransform != null) // [NEW]
            targetCanvas = uiRectTransform.GetComponentInParent<Canvas>(); // [NEW]
    }
    /// <summary>
    /// 讓外部傳入多組座標，每組座標會有一個對應特效
    /// </summary>
    public void SyncParticlesToScreenPositions(Canvas canvas, List<Vector2> screenPosList)
    {
        noUpdateTimer = 0f;

        if (!effectsByCanvas.ContainsKey(canvas))
        {
            effectsByCanvas[canvas] = new List<GameObject>();
        }

        var list = effectsByCanvas[canvas];

        // 補足
        while (list.Count < screenPosList.Count)
        {
            var go = Instantiate(particlePrefab, canvas.transform);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.maxParticles = 1000;
            }
            list.Add(go);
        }

        // 清除多的
        while (list.Count > screenPosList.Count)
        {
            int last = list.Count - 1;
            Destroy(list[last]);
            list.RemoveAt(last);
        }

        // 轉換位置
        var canvasRect = canvas.GetComponent<RectTransform>();
        Camera cam = canvas.worldCamera;

        for (int i = 0; i < screenPosList.Count; i++)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosList[i], cam, out Vector2 localPos))
            {
                list[i].transform.localPosition = new Vector3(localPos.x, localPos.y, -500f);
            }
        }
    }

    // =========================================================
    // [NEW] UV 版本：InteractiveQuad 用這個
    //      uv (0,0)=左下, (1,1)=右上；會用 uiRectTransform 把 UV 投影到 targetCanvas
    // =========================================================
    public void SyncParticlesToUVPositions(List<Vector2> uvList, bool clamp01 = true) // [NEW]
    {
        noUpdateTimer = 0f; // [NEW]
        if (uiRectTransform == null) { Debug.LogWarning("[HandParticleEffectSpawner] uiRectTransform 未指定"); return; } // [NEW]

        // 找到 Canvas
        var canvas = targetCanvas != null ? targetCanvas : uiRectTransform.GetComponentInParent<Canvas>(); // [NEW]
        if (canvas == null) { Debug.LogWarning("[HandParticleEffectSpawner] 找不到 Canvas"); return; }    // [NEW]

        if (!effectsByCanvas.ContainsKey(canvas)) // [NEW]
            effectsByCanvas[canvas] = new List<GameObject>(); // [NEW]
        var list = effectsByCanvas[canvas]; // [NEW]

        // 數量控管：不足補、超出刪 // [NEW]
        while (list.Count < uvList.Count) // [NEW]
        {
            var go = Instantiate(particlePrefab, canvas.transform); // [NEW]
            var ps = go.GetComponent<ParticleSystem>();             // [NEW]
            if (ps != null)                                         // [NEW]
            {                                                       // [NEW]
                var main = ps.main;                                  // [NEW]
                main.maxParticles = 1000;                            // [NEW]
            }                                                       // [NEW]
            list.Add(go);                                           // [NEW]
        }                                                           // [NEW]
        while (list.Count > uvList.Count)                           // [NEW]
        {                                                           // [NEW]
            int last = list.Count - 1;                              // [NEW]
            Destroy(list[last]);                                    // [NEW]
            list.RemoveAt(last);                                    // [NEW]
        }                                                           // [NEW]

        // UV → local(canvas) // [NEW]
        var canvasRect = canvas.GetComponent<RectTransform>(); // [NEW]
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main); // [NEW]

        Rect r = uiRectTransform.rect;   // [NEW]
        Vector2 size = r.size;           // [NEW]
        Vector2 pivot = uiRectTransform.pivot; // [NEW]

        for (int i = 0; i < uvList.Count; i++) // [NEW]
        {
            Vector2 uv = uvList[i];            // [NEW]
            if (clamp01) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); } // [NEW]

            // UV → target local（考慮 pivot） // [NEW]
            Vector2 localInTarget = new Vector2(uv.x * size.x - pivot.x * size.x,
                                                uv.y * size.y - pivot.y * size.y);  // [NEW]
            // target local → world // [NEW]
            Vector3 world = uiRectTransform.TransformPoint(localInTarget);           // [NEW]
            // world → screen（用 Canvas 相機） // [NEW]
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);    // [NEW]
            // screen → canvas local // [NEW]
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 localOnCanvas)) // [NEW]
            {
                list[i].transform.localPosition = new Vector3(localOnCanvas.x, localOnCanvas.y, -500f); // [NEW]
            }
        }
    }

    /// <summary>
    /// 沒有任何有效座標時呼叫，全部回收
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in effectsByCanvas)
        {
            foreach (var go in kv.Value)
            {
                StartCoroutine(DelayedParticleDestroy(go));
            }
        }

        effectsByCanvas.Clear();
    }
    private IEnumerator DelayedParticleDestroy(GameObject go)
    {
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // 設定最大粒子數為0
            var main = ps.main;
            main.maxParticles = 0;
        }
        // 等1秒
        yield return new WaitForSeconds(3f);

        Destroy(go);
    }
    private void Update()
    {
        noUpdateTimer += Time.deltaTime;
        if (noUpdateTimer > noUpdateThreshold)
        {
            ClearAll();
        }
    }
    // ===== 可選：由外部統一指定 ===== // [NEW]
    public void SetTargets(RectTransform ui, Canvas canvas = null)  // [NEW]
    {
        uiRectTransform = ui;                                       // [NEW]
        targetCanvas = canvas != null ? canvas : (ui != null ? ui.GetComponentInParent<Canvas>() : null); // [NEW]
    }
}
