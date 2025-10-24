Shader "Custom/DrillDeformPersistentSafe"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _DamageMap("Damage Map", 2D) = "white" {}
        _DisplaceAmount("Displacement Amount", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "DrillDeformSafe"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _DamageMap;
            float4 _BaseColor;
            float _DisplaceAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                // safe default: mask 0 = no displacement
                float mask = tex2D(_DamageMap, v.uv).r;
                worldPos -= worldNormal * _DisplaceAmount * mask;

                o.worldPos = worldPos;
                o.uv = v.uv;
                o.pos = UnityObjectToClipPos(float4(worldPos,1.0));

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _BaseColor; // no darkening
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
