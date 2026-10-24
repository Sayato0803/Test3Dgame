// Toon/cel shading cho URP: 2 dải sáng/tối rõ ràng, điểm highlight, rim light, viền đen (inverted hull) có độ dày cố định trên màn hình.
Shader "KingHunt/Toon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _ShadeColor ("Shade Tint", Color) = (0.62, 0.55, 0.78, 1)
        _ShadeStep ("Shade Step", Range(-1, 1)) = 0.0
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.5)) = 0.03
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightSize ("Highlight Size", Range(0, 0.3)) = 0.04
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.35
        _RimColor ("Rim Color", Color) = (1, 0.97, 0.9, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 4
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35
        _OutlineColor ("Outline Color", Color) = (0.09, 0.06, 0.12, 1)
        _OutlineWidth ("Outline Width (screen)", Range(0, 0.02)) = 0.0035
        [Toggle] _OutlineSmoothNormals ("Outline: pháp tuyến mượt trong Tangent", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ShadeColor;
            float _ShadeStep;
            float _ShadeSoftness;
            float4 _HighlightColor;
            float _HighlightSize;
            float _HighlightStrength;
            float4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _AmbientStrength;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSmoothNormals;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float Band(float ndl)
            {
                return smoothstep(_ShadeStep - _ShadeSoftness, _ShadeStep + _ShadeSoftness, ndl);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ndl = dot(n, light.direction);
                float lit = Band(ndl) * smoothstep(0.35, 0.65, light.shadowAttenuation);

                half3 lightCol = light.color;
                half3 col = lerp(albedo.rgb * _ShadeColor.rgb, albedo.rgb * lightCol, lit);
                col += albedo.rgb * SampleSH(n) * _AmbientStrength;

                // Highlight cứng (blinn) chỉ ở vùng sáng.
                float3 h = normalize(light.direction + v);
                float spec = smoothstep(1 - _HighlightSize - 0.01, 1 - _HighlightSize, dot(n, h)) * lit;
                col += _HighlightColor.rgb * spec * _HighlightStrength;

                // Rim light viền sáng mép model.
                float rim = pow(1 - saturate(dot(n, v)), _RimPower);
                rim = smoothstep(0.45, 0.55, rim) * _RimStrength;
                col += _RimColor.rgb * rim * (0.4 + 0.6 * lit);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                // LIGHT_LOOP_BEGIN của URP cần biến inputData (Forward+ tra đèn theo cluster trên màn hình).
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_FORWARD_PLUS
                for (uint li = 0; li < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); li++)
                {
                    Light dl = GetAdditionalLight(li, i.positionWS, half4(1, 1, 1, 1));
                    col += albedo.rgb * dl.color * Band(dot(n, dl.direction)) * dl.distanceAttenuation * 0.6;
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                    col += albedo.rgb * al.color * Band(dot(n, al.direction)) * al.distanceAttenuation * 0.6;
                LIGHT_LOOP_END
                #endif

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // Viền: vẽ mặt sau, đẩy đỉnh theo pháp tuyến trong clip space (độ dày không đổi theo khoảng cách).
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float fogFactor : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 cs = TransformObjectToHClip(v.positionOS.xyz);
                // Model low-poly có pháp tuyến tách ở cạnh cứng -> viền bị hở. ToonStyle/builder lưu pháp tuyến
                // đã làm mượt (trung bình các đỉnh trùng vị trí) vào kênh tangent; tangent được skin cùng xương.
                float3 nOS = _OutlineSmoothNormals > 0.5 ? v.tangentOS.xyz : v.normalOS;
                float3 nWS = TransformObjectToWorldNormal(nOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = max(length(nCS), 1e-5);
                float2 offset = nCS / len * _OutlineWidth * cs.w;
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                cs.xy += offset * 2;
                o.positionCS = cs;
                o.fogFactor = ComputeFogFactor(cs.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(MixFog(_OutlineColor.rgb, i.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
