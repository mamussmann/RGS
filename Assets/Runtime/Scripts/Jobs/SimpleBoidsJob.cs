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
using RGS.Agents;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    /// <summary>
    /// Burst compiled job that simulates boids that place points on their path
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct SimpleBoidsJob : IJobParallelFor
    {
        [ReadOnly]
        public float SeparationDistance;
        [ReadOnly]
        public float CohesionDistance;
        [ReadOnly]
        public float PerceptionAngle;
        [ReadOnly]
        public float Delta;
        [ReadOnly]
        public NativeArray<float> Weights;
        [ReadOnly]
        public float4 Center;
        [ReadOnly]
        public NativeArray<SimpleBoidsAgentData> AgentsReadOnly;
        [WriteOnly]
        public NativeArray<SimpleBoidsAgentData> AgentsWriteOnly;
        public void Execute(int i)
        {
            float sqSeprationDist = SeparationDistance * SeparationDistance;
            float4 avgSeparationPoint = new float4();
            int sepCount = 0;
            for (int j = 0; j < AgentsReadOnly.Length; j++)
            {
                if(j == i) continue;
                if(math.distancesq(AgentsReadOnly[i].PosCol, AgentsReadOnly[j].PosCol) < sqSeprationDist)
                {
                    avgSeparationPoint += AgentsReadOnly[j].PosCol;
                    sepCount++;
                }
            }
            if(sepCount > 0){
                avgSeparationPoint = avgSeparationPoint / (float) sepCount;
            }

            float4 avgFlockPosition = new float4();
            float4 avgFlockDirection = new float4();
            int flockCount = 0;
            for (int j = 0; j < AgentsReadOnly.Length; j++)
            {
                if(j == i) continue;
                float4 neighborDirection = math.normalize(AgentsReadOnly[j].PosCol - AgentsReadOnly[i].PosCol);
                float angle = math.acos(math.dot(AgentsReadOnly[i].Direction, neighborDirection));
                if(angle < PerceptionAngle && math.distancesq(AgentsReadOnly[i].PosCol, AgentsReadOnly[j].PosCol) < CohesionDistance)
                {
                    avgFlockPosition += AgentsReadOnly[j].PosCol;
                    avgFlockDirection += AgentsReadOnly[j].Direction;
                    flockCount++;
                }
            }
            avgFlockPosition = avgFlockPosition / (float) flockCount;
            avgFlockDirection = avgFlockDirection / (float) flockCount;
            float4 vacc = AgentsReadOnly[i].RandomDirection * Weights[4];
            vacc += math.normalize(Center - AgentsReadOnly[i].PosCol) * Weights[3];
            if(sepCount > 0){
                vacc += math.normalize(AgentsReadOnly[i].PosCol - avgSeparationPoint) * Weights[0];
            }
            if(flockCount > 0){
                vacc += math.normalize(avgFlockPosition - AgentsReadOnly[i].PosCol) * Weights[1];
                vacc += math.normalize(avgFlockDirection) * Weights[2];
            }
            vacc = math.normalize(vacc);
            AgentsWriteOnly[i] = new SimpleBoidsAgentData(AgentsReadOnly[i].PosCol + (vacc * Delta), vacc, AgentsReadOnly[i].PosCol, AgentsReadOnly[i].Color, AgentsReadOnly[i].RandomDirection);
        }
    }
}

