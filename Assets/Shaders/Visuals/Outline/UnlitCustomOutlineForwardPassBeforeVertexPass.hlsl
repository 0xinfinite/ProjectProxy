
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif


struct VertexPositionNormalInputs {
    VertexPositionInputs position;
    VertexNormalInputs normal;
};

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 color : COLOR;
    //float3 normalOS : NORMAL;

    //#if defined(DEBUG_DISPLAY)
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    //#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float fogCoord : TEXCOORD1;
    float4 positionCS : SV_POSITION;

    //#if defined(DEBUG_DISPLAY)
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float3 viewDirWS : TEXCOORD4;
    //#endif

    float3 color : TEXCOORD5;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;

    #if defined(DEBUG_DISPLAY)
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
    inputData.viewDirectionWS = input.viewDirWS;
    #else
    inputData.positionWS = float3(0, 0, 0);
    inputData.normalWS = half3(0, 0, 1);
    inputData.viewDirectionWS = half3(0, 0, 1);
    #endif
    inputData.shadowCoord = 0;
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = 0;
    inputData.shadowMask = half4(1, 1, 1, 1);
}

VertexPositionNormalInputs GetVertexPositionNormalInputs(float3 positionOS, float3 normalOS, float4 tangentOS, float thickness, float depthForward)
{
    VertexPositionNormalInputs input = (VertexPositionNormalInputs)0;
    VertexPositionInputs posInput;
    VertexNormalInputs normInput;
    posInput.positionWS = TransformObjectToWorld(positionOS);

    normInput = GetVertexNormalInputs(normalOS, tangentOS);

    float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS); //_WorldSpaceCameraPos.xyz - input.positionWS;

    posInput.positionWS += viewDirection * (depthForward+ depthForward*0.1);

    posInput.positionWS.xyz += normInput.normalWS * thickness;

    posInput.positionVS = TransformWorldToView(posInput.positionWS);
    posInput.positionCS = TransformWorldToHClip(posInput.positionWS);

    float4 ndc = posInput.positionCS * 0.5f;
    posInput.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    posInput.positionNDC.zw = posInput.positionCS.zw;

    input.position = posInput;
    input.normal = normInput;

    return input;
}

VertexPositionNormalInputs GetVertexPositionNormalInputs(float3 positionOS, float3 normalOS, float4 tangentOS, float thickness) {
    return GetVertexPositionNormalInputs(positionOS, normalOS, tangentOS, thickness,0);
}