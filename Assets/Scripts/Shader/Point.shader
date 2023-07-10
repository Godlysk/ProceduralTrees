Shader "Custom/Point"
{
    Properties
    {
        _Color ("Color", Vector) = (0.1, 0.1, 0.1, 1.0)
    }
    SubShader
    {
        Pass
        {
            ZWrite On Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;

            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                half psize : PSIZE;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                float3 position = mul(unity_ObjectToWorld, v.vertex);
                float factor = clamp(length(position - _WorldSpaceCameraPos), 5.0, 50.0);
                o.worldPosition = position;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.psize = 150.0 / factor;
                return o;
            }

            fixed4 frag (VertexOutput i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
