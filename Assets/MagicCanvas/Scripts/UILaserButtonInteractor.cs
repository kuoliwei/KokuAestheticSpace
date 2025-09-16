using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILaserButtonInteractor : MonoBehaviour
{
    [Header("UI Raycast")]
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    [Header("互動設定")]
    [Tooltip("命中同一顆按鈕累積多久才觸發 onClick（秒）")]
    [SerializeField] private float holdTimeToClick;
    [SerializeField] private bool autoClickOnHit = true;

    // [NEW] 沒被命中的寬限秒數（超過才清零）
    [SerializeField] private float missGrace;// 0.x 秒，照需求調

    // [NEW] 紀錄每顆按鈕最後一次被命中的時間
    private readonly Dictionary<Button, float> lastHitAt = new();

    // [NEW] UV 對應的目標 UI（UV 的 0..1 範圍會映到這塊 RectTransform）
    [Header("UV 對應設定")]
    private RectTransform uiRectTransform; // [NEW]
    public void SetTargetRectTransform(RectTransform rt) // [NEW]
    {
        uiRectTransform = rt; // [NEW]
    }

    private readonly Dictionary<Button, float> holdTimers = new(); // 同幀/連續幀累積
    private readonly HashSet<Button> hitThisFrame = new();

    void Reset()
    {
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = FindAnyObjectByType<EventSystem>();
    }

    /// <summary>把同一幀的所有螢幕座標送進來，逐一做 UI Raycast。</summary>
    public void Process(List<Vector2> screenPosList)
    {
        hitThisFrame.Clear();
        if (raycaster == null || eventSystem == null || screenPosList == null) return;

        var results = new List<RaycastResult>();
        var ped = new PointerEventData(eventSystem);

        foreach (var pos in screenPosList)
        {
            ped.position = pos;
            results.Clear();
            raycaster.Raycast(ped, results);

            // 取最上層的 Button（或你要的指定元件）
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                var btn = go.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    hitThisFrame.Add(btn);

                    // [NEW] 記錄最後命中時間
                    lastHitAt[btn] = Time.time;

                    break; // 命中一顆就夠了（避免同一束擊中多層）
                }
            }
        }

        // 更新 hold 累積 / 觸發 click
        // 1) 命中的累加計時
        foreach (var btn in hitThisFrame)
        {
            if (!holdTimers.ContainsKey(btn)) holdTimers[btn] = 0f;
            holdTimers[btn] += Time.deltaTime;

            if (autoClickOnHit && holdTimers[btn] >= holdTimeToClick)
            {
                holdTimers[btn] = 0f; // 重置避免連點
                btn.onClick?.Invoke();
            }
        }

        // [CHANGED] 清理：僅當「距離最後命中超過 missGrace」才清零
        var now = Time.time;
        var toRemove = new List<Button>();
        foreach (var kv in holdTimers)
        {
            var btn = kv.Key;
            if (!hitThisFrame.Contains(btn))
            {
                // 沒在本幀命中 → 檢查寬限
                if (!lastHitAt.TryGetValue(btn, out float last) || (now - last) > missGrace)
                {
                    toRemove.Add(btn);
                }
            }
        }
        foreach (var b in toRemove)
        {
            holdTimers.Remove(b);
            lastHitAt.Remove(b); // [NEW] 一併移除最後命中時間
        }
    }
    // ======================================================================
    // [NEW] UV 版本：吃一串 0..1 的座標（(0,0)=左下, (1,1)=右上），
    //      會以 uiRectTransform 為參考平面，轉成螢幕座標後沿用原本的 Process。
    // ======================================================================
    // ===== 直接取代舊的 ProcessUV =====
    public void ProcessUV(List<Vector2> uvList, bool clamp01, out int heatedUvIndex, out float heatedHoldTimesPercentage)
    {
        heatedUvIndex = -1;
        heatedHoldTimesPercentage = 0f;

        hitThisFrame.Clear();
        if (uvList == null || uvList.Count == 0) return;
        if (raycaster == null || eventSystem == null) return;
        if (uiRectTransform == null) return;

        // Canvas / Camera
        var canvas = raycaster.GetComponent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ?? raycaster.eventCamera ?? Camera.main)
            : null;

        // UV → local(uiRect) → world → screen，並 Raycast
        Rect r = uiRectTransform.rect;
        Vector2 size = r.size, pivot = uiRectTransform.pivot;

        var ped = new PointerEventData(eventSystem);
        var results = new List<RaycastResult>();

        Button firstHitBtn = null; // 這幀第一個命中的按鈕

        for (int i = 0; i < uvList.Count; i++)
        {
            Vector2 uv = uvList[i];
            if (clamp01) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); }

            Vector2 local = new Vector2(
                uv.x * size.x - pivot.x * size.x,
                uv.y * size.y - pivot.y * size.y
            );
            Vector3 world = uiRectTransform.TransformPoint(local);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);

            ped.position = screen;
            results.Clear();
            raycaster.Raycast(ped, results);

            for (int j = 0; j < results.Count; j++)
            {
                var go = results[j].gameObject;
                var btn = go.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    hitThisFrame.Add(btn);

                    if (heatedUvIndex == -1) // 記錄第一個命中的 UV 與按鈕
                    {
                        heatedUvIndex = i;
                        firstHitBtn = btn;
                    }

                    // 記錄最後命中時間（給 missGrace 用）
                    lastHitAt[btn] = Time.time;
                    break;
                }
            }
        }

        // 累積 / 觸發（同時取第一顆命中按鈕的累積秒數）
        foreach (var btn in hitThisFrame)
        {
            float current = 0f;
            holdTimers.TryGetValue(btn, out current);

            float updated = current + Time.deltaTime;

            // 若這顆就是「第一個命中按鈕」，回傳它本幀累積到的秒數（在重置之前）
            if (btn == firstHitBtn)
                heatedHoldTimesPercentage = Mathf.Clamp((updated / holdTimeToClick) * 100, 0f, 100f);

            if (autoClickOnHit && updated >= holdTimeToClick)
            {
                btn.onClick?.Invoke();
                holdTimers[btn] = 0f; // 重置避免連點
            }
            else
            {
                holdTimers[btn] = updated;
            }
        }

        // 清零：超過 missGrace 秒沒再命中才移除
        var now = Time.time;
        var toRemove = new List<Button>();
        foreach (var kv in holdTimers)
        {
            var btn = kv.Key;
            if (!hitThisFrame.Contains(btn))
            {
                if (!lastHitAt.TryGetValue(btn, out float last) || (now - last) > missGrace)
                    toRemove.Add(btn);
            }
        }
        foreach (var b in toRemove)
        {
            holdTimers.Remove(b);
            lastHitAt.Remove(b);
        }
    }
    // （可選）若你有清狀態函式，記得加上 lastHitAt 清空
    public void ClearState() // 若已存在就補一行
    {
        holdTimers.Clear();
        hitThisFrame.Clear();
        lastHitAt.Clear(); // [NEW]
    }
}
