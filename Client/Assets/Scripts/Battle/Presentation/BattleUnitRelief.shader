Shader "MyDefense/Battle/Unit Relief"
{
    Properties { _MainTex ("Artwork", 2D) = "white" {} _PresentationDim ("Presentation dim",Range(0,1))=1 }
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
            float _PresentationDim;
            struct VertexInput { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };
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
                float coverage=saturate((paint.a-.27)/max(fwidth(paint.a),.01)+.5);
                clip(coverage-.001);
                paint.a=coverage;
                float diffuse = saturate(dot(normalize(input.normal), normalize(float3(-.45, .8, -.55))));
                paint.rgb *= lerp(.86, 1.08, diffuse);
                paint.rgb *= _PresentationDim;
                return paint;
            }
            ENDCG
        }
    }
}
