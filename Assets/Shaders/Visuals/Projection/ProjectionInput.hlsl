#ifndef UNIVERSAL_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<float3x2> _Stretched;
#endif

VertexPositionInputs GetVertexPositionInputsOverrided(float3 positionOS, float4 color)
{
    VertexPositionInputs input;
    input.positionWS = positionOS;//TransformObjectToWorld(positionOS);
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    input.positionWS += lerp(float3(0,0,0), float3(_Stretched.m00, _Stretched.m01,_Stretched.m02), color.b);
    input.positionWS += lerp(float3(0,0,0), float3(_Stretched.m10,_Stretched.m11,_Stretched.m12), color.a);
    #endif
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _Surface;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
#define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
#endif

#endif
