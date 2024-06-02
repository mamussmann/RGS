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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace RGS.Agents
{

    [StructLayout(LayoutKind.Sequential)]
    public struct WaterAgentData 
    {
        public float3 Position;
        public float3 Velocity;
        public float3 Acceleration;
        public float WaterContent;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaterAgentData SetAcceleration(float3 acceleration)
        {
            this.Acceleration = acceleration;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaterAgentData Move(float3 nextPosition, float3 newVelocity)
        {
            this.Position = nextPosition;
            this.Velocity = newVelocity;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaterAgentData SetWater(float value)
        {
            this.WaterContent = value;
            return this;
        }
    }
    
}
