Shader "Custom/SpikeOutlineEcho"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} _Color ("Color", Color) = (1, 1, 1, 1) _OutlineWidth ("Width", Float) = 1.0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 worldPos : TEXCOORD1; };
            sampler2D _MainTex; float4 _MainTex_TexelSize; fixed4 _Color; float _OutlineWidth;
            // 통합 변수들 (위와 동일하게)
            float4 _WavePos; float _WaveRadius; float _WaveWidth;
            v2f vert (appdata_full v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                float2 off = _MainTex_TexelSize.xy * _OutlineWidth;
                float4 col = tex2D(_MainTex, i.uv);
                float4 up = tex2D(_MainTex, i.uv + float2(0, off.y));
                float4 down = tex2D(_MainTex, i.uv + float2(0, -off.y));
                float4 left = tex2D(_MainTex, i.uv + float2(-off.x, 0));
                float4 right = tex2D(_MainTex, i.uv + float2(off.x, 0));
                float isEdge = step(0.1, (up.a + down.a + left.a + right.a)) * (1.0 - step(0.1, col.a));
                float dist = distance(i.worldPos.xy, _WavePos.xy);
                float waveAlpha = 1.0 - smoothstep(_WaveRadius - _WaveWidth, _WaveRadius, dist);
                if (dist > _WaveRadius || dist < _WaveRadius - _WaveWidth * 2.0) waveAlpha = 0;
                return fixed4(_Color.rgb, isEdge * waveAlpha);
            }
            ENDCG
        }
    }
}