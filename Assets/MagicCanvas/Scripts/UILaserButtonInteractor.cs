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
    private readonly Dictionary<Button, float> firstHitAt = new();
    private readonly Dictionary<Button, float> lastHitAt = new();
    private readonly Dictionary<Button, float> holdTimers = new(); // 同幀/連續幀累積
    private readonly HashSet<Button> hitThisFrame = new();
    // 第一次命中該按鈕時的 UV index
    private readonly Dictionary<Button, int> firstHitUvIndex = new();

    // [NEW] UV 對應的目標 UI（UV 的 0..1 範圍會映到這塊 RectTransform）
    [Header("UV 對應設定")]
    private RectTransform uiRectTransform; // [NEW]
    public void SetTargetRectTransform(RectTransform rt) // [NEW]
    {
        uiRectTransform = rt; // [NEW]
    }



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

        float now = Time.time; // 如需忽略 timeScale 可改用 Time.unscaledTime

        // 取用 Raycast 相機
        var canvas = raycaster.GetComponent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? (canvas.worldCamera ?? raycaster.eventCamera ?? Camera.main)
            : null;

        // UV → local(uiRect) → world → screen → Raycast(Button)
        Rect r = uiRectTransform.rect;
        Vector2 size = r.size, pivot = uiRectTransform.pivot;

        var ped = new PointerEventData(eventSystem);
        var results = new List<RaycastResult>();

        // ---------- 回收/清理：失效元件與 missGrace 超時 ----------
        // 用快照避免遍歷時修改集合
        var trackedSnapshot = new List<Button>(firstHitAt.Keys);
        foreach (var btn in trackedSnapshot)
        {
            bool invalid = (btn == null) || !btn.gameObject || !btn.gameObject.activeInHierarchy || !btn.interactable;
            if (invalid)
            {
                firstHitAt.Remove(btn);
                lastHitAt.Remove(btn);
                holdTimers.Remove(btn);
                firstHitUvIndex.Remove(btn);
                continue;
            }

            // 超過 missGrace 未再命中 → 回收
            if (!lastHitAt.TryGetValue(btn, out float last))
            {
                // 沒有最後命中時間紀錄，視為無效
                firstHitAt.Remove(btn);
                holdTimers.Remove(btn);
                firstHitUvIndex.Remove(btn);
                continue;
            }
            if ((now - last) > missGrace)
            {
                firstHitAt.Remove(btn);
                lastHitAt.Remove(btn);
                holdTimers.Remove(btn);
                firstHitUvIndex.Remove(btn);
                Debug.Log($"now:{now}, last:{last}, {now - last}>{missGrace},超時");
            }
            Debug.Log($"now:{now}, last:{last}");
        }

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

            // 取結果中第一顆可互動的 Button（最上層）
            for (int j = 0; j < results.Count; j++)
            {
                var go = results[j].gameObject;
                var btn = go != null ? go.GetComponentInParent<Button>() : null;
                if (btn != null && btn.interactable && btn.gameObject.activeInHierarchy)
                {
                    hitThisFrame.Add(btn);

                    // 第一次被擊中 → 建檔 firstHitAt / firstHitUvIndex
                    if (!firstHitAt.ContainsKey(btn))
                    {
                        firstHitAt[btn] = now;
                        firstHitUvIndex[btn] = i;
                    }

                    // 每幀命中都刷新最後命中時間
                    lastHitAt[btn] = now;
                    break; // 這個 UV 已命中一顆有效 Button，往下一個 UV
                }
            }
        }

        // ---------- 以「時間差」更新目前持續按壓秒數（非累加） ----------
        //（只要在追蹤中，就會隨時間前進；missGrace 內不中斷就不清零）
        holdTimers.Clear();
        foreach (var kv in firstHitAt)
        {
            var btn = kv.Key;
            float started = kv.Value;
            float held = Mathf.Max(0f, now - started);
            holdTimers[btn] = held;
        }

        // ---------- 找出目前「hold 最久」的領先者，用於 UI 回傳 ----------
        Button leader = null;
        float bestHeld = -1f;
        float bestLast = -1f;
        int bestId = int.MaxValue;

        foreach (var kv in holdTimers)
        {
            var btn = kv.Key;
            float held = kv.Value;
            float last = lastHitAt.TryGetValue(btn, out var l) ? l : 0f;
            int id = btn != null ? btn.GetInstanceID() : int.MinValue;

            bool better =
                (held > bestHeld) ||
                (Mathf.Approximately(held, bestHeld) && last > bestLast) ||
                (Mathf.Approximately(held, bestHeld) && Mathf.Approximately(last, bestLast) && id < bestId);

            if (better)
            {
                leader = btn;
                bestHeld = held;
                bestLast = last;
                bestId = id;
            }
        }

        if (leader != null)
        {
            // 百分比與 UV
            heatedHoldTimesPercentage = Mathf.Clamp01(bestHeld / holdTimeToClick) * 100f;
            if (firstHitUvIndex.TryGetValue(leader, out var idx)) heatedUvIndex = idx;
            Debug.Log($"bestHeld:{bestHeld},heatedHoldTimesPercentage:{heatedHoldTimesPercentage}");
            // ---------- 自動觸發：只允許一顆成功 ----------
            if (autoClickOnHit && bestHeld >= holdTimeToClick)
            {
                leader.onClick?.Invoke();

                // 觸發後清場，確保同一時刻只會有一顆成功
                firstHitAt.Clear();
                lastHitAt.Clear();
                holdTimers.Clear();
                firstHitUvIndex.Clear();
                hitThisFrame.Clear();

                // 觸發當幀，UI 你可選擇維持 100% 或由外層重繪
            }
        }
        else
        {
            // 沒有任何候選 → 回傳預設
            heatedUvIndex = -1;
            heatedHoldTimesPercentage = 0f;
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
