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
using RGS.Models;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct CalculateWaterAgentsDirectionsJob : IJobParallelFor
    {
        [ReadOnly]
        public float MaxVelocity;
        [ReadOnly]
        public float AgentRadius;
        [ReadOnly]
        public AgentTypePerceptionData PerceptionData;
        [ReadOnly]
        public NativeArray<float> RandomValues;
        [ReadOnly]
        public int RandomSeed;
        [ReadOnly]
        public int CollisionPointId;
        [ReadOnly]
        public NativeArray<WaterAgentData> AgentsReadOnly;
        [ReadOnly]
        public NativeArray<int2> PointsDataStartLength;
        [ReadOnly]
        public NativeArray<LeafBody> PointsInRadius;
        [NativeDisableParallelForRestriction] 
        public NativeArray<float3> AgentsPossibleDirections;
        public void Execute (int index)
        {
            float perceptionAngleRad = PerceptionData.PerceptionAngleRad;
            float minPerceptionAngleRad = 0.0f;
            float3 normViewDirection = new float3(0,-1,0);
            for (int i = 0; i < PerceptionData.AngleChangeCount; i++)
            {
                int notCollidingTriesCount = 0;
                for (int j = 0; j < PerceptionData.NumberOfTries; j++)
                {
                    float3 direction = GenerateDirectionInView(minPerceptionAngleRad, perceptionAngleRad, normViewDirection, index + j);
                    int isInCollision = IsInCollision(AgentsReadOnly[index].Position + (direction * AgentRadius), index);
                    if(isInCollision == 1) {
                        AgentsPossibleDirections[index * PerceptionData.NumberOfTries + j] = direction;
                        notCollidingTriesCount++;
                    }
                }
                if(notCollidingTriesCount > 0){
                    return;
                } 
                minPerceptionAngleRad = perceptionAngleRad;
                perceptionAngleRad += PerceptionData.DeltaPerceptionAngleRad;
            }
        }
        
        private int IsInCollision(float3 position, int index)
        {
            for (int i = 0; i < PointsDataStartLength[index].y; i++)
            {
                LeafBody point = PointsInRadius[PointsDataStartLength[index].x + i];
                float distance = math.distance(point.PosCol.xyz, position);
                if (point.PointType == CollisionPointId && distance  <= point.NormSize.y + AgentRadius) {
                    return 0;
                }
            }
            return 1;
        }

        private float3 GenerateDirectionInView(float minPerceptionAngle, float maxPerceptionAngle, float3 direction, int index)
        {
            float randomPerceptionAngle = RandomValues[(index + RandomSeed) % RandomValues.Length] * (maxPerceptionAngle - minPerceptionAngle) + minPerceptionAngle;
            float angleRadial = math.PI * 2.0f * RandomValues[(index + RandomSeed + 1) % RandomValues.Length] ;
            // rotate direction by given angles
            var other = math.abs(math.dot(new float3(0.0f, 1.0f, 0.0f), direction)) < 1.0f ? new float3(0.0f, 1.0f, 0.0f) : new float3(1.0f, 0.0f, 0.0f);
            float3 perpendicularVector = math.normalize(math.cross(direction, other));
            float3 result = math.mul(float3x3.AxisAngle(perpendicularVector, randomPerceptionAngle), direction);
            result = math.mul(float3x3.AxisAngle(direction, angleRadial), result);
            return math.normalize(result);
        }
    }

}
