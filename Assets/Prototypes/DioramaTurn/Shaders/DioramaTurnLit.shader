// DioramaTurn surface shader, written by hand for URP (no Shader Graph, no package).
//
// One shader for the room:
//  - The plinth: a tileable wood albedo and normal map projected in WORLD space from three sides
//    (triplanar), so no face repeats its neighbour.
//  - Blocks, pillar and goal pedestal: flat painted colour, no texture by design, plus the ambient
//    occlusion baked into the vertex colours by Tools/blender/DioramaTurn/make_room_models.py.
//  - No dissolve: the spec's surfaces table says pieces pop, they do not burn, so nothing here
//    fades a model away.
//
// Lit by URP's own PBR function, so the key light, its soft shadows, the fill and ambient behave
// like the Lit shader. Not supported (and not used): lightmaps, reflection probes, decals.
Shader "Prototypes/DioramaTurn/Lit"
{
    Properties
    {
        _BaseColor("Base colour", Color) = (1, 1, 1, 1)
        _BaseMap("Albedo detail map", 2D) = "white" {}
        [Normal] _NormalMap("Normal map", 2D) = "bump" {}
        _TextureStrength("Texture strength", Range(0, 1)) = 1
        _NormalStrength("Normal strength", Range(0, 2)) = 1
        _MapScale("Repeats per metre", Float) = 0.4
        [Toggle] _WorldMapping("World-space mapping (1) or object-space (0)", Float) = 1
        [Toggle] _MapRotate("Turn the top mapping 90 degrees", Float) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.45
        _AoStrength("Baked occlusion strength", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _TextureStrength;
            half _NormalStrength;
            float _MapScale;
            half _WorldMapping;
            half _MapRotate;
            half _Metallic;
            half _Smoothness;
            half _AoStrength;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- forward lit
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
                half4 color       : COLOR;   // r: baked occlusion (white when the mesh has none)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS    : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half3 normalOS    : TEXCOORD3;
                half4 fogAndVertexLight : TEXCOORD4; // x: fog, yzw: vertex lights
                half occlusion    : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vn = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;
                output.normalWS = vn.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.occlusion = input.color.r;
                half3 vertexLight = VertexLighting(vp.positionWS, vn.normalWS);
                output.fogAndVertexLight = half4(ComputeFogFactor(vp.positionCS.z), vertexLight);
                return output;
            }

            // The albedo detail (rgb) and world normal, projected from three sides.
            void Surface(Varyings input, out half3 detail, out half3 normalWS)
            {
                bool world = _WorldMapping > 0.5;
                float3 p = (world ? input.positionWS : input.positionOS) * _MapScale;
                float3 nBlend = world ? input.normalWS : input.normalOS;
                float3 w = pow(abs(nBlend), 4.0);
                w /= max(w.x + w.y + w.z, 1e-4);

                float2 uvX = p.zy;
                float2 uvY = _MapRotate > 0.5 ? p.zx : p.xz;
                float2 uvZ = p.xy;

                detail = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb * w.x
                       + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb * w.y
                       + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb * w.z;

                normalWS = normalize(input.normalWS);
                if (world && _NormalStrength > 0.001)
                {
                    half3 nW = normalWS;
                    half3 tX = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX), _NormalStrength);
                    half3 tY = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvY), _NormalStrength);
                    half3 tZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ), _NormalStrength);
                    if (_MapRotate > 0.5) tY.xy = tY.yx;
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
                Surface(input, detail, normalWS);

                half ao = lerp(1.0h, saturate(input.occlusion), _AoStrength);

                SurfaceData surface = (SurfaceData)0;
                half3 tinted = _BaseColor.rgb * lerp(half3(1, 1, 1), detail * 1.06h, _TextureStrength);
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

        // ---------------------------------------------------------------- shadows
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

        // ---------------------------------------------------------------- depth
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
