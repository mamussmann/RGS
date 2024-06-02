// 
// Copyright (c) 2024 Marc Mußmann
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

// This file incorporates work covered by the following copyright and  
// permission notice:  
// https://github.com/keijiro/Pcx
// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
// 
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// 
// For more information, please refer to <http://unlicense.org/>


Shader "RGS/RootSelectPoint"
{    
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Back
        ZWrite On
        Pass 
        {
            Name "RootSelectPointPass"
            CGPROGRAM

            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment

            #pragma multi_compile_fog
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _QUAD_GEOMETRY

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float3 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_Position;
                float3 color : COLOR;
                float pointSize : TEXCOORD0;
            };

            struct PointData
            {
                float3 pos;
                float3 idTimeSize;
            };

            StructuredBuffer<PointData> _rootPointBuffer;

            Varyings Vertex(uint vid : SV_VertexID)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(_rootPointBuffer[vid].pos);
                o.color = float3(_rootPointBuffer[vid].idTimeSize.x, _rootPointBuffer[vid].idTimeSize.y, 0.0);
                o.pointSize = max(_rootPointBuffer[vid].idTimeSize.z * 2.0f, 0.001);
                return o;
            }

            // Geometry phase
            [maxvertexcount(36)]
            void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream)
            {
                float4 origin = input[0].position;
                float2 extent = abs(UNITY_MATRIX_P._11_22 * input[0].pointSize);

                Varyings o = input[0];

                float radius = extent.y / origin.w * _ScreenParams.y;
                uint slices = min((radius + 1) / 5, 4) + 2;

                if (slices == 2) extent *= 1.2;

                o.position.y = origin.y + extent.y;
                o.position.xzw = origin.xzw;
                outStream.Append(o);

                float sn, cs;
                UNITY_LOOP for (uint i = 1; i < slices; i++)
                {
                    sincos(UNITY_PI / slices * i, sn, cs);

                    float2 sExtend = extent * float2(sn, cs);
                    // Left side vertex
                    o.position.x = origin.x - sExtend.x;
                    o.position.y = origin.y + sExtend.y;
                    outStream.Append(o);
                    // Right side vertex
                    o.position.xy = origin.xy + sExtend;
                    outStream.Append(o);

                }

                // Bottom vertex
                o.position.x = origin.x;
                o.position.y = origin.y - extent.y;
                outStream.Append(o);

                outStream.RestartStrip();
            }

            float Fragment(Varyings input) : SV_Target
            {
                return input.color.x;
            }
            ENDCG
        }
    }
}
