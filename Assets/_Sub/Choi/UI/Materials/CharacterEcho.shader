Shader "Custom/CharacterEcho"
{
    Properties 
    { 
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM                        
            #pragma vertex vert                        
            #pragma fragment frag                        
            #include "UnityCG.cginc"

            struct v2f { 
                float4 pos : SV_POSITION; 
                float2 uv : TEXCOORD0; 
            };
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;

            // EchoManager에서 받아올 윤곽선 제어 변수
            float _OutlineEnabled;

            v2f vert (appdata_full v) {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // [핵심] _OutlineEnabled가 0(꺼짐)이면 윤곽선 로직을 타지 않고 바로 종료
                if (_OutlineEnabled < 0.5) discard;

                float2 texel = _MainTex_TexelSize.xy;
                
                // 현재 픽셀이 본체인지 확인
                float alpha = tex2D(_MainTex, i.uv).a;
                
                // 이웃 픽셀들 확인
                float aU = tex2D(_MainTex, i.uv + float2(0, texel.y)).a;
                float aD = tex2D(_MainTex, i.uv + float2(0, -texel.y)).a;
                float aL = tex2D(_MainTex, i.uv + float2(-texel.x, 0)).a;
                float aR = tex2D(_MainTex, i.uv + float2(texel.x, 0)).a;
                
                // 윤곽선 그리기 로직 (상시 출력)
                if (alpha <= 0.1 && (aU > 0.1 || aD > 0.1 || aL > 0.1 || aR > 0.1)) {
                    return _OutlineColor;
                }
                
                // 본체 내부나 배경은 그리지 않음
                discard;
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}