using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILaserButtonInteractor : MonoBehaviour
{
    [Header("UI Raycast")]
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    [Header("���ʳ]�w")]
    [Tooltip("�R���P�@�����s�ֿn�h�[�~Ĳ�o onClick�]��^")]
    [SerializeField] private float holdTimeToClick;
    [SerializeField] private bool autoClickOnHit = true;

    // [NEW] �S�Q�R�����e����ơ]�W�L�~�M�s�^
    [SerializeField] private float missGrace;// 0.x ��A�ӻݨD��

    // [NEW] �����C�����s�̫�@���Q�R�����ɶ�
    private readonly Dictionary<Button, float> lastHitAt = new();

    // [NEW] UV �������ؼ� UI�]UV �� 0..1 �d��|�M��o�� RectTransform�^
    [Header("UV �����]�w")]
    private RectTransform uiRectTransform; // [NEW]
    public void SetTargetRectTransform(RectTransform rt) // [NEW]
    {
        uiRectTransform = rt; // [NEW]
    }

    private readonly Dictionary<Button, float> holdTimers = new(); // �P�V/�s��V�ֿn
    private readonly HashSet<Button> hitThisFrame = new();

    void Reset()
    {
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = FindAnyObjectByType<EventSystem>();
    }

    /// <summary>��P�@�V���Ҧ��ù��y�аe�i�ӡA�v�@�� UI Raycast�C</summary>
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

            // ���̤W�h�� Button�]�ΧA�n�����w����^
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                var btn = go.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    hitThisFrame.Add(btn);

                    // [NEW] �O���̫�R���ɶ�
                    lastHitAt[btn] = Time.time;

                    break; // �R���@���N���F�]�קK�P�@�������h�h�^
                }
            }
        }

        // ��s hold �ֿn / Ĳ�o click
        // 1) �R�����֥[�p��
        foreach (var btn in hitThisFrame)
        {
            if (!holdTimers.ContainsKey(btn)) holdTimers[btn] = 0f;
            holdTimers[btn] += Time.deltaTime;

            if (autoClickOnHit && holdTimers[btn] >= holdTimeToClick)
            {
                holdTimers[btn] = 0f; // ���m�קK�s�I
                btn.onClick?.Invoke();
            }
        }

        // [CHANGED] �M�z�G�ȷ�u�Z���̫�R���W�L missGrace�v�~�M�s
        var now = Time.time;
        var toRemove = new List<Button>();
        foreach (var kv in holdTimers)
        {
            var btn = kv.Key;
            if (!hitThisFrame.Contains(btn))
            {
                // �S�b���V�R�� �� �ˬd�e��
                if (!lastHitAt.TryGetValue(btn, out float last) || (now - last) > missGrace)
                {
                    toRemove.Add(btn);
                }
            }
        }
        foreach (var b in toRemove)
        {
            holdTimers.Remove(b);
            lastHitAt.Remove(b); // [NEW] �@�ֲ����̫�R���ɶ�
        }
    }
    // ======================================================================
    // [NEW] UV �����G�Y�@�� 0..1 ���y�С](0,0)=���U, (1,1)=�k�W�^�A
    //      �|�H uiRectTransform ���Ѧҥ����A�ন�ù��y�Ы�u�έ쥻�� Process�C
    // ======================================================================
    // ===== �������N�ª� ProcessUV =====
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

        // UV �� local(uiRect) �� world �� screen�A�� Raycast
        Rect r = uiRectTransform.rect;
        Vector2 size = r.size, pivot = uiRectTransform.pivot;

        var ped = new PointerEventData(eventSystem);
        var results = new List<RaycastResult>();

        Button firstHitBtn = null; // �o�V�Ĥ@�өR�������s

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

                    if (heatedUvIndex == -1) // �O���Ĥ@�өR���� UV �P���s
                    {
                        heatedUvIndex = i;
                        firstHitBtn = btn;
                    }

                    // �O���̫�R���ɶ��]�� missGrace �Ρ^
                    lastHitAt[btn] = Time.time;
                    break;
                }
            }
        }

        // �ֿn / Ĳ�o�]�P�ɨ��Ĥ@���R�����s���ֿn��ơ^
        foreach (var btn in hitThisFrame)
        {
            float current = 0f;
            holdTimers.TryGetValue(btn, out current);

            float updated = current + Time.deltaTime;

            // �Y�o���N�O�u�Ĥ@�өR�����s�v�A�^�ǥ����V�ֿn�쪺��ơ]�b���m���e�^
            if (btn == firstHitBtn)
                heatedHoldTimesPercentage = Mathf.Clamp((updated / holdTimeToClick) * 100, 0f, 100f);

            if (autoClickOnHit && updated >= holdTimeToClick)
            {
                btn.onClick?.Invoke();
                holdTimers[btn] = 0f; // ���m�קK�s�I
            }
            else
            {
                holdTimers[btn] = updated;
            }
        }

        // �M�s�G�W�L missGrace ��S�A�R���~����
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
    // �]�i��^�Y�A���M���A�禡�A�O�o�[�W lastHitAt �M��
    public void ClearState() // �Y�w�s�b�N�ɤ@��
    {
        holdTimers.Clear();
        hitThisFrame.Clear();
        lastHitAt.Clear(); // [NEW]
    }
}
