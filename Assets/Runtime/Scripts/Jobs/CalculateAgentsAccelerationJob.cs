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
using PCMTool.Tree;
using RGS.Agents;
using RGS.Configurations.Root;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct CalculateAgentsAccelerationJob : IJobParallelFor
    {
        [ReadOnly]
        public float UnitTropismVectorMagnitude;
        [ReadOnly]
        public int RootPointType;
        [ReadOnly]
        public NativeArray<WaterAgentData> WaterAgentsReadonly;
        [ReadOnly]
        public NativeArray<float> AgentsGSA;
        [ReadOnly]
        public NativeArray<float> AgentsSoundDetectionRadius;
        [ReadOnly]
        public int SoilPointType;
        [ReadOnly]
        public NativeArray<float3> MagnetotropismBounds;
        [ReadOnly]
        public NativeArray<int> ChemotropismPointTypesWithSign;
        [ReadOnly]
        public int NaClPointType;
        [ReadOnly]
        public NativeArray<int> AgentsNumberOfTries;
        [ReadOnly]
        public NativeArray<float> AgentsAccelerationLimits;
        [ReadOnly]
        public NativeArray<float> AgentsRadius;
        [ReadOnly]
        public NativeArray<float> AgentsWeights;
        public NativeArray<RootSGAgentData> AgentsReadOnly;
        [ReadOnly]
        public NativeArray<int> PossibleDirectionsDataStartIndex;
        [ReadOnly] 
        public NativeArray<float3> AgentsPossibleDirections; // is normalized
        [ReadOnly]
        public NativeArray<RootSGScalingFactors> AgentsRootScalingFactors;
        [ReadOnly]
        public NativeArray<int2> PointsDataStartLength;
        [ReadOnly]
        public NativeArray<LeafBody> PointsInRadius;
        [ReadOnly]
        public NativeArray<float> AgentsVelocityLimits;
        public void Execute (int index)
        {
            int agentType = AgentsReadOnly[index].AgentType;
            float3 acceleration = 
                (GetGravitropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_GRAVITROPISM_OFFSET])+ 
                (GetHalotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_HALOTROPISM_OFFSET]) + 
                (GetChemotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_CHEMOTROPISM_OFFSET])+ 
                (GetMagnetotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_MAGNETOTROPISM_OFFSET])+ 
                (GetThigmotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_THIGMOTROPISM_OFFSET])+ 
                (GetHydrotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_HYDROTROPISM_OFFSET])+ 
                (GetPhonotropismVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_PHONOTROPISM_OFFSET])+
                (GetSeparationUrgeVector(index, agentType) * AgentsWeights[agentType *  RootSGAgent.WEIGHTS_COUNT + RootSGAgent.WEIGHTS_SEPARATION_OFFSET]);
            float magnitude = math.clamp(math.length(acceleration), 0.0f,  AgentsAccelerationLimits[agentType]);
            acceleration = math.normalizesafe(acceleration) * magnitude;
            AgentsReadOnly[index] = AgentsReadOnly[index].SetAcceleration(acceleration);
        }

        private float3 GetSeparationUrgeVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 position = AgentsReadOnly[index].Position;
            float3 bestVector = float3.zero;
            int rootPointCount = 0;
            float smallestDotValue = float.MaxValue;
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float value = 0.0f;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    if (point.PointType == RootPointType)
                    {
                        rootPointCount++;
                        float dValue = math.dot(AgentsPossibleDirections[startIndex+i], math.normalize(point.PosCol.xyz - position));
                        dValue = math.max(dValue, 0.0f);
                        value += dValue;
                    }
                }
                if(value < smallestDotValue) {
                    smallestDotValue = value;
                    bestVector = AgentsPossibleDirections[startIndex+i];
                }
            }
            if(rootPointCount == 0)
            {
                return float3.zero;
            }
            return bestVector * UnitTropismVectorMagnitude;
        }

        private float3 GetHydrotropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 bestVector = float3.zero;
            float largestWaterValue = float.MinValue;
            int hydrotropismVectorCount = 0; // amount of points that influence halotropism
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float3 possibleTargetPosition = AgentsReadOnly[index].Position + (AgentsPossibleDirections[startIndex + i] * AgentsVelocityLimits[agentType * 2 + 1]);
                float value = 0.0f;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    if (point.PointType == SoilPointType)
                    {
                        hydrotropismVectorCount++;
                        float distance = math.max(math.distance(point.PosCol.xyz, possibleTargetPosition) - (point.NormSize.y + AgentsRadius[agentType]), 0.00001f);
                        value += point.NormSize.z * (1.0f / distance);
                    }
                }
                if (value > largestWaterValue)
                {
                    largestWaterValue = value;
                    bestVector = AgentsPossibleDirections[startIndex + i];
                }
            }
            if (hydrotropismVectorCount == 0)
            {
                return float3.zero;
            }
            return bestVector * UnitTropismVectorMagnitude;
        }

        /// <summary>
        /// Uses the weighted mean of all water agents in a radius.
        /// </summary>
        private float3 GetPhonotropismVector(int index, int agentType)
        {
            float3 averagePosition = float3.zero;
            float weightSum = 0.0f;
            int vectorCount = 0;
            for (int i = 0; i < WaterAgentsReadonly.Length; i++)
            {
                float dist = math.distance(AgentsReadOnly[index].Position, WaterAgentsReadonly[i].Position);
                if(dist <= AgentsSoundDetectionRadius[agentType] + AgentsRadius[agentType])
                {
                    weightSum += (1.0f / dist);
                    averagePosition += WaterAgentsReadonly[i].Position * (1.0f / dist);
                    vectorCount++;
                }
            }
            if (vectorCount == 0)
            {
                return float3.zero;
            }
            float3 direction = math.normalize((averagePosition / weightSum) - AgentsReadOnly[index].Position);
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 possibleDirection = AgentsPossibleDirections[startIndex];
            float lastDot = math.dot(direction, possibleDirection);
            for (int i = 1; i < AgentsNumberOfTries[agentType]; i++)
            {
                float3 possibleAgentDirection = AgentsPossibleDirections[startIndex + i];
                if(math.dot(direction, possibleAgentDirection) > lastDot)
                {
                    possibleDirection = possibleAgentDirection;
                }
            }
            return possibleDirection * UnitTropismVectorMagnitude;
        }

        private float3 GetChemotropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 bestPositiveVector = float3.zero;
            float3 bestNegativeVector = float3.zero;
            float highestChemotropismValue = float.MinValue;
            float lowestChemotropismValue = float.MaxValue;
            int chemotropismVectorCount = 0; // amount of points that influence chemotropism
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float3 possibleTargetPosition = AgentsReadOnly[index].Position + (AgentsPossibleDirections[startIndex + i] * AgentsVelocityLimits[agentType * 2 +1]);
                float positiveValue = 0.0f;
                float negativeValue = 0.0f;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    for (int k = 0; k < ChemotropismPointTypesWithSign.Length; k++)
                    {
                        if (point.PointType == math.abs(ChemotropismPointTypesWithSign[k]))
                        {
                            float distance = math.max(math.distance(point.PosCol.xyz, possibleTargetPosition) - (point.NormSize.y + AgentsRadius[agentType]), 0.00001f);
                            chemotropismVectorCount++;
                            if(ChemotropismPointTypesWithSign[k] > 0.0f) {
                                positiveValue += point.NormSize.z * (1.0f / distance);
                            } else
                            {
                                negativeValue += point.NormSize.z * (1.0f / distance);
                            }
                            break;
                        }
                    }
                }
                if (negativeValue < lowestChemotropismValue) {
                    lowestChemotropismValue = negativeValue;
                    bestNegativeVector = AgentsPossibleDirections[startIndex + i];
                }
                if (positiveValue > highestChemotropismValue)
                {
                    highestChemotropismValue = positiveValue;
                    bestPositiveVector = AgentsPossibleDirections[startIndex + i];
                }
            }
            if (chemotropismVectorCount == 0)
            {
                return float3.zero;
            }
            return math.normalize(bestPositiveVector + bestNegativeVector) * 0.01f;
        }
        private float3 GetHalotropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 bestVector = float3.zero;
            float smallestNaClValue = float.MaxValue;
            int halotropismVectorCount = 0; // amount of points that influence halotropism
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float3 possibleTargetPosition = AgentsReadOnly[index].Position + (AgentsPossibleDirections[startIndex + i] * AgentsVelocityLimits[agentType * 2 +1]);
                float value = 0.0f;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    if (point.PointType == NaClPointType)
                    {
                        halotropismVectorCount++;
                        float distance = math.max(math.distance(point.PosCol.xyz, possibleTargetPosition) - (point.NormSize.y + AgentsRadius[agentType]), 0.00001f);
                        value += point.NormSize.z * (1.0f / distance);
                    }
                }
                if (value < smallestNaClValue)
                {
                    smallestNaClValue = value;
                    bestVector = AgentsPossibleDirections[startIndex + i];
                }
            }
            if (halotropismVectorCount == 0)
            {
                return float3.zero;
            }
            return bestVector * UnitTropismVectorMagnitude;
        }
        private float3 GetGravitropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float rootGSA = math.clamp( AgentsGSA[agentType] * AgentsRootScalingFactors[index].GSAScale, 0.0f, math.PI * 2.0f);
            float3 closestVector = float3.zero;
            float smallestAngleDifference = float.MaxValue;
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float dValue = math.distance(rootGSA, math.acos(math.dot(AgentsPossibleDirections[startIndex+i], new float3(0,-1,0))));
                if(dValue < smallestAngleDifference) {
                    smallestAngleDifference = dValue;
                    closestVector = AgentsPossibleDirections[startIndex+i];
                }
            }
            return new float3(closestVector * UnitTropismVectorMagnitude) * AgentsRootScalingFactors[index].GravitropismScale; //scale to 1 cm
        }
        private float3 GetMagnetotropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 position = AgentsReadOnly[index].Position;
            float3 magneticFieldDirection = float3.zero;
            bool isInBounds = false;
            for (int i = 0; i < MagnetotropismBounds.Length / 3; i++)
            {
                float3 min = MagnetotropismBounds[i * 3];
                float3 max = MagnetotropismBounds[i * 3 + 1];
                if(
                    min.x < position.x && min.y < position.y && min.z < position.z &&
                    max.x >= position.x && max.y >= position.y && max.z >= position.z)
                {
                    isInBounds = true;
                    magneticFieldDirection = MagnetotropismBounds[i * 3 + 2];
                    break;
                }
            }
            if(!isInBounds) return float3.zero;
            float3 closestVector = float3.zero;
            float largestDotValue = float.MinValue;
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float dValue = math.dot(AgentsPossibleDirections[startIndex+i], magneticFieldDirection);
                if(dValue > largestDotValue) {
                    largestDotValue = dValue;
                    closestVector = AgentsPossibleDirections[startIndex+i];
                }
            }
            return closestVector * UnitTropismVectorMagnitude; //scale to 1 cm
        }
        private float3 GetThigmotropismVector(int index, int agentType)
        {
            int startIndex = PossibleDirectionsDataStartIndex[index];
            float3 position = AgentsReadOnly[index].Position;
            float3 bestVector = float3.zero;
            int soilPointCount = 0;
            float smallestDotValue = float.MaxValue;
            for (int i = 0; i < AgentsNumberOfTries[agentType]; i++)
            {
                float value = 0.0f;
                for (int j = 0; j < PointsDataStartLength[index].y; j++)
                {
                    LeafBody point = PointsInRadius[PointsDataStartLength[index].x + j];
                    if (point.PointType == SoilPointType)
                    {
                        soilPointCount++;
                        float dValue = math.dot(AgentsPossibleDirections[startIndex+i], math.normalize(point.PosCol.xyz - position));
                        dValue = math.max(dValue, 0.0f);
                        value += dValue;
                    }
                }
                if(value < smallestDotValue) {
                    smallestDotValue = value;
                    bestVector = AgentsPossibleDirections[startIndex+i];
                }
            }
            if(soilPointCount == 0)
            {
                return float3.zero;
            }
            return bestVector * UnitTropismVectorMagnitude;
        }
    }

}
