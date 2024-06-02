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

using RGS.Agents;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{

    [BurstCompile(CompileSynchronously = true)]
    public struct CreateWaterAgentsInPlaneJob : IJob
    {
        [ReadOnly]
        public float InitalVelocity;
        [ReadOnly]
        public float InitalWaterValue;
        [ReadOnly]
        public int Count;
        [ReadOnly]
        public NativeArray<float> PrecomputedRandomValues;
        [ReadOnly]
        public int Offset;
        [ReadOnly]
        public float Width;
        [ReadOnly]
        public float Depth;
        [ReadOnly]
        public float3 Center;
        [WriteOnly]
        public NativeList<WaterAgentData> WaterAgents;
        public void Execute()
        {
            int index = Offset;
            for (int i = 0; i < Count; i++)
            {
                index = (index + i) % PrecomputedRandomValues.Length;
                float rndX = PrecomputedRandomValues[index] * Width;
                index = (index + i + 1) % PrecomputedRandomValues.Length;
                float rndZ = PrecomputedRandomValues[index] * Depth;
                
                WaterAgents.Add(new WaterAgentData()
                {
                    Position = Center + (new float3(1,0,0) * rndX) + (new float3(0,0,1) * rndZ),
                    Velocity = new float3(0.0f, -InitalVelocity, 0.0f),
                    Acceleration = float3.zero,
                    WaterContent = InitalWaterValue
                });
            }
        }
    }

}
