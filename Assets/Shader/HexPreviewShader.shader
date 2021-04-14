Shader "Custom/HexPreviewShader"
{
    Properties
    {
        _Color("Color", color) = (0, 0, 0, 255)
    }
        SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

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
            float4 pos : SV_POSITION;
            float4 spos : TEXCOORD1;
            float4 diffuse : COLOR;
            uint instanceID : SV_InstanceID;
        };

        v2f vert(float4 vertex : POSITION, float3 normal : NORMAL, uint instanceID : SV_InstanceID)
        {
            float4 pos_s = dataBuffer[instanceID].pos_s;
            float3 pos = pos_s.xyz;
            float scale = pos_s.w;

            unity_ObjectToWorld._11_21_31_41 = float4(1.025f, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, scale + 0.025f, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, 1.025f, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(pos, 1);

            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;

            v2f output;
            output.pos = UnityObjectToClipPos(vertex);
            output.spos = ComputeScreenPos(output.pos);
            output.instanceID = instanceID;

            output.diffuse = max(dot(normal, normalize(float3(0.8f, 0.8f, 0.3f))), 0);
            output.diffuse.w = 1;

            return output;
        }

        // Returns > 0 if not clipped, < 0 if clipped based
        // on the dither
        // For use with the "clip" function
        // pos is the fragment position in screen space from [0,1]
        float isDithered(float2 pos, float alpha) {
            pos *= _ScreenParams.xy;

            // Define a dither threshold matrix which can
            // be used to define how a 4x4 set of pixels
            // will be dithered
            float DITHER_THRESHOLDS[16] =
            {
                1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
            };

            uint index = (uint(pos.x) % 4) * 4 + uint(pos.y) % 4;
            return alpha - DITHER_THRESHOLDS[index];
        }

        // Returns whether the pixel should be discarded based
        // on the dither texture
        // pos is the fragment position in screen space from [0,1]
        float isDithered(float2 pos, float alpha, sampler2D tex, float scale) {
            pos *= _ScreenParams.xy;

            // offset so we're centered
            pos.x -= _ScreenParams.x / 2;
            pos.y -= _ScreenParams.y / 2;

            // scale the texture
            pos.x /= scale;
            pos.y /= scale;

            // ensure that we clip if the alpha is zero by
            // subtracting a small value when alpha == 0, because
            // the clip function only clips when < 0
            return alpha - tex2D(tex, pos.xy).r - 0.0001 * (1 - ceil(alpha));
        }

        // Helpers that call the above functions and clip if necessary
        void ditherClip(float2 pos, float alpha) {
            clip(isDithered(pos, alpha));
        }

        fixed4 _Color;

        fixed4 frag(v2f i) : SV_Target
        {
            float4 red = float4(1, 0.2f, 0.2f, 1);
            fixed4 col = _Color * i.diffuse;
            if (dataBuffer[i.instanceID].textureIndex == 1)
            {
                col *= red;
            }
            ditherClip(i.spos.xy / i.spos.w, col.a);
            return col;
        }

        ENDCG
    }
    }
}