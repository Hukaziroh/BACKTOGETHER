Shader "Custom/CharacterEcho"
{
    Properties 
    { 
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f { 
                float4 pos : SV_POSITION; 
                float2 uv : TEXCOORD0; 
            };
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            
            // EchoManager가 보내는 전역 변수를 받기 위한 선언
            float _OutlineEnabled;

            v2f vert (appdata v) {
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float isBlack(float4 col) {
                return (col.r < 0.3 && col.g < 0.3 && col.b < 0.3) ? 1.0 : 0.0;
            }

            fixed4 frag (v2f i) : SV_Target {
                // EchoManager가 꺼져있을 때(_OutlineEnabled == 0)는 아무것도 안 그리고 날려버림
                if (_OutlineEnabled < 0.5) {
                    discard;
                    return fixed4(0,0,0,0);
                }

                float4 col = tex2D(_MainTex, i.uv);
                
                if (isBlack(col) > 0.5) {
                    return col;
                }

                float2 texel = _MainTex_TexelSize.xy;
                float edgeFound = 0;
                
                [unroll]
                for (int x = -4; x <= 4; x++) {
                    [unroll]
                    for (int y = -4; y <= 4; y++) {
                        float4 neighbor = tex2D(_MainTex, i.uv + float2(x, y) * texel * 1.5);
                        if (isBlack(neighbor) > 0.5) {
                            edgeFound = 1.0;
                            break;
                        }
                    }
                    if (edgeFound > 0.0) break;
                }

                if (edgeFound > 0.0) {
                    return _OutlineColor;
                }

                discard;
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}