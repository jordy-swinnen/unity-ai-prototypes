// A hand-written URP full-screen pass that blurs the top and bottom bands of the screen and keeps
// the middle sharp: the tilt-shift that gives the diorama its miniature feel without a depth texture
// (URP's Depth of Field cannot be used with an orthographic camera).
// Added by RendererSetup as a FullScreenPassRendererFeature on the DioramaTurn renderer.
Shader "Prototypes/DioramaTurn/TiltShift"
{
    Properties
    {
        _BlitTexture ("Source", 2D) = "white" {}
        _BandCentre ("Sharp band centre (0..1 of the screen)", Range(0, 1)) = 0.5
        _BandHalfHeight ("Sharp band half height", Range(0, 0.5)) = 0.28
        _BlurStrength ("Blur strength", Range(0, 2)) = 1
        _BlurRadius ("Blur radius in UV", Range(0, 0.02)) = 0.004
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "TiltShiftBlur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            // Core.hlsl for XR dependencies and the full-screen vertex/fragment helpers.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);

            float _BandCentre;
            float _BandHalfHeight;
            float _BlurStrength;
            float _BlurRadius;

            // 0 inside the sharp band, rising to 1 outside it.
            float BandWeight(float2 uv)
            {
                float distance = abs(uv.y - _BandCentre) - _BandHalfHeight;
                return saturate(distance / 0.18) * _BlurStrength;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float weight = BandWeight(uv);
                if (weight <= 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                float2 r = float2(_BlurRadius, _BlurRadius * 0.6) * weight;

                half4 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2( r.x, 0));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2(-r.x, 0));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2(0,  r.y));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2(0, -r.y));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + r);
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - r);
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2( r.x, -r.y));
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + float2(-r.x,  r.y));
                return sum / 9.0;
            }
            ENDHLSL
        }
    }
}
