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

    [Header("�ѦҬ��I����")]
    [SerializeField] private Sprite hand;
    [SerializeField] private Sprite translucentRound;
    [SerializeField] private AudioSource paintingAudioSource;
    public enum dotSprites { hand, translucentRound };

    [Header("�۰ʲM��")]
    [Tooltip("�h�[�S��s�N�۰ʲM�š]��^")]
    public float noUpdateThreshold = 0.5f;

    private readonly List<Image> _dots = new();
    private float _noUpdateTimer = 0f;
    Canvas _canvas;
    private RectTransform uiRectTransform;
    // �Τ@�ѥ~�����w UV �������ؼ� UI
    public void SetTargetRectTransform(RectTransform rt)
    {
        uiRectTransform = rt;
    }

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
    public void ChangeSprite(dotSprites spriteName)
    {
        switch (spriteName)
        {
            case dotSprites.hand:
                dotPrefab.sprite = hand;
                dotPrefab.rectTransform.sizeDelta = new Vector3(250, 250, 1);
                if (paintingAudioSource.isPlaying) paintingAudioSource.Stop();
                break;
            case dotSprites.translucentRound:
                dotPrefab.sprite = translucentRound;
                dotPrefab.rectTransform.sizeDelta = new Vector3(500, 500, 1);
                if (!paintingAudioSource.isPlaying) paintingAudioSource.Play();
                break;
        }
        ClearAll();
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
    // ========================================================================
    // �H�u���a��� uiRectTransform �� UV�]0..1�^�v���\����I�C
    //       uv (0,0)=���U�F(1,1)=�k�W�C�u�έ쪩���ƶq���޼g�k�]�����ɡB�W�X�R�^�C
    //       ���I�̲ץͦ��b this.container ���U�C
    // ========================================================================
    /// <summary>�H uiRectTransform �� UV�]0..1�^�y���\����I�C</summary>
    /// <param name="uvList">UV �M��A(0,0)=���U, (1,1)=�k�W�C</param>
    /// <param name="clamp01">�O�_�N UV ���� [0,1] �d��C</param>
    public void SyncDotsToUVPositions(List<Vector2> uvList, bool clamp01, int heatedUvIndex, float heatedHoldTimesPercentage)
    {
        _noUpdateTimer = 0f;

        if (uiRectTransform == null)
        {
            Debug.LogError("[HandReferenceDotSpawner] uiRectTransform �����w�A�L�k�H UV �������I��m�C");
            return;
        }

        // �ƶq����]�����N�ɡB�Ӧh�N���^�X�X�u�έ쪩����
        while (_dots.Count < uvList.Count)
        {
            var img = Instantiate(dotPrefab, container);
            img.gameObject.SetActive(true);             
            _dots.Add(img);                             
        }
        for (int i = _dots.Count - 1; i >= uvList.Count; i--)
        {
            Destroy(_dots[i].gameObject);
            _dots.RemoveAt(i);           
        }
          // �w����s�i�װ�G�u���R���������]���ʤ���A��L�k 0�]�קK�W�@�V�ݯd�^
        for (int i = 0; i < _dots.Count; i++)
        {
            var pc = _dots[i]?.GetComponentInChildren<ProgressCircleController>(true);
            if (pc == null) continue;

            if (heatedUvIndex >= 0 && heatedUvIndex < _dots.Count && i == heatedUvIndex)
            {
                pc.SetByPercentage(heatedHoldTimesPercentage); // 0..100
            }
            else
            {
                pc.SetByPercentage(0f);
            }
        }
        var cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        for (int i = 0; i < uvList.Count; i++)
        {
            Vector2 uv = uvList[i];
            if (clamp01) { uv.x = Mathf.Clamp01(uv.x); uv.y = Mathf.Clamp01(uv.y); }

            // Step 1: UV �� uiRectTransform ���a�y�С]�Ҽ{ pivot�^
            // local = (uv * size) - (pivot * size)                                    
            Rect r = uiRectTransform.rect;                                            
            Vector2 size = r.size;                                                    
            Vector2 pivot = uiRectTransform.pivot;                                    
            Vector2 localInTarget = new Vector2(                                      
                uv.x * size.x - pivot.x * size.x,                                     
                uv.y * size.y - pivot.y * size.y                                      
            );                                                                         

            // Step 2: target ���a�y�� �� �@�ɮy��
            Vector3 world = uiRectTransform.TransformPoint(localInTarget);

            // Step 3: �@�ɮy�� �� �ù��y��
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);

            // Step 4: �ù��y�� �� container ���a�y��
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screen, cam, out Vector2 localInContainer))  
            {
                _dots[i].rectTransform.anchoredPosition = localInContainer;
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
