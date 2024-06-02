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
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace PCMTool.Tree.Jobs
{    
    /// <summary>
    /// Burst compiled job copying all points from one leaf to 
    /// the eight sub leafs based on their position in 3D space.
    /// Used for leaf data within the same block/native list.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct LocalCopyLeafToSubLeafsJob : IJob
    {
        [ReadOnly]
        public float3 Min;
        [ReadOnly]
        public float3 Max;
        [ReadOnly]
        public int InputLeafIndex;
        public NativeArray<int> FreeLeafIndicesAndHeaders; // 0-7 freeLeafIndices, 8-15 headers
        public NativeList<LeafBody> Bodies;
        public void Execute()
        {
            
            for (int j = 8; j < 16; j++)
            {
                FreeLeafIndicesAndHeaders[j] = 0;
            }
            float3 half = (Max - Min) * 0.5f;
            float3 diff = half;
            for (int i = 0; i < DataConstants.TREE_BLOCK_LEAF_CAPACITY; i++)
            {
                diff = half;
                float4 point = Bodies[InputLeafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i].PosCol;
                float3 normSize = Bodies[InputLeafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i].NormSize;
                int pointType = Bodies[InputLeafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i].PointType;
                for (int j = 0; j < 8; j++)
                {
                    diff = half * DataConstants.F_MASK[j];
                    float3 smin = Min + diff;
                    float3 smax = Min + diff + half;
                    if(point.x > smin.x && point.x <= smax.x && 
                        point.y > smin.y && point.y <= smax.y && 
                        point.z > smin.z && point.z <= smax.z){
                            Bodies[(FreeLeafIndicesAndHeaders[j]*DataConstants.TREE_BLOCK_LEAF_CAPACITY)+FreeLeafIndicesAndHeaders[8+j]] = new LeafBody(){
                                PosCol = point,
                                NormSize = normSize,
                                BufferIndex = -1,
                                ModifyFlag = 1,
                                PointType = pointType
                            };
                            
                        FreeLeafIndicesAndHeaders[8+j] = FreeLeafIndicesAndHeaders[8+j] + 1;
                    }
                }
            }

            for (int j = 0; j < 8; j++)
            {
                if(FreeLeafIndicesAndHeaders[8+j] == 0)
                {
                    FreeLeafIndicesAndHeaders[j] = -1;
                }
            }
        }
    }
}