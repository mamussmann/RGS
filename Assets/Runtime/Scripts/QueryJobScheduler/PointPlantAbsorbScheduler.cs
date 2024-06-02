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
using PCMTool.Tree.Query;
using RGS.Configurations;
using RGS.Jobs;
using RGS.Models;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.QueryJobScheduler
{

    public class PointPlantAbsorbScheduler
    {
        private PointCloudTree m_tree;
        private NativeList<JobHandle> m_jobHandles;
        private int m_soilPointId;
        private float m_simulationTimeStep;
        private NativeArray<int> m_excludedPoints;
        private float m_waterAbsorbPerTimeStep;
        private float m_waterUsagePerTimeStep;
        private float m_nutrientAbsorbPerTimeStep;
        public PointPlantAbsorbScheduler(PointCloudTree tree, int soilPointId, int[] excludedPoints)
        {
            m_tree = tree;
            m_soilPointId = soilPointId;
            m_jobHandles = new NativeList<JobHandle>(128, Allocator.Persistent);
            m_simulationTimeStep = RGSConfiguration.Get().SimulationTimestep * RGSConfiguration.Get().NutrientPointSimStep;
            m_excludedPoints = new NativeArray<int>(excludedPoints, Allocator.Persistent);
            m_waterAbsorbPerTimeStep = RGSConfiguration.Get().WaterAbsorbPerTimeStep;
            m_waterUsagePerTimeStep = RGSConfiguration.Get().WaterUsagePerTimeStep;
            m_nutrientAbsorbPerTimeStep = RGSConfiguration.Get().NutrientAbsorbPerTimeStep;
        }
        public void SchedulePlantsNutrientUpdateJobs(List<PlantSeedModel> plantSeeds)
        {
            var blockData = m_tree.GetBlockTreeDataList();
            ILeafDataArray leafDataArray;
            PointPlantUpdateJob pointPlantUpdateJob;
            float3 min;
            float3 max;
            for (int i = 0; i < plantSeeds.Count; i++)
            {
                if(!plantSeeds[i].SimulateResourceCost) continue;
                min = plantSeeds[i].BoundingBox.min;
                max = plantSeeds[i].BoundingBox.max;
                m_tree.ClearModifiedBlockList();
                m_tree.QueryOverlapPoints(new AABBQueryOveralapVolume(min, max));
                plantSeeds[i].ModifiedBlocksAndLeafs.Clear();
                var modBlockLeafs = plantSeeds[i].ModifiedBlocksAndLeafs;
                
                for (int j = 0; j < m_tree.ModifiedBlocks.Length; j++)
                {
                    int blockIndex = m_tree.ModifiedBlocks[j];
                    leafDataArray = blockData[blockIndex].GetLeafDataArray();
                    for (int k = 0; k < leafDataArray.GetModifiedLeafsList().Length; k++)
                    {
                        modBlockLeafs.Add(leafDataArray.GetModifiedLeafsList()[k]);
                    }
                }

                int leafOffsetIndex = 0;
                for (int j = 0; j < m_tree.ModifiedBlocks.Length; j++)
                {
                    int blockIndex = m_tree.ModifiedBlocks[j];
                    leafDataArray = blockData[blockIndex].GetLeafDataArray();
                    //UnityEngine.Debug.Log("modList length:"+leafDataArray.GetModifiedLeafsList().Length);
                    pointPlantUpdateJob = new PointPlantUpdateJob()
                    {
                        ModifiedLeafsOffsetIndex = leafOffsetIndex,
                        ModifiedLeafs = plantSeeds[i].ModifiedBlocksAndLeafs,
                        PlantRootPoints = plantSeeds[i].NutrientRootPointDataList.AsArray(),
                        NutrientCapacity = plantSeeds[i].NutrientCapacity,
                        NutrientBalanceValues = plantSeeds[i].NutrientBalanceValues,
                        NutrientIndexMapping = plantSeeds[i].NutrientIndexMapping,
                        WaterAbsorbPerTimeStep = m_waterAbsorbPerTimeStep,
                        WaterUsagePerTimeStep = m_waterUsagePerTimeStep,
                        NutrientAbsorbPerTimeStep = m_nutrientAbsorbPerTimeStep,
                        SoilPointType = m_soilPointId,
                        PlantBoundsMin = min,
                        PlantBoundsMax = max,
                        SimulationTimeStep = m_simulationTimeStep,
                        ExcludedPoints = m_excludedPoints,
                        LeafHeaders = leafDataArray.ExposeHeaderNativeList().AsArray(),
                        LeafBodies = leafDataArray.ExposeBodyNativeList().AsArray()
                    };
                    leafOffsetIndex += leafDataArray.GetModifiedLeafsList().Length;
                    m_jobHandles.Add(pointPlantUpdateJob.Schedule(leafDataArray.GetModifiedLeafsList().Length, 8));
                }
            }
            
        }

        public void Complete()
        {
            JobHandle.CompleteAll(m_jobHandles);
            m_jobHandles.Clear();
        }

        public int GetActiveJobCount()
        {
            return m_jobHandles.Length;
        }

        public void Dispose()
        {
            Complete();
            m_jobHandles.Dispose();
            m_excludedPoints.Dispose();
        }
    }

}
