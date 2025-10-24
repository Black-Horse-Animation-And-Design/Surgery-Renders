Shader "Custom/DrillChipHDRP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _DrillPosition("Drill Position (World)", Vector) = (0,0,0,0)
        _DrillRadius("Drill Radius", Float) = 0.2
        _DisplaceAmount("Displacement Amount", Float) = 0.02
        _NoiseScale("Noise Scale", Float) = 25
        _DarkenStrength("Darken Strength", Range(0,1)) = 0.5
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassVertex.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassFragment.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Noise.hlsl"

    struct Attributes
    {
        float3 positionOS : POSITION;
        float3 normalOS : NORMAL;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
    };

    float3 _DrillPosition;
    float _DrillRadius;
    float _DisplaceAmount;
    float _NoiseScale;
    float _DarkenStrength;
    float4 _BaseColor;

    Varyings Vert(Attributes IN)
    {
        Varyings OUT;
        float3 positionWS = TransformObjectToWorld(IN.positionOS);
        float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));

        // Distance from drill point
        float dist = distance(positionWS, _DrillPosition);
        float influence = saturate(1.0 - (dist / _DrillRadius));

        // Noise-based chipping
        float n = snoise(positionWS * _NoiseScale) * 0.5 + 0.5;

        // Push vertices inward near the drill
        positionWS -= normalWS * _DisplaceAmount * influence * n;

        OUT.positionWS = positionWS;
        OUT.normalWS = normalWS;
        OUT.positionCS = TransformWorldToHClip(positionWS);
        return OUT;
    }

    float4 Frag(Varyings IN) : SV_Target
    {
        // Darken near drill point
        float dist = distance(IN.positionWS, _DrillPosition);
        float influence = saturate(1.0 - (dist / _DrillRadius));
        float3 color = _BaseColor.rgb * lerp(1.0, (1.0 - _DarkenStrength), influence);
        return float4(color, 1);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Forward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    FallBack Off
}
