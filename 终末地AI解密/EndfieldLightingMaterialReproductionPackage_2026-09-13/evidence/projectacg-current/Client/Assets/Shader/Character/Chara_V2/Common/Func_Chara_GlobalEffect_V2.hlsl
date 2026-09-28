// Purpose: Shared color-adjust and fresnel-style global effect helpers.
#ifndef CHARA_GLOBAL_EFFECT_V2_INCLUDED
#define CHARA_GLOBAL_EFFECT_V2_INCLUDED

half3 ApplyCharaRedTint(half3 rgb)
{
    half s = saturate(_CharacterRedTint);
    // Preserve original contrast while adding red tint.
    rgb = rgb + (half3)_CharacterRedTintColor.rgb * s;
    return rgb;
}

half3 ApplyCharaFresnel(half3 rgb, half3 normalWS, half3 viewDirWS, half fresnelScale, half fresnelPower, half4 fresnelColor)
{
    // Fresnel = scale * pow(1 - dot(N, V), power)
    half fresnel = fresnelScale * pow(saturate(1.0 - dot(normalWS, viewDirWS)), fresnelPower);
    rgb = rgb + fresnelColor.rgb * fresnel;
    return rgb;
}

#endif
