using UnityEngine;
using UnityEngine.UI;
using System;

public class DragPicturesHintController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RawImage sourceRawImage;   // 作畫區的 Tex_HiddenImage.RawImage
    [SerializeField] private RawImage interactionImage;    // 互動區的 Image

    [Header("Settings")]
    [SerializeField] private float autoDragDelay; // 幾秒後自動觸發
    private bool alreadyDragged = false;
    // 當搬移完成 → 通知 PanelFlowController
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
            Debug.Log("[DragPicturesHintController] 超時自動執行拖曳");
            SimulateDrag();
        }
    }
    /// <summary>
    /// 假裝左滑：把底圖從作畫區複製到互動區
    /// </summary>
    public void SimulateDrag()
    {
        if (alreadyDragged) return; // ★ 防止重複觸發
        alreadyDragged = true;

        if (sourceRawImage != null && interactionImage != null)
        {
            interactionImage.texture = null;
            interactionImage.material = null;
            interactionImage.texture = sourceRawImage.texture;

            Debug.Log("[DragPicturesHintController] 底圖已搬移到互動區");
        }
        else
        {
            Debug.LogWarning("[DragPicturesHintController] 尚未指派 RawImage 或 InteractionImage！");
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

        OnDragSimulated?.Invoke();
    }

}
