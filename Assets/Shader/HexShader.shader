Shader "Custom/HexShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Terrain Texture Array", 2DArray) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard addshadow fullforwardshadows vertex:vert
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:makeInstanced

            #pragma target 3.5

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            float4 _MainTex_ST;

            half _Glossiness;
            half _Metallic;
            half4 _Color;

            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                struct Data
                {
                    float4 pos_s;
                    float textureIndex;
                    int hexCoordX;
                    int hexCoordY;
                };
                StructuredBuffer<Data> dataBuffer;
            #endif

            void makeInstanced()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    float4 pos_s = dataBuffer[unity_InstanceID].pos_s;
                    float3 pos = pos_s.xyz;
                    float scale = pos_s.w;

                    unity_ObjectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
                    unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
                    unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
                    unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);

                    unity_WorldToObject = unity_ObjectToWorld;
                    unity_WorldToObject._14_24_34 *= -1;
                    unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
                #endif
            }

            struct Input
            {
                float3 worldPos;
                float3 normal;
            };

            void vert(inout appdata_full v, out Input o)
            {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                o.normal = v.normal;
            }

            float getTextureIndex()
            {
                float index;
                // Albedo comes from a texture tinted by color
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                index = dataBuffer[unity_InstanceID].textureIndex;
                #else
                index = 0;
                #endif
                return index;
            }

            half4 sampleTex(float2 uv)
            {
                float index = getTextureIndex();
                uv = TRANSFORM_TEX(uv, _MainTex);
                return UNITY_SAMPLE_TEX2D(_MainTex, float3(uv, index));
            }

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                float3 bf = normalize(abs(IN.normal));
                bf /= dot(bf, (float3)1);

                float control = IN.normal.y;

                // side faces
                float2 tx = IN.worldPos.yz;
                float2 tz = IN.worldPos.xy;
                // top faces
                float2 uvTop = IN.worldPos.xz;

                float2 uv = lerp(tx, uvTop, control);
                half4 cx = sampleTex(uv) * lerp(bf.x, 1, control);
                half4 cz = sampleTex(tz) * lerp(bf.z, 0, control);
                half4 color = (cx + cz) * _Color;

                o.Albedo = color.rgb;
                o.Alpha = color.a;
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
            }
            ENDCG

                // shadow caster rendering pass, implemented manually
                // using macros from UnityCG.cginc
                Pass
                {
                    Tags {"LightMode" = "ShadowCaster"}

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #pragma multi_compile_shadowcaster
                    #include "UnityCG.cginc"

                    struct v2f {
                        V2F_SHADOW_CASTER;
                    };

                    v2f vert(appdata_base v)
                    {
                        v2f o;
                        TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                        return o;
                    }

                    float4 frag(v2f i) : SV_Target
                    {
                        SHADOW_CASTER_FRAGMENT(i)
                    }
                    ENDCG
                }
        }
            FallBack "Diffuse"
}