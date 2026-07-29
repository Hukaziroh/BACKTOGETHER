Shader "Custom/SplitColorSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Color1 ("Color 1", Color) = (1,1,1,1)
        _Color2 ("Color 2", Color) = (1,1,1,1)
        _Color3 ("Color 3", Color) = (1,1,1,1)
        _Color4 ("Color 4", Color) = (1,1,1,1)
        _SplitMode ("Split Mode", Float) = 1

        // 🌟 에코 전용 아웃라인 색상 프로퍼티 추가
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // 🌟 아웃라인 계산용 텍셀 크기
            
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            fixed4 _Color4;
            float _SplitMode;

            // 🌟 EchoManager에서 보낸 _OutlineEnabled 및 아웃라인 색상 수신기
            float4 _OutlineColor;
            float _OutlineEnabled;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // =========================================================
                // 1. 에코존 진입 시 (_OutlineEnabled == 1 일 때만 작동)
                // =========================================================
                if (_OutlineEnabled > 0.5)
                {
                    float2 texel = _MainTex_TexelSize.xy;
                    float alpha = tex2D(_MainTex, IN.texcoord).a;
                    
                    // 상하좌우 주변 픽셀 투명도 확인
                    float aU = tex2D(_MainTex, IN.texcoord + float2(0, texel.y)).a;
                    float aD = tex2D(_MainTex, IN.texcoord + float2(0, -texel.y)).a;
                    float aL = tex2D(_MainTex, IN.texcoord + float2(-texel.x, 0)).a;
                    float aR = tex2D(_MainTex, IN.texcoord + float2(texel.x, 0)).a;
                    
                    // 테두리 픽셀이면 아웃라인만 그리기
                    if (alpha <= 0.1 && (aU > 0.1 || aD > 0.1 || aL > 0.1 || aR > 0.1)) 
                    {
                        fixed4 outline = _OutlineColor;
                        outline.rgb *= outline.a; // 투명도 보정
                        return outline;
                    }
                    
                    // 몸통 및 안쪽 스프라이트는 완전히 버림 (속살 감추기)
                    discard;
                    return fixed4(0,0,0,0);
                }

                // =========================================================
                // 2. 평소 모드 (기존 SplitColorSprite 색상 분할 로직)
                // =========================================================
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                fixed4 tint = _Color1;

                if (_SplitMode == 2)
                {
                    // 듀오: 세로로 반반 쪼개기
                    if (IN.texcoord.x > 0.5) tint = _Color2;
                }
                else if (_SplitMode == 4)
                {
                    // 쿼드: 십자가 4분할 쪼개기
                    if (IN.texcoord.x <= 0.5 && IN.texcoord.y >= 0.5) tint = _Color1; // 좌상
                    else if (IN.texcoord.x > 0.5 && IN.texcoord.y >= 0.5) tint = _Color2; // 우상
                    else if (IN.texcoord.x <= 0.5 && IN.texcoord.y < 0.5) tint = _Color3; // 좌하
                    else tint = _Color4; // 우하
                }

                c.rgb *= tint.rgb;
                c.rgb *= c.a; // 투명도 처리

                return c;
            }
            ENDCG
        }
    }
}