#ifndef CUSTOM_PROJECTION_INCLUDED
#define CUSTOM_PROJECTION_INCLUDED

			float4 UVSpreadOnVertex(float2 inputUV) {
				float4 uv = float4(0, 0, 0, 1);
				uv.xy = float2(1, _ProjectionParams.x)*(inputUV.xy* float2(2, 2) - float2(1, 1));
				return uv;
			}

			float4 ProjectUV(float4x4 projection, float4 inputVertex) {
				float4 projVertex = mul(projection, TransformObjectToWorld(inputVertex)/*mul(unity_ObjectToWorld, inputVertex)*/);

				return ComputeScreenPos(projVertex);
			}
			float4 ProjectWorldPosToUV(float4x4 projection, float3 worldPos) {
				float4 projVertex = mul(projection, float4(worldPos,1));

				return ComputeScreenPos(projVertex);
			}

			float4 ProjectUVLocal(float4x4 worldToHClipMatrix, float4 inputVertex) {
				float4 projVertex = mul(worldToHClipMatrix, TransformObjectToWorld(inputVertex)/*mul(unity_ObjectToWorld, inputVertex)*/);

				#if UNITY_REVERSED_Z
				projVertex.z = 1-projVertex.z;
				#endif
				
				return ComputeScreenPos(projVertex);
			}

			float4 ProjectUVFromWorldPos(float4x4 projection, float3 worldPos) {
				float4 projVertex = mul(projection, float4(worldPos.x,worldPos.y, worldPos.z, 1) );
//				
//#if UNITY_REVERSED_Z
//				projVertex.z = 1-projVertex.z;
//				#endif

				return ComputeScreenPos(projVertex);
				//float3 os = TransformWorldToObject(worldPos);
				//return ProjectUV( projection, float4(os.xyz, 1) );
			}
			
			float2 ProjectionUVToTex2DUV(float4 projUV) {
				return projUV.xy / projUV.w;
			}


			float2 ProjectionUVToTex2DUV(float4 projUV, float4 offset) {
				float2 xy = projUV.xy + offset.xy * offset.zw; 
				return xy / projUV.w;
			}

			bool ClipUVBoarder(float2 uv) {
				if (uv.x < 0 || uv.x>1) return true;
				if (uv.y < 0 || uv.y>1) return true;

				return false;
			}

			bool ClipBackProjection(float4 projUV) {
				if (projUV.z < 0) return true;
				
				return false;
			}

			float NormalDotProjector(float3 normal, float3 worldPos, float3 projectorPos) {
				return dot(normal, normalize(projectorPos- worldPos));

			}

			float DepthFromProjection(float4 projUV) {
//#if UNITY_REVERSED_Z
//				return projUV.z / projUV.w;
//#endif
				return 1 - projUV.z / projUV.w;
			}

			float DepthFromDepthmap(Texture2D depthMap, sampler sampler_depthMap, float2 projectedUV, float bias) {
				return SAMPLE_TEXTURE2D(depthMap, sampler_depthMap, projectedUV).r*bias;
			}

			float DepthFromDepthmap(Texture2D depthMap, sampler sampler_depthMap, float2 projectedUV) {
				return //LinearEyeDepth(
					//Linear01Depth(
						SAMPLE_TEXTURE2D(depthMap, sampler_depthMap, projectedUV).r//,_ZBufferParams)
				//,_ZBufferParams)
				;
			}

			bool ClipProjectionShadow(float depthFromPos, float depthFromMap, float calculationOffset) {
				if (depthFromPos - depthFromMap < calculationOffset) return true;

				return false;
			}

			float ProjectionShadow(float depthFromPos, float depthFromMap) {
				return depthFromPos - depthFromMap;

			}


#endif

			//#define CLIP_BACK_PROJECTION if (i.projUV.z < 0) //return _BlankColor;
			//#define CLIP_BOARDER_X if (uv.x < 0 || uv.x>1) //return _BlankColor;
			//#define CLIP_BOARDER_Y if (uv.y < 0 || uv.y>1) //return _BlankColor;
			//#define CLIP_NORMAL_DOT if (dot(i.normal, normalize(i.worldPos - _ProjectorWorldPos)) > _NormalClip) 