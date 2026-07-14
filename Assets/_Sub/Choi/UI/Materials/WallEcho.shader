Shader "Custom/WallEcho"
{
    Properties { _Color ("Color", Color) = (0, 1, 1, 1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };
            float4 _Color;
            // 통합 변수들
            float4 _WavePos; float _WaveRadius; float _WaveWidth;
            v2f vert (appdata_base v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                float dist = distance(i.worldPos.xy, _WavePos.xy);
                float alpha = 1.0 - smoothstep(_WaveRadius - _WaveWidth, _WaveRadius, dist);
                if (dist > _WaveRadius || dist < _WaveRadius - _WaveWidth * 2.0) alpha = 0;
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}