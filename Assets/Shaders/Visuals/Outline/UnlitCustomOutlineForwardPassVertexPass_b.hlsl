
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    //input.positionOS.xyz += input.normalOS.xyz * _Thickness * input.color.b*0.01;

    //VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexPositionNormalInputs vertexInput = GetVertexPositionNormalInputs(input.positionOS.xyz, input.normalOS, input.tangentOS, 0);
    VertexPositionInputs vertexPosInput = vertexInput.position;
    VertexNormalInputs normalInput = vertexInput.normal;

    output.positionCS = vertexPosInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
#if defined(_FOG_FRAGMENT)
    output.fogCoord = vertexPosInput.positionVS.z;
#else
    output.fogCoord = ComputeFogFactor(vertexPosInput.positionCS.z);
#endif

    //#if defined(DEBUG_DISPLAY)
    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    //VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceViewDir(vertexPosInput.positionWS);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    //vertexInput.positionWS.xyz += normalInput.normalWS * _Thickness * input.color.r;
    output.positionWS = vertexPosInput.positionWS;

    output.viewDirWS = viewDirWS;

    output.color = input.color;
    //#endif

    return output;
}

