Shader "Hidden/DamageDraw"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _BrushPos; // x=U, y=V, z=brushSize, w=drawStrength

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                float dist = distance(i.uv, _BrushPos.xy);
                float brush = smoothstep(_BrushPos.z, 0.0, dist);
                // Additively paint (clamp to [0,1])
                return saturate(col.r + brush * _BrushPos.w);
            }
            ENDCG
        }
    }
    FallBack Off
}
