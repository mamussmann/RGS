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
    /// Burst compiled job flagging all Sphere overlapping points as to be removed.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct SphereLeafsOverlapRemoveJob : IJob
    {
        [ReadOnly]
        public int LeafCount;
        [ReadOnly]
        public float4 OriginRadius;
        public NativeSlice<LeafBody> LeafBodies;
        public NativeArray<int> OutputLeafCount;
        public void Execute()
        {
            OutputLeafCount[0] = LeafCount;
            for (int i = 0; i < LeafCount; i++)
            {
                if(math.distance(OriginRadius.xyz, LeafBodies[i].PosCol.xyz) <= OriginRadius.w) {
                    LeafBodies[i] = LeafBodies[i].SetModifyFlag(3);
                    OutputLeafCount[0] = OutputLeafCount[0] - 1;
                }
            }
        }
    }

}
