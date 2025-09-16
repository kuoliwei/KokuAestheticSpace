using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PoseTypes; // JointId / FrameSample / PersonSkeleton

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
    // ----- 內部狀態 -----
    class SkeletonVisual
    {
        public int personId;
        public GameObject root;
        public Transform[] joints = new Transform[PoseSchema.JointCount];
        public Renderer[] renderers = new Renderer[PoseSchema.JointCount];
    }

    private readonly Dictionary<int, SkeletonVisual> visuals = new Dictionary<int, SkeletonVisual>();
    private readonly List<int> _tmpToRemove = new List<int>();

    /// <summary>接收一幀骨架資料：更新/建立/刪除可視化，並（可選）列印到 Console。</summary>
    public void HandleSkeletonFrame(FrameSample frame)
    {
        if (frame == null || frame.persons == null)
            return;

        var seen = new HashSet<int>();
        var brushList = new List<BrushData>(); // ← 本幀所有手腕命中的 UV 都收這裡
        var effectList = new List<Vector2>();       // [NEW] InteractiveQuad → 用來互動/特效/按鈕

        bool anyPerson = frame.persons.Count > 0;

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

                // 新版（分流）
                if (TryGetWristUV(vis.joints[(int)JointId.LeftWrist], out var uvL, out var quadL)) // [NEW]
                {                                                                                  // [NEW]
                    if (quadL == QuadType.Painting)                                                // [NEW]
                        brushList.Add(new BrushData { point = new float[] { uvL.x, uvL.y } });     // [NEW]
                    else if (quadL == QuadType.Interactive)                                        // [NEW]
                        effectList.Add(uvL);                                                        // [NEW]
                }                                                                                  // [NEW]
                if (TryGetWristUV(vis.joints[(int)JointId.RightWrist], out var uvR, out var quadR))// [NEW]
                {                                                                                  // [NEW]
                    if (quadR == QuadType.Painting)                                                // [NEW]
                        brushList.Add(new BrushData { point = new float[] { uvR.x, uvR.y } });     // [NEW]
                    else if (quadR == QuadType.Interactive)                                        // [NEW]
                        effectList.Add(uvR);                                                        // [NEW]
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
                brushProcessor.HandleBrushData(brushList);     // Painting：照舊刮除

            if (effectList.Count > 0)
                brushProcessor.HandleEffectUV(effectList);     // [NEW] Interactive：互動/按鈕/特效
        }
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
