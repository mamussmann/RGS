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
using PCMTool.Tree.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace PCMTool.Tree.OverlapVolume
{

    /// <summary>
    /// Sphere overlap volume used for scheduling overlap jobs and testing.
    /// </summary>
    public struct SphereOveralapVolume : IOverlapVolume
    {
        private float4 m_centerRadius;
        private int m_pointTypeFilter;
        private NativeArray<int> m_jobResult;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphereOveralapVolume(Vector3 center, float radius, NativeArray<int> jobResult, int pointTypeFilter = -1)
        {
            m_centerRadius = new float4(center.x, center.y, center.z, radius);
            m_jobResult = jobResult;
            m_pointTypeFilter = pointTypeFilter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphereOveralapVolume(float3 center, float radius, NativeArray<int> jobResult, int pointTypeFilter = -1)
        {
            m_centerRadius = new float4(center.x, center.y, center.z, radius);
            m_jobResult = jobResult;
            m_pointTypeFilter = pointTypeFilter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphereOveralapVolume(float4 centerRadius, NativeArray<int> jobResult, int pointTypeFilter = -1)
        {
            m_centerRadius = centerRadius;
            m_jobResult = jobResult;
            m_pointTypeFilter = pointTypeFilter;
        }

        public int ExecuteOverlapRemoveJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            var overlapRemoveJob = new SphereLeafsOverlapRemoveJob()
            {
                OutputLeafCount = m_jobResult,
                LeafCount = leafCount,
                LeafBodies = leafBodies,
                OriginRadius = m_centerRadius
            };
            overlapRemoveJob.Schedule().Complete();
            return m_jobResult[0];
        }

        public bool ExecuteOverlapTestJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            //TODO: refactor to remove branching
            if (m_pointTypeFilter != -1 ) {
                var overlapTestJob = new SphereLeafsOverlapFilterTestJob()
                {
                    Output = m_jobResult,
                    LeafCount = leafCount,
                    LeafBodies = leafBodies,
                    OriginRadius = m_centerRadius,
                    PointTypeFilter = m_pointTypeFilter
                };
                overlapTestJob.Schedule().Complete();
            } else {
                var overlapTestJob = new SphereLeafsOverlapTestJob()
                {
                    Output = m_jobResult,
                    LeafCount = leafCount,
                    LeafBodies = leafBodies,
                    OriginRadius = m_centerRadius
                };
                overlapTestJob.Schedule().Complete();
            }
            return m_jobResult[0] == 1;
        }

        /// <summary>
        /// Tests if the given bounding box overlaps the sphere volume.
        /// </summary>
        /// <param name="min"> Minimum of the axis aligned bounding box.</param>
        /// <param name="max"> Maximum of the axis aligned bounding box.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TestAABBOverlap(float3 min, float3 max)
        {
            float ex = Mathf.Max(min.x - m_centerRadius.x, 0.0f) + Mathf.Max(m_centerRadius.x - max.x, 0.0f);
            float ey = Mathf.Max(min.y - m_centerRadius.y, 0.0f) + Mathf.Max(m_centerRadius.y - max.y, 0.0f);
            float ez = Mathf.Max(min.z - m_centerRadius.z, 0.0f) + Mathf.Max(m_centerRadius.z - max.z, 0.0f);

            return (ex < m_centerRadius.w) && (ey < m_centerRadius.w) && (ez < m_centerRadius.w) && (ex * ex + ey * ey + ez * ez < m_centerRadius.w * m_centerRadius.w);
        }
    }
}