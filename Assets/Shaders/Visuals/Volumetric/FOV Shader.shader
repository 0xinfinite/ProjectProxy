Shader "FOV Depth"
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

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float viewDepth : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // view space in the shader is -z forward
                o.viewDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                return  i.viewDepth;
            }
            ENDCG
        }
    }
}