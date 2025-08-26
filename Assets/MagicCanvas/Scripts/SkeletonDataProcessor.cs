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
    [SerializeField] private float rayLength = 5f;  // Ray 最長距離
    [SerializeField] private BrushDataProcessor brushProcessor; // 連結 BrushDataProcessor

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
                TryShootWristRay(vis.joints[(int)JointId.LeftWrist]);
                TryShootWristRay(vis.joints[(int)JointId.RightWrist]);
            }

            //if (enableConsoleLog)
            //    Debug.Log(sb.ToString());
        }

        // 3) 刪除本幀沒出現的人
        PruneMissingPersons(seen);

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
