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
    private float noUpdateThreshold = 0.5f; // 0.5��L��s�N�۰ʲM��
    /// <summary>
    /// ���~���ǤJ�h�ծy�СA�C�ծy�з|���@�ӹ����S��
    /// </summary>
    public void SyncParticlesToScreenPositions(Canvas canvas, List<Vector2> screenPosList)
    {
        noUpdateTimer = 0f;

        if (!effectsByCanvas.ContainsKey(canvas))
        {
            effectsByCanvas[canvas] = new List<GameObject>();
        }

        var list = effectsByCanvas[canvas];

        // �ɨ�
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

        // �M���h��
        while (list.Count > screenPosList.Count)
        {
            int last = list.Count - 1;
            Destroy(list[last]);
            list.RemoveAt(last);
        }

        // �ഫ��m
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
    /// �S�����󦳮Įy�ЮɩI�s�A�����^��
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
            // �]�w�̤j�ɤl�Ƭ�0
            var main = ps.main;
            main.maxParticles = 0;
        }
        // ��1��
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
