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
    public struct SimulateWaterAgentsJob : IJobParallelFor
    {
        [ReadOnly]
        public float AgentRadius;
        [ReadOnly]
        public float DeltaTime;
        [ReadOnly]
        public float MaxVelocity;
        [ReadOnly]
        public float Acceleration;
        [ReadOnly]
        public int NumberOfTries;
        [ReadOnly]
        public int CollisionPointId;
        
        public NativeArray<WaterAgentData> AgentsReadOnly;
        [WriteOnly]
        public NativeArray<WaterAgentData> AgentsWriteOnly;
        [ReadOnly]
        public NativeArray<float3> AgentsPossibleDirections; // is normalized
        [ReadOnly]
        public NativeArray<int2> PointsDataStartLength;
        [ReadOnly]
        public NativeArray<LeafBody> PointsInRadius;
        public void Execute(int index)
        {
            float3 acceleration = GetGravitropismVector(index);
            if(acceleration.x + acceleration.y + acceleration.z == 0.0f) {
                AgentsReadOnly[index] = AgentsReadOnly[index].SetAcceleration(float3.zero);
                return;
            }
            // clamp acceleration
            acceleration = math.normalizesafe(acceleration) * Acceleration;
            AgentsReadOnly[index] = AgentsReadOnly[index].SetAcceleration(acceleration);
            // calculate new velocity and apply collision
            float oldVelocityScale = IsInCollision(AgentsReadOnly[index].Position + (math.normalize(AgentsReadOnly[index].Velocity) * AgentRadius), index);
            // v_new = v_old + a * dt
            float3 newVelocity = (AgentsReadOnly[index].Velocity * oldVelocityScale) +  (AgentsReadOnly[index].Acceleration * DeltaTime);
            // clamp velocity
            float velocityMagnitude = math.clamp(math.length(newVelocity), 0.0f, MaxVelocity);
            newVelocity = math.normalizesafe(newVelocity) * velocityMagnitude;
            // pos_new = pos_old + v_new * dt
            float3 nextPosition = AgentsReadOnly[index].Position + (newVelocity * DeltaTime);
            AgentsWriteOnly[index] = AgentsReadOnly[index].Move(nextPosition, newVelocity);
        }

        private float3 GetGravitropismVector(int index)
        {
            float3 gravityDirection = new float3(0.0f, -1.0f, 0.0f);
            float3 closestVector = float3.zero;
            float largestDotValue = float.MinValue;
            for (int i = 0; i < NumberOfTries; i++)
            {
                float dValue = math.dot(AgentsPossibleDirections[index*NumberOfTries+i], gravityDirection);
                if(dValue > largestDotValue) {
                    largestDotValue = dValue;
                    closestVector = AgentsPossibleDirections[index*NumberOfTries+i];
                }
            }
            return closestVector;
        }
        private float IsInCollision(float3 position, int index)
        {
            for (int i = 0; i < PointsDataStartLength[index].y; i++)
            {
                LeafBody point = PointsInRadius[PointsDataStartLength[index].x + i];
                if (point.PointType == CollisionPointId && math.distance(point.PosCol.xyz, position) <= point.NormSize.y + AgentRadius) {
                    return 0.5f;
                }
            }
            return 1.0f;
        }
    }
    
}
