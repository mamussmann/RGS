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

namespace RGS.Agents
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SimpleBoidsAgentData 
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
        [FieldOffset(16)] public float4 Direction;
        [FieldOffset(32)] public float4 LastPosition;
        [FieldOffset(48)] public float4 Color;
        [FieldOffset(64)] public float4 RandomDirection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SimpleBoidsAgentData(float4 positionColor, float4 direction, float4 lastPosition, float4 color, float4 randomDirection)
        {
            this.PosCol = positionColor;
            this.Direction = direction;
            this.LastPosition = lastPosition;
            this.Color = color;
            this.RandomDirection = randomDirection;
        }
    }
}
