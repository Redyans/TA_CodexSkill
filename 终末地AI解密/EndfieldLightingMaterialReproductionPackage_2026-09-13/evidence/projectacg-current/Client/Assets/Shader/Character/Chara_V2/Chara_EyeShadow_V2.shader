Shader "Valkyria/Chara/Chara_EyeShadow_V2"
{
    Properties
    {
        // RedTint (driven by script via MaterialPropertyBlock)
        [HideInInspector] _CharacterRedTint ("_CharacterRedTint", Range(0, 1)) = 0
        [HideInInspector] _CharacterRedTintColor ("_CharacterRedTintColor", Color) = (1, 0, 0, 1)
        [HideInInspector] _Alpha ("Alpha", Range(0, 1)) = 1
        [Foldout(1, 1, 0, 1)] _eyeShadow("眼部阴影_Foldout", Float) = 1
        _EyeShadowMask ("眼部阴影遮罩", 2D) = "white" {}
        _EyeShadowColor("眼部阴影颜色", Color) = (1.0,1.0,1.0,1.0)

    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 normal: NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            
            // params
            #include "Chara_Eye_V2/Chara_Eye_V2_Bindings.hlsl"
            

            #include "Common/Func_Chara_GlobalEffect_V2.hlsl"
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half eyeShadowMask = SAMPLE_TEXTURE2D(_EyeShadowMask, sampler_EyeShadowMask, i.uv).r;
                half3 finalColor = ApplyCharaRedTint(_EyeShadowColor);
                return float4(finalColor, eyeShadowMask * _EyeShadowColor.a * saturate(_Alpha));
            }
            ENDHLSL
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}
