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
using RGS.Configurations.Root;
using RGS.Models;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
        
    [BurstCompile(CompileSynchronously = true)]
    public struct CalculateRootParameterScalingJob : IJobParallelFor
    {
        [ReadOnly]
        public ScalingFunctionType ScalingFunctionType;
        [ReadOnly]
        public NativeArray<RootSGAgentData> AgentsReadOnly;
        [ReadOnly]
        public NativeArray<int2> ScalingFunctionParameterStartLength;
        [ReadOnly]
        public NativeArray<RootSGScalingFunctionParameter> ScalingFunctionParameterDataLength;
        [ReadOnly]
        public NativeArray<int2> PointsDataStartLength;
        [ReadOnly]
        public NativeArray<LeafBody> PointsInRadius;
        [NativeDisableParallelForRestriction] 
        public NativeArray<RootSGScalingFactors> AgentsRootScalingFactors;
        public void Execute (int index)
        {
            int agentTypeIndex = AgentsReadOnly[index].AgentType;
            AgentsRootScalingFactors[index] = AgentsRootScalingFactors[index].SetScalingFactor(1.0f, ScalingFunctionType);
            if (ScalingFunctionParameterStartLength[agentTypeIndex].y == 0) return;
            int startIndex = ScalingFunctionParameterStartLength[agentTypeIndex].x;
            float averageScalingFactor = 0.0f;
            for (int i = 0; i < ScalingFunctionParameterStartLength[agentTypeIndex].y; i++)
            {
                int pointTypeIndex = ScalingFunctionParameterDataLength[startIndex + i].EnvironmentPointType;
                float value = 0.0f;
                int counter = 0;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    if (point.PointType == pointTypeIndex)
                    {
                        value += point.NormSize.z;
                        counter++;
                    }
                }
                value /= counter;
                if (value < ScalingFunctionParameterDataLength[startIndex + i].MinimumConcentration || counter == 0)
                {
                    averageScalingFactor += 1.0f;
                }
                else
                {
                    // f(x) = m(x - c_min) + 1.0f //m = slope, c_min = minimum concentration
                    averageScalingFactor += math.clamp(ScalingFunctionParameterDataLength[startIndex + i].Slope * (value - ScalingFunctionParameterDataLength[startIndex + i].MinimumConcentration) + 1.0f, ScalingFunctionParameterDataLength[startIndex + i].MinimumFactor, ScalingFunctionParameterDataLength[startIndex + i].MaximumFactor);
                }
            }
            averageScalingFactor /= ScalingFunctionParameterStartLength[agentTypeIndex].y;
            AgentsRootScalingFactors[index] = AgentsRootScalingFactors[index].SetScalingFactor(averageScalingFactor, ScalingFunctionType);
        }
    }

}
