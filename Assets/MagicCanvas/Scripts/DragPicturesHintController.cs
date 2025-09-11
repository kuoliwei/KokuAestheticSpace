using UnityEngine;
using UnityEngine.UI;
using System;

public class DragPicturesHintController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RawImage sourceRawImage;   // 作畫區的 Tex_HiddenImage.RawImage
    [SerializeField] private RawImage interactionImage;    // 互動區的 Image

    // 當搬移完成 → 通知 PanelFlowController
    public event Action OnDragSimulated;

    /// <summary>
    /// 假裝左滑：把底圖從作畫區複製到互動區
    /// </summary>
    public void SimulateDrag()
    {
        if (sourceRawImage != null && interactionImage != null)
        {
            interactionImage.texture = null;   // 確保是用 texture 顯示
            interactionImage.material = null;
            interactionImage.texture = sourceRawImage.texture;

            Debug.Log("[DragPicturesHintController] 底圖已搬移到互動區");
        }
        else
        {
            Debug.LogWarning("[DragPicturesHintController] 尚未指派 RawImage 或 InteractionImage！");
        }

        OnDragSimulated?.Invoke();
    }
}
