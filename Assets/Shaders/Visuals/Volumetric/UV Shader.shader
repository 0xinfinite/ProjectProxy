Shader "Camera UV"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

           /* struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };*/

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                //float viewDepth : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;//o.uv = v.uv;
                // view space in the shader is -z forward
                //o.viewDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(i.uv.x,i.uv.y,0,1);//i.viewDepth;
            }
            ENDCG
        }
    }
}