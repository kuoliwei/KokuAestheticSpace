using System.Collections.Generic;
using UnityEngine;

public class HandTrailEffectSpawner : MonoBehaviour
{
    [Header("Prefab �P Canvas �]�w")]
    [SerializeField] private GameObject handTrailPrefab;    // ����βɤl prefab
    [SerializeField] private RectTransform uiRectTransform; // UV �ҹ��������ʰ�
    [SerializeField] private Canvas targetCanvas;           // �n�ͦ��b���� Canvas ���U
    [SerializeField] private float expireSeconds = 1f;      // �W�L�h�[�S��s�N�P��

    // �C�ӤH�����k��S��
    private class PersonHandEffects
    {
        public GameObject leftEffect;
        public GameObject rightEffect;
        public float lastSeenTime;
    }

    // key = personIndex (�C�V���ǡA���O server ���� ID)
    private readonly Dictionary<int, PersonHandEffects> effectsByIndex = new();

    void Awake()
    {
        if (targetCanvas == null && uiRectTransform != null)
            targetCanvas = uiRectTransform.GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// ��s�Ϋإߤⳡ�S��
    /// </summary>
    public void UpdateHand(int personIndex, bool isLeft, Vector2 uv)
    {
        if (uiRectTransform == null || targetCanvas == null)
        {
            Debug.LogWarning("[HandTrailEffectSpawner] uiRectTransform �� targetCanvas �����w");
            return;
        }

        // ���o�ӤH���S�ġA�S���N�إ�
        if (!effectsByIndex.TryGetValue(personIndex, out var personEffects))
        {
            personEffects = new PersonHandEffects();
            personEffects.leftEffect = Instantiate(handTrailPrefab, targetCanvas.transform);
            personEffects.rightEffect = Instantiate(handTrailPrefab, targetCanvas.transform);
            effectsByIndex[personIndex] = personEffects;
        }

        // ��s��m
        if (isLeft)
            UpdateEffectPosition(personEffects.leftEffect, uv);
        else
            UpdateEffectPosition(personEffects.rightEffect, uv);

        // ��s�̫�X�{�ɶ�
        personEffects.lastSeenTime = Time.time;
    }

    /// <summary>
    /// UV �ഫ�� Canvas localPosition�A�ç�s�����m
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

        // UV �� local(in target)
        Vector2 localInTarget = new Vector2(
            uv.x * size.x - pivot.x * size.x,
            uv.y * size.y - pivot.y * size.y
        );

        // target local �� world
        Vector3 world = uiRectTransform.TransformPoint(localInTarget);
        // world �� screen
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        // screen �� canvas local
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 localOnCanvas))
        {
            effect.transform.localPosition = new Vector3(localOnCanvas.x, localOnCanvas.y, -500f);
        }
    }

    void Update()
    {
        // �����n�R���� index
        List<int> expired = new List<int>();
        foreach (var kv in effectsByIndex)
        {
            if (Time.time - kv.Value.lastSeenTime > expireSeconds)
                expired.Add(kv.Key);
        }

        // �P���ò���
        foreach (int idx in expired)
        {
            var eff = effectsByIndex[idx];
            if (eff.leftEffect != null) Destroy(eff.leftEffect);
            if (eff.rightEffect != null) Destroy(eff.rightEffect);
            effectsByIndex.Remove(idx);
        }
    }

    /// <summary>
    /// �M���Ҧ��H�S��
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
