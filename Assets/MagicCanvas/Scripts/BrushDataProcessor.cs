using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BrushDataProcessor : MonoBehaviour
{
    [SerializeField] private List<ScratchCard> scratchCards;
    [SerializeField] private HandReferenceDotSpawner refDotSpawner; // 參考紅點顯示

    //[SerializeField] HandParticleEffectSpawner spawner;
    public bool isRevealing = false;
    public void HandleBrushData(List<BrushData> dataList)
    {
        List<Vector2> screenPosList = new List<Vector2>(); // [新增] 收集所有手座標的螢幕位置
        foreach (var data in dataList)
        {
            if (data.point == null || data.point.Length < 2)
                continue;


            float x = data.point[0];
            //float y = 1f - data.point[1];            // JSON 傳來為左上為原點，需轉為左下為原點
            float y = data.point[1];            // 改為內部判斷，不須反轉
            Vector2 uv = new Vector2(x, y);

            screenPosList.Add(new Vector2(x * Screen.width, y * Screen.height)); // [新增] 加入螢幕座標清單

            foreach (var card in scratchCards)
            {
                if (!card.isRevealing)
                {
                    card.EraseAtNormalizedUV(uv); // 每一個卡片都處理這個刮除點
                }
            }
        }

        // —— 畫面紅點同步 —— //
        if (refDotSpawner != null)
        {
            if (screenPosList.Count > 0)
                refDotSpawner.SyncDotsToScreenPositions(screenPosList);
            else
                refDotSpawner.ClearAll();
        }

        //// [新增] 控制多個特效物件
        //if (scratchCards[0].isRevealing && screenPosList.Count > 0)
        //    spawner.SyncParticlesToScreenPositions(screenPosList); // 多個手就會有多個特效
        //else
        //    spawner.ClearAll(); // 沒人時全部銷毀
    }
}
