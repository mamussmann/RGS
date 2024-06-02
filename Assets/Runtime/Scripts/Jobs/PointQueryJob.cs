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
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    /// <summary>
    /// Burst compiled job overriding the point size of given points overlapping the specified Sphere.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct PointQueryJob : IJob
    {
        [ReadOnly]
        public NativeList<int> ModifiedLeafs;
        [ReadOnly]
        public NativeList<LeafHead> LeafHeaders;
        [ReadOnly]
        public NativeList<LeafBody> LeafBodies;
        [WriteOnly]
        public NativeList<LeafBody> OutputPointData;
        [ReadOnly]
        public float4 OriginRadius;
        public void Execute()
        {
            for (int i = 0; i < ModifiedLeafs.Length; i++)
            {
                int index = ModifiedLeafs[i];
                for (int j = 0; j < LeafHeaders[index].UsedLength; j++)
                {
                    int leafIndex = index * DataConstants.TREE_BLOCK_LEAF_CAPACITY + j;
                    float dist = math.distancesq(OriginRadius.xyz, LeafBodies[leafIndex].PosCol.xyz);
                    if (dist <= OriginRadius.w)
                    {
                        OutputPointData.Add(LeafBodies[leafIndex]);
                    }
                }
            }
        }

    }
}
