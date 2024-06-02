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
using RGS.Models;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{

    [BurstCompile(CompileSynchronously = true)]
    public struct PointPlantUpdateJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction] 
        public NativeArray<NutrientRootPointData> PlantRootPoints;
        [NativeDisableContainerSafetyRestriction] 
        public NativeArray<float> NutrientCapacity;
        [NativeDisableContainerSafetyRestriction] 
        public NativeArray<float> NutrientBalanceValues;
        [ReadOnly]
        public NativeArray<int> NutrientIndexMapping;
        [ReadOnly]
        public int SoilPointType;
        [ReadOnly]
        public float3 PlantBoundsMin;
        [ReadOnly]
        public float3 PlantBoundsMax;
        [ReadOnly]
        public float SimulationTimeStep;
        [ReadOnly]
        public float WaterAbsorbPerTimeStep;
        [ReadOnly]
        public float WaterUsagePerTimeStep;
        [ReadOnly]
        public float NutrientAbsorbPerTimeStep;
        [ReadOnly]
        public int ModifiedLeafsOffsetIndex;
        [NativeDisableContainerSafetyRestriction]
        public NativeList<int> ModifiedLeafs;
        [ReadOnly]
        public NativeArray<int> ExcludedPoints;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<LeafHead> LeafHeaders;
        [NativeDisableContainerSafetyRestriction] 
        public NativeArray<LeafBody> LeafBodies;
        public unsafe void Execute(int index)
        {
            float3 pointPosition;
            float dist = 0.0f;
            int leafIndex = 0;
            int leafHeaderIndex = ModifiedLeafs[ModifiedLeafsOffsetIndex + index];
            for (int i = 0; i < LeafHeaders[leafHeaderIndex].UsedLength; i++)
            {
                // bounds check
                leafIndex = leafHeaderIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i;
                pointPosition = LeafBodies[leafIndex].PosCol.xyz;
                if(
                    PlantBoundsMin.x < pointPosition.x && PlantBoundsMin.y < pointPosition.y && PlantBoundsMin.z < pointPosition.z && 
                    PlantBoundsMax.x > pointPosition.x && PlantBoundsMax.y > pointPosition.y && PlantBoundsMax.z > pointPosition.z )
                {
                    for (int k = 0; k < ExcludedPoints.Length; k++)
                    {
                        if(LeafBodies[leafIndex].PointType == ExcludedPoints[k]){
                            leafIndex = -1;
                            break;
                        }
                    }
                    
                    if(leafIndex == -1) continue;
                    int nutrientMappingIndex = NutrientIndexMapping[LeafBodies[leafIndex].PointType];
                    if(nutrientMappingIndex == -1 && LeafBodies[leafIndex].PointType != SoilPointType) continue;

                    for (int j = 0; j < PlantRootPoints.Length; j++)
                    {
                        dist = math.distancesq(PlantRootPoints[j].Position, LeafBodies[leafIndex].PosCol.xyz);
                        if (dist <= PlantRootPoints[j].SquareRadius)
                        {
                            if(LeafBodies[leafIndex].PointType == SoilPointType && LeafBodies[leafIndex].NormSize.z > (WaterAbsorbPerTimeStep * SimulationTimeStep) ){
                                Thread.MemoryBarrier();
                                PlantRootPoints[j] = PlantRootPoints[j].AddWater(WaterAbsorbPerTimeStep * SimulationTimeStep);
                                LeafBodies[leafIndex] = LeafBodies[leafIndex].SetValue(math.clamp(LeafBodies[leafIndex].NormSize.z - (WaterAbsorbPerTimeStep * SimulationTimeStep), 0.0f, 1.0f));
                                Thread.MemoryBarrier();
                                break;
                            }else if(PlantRootPoints[j].WaterContent >= WaterUsagePerTimeStep * SimulationTimeStep && LeafBodies[leafIndex].NormSize.z > (NutrientAbsorbPerTimeStep * SimulationTimeStep) && 
                                    NutrientBalanceValues[nutrientMappingIndex] < NutrientCapacity[nutrientMappingIndex]) {
                                Thread.MemoryBarrier();
                                PlantRootPoints[j] = PlantRootPoints[j].RemoveWater(WaterUsagePerTimeStep * SimulationTimeStep);
                                NutrientBalanceValues[nutrientMappingIndex] = NutrientBalanceValues[nutrientMappingIndex] + (NutrientAbsorbPerTimeStep * SimulationTimeStep);
                                LeafBodies[leafIndex] = LeafBodies[leafIndex].SetValue(math.clamp(LeafBodies[leafIndex].NormSize.z - (NutrientAbsorbPerTimeStep * SimulationTimeStep), 0.0f, 1.0f));
                                Thread.MemoryBarrier();
                                break;
                            }
                        }
                    }
                }
            }
        }

    }

}