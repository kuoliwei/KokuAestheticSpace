using UnityEngine;
using UnityEngine.UI;
using System;

public class DragPicturesHintController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RawImage sourceRawImage;   // �@�e�Ϫ� Tex_HiddenImage.RawImage
    [SerializeField] private RawImage interactionImage;    // ���ʰϪ� Image

    [Header("Settings")]
    [SerializeField] private float autoDragDelay; // �X���۰�Ĳ�o
    private bool alreadyDragged = false;
    // ��h������ �� �q�� PanelFlowController
    public event Action OnDragSimulated;
    private Coroutine coroutine;
    private void OnEnable()
    {
        alreadyDragged = false;

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        coroutine = StartCoroutine(AutoDragAfterDelay(autoDragDelay));
    }
    private System.Collections.IEnumerator AutoDragAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (!alreadyDragged)
        {
            Debug.Log("[DragPicturesHintController] �W�ɦ۰ʰ���즲");
            SimulateDrag();
        }
    }
    /// <summary>
    /// ���˥��ơG�⩳�ϱq�@�e�Ͻƻs�줬�ʰ�
    /// </summary>
    public void SimulateDrag()
    {
        if (alreadyDragged) return; // �� �����Ĳ�o
        alreadyDragged = true;

        if (sourceRawImage != null && interactionImage != null)
        {
            interactionImage.texture = null;
            interactionImage.material = null;
            interactionImage.texture = sourceRawImage.texture;

            Debug.Log("[DragPicturesHintController] ���Ϥw�h���줬�ʰ�");
        }
        else
        {
            Debug.LogWarning("[DragPicturesHintController] �|������ RawImage �� InteractionImage�I");
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        OnDragSimulated?.Invoke();
    }

}
