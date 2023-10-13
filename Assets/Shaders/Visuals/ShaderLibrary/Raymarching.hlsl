#ifndef RAYMARCHING_INCLUDED
#define RAYMARCHING_INCLUDED

#define MAX_STEPS = 128 
#define MAX_DIST = 1000

Texture2D<float4> Source;
RWTexture2D<float4> Destination;

float4x4 _CameraInverseProjection;

float3 _CameraPosition;
float3 _CameraDirection;

float3 _Light;
bool positionLight;

static const float maxDst = 80;
static const float epsilon = 0.001f;
static const float shadowBias = epsilon * 50;

struct Shape {
    
    float3 position;
    float3 size;
    float3 colour;
    int shapeType;
    int operation;
    float blendStrength;
    int numChildren;
};

StructuredBuffer<Shape> shapes;
int numShapes;


struct Ray {
    float3 origin;
    float3 direction;
    float2 uv;
};

float SphereDistance(float3 eye, float3 centre, float radius) {
    return distance(eye, centre) - radius;
}

float CubeDistance(float3 eye, float3 centre, float3 size) {
    float3 o = abs(eye-centre) -size;
    float ud = length(max(o,0));
    float n = max(max(min(o.x,0),min(o.y,0)), min(o.z,0));
    return ud+n;
}

// Following distance functions from http://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float TorusDistance(float3 eye, float3 centre, float r1, float r2)
{   
    float2 q = float2(length((eye-centre).xz)-r1,eye.y-centre.y);
    return length(q)-r2;
}

float PrismDistance(float3 eye, float3 centre, float2 h) {
    float3 q = abs(eye-centre);
    return max(q.z-h.y,max(q.x*0.866025+eye.y*0.5,-eye.y)-h.x*0.5);
}


float CylinderDistance(float3 eye, float3 centre, float2 h) {
    float2 d = abs(float2(length((eye).xz), eye.y)) - h;
    return length(max(d,0.0)) + max(min(d.x,0),min(d. y,0));
}

Ray CreateRay(float3 origin, float3 direction) {
    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    return ray;
}

Ray CreateCameraRay(float2 uv) {
   
    float3 origin = _WorldSpaceCameraPos;
    
    uv.x = remap(uv.x,0,1,1,0);//1-uv.x;
    //uv.y = remap(uv.y, 0,1,1,0);
    float3 direction = ComputeWorldSpacePosition(uv, 10,  unity_CameraInvProjection//mul(unity_CameraToWorld,unity_CameraInvProjection)
        );//mul(unity_CameraInvProjection, uv_ray).xyz;

     direction = mul(unity_CameraToWorld, direction).xyz;
    
    #if UNITY_REVERSED_Z
   // direction.z *= -1.0;
    #endif
    //direction = mul(direction, float3(uv,0));
    //direction += mul(unity_CameraProjection, float4(uv,0,1)).xyz;
    //direction = normalize( mul(unity_CameraToWorld, float4(direction,0)).xyz);
    direction = normalize(direction);
    return CreateRay(origin,direction);
}

Ray CreateCameraRay(float2 uv, float3 camPos, float4x4 cameraInvViewProjection, float4x4 cameraToWorld) {
   
    float3 origin = camPos;
    
    //uv.x = remap(uv.x,0,1,1,0);//1-uv.x;
    //uv.y = remap(uv.y, 0,1,1,0);
    float3 direction = ComputeWorldSpacePosition(uv, 10,  cameraInvViewProjection//mul(unity_CameraToWorld,unity_CameraInvProjection)
        );//mul(unity_CameraInvProjection, uv_ray).xyz;

   // direction = mul(cameraToWorld, direction).xyz;
    
    #if UNITY_REVERSED_Z
    // direction.z *= -1.0;
    #endif
    //direction = mul(direction, float3(uv,0));
    //direction += mul(unity_CameraProjection, float4(uv,0,1)).xyz;
    //direction = normalize( mul(unity_CameraToWorld, float4(direction,0)).xyz);
    direction = normalize(direction);
    return CreateRay(origin,direction);
}

// polynomial smooth min (k = 0.1);
// from https://www.iquilezles.org/www/articles/smin/smin.htm
float4 Blend( float a, float b, float3 colA, float3 colB, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    float blendDst = lerp( b, a, h ) - k*h*(1.0-h);
    float3 blendCol = lerp(colB,colA,h);
    return float4(blendCol, blendDst);
}

float4 Combine(float dstA, float dstB, float3 colourA, float3 colourB, int operation, float blendStrength) {
    float dst = dstA;
    float3 colour = colourA;

    if (operation == 0) {
        if (dstB < dstA) {
            dst = dstB;
            colour = colourB;
        }
    } 
    // Blend
    else if (operation == 1) {
        float4 blend = Blend(dstA,dstB,colourA,colourB, blendStrength);
        dst = blend.w;
        colour = blend.xyz;
    }
    // Cut
    else if (operation == 2) {
        // max(a,-b)
        if (-dstB > dst) {
            dst = -dstB;
            colour = colourB;
        }
    }
    // Mask
    else if (operation == 3) {
        // max(a,b)
        if (dstB > dst) {
            dst = dstB;
            colour = colourB;
        }
    }

    return float4(colour,dst);
}

float GetShapeDistance(Shape shape, float3 eye) {
   
    if (shape.shapeType == 0) {
        return SphereDistance(eye, shape.position, shape.size.x);
    }
    else if (shape.shapeType == 1) {
        return CubeDistance(eye, shape.position, shape.size);
    }
    else if (shape.shapeType == 2) {
        return TorusDistance(eye, shape.position, shape.size.x, shape.size.y);
    }

    return maxDst;
}


// float4 SceneInfo(float3 eye) {
//     float globalDst = //LinearDepthToEyeDepth(
//         SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture,(eye.x, eye.y));
//     //);// maxDst;
//     float3 globalColour = 1;
//     
//     for (int i = 0; i < numShapes; i ++) {
//         Shape shape = shapes[i];
//         int numChildren = shape.numChildren;
//
//         float localDst = GetShapeDistance(shape,eye);
//         float3 localColour = shape.colour;
//
//
//         for (int j = 0; j < numChildren; j ++) {
//             Shape childShape = shapes[i+j+1];
//             float childDst = GetShapeDistance(childShape,eye);
//
//             float4 combined = Combine(localDst, childDst, localColour, childShape.colour, childShape.operation, childShape.blendStrength);
//             localColour = combined.xyz;
//             localDst = combined.w;
//         }
//         i+=numChildren; // skip over children in outer loop
//         
//         float4 globalCombined = Combine(globalDst, localDst, globalColour, localColour, shape.operation, shape.blendStrength);
//         globalColour = globalCombined.xyz;
//         globalDst = globalCombined.w;        
//     }
//
//     return float4(globalColour, globalDst);
// }

// float3 EstimateNormal(float3 p) {
//     float x = SceneInfo(float3(p.x+epsilon,p.y,p.z)).w - SceneInfo(float3(p.x-epsilon,p.y,p.z)).w;
//     float y = SceneInfo(float3(p.x,p.y+epsilon,p.z)).w - SceneInfo(float3(p.x,p.y-epsilon,p.z)).w;
//     float z = SceneInfo(float3(p.x,p.y,p.z+epsilon)).w - SceneInfo(float3(p.x,p.y,p.z-epsilon)).w;
//     return normalize(float3(x,y,z));
// }

// float CalculateShadow(Ray ray, float dstToShadePoint) {
//     float rayDst = 0;
//     int marchSteps = 0;
//     float shadowIntensity = .2;
//     float brightness = 1;
//
//     while (rayDst < dstToShadePoint) {
//         marchSteps ++;
//         float4 sceneInfo = SceneInfo(ray.origin);
//         float dst = sceneInfo.w;
//         
//         if (dst <= epsilon) {
//             return shadowIntensity;
//         }
//
//         brightness = min(brightness,dst*200);
//
//         ray.origin += ray.direction * dst;
//         rayDst += dst;
//     }
//     return shadowIntensity + (1-shadowIntensity) * brightness;
// }

#endif