Shader "Custom/UIBlurSimple"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0,30)) = 0
        _Color ("Tint", Color) = (1,1,1,1)
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 🔥 Strong blur so you can SEE it clearly
                float2 offset = _MainTex_TexelSize.xy * _BlurSize * 20;

                fixed4 col = 0;

                col += tex2D(_MainTex, uv);
                col += tex2D(_MainTex, uv + offset);
                col += tex2D(_MainTex, uv - offset);
                col += tex2D(_MainTex, uv + float2(offset.x, -offset.y));
                col += tex2D(_MainTex, uv + float2(-offset.x, offset.y));

                col /= 5;

                return col * i.color;
            }
            ENDCG
        }
    }
}
