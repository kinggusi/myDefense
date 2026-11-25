Shader "MyDefense/Battle/Board Surface"
{
    Properties { _MainTex ("Painted floor", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 Frag(v2f_img input) : SV_Target { return tex2D(_MainTex, input.uv); }
            ENDCG
        }
    }
}
