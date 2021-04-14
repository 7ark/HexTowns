Shader "Custom/GenericInstancedObject"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Object Texture", 2D) = "white" {}
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

            UNITY_DECLARE_TEX2D(_MainTex);

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct Data
            {
                float4x4 world2obj;
                float4x4 obj2world;
            };
            StructuredBuffer<Data> dataBuffer;
        #endif

        void makeInstanced()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                
            unity_ObjectToWorld = dataBuffer[unity_InstanceID].obj2world;
            unity_WorldToObject = dataBuffer[unity_InstanceID].world2obj;
                //float3 pos_s = dataBuffer[unity_InstanceID].pos_s;
		        //float3 pos = pos_s.xyz;
                //
                //unity_ObjectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
                //unity_ObjectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
                //unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
                //unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);
                //
		        //unity_WorldToObject = unity_ObjectToWorld;
		        //unity_WorldToObject._14_24_34 *= -1;
		        //unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
            #endif
        }
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = UNITY_SAMPLE_TEX2D(_MainTex, float3(IN.uv_MainTex, 0)) * _Color;
            //#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            //fixed4 c = UNITY_SAMPLE_TEX2D (_MainTex, float3(IN.uv_MainTex, dataBuffer[unity_InstanceID].textureIndex))  * _Color;
            //#else
            //#endif
            
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
            Tags {"LightMode"="ShadowCaster"}

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
