// PixelInvaders URP Lit shader for the 3D command trench (deck and side rails).
// Maps procedural brushed-metal albedo and normal maps in world space (triplanar) and multiplies
// baked vertex-colour ambient occlusion (AO) from Tools/blender/PixelInvaders/make_trench.py.
// Calls UniversalFragmentPBR so directional shadows and dynamic projectile point lights work natively.
Shader "Prototypes/PixelInvaders/Lit"
{
    Properties
    {
        _BaseColor("Base colour", Color) = (0.12, 0.16, 0.22, 1)
        _DetailAlbedo("Detail albedo map", 2D) = "white" {}
        [Normal] _DetailNormal("Detail normal map", 2D) = "bump" {}
        _TextureStrength("Texture strength", Range(0, 1)) = 0.85
        _NormalStrength("Normal strength", Range(0, 2)) = 0.55
        _MapScale("Repeats per metre", Float) = 0.35
        _Metallic("Metallic", Range(0, 1)) = 0.35
        _Smoothness("Smoothness", Range(0, 1)) = 0.38
        _OcclusionStrength("Baked occlusion strength", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_DetailAlbedo); SAMPLER(sampler_DetailAlbedo);
        TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _TextureStrength;
            half _NormalStrength;
            float _MapScale;
            half _Metallic;
            half _Smoothness;
            half _OcclusionStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                half4 fogAndVertexLight : TEXCOORD2;
                half occlusion    : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;
                output.normalWS = vn.normalWS;
                output.occlusion = input.color.r;
                half3 vertexLight = VertexLighting(vp.positionWS, vn.normalWS);
                output.fogAndVertexLight = half4(ComputeFogFactor(vp.positionCS.z), vertexLight);
                return output;
            }

            void SampleTriplanar(Varyings input, out half3 detail, out half3 normalWS)
            {
                float3 p = input.positionWS * _MapScale;
                float3 w = pow(abs(input.normalWS), 4.0);
                w /= max(w.x + w.y + w.z, 1e-4);

                float2 uvX = p.zy;
                float2 uvY = p.xz;
                float2 uvZ = p.xy;

                detail = SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, uvX).rgb * w.x
                       + SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, uvY).rgb * w.y
                       + SAMPLE_TEXTURE2D(_DetailAlbedo, sampler_DetailAlbedo, uvZ).rgb * w.z;

                normalWS = normalize(input.normalWS);
                if (_NormalStrength > 0.001)
                {
                    half3 nW = normalWS;
                    half3 tX = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, uvX), _NormalStrength);
                    half3 tY = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, uvY), _NormalStrength);
                    half3 tZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormal, sampler_DetailNormal, uvZ), _NormalStrength);
                    tX = half3(tX.xy + nW.zy, abs(tX.z) * nW.x);
                    tY = half3(tY.xy + nW.xz, abs(tY.z) * nW.y);
                    tZ = half3(tZ.xy + nW.xy, abs(tZ.z) * nW.z);
                    normalWS = normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);
                }
            }

            void frag(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                half3 detail, normalWS;
                SampleTriplanar(input, detail, normalWS);

                half ao = lerp(1.0h, saturate(input.occlusion), _OcclusionStrength);

                SurfaceData surface = (SurfaceData)0;
                half3 tinted = _BaseColor.rgb * lerp(half3(1, 1, 1), detail * 1.12h, _TextureStrength);
                surface.albedo = tinted * lerp(1.0h, ao, 0.75h);
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion = ao;
                surface.emission = half3(0, 0, 0);
                surface.alpha = 1.0h;
                surface.normalTS = half3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogAndVertexLight.x);
                inputData.vertexLighting = input.fogAndVertexLight.yzw;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1.0h;
                outColor = color;
                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer(GetMeshRenderingLayer());
                #endif
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
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes { float4 positionOS : POSITION; };
            struct DepthVaryings { float4 positionCS : SV_POSITION; };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
