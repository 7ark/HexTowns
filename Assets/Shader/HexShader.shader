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
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma surface surf Standard addshadow fullforwardshadows
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:makeInstanced

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            struct Input
            {
                float2 uv_MainTex;
            };

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                struct Data
                {
                    float4 pos_s;
                    float index;
                };
                StructuredBuffer<Data> dataBuffer;
            #endif

            void makeInstanced()
            {
                #define unity_ObjectToWorld unity_ObjectToWorld
                #define unity_WorldToObject unity_WorldToObject
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

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                // Albedo comes from a texture tinted by color
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                fixed4 c = UNITY_SAMPLE_TEX2D(_MainTex, float3(IN.uv_MainTex, dataBuffer[unity_InstanceID].index)) * _Color;
                #else
                fixed4 c = UNITY_SAMPLE_TEX2D(_MainTex, float3(IN.uv_MainTex, 0)) * _Color;
                #endif

                o.Albedo = c.rgb;
                // Metallic and smoothness come from slider variables
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = c.a;
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