using System.Collections.Generic;
using UnityEngine;

public class HandTrailEffectSpawner : MonoBehaviour
{
    [Header("Prefab 與 Canvas 設定")]
    [SerializeField] private GameObject handTrailPrefab;    // 拖尾或粒子 prefab
    [SerializeField] private RectTransform uiRectTransform; // UV 所對應的互動區
    [SerializeField] private Canvas targetCanvas;           // 要生成在哪個 Canvas 底下
    [SerializeField] private float expireSeconds = 1f;      // 超過多久沒更新就銷毀

    // 每個人的左右手特效
    private class PersonHandEffects
    {
        public GameObject leftEffect;
        public GameObject rightEffect;
        public float lastSeenTime;
    }

    // key = personIndex (每幀順序，不是 server 給的 ID)
    private readonly Dictionary<int, PersonHandEffects> effectsByIndex = new();

    void Awake()
    {
        if (targetCanvas == null && uiRectTransform != null)
            targetCanvas = uiRectTransform.GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// 更新或建立手部特效
    /// </summary>
    public void UpdateHand(int personIndex, bool isLeft, Vector2 uv)
    {
        if (uiRectTransform == null || targetCanvas == null)
        {
            Debug.LogWarning("[HandTrailEffectSpawner] uiRectTransform 或 targetCanvas 未指定");
            return;
        }

        // 取得該人的特效，沒有就建立
        if (!effectsByIndex.TryGetValue(personIndex, out var personEffects))
        {
            personEffects = new PersonHandEffects();
            personEffects.leftEffect = Instantiate(handTrailPrefab, targetCanvas.transform);
            personEffects.rightEffect = Instantiate(handTrailPrefab, targetCanvas.transform);
            effectsByIndex[personIndex] = personEffects;
        }

        // 更新位置
        if (isLeft)
            UpdateEffectPosition(personEffects.leftEffect, uv);
        else
            UpdateEffectPosition(personEffects.rightEffect, uv);

        // 更新最後出現時間
        personEffects.lastSeenTime = Time.time;
    }

    /// <summary>
    /// UV 轉換為 Canvas localPosition，並更新物件位置
    /// </summary>
    private void UpdateEffectPosition(GameObject effect, Vector2 uv, bool clamp01 = true)
    {
        if (effect == null) return;

        if (clamp01)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        var canvasRect = targetCanvas.GetComponent<RectTransform>();
        Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                    : (targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main);

        Rect r = uiRectTransform.rect;
        Vector2 size = r.size;
        Vector2 pivot = uiRectTransform.pivot;

        // UV → local(in target)
        Vector2 localInTarget = new Vector2(
            uv.x * size.x - pivot.x * size.x,
            uv.y * size.y - pivot.y * size.y
        );

        // target local → world
        Vector3 world = uiRectTransform.TransformPoint(localInTarget);
        // world → screen
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        // screen → canvas local
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 localOnCanvas))
        {
            effect.transform.localPosition = new Vector3(localOnCanvas.x, localOnCanvas.y, -500f);
        }
    }

    void Update()
    {
        // 收集要刪除的 index
        List<int> expired = new List<int>();
        foreach (var kv in effectsByIndex)
        {
            if (Time.time - kv.Value.lastSeenTime > expireSeconds)
                expired.Add(kv.Key);
        }

        // 銷毀並移除
        foreach (int idx in expired)
        {
            var eff = effectsByIndex[idx];
            if (eff.leftEffect != null) Destroy(eff.leftEffect);
            if (eff.rightEffect != null) Destroy(eff.rightEffect);
            effectsByIndex.Remove(idx);
        }
    }

    /// <summary>
    /// 清除所有人特效
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in effectsByIndex)
        {
            if (kv.Value.leftEffect != null) Destroy(kv.Value.leftEffect);
            if (kv.Value.rightEffect != null) Destroy(kv.Value.rightEffect);
        }
        effectsByIndex.Clear();
    }
}
