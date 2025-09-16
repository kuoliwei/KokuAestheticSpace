using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BrushDataProcessor : MonoBehaviour
{
    [SerializeField] private List<ScratchCard> scratchCards;
    [SerializeField] private HandReferenceDotSpawner refDotSpawner; // 參考紅點顯示
                                                                    // BrushDataProcessor.cs（節錄）
    [SerializeField] private UILaserButtonInteractor buttonInteractor;
    // [NEW] 這就是兩邊共用的目標 UI（UV 的 0..1 會映在這塊 RectTransform 上）
    [SerializeField] private RectTransform paintingUIRectTransform; // [NEW]
    [SerializeField] private RectTransform interactiveUIRectTransform; // [NEW]
    [SerializeField] HandParticleEffectSpawner spawner;
    public bool isRevealing = false;
    void Start() // 或 Awake()
    {
        // [NEW] 統一指派給兩個元件
        if (paintingUIRectTransform != null)
        {
            if (refDotSpawner != null) refDotSpawner.SetTargetRectTransform(paintingUIRectTransform);   // [NEW]
            if (buttonInteractor != null) buttonInteractor.SetTargetRectTransform(paintingUIRectTransform); // [NEW]
        }
        else
        {
            Debug.LogWarning("[BrushDataProcessor] sharedUIRectTransform 未指定，UV 轉換將失效。"); // [NEW]
        }
        // 新增：把互動區的目標給特效 Spawner
        if (spawner != null && interactiveUIRectTransform != null)                          // [NEW]
            spawner.SetTargets(interactiveUIRectTransform, interactiveUIRectTransform.GetComponentInParent<Canvas>()); // [NEW]
    }
    public void HandleBrushData(List<BrushData> dataList)
    {
        List<Vector2> screenPosList = new List<Vector2>(); // [新增] 收集所有手座標的螢幕位置
        List<Vector2> uvList = new List<Vector2>();        // [NEW] 直接收集 UV，給紅點 UI 用
        foreach (var data in dataList)
        {
            if (data.point == null || data.point.Length < 2)
                continue;

            float x = data.point[0];
            //float y = 1f - data.point[1];            // JSON 傳來為左上為原點，需轉為左下為原點
            float y = data.point[1];            // 改為內部判斷，不須反轉
            Vector2 uv = new Vector2(x, y);

            screenPosList.Add(new Vector2(x * Screen.width, y * Screen.height)); // [新增] 加入螢幕座標清單
            uvList.Add(uv); // [NEW] 累積 UV 供紅點 UI 使用

            foreach (var card in scratchCards)
            {
                if (!card.isRevealing && card.gameObject.activeSelf)
                {
                    card.EraseAtNormalizedUV(uv); // 每一個卡片都處理這個刮除點
                }
            }
        }

        //// ★ 新增：按鈕命中判斷
        //if (buttonInteractor != null && screenPosList.Count > 0)
        //    buttonInteractor.Process(screenPosList);
        // ★ 新增：按鈕命中判斷（UV版）
        int heatedUvIndex = -1;
        float heatedHoldTimesPercentage = 0;
        if (buttonInteractor != null && uvList.Count > 0)
            buttonInteractor.ProcessUV(uvList, true, out heatedUvIndex, out heatedHoldTimesPercentage);

        // —— 畫面紅點同步 —— //
        if (refDotSpawner != null)
        {
            //if (screenPosList.Count > 0)
            //    refDotSpawner.SyncDotsToScreenPositions(screenPosList);
            //else
            //    refDotSpawner.ClearAll();
            if (uvList.Count > 0)                                  // [NEW]
                refDotSpawner.SyncDotsToUVPositions(uvList,true, heatedUvIndex, heatedHoldTimesPercentage);       // [NEW] 直接把 UV 傳給 spawner（會用 uiRectTransform 計算）
            else                                                   // [NEW]
                refDotSpawner.ClearAll();
        }

        //// [新增] 控制多個特效物件
        //if (scratchCards[0].isRevealing && screenPosList.Count > 0)
        //    spawner.SyncParticlesToScreenPositions(screenPosList); // 多個手就會有多個特效
        //else
        //    spawner.ClearAll(); // 沒人時全部銷毀
    }
    // BrushDataProcessor.cs 內新增（不動你原有 HandleBrushData）
    public void HandleEffectUV(List<Vector2> effectUVs) // [NEW]
    {
        if (effectUVs == null || effectUVs.Count == 0) return;

        // —— 互動區特效（Interactive 區） ——
        if (spawner != null)
        {
            if (effectUVs != null && effectUVs.Count > 0)     // [NEW] 與紅點一致：有就同步
                spawner.SyncParticlesToUVPositions(effectUVs);
            else                                              // [NEW] 沒有就馬上清空，避免殘留
                spawner.ClearAll();
        }
    }
}
