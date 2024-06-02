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
    /// <summary>
    /// Burst compiled job that simulates boids that place points on their path
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct SimpleSGJob : IJob
    {
        [ReadOnly]
        public float SeparationDistance;
        [ReadOnly]
        public float CohesionDistance;
        [ReadOnly]
        public float Delta;
        [ReadOnly]
        public float4 Center;
        [ReadOnly]
        public NativeArray<float> Weights;
        [ReadOnly]
        public NativeList<SimpleSGAgent> AgentsReadOnly;
        [WriteOnly]
        public NativeList<SimpleSGAgent> AgentsWriteOnly;
        public void Execute()
        {
            float sqSeprationDist = SeparationDistance * SeparationDistance;
            for (int i = 0; i < AgentsReadOnly.Length; i++)
            {
                float phototropismWeight = Weights[AgentsReadOnly[i].AgentType * 3];
                float gravityWeight = Weights[AgentsReadOnly[i].AgentType * 3 + 1];
                float randomWeight = Weights[AgentsReadOnly[i].AgentType * 3 + 2];

                float4 vAccumulated = AgentsReadOnly[i].RandomDirection * randomWeight;
                vAccumulated += math.normalize(Center - AgentsReadOnly[i].Position) * phototropismWeight;
                vAccumulated += new float4(0,-1,0,0) * gravityWeight;
                //vAccumulated += AgentsReadOnly[i].Direction * 0.2f;
                vAccumulated = math.normalize(vAccumulated);
                AgentsWriteOnly[i] = new SimpleSGAgent(
                    AgentsReadOnly[i].Position + (vAccumulated * Delta), 
                    vAccumulated, 
                    AgentsReadOnly[i].Position, 
                    AgentsReadOnly[i].RandomDirection,
                    AgentsReadOnly[i].AgentType,
                    AgentsReadOnly[i].TravelDistance
                    );
            }
        }
    }
}

