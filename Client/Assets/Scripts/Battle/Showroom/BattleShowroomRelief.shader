Shader "MyDefense/Showroom/Concept Relief"
{
    Properties
    {
        _MainTex ("Artwork", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        AlphaToMask On

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct VertexInput
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings Vert(VertexInput input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.normal = UnityObjectToWorldNormal(input.normal);
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 paint = tex2D(_MainTex, input.uv);
                clip(paint.a - 0.27);
                float diffuse = saturate(dot(normalize(input.normal), normalize(float3(-0.45, 0.8, -0.55))));
                paint.rgb *= lerp(0.86, 1.08, diffuse);
                return paint;
            }
            ENDCG
        }
    }
}
