Shader "Custom/HexSelectShader"
{
    Properties
    {
        _OffsetX ("X Offset", int) = 0
        _OffsetZ ("Z Offset", int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        ZWrite On
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            int _OffsetX;
            int _OffsetZ;
            
            UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Data
            {
                float4 pos_s;
		        float textureIndex;
                int hexCoordX;
                int hexCoordZ;
            };
            StructuredBuffer<Data> dataBuffer;

            struct v2f {
                float4 vertex : SV_POSITION;
                uint instanceID : SV_InstanceID;
            };
            
            v2f vert (float4 vertex : POSITION, uint instanceID : SV_InstanceID)
            {
                float4 pos_s = dataBuffer[instanceID].pos_s;
		        float3 pos = pos_s.xyz;
		        float scale = pos_s.w;

                unity_ObjectToWorld._11_21_31_41 = float4(1, 0, 0, 0);
                unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
                unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1, 0);
                unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);

		        unity_WorldToObject = unity_ObjectToWorld;
		        unity_WorldToObject._14_24_34 *= -1;
		        unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;

                v2f output;
                output.vertex = UnityObjectToClipPos(vertex);
                output.instanceID = instanceID;
                
                return output;
            }

            float4 frag (v2f input) : SV_Target
            {
                float4 o;
                int x = dataBuffer[input.instanceID].hexCoordX;
                int z = dataBuffer[input.instanceID].hexCoordZ;
                
                o.x = asfloat(x + _OffsetX);
                o.y = asfloat(z + _OffsetZ);

                o.z = 0;
                o.w = 1;
                return o;
            }

            ENDCG
        }
    }
}
