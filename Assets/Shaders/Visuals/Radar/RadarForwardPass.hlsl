
#ifndef URP_UNLIT_FORWARD_PASS_INCLUDED
#define URP_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
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
    float4 positionOS : TEXCOORD8;
    float4 positionHCS : TEXCOORD9;
    float4 positionCSFromRadar : TEXCOORD10;
    float3 viewDir : TEXCOORD11;
    //float3 viewRayWorldOnRadar : TEXCOORD10;

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


TEXTURE2D(_RadarDepthTex);
SAMPLER(sampler_RadarDepthTex);
float _Temp;

 //float4x4 _MatrixHClipToWorld;
 float4x4 _RadarCameraMatrixVP;
 float4x4 _RadarCameraMatrix_I_VP;
float4x4 _RadarCameraMatrix_I_P;
float4x4 _RadarCameraMatrix_I_V;
float3 _RadarCameraPos;
float4x4 _RadarCameraToWorld;
float4x4 _RadarProjector;
float4x4 _RadarMatrixHClipToWorld;
float4x4 _RadarCameraInverseProjection;
float4x4 _RadarCameraWorldToHClipMatrix;
float _DepthBias;
float _VisibleRange;
float _SightMode;

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
    output.projUV = ProjectUV(_RadarCameraMatrixVP, input.positionOS);//
        //ProjectUVFromWorldPos(_RadarProjector,  output.positionWS);
    output.positionOS = input.positionOS;
    output.positionHCS = TransformObjectToHClip(input.positionOS);
    output.positionCSFromRadar = mul(_RadarCameraMatrixVP/*_RadarCameraWorldToHClipMatrix*/ , float4(vertexInput.positionWS,1.0));
    output.viewDir = normalize( GetWorldSpaceViewDir(vertexInput.positionWS)*-1);
// float sceneRawDepth = 1;
//     #if UNITY_REVERSED_Z
//     sceneRawDepth = 1- sceneRawDepth;
//     #endif
//     
    //output.viewRayWorldOnRadar =  
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


bool isInsideOfProjector(float4 projUV, Texture2D _DepthTex, sampler sampler_DepthTex)
{
    float2 projUV2 = ProjectionUVToTex2DUV(projUV);

    float dfp = DepthFromProjection(projUV);
    float dfd = DepthFromDepthmap(_DepthTex, sampler_DepthTex, projUV2, 1);
    if(ClipBackProjection(projUV)||ClipUVBoarder(projUV2) || ClipProjectionShadow(dfp, dfd, 0))
    {
        return false;
    }
    else
    {
        return true;
    }
}

bool isWorldPosInsideOfProjector(float3 worldPos, float4x4 projector, Texture2D depthTex, sampler samplerDepthTex)
{
    float4 projUV = ProjectUVFromWorldPos(projector, worldPos);
    float2 projUV2 = ProjectionUVToTex2DUV(projUV);
    
     //projUV2.y = 1-projUV2.y; 

    float dfp = DepthFromProjection(projUV);
    float dfd = DepthFromDepthmap(depthTex, samplerDepthTex, projUV2, 2);
    if(ClipBackProjection(projUV)||ClipUVBoarder(projUV2) //||
        //!ClipProjectionShadow(dfp, dfd, 0)
        )
    {
        //return false;
        return ClipProjectionShadow(dfp, dfd, 0);
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

inline half3 TransformUVToWorldPos(half2 uv, Texture2D depthTex, sampler samplerDepthTex, float4x4 matrixHClipToWorld)
{
    half depth = SAMPLE_DEPTH_TEXTURE(depthTex, samplerDepthTex, uv);//tex2D(_CameraDepthTexture, uv).r;
    #ifndef SHADER_API_GLCORE
    half4 positionCS = half4(uv * 2 - 1, depth, 1) * LinearEyeDepth(depth, _ZBufferParams);
    #else
    half4 positionCS = half4(uv * 2 - 1, depth * 2 - 1, 1) * LinearEyeDepth(depth, _ZBufferParams);
    #endif
    return mul(matrixHClipToWorld, positionCS).xyz;
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

    half2 uv = input.positionHCS.xy / _ScaledScreenParams.xy;//input.uv;

    #if UNITY_REVERSED_Z
    real depth = SampleSceneDepth(uv);
    #else
    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
    #endif

    outColor = float4(depth,depth,depth,1);
    //return;
    //float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

    
     float2 screenUVs = ProjectionUVToTex2DUV(input.screenPos);
     float zRaw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenUVs);
     float z01 = Linear01Depth(zRaw, _ZBufferParams);
    //  //float zEye = LinearEyeDepth(z01, _ZBufferParams);
    // outColor = float4( z01,z01,z01,1);
    // float3 worldPos = ComputeWorldSpacePosition(screenUVs, zRaw, UNITY_MATRIX_I_VP);
    //
    // outColor = float4(worldPos.x,worldPos.y,worldPos.z,1);
    //return;

    //bool b = isWorldPosInsideOfProjector(worldPos, _RadarProjector, _RadarDepthTex, sampler_RadarDepthTex);
    
    float4 radarProj = ProjectUV( _RadarCameraMatrixVP, input.positionOS);
    float2 radarProjScreenUV = ProjectionUVToTex2DUV(radarProj);
    //radarProjScreenUV = float2(radarProjScreenUV.x * 2 - 1, radarProjScreenUV.y * 2 - 1);
    #ifdef UNITY_UV_STARTS_AT_TOP
    //radarProjScreenUV.y = 1-radarProjScreenUV.y;
    #endif
    float radarZRaw = SAMPLE_DEPTH_TEXTURE(_RadarDepthTex, sampler_RadarDepthTex, radarProjScreenUV);
    ////radarZRaw = Linear01Depth(radarZRaw, _ZBufferParams);
    //float4 radarNDC = float4(radarProjScreenUV.x * 2 - 1, radarProjScreenUV.y * 2 - 1, radarZRaw, 1);
    // #if UNITY_UV_STARTS_AT_TOP
    //     radarNDC.y *= -1;
    // #endif
    //
     float r01 =  Linear01Depth( radarZRaw, _ZBufferParams);//, _ZBufferParams;// / _ProjectionParams.z;
    // float reye = LinearEyeDepth(r01, _ZBufferParams);
    outColor = float4(z01,z01,z01, 1);
    //return;

    float a = isInsideOfProjector(radarProj, _RadarDepthTex, sampler_RadarDepthTex)//isWorldPosInsideOfProjector(input.positionWS, _RadarCameraMatrixVP, _RadarDepthTex, sampler_RadarDepthTex)
    ? 1 : 0;
    
    outColor.rgb *= a;
    //return;

    
    
     Ray ray = CreateCameraRay(screenUVs);
   //  
   //  //Ray rayFromRadar = CreateRay(_RadarCameraPos, normalize(input.positionWS-_RadarCameraPos)); //CreateCameraRay(radarProjScreenUV, _RadarCameraPos,
   //  //_RadarCameraInverseProjection , _RadarCameraToWorld);
   //  
   //  //float3 depthXview = input.viewDir * radarZRaw;
   //  float4 fragPos = float4(input.screenPos.xy * 2.0f - 1.0f, radarZRaw, 1.0f);
   //              
   //  float4 depthCameraPos = mul(_RadarCameraMatrix_I_V, mul(_RadarCameraMatrix_I_P, fragPos));
   //  
   //  
   //  float3 worldPosFromRadarDepth =// mul(_RadarCameraMatrix_I_VP, radarNDC);//TransformUVToWorldPos(radarProjScreenUV,
   //      //_RadarDepthTex, sampler_RadarDepthTex, _RadarMatrixHClipToWorld);//
   //  //     ComputeWorldSpacePosition(radarProjScreenUV//radarNDC.xy
   //  //         , radarZRaw, _RadarCameraMatrix_I_VP );
   //      //depthXview;
   //      mul(unity_ObjectToWorld, depthCameraPos);
   //  //ComputeWorldSpacePosition(input.positionCSFromRadar,_RadarCameraMatrix_I_VP );
   //  //ComputeWorldSpacePosition(radarProjScreenUV, radarZRaw, _RadarCameraMatrix_I_VP);//TransformUVToWorldPos(screenUVs, zRaw);
   //  //(ray.origin) +
   //      //ray.direction*radarZRaw ;// * radarZRaw;
   //  //worldPosFromRadarDepth = mul(_RadarCameraMatrix_I_P,
   //      //mul(_RadarCameraMatrix_I_V,
   //  //         worldPosFromRadarDepth)
   //      //)
   // //;
   //  float4 clipPosFromRadarDepth = TransformWorldToHClip(worldPosFromRadarDepth);
   //
   //  float transparency = saturate((worldPosFromRadarDepth.z - _VisibleRange) / (1 - _VisibleRange));
   //
   //  
   //  outColor.rgb = transparency;//clipPosFromRadarDepth.xyz;
   //  outColor.a = 1;
   //  return;
    //worldPosFromRadarDepth = mul(_RadarCameraMatrix_I_V ,mul(_RadarCameraMatrix_I_VP, worldPosFromRadarDepth));

    //worldPosFromRadarDepth = normalize(ray.origin-worldPosFromRadarDepth) * length(ray.origin-worldPosFromRadarDepth) * -1;  
    //worldPosFromRadarDepth += rayFromRadar.origin*_DepthBias;
    // outColor =// length(worldPosFromRadarDepth - _WorldSpaceCameraPos);///_ProjectionParams.z;//
    //     float4(worldPosFromRadarDepth.x,worldPosFromRadarDepth.y,worldPosFromRadarDepth.z,1);
    // outColor.a = 1;
    //outColor.xyz *= 0.1;
    
    // float finalDepth = length(worldPosFromRadarDepth - _WorldSpaceCameraPos);
    // outColor = finalDepth/ _ProjectionParams.z;
    // outColor.a = 1;
    //return;
    
    //return;
   // float4 worldPosToHCSFromRadarDepth = TransformWorldToHClip(worldPosFromRadarDepth);

    
    
    
    
    //float3 worldPosToHCSToWorldPosFromRadarDepth = TransformHClip//ComputeWorldSpacePosition(screenUVs, )
    
    //worldPosFromRadarDepth /= worldPosFromRadarDepth.w;
    //outColor = float4(worldPosFromRadarDepth.x,worldPosFromRadarDepth.y,worldPosFromRadarDepth.z,1);
    //return;
    
    // if(ClipUVBoarder(radarProjScreenUV) || ClipBackProjection(radarProj))
    // {
    //     //clip(-0.1);
    // }
    
  
 

   // outColor = float4(rayFromRadar.direction.x,rayFromRadar.direction.y,rayFromRadar.direction.z,1);
//return;
    
    // if(length(worldPos-ray.origin)>length(worldPosFromRadarDepth-rayFromRadar.origin))
    // {
    //     outColor = 1;
    //     //return;
    // }
    // float v = length(worldPosFromRadarDepth-ray.origin)/_ProjectionParams.z; //
    // //length(worldPos-ray.origin)/_ProjectionParams.z; // == z01
    // outColor = float4(v,v,v,1);
    // return;
    
    //if(//isWorldPosInsideOfProjector(/*worldPosFromRadarDepth*/input.positionWS, unity_CameraProjection,_CameraDepthTexture, sampler_CameraDepthTexture))
        
    //)
    //outColor = float4( radarZRaw,radarZRaw,radarZRaw,1);
    //return;
    
    // if (zRaw > radarZRaw)
    // {
    //     outColor = radarZRaw;
    //     return;
    // }
    // outColor = float4(1,0,0,1);
    // return;
    // clip(-1);
    // outColor = isWorldPosInsideOfProjector1//(tempWorldPos)//
    // (input.positionWS)//
    // //isInsideOfProjector1(input.projUV)
    // ? 1: 0;
    //
    // return;
    
    //outColor = float4(projUV.x, projUV.y, 0,1);
    //return;
    
    //float2 projUV2 = ProjectionUVToTex2DUV(input.projUV);
    //float2 screenUVs1 = screenPos.xy / screenPos.w;
	//						float2 uv = ProjectionUVToTex2DUV(input.projUV);
    //float depthFromPos = DepthFromProjection(input.projUV);
     //float z1_raw = SAMPLE_DEPTH_TEXTURE(_RadarDepthTex, sampler_RadarDepthTex, projUV2);
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
    //float maxDistance = _ProjectionParams.z;
    float startRange = _ProjectionParams.y;
    //float visibleRange = _VisibleRange;
    //float depthDistance = (maxDistance*200)/_ProjectionParams.z;
    float d0 = 0;
    
    //bool blocked = false;
    //int step = 2;
    int count = 100;
    float distance = ((_VisibleRange-startRange)/count)*0.9;
    //float stepDistance = (distance/step);
    //float worldDistance = (1/depthDistance);
    //float2 distance2 = 0;
    //float goggleVisibility = saturate(remap(_SightMode,0.9,1,0,1));
    
    for(int i = 0; i<count; ++i)
    {
       
       float proceedDistance = startRange + i*distance;

        float3 proceedRayPoint = ray.origin + ray.direction*proceedDistance;
        float4 worldPosToProjUV = ProjectWorldPosToUV(_RadarCameraMatrixVP, proceedRayPoint);

        float2 worldPosProjUV2 = ProjectionUVToTex2DUV(worldPosToProjUV);

        if(isBlocked((proceedDistance-distance*_DepthBias)/_ProjectionParams.z, z01))
        {
            clip(-1);
            return;
        }
        
        if(!ClipUVBoarder(worldPosProjUV2) && !ClipBackProjection(worldPosToProjUV))
        {
                     
            float dfp = DepthFromProjection(worldPosToProjUV);
            float dfd = DepthFromDepthmap(_RadarDepthTex, sampler_RadarDepthTex, worldPosProjUV2, 1);
            float eyeDepth = LinearEyeDepth(Linear01Depth(dfp, _ZBufferParams), _ZBufferParams);
            float shadow = ProjectionShadow(dfp, dfd);
            if( shadow < _Temp*0.5*eyeDepth )
            {
                //return false;
                if(shadow > _Temp*-0.5)
                {
                    outColor.rb = saturate(remap( eyeDepth, 0,1, -1,1));
                    outColor.g = saturate(remap(eyeDepth, 0,1,0,2));   
                }
                else
                {
                    outColor.r = lerp( eyeDepth*5, 0, shadow*-5); 
                    outColor.yz = proceedDistance/_VisibleRange;
                    
                }
                outColor.a = 1;
                return;
            }
        }
        // if(isWorldPosInsideOfProjector())
        // {
        //     
        // }
        
        // if(length(proceedDistance) > )//isBlocked(proceedDistance, z01))
         {
        //     d0 = i*distance ;//+ distance2;
        //     //blocked = true;
        //     float value = remap(d0,0,  _VisibleRange,1,0);
        //     value *= saturate(remap(_SightMode,0.75,0.9,0,1));// * _Time.w;
        //     outColor = float4(value,value,value,lerp(value,1,goggleVisibility));
        //     ;
        //     return;
        //     break;
         }
        // if(isWorldPosInsideOfProjector(
        //   ray.origin + (ray.direction*proceedDistance)  //ray.origin+(ray.direction*_DepthBias)//reachedWorldPos
        //    ,_RadarProjector, _RadarDepthTex,sampler_RadarDepthTex))
        // {
        //     d0 = i*distance;
        //     //blocked = true;
        //     float value = remap(d0,0,  _VisibleRange,1,0);
        //     outColor = float4(value,0,0,lerp(_SightMode*value,1,goggleVisibility));
        //     return;
        //     break;
        //
        // }
       
       
       
         
       
    }

    clip(-1);
    outColor = float4(0,0,0,0);//goggleVisibility);
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
