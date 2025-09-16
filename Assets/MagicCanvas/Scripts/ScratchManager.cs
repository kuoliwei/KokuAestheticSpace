using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScratchManager : MonoBehaviour
{
    [Header("Scratch")]
    public List<Sprite> maskImages;
    public List<Texture> backgroundImages;
    public List<RawImage> backgroundRenderers;
    public List<ScratchCard> scratchCards;
    public float clearThreshold = 0.6f;
    public float revealHoldTime = 15f;
    public float restoreSpeed = 1f;
    public Texture brushTexture;
    public Material eraseMaterial;
    public float brushSize = 64f;

    [Header("Koku Aesthetic UI")]
    [SerializeField] private RawImage previewImage; // TakingPhotoPanel/RawImage（即時預覽）
    [SerializeField] private RawImage resultImage;  // Tex_HiddenImage 的 RawImage（風格化結果）
    [SerializeField] private GameObject takingPhotoPanel;
    [SerializeField] private GameObject scratchSurface;
    [SerializeField] private WebCamController webCam;
    [SerializeField] private PanelFlowController flow; // 新增

    private int currentIndex = 0;
    private bool imageFullyRevealed = false;
    private readonly List<Coroutine> restoreRoutines = new();
    private readonly HashSet<ScratchCard> revealedCards = new();

    private void Start()
    {
        foreach (var card in scratchCards)
        {
            card.Init();
            card.SetBrush(brushTexture, eraseMaterial, brushSize);
        }
        ShowImageAt(0);
    }

    // === 可庫美學：開始預覽 ===
    public void UI_StartPreview()
    {
        {  //  包含風格轉換
            takingPhotoPanel.SetActive(true);
            scratchSurface.SetActive(false);
        }
        {  //  不包含風格轉換
            //takingPhotoPanel.SetActive(false);
            //scratchSurface.SetActive(true);
        }

        webCam.OpenCamera(webCam.selectedDeviceName);                         // 開鏡頭
        if (previewImage != null) previewImage.texture = webCam.PreviewTexture; // 綁預覽貼圖
    }

    // === 可庫美學：拍照並風格化 ===
    public void UI_CaptureAndStylize(string styleName)
    {
        StartCoroutine(webCam.CaptureAndStylize(styleName, OnStyledReady));
    }

    // 風格化完成：顯示在 Tex_HiddenImage，切回刮刮介面
    private void OnStyledReady(Texture2D tex)
    {
        if (tex == null) return;

        if (resultImage != null) resultImage.texture = tex; // 顯示到 Tex_HiddenImage
        // 同步更新所有刮刮卡底圖
        foreach (var r in backgroundRenderers) r.texture = tex;
        foreach (var c in scratchCards) { c.ResetScratch(); }

        takingPhotoPanel.SetActive(false);
        scratchSurface.SetActive(true);
    }

    // ==== 既有刮刮卡流程（原樣保留） ====
    public void ShowImageAt(int index)
    {
        if (index >= backgroundImages.Count) index = 0;
        currentIndex = index;
        imageFullyRevealed = false;

        foreach (var routine in restoreRoutines) if (routine != null) StopCoroutine(routine);
        restoreRoutines.Clear();
        revealedCards.Clear();

        foreach (var renderer in backgroundRenderers) renderer.texture = backgroundImages[index];
        foreach (var card in scratchCards)
        {
            card.SetMask(maskImages[index]);
            card.ResetScratch();
        }
    }

    private IEnumerator AutoRestoreAfterDelay(ScratchCard target)
    {
        yield return new WaitForSeconds(revealHoldTime);
        yield return target.SmoothRestoreMask(restoreSpeed);
        if (revealedCards.Count >= scratchCards.Count) ShowImageAt(GetRandomIndex());
    }

    private int GetRandomIndex()
    {
        if (backgroundImages.Count <= 1) return 0;
        int randomIndex;
        do { randomIndex = Random.Range(0, backgroundImages.Count); }
        while (randomIndex == currentIndex);
        return randomIndex;
    }

    private void Update()
    {
        if (imageFullyRevealed) return;

        foreach (var card in scratchCards)
        {
            if (!revealedCards.Contains(card) && card.GetClearedRatio() >= clearThreshold)
            {
                revealedCards.Add(card);
                card.ShowFullImage();
                // 串接 PanelFlow：當所有卡都達標，立即通知流程切換
                if (revealedCards.Count >= scratchCards.Count && !imageFullyRevealed)
                {
                    imageFullyRevealed = true;
                    if (flow != null) flow.Sys_OnScratchRevealComplete();
                }
                //var routine = StartCoroutine(AutoRestoreAfterDelay(card));
                //restoreRoutines.Add(routine);
            }
            if (revealedCards.Count >= scratchCards.Count) imageFullyRevealed = true;
        }
    }
}
