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

Shader "PCM/LeafPoint"
{    
    Properties
    {
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PlaneDirection ("PlaneDirection", Vector) = (1,0,0,0) 
        _PointType("Point Type", Int) = 0
        [Toggle] _ShowAllPoints("Show All points", Float) = 0
        [Toggle] _EnableCullingPlane("Enable Cullingplane", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Back
        ZWrite On
        Pass
        {
            CGPROGRAM

            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment

            #pragma multi_compile_fog
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _QUAD_GEOMETRY

            #include "Common.cginc"
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                half3 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_Position;
                half3 color : COLOR;
                half4 wsPos : COLOR1;
                float pointSize : TEXCOORD0;
                uint pointType : TEXCOORD1;
                float pointValue : TEXCOORD2;
                UNITY_FOG_COORDS(0)
            };

            half4 _Tint;
            half4 _PlaneDirection;
            half4 _PlanePosition;
            float4x4 _Transform;
            half _PointSize;
            int _PointType;
            float _ShowAllPoints;
            float _EnableCullingPlane;

            int _heatmapOverlayPointType;
            float _heatmapOverlayThreshold;

            struct PointData
            {
                float3 pos;
                uint color;
                float3 normSize;
                uint type;
            };

            StructuredBuffer<PointData> _PointBuffer;

            Varyings Vertex(uint vid : SV_VertexID)
            {
                float3 pt = _PointBuffer[vid].pos;
                float4 pos = mul(_Transform, float4(pt, 1));
                //half3 col = _PointBuffer[vid * 2+1].rgb;
                half3 col = PcxDecodeColor(_PointBuffer[vid].color);
                uint type = _PointBuffer[vid].type;
            #ifdef UNITY_COLORSPACE_GAMMA
                col *= _Tint.rgb * 2;
            #else
                col *= LinearToGammaSpace(_Tint.rgb) * 2;
                col = GammaToLinearSpace(col);
            #endif

                Varyings o;
                o.position = UnityObjectToClipPos(pos);
                o.wsPos = mul(unity_ObjectToWorld, float4(pos.xyz, 0));
                o.color = col;
                o.pointSize = _PointBuffer[vid].normSize.y;
                o.pointValue = _PointBuffer[vid].normSize.z;
                o.pointType = type;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }

            // Geometry phase
            [maxvertexcount(36)]
            void Geometry(point Varyings input[1], inout TriangleStream<Varyings> outStream)
            {
#ifdef _QUAD_GEOMETRY
                Varyings o = input[0];
                float4 origin = input[0].position;
                float2 extent = abs(UNITY_MATRIX_P._11_22 * input[0].pointSize );

                o.position.y = origin.y + extent.y;
                o.position.x = origin.x - extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);
                
                o.position.y = origin.y - extent.y;
                o.position.x = origin.x - extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);

                o.position.y = origin.y - extent.y;
                o.position.x = origin.x + extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);

                o.position.y = origin.y + extent.y;
                o.position.x = origin.x - extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);
                
                o.position.y = origin.y + extent.y;
                o.position.x = origin.x + extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);

                o.position.y = origin.y - extent.y;
                o.position.x = origin.x + extent.x;
                o.position.zw = origin.zw;
                outStream.Append(o);
                outStream.RestartStrip();
#else
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
#endif

            }

            half4 Fragment(Varyings input) : SV_Target
            {
                if (input.pointType != _PointType && _EnableCullingPlane > 0.5f && dot(input.wsPos - _PlanePosition, _PlaneDirection) < 0.0) {
                    discard;
                }
                if(_heatmapOverlayPointType != -1)
                {
                    if(input.pointType == _heatmapOverlayPointType && input.pointValue >= _heatmapOverlayThreshold) {
                        return lerp(half4(0,0,1,1), half4(1,0,0,1), input.pointValue);
                    }else {
                        discard;
                    }
                }
                if (input.pointType != _PointType && _ShowAllPoints < 0.5) {
                    discard;
                }
                half4 c = half4(input.color, _Tint.a);
                UNITY_APPLY_FOG(input.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
