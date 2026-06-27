// ============================================================================
// Ludocore/SurfaceUV — URP unlit shader that samples its texture by MESH UV.
// Pair with MonitorRenderer: a fixed camera renders a feed into _MainTex, and this
// shader paints it onto the surface like an ordinary texture (a TV/CCTV screen) —
// the picture stays mapped to the geometry, same from any viewing angle.
//
// Contrast with Ludocore/PortalScreenSpace, which samples by SCREEN position to
// make the image stick to the world (portals & mirrors).
// ============================================================================

Shader "Ludocore/SurfaceUV"
{
    Properties
    {
        [MainTexture] _MainTex ("Feed", 2D) = "black" {}
        [MainColor]   _Tint    ("Tint", Color) = (1, 1, 1, 1)
        [Toggle] _FlipY ("Flip Vertically", Float) = 0
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
            Name "SurfaceUVUnlit"
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float  _FlipY;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                float2 uv = TRANSFORM_TEX(IN.uv, _MainTex);
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                OUT.uv = uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return col * _Tint;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
