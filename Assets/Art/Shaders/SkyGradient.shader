// Skybox gradient kiểu cartoon: trời xanh trên đỉnh, chân trời sáng ấm, mặt trời tròn mềm.
Shader "KingHunt/SkyGradient"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.32, 0.62, 0.98, 1)
        _HorizonColor ("Horizon", Color) = (0.86, 0.94, 1, 1)
        _BottomColor ("Bottom", Color) = (0.55, 0.78, 0.95, 1)
        _Exponent ("Gradient Exponent", Range(0.1, 4)) = 0.7
        _SunColor ("Sun", Color) = (1, 0.96, 0.8, 1)
        _SunDir ("Sun Direction", Vector) = (-0.4, 0.45, 0.8, 0)
        _SunSize ("Sun Size", Range(0.9, 1)) = 0.996
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _HorizonColor;
                float4 _BottomColor;
                float _Exponent;
                float4 _SunColor;
                float4 _SunDir;
                float _SunSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;
                half3 col = up >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(up), _Exponent))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-up), 0.5));
                float s = dot(d, normalize(_SunDir.xyz));
                col += _SunColor.rgb * smoothstep(_SunSize, _SunSize + 0.002, s);          // đĩa mặt trời
                col += _SunColor.rgb * pow(saturate(s), 64) * 0.25;                          // quầng sáng
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
