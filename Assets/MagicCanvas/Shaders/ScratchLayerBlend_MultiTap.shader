Shader "Custom/ScratchLayerBlend_MultiTap"
{
    Properties
    {
        _MainTex ("Mask (RenderTexture)", 2D) = "white" {}
        _BrushTex ("Brush", 2D) = "white" {}
        _BrushPos ("Brush Pos & Size (x, y, width, height)", Vector) = (0.5, 0.5, 0.1, 0.1)
        _AlphaDecayFactor ("Alpha Decay Factor", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        Pass
        {
            ZWrite Off
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BrushTex;

            float4 _BrushPos; // 單筆備用欄位
            float _AlphaDecayFactor;
            float4 _BrushMultiPos[32]; // 多筆資料，每筆：x, y = center, z, w = size

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
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

                for (int k = 0; k < 32; k++)
                {
                    float4 brush = _BrushMultiPos[k];

                        // 提早退出：無效筆刷視為終止
                    if (brush.z == 0.0 || brush.w == 0.0)
                        break;

                    float2 halfSize = brush.zw * 0.5;
                    float2 brushUV = (uv - (brush.xy - halfSize)) / brush.zw;

                    // 範圍外直接返回當前像素值（early return）
                    if (brushUV.x < 0.0 || brushUV.x > 1.0 ||
                        brushUV.y < 0.0 || brushUV.y > 1.0)
                        continue;

                    float brushAlpha = tex2D(_BrushTex, brushUV).a;
                    col.a = max(0.0, col.a - brushAlpha * _AlphaDecayFactor);
                }

                return col;
            }
            ENDCG
        }
    }
}
