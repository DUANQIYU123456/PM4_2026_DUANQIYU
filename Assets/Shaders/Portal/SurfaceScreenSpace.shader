// ============================================================================
// Ludocore/SurfaceScreenSpace — URP unlit shader that samples its texture by
// SCREEN position rather than mesh UVs. Pair with a SurfaceRenderer whose capture
// camera is parallax-locked to the player — PortalRenderer (doors) and
// MirrorRenderer (reflections). Each pixel of the surface is drawn using the
// matching screen pixel of _MainTex, so the image stays glued to the world, not
// the surface. (For monitors, which DON'T need parallax, use Ludocore/SurfaceUV.)
// ============================================================================

Shader "Ludocore/SurfaceScreenSpace"
{
    Properties
    {
        [MainTexture] _MainTex ("Surface Texture", 2D) = "black" {}
        [MainColor]   _Tint    ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "PortalUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull   Back
            ZWrite On
            ZTest  LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // ComputeScreenPos handles platform Y-flip for RT sampling.
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return col * _Tint;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
