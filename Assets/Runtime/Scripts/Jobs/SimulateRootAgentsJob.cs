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
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct SimulateRootAgentsJob : IJobParallelFor
    {
        // collision
        [ReadOnly]
        public NativeArray<float> AgentsRadius;
        [ReadOnly]
        public int CollisionPointId;
        [ReadOnly]
        public NativeArray<int2> PointsDataStartLength;
        [ReadOnly]
        public NativeArray<LeafBody> PointsInRadius;
        //
        [ReadOnly]
        public float DeltaTime;
        [ReadOnly]
        public NativeArray<float> AgentsVelocityLimits;
        [ReadOnly]
        public NativeArray<RootSGAgentData> AgentsReadOnly;
        [WriteOnly]
        public NativeArray<RootSGAgentData> AgentsWriteOnly;
        [ReadOnly]
        public NativeArray<RootSGScalingFactors> AgentsRootScalingFactors;
        public void Execute (int index)
        {
            float agentRadius = AgentsRadius[AgentsReadOnly[index].AgentType];
            float oldVelocityScale = IsInCollision(AgentsReadOnly[index].Position + (AgentsReadOnly[index].Velocity * DeltaTime), index, agentRadius);
            // v_new = v_old + a * dt
            float3 newVelocity = (AgentsReadOnly[index].Velocity * oldVelocityScale) + (AgentsReadOnly[index].Acceleration * DeltaTime);
            // clamp velocity
            float minVelocity = AgentsVelocityLimits[AgentsReadOnly[index].AgentType * 2];
            float maxVelocity = AgentsVelocityLimits[AgentsReadOnly[index].AgentType * 2 + 1];
            float magnitude = math.clamp(math.length(newVelocity), minVelocity, (maxVelocity - minVelocity) * AgentsRootScalingFactors[index].ElongationScale + minVelocity );
            newVelocity = math.normalizesafe(newVelocity) * magnitude;
            // pos_new = pos_old + v_new * dt
            float3 nextPosition = AgentsReadOnly[index].Position + (newVelocity * DeltaTime);
            float deltaDistance = math.distance(nextPosition, AgentsReadOnly[index].Position);
            // Update Root Agent data
            AgentsWriteOnly[index] = AgentsReadOnly[index].MoveAgent(nextPosition, newVelocity, deltaDistance);
        }
        private float IsInCollision(float3 position, int index, float agentRadius)
        {
            for (int i = 0; i < PointsDataStartLength[index].y; i++)
            {
                LeafBody point = PointsInRadius[PointsDataStartLength[index].x + i];
                if (point.PointType == CollisionPointId && math.distance(point.PosCol.xyz, position) <= point.NormSize.y + agentRadius) {
                    return 0.5f;
                }
            }
            return 1.0f;
        }
    }

}
