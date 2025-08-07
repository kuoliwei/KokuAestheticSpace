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
    private float noUpdateThreshold = 0.5f; // 0.5秒無更新就自動清空
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
}
