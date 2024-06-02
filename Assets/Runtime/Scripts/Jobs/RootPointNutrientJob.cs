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
using PCMTool.Tree;
using RGS.Models;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    /// <summary>
    /// Burst compiled job 
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct RootPointNutrientJob : IJob
    {
        [ReadOnly]
        public float Rate;
        [ReadOnly]
        public int NutrientPointType;
        [ReadOnly]
        public float Radius;
        [NativeDisableContainerSafetyRestriction]
        public NativeSlice<float> AbsorbedNutients;
        [ReadOnly]
        public NativeList<int> ModifiedLeafs;
        [ReadOnly]
        public NativeList<LeafHead> LeafHeaders;
        
        public NativeList<LeafBody> LeafBodies;
        [ReadOnly]
        public  NativeList<RootPointData> RootPoints;
        public void Execute()
        {
            for (int i = 0; i < ModifiedLeafs.Length; i++)
            {
                int index = ModifiedLeafs[i];
                for (int j = 0; j < LeafHeaders[index].UsedLength; j++)
                {
                    int leafIndex = index * DataConstants.TREE_BLOCK_LEAF_CAPACITY + j;
                    if (LeafBodies[leafIndex].PointType == NutrientPointType && LeafBodies[leafIndex].NormSize.z > Rate)
                    {
                        for (int k = 0; k < RootPoints.Length; k++)
                        {
                            if(math.distancesq(RootPoints[k].Position.xyz, LeafBodies[leafIndex].PosCol.xyz) <= Radius) {
                                AbsorbedNutients[0] += Rate;
                                LeafBodies[leafIndex] = new LeafBody()
                                {
                                    PosCol = LeafBodies[leafIndex].PosCol,
                                    NormSize = new float3(LeafBodies[leafIndex].NormSize.xy, LeafBodies[leafIndex].NormSize.z - Rate),
                                    BufferIndex = LeafBodies[leafIndex].BufferIndex,
                                    ModifyFlag = 2,
                                    PointType = LeafBodies[leafIndex].PointType
                                };
                            }
                        }
                    }
                }
            }
        }

    }
}
