using PoseTypes; // JointId / FrameSample / PersonSkeleton
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class SkeletonDataProcessor : MonoBehaviour
{
    [Header("可視化")]
    public GameObject jointPrefab;
    public Transform skeletonParent;
    public Vector3 jointScale = Vector3.one;

    [Header("座標轉換（資料 -> 世界座標）")]
    public Vector3 positionScale = Vector3.one;
    public Vector3 positionOffset = Vector3.zero;
    //public bool invertY = false;

    [Tooltip("勾選 = 使用世界座標；否則使用 SkeletonParent 的本地座標")]
    public bool useWorldSpace = true;

    [Header("顯示條件")]
    public bool hideWhenLowConfidence = false;
    public float minConfidence = 0f;

    [Header("Console 列印")]
    public bool enableConsoleLog = true;     // ← 打開就會列印
    public bool logOnlyWhenSomeonePresent = true;

    // 在 class SkeletonDataProcessor 中新增
    [SerializeField] private LayerMask canvasLayer; // 指定 Quad (畫布) 的 layer
    [SerializeField] private float rayLength = 2f;  // Ray 最長距離
    [SerializeField] private BrushDataProcessor brushProcessor; // 連結 BrushDataProcessor
    [SerializeField] private AudioSource paintingAudioSource;

    // ===========================
    // [NEW] Quad 分類設定
    // 你可以用「Tag」來分辨是哪一塊 Quad（推薦）；
    // 若專案尚未設 Tag，也可在 Inspector 指定對應的 Collider 作為備援。
    // ===========================
    [Header("Quad 分類（擇一或並用）")]
    [SerializeField] private string paintingTag = "PaintingQuad";     // [NEW]
    [SerializeField] private string interactiveTag = "InteractiveQuad"; // [NEW]
    [SerializeField] private Collider paintingCollider;               // [NEW]
    [SerializeField] private Collider interactiveCollider;            // [NEW]
    // [NEW] 命中哪一塊的列舉
    private enum QuadType { None, Painting, Interactive } // [NEW]

    // [FPS] 監測：每秒統計一次
    [Header("骨架頻率統計")]
    [SerializeField] private bool logFpsEachSecond = true;   // 打開就每秒列印一次
    [SerializeField] private bool logOnlyWhenValid = true;  // 只在「有效幀>0」時列印

    // [FPS] 內部累計
    private int _recvFramesThisSec = 0;      // 本秒收到的總幀數（含無人）
    private int _validFramesThisSec = 0;     // 本秒「有效（有人）」的幀數
    private float _fpsWindowStart = 0f;      // 本秒起始時間
                                             // ----- 內部狀態 -----

    [Header("生成Trail特效")] // 生成Trail特效
    [SerializeField] private HandTrailEffectSpawner handTrailSpawner;
    class SkeletonVisual
    {
        public int personId;
        public GameObject root;
        public Transform[] joints = new Transform[PoseSchema.JointCount];
        public Renderer[] renderers = new Renderer[PoseSchema.JointCount];
    }

    private readonly Dictionary<int, SkeletonVisual> visuals = new Dictionary<int, SkeletonVisual>();
    private readonly List<int> _tmpToRemove = new List<int>();

    // [NEW] 用來記錄有效骨架幀間的間隔
    private float _lastValidFrameTime = -1f;         // 上一筆有效骨架時間
    private readonly List<float> _validIntervals = new List<float>(); // 本秒內所有間隔

    private HandSmoother leftHandSmoother = new HandSmoother(0.2f, 0.002f);
    private HandSmoother rightHandSmoother = new HandSmoother(0.2f, 0.002f);
    /// <summary>接收一幀骨架資料：更新/建立/刪除可視化，並（可選）列印到 Console。</summary>
    public void HandleSkeletonFrame(FrameSample frame)
    {
        if (frame == null || frame.persons == null)
            return;

        var seen = new HashSet<int>();
        var brushList = new List<BrushData>(); // ← 本幀所有手腕命中的 UV 都收這裡
        var effectList = new List<Vector2>();       // [NEW] InteractiveQuad → 用來互動/特效/按鈕
                                                    // [FPS] 累計「收到幀數」
        _recvFramesThisSec++;

        if (frame == null || frame.persons == null)
            return;
        bool anyPerson = frame.persons.Count > 0;
        // [FPS] 若為有效幀則累計
        if (anyPerson)
        {
            _validFramesThisSec++;

            // [NEW] 計算間隔並存入 list
            if (_lastValidFrameTime > 0f)
            {
                float interval = Time.time - _lastValidFrameTime;
                _validIntervals.Add(interval);
            }
            _lastValidFrameTime = Time.time;
        }

        // [FPS] 每秒輸出一次
        if (logFpsEachSecond && Time.time - _fpsWindowStart >= 1f)
        {
            if (!logOnlyWhenValid || _validFramesThisSec > 0)
            {
                string intervalsStr = _validIntervals.Count > 0
                    ? string.Join(", ", _validIntervals.Select(v => v.ToString("F2")))
                    : "N/A";

                //Debug.Log($"[Pose/FPS] recv={_recvFramesThisSec}/s, valid={_validFramesThisSec}/s, intervals=[{intervalsStr}]");
            }

            // [NEW] 輸出後清空
            _validIntervals.Clear();

            // 滾動到下一秒視窗
            _fpsWindowStart += 1f;
            _recvFramesThisSec = 0;
            _validFramesThisSec = 0;
        }
        // ---------- 可視化 & 列印 ----------
        for (int p = 0; p < frame.persons.Count; p++)
        {
            var person = frame.persons[p];
            if (person == null || person.joints == null || person.joints.Length < PoseSchema.JointCount)
                continue;

            seen.Add(p);

            // 1) 沒有就建立可視化
            if (!visuals.TryGetValue(p, out var vis))
            {
                vis = CreateVisualForPerson(p);
                visuals.Add(p, vis);
            }

            // 2) 逐關節：更新位置 & 顯示狀態；同時建立列印字串
            StringBuilder sb = enableConsoleLog ? new StringBuilder() : null;
            if (enableConsoleLog)
                sb.AppendLine($"[Pose] frame={frame.frameIndex} person={p} joints:");

            for (int j = 0; j < PoseSchema.JointCount; j++)
            {
                var data = person.joints[j]; // PoseTypes.Joint

                // 可視化座標
                Vector3 pos = new Vector3(
                    data.x * positionScale.x,
                    data.z * positionScale.z,   // Z → Unity 的 Y
                    data.y * positionScale.y    // Y → Unity 的 Z
                ) + positionOffset;

                if (useWorldSpace)
                    vis.joints[j].position = pos;
                else
                    vis.joints[j].localPosition = pos;



                // 顯示/隱藏
                var r = vis.renderers[j];
                if (r != null)
                {
                    if (hideWhenLowConfidence)
                        r.enabled = (data.conf > minConfidence);
                    else
                        r.enabled = true; // 確保未開啟過濾時一定顯示
                }

                // 列印
                if (enableConsoleLog)
                {
                    string name = ((JointId)j).ToString();
                    sb.AppendLine($"  {name,-14} => x={data.x:F3}, y={data.y:F3}, z={data.z:F3}, conf={data.conf:F2}");
                }
            }

            // 在 HandleSkeletonFrame(...) 最後，跑完 joints 更新後加上：
            if (vis != null)
            {
                //TryShootWristRay(vis.joints[(int)JointId.LeftWrist]);
                //TryShootWristRay(vis.joints[(int)JointId.RightWrist]);
                // 收集左右手腕命中的 UV（不立即送）
                // 舊版（不分流）
                // if (TryGetWristUV(vis.joints[(int)JointId.LeftWrist], out var uvL))
                //     brushList.Add(new BrushData { point = new float[] { uvL.x, uvL.y } });
                // if (TryGetWristUV(vis.joints[(int)JointId.RightWrist], out var uvR))
                //     brushList.Add(new BrushData { point = new float[] { uvR.x, uvR.y } });

                //// 新版（分流）
                //if (TryGetWristUV(vis.joints[(int)JointId.LeftWrist], out var uvL, out var quadL)) // [NEW]
                //{                                                                                  // [NEW]
                //    if (quadL == QuadType.Painting)                                                // [NEW]
                //        brushList.Add(new BrushData { point = new float[] { uvL.x, uvL.y } });     // [NEW]
                //    else if (quadL == QuadType.Interactive)                                        // [NEW]
                //        effectList.Add(uvL);                                                        // [NEW]
                //}                                                                                  // [NEW]
                //if (TryGetWristUV(vis.joints[(int)JointId.RightWrist], out var uvR, out var quadR))// [NEW]
                //{                                                                                  // [NEW]
                //    if (quadR == QuadType.Painting)                                                // [NEW]
                //        brushList.Add(new BrushData { point = new float[] { uvR.x, uvR.y } });     // [NEW]
                //    else if (quadR == QuadType.Interactive)                                        // [NEW]
                //        effectList.Add(uvR);                                                        // [NEW]
                //}
                // 先取出左右髖關節，計算腰部高度
                if (person.TryGet(JointId.LeftHip, out var leftHip) &&
                    person.TryGet(JointId.RightHip, out var rightHip))
                {
                    //float hipY = (leftHip.y + rightHip.y) / 2f; // 腰部基準高度
                    float hipZ = ((leftHip.z + rightHip.z) / 2f) * 1.2f;// 基準高度上調 1.2 倍

                    // 取左右手腕
                    var lw = person.joints[(int)JointId.LeftWrist];
                    var rw = person.joints[(int)JointId.RightWrist];
                    Debug.Log($"左手高度：{lw.z}，右手高度：{rw.z}，骨盆高度{hipZ}");
                    // 只有手腕高於腰部才允許射線流程
                    if (lw.z > hipZ)
                    {
                        if (TryGetWristUV(vis.joints[(int)JointId.LeftWrist], out var uvL, out var quadL))
                        {
                            uvL = leftHandSmoother.Smooth(uvL);   // 平滑處理
                            if (quadL == QuadType.Painting)
                                brushList.Add(new BrushData { point = new float[] { uvL.x, uvL.y } });
                            else if (quadL == QuadType.Interactive)
                            {
                                handTrailSpawner.UpdateHand(p, true, uvL);
                                //effectList.Add(uvL);
                            }
                        }
                    }
                    if (rw.z > hipZ)
                    {
                        if (TryGetWristUV(vis.joints[(int)JointId.RightWrist], out var uvR, out var quadR))
                        {
                            uvR = rightHandSmoother.Smooth(uvR);  // 平滑處理
                            if (quadR == QuadType.Painting)
                                brushList.Add(new BrushData { point = new float[] { uvR.x, uvR.y } });
                            else if (quadR == QuadType.Interactive)
                            {
                                handTrailSpawner.UpdateHand(p, false, uvR);
                                //effectList.Add(uvR);
                            }
                        }
                    }
                }
            }

            //if (enableConsoleLog)
            //    Debug.Log(sb.ToString());
        }

        // 3) 刪除本幀沒出現的人
        PruneMissingPersons(seen);
        // 本幀一次送出（1~4 筆都可）
        // 4) 輸出到 BrushDataProcessor
        if (brushProcessor != null)
        {
            if (brushList.Count > 0)
            {
                paintingAudioSource.volume = 1;
                brushProcessor.HandleBrushData(brushList);     // Painting：照舊刮除
            }
            else
            {
                paintingAudioSource.volume = 0;
            }

            //if (effectList.Count > 0)
            //    brushProcessor.HandleEffectUV(effectList);     // [NEW] Interactive：互動/按鈕/特效
        }

        // 可視化完成後才計算延遲
        DateTime now = DateTime.Now;

        // 時分秒用 DateTime，秒的小數部分自己拼上去
        string timeStr = $"{now:HH:mm}:{now.Second + now.Millisecond / 1000.0:F6}";
        float delay = (Time.realtimeSinceStartup - frame.recvTime) * 1000f;
        //Debug.Log($"[Latency] Frame {frame.frameIndex} 完整顯示延遲 = {delay:F1} ms"+ "\n$\"[Time] 收到時間 {timeStr}\"");

        // 4) 若開了「有人才列印」且這幀沒人，印一行提示
        if (enableConsoleLog && !anyPerson && !logOnlyWhenSomeonePresent)
        {
            Debug.Log($"[Pose] frame={frame.frameIndex} 無人物資料。");
        }
    }

    // 建立一位人員的 17 顆球
    private SkeletonVisual CreateVisualForPerson(int personId)
    {
        var vis = new SkeletonVisual { personId = personId };

        vis.root = new GameObject($"Person_{personId}");
        if (skeletonParent != null)
            vis.root.transform.SetParent(skeletonParent, worldPositionStays: false);

        for (int j = 0; j < PoseSchema.JointCount; j++)
        {
            string jointName = ((JointId)j).ToString();
            GameObject go;

            if (jointPrefab != null)
                go = Instantiate(jointPrefab, vis.root.transform);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(vis.root.transform, worldPositionStays: false);
            }

            go.name = $"j_{j}_{jointName}";
            go.transform.localScale = jointScale;

            vis.joints[j] = go.transform;
            vis.renderers[j] = go.GetComponent<Renderer>();
        }

        return vis;
    }

    private void PruneMissingPersons(HashSet<int> seen)
    {
        _tmpToRemove.Clear();
        foreach (var kv in visuals)
            if (!seen.Contains(kv.Key)) _tmpToRemove.Add(kv.Key);

        foreach (var id in _tmpToRemove)
        {
            var vis = visuals[id];
            if (vis != null && vis.root != null)
                Destroy(vis.root);
            visuals.Remove(id);
        }
    }

    //private bool TryGetWristUV(Transform wrist, out Vector2 uv)
    //{
    //    uv = default;
    //    if (wrist == null) return false;

    //    Ray ray = new Ray(wrist.position, wrist.forward);
    //    if (Physics.Raycast(ray, out RaycastHit hit, rayLength, canvasLayer))
    //    {
    //        uv = hit.textureCoord; // 0..1，左下為原點（Unity 的 Texture 座標系）
    //        Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f);
    //        return true;
    //    }
    //    Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 0.1f);
    //    return false;
    //}
    // ===========================
    // 命中 → 取得 UV + 分類（Painting / Interactive）
    // ===========================
    private bool TryGetWristUV(Transform wrist, out Vector2 uv, out QuadType quad) // [NEW signature]
    {
        uv = default;
        quad = QuadType.None; // [NEW]
        if (wrist == null) return false;

        Ray ray = new Ray(wrist.position, wrist.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, canvasLayer))
        {
            uv = hit.textureCoord; // 0..1，左下為原點（Unity 的 Texture 座標系）
            quad = ClassifyQuad(hit.collider); // [NEW]
            Debug.DrawRay(ray.origin, ray.direction * hit.distance,
                          quad == QuadType.Painting ? Color.green :
                          quad == QuadType.Interactive ? Color.cyan : Color.yellow, 0.1f); // [NEW]
            return true;
        }
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 0.1f);
        return false;
    }
    // ============================================================
    // 命中偵測 (雙方向) - 同時射出 +Z 與 -X 兩條射線
    // 若兩條皆命中會輸出兩組 UV
    // ============================================================
    private int TryGetWristUVs(Transform wrist, List<Vector2> uvs, List<QuadType> quads)
    {
        if (wrist == null) return 0;

        int hitCount = 0;
        RaycastHit hit;

        // --- 第一條：朝 +Z 方向 ---
        Ray rayZ = new Ray(wrist.position, wrist.forward);
        if (Physics.Raycast(rayZ, out hit, rayLength, canvasLayer))
        {
            uvs.Add(hit.textureCoord);
            quads.Add(ClassifyQuad(hit.collider));
            Debug.DrawRay(rayZ.origin, rayZ.direction * hit.distance, Color.cyan, 0.1f);
            hitCount++;
        }

        // --- 第二條：朝 -X 方向 ---
        Ray rayX = new Ray(wrist.position, -wrist.right);
        if (Physics.Raycast(rayX, out hit, rayLength, canvasLayer))
        {
            uvs.Add(hit.textureCoord);
            quads.Add(ClassifyQuad(hit.collider));
            Debug.DrawRay(rayX.origin, rayX.direction * hit.distance, Color.green, 0.1f);
            hitCount++;
        }

        // --- 若兩者都未命中 ---
        if (hitCount == 0)
        {
            Debug.DrawRay(wrist.position, wrist.forward * rayLength, Color.red, 0.1f);
            Debug.DrawRay(wrist.position, -wrist.right * rayLength, Color.red, 0.1f);
        }

        return hitCount;
    }


    // [NEW] 用 Tag / 指定 Collider / 名稱三種方式做分類（任一命中即可）
    private QuadType ClassifyQuad(Collider col) // [NEW]
    {
        if (col == null) return QuadType.None;

        // 1) Tag 優先
        if (!string.IsNullOrEmpty(paintingTag) && col.CompareTag(paintingTag)) return QuadType.Painting;
        if (!string.IsNullOrEmpty(interactiveTag) && col.CompareTag(interactiveTag)) return QuadType.Interactive;

        // 2) 指定 Collider（保險備援）
        if (paintingCollider && col == paintingCollider) return QuadType.Painting;
        if (interactiveCollider && col == interactiveCollider) return QuadType.Interactive;

        // 3) 名稱 fallback（不建議長期使用）
        var n = col.gameObject.name;
        if (!string.IsNullOrEmpty(n))
        {
            if (n.Contains("Painting")) return QuadType.Painting;
            if (n.Contains("Interactive")) return QuadType.Interactive;
        }
        return QuadType.None;
    }
    private void TryShootWristRay(Transform wrist)
    {
        if (wrist == null) return;

        Ray ray = new Ray(wrist.position, wrist.forward); // 從手腕往前射出
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, canvasLayer))
        {
            // 取 quad 上的 UV 座標
            Vector2 uv = hit.textureCoord;

            // 呼叫 BrushDataProcessor → 在 ScratchCard 抹除
            brushProcessor.HandleBrushData(new List<BrushData> {
            new BrushData { point = new float[]{ uv.x, uv.y } }
        });

            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f); // Debug 用
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 0.1f);
        }
    }
}
// 專門處理手部座標的平滑器
public class HandSmoother
{
    private Vector2 lastSmoothed;
    private bool hasValue = false;

    private readonly float smoothFactor;
    private readonly float minThreshold;

    public HandSmoother(float smoothFactor = 0.2f, float minThreshold = 0.002f)
    {
        this.smoothFactor = smoothFactor;
        this.minThreshold = minThreshold;
    }

    public Vector2 Smooth(Vector2 current)
    {
        if (!hasValue)
        {
            lastSmoothed = current;
            hasValue = true;
            return current;
        }

        // 如果變動太小，忽略抖動
        if (Vector2.Distance(lastSmoothed, current) < minThreshold)
            return lastSmoothed;

        // 指數平滑 (EMA)
        lastSmoothed = Vector2.Lerp(lastSmoothed, current, smoothFactor);
        return lastSmoothed;
    }
}
