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
using RGS.Agents;
using RGS.Configurations;
using RGS.Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RGS.QueryJobScheduler
{
    public class PointWaterScheduler
    {
        private PointCloudTree m_tree;
        private NativeList<JobHandle> m_jobHandles;
        private int m_soilPointId;
        private float m_simulationTimeStep;
        private float m_waterEvaporationPerTimeStep;
        private float m_waterAgentSquareRadius;
        private float m_waterLossPerTimeStep;
        public PointWaterScheduler(PointCloudTree tree, int soilPointId, WaterAgentConfig waterAgentConfig)
        {
            m_tree = tree;
            m_soilPointId = soilPointId;
            m_jobHandles = new NativeList<JobHandle>(128, Allocator.Persistent);
            m_simulationTimeStep = RGSConfiguration.Get().SimulationTimestep;
            m_waterEvaporationPerTimeStep = RGSConfiguration.Get().WaterEvaporationPerTimeStep;
            m_waterAgentSquareRadius = waterAgentConfig.WaterLossRadius * waterAgentConfig.WaterLossRadius;
            m_waterLossPerTimeStep = waterAgentConfig.WaterLossPerTimeStep;
        }
        public void SchedulePointWaterUpdateJobs(NativeArray<WaterAgentData> waterAgentsReadonly)
        {
            var blockData = m_tree.GetBlockTreeDataList();
            for (int i = 0; i < blockData.Count; i++)
            {
                var leafDataArray = blockData[i].GetLeafDataArray();

                var job = new PointWaterUpdateJob()
                {
                    WaterAgentsReadonly = waterAgentsReadonly,
                    SimulationTimeStep = m_simulationTimeStep,
                    WaterLossPerTimeStep = m_waterLossPerTimeStep,
                    WaterEvaporationPerTimeStep = m_waterEvaporationPerTimeStep,
                    WaterAgentSquareRadius = m_waterAgentSquareRadius,
                    SoilPointType = m_soilPointId,
                    LeafHeaders = leafDataArray.ExposeHeaderNativeList().AsArray(),
                    LeafBodies = leafDataArray.ExposeBodyNativeList().AsArray()
                };
                m_jobHandles.Add(job.Schedule(leafDataArray.ExposeHeaderNativeList().Length, 16));
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
        }
    }

}
