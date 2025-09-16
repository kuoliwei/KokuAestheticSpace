using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ScratchCard : MonoBehaviour, IPointerDownHandler
{
    private Sprite maskImage;
    public RawImage rawImage;
    private Texture brushTexture;
    private Material eraseMaterial;
    private float brushSize = 64f;

    private RenderTexture renderTex;
    private RectTransform rect;
    private Texture2D croppedTex;
    private bool isFullyRevealed = false;

    public System.Action OnFullyRevealed;
    private readonly Queue<Vector2> uvQueue = new();
    [SerializeField] int maxUvPerFrame = 32;
    private void Start()
    {

    }
    // [NEW] 回到此物件啟用時，自動確保是「未刮」狀態
    private void OnEnable() // [NEW]
    {
        RestoreMaskImmediate();
    }
    public void Init()
    {
        rect = rawImage.rectTransform;
        renderTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        renderTex.Create();
        rawImage.texture = renderTex;
    }

    public void ResetScratch()
    {
        isFullyRevealed = false;

        if (renderTex == null || maskImage == null)
        {
            Debug.LogWarning("RenderTex 或 maskImage 為 null，無法重置");
            return;
        }

        Texture2D tex = maskImage.texture;
        Rect spriteRect = maskImage.rect;

        croppedTex = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
        croppedTex.SetPixels(tex.GetPixels(
            (int)spriteRect.x,
            (int)spriteRect.y,
            (int)spriteRect.width,
            (int)spriteRect.height
        ));
        croppedTex.Apply();

        Graphics.Blit(croppedTex, renderTex);
        rawImage.texture = renderTex;
        isRevealing = false;
    }

    private void DrawTransparentAt(Vector2 uv)
    {
        if (brushTexture == null || eraseMaterial == null || renderTex == null) return;

        eraseMaterial.SetTexture("_MainTex", renderTex);
        eraseMaterial.SetTexture("_BrushTex", brushTexture);
        eraseMaterial.SetVector("_BrushPos", new Vector4(
            uv.x, uv.y,
            brushSize / renderTex.width,
            brushSize / renderTex.height
        ));

        RenderTexture tempRT = RenderTexture.GetTemporary(renderTex.width, renderTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(renderTex, tempRT, eraseMaterial);
        Graphics.Blit(tempRT, renderTex);
        RenderTexture.ReleaseTemporary(tempRT);

        rawImage.texture = renderTex;

        //if (!isFullyRevealed && GetClearedRatio() >= clearThreshold)
        //{
        //    ShowFullImage();
        //}
    }
    public void DrawTransparentAt(List<Vector2> uvList)
    {
        //{//多次blit
        //    if (brushTexture == null || eraseMaterial == null || renderTex == null || uvList == null || uvList.Count == 0)
        //        return;

        //    RenderTexture tempRT = RenderTexture.GetTemporary(renderTex.width, renderTex.height, 0, RenderTextureFormat.ARGB32);
        //    Graphics.Blit(renderTex, tempRT); // 先複製目前遮罩狀態

        //    eraseMaterial.SetTexture("_MainTex", tempRT);
        //    eraseMaterial.SetTexture("_BrushTex", brushTexture);

        //    foreach (var uv in uvList)
        //    {
        //        eraseMaterial.SetVector("_BrushPos", new Vector4(
        //            uv.x, uv.y,
        //            brushSize / renderTex.width,
        //            brushSize / renderTex.height
        //        ));

        //        Graphics.Blit(tempRT, renderTex, eraseMaterial);

        //        // 把 renderTex 再複製回 tempRT，讓下次刮除在最新狀態上
        //        Graphics.Blit(renderTex, tempRT);
        //    }

        //    RenderTexture.ReleaseTemporary(tempRT);
        //    rawImage.texture = renderTex;

        //    if (!isFullyRevealed && GetClearedRatio() >= clearThreshold)
        //    {
        //        ShowFullImage();
        //    }
        //}
        {//單次blit
            if (brushTexture == null || eraseMaterial == null || renderTex == null || uvList == null || uvList.Count == 0)
                return;

            eraseMaterial.SetTexture("_MainTex", renderTex);
            eraseMaterial.SetTexture("_BrushTex", brushTexture);
            eraseMaterial.SetFloat("_AlphaDecayFactor", 0.1f);

            const int MaxBrushCount = 10;
            Vector4[] brushMultiPos = new Vector4[MaxBrushCount];

            int count = Mathf.Min(uvList.Count, MaxBrushCount);
            float sizeX = brushSize / renderTex.width;
            float sizeY = brushSize / renderTex.height;

            for (int i = 0; i < count; i++)
            {
                Vector2 uv = uvList[i];
                brushMultiPos[i] = new Vector4(uv.x, uv.y, sizeX, sizeY);
            }

            eraseMaterial.SetVectorArray("_BrushMultiPos", brushMultiPos);

            RenderTexture tempRT = RenderTexture.GetTemporary(renderTex.width, renderTex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(renderTex, tempRT, eraseMaterial);
            Graphics.Blit(tempRT, renderTex);
            RenderTexture.ReleaseTemporary(tempRT);

            rawImage.texture = renderTex;

            //if (!isFullyRevealed && GetClearedRatio() >= clearThreshold)
            //{
            //    ShowFullImage();
            //}
        }
    }
    public float GetClearedRatio()
    {
        Texture2D tempTex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.ARGB32, false);
        RenderTexture.active = renderTex;
        tempTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        tempTex.Apply();
        RenderTexture.active = null;

        Color32[] pixels = tempTex.GetPixels32();
        int cleared = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 8) cleared++;
        }

        Destroy(tempTex);
        return (float)cleared / pixels.Length;
    }
    public bool isRevealing = false;
    public void ShowFullImage()
    {
        isFullyRevealed = true;
        isRevealing = true;

        Texture2D clearTex = new Texture2D(renderTex.width, renderTex.height);
        Color32[] pixels = new Color32[renderTex.width * renderTex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);
        clearTex.SetPixels32(pixels);
        clearTex.Apply();

        Graphics.Blit(clearTex, renderTex);
        Destroy(clearTex);

        rawImage.texture = renderTex;
        OnFullyRevealed?.Invoke();
    }
    // [NEW] 立即把遮罩恢復到「未刮」狀態（不走 SmoothRestoreMask 漸進）
    public void RestoreMaskImmediate()
    {
        // 若還沒建 renderTex（例如流程切換剛啟用），這裡保險建一個
        if (renderTex == null)
        {
            renderTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            renderTex.Create();
            if (rawImage != null) rawImage.texture = renderTex;
        }

        // 若沒有現成的 croppedTex，就依照目前的 maskImage 重建一次（同 ResetScratch 的作法）
        if (croppedTex == null)
        {
            if (maskImage == null)
            {
                Debug.LogWarning("[ScratchCard] 無法立即恢復：maskImage 尚未指定。");
                return;
            }
            Texture2D tex = maskImage.texture;
            Rect spriteRect = maskImage.rect;
            croppedTex = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
            croppedTex.SetPixels(tex.GetPixels(
                (int)spriteRect.x,
                (int)spriteRect.y,
                (int)spriteRect.width,
                (int)spriteRect.height
            ));
            croppedTex.Apply();
        }

        // 直接把原始遮罩覆蓋回 renderTex
        Graphics.Blit(croppedTex, renderTex);
        if (rawImage != null) rawImage.texture = renderTex;

        // 重置旗標
        isFullyRevealed = false;
        isRevealing = false;
    }
    public IEnumerator SmoothRestoreMask(float restoreSpeed)
    {
        if (croppedTex == null || renderTex == null) yield break;
        Texture2D restoreTex = new Texture2D(croppedTex.width, croppedTex.height);
        restoreTex.SetPixels(croppedTex.GetPixels());
        restoreTex.Apply();
        float t = 0;
        while (t < 1f)
        {
            for (int y = 0; y < restoreTex.height; y++)
            {
                for (int x = 0; x < restoreTex.width; x++)
                {
                    Color c = croppedTex.GetPixel(x, y);
                    c.a *= t;
                    restoreTex.SetPixel(x, y, c);
                }
            }
            restoreTex.Apply();
            Graphics.Blit(restoreTex, renderTex);
            t += Time.deltaTime * restoreSpeed;
            yield return null;
        }
        Graphics.Blit(croppedTex, renderTex);
        Destroy(restoreTex);
        yield return null;
        isRevealing = false;
    }
    public void EraseAtNormalizedUV(Vector2 uv)
    {
        if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
        {
            uvQueue.Enqueue(uv);
        }
    }

    public void EraseAtScreenPosition(Vector2 screenPos)
    {
        if (rect == null) rect = rawImage.rectTransform;

        //if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, null, out Vector2 localPos))
        // [改動] 為 World Space Canvas 提供有效 camera，避免位置轉換失敗
        Camera eventCam = rawImage.canvas.renderMode == RenderMode.WorldSpace ? rawImage.canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, eventCam, out Vector2 localPos))
        {
            Vector2 uv = new Vector2(
                (localPos.x + rect.rect.width * 0.5f) / rect.rect.width,
                (localPos.y + rect.rect.height * 0.5f) / rect.rect.height
            );
            //Debug.Log("uv : " + uv.x + ", " + uv.y);
            uvQueue.Enqueue(uv); // 改為加入 queue
        }
    }
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        //Vector2 screenPos = eventData.position;
        //EraseAtScreenPosition(screenPos);
    }
    private void Update()
    {
        {//一幀處裡多筆uv
            List<Vector2> uvBatch = new List<Vector2>();

            while (uvQueue.Count > 0 && uvBatch.Count < maxUvPerFrame)
            {
                uvBatch.Add(uvQueue.Dequeue());
            }

            if (uvBatch.Count > 0)
            {
                Debug.Log($"[Debug] 本幀處理筆數：{uvBatch.Count}");
                Debug.Log($"[Debug] Queue 剩餘數量：{uvQueue.Count}");
                DrawTransparentAt(uvBatch); // 使用你剛剛實作的多筆版本
            }
        }
    }
    public void SetMask(Sprite newMask)
    {
        maskImage = newMask;
    }
    public void SetBrush(Texture brush, Material material, float size)
    {
        this.brushTexture = brush;
        this.eraseMaterial = material;
        this.brushSize = size;
    }
}
