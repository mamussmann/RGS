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
using RGS.Models;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Configurations.Root
{
    [Serializable]
    public struct RootSGAgent
    {
        public const int WEIGHTS_GRAVITROPISM_OFFSET = 1;
        public const int WEIGHTS_HALOTROPISM_OFFSET = 2;
        public const int WEIGHTS_CHEMOTROPISM_OFFSET = 3;
        public const int WEIGHTS_MAGNETOTROPISM_OFFSET = 4;
        public const int WEIGHTS_THIGMOTROPISM_OFFSET = 5;
        public const int WEIGHTS_HYDROTROPISM_OFFSET = 6;
        public const int WEIGHTS_PHONOTROPISM_OFFSET = 7;
        public const int WEIGHTS_SEPARATION_OFFSET = 8;
        public const int WEIGHTS_COUNT = 9;
        public char AgentType;
        public Color UnityColor => m_color;
        public float3 Color => new float3(m_color.r, m_color.g, m_color.b);
        [SerializeField] private Color m_color;
        [SerializeField] public float Radius;
        public float SoundDetectionRadius;
        public FloatStdDev RootLifeTime;
        [SerializeField] [Range(0.0f, 1.0f)] public float BranchingProbability;
        public RootSGProductionRule[] ProductionRules;
        [Header("Boid")]
        [Tooltip("Agent perception angle in degrees")] [Range(0.0f, 180.0f)] public float PerceptionAngle;
        [Tooltip("Agent initial velocity at emergance")] public float InitialVelocity;
        [Tooltip("Agent minimum velocity")] public float MinimumVelocity;
        [Tooltip("Agent maximum velocity")] public float MaximumVelocity;
        [Tooltip("Agent maximum acceleration")] public float MaximumAcceleration;
        [Header("Root")]
        public string DisplayName;
        [Tooltip("Branch insertion angle in degrees")] public FloatStdDev RootInsertionAngle;
        public FloatStdDev RootBasalZoneLength;
        public FloatStdDev RootInterBranchingDistance;
        public FloatStdDev RootApicalZoneLength;
        public bool UseMaxLength;
        public FloatStdDev MaximumRootLength;
        [Tooltip("Amount of random tropism directions.")] [Min(1)] public int NumberOfTries;
        [Min(0.0f)] public float DeltaPerceptionAngle;
        [Header("Tropisms")]
        public float GravitropismWeight;
        [Range(0.0f, 180.0f)] public float GravitySetPointAngle;
        public float HalotropismWeight;
        public float ChemotropismWeight;
        public float MagnetotropismWeight;
        public float ThigmotropismWeight;
        public float HydrotropismWeight;
        public float PhonotropismWeight;
        public float SeparationWeight;
        [Header("Scaling Functions")]
        public RootDistanceBasedScalingFunctionParameters GSAScalingByLengthFunction;
        public RootSGScalingFunctionParameter[] BranchingDensityScalingFunction;
        public RootSGScalingFunctionParameter[] RootLengthScalingFunction;
        public RootSGScalingFunctionParameter[] InsertionAngleScalingFunction;
        public RootSGScalingFunctionParameter[] BranchingProbabilityScalingFunction;
        public RootSGScalingFunctionParameter[] ElongationRateScalingFunction;
        public RootSGScalingFunctionParameter[] GravitropismScalingFunction;
        public RootSGScalingFunctionParameter[] GSAScalingFunction;
        [Header("Required Resources")]
        public RootAgentRequiredResource[] RequiredResources;
        public RootSGScalingFunctionParameter[] GetScalingFunctionArrayByType(ScalingFunctionType scalingFunctionType)
        {
            switch (scalingFunctionType)
            {
                case ScalingFunctionType.BRANCH_DENSITY: return BranchingDensityScalingFunction;
                case ScalingFunctionType.BRANCHING_PROBABILITY: return BranchingProbabilityScalingFunction;
                case ScalingFunctionType.ELONGATION_RATE: return ElongationRateScalingFunction;
                case ScalingFunctionType.INSERTION_ANGLE: return InsertionAngleScalingFunction;
                case ScalingFunctionType.ROOT_LENGTH: return RootLengthScalingFunction;
                case ScalingFunctionType.GRAVITROPISM: return GravitropismScalingFunction;
                case ScalingFunctionType.GSA_ANGLE: return GSAScalingFunction;
            }
            return null;
        }
    }
}

