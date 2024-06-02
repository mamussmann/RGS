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
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Models;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Simulation
{
    public struct RootSGAgentTypeSOA
    {
        public NativeArray<float> AgentsWeights;
        public NativeArray<float> AgentsRadius;
        public NativeArray<float> AgentsSoundDetectRadius;
        public NativeArray<float> AgentsGSA;
        public NativeArray<float> AgentsVelocityLimits;
        public NativeArray<float> AgentsAccelerationLimits;
        public NativeArray<int> AgentsNumberOfTries;
        public NativeArray<AgentTypePerceptionData> AgentsPerceptionData;
        public NativeArray<float3> AgentColors;
        public NativeArray<float> AgentsEncodedColors;
        public NativeList<LeafBody> OutputPoints;
        public UnevenSequentialDataArray<int2, RootAgentRequiredResource>  AgentsRequiredResources;
        public NativeArray<float> AgentsSumOfRequiredResources;
        public RootSGAgentTypeSOA LoadData(RootSGConfiguration configuration)
        {
            AgentsWeights = new NativeArray<float>(configuration.RootSGAgents.Length * RootSGAgent.WEIGHTS_COUNT, Allocator.Persistent);
            AgentColors = new NativeArray<float3>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsVelocityLimits = new NativeArray<float>(configuration.RootSGAgents.Length * 2, Allocator.Persistent);
            AgentsAccelerationLimits = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsNumberOfTries = new NativeArray<int>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsPerceptionData = new NativeArray<AgentTypePerceptionData>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsGSA = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsRadius = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsSoundDetectRadius = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsEncodedColors = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            AgentsSumOfRequiredResources = new NativeArray<float>(configuration.RootSGAgents.Length, Allocator.Persistent);
            OutputPoints = new NativeList<LeafBody>(256, Allocator.Persistent);
            int agentsRequiredResourcesSum = 0;
            foreach (var agentType in configuration.RootSGAgents)
            {
                agentsRequiredResourcesSum += agentType.RequiredResources.Length;   
            }
            AgentsRequiredResources = new UnevenSequentialDataArray<int2, RootAgentRequiredResource>(configuration.RootSGAgents.Length, agentsRequiredResourcesSum);
            int index = 0;
            for (int i = 0; i < configuration.RootSGAgents.Length; i++)
            {
                AgentsRequiredResources.DataStartIndexArray[i] = new int2(index, configuration.RootSGAgents[i].RequiredResources.Length);
                float requiredSum = 0.0f;
                for (int j = 0; j < configuration.RootSGAgents[i].RequiredResources.Length; j++)
                {
                    AgentsRequiredResources.SequentialDataArray[index + j] = configuration.RootSGAgents[i].RequiredResources[j];
                    requiredSum += configuration.RootSGAgents[i].RequiredResources[j].RequiredAmount;
                }
                AgentsSumOfRequiredResources[i] = requiredSum;
                index += configuration.RootSGAgents[i].RequiredResources.Length;
            }
            for (int i = 0; i < configuration.RootSGAgents.Length; i++)
            {
                RootSGAgent rootAgent = configuration.RootSGAgents[i];
                AgentColors[i] = rootAgent.Color;
                AgentsEncodedColors[i] = PointColorEncoding.EncodeColorFloat(rootAgent.Color);
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT] = Mathf.Deg2Rad * rootAgent.PerceptionAngle;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_GRAVITROPISM_OFFSET] = rootAgent.GravitropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_HALOTROPISM_OFFSET] = rootAgent.HalotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_CHEMOTROPISM_OFFSET] = rootAgent.ChemotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_MAGNETOTROPISM_OFFSET] = rootAgent.MagnetotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_THIGMOTROPISM_OFFSET] = rootAgent.ThigmotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_HYDROTROPISM_OFFSET] = rootAgent.HydrotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_PHONOTROPISM_OFFSET] = rootAgent.PhonotropismWeight;
                AgentsWeights[i * RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_SEPARATION_OFFSET] = rootAgent.SeparationWeight;
                AgentsRadius[i] = rootAgent.Radius;
                AgentsSoundDetectRadius[i] = rootAgent.SoundDetectionRadius;
                AgentsVelocityLimits[i * 2] = rootAgent.MinimumVelocity;
                AgentsVelocityLimits[i * 2 + 1] = rootAgent.MaximumVelocity;
                AgentsAccelerationLimits[i] = rootAgent.MaximumAcceleration;
                AgentsNumberOfTries[i] = rootAgent.NumberOfTries;
                AgentsPerceptionData[i] = new AgentTypePerceptionData(math.radians(rootAgent.PerceptionAngle), math.radians(rootAgent.DeltaPerceptionAngle), rootAgent.NumberOfTries);
                AgentsGSA[i] = math.radians(rootAgent.GravitySetPointAngle);
            }
            return this;
        }

        public void Dispose()
        {
            AgentsSumOfRequiredResources.Dispose();
            AgentsWeights.Dispose();
            AgentColors.Dispose();
            AgentsEncodedColors.Dispose();
            OutputPoints.Dispose();
            AgentsVelocityLimits.Dispose();
            AgentsAccelerationLimits.Dispose();
            AgentsNumberOfTries.Dispose();
            AgentsPerceptionData.Dispose();
            AgentsRadius.Dispose();
            AgentsSoundDetectRadius.Dispose();
            AgentsGSA.Dispose();
            AgentsRequiredResources.Dispose();
        }
    }

}
