Shader "BasketballLegends2020/TextMeshOutlined"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 0)
        _OutlineWidth ("Outline Width", Float) = 0
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0)
        _ShadowOffset ("Shadow Offset", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Lighting Off
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            fixed4 _ShadowColor;
            float4 _ShadowOffset;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            float SampleOutline(float2 uv, float width)
            {
                float2 texel = _MainTex_TexelSize.xy;
                float diagonal = width * 0.70710678;
                float halfWidth = width * 0.5;
                float outline = 0.0;

                outline = max(outline, SampleAlpha(uv + texel * float2(width, 0.0)));
                outline = max(outline, SampleAlpha(uv + texel * float2(-width, 0.0)));
                outline = max(outline, SampleAlpha(uv + texel * float2(0.0, width)));
                outline = max(outline, SampleAlpha(uv + texel * float2(0.0, -width)));
                outline = max(outline, SampleAlpha(uv + texel * float2(diagonal, diagonal)));
                outline = max(outline, SampleAlpha(uv + texel * float2(diagonal, -diagonal)));
                outline = max(outline, SampleAlpha(uv + texel * float2(-diagonal, diagonal)));
                outline = max(outline, SampleAlpha(uv + texel * float2(-diagonal, -diagonal)));

                outline = max(outline, SampleAlpha(uv + texel * float2(halfWidth, 0.0)));
                outline = max(outline, SampleAlpha(uv + texel * float2(-halfWidth, 0.0)));
                outline = max(outline, SampleAlpha(uv + texel * float2(0.0, halfWidth)));
                outline = max(outline, SampleAlpha(uv + texel * float2(0.0, -halfWidth)));

                return outline;
            }

            fixed4 AlphaOver(fixed4 underColor, fixed4 overColor)
            {
                fixed outAlpha = overColor.a + underColor.a * (1.0 - overColor.a);
                fixed3 outRgb = overColor.rgb * overColor.a + underColor.rgb * underColor.a * (1.0 - overColor.a);
                return fixed4(outRgb / max(outAlpha, 0.0001), outAlpha);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fillAlpha = SampleAlpha(i.uv);
                float outlineAlpha = _OutlineWidth > 0.001 ? max(0.0, SampleOutline(i.uv, _OutlineWidth) - fillAlpha) : 0.0;

                float2 shadowUvOffset = float2(-_ShadowOffset.x, _ShadowOffset.y) * _MainTex_TexelSize.xy;
                float shadowAlpha = _ShadowColor.a > 0.001 ? SampleAlpha(i.uv + shadowUvOffset) * (1.0 - fillAlpha) : 0.0;

                fixed4 shadowColor = fixed4(_ShadowColor.rgb, saturate(_ShadowColor.a * shadowAlpha));
                fixed4 outlineColor = fixed4(_OutlineColor.rgb, saturate(_OutlineColor.a * outlineAlpha));
                fixed4 fillColor = fixed4(i.color.rgb, saturate(i.color.a * fillAlpha));

                fixed4 result = shadowColor;
                result = AlphaOver(result, outlineColor);
                result = AlphaOver(result, fillColor);
                clip(result.a - 0.001);
                return result;
            }
            ENDCG
        }
    }
}
