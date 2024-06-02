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
using System.Runtime.CompilerServices;
using PCMTool.Tree;
using PCMTool.Tree.OverlapVolume;
using RGS.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RGS.OverlapVolume
{
    /// <summary>
    /// AABB overlap volume used for scheduling overlap jobs and testing.
    /// </summary>
    public struct AABBPointsOverlapVolume : IOverlapVolume
    {
        private float3 m_min, m_max;
        private NativeArray<int> m_jobResult;
        private NativeArray<float3> m_points;
        private float m_toleranceRadius;
        private int m_pointType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABBPointsOverlapVolume(float3 min, float3 max, NativeArray<float3> points, float toleranceRadius, int pointType, NativeArray<int> jobResult)
        {
            m_min = min;
            m_max = max;
            m_jobResult = jobResult;
            m_points = points;
            m_toleranceRadius = toleranceRadius;
            m_pointType = pointType;
        }

        public int ExecuteOverlapRemoveJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            var overlapRemoveJob = new AABBPointsLeafsOverlapRemoveJob()
            {
                ToleranceRadius = m_toleranceRadius,
                Points = m_points,
                OutputLeafCount = m_jobResult,
                LeafCount = leafCount,
                LeafBodies = leafBodies,
                Min = m_min,
                Max = m_max,
                PointType = m_pointType
            };
            overlapRemoveJob.Schedule().Complete();
            return m_jobResult[0];
        }

        public bool ExecuteOverlapTestJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            var overlapTestJob = new AABBPointsLeafsOverlapTestJob()
            {
                ToleranceRadius = m_toleranceRadius,
                Points = m_points,
                Output = m_jobResult,
                LeafCount = leafCount,
                LeafBodies = leafBodies,
                Min = m_min,
                Max = m_max,
                PointType = m_pointType
            };
            overlapTestJob.Schedule().Complete();
            return m_jobResult[0] == 1;
        }

        /// <summary>
        /// Tests if the given bounding box overlaps the volumes' bounding box.
        /// </summary>
        /// <param name="min"> Minimum of the axis aligned bounding box.</param>
        /// <param name="max"> Maximum of the axis aligned bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestAABBOverlap(float3 min, float3 max)
        {
            return
                m_min.x < max.x && m_max.x >= min.x &&
                m_min.y < max.y && m_max.y >= min.y &&
                m_min.z < max.z && m_max.z >= min.z;
        }
    }
}