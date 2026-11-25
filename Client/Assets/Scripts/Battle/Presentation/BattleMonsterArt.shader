Shader "MyDefense/Battle/Monster Artwork"
{
    Properties { _MainTex("Artwork",2D)="white"{} _Opacity("Opacity",Range(0,1))=1 _Flash("Hit",Range(0,1))=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Opacity,_Flash;
            struct Input{float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct Output{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            Output Vert(Input v){Output o;o.position=UnityObjectToClipPos(v.position);o.uv=v.uv;return o;}
            fixed4 Frag(Output input):SV_Target
            {
                fixed4 paint=tex2D(_MainTex,input.uv);
                paint.a=saturate((paint.a-.12)/max(fwidth(paint.a),.01)+.5)*_Opacity;
                clip(paint.a-.005);paint.rgb=lerp(paint.rgb,float3(.79,.97,1),_Flash*.28);return paint;
            }
            ENDCG
        }
    }
}
