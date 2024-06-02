/* 
* Copyright (c) 2024 Marc Mußmann
*
* Permission is hereby granted, free of charge, to any person obtaining a copy of 
* this software and associated documentation files (the "Software"), to deal in the 
* Software without restriction, including without limitation the rights to use, copy, 
* modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
* and to permit persons to whom the Software is furnished to do so, subject to the 
* following conditions:
*
* The above copyright notice and this permission notice shall be included in all 
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
* INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
* PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
* FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
* DEALINGS IN THE SOFTWARE.
*/
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace PCMTool.Tree
{    
    /// <summary>
    /// Struct containing the data of a point in the same layout that is assumed by the leaf shader.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PointData
    {
        /// <summary>
        /// Position of the point in 3D space.
        /// </summary>
        public float3 Position => PosCol.xyz;
        /// <summary>
        /// A float4 containing the 3D position of the point as well as the encoded color information.
        /// (xyz = position, w = color)
        /// </summary>
        [FieldOffset(0)] public float4 PosCol;
        /// <summary>
        /// A float2 containing the encoded normal information (currently not used) and the points' size.
        /// (x = normal, y = point Size)
        /// </summary>
        [FieldOffset(16)] public float3 NormSize;
        [FieldOffset(28)] public int PointType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(float4 positionColor, float3 normalSize, int pointType)
        {
            this.PosCol = positionColor;
            this.NormSize = normalSize;
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(float3 position, float color, float pointScale, int pointType)
        {
            this.PosCol = new float4(position, color);
            this.NormSize = new float3(0.0f, pointScale, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Color color, int pointType)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, 1.0f, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Vector3 color, int pointType)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, 1.0f, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(float3 position, float3 color, int pointType)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, 1.0f, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Color color, float pointScale, int pointType)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, pointScale, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Color color, float pointScale, int pointType, float amount)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, pointScale, amount);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Vector3 color, float pointScale, int pointType, float amount)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, pointScale, amount);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(Vector3 position, Vector3 color, float pointScale, int pointType)
        {
            this.PosCol = new float4(position, PointColorEncoding.EncodeColorFloat(color));
            this.NormSize = new float3(0.0f, pointScale, 0.0f);
            this.PointType = pointType;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(float x, float y, float z, float r, float g, float b)
        {
            this.PosCol = new float4(x,y,z, PointColorEncoding.EncodeColorFloat(new Color(r,g,b)));
            this.NormSize = new float3(0.0f, 1.0f, 0.0f);
            this.PointType = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PointData(float x, float y, float z)
        {
            this.PosCol = new float4(x,y,z, 0.0f);
            this.NormSize = new float3(0.0f, 1.0f, 0.0f);
            this.PointType = 0;
        }
    }

}
