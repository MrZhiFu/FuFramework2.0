Shader "Custom/UIBlurBackground"
{
    Properties
    {
        // ── 核心参数 ──
        [HideInInspector] _MainTex ("Main Texture", 2D) = "black" {}
        _BlurBGTex ("Background Texture", 2D) = "black" {} // 截屏纹理，由 C# 在显示模糊层时注入
        _BlurSize ("Blur Scale", Float) = 1.6 // 模糊采样步长（BRP 半分辨率需 ×2）
        _MaskPower ("Mask Power", Range(0, 1)) = 0.35 // 压暗强度：0=不压暗，1=全黑
        _BlurProgress ("Blur Progress", Range(0, 1)) = 1.0 // 渐变进度：0=清晰，1=全模糊

        // ── 模板测试（与 FairyGUI 渲染管线配合）──
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        // ── 颜色掩码与混合 ──
        _ColorMask ("Color Mask", Float) = 15
        _BlendSrcFactor ("Blend SrcFactor", Float) = 5 // SrcAlpha
        _BlendDstFactor ("Blend DstFactor", Float) = 10 // OneMinusSrcAlpha
    }

    SubShader
    {
        LOD 100

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Fog
        {
            Mode Off
        }
        Blend [_BlendSrcFactor] [_BlendDstFactor], One One
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIBlurDisc"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
            };

            sampler2D _BlurBGTex;
            float4 _BlurBGTex_TexelSize;
            float _BlurSize;
            float _MaskPower;
            float _BlurProgress;

            /// 单级星型模糊：8 方向采样加权混合（正交权重 1，对角权重 2，总和 ÷12）
            half4 BlurSample8(float2 uv, float2 texelSize, float scale)
            {
                float2 o = texelSize * scale;
                half4 sum;
                sum = tex2D(_BlurBGTex, uv + float2(-o.x * 2.0, 0)) * 1.0;
                sum += tex2D(_BlurBGTex, uv + float2(-o.x, o.y)) * 2.0;
                sum += tex2D(_BlurBGTex, uv + float2(0, o.y * 2.0)) * 1.0;
                sum += tex2D(_BlurBGTex, uv + float2(o.x, o.y)) * 2.0;
                sum += tex2D(_BlurBGTex, uv + float2(o.x * 2.0, 0)) * 1.0;
                sum += tex2D(_BlurBGTex, uv + float2(o.x, -o.y)) * 2.0;
                sum += tex2D(_BlurBGTex, uv + float2(0, -o.y * 2.0)) * 1.0;
                sum += tex2D(_BlurBGTex, uv + float2(-o.x, -o.y)) * 2.0;
                sum /= 12.0;
                return sum;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = ComputeScreenPos(o.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord.xy / i.texcoord.w;
                float2 texelSize = _BlurBGTex_TexelSize.xy;
                float s = max(_BlurSize, 0.1);

                // 三级星型模糊
                half4 c0 = BlurSample8(uv, texelSize, s * 0.9);
                half4 c1 = BlurSample8(uv, texelSize, s * 2.2);
                half4 c2 = BlurSample8(uv, texelSize, s * 4.2);
                half4 blurred = c0 * 0.50;
                blurred += c1 * 0.32;
                blurred += c2 * 0.18;

                // 渐变结冰效果
                half4 original = tex2D(_BlurBGTex, uv);
                half4 result = lerp(original, blurred, _BlurProgress);
                result.rgb *= lerp(1.0, 1.0 - _MaskPower, _BlurProgress);
                result.a = 1.0;
                return result;
            }
            ENDCG
        }
    }
}