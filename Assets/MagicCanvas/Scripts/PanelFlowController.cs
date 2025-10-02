using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelFlowController : MonoBehaviour
{
    public enum FlowState
    {
        Connect,               // ConnectPanel
        Illustrate,            // IllustratePanel（說明頁）
        AuthorizationModal,    // AuthorizationDeclarationPanel（覆蓋在 Illustrate 上）
        TakingPhoto,           // TakingPhotoPanel（倒數→拍照）
        ChooseStyle,           // 風格選擇
        WaitingTransform,      // 等待轉換
        TransformFinishHint,   // 轉換完成提示（倒數後進入刮刮樂）
        ScratchGame,           // 只顯示 Tex_HiddenImage + ScratchSurface
        DragPicturesHint       // 提示用戶向左滑
    }

    [Header("Panels")]
    [SerializeField] GameObject connectPanel;
    [SerializeField] GameObject illustratePanel;
    [SerializeField] GameObject authorizationDeclarationPanel;
    [SerializeField] GameObject takingPhotoPanel;
    [SerializeField] GameObject chooseStylePanel;
    [SerializeField] GameObject waitingStyleTransformPanel;
    [SerializeField] GameObject styleTransformFinishHintPanel;
    [SerializeField] GameObject scratchSurfacePanel;    // 遮罩
    [SerializeField] GameObject texHiddenImagePanel;    // 底圖 RawImage 所在
    [SerializeField] GameObject dragPicturesHintPanel;

    [Header("UI Refs")]
    [SerializeField] Text takingPhotoCountdownText;     // TakingPhotoPanel 上的倒數 Text

    [Header("Timings (seconds)")]
    [SerializeField] int takingPhotoCountdown = 3;      // 拍照倒數
    [SerializeField] int transformFinishStaySeconds = 5;// 完成提示停留秒數
    [SerializeField] int revealStaySeconds = 3;         // 圖片揭示後停留秒數

    Coroutine runningRoutine;
    public FlowState State { get; private set; } = FlowState.Connect;

    [Header("Connect deps")]
    [SerializeField] WebSocketConnectUI wsUI; // 在 Inspector 指派
    [Header("Controllers")]
    [SerializeField] TakingPhotoController takingPhotoCtrl; // 指到 TakingPhotoPanel 上的新控制器
                                                            // PanelFlowController.cs
    [SerializeField] ChooseStyleController chooseStyleCtrl;  // ← 新增欄位
    private string pendingTaskId;

    [SerializeField] private WaitingStyleController waitingStyleCtrl;    // 欄位（新增）
    // pendingTaskId：你在 HandleStyleTaskCreated() 內已經有存起來
    [SerializeField] private StyleTransformFinishHintController finishHintCtrl;
    [SerializeField] private DragPicturesHintController dragPicturesHintCtrl;
    [SerializeField] private ScratchManager scratchManager;
    [SerializeField] private HandReferenceDotSpawner refDotSpawner; // 參考紅點顯示


    void Start()
    {
        if (wsUI != null) wsUI.OnConnectResult += HandleConnectResult;
        if (takingPhotoCtrl != null)
            takingPhotoCtrl.OnCaptureCompleted += HandleCaptureCompleted; // ★ 訂閱完成事件

        if (chooseStyleCtrl != null)
        {
            chooseStyleCtrl.OnStyleTaskCreated += HandleStyleTaskCreated; // ← 成功：拿到 taskId
            chooseStyleCtrl.OnStyleTaskFailed += HandleStyleTaskFailed;  // ← 失敗：留在原面板
        }
        // 等待轉換（查進度 / 下載）
        if (waitingStyleCtrl != null)
        {
            waitingStyleCtrl.OnProgressCompleted += HandleWaitingProgressCompleted;     // 100% 才觸發
            waitingStyleCtrl.OnProgressNotCompleted += HandleWaitingProgressNotCompleted;  // 尚未完成 → 再查一次
            waitingStyleCtrl.OnProgressFailed += HandleWaitingProgressFailed;        // 真正失敗
            waitingStyleCtrl.OnDownloadSucceeded += HandleWaitingDownloadSucceeded;     // 下載成功 → 進完成提示
            waitingStyleCtrl.OnDownloadFailed += HandleWaitingDownloadFailed;        // 下載失敗
        }
        if (finishHintCtrl != null)
            finishHintCtrl.OnFinishCountdown += UI_OnGoToScratchGame;
        if (dragPicturesHintCtrl != null)
            dragPicturesHintCtrl.OnDragSimulated += HandleDragSimulated;
        GoTo(FlowState.Connect);
    }

    void OnDestroy()
    {
        if (wsUI != null) wsUI.OnConnectResult -= HandleConnectResult;
        if (takingPhotoCtrl != null)
            takingPhotoCtrl.OnCaptureCompleted -= HandleCaptureCompleted;

        if (chooseStyleCtrl != null)
        {
            chooseStyleCtrl.OnStyleTaskCreated -= HandleStyleTaskCreated;
            chooseStyleCtrl.OnStyleTaskFailed -= HandleStyleTaskFailed;
        }

        if (waitingStyleCtrl != null)
        {
            waitingStyleCtrl.OnProgressCompleted -= HandleWaitingProgressCompleted;
            waitingStyleCtrl.OnProgressNotCompleted -= HandleWaitingProgressNotCompleted;
            waitingStyleCtrl.OnProgressFailed -= HandleWaitingProgressFailed;
            waitingStyleCtrl.OnDownloadSucceeded -= HandleWaitingDownloadSucceeded;
            waitingStyleCtrl.OnDownloadFailed -= HandleWaitingDownloadFailed;
            waitingStyleCtrl.CancelAll(); // 保險清理協程
        }
        if (finishHintCtrl != null)
            finishHintCtrl.OnFinishCountdown -= UI_OnGoToScratchGame;
        if (dragPicturesHintCtrl != null)
            dragPicturesHintCtrl.OnDragSimulated -= HandleDragSimulated;

    }

    // ======================
    // Public: 給 UI/系統呼叫
    // ======================

    // ConnectPanel：按「連線」
    public void UI_OnConnectClicked()
    {
        //Debug.Log("按下連線按鍵");
        if (wsUI != null) wsUI.TryConnect();
    }

    private void HandleConnectResult(bool ok, string msg)
    {
        if (ok) GoTo(FlowState.Illustrate);
        // 失敗：WebSocketConnectUI 會在 message Text 顯示，不換面板
    }

    // IllustratePanel：按「Start」→ 打開授權聲明
    public void UI_OnShowAuthorization()
    {
        GoTo(FlowState.AuthorizationModal);
    }

    // AuthorizationDeclarationPanel：按「不同意」
    public void UI_OnDeclineAuthorization()
    {
        // 關閉聲明，回到說明面板停留
        authorizationDeclarationPanel.SetActive(false);
        GoTo(FlowState.Illustrate);
    }

    // AuthorizationDeclarationPanel：按「同意」
    public void UI_OnAcceptAuthorization()
    {
        GoTo(FlowState.TakingPhoto);
    }
    public void UI_OnTakingPhotoDone()
    {
        //Debug.Log("按下拍照按鍵");
        if (takingPhotoCtrl != null)
            takingPhotoCtrl.StartCaptureCountdown();  // 不切換狀態，只把照片顯示在 previewRawImage
        //GoTo(FlowState.ChooseStyle);
    }
    private void HandleCaptureCompleted()
    {
        // 現在才正式前往下一面板（選風格）
        GoTo(FlowState.ChooseStyle);
    }
    // ChooseStyle：點選任一風格
    public void UI_OnChooseStyle(string styleIndex)
    {
        Debug.Log("按下風格選擇按鍵");
        chooseStyleCtrl.ShowResult($"開始嘗試風格轉換...");
        // TODO: 告知 AI 開始風格轉換（styleIndex）
        chooseStyleCtrl.ChooseStyle(styleIndex);
    }

    // 系統事件：當 AI 風格轉換完成 & 已下載好成品圖
    public void UI_OnFakeStyleTransformDone()
    {
        // （之後再補圖片下載/顯示）
        GoTo(FlowState.TransformFinishHint);
    }
    // StyleTransformFinishHint → ScratchGame
    public void UI_OnGoToScratchGame()
    {
        GoTo(FlowState.ScratchGame);
    }
    // 刮刮樂內部事件：當刮除超過 60% → 完整揭示後呼叫
    public void Sys_OnScratchRevealComplete()
    {
        Debug.Log("執行Sys_OnScratchRevealComplete");
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        UI_OnGoToDragHint();
        //runningRoutine = StartCoroutine(ShowRevealedThenDragHint());
    }
    public void UI_OnGoToDragHint()
    {
        Debug.Log("執行UI_OnGoToDragHint");
        GoTo(FlowState.DragPicturesHint);
    }
    private void HandleDragSimulated()
    {
        Debug.Log("[PanelFlowController] 假滑動完成 → 回到 IllustratePanel");
        GoTo(FlowState.Illustrate);
    }

    // ======================
    // Core: 狀態切換
    // ======================
    void GoTo(FlowState next)
    {
        // 若是從 TakingPhoto 離開，先關鏡頭
        if (State == FlowState.TakingPhoto && takingPhotoCtrl != null)
            takingPhotoCtrl.Exit();
        State = next;
        // 統一先全部關閉
        SetAll(false);

        // 若有長流程/倒數，切換時先停掉舊的
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        switch (next)
        {
            case FlowState.Connect:
                connectPanel.SetActive(true);
                break;

            case FlowState.Illustrate:
                illustratePanel.SetActive(true);
                refDotSpawner.ChangeSprite(HandReferenceDotSpawner.dotSprites.hand);
                break;

            case FlowState.AuthorizationModal:
                illustratePanel.SetActive(true);                // 背後仍顯示
                authorizationDeclarationPanel.SetActive(true);  // 蓋在上面
                break;

            case FlowState.TakingPhoto:
                takingPhotoPanel.SetActive(true);
                // 不做倒數，由控制器開鏡頭
                if (takingPhotoCtrl != null)
                {
                    takingPhotoCtrl.Enter();
                    takingPhotoCtrl.StartCaptureCountdown();
                }
                //runningRoutine = StartCoroutine(Co_TakingPhotoCountdown());
                break;

            case FlowState.ChooseStyle:
                chooseStylePanel.SetActive(true);
                if (chooseStyleCtrl != null && takingPhotoCtrl != null)
                    chooseStyleCtrl.SetSourcePhoto(takingPhotoCtrl.LastPhotoRotated); // ← 關鍵：給來源照片
                break;

            case FlowState.WaitingTransform:
                waitingStyleTransformPanel.SetActive(true);
                if (waitingStyleCtrl != null)
                    waitingStyleCtrl.BeginTracking(pendingTaskId);
                // 等待期間由外部（AI）完成後，呼叫 Sys_OnStyleTransformDone
                break;

            case FlowState.TransformFinishHint:
                styleTransformFinishHintPanel.SetActive(true);
                if (finishHintCtrl != null)
                    finishHintCtrl.BeginCountdown(transformFinishStaySeconds);
                //runningRoutine = StartCoroutine(Co_WaitThenEnterScratch());
                break;

            case FlowState.ScratchGame:
                // 只顯示底圖與刮刮樂遮罩
                texHiddenImagePanel.SetActive(true);
                scratchSurfacePanel.SetActive(true);
                //scratchManager.ResetImageFullyRevealed();
                scratchManager.StartCountingExperienceTime();
                refDotSpawner.ChangeSprite(HandReferenceDotSpawner.dotSprites.translucentRound);
                break;

            case FlowState.DragPicturesHint:
                refDotSpawner.ChangeSprite(HandReferenceDotSpawner.dotSprites.hand);
                dragPicturesHintPanel.SetActive(true);
                texHiddenImagePanel.SetActive(true);
                break;
        }
    }

    void SetAll(bool active)
    {
        connectPanel.SetActive(active);
        illustratePanel.SetActive(active);
        authorizationDeclarationPanel.SetActive(active);
        takingPhotoPanel.SetActive(active);
        chooseStylePanel.SetActive(active);
        waitingStyleTransformPanel.SetActive(active);
        styleTransformFinishHintPanel.SetActive(active);
        scratchSurfacePanel.SetActive(active);
        texHiddenImagePanel.SetActive(active);
        dragPicturesHintPanel.SetActive(active);
    }

    // ======================
    // Routines
    // ======================
    IEnumerator Co_TakingPhotoCountdown()
    {
        int t = Mathf.Max(1, takingPhotoCountdown);
        while (t > 0)
        {
            if (takingPhotoCountdownText) takingPhotoCountdownText.text = t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }

        // TODO: 這裡觸發真正拍照邏輯（快門/擷取）
        // 拍完進入選風格
        GoTo(FlowState.ChooseStyle);
    }

    IEnumerator Co_WaitThenEnterScratch()
    {
        yield return new WaitForSeconds(Mathf.Max(0, transformFinishStaySeconds));
        // 進入刮刮樂：只開啟底圖 + 遮罩
        GoTo(FlowState.ScratchGame);
    }

    IEnumerator ShowRevealedThenDragHint()
    {
        // 圖片已揭示：短暫展示
        yield return new WaitForSeconds(Mathf.Max(0, revealStaySeconds));
        GoTo(FlowState.DragPicturesHint);
    }
    private void HandleStyleTaskCreated(string taskId)
    {
        Debug.Log("呼叫HandleStyleTaskCreated()");
        pendingTaskId = taskId;           // 先存起來

        // 顯示訊息（可選）
        chooseStyleCtrl.ShowResult("風格任務建立成功，正在等待伺服器處理…");
        GoTo(FlowState.WaitingTransform); // ← 成功才切到等待面板
                                          // 之後你做了 WaitingStyleController，再在此把 taskId 傳給它 BeginTracking(taskId)
    }

    private void HandleStyleTaskFailed(string reason)
    {
        Debug.Log("呼叫HandleStyleTaskFailed()");
        Debug.LogError($"Style task create failed: {reason}");
        // 留在 ChooseStyle 面板，或在面板上顯示錯誤字串
        // 顯示錯誤訊息，停留在 ChooseStylePanel
        chooseStyleCtrl.ShowResult($"任務建立失敗：{reason}");

        //{// 測試用，即使失敗也進下一步
        //    GoTo(FlowState.WaitingTransform);
        //}
    }

    // 等待面板：進度 = 100% → 進入下載階段
    private void HandleWaitingProgressCompleted()
    {
        if (waitingStyleCtrl != null)
            waitingStyleCtrl.BeginDownload();
    }

    // 等待面板：進度未完成 → 再查一次（可加微延遲避免過頻）
    private void HandleWaitingProgressNotCompleted()
    {
        StartCoroutine(RequeryAfterDelay(0.5f));
    }
    private IEnumerator RequeryAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (State == FlowState.WaitingTransform && waitingStyleCtrl != null)
            waitingStyleCtrl.BeginTracking(pendingTaskId);
    }

    // 等待面板：查進度真正失敗
    private void HandleWaitingProgressFailed(string reason)
    {
        Debug.LogError($"Progress failed: {reason}");
        // 視需求：停留在 Waiting 顯示錯誤，或退回 ChooseStyle
    }

    // 等待面板：下載成功 → 進完成提示
    private void HandleWaitingDownloadSucceeded()
    {
        GoTo(FlowState.TransformFinishHint);
    }

    // 等待面板：下載失敗
    private void HandleWaitingDownloadFailed(string reason)
    {
        Debug.LogError($"Download failed: {reason}");
        // 視需求處理（停留或退回）
    }
}
