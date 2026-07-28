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
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            fixed4 _Color4;
            float _SplitMode;

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
                // 기본 스프라이트 텍스처
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                fixed4 tint = _Color1;

                if (_SplitMode == 2)
                {
                    // 듀오: 세로로 반반 (절반 쪼개기)
                    if (IN.texcoord.x > 0.5) tint = _Color2;
                }
                else if (_SplitMode == 4)
                {
                    // 쿼드: 십자가 4분할 (상하좌우 쪼개기)
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