using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandReferenceDotSpawner : MonoBehaviour
{
    [Header("UI �P Prefab �]�w")]
    [Tooltip("�n�\����I��������]�q�`�O�Y�� Canvas ���U���@�ӪŪ� RectTransform �e���^�C")]
    public RectTransform container;   // ��ĳ���b Canvas �U���@�ӪŪ���
    [Tooltip("���I�� UI Image �w�m��]��ĳ 24~32 px ����ΡAColor �]����b�z���^�C")]
    public Image dotPrefab;

    [Header("�۰ʲM��")]
    [Tooltip("�h�[�S��s�N�۰ʲM�š]��^")]
    public float noUpdateThreshold = 0.5f;

    private readonly List<Image> _dots = new();
    private float _noUpdateTimer = 0f;
    private Canvas _canvas;

    void Awake()
    {
        if (container == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] container �����w�I");
            enabled = false;
            return;
        }
        _canvas = container.GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] �䤣��W�h Canvas�I");
            enabled = false;
            return;
        }
        if (dotPrefab == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] dotPrefab �����w�I");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        _noUpdateTimer += Time.deltaTime;
        if (_noUpdateTimer > noUpdateThreshold)
        {
            ClearAll();
        }
    }

    /// <summary>
    /// �N���I�P�B��h�ӿù��y�С]�����^�C�@�Ӯy�Ф@�Ӭ��I�C
    /// </summary>
    public void SyncDotsToScreenPositions(List<Vector2> screenPosList)
    {
        _noUpdateTimer = 0f;

        // �ƶq����]�����N�ɡB�Ӧh�N���^
        while (_dots.Count < screenPosList.Count)
        {
            var img = Instantiate(dotPrefab, container);
            img.gameObject.SetActive(true);
            _dots.Add(img);
        }
        for (int i = _dots.Count - 1; i >= screenPosList.Count; i--)
        {
            Destroy(_dots[i].gameObject);
            _dots.RemoveAt(i);
        }

        // �ù��y�� �� �e�� localPosition
        var cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        for (int i = 0; i < screenPosList.Count; i++)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screenPosList[i], cam, out Vector2 localPos))
            {
                _dots[i].rectTransform.anchoredPosition = localPos;
            }
        }
    }

    /// <summary>
    /// �ߧY�M�ũҦ����I�C
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < _dots.Count; i++)
        {
            if (_dots[i] != null)
                Destroy(_dots[i].gameObject);
        }
        _dots.Clear();
        _noUpdateTimer = 0f;
    }
}
