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
    private Canvas _canvas;

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
