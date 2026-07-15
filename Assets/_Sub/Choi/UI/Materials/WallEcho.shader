Shader "Custom/WallEcho"
{
    Properties 
    { 
        _Color ("Color", Color) = (0, 1, 1, 1) 
    }
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
            float4 _WavePos; 
            float _WaveWidth;
            float _WaveRadius1, _WaveRadius2, _WaveRadius3;
            float _WaveAlpha1, _WaveAlpha2, _WaveAlpha3;
            float _OutlineEnabled;

            v2f vert (appdata_base v) {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 트리거 구역 밖이면 아예 그리지 않음 (기존과 동일)
                if (_OutlineEnabled < 0.5) return fixed4(0,0,0,0);

                float dist = distance(i.worldPos.xy, _WavePos.xy);
                float finalAlpha = 0.0;

                // 3개의 파동 각각에 대해 계산
                // 현재 거리와 파동 반지름 사이의 차이를 구해서 범위 안에 있는지 체크
                // 파동이 0보다 커야(발사 중이어야)만 계산합니다.
                
                if (_WaveRadius1 > 0 && dist <= _WaveRadius1 && dist >= (_WaveRadius1 - _WaveWidth))
                    finalAlpha = max(finalAlpha, (1.0 - smoothstep(_WaveRadius1 - _WaveWidth, _WaveRadius1, dist)) * _WaveAlpha1);
                
                if (_WaveRadius2 > 0 && dist <= _WaveRadius2 && dist >= (_WaveRadius2 - _WaveWidth))
                    finalAlpha = max(finalAlpha, (1.0 - smoothstep(_WaveRadius2 - _WaveWidth, _WaveRadius2, dist)) * _WaveAlpha2);

                if (_WaveRadius3 > 0 && dist <= _WaveRadius3 && dist >= (_WaveRadius3 - _WaveWidth))
                    finalAlpha = max(finalAlpha, (1.0 - smoothstep(_WaveRadius3 - _WaveWidth, _WaveRadius3, dist)) * _WaveAlpha3);

                // 파동이 하나도 닿지 않았다면 최종 알파는 0이 되어 투명해짐
                if (finalAlpha <= 0.0) return fixed4(0, 0, 0, 0);

                return fixed4(_Color.rgb, _Color.a * finalAlpha);
            }
            ENDCG
        }
    }
}