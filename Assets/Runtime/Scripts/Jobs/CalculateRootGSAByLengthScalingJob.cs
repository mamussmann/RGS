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
    public struct CalculateRootGSAByLengthScalingJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<RootSGAgentData> AgentsReadOnly;
        [NativeDisableParallelForRestriction] 
        public NativeArray<RootDistanceBasedScalingFunctionParameters> GSAByLengthScalingFactors;
        [NativeDisableParallelForRestriction] 
        public NativeArray<RootSGScalingFactors> AgentsRootScalingFactors;
        public void Execute (int index)
        {
            int agentTypeIndex = AgentsReadOnly[index].AgentType;
            // f(x) = m(x - c_min) + 1.0f //m = slope, c_min = minimum concentration
            float gsaScale = math.clamp(GSAByLengthScalingFactors[agentTypeIndex].Slope * (AgentsReadOnly[index].TravelDistance - GSAByLengthScalingFactors[agentTypeIndex].MinimumLength) + 1.0f, GSAByLengthScalingFactors[agentTypeIndex].MinimumFactor, GSAByLengthScalingFactors[agentTypeIndex].MaximumFactor);
            AgentsRootScalingFactors[index] = AgentsRootScalingFactors[index].SetScalingFactor(gsaScale, ScalingFunctionType.GSA_ANGLE);
        }
    }

}
