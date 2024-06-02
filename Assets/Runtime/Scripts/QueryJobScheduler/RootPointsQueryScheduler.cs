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
using System.Collections.Generic;
using PCMTool.Tree;
using RGS.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.QueryJobScheduler
{
    /// <summary>
    /// Struct used for scheduling multiple jobs running in parallel
    /// to get all points in a radius.
    /// </summary>
    public struct RootPointsQueryScheduler
    {
        public PointCloudTree Tree;
        public NativeArray<float4> SchedulePointQueryJobs()
        {
            var blockData = Tree.GetBlockTreeDataList();
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(Tree.ModifiedBlocks.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            List<NativeList<float4>> rootPointsLists = new List<NativeList<float4>>();
            for (int i = 0; i < Tree.ModifiedBlocks.Length; i++)
            {
                rootPointsLists.Add(new NativeList<float4>(Allocator.TempJob));
            }
            
            for (int i = 0; i < Tree.ModifiedBlocks.Length; i++)
            {
                int blockIndex = Tree.ModifiedBlocks[i];
                var leafDataArray = blockData[blockIndex].GetLeafDataArray();

                var job = new RootPointQueryJob()
                {
                    ModifiedLeafs = leafDataArray.GetModifiedLeafsList(),
                    LeafHeaders = leafDataArray.ExposeHeaderNativeList(),
                    LeafBodies = leafDataArray.ExposeBodyNativeList(),
                    OutputRootPointData = rootPointsLists[i]
                };
                job.Schedule().Complete();
                handles[i] = job.Schedule();
            }
            JobHandle.CompleteAll(handles);
            int rootPointCount = 0;
            foreach (var list in rootPointsLists)
            {
                rootPointCount += list.Length;
            }
            
            NativeArray<float4> combination = new NativeArray<float4>(rootPointCount, Allocator.TempJob);

            int index = 0;
            foreach (var list in rootPointsLists)
            {
                foreach (var rootPoint in list)
                {
                    combination[index++] = rootPoint;
                }
                list.Dispose();
            }
            
            handles.Dispose();
            return combination;
        }
    }

}
