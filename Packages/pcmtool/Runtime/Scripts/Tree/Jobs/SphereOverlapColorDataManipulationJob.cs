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
    /// Burst compiled job overriding the color of given points overlapping the specified Sphere.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct SphereOverlapColorDataManipulationJob : IJob
    {
        [ReadOnly]
        public NativeList<int> ModifiedLeafs;
        public NativeList<LeafHead> LeafHeaders;
        public NativeList<LeafBody> LeafBodies;
        [ReadOnly]
        public float4 OriginRadius;
        [ReadOnly]
        public float EncodedColor;
        public void Execute()
        {
            for (int i = 0; i < ModifiedLeafs.Length; i++)
            {
                int index = ModifiedLeafs[i];
                for (int j = 0; j < LeafHeaders[index].UsedLength; j++)
                {
                    int leafIndex = index * DataConstants.TREE_BLOCK_LEAF_CAPACITY + j;
                    if(math.distance(OriginRadius.xyz, LeafBodies[leafIndex].PosCol.xyz) <= OriginRadius.w) {
                        LeafBodies[leafIndex] = new LeafBody()
                        {
                            PosCol = new float4(LeafBodies[leafIndex].PosCol.xyz, EncodedColor),
                            NormSize = LeafBodies[leafIndex].NormSize,
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
