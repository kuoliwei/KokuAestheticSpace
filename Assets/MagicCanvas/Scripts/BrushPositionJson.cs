using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class BrushPositionJson
{
    public List<BrushData> data;
}

[System.Serializable]
public class BrushData
{
    public int roller_id;
    public float[] point; // 預期格式為 [x, y]，0~1 相對座標
}

[System.Serializable]
public class BrushUvData
{
    public List<Vector2> uvList = new List<Vector2>();
}
