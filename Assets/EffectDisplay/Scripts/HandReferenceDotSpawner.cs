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

    [Header("參考紅點素材")]
    [SerializeField] private Sprite hand;
    [SerializeField] private Sprite translucentRound;
    [SerializeField] private AudioSource paintingAudioSource;
    public enum dotSprites { hand, translucentRound };

    [Header("自動清空")]
    [Tooltip("多久沒更新就自動清空（秒）")]
    public float noUpdateThreshold = 0.5f;

    private readonly List<Image> _dots = new();
    private float _noUpdateTimer = 0f;
    Canvas _canvas;
    private RectTransform uiRectTransform;
    // 統一由外部指定 UV 對應的目標 UI
    public void SetTargetRectTransform(RectTransform rt)
    {
        uiRectTransform = rt;
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
    public void ChangeSprite(dotSprites spriteName)
    {
        switch (spriteName)
        {
            case dotSprites.hand:
                dotPrefab.sprite = hand;
                dotPrefab.rectTransform.sizeDelta = new Vector3(250, 250, 1);
                if (paintingAudioSource.isPlaying) paintingAudioSource.Stop();
                break;
            case dotSprites.translucentRound:
                dotPrefab.sprite = translucentRound;
                dotPrefab.rectTransform.sizeDelta = new Vector3(500, 500, 1);
                if (!paintingAudioSource.isPlaying) paintingAudioSource.Play();
                break;
        }
        ClearAll();
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
    // 以「本地欄位 uiRectTransform 的 UV（0..1）」來擺放紅點。
    //       uv (0,0)=左下；(1,1)=右上。沿用原版的數量控管寫法（不足補、超出刪）。
    //       紅點最終生成在 this.container 之下。
    // ========================================================================
    /// <summary>以 uiRectTransform 的 UV（0..1）座標擺放紅點。</summary>
    /// <param name="uvList">UV 清單，(0,0)=左下, (1,1)=右上。</param>
    /// <param name="clamp01">是否將 UV 夾到 [0,1] 範圍。</param>
    public void SyncDotsToUVPositions(List<Vector2> uvList, bool clamp01, int heatedUvIndex, float heatedHoldTimesPercentage)
    {
        _noUpdateTimer = 0f;

        if (uiRectTransform == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] uiRectTransform 未指定，無法以 UV 對應紅點位置。");
            return;
        }

        // 數量對齊（不足就補、太多就關）——沿用原版風格
        while (_dots.Count < uvList.Count)
        {
            var img = Instantiate(dotPrefab, container);
            img.gameObject.SetActive(true);             
            _dots.Add(img);                             
        }
        for (int i = _dots.Count - 1; i >= uvList.Count; i--)
        {
            Destroy(_dots[i].gameObject);
            _dots.RemoveAt(i);           
        }
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
        var cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        for (int i = 0; i < uvList.Count; i++)
        {
            Vector2 uv = uvList[i];
            if (clamp01) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); }

            // Step 1: UV → uiRectTransform 本地座標（考慮 pivot）
            // local = (uv * size) - (pivot * size)                                    
            Rect r = uiRectTransform.rect;                                            
            Vector2 size = r.size;                                                    
            Vector2 pivot = uiRectTransform.pivot;                                    
            Vector2 localInTarget = new Vector2(                                      
                uv.x * size.x - pivot.x * size.x,                                     
                uv.y * size.y - pivot.y * size.y                                      
            );                                                                         

            // Step 2: target 本地座標 → 世界座標
            Vector3 world = uiRectTransform.TransformPoint(localInTarget);

            // Step 3: 世界座標 → 螢幕座標
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);

            // Step 4: 螢幕座標 → container 本地座標
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screen, cam, out Vector2 localInContainer))  
            {
                _dots[i].rectTransform.anchoredPosition = localInContainer;
            }
        }
    }
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
