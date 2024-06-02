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
using System;
using RGS.Configurations.Root;
using RGS.Models;
using Unity.Collections;
using Unity.Mathematics;

namespace RGS.Simulation
{

    public struct AgentScalingParametersSOA
    {
        public UnevenSequentialDataArray<int2, RootSGScalingFunctionParameter>[] AgentsScalingFactorsArray;
        public NativeArray<RootDistanceBasedScalingFunctionParameters> GSAByLengthScalingFactors;
        public AgentScalingParametersSOA LoadData(RootSGConfiguration configuration)
        {
            AgentsScalingFactorsArray = new UnevenSequentialDataArray<int2, RootSGScalingFunctionParameter>[Enum.GetNames(typeof(ScalingFunctionType)).Length];
            for (int i = 0; i < Enum.GetNames(typeof(ScalingFunctionType)).Length; i++)
            {
                InitScalingParameterArrays(configuration, (ScalingFunctionType)i);
            }
            GSAByLengthScalingFactors = new NativeArray<RootDistanceBasedScalingFunctionParameters>(configuration.RootSGAgents.Length, Allocator.Persistent);
            for (int i = 0; i < configuration.RootSGAgents.Length; i++)
            {
                GSAByLengthScalingFactors[i] = configuration.RootSGAgents[i].GSAScalingByLengthFunction;
            }
            return this;
        }
        public UnevenSequentialDataArray<int2, RootSGScalingFunctionParameter> GetAgentsFactorsArrayByType(ScalingFunctionType scalingFunctionType)
        {
            return AgentsScalingFactorsArray[(int)scalingFunctionType];
        }
        private void InitScalingParameterArrays(RootSGConfiguration configuration, ScalingFunctionType scalingFunctionType)
        {
            int indexArrayLength = configuration.RootSGAgents.Length;
            int dataArrayLength = 0;
            foreach (var rootAgent in configuration.RootSGAgents)
            {
                dataArrayLength += rootAgent.GetScalingFunctionArrayByType(scalingFunctionType).Length;
            }
            AgentsScalingFactorsArray[(int)scalingFunctionType] = new UnevenSequentialDataArray<int2, RootSGScalingFunctionParameter>(indexArrayLength, dataArrayLength);

            dataArrayLength = 0;
            for (int i = 0; i < configuration.RootSGAgents.Length; i++)
            {
                int dataLength = configuration.RootSGAgents[i].GetScalingFunctionArrayByType(scalingFunctionType).Length;
                GetAgentsFactorsArrayByType(scalingFunctionType).DataStartIndexArray[i] = new int2(dataArrayLength, dataLength);
                for (int j = 0; j < dataLength; j++)
                {
                    GetAgentsFactorsArrayByType(scalingFunctionType).SequentialDataArray[dataArrayLength + j] = configuration.RootSGAgents[i].GetScalingFunctionArrayByType(scalingFunctionType)[j];
                }
                dataArrayLength += dataLength;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < AgentsScalingFactorsArray.Length; i++)
            {
                AgentsScalingFactorsArray[i].Dispose();
            }
            GSAByLengthScalingFactors.Dispose();
        }
    }
}
