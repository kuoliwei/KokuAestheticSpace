using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoseTypes
{
    /// <summary>骨架關節索引（0~16），對應你提供的命名。</summary>
    public enum JointId
    {
        Nose = 0,
        LeftEye = 1,
        RightEye = 2,
        LeftEar = 3,
        RightEar = 4,
        LeftShoulder = 5,
        RightShoulder = 6,
        LeftElbow = 7,
        RightElbow = 8,
        LeftWrist = 9,
        RightWrist = 10,
        LeftHip = 11,
        RightHip = 12,
        LeftKnee = 13,
        RightKnee = 14,
        LeftAnkle = 15,
        RightAnkle = 16,
    }

    public static class PoseSchema
    {
        public const int JointCount = 17;
    }

    /// <summary>
    /// 單一關節資料：x, y, z, conf（信心/可見度）。
    /// 建議 conf > 0 視為有效（之後解析器會依此設置）。
    /// </summary>
    [Serializable]
    public struct Joint
    {
        public float x;
        public float y;
        public float z;
        public float conf;

        public Joint(float x, float y, float z, float conf)
        {
            this.x = x; this.y = y; this.z = z; this.conf = conf;
        }

        /// <summary>是否可用（之後解析時可用 conf<=0 代表無效）。</summary>
        public bool IsValid => conf > 0f;

        /// <summary>方便取用的 XYZ。</summary>
        public Vector3 XYZ => new Vector3(x, y, z);

        public override string ToString() => $"({x:F3},{y:F3},{z:F3}|c={conf:F2})";
    }

    /// <summary>
    /// 單一人物的一整組 17 個關節。
    /// 使用 class 以避免值型別拷貝造成的陣列引用混淆與效能成本。
    /// </summary>
    [Serializable]
    public class PersonSkeleton
    {
        // 固定長度 17
        public Joint[] joints = new Joint[PoseSchema.JointCount];

        /// <summary>以 JointId 取用/設定關節。</summary>
        public Joint this[JointId id]
        {
            get => joints[(int)id];
            set => joints[(int)id] = value;
        }

        /// <summary>安全讀取（若資料不足、索引錯誤時不拋例外）。</summary>
        public bool TryGet(JointId id, out Joint j)
        {
            int idx = (int)id;
            if (joints != null && idx >= 0 && idx < joints.Length)
            {
                j = joints[idx];
                return true;
            }
            j = default;
            return false;
        }
    }

    /// <summary>
    /// 一個影格的多人骨架資料。
    /// 你的 Server 訊息是 {"<frameIndex>": [ persons... ]}，
    /// 解析後我們統一用 int frameIndex + List<PersonSkeleton> 表示。
    /// </summary>
    [Serializable]
    public class FrameSample
    {
        public int frameIndex;
        public List<PersonSkeleton> persons = new List<PersonSkeleton>();

        // Unity 收到時間
        public float recvTime;

        public FrameSample() { }
        public FrameSample(int frameIndex) { this.frameIndex = frameIndex; }
    }
}
