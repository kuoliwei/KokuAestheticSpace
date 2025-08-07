Shader "Custom/ScratchLayerBlend"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}                 // [新增] 筆刷貼圖（圓形）
        _BrushPos ("Brush Pos & Size (x, y, width, height)", Vector) = (0.5, 0.5, 0.1, 0.1)
        _AlphaDecayFactor ("Alpha Decay Factor", Range(0, 1)) = 0.1  // [新增] 控制透明度下降程度
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend One Zero   // [確保寫入 RenderTexture，不做混合]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;           // [新增]
            float4 _BrushPos;              // xy = 位置, zw = 尺寸
            float _AlphaDecayFactor;       // [新增]

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 col = tex2D(_MainTex, uv);

                float2 halfSize = _BrushPos.zw * 0.5;

                // [新增] 計算筆刷區域對應的 UV 座標
                float2 brushUV = (uv - (_BrushPos.xy - halfSize)) / _BrushPos.zw;

                // [新增] 若不在筆刷範圍內就略過
                if (brushUV.x < 0 || brushUV.x > 1 || brushUV.y < 0 || brushUV.y > 1)
                    return col;

                // [新增] 根據筆刷貼圖 alpha 來降低透明度
                float brushAlpha = tex2D(_BrushTex, brushUV).a;
                col.a = max(0, col.a - brushAlpha * _AlphaDecayFactor);

                // // [保留] 中心 ±0.01 的位置畫紅點
                // float2 delta = abs(uv - _BrushPos.xy);
                // if (delta.x < 0.01 && delta.y < 0.01)
                // {
                //     col.rgb = float3(1, 0, 0);
                // }

                return col;
            }
            ENDCG
        }
    }
}
