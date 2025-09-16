using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandReferenceDotSpawner : MonoBehaviour
{
    [Header("UI 與 Prefab 設定")]
    [Tooltip("要擺放紅點的父物件（通常是某個 Canvas 底下的一個空的 RectTransform 容器）。")]
    public RectTransform container;   // 建議掛在 Canvas 下的一個空物件
    [Tooltip("紅點的 UI Image 預置體（建議 24~32 px 的圓形，Color 設紅色半透明）。")]
    public Image dotPrefab;

    [Header("自動清空")]
    [Tooltip("多久沒更新就自動清空（秒）")]
    public float noUpdateThreshold = 0.5f;

    private readonly List<Image> _dots = new();
    private float _noUpdateTimer = 0f;
    Canvas _canvas;
    private RectTransform uiRectTransform;
    // [NEW] 統一由外部指定 UV 對應的目標 UI
    public void SetTargetRectTransform(RectTransform rt) // [NEW]
    {
        uiRectTransform = rt; // [NEW]
    }

    void Awake()
    {
        if (container == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] container 未指定！");
            enabled = false;
            return;
        }
        _canvas = container.GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] 找不到上層 Canvas！");
            enabled = false;
            return;
        }
        if (dotPrefab == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] dotPrefab 未指定！");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        _noUpdateTimer += Time.deltaTime;
        if (_noUpdateTimer > noUpdateThreshold)
        {
            ClearAll();
        }
    }

    /// <summary>
    /// 將紅點同步到多個螢幕座標（像素）。一個座標一個紅點。
    /// </summary>
    public void SyncDotsToScreenPositions(List<Vector2> screenPosList)
    {
        _noUpdateTimer = 0f;

        // 數量對齊（不足就補、太多就關）
        while (_dots.Count < screenPosList.Count)
        {
            var img = Instantiate(dotPrefab, container);
            img.gameObject.SetActive(true);
            _dots.Add(img);
        }
        for (int i = _dots.Count - 1; i >= screenPosList.Count; i--)
        {
            Destroy(_dots[i].gameObject);
            _dots.RemoveAt(i);
        }

        // 螢幕座標 → 容器 localPosition
        var cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        for (int i = 0; i < screenPosList.Count; i++)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screenPosList[i], cam, out Vector2 localPos))
            {
                _dots[i].rectTransform.anchoredPosition = localPos;
            }
        }
    }
    // ========================================================================
    // [NEW] 以「本地欄位 uiRectTransform 的 UV（0..1）」來擺放紅點。
    //       uv (0,0)=左下；(1,1)=右上。沿用原版的數量控管寫法（不足補、超出刪）。
    //       紅點最終生成在 this.container 之下。
    // ========================================================================
    /// <summary>以 uiRectTransform 的 UV（0..1）座標擺放紅點。</summary> // [NEW]
    /// <param name="uvList">UV 清單，(0,0)=左下, (1,1)=右上。</param> // [NEW]
    /// <param name="clamp01">是否將 UV 夾到 [0,1] 範圍。</param> // [NEW]
    public void SyncDotsToUVPositions(List<Vector2> uvList, bool clamp01, int heatedUvIndex, float heatedHoldTimesPercentage) // [NEW]
    { // [NEW]
        _noUpdateTimer = 0f; // [NEW]

        if (uiRectTransform == null) // [NEW]
        { // [NEW]
            Debug.LogError("[HandReferenceDotSpawner] uiRectTransform 未指定，無法以 UV 對應紅點位置。"); // [NEW]
            return; // [NEW]
        } // [NEW]

        // 數量對齊（不足就補、太多就關）——沿用原版風格 // [NEW]
        while (_dots.Count < uvList.Count) // [NEW]
        { // [NEW]
            var img = Instantiate(dotPrefab, container); // [NEW]
            img.gameObject.SetActive(true);              // [NEW]
            _dots.Add(img);                              // [NEW]
        } // [NEW]
        for (int i = _dots.Count - 1; i >= uvList.Count; i--) // [NEW]
        { // [NEW]
            Destroy(_dots[i].gameObject); // [NEW]
            _dots.RemoveAt(i);            // [NEW]
        } // [NEW]
          // 安全更新進度圈：只有命中的那顆設為百分比，其他歸 0（避免上一幀殘留）
        for (int i = 0; i < _dots.Count; i++)
        {
            var pc = _dots[i]?.GetComponentInChildren<ProgressCircleController>(true);
            if (pc == null) continue;

            if (heatedUvIndex >= 0 && heatedUvIndex < _dots.Count && i == heatedUvIndex)
            {
                pc.SetByPercentage(heatedHoldTimesPercentage); // 0..100
            }
            else
            {
                pc.SetByPercentage(0f);
            }
        }
        var cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera; // [NEW]

        for (int i = 0; i < uvList.Count; i++) // [NEW]
        { // [NEW]
            Vector2 uv = uvList[i]; // [NEW]
            if (clamp01) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); } // [NEW]

            // Step 1: UV → uiRectTransform 本地座標（考慮 pivot） // [NEW]
            // local = (uv * size) - (pivot * size)                                     // [NEW]
            Rect r = uiRectTransform.rect;                                             // [NEW]
            Vector2 size = r.size;                                                     // [NEW]
            Vector2 pivot = uiRectTransform.pivot;                                     // [NEW]
            Vector2 localInTarget = new Vector2(                                       // [NEW]
                uv.x * size.x - pivot.x * size.x,                                      // [NEW]
                uv.y * size.y - pivot.y * size.y                                       // [NEW]
            );                                                                          // [NEW]

            // Step 2: target 本地座標 → 世界座標 // [NEW]
            Vector3 world = uiRectTransform.TransformPoint(localInTarget); // [NEW]

            // Step 3: 世界座標 → 螢幕座標 // [NEW]
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world); // [NEW]

            // Step 4: 螢幕座標 → container 本地座標 // [NEW]
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle( // [NEW]
                container, screen, cam, out Vector2 localInContainer))   // [NEW]
            { // [NEW]
                _dots[i].rectTransform.anchoredPosition = localInContainer; // [NEW]
            } // [NEW]
        } // [NEW]
    } // [NEW]
    /// <summary>
    /// 立即清空所有紅點。
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < _dots.Count; i++)
        {
            if (_dots[i] != null)
                Destroy(_dots[i].gameObject);
        }
        _dots.Clear();
        _noUpdateTimer = 0f;
    }
}
