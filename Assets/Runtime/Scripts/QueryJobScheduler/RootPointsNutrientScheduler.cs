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
using RGS.Configurations;
using RGS.Jobs;
using RGS.Models;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.QueryJobScheduler
{
    /// <summary>
    /// Struct used for scheduling multiple jobs running in parallel
    /// to get all points in a radius.
    /// </summary>
    public struct RootPointsNutrientScheduler
    {
        public PointCloudTree Tree;
        public NativeList<RootPointData> RootPointsList;
        public int NutrientPointType;
        public float ScheduleRootPointsNutrientJobs()
        {
            var blockData = Tree.GetBlockTreeDataList();
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(Tree.ModifiedBlocks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float> absorbedNutients = new NativeArray<float>(Tree.ModifiedBlocks.Length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            
            for (int i = 0; i < Tree.ModifiedBlocks.Length; i++)
            {
                int blockIndex = Tree.ModifiedBlocks[i];
                var leafDataArray = blockData[blockIndex].GetLeafDataArray();

                var job = new RootPointNutrientJob()
                {
                    ModifiedLeafs = leafDataArray.GetModifiedLeafsList(),
                    LeafHeaders = leafDataArray.ExposeHeaderNativeList(),
                    LeafBodies = leafDataArray.ExposeBodyNativeList(),
                    AbsorbedNutients = absorbedNutients.Slice(i, 1),
                    Rate = 0.001f,
                    NutrientPointType = NutrientPointType,
                    Radius = 0.001f,
                    RootPoints = RootPointsList
                };
                handles[i] = job.Schedule();
            }
            JobHandle.CompleteAll(handles);
            handles.Dispose();
            
            float sum = 0.0f;
            for (int i = 0; i < absorbedNutients.Length; i++)
            {
                sum += absorbedNutients[i];
            }
            absorbedNutients.Dispose();
            return sum;
        }
    }

}
