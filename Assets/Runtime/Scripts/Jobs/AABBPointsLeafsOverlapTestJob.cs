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
    /// Burst compiled job checking if any of the given points overlap the given AABB.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct AABBPointsLeafsOverlapTestJob : IJob
    {
        [ReadOnly]
        public int PointType;
        [ReadOnly]
        public float ToleranceRadius;
        [ReadOnly]
        public NativeArray<float3> Points;
        [WriteOnly]
        public NativeArray<int> Output;
        [ReadOnly]
        public int LeafCount;
        [ReadOnly]
        public NativeSlice<LeafBody> LeafBodies;
        [ReadOnly]
        public float3 Min;
        [ReadOnly]
        public float3 Max;
        public void Execute()
        {
            Output[0] = 0;
            for (int i = 0; i < LeafCount; i++)
            {
                if(LeafBodies[i].PointType != PointType) continue;
                float3 point = LeafBodies[i].PosCol.xyz;
                if (point.x > Min.x && point.x <= Max.x &&
                    point.y > Min.y && point.y <= Max.y &&
                    point.z > Min.z && point.z <= Max.z)
                {
                    for (int j = 0; j < Points.Length; j++)
                    {
                        if(math.distance(Points[j], point) <= ToleranceRadius){
                            Output[0] = 1;
                            return;
                        }
                    }
                }
            }
        }
    }
}
