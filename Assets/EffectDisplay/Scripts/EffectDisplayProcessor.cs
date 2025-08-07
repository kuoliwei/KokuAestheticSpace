using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectDisplayProcessor : MonoBehaviour
{
    [SerializeField] private List<Canvas> effectDisplayCanvas;
    [SerializeField] private HandParticleEffectSpawner spawner;

    public void HandleEffectData(List<BrushData> dataList)
    {
        if (dataList == null || dataList.Count == 0)
            return;

        List<Vector2> screenPosList = new List<Vector2>();

        foreach (var data in dataList)
        {
            if (data.point == null || data.point.Length < 2)
                continue;

            float x = data.point[0];
            float y = 1f - data.point[1]; // 左上 → 左下原點轉換
            Vector2 screenPos = new Vector2(x * Screen.width, y * Screen.height);

            screenPosList.Add(screenPos);
        }

        if (screenPosList.Count > 0)
        {
            foreach (var canvas in effectDisplayCanvas)
            {
                spawner.SyncParticlesToScreenPositions(canvas, screenPosList);
            }
        }
        else
        {
            spawner.ClearAll();
        }
    }
}
