// Purpose: Shared transparent depth prepass helpers for character shaders.
#ifndef CHARA_TRANSPARENT_DEPTH_PREPASS_INCLUDED
#define CHARA_TRANSPARENT_DEPTH_PREPASS_INCLUDED

#ifndef CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING
#define CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING(positionCS)
#endif

struct CharaTransparentDepthAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CharaTransparentDepthVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

CharaTransparentDepthVaryings CharaTransparentDepthPrepassVertex(CharaTransparentDepthAttributes input)
{
    CharaTransparentDepthVaryings output = (CharaTransparentDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 CharaTransparentDepthPrepassFragment(CharaTransparentDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    CHARA_TRANSPARENT_DEPTH_APPLY_DITHERING(input.positionCS);
    return 0;
}

#endif
