Shader "VaultDash/ToonCelShaded"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _AmbientLight ("Ambient Light", Range(0,1)) = 0.3
        _Steps ("Shading Steps", Range(1,4)) = 3
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0.02
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // === CEL SHADED PASS ===
        Pass
        {
            Name "CelShading"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 pos : SV_POSITION;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _AmbientLight;
            float _Steps;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                // Diffuse lighting
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = dot(normal, lightDir);

                // Cel-shading: quantize naar N stappen (geen smooth gradient)
                float celShade = ceil(saturate(NdotL) * _Steps) / _Steps;
                celShade = max(celShade, _AmbientLight);

                // GEEN specular — flat toon look
                fixed4 color = texColor * fixed4(_LightColor0.rgb * celShade, 1.0);
                return color;
            }
            ENDCG
        }

        // === OUTLINE PASS ===
        Pass
        {
            Name "Outline"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                float4 pos = UnityObjectToClipPos(v.vertex);
                float3 clipNorm = mul((float3x3)UNITY_MATRIX_VP,
                                     mul((float3x3)unity_ObjectToWorld, norm));
                pos.xy += normalize(clipNorm.xy) * _OutlineWidth;
                o.pos = pos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
