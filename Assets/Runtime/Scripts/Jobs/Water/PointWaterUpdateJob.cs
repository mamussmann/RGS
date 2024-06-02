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
using System.Threading;
using PCMTool.Tree;
using RGS.Agents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{

    [BurstCompile(CompileSynchronously = true)]
    public struct PointWaterUpdateJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<WaterAgentData> WaterAgentsReadonly;
        [ReadOnly]
        public float WaterAgentSquareRadius;
        [ReadOnly]
        public float WaterLossPerTimeStep;
        [ReadOnly]
        public float SimulationTimeStep;
        [ReadOnly]
        public float WaterEvaporationPerTimeStep;
        [ReadOnly]
        public int SoilPointType;
        [ReadOnly]
        public NativeArray<LeafHead> LeafHeaders;
        [NativeDisableParallelForRestriction] 
        public NativeArray<LeafBody> LeafBodies;
        public unsafe void Execute(int index)
        {
            float agentWaterLoss = 0.0f;
            for (int i = 0; i < LeafHeaders[index].UsedLength; i++)
            {
                int leafIndex = index * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i;
                if(LeafBodies[leafIndex].PointType == SoilPointType) 
                {
                    float waterDelta = 0.0f;
                    for (int j = 0; j < WaterAgentsReadonly.Length; j++)
                    {
                        float dist = math.distancesq(WaterAgentsReadonly[j].Position, LeafBodies[leafIndex].PosCol.xyz);
                        if (dist <= WaterAgentSquareRadius)
                        {
                            Thread.MemoryBarrier();
                            if(WaterAgentsReadonly[j].WaterContent >= 0) {
                                agentWaterLoss = math.min(WaterLossPerTimeStep * SimulationTimeStep * (1.0f - (dist / WaterAgentSquareRadius)), WaterAgentsReadonly[j].WaterContent);
                                // only remove water if points can hold it
                                if(LeafBodies[leafIndex].NormSize.z + (waterDelta + agentWaterLoss) <= 1.0f) {
                                    waterDelta += agentWaterLoss;
                                    WaterAgentsReadonly[j] = WaterAgentsReadonly[j].SetWater(WaterAgentsReadonly[j].WaterContent - agentWaterLoss);
                                }
                            }
                            Thread.MemoryBarrier();
                        }
                    }
                    waterDelta -= WaterEvaporationPerTimeStep * SimulationTimeStep;
                    Thread.MemoryBarrier();
                    LeafBodies[leafIndex] = LeafBodies[leafIndex].SetValue(math.clamp(LeafBodies[leafIndex].NormSize.z + waterDelta, 0.0f, 1.0f));
                    Thread.MemoryBarrier();
                }
            }
        }

    }

}