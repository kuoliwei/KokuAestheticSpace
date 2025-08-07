using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;


public class ScratchManager : MonoBehaviour
{
    public List<Sprite> maskImages;     // 遮罩圖片（共用）
    public List<Texture> backgroundImages; // 底圖（會顯示在 RawImage 背後）
    public List<RawImage> backgroundRenderers; // 顯示底圖的 RawImage
    public List<ScratchCard> scratchCards; // [修改] 改為多個 ScratchCard

    public float clearThreshold = 0.6f; // 觸發立即揭曉的百分比
    public float revealHoldTime = 15f;  // 顯示後等待恢復的秒數
    public float restoreSpeed = 1f; // [新增] 從 ScratchCard 移過來，由 Manager 控制還原速度

    public Texture brushTexture;        // 新增：由 Manager 統一提供筆刷
    public Material eraseMaterial;      // 新增：由 Manager 統一提供刮除材質
    public float brushSize = 64f;       // 新增：由 Manager 控一律設定筆刷大小

    private int currentIndex = 0;
    private bool imageFullyRevealed = false;
    private List<Coroutine> restoreRoutines = new List<Coroutine>();      // 每卡一個協程
    private HashSet<ScratchCard> revealedCards = new HashSet<ScratchCard>(); // 記錄已揭曉卡片
    void Start()
    {
        foreach (var card in scratchCards)
        {
            card.Init();
            card.SetBrush(brushTexture, eraseMaterial, brushSize); // [新增] 初始化筆刷參數
        }
        ShowImageAt(0);
    }

    public void ShowImageAt(int index)
    {
        if (index >= backgroundImages.Count) index = 0;
        currentIndex = index;
        imageFullyRevealed = false;
        // [新增] 停止所有協程
        foreach (var routine in restoreRoutines)
        {
            if (routine != null) StopCoroutine(routine);
        }
        restoreRoutines.Clear();
        revealedCards.Clear(); // 清空揭曉紀錄
        foreach (var renderer in backgroundRenderers)
        {
            renderer.texture = backgroundImages[index];
        }
        // 每張卡片個別設定遮罩
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
        // 所有卡都揭曉過且協程跑完才切換
        if (revealedCards.Count >= scratchCards.Count)
        {
            ShowImageAt(GetRandomIndex());
        }
    }
    private int GetRandomIndex()
    {
        if (backgroundImages.Count <= 1)
            return 0;

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, backgroundImages.Count);
        }
        while (randomIndex == currentIndex); // 避免重複同一張

        return randomIndex;
    }
    private void Update()
    {
        if (!imageFullyRevealed)
        {
            foreach (var card in scratchCards)
            {
                if (!revealedCards.Contains(card) && card.GetClearedRatio() >= clearThreshold)
                {
                    revealedCards.Add(card);                // 加入已揭曉集合
                    card.ShowFullImage();                   // 顯示全圖
                    var routine = StartCoroutine(AutoRestoreAfterDelay(card));
                    restoreRoutines.Add(routine);
                }
                if (revealedCards.Count >= scratchCards.Count)
                {
                    imageFullyRevealed = true; // 全部揭曉後只記一次
                }
            }
        }
    }
}
