
#ifndef URP_UNLIT_FORWARD_PASS_INCLUDED
#define URP_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

float remap(float val, float in1, float in2, float out1, float out2)  //리맵하는 함수
    {
    return out1 + (val - in1) * (out2 - out1) / (in2 - in1);
    }

#include "../ShaderLibrary/Raymarching.hlsl"
#include "../ShaderLibrary/Projection.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;

    #if defined(DEBUG_DISPLAY)
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float fogCoord : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    float4 color : COLOR0;
    float4 screenPos : TEXCOORD5;
    float3 positionWS : TEXCOORD6;
    float4 projUV : TEXCOORD7;
    //float4 positionOS : TEXCOORD8;

    #if defined(DEBUG_DISPLAY)
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float3 viewDirWS : TEXCOORD4;
    #endif

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
    inputData.positionWS = input.positionWS;//float3(0, 0, 0);
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


// float RayMarch(float3 ro, float3 rd)
// {
//     // RO에서 현재까지 전진한 누적 거리 저장
//     float dO = 0;
//     
//     for(int i = 0; i < 128; i++)
//     {
//         float3 p = ro + rd * dO;
//         
//         // GetDist() : 지정한 위치로부터 최단 SDF 거리값 계산
//         float dS = 0.1;//GetDist(p); // 이번 스텝에 전진할 거리
//         dO += dS;              // 레이 전진
//         
//         // 레이 제한 거리까지 도달하거나
//         // 레이가 물체의 정점 또는 땅에 닿은 경우 레이 마칭 종료
//         if(dO > 1000 )
//             break;
//     }
//     
//     return dO;
// }

TEXTURE2D(_RadarDepthTex);
SAMPLER(sampler_RadarDepthTex);
float _DepthBias;

 //float4x4 _MatrixHClipToWorld;
float4x4 _RadarCameraMatrixVP;
//float4x4 _RadarProjector;

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.positionCS = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.screenPos = ComputeScreenPos(output.positionCS);
    output.positionWS = vertexInput.positionWS;
    //output.positionOS = input.positionOS;
    output.projUV = ProjectUV(_RadarCameraMatrixVP, input.positionOS);
    #if defined(_FOG_FRAGMENT)
    output.fogCoord = vertexInput.positionVS.z;
    #else
    output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    #if defined(DEBUG_DISPLAY)
    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

    // already normalized from normal transform to WS.
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
    #endif

    return output;
}


bool isInsideOfProjector(float4 projUV, Texture2D depthTex, sampler depthTexSampler)
{
    float2 projUV2 = ProjectionUVToTex2DUV(projUV);

    if(ClipBackProjection(projUV)||ClipUVBoarder(projUV2))
    {
        return false;
    }
    else
    {
        float dfp = //Linear01Depth(
            DepthFromProjection(projUV)
           // , _ZBufferParams)
        ;
        float dfd = DepthFromDepthmap(depthTex, depthTexSampler, projUV2,1);
        return !ClipProjectionShadow(dfp, dfd, -0.001);
        //
    }
}

bool isWorldPosInsideOfProjector(float3 worldPos, float4x4 _Projector, Texture2D depthTex, sampler depthTexSampler)
{
    float4 projUV = ProjectUVFromWorldPos(_Projector, worldPos);
    float2 projUV2 = ProjectionUVToTex2DUV(projUV);
    
     projUV2.y = 1-projUV2.y; 

    float dfp = DepthFromProjection(projUV);
    float dfd = DepthFromDepthmap(depthTex, depthTexSampler, projUV2);
    if(ClipBackProjection(projUV)||ClipUVBoarder(projUV2) ||
        ClipProjectionShadow(dfp, dfd, _DepthBias)
        )
    {
        return false;
    }
    else
    {
        return true;
    }
}

bool isBlocked(float d0, float depth01)
{    
    return d0 > depth01;// ; //
    // || 
}


void UnlitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    //half2 uv = input.uv;
    //float2 screenUVs = ProjectionUVToTex2DUV(input.screenPos);
    //Ray ray = CreateCameraRay(screenUVs);

     //outColor = float4(ray.direction.x, ray.direction.y,ray.direction.z,1);
      //outColor.r= 0;//remap(outColor.r,-1,1,0,1);
     // outColor.g= 0;//remap(outColor.g,-1,1,0,1);
   // outColor.b= 0;//remap(outColor.b,-1,1,0,1);
    //
     //return;
    
    //float zRaw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenUVs);
    //float z01 = Linear01Depth(zRaw, _ZBufferParams);
    // float zEye = LinearEyeDepth(z01, _ZBufferParams);
    //outColor = float4( z01,z01,z01,1);
    //return;

    //outColor = 1;
    //return;
    
    //float3 tempWorldPos =  TransformUVToWorldPos(screenUVs, zRaw);
    //float2 projUV2 = ProjectionUVToTex2DUV(input.projUV);
    //float dfd = DepthFromDepthmap(_RadarDepthTex, sampler_RadarDepthTex, projUV2)* _DepthBias;
    
     outColor = 1; //dfd;
    //DepthFromProjection(input.projUV);
    //outColor.a = 1;//1; //isWorldPosInsideOfProjector//(tempWorldPos)//
     //(input.positionWS, _RadarProjector, _DepthTex, sampler_DepthTex)//
    if(!isInsideOfProjector(input.projUV, _RadarDepthTex, sampler_RadarDepthTex))
clip(-1);
        return;
     //? 1: float4(0,0,0,0);
    //

    
     return;
    
    //outColor = float4(projUV.x, projUV.y, 0,1);
    //return;
    
    //float2 projUV2 = ProjectionUVToTex2DUV(input.projUV1);
    //float2 screenUVs1 = screenPos.xy / screenPos.w;
	//						float2 uv = ProjectionUVToTex2DUV(input.projUV);
    //float depthFromPos = DepthFromProjection(input.projUV);
     //float z1_raw = SAMPLE_DEPTH_TEXTURE(_DepthTex1, sampler_DepthTex1, projUV2);
     //float z1_01 = Linear01Depth(z1_raw, _ZBufferParams);
     //float z1_eye = LinearEyeDepth(z1_01, _ZBufferParams);
     //outColor = float4(z1_eye,z1_eye,z1_eye,1);
    //return;

     //float2 screenUVs = input.screenPos.xy / input.screenPos.w;
     
     //float z1_01 = //Linear01Depth(z1_raw, _ZBufferParams);
    // float z1_eye = LinearEyeDepth(z1_01, _ZBufferParams);
    // if(IsInsideOfProjectionDepthCamera1(input.positionWS))
    // {
    //     outColor = 1;
    // }
    // else
    // {
    //     outColor = float4(0,0,0,1);
    // }
    //
    // return;
   // outColor = float4(0,0,0,saturate(remap(_SightMode,0.5,1,0,1)));
    
//     return;
// //    outColor = float4( remapped,remapped,remapped,1);
//     
//     half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
//     half3 color = texColor.rgb * _BaseColor.rgb;
//     half alpha = texColor.a * _BaseColor.a;
//
//     alpha = AlphaDiscard(alpha, _Cutoff);
//     color = AlphaModulate(color, alpha);
//
// #ifdef LOD_FADE_CROSSFADE
//     LODFadeCrossFade(input.positionCS);
// #endif
//
//     InputData inputData;
//     InitializeInputData(input, inputData);
//     SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);
//
// #ifdef _DBUFFER
//     ApplyDecalToBaseColor(input.positionCS, color);
// #endif
//
//     half4 finalColor = UniversalFragmentUnlit(inputData, color, alpha);
//
// #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
//     float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
//     AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
//     finalColor.rgb *= aoFactor.directAmbientOcclusion;
// #endif
//
// #if defined(_FOG_FRAGMENT)
// #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
//     float viewZ = -input.fogCoord;
//     float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
//     half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
// #else
//     half fogFactor = 0;
// #endif
// #else
//     half fogFactor = input.fogCoord;
// #endif
//     finalColor.rgb = MixFog(finalColor.rgb, fogFactor);
//     finalColor.a = OutputAlpha(finalColor.a, IsSurfaceTypeTransparent(_Surface));
//
//     outColor = finalColor;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

#endif
