Shader "Holobiont/PetriDishBackgroundLitBumped"
{
    Properties
    {
        // Kept so existing material references using SpriteRenderer still bind; never sampled.
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Palette)]
        _BaseColor      ("Base (cell fill)",     Color) = (0.32, 0.46, 0.42, 1)
        _ChannelColor   ("Channels (membranes)", Color) = (0.55, 0.72, 0.70, 1)
        _AccentColor    ("Accent (warm pockets)",Color) = (0.75, 0.55, 0.32, 1)
        _DeepColor      ("Deep (cell centers)",  Color) = (0.18, 0.28, 0.30, 1)
        _AccentAmount   ("Accent amount",        Range(0, 1)) = 0.08

        [Header(Voronoi Cells)]
        _CellScale      ("Cell scale (world)",   Range(0.05, 5)) = 0.6
        _CellJitter     ("Cell jitter (organic)",Range(0, 1)) = 0.85
        _CellAnimSpeed  ("Cell drift speed",     Range(0, 1)) = 0.08

        [Header(UV Warp)]
        _WarpStrength   ("Warp strength",        Range(0, 2)) = 0.55
        _WarpScale      ("Warp scale",           Range(0.05, 5)) = 0.35
        _WarpSpeed      ("Warp scroll speed",    Range(0, 0.5)) = 0.02
        _FlowTurbScale  ("Flow turbulence scale (sync)",  Range(0, 5)) = 0
        _FlowTurbAmount ("Flow turbulence amount (sync)", Range(0, 5)) = 0

        [Header(Channels)]
        _ChannelWidth   ("Channel width",        Range(0.001, 0.2)) = 0.04
        _ChannelSoftness("Channel softness",     Range(0.001, 0.2)) = 0.06
        _ChannelIntensity("Channel intensity",   Range(0, 2)) = 0.25
        _JunctionBoost  ("Junction boost",       Range(0, 3)) = 0.4

        [Header(Pulse)]
        _PulseSpeed     ("Pulse speed",          Range(0, 4)) = 0.6
        _PulseFreq      ("Pulse frequency",      Range(0, 20)) = 4.0
        _PulseStrength  ("Pulse strength",       Range(0, 1)) = 0.25

        [Header(Particulate)]
        _GrainScale     ("Grain scale",          Range(1, 200)) = 60
        _GrainIntensity ("Grain intensity",      Range(0, 0.5)) = 0.06

        [Header(Composition)]
        _GlobalIntensity("Global intensity",     Range(0, 2)) = 0.85
        _TimeScale      ("Time scale (master)",  Range(0, 2)) = 1.0

        [Header(3D Lighting)]
        _Smoothness     ("Smoothness",           Range(0, 1)) = 0.08
        _Metallic       ("Metallic",             Range(0, 1)) = 0.0
        _NormalStrength ("Normal strength",      Range(0, 30)) = 5
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
        Cull Off
        ZWrite On

        Pass
        {
            Name "PetriDishBackgroundLitBumpedForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PetriDishSurface.hlsl"

            // Per-pass uniforms (kept out of UnityPerMaterial in PetriDishSurface.hlsl so the
            // shared CBUFFER stays stable across the Lit/Unlit variants).
            float _NormalStrength;
            float _Smoothness;
            float _Metallic;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2; // xyz = tangent, w = sign for bitangent
                float2 uvWorld     : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs v = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = v.positionCS;
                OUT.positionWS  = v.positionWS;
                OUT.normalWS    = n.normalWS;
                OUT.tangentWS   = float4(n.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uvWorld     = v.positionWS.xy;
                OUT.fogFactor   = ComputeFogFactor(v.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t   = _Time.y * _TimeScale;
                half3 col = ComputePetriDishColor(IN.uvWorld, t);

                // Heightfield → tangent-space normal via screen-space gradient.
                // Channels become raised veins so 3D lights catch their edges.
                float h    = ComputePetriDishHeight(IN.uvWorld, t);
                float dhdx = ddx(h);
                float dhdy = ddy(h);
                half3 normalTS = normalize(half3(-dhdx * _NormalStrength,
                                                 -dhdy * _NormalStrength,
                                                  1.0));

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = col;
                surfaceData.alpha      = 1.0h;
                surfaceData.metallic   = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion  = 1.0h;
                surfaceData.normalTS   = normalTS;

                // Build TBN and transform tangent-space normal into world space for PBR.
                half3 normalWS    = normalize(IN.normalWS);
                half3 tangentWS   = normalize(IN.tangentWS.xyz);
                half  sign        = IN.tangentWS.w;
                half3 bitangentWS = sign * cross(normalWS, tangentWS);
                half3 nWS         = normalize(mul(normalTS, half3x3(tangentWS, bitangentWS, normalWS)));

                InputData inputData = (InputData)0;
                inputData.positionWS      = IN.positionWS;
                inputData.normalWS        = nWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord     = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord        = IN.fogFactor;
                inputData.bakedGI         = SampleSH(nWS);

                half4 lit = UniversalFragmentPBR(inputData, surfaceData);
                lit.rgb   = MixFog(lit.rgb, IN.fogFactor);
                return lit;
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes IN)
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 ShadowPassFragment(Varyings IN) : SV_TARGET { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings DepthOnlyVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthOnlyFragment(Varyings IN) : SV_TARGET { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}
