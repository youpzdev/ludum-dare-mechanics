Shader "Neon/Sprite Lit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Toggle] _UseSpriteProperties ("Use Sprite Tint And Flip", Float) = 1
        [Enum(Texture, 0, Rectangle, 1, Circle, 2)] _Shape ("Shape", Float) = 0
        _RimWidth ("Rim Width In UV", Range(0, 0.5)) = 0.06
        _BaseIntensity ("Lit Base Intensity", Range(0, 2)) = 1
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1
        _BaseReadability ("Unlit Base Readability", Range(0, 1)) = 0.15
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _CoreEmission ("Core Emission", Range(0, 20)) = 0.1
        _RimEmission ("Rim Emission", Range(0, 20)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "CanUseSpriteAtlas" = "False"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fog

            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _EmissionColor;
                float _UseSpriteProperties;
                float _Shape;
                float _RimWidth;
                half _BaseIntensity;
                half _AmbientStrength;
                half _BaseReadability;
                half _CoreEmission;
                half _RimEmission;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half4 color : COLOR;
                half3 vertexLighting : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half4 tint = input.color * _Color;
                if (_UseSpriteProperties > 0.5)
                {
                    SetUpSpriteInstanceProperties();
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                    tint *= unity_SpriteColor;
                }

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.color = tint;
                output.vertexLighting = VertexLighting(positions.positionWS, half3(0, 0, -1));
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(input.uv, _MainTex)) * input.color;
                half coverage = 1;
                half rim = 0;
                if (_Shape > 0.5)
                {
                    float2 edge = min(input.uv, 1 - input.uv);
                    float distanceToEdge = _Shape > 1.5
                        ? 0.5 - length(input.uv - 0.5)
                        : min(edge.x, edge.y);
                    float antialias = max(fwidth(distanceToEdge), 0.0001);
                    coverage = smoothstep(-antialias, antialias, distanceToEdge);
                    rim = 1 - smoothstep(max(0, _RimWidth - antialias), _RimWidth + antialias, distanceToEdge);
                }

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = half3(0, 0, -1);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.vertexLighting = input.vertexLighting;
                lighting.bakedGI = SampleSH(lighting.normalWS) * _AmbientStrength;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = color.rgb * _BaseIntensity;
                surface.alpha = color.a * coverage;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.emission = color.rgb * (_EmissionColor.rgb * (_CoreEmission + rim * _RimEmission)
                    + _BaseReadability * _BaseIntensity);

                half4 result = UniversalFragmentBlinnPhong(lighting, surface);
                result.rgb = MixFog(result.rgb, input.fogFactor);
                return result;
            }
            ENDHLSL
        }
    }
}
