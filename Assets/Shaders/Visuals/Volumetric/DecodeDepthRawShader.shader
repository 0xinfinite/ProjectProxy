Shader "Hidden/Decode Depth Raw Shader"
{
    Properties
    {
        
        _DepthTex("Depth Texture", 2D) = "black" {}

        // BlendMode
        _Surface("__surface", Float) = 0.0
            //_Blend("__mode", Float) = 0.0
            //_Cull("__cull", Float) = 2.0
            [ToggleUI] _AlphaClip("__clip", Float) = 0.0
            //[HideInInspector] _BlendOp("__blendop", Float) = 0.0
            //[HideInInspector] _SrcBlend("__src", Float) = 1.0
            //[HideInInspector] _DstBlend("__dst", Float) = 0.0
            //[HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
            //[HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
            [HideInInspector] _ZWrite("__zw", Float) = 1.0
            [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

            // Editmode props
            _QueueOffset("Queue offset", Float) = 0.0

            // ObsoleteProperties
            [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
            [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)
            [HideInInspector] _SampleGI("SampleGI", float) = 0.0 // needed from bakedlit
    }

        SubShader
            {
                Tags
                {
                    "RenderType" = "Opaque"
                    "IgnoreProjector" = "True"
                    "RenderPipeline" = "UniversalPipeline"
                }
                LOD 100

                // -------------------------------------
                // Render State Commands
                Blend SrcAlpha OneMinusSrcAlpha //[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
                ZWrite[_ZWrite]
                Cull Front//[_Cull]

                Pass
                {
                    Name "Unlit"

                    // -------------------------------------
                    // Render State Commands
                    AlphaToMask[_AlphaToMask]

                    HLSLPROGRAM
                    #pragma target 2.0

                // -------------------------------------
                // Shader Stages
                #pragma vertex UnlitPassVertex
                #pragma fragment UnlitPassFragment

                // -------------------------------------
                // Material Keywords
                #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ALPHATEST_ON
                #pragma shader_feature_local_fragment _ALPHAMODULATE_ON

                // -------------------------------------
                // Unity defined keywords
                #pragma multi_compile_fog
                #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile _ DEBUG_DISPLAY
                #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
                #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

                //--------------------------------------
                // GPU Instancing
                #pragma multi_compile_instancing
                #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

                // -------------------------------------
                // Includes
                
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

//#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
//                StructuredBuffer<float3x2> _Stretched;
//#endif

            VertexPositionInputs GetVertexPositionInputsOverrided(float3 positionOS, float4 color)
            {
                VertexPositionInputs input;
                input.positionWS = positionOS;//TransformObjectToWorld(positionOS);
//#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
//                input.positionWS += lerp(float3(0, 0, 0), float3(_Stretched.m00, _Stretched.m01, _Stretched.m02), color.b);
//                input.positionWS += lerp(float3(0, 0, 0), float3(_Stretched.m10, _Stretched.m11, _Stretched.m12), color.a);
//#endif
                input.positionVS = TransformWorldToView(input.positionWS);
                input.positionCS = TransformWorldToHClip(input.positionWS);

                float4 ndc = input.positionCS * 0.5f;
                input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                input.positionNDC.zw = input.positionCS.zw;

                return input;
            }

            /*CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Surface;
            CBUFFER_END*/

#ifdef UNITY_DOTS_INSTANCING_ENABLED
                /*UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
                UNITY_DOTS_INSTANCED_PROP(float, _Surface)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)*/

//#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
//#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
//#define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
#endif



                //#include "VolumetricForwardPass.hlsl"

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

                //TEXTURE2D(_CameraDepthTexture);
                //SAMPLER(sampler_CameraDepthTexture);

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
                    float4 projUV1 : TEXCOORD7;
                    //float4 positionOS : TEXCOORD8;
                    float4 projUV2 : TEXCOORD8;
                    float4 projUV3 : TEXCOORD9;
                    float4 projUV4 : TEXCOORD10;

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

                TEXTURE2D(_DepthTex);
                SAMPLER(sampler_DepthTex);
                /*TEXTURE2D(_DepthTex1);
                SAMPLER(sampler_DepthTex1);
                TEXTURE2D(_DepthTex2);
                SAMPLER(sampler_DepthTex2);
                TEXTURE2D(_DepthTex3);
                SAMPLER(sampler_DepthTex3);
                TEXTURE2D(_DepthTex4);
                SAMPLER(sampler_DepthTex4);
                int _ProjectorCount;*/

                //float4x4 _MatrixHClipToWorld;
                float4x4 _CameraMatrixVP;
                /*float4x4 _Camera1MatrixVP;
                float4x4 _Projector1;
                float4x4 _Camera2MatrixVP;
                float4x4 _Projector2;
                float4x4 _Camera3MatrixVP;
                float4x4 _Projector3;
                float4x4 _Camera4MatrixVP;
                float4x4 _Projector4;
                float _DepthBias;
                float _VisibleRange;
                float _SightMode;*/

                Varyings UnlitPassVertex(Attributes input)
                {
                    Varyings output = (Varyings)0;

                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                    output.positionCS = vertexInput.positionCS;
                    output.uv = half2(0.5, 0.5);//TRANSFORM_TEX(input.uv, _BaseMap);
                    //output.screenPos = ComputeScreenPos(output.positionCS);
                    output.positionWS = vertexInput.positionWS;
                    //output.positionOS = input.positionOS;
                     output.screenPos = ProjectUV(_CameraMatrixVP, input.positionOS);//
                    //output.projUV1 = ProjectUV(_Camera1MatrixVP, input.positionOS);//
                    //ProjectUVFromWorldPos(_Projector1,  output.positionWS);
                    //output.projUV2 = ProjectUV(_Camera2MatrixVP, input.positionOS);
                    //output.projUV3 = ProjectUV(_Camera3MatrixVP, input.positionOS);
                    //output.projUV4 = ProjectUV(_Camera4MatrixVP, input.positionOS);
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
                    if (ClipBackProjection(projUV) || ClipUVBoarder(projUV2) || ClipProjectionShadow(dfp, dfd, 0))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                bool isWorldPosInsideOfProjector(float3 worldPos, float4x4 _Projector, Texture2D _DepthTex, sampler sampler_DepthTex)
                {
                    float4 projUV = ProjectUVFromWorldPos(_Projector, worldPos);
                    float2 projUV2 = ProjectionUVToTex2DUV(projUV);

                    projUV2.y = 1 - projUV2.y;

                    float dfp = DepthFromProjection(projUV);
                    float dfd = DepthFromDepthmap(_DepthTex, sampler_DepthTex, projUV2, 2);
                    if (ClipBackProjection(projUV) || ClipUVBoarder(projUV2) ||
                        ClipProjectionShadow(dfp, dfd, 0)
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

                    half2 uv = input.uv;
                    float2 screenUVs = ProjectionUVToTex2DUV(input.screenPos);
                    Ray ray = CreateCameraRay(screenUVs);

                    //outColor = float4(ray.direction.x, ray.direction.y,ray.direction.z,1);
                     //outColor.r= 0;//remap(outColor.r,-1,1,0,1);
                    // outColor.g= 0;//remap(outColor.g,-1,1,0,1);
                  // outColor.b= 0;//remap(outColor.b,-1,1,0,1);
                   //
                    //return;

                    float zRaw = SAMPLE_DEPTH_TEXTURE(_DepthTex, sampler_DepthTex, screenUVs);//(_CameraDepthTexture, sampler_CameraDepthTexture, screenUVs);
                    
                    float z01 = Linear01Depth(zRaw, _ZBufferParams);
                    outColor = float4(1, 0, 0, 1);
                    //return;
                    // float zEye = LinearEyeDepth(z01, _ZBufferParams);
                    //outColor = float4( z01,z01,z01,1);
                    //return;

                    //float3 tempWorldPos =  TransformUVToWorldPos(screenUVs, zRaw);
                    // outColor = isWorldPosInsideOfProjector1//(tempWorldPos)//
                    // (input.positionWS)//
                    // //isInsideOfProjector1(input.projUV1)
                    // ? 1: 0;
                    //
                    // return;

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
                    

                   // outColor = float4(0, 0, 0, 1);

#ifdef _WRITE_RENDERING_LAYERS
                    uint renderingLayers = GetMeshRenderingLayer();
                    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
                }

                ENDHLSL
            }

            }

                FallBack "Hidden/Universal Render Pipeline/FallbackError"
                // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.UnlitShader"
}


//Shader "Hidden/Decode Depth Raw Shader"
//{
//    Properties
//    {
//        _MainTex ("Texture", 2D) = "white" {}
//    }
//    SubShader
//    {
//        // No culling or depth
//        Cull Off ZWrite Off ZTest Always
//
//        Pass
//        {
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//
//            #include "UnityCG.cginc"
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float2 uv : TEXCOORD0;
//            };
//
//            struct v2f
//            {
//                float2 uv : TEXCOORD0;
//                float4 vertex : SV_POSITION;
//            };
//
//            v2f vert (appdata v)
//            {
//                v2f o;
//                o.vertex = UnityObjectToClipPos(v.vertex);
//                o.uv = v.uv;
//                return o;
//            }
//
//            sampler2D _MainTex;
//
//            fixed4 frag (v2f i) : SV_Target
//            {
//                fixed4 col = tex2D(_MainTex, i.uv);
//                // just invert the colors
//                col.rgb = 1 - col.rgb;
//                return col;
//            }
//            ENDCG
//        }
//    }
//}
