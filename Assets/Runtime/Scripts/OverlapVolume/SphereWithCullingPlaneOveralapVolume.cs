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
using RGS.Jobs.Overlap;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace PCMTool.Tree.OverlapVolume
{

    /// <summary>
    /// Sphere overlap volume used for scheduling overlap jobs and testing.
    /// </summary>
    public struct SphereWithCullingPlaneOveralapVolume : IOverlapVolume
    {
        private float4 m_centerRadius;
        private int m_pointTypeFilter;
        private NativeArray<int> m_jobResult;
        private float3 m_planeCenter;
        private float3 m_planeDirection;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SphereWithCullingPlaneOveralapVolume(Vector3 center, float radius, NativeArray<int> jobResult, Vector3 planePosition, Vector3 planeDirection,int pointTypeFilter = -1)
        {
            m_centerRadius = new float4(center.x, center.y, center.z, radius);
            m_jobResult = jobResult;
            m_pointTypeFilter = pointTypeFilter;
            m_planeCenter = planePosition;
            m_planeDirection = planeDirection;
        }

        public int ExecuteOverlapRemoveJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            var overlapRemoveJob = new SphereCullingPlaneLeafsOverlapRemoveJob()
            {
                OutputLeafCount = m_jobResult,
                LeafCount = leafCount,
                LeafBodies = leafBodies,
                OriginRadius = m_centerRadius,
                PointTypeFilter = m_pointTypeFilter,
                PlaneCenter = m_planeCenter,
                PlaneDirection = m_planeDirection
            };
            overlapRemoveJob.Schedule().Complete();
            return m_jobResult[0];
        }

        public bool ExecuteOverlapTestJob(int leafCount, NativeSlice<LeafBody> leafBodies)
        {
            var overlapTestJob = new SphereCullingPlaneLeafsOverlapFilterTestJob()
            {
                Output = m_jobResult,
                LeafCount = leafCount,
                LeafBodies = leafBodies,
                OriginRadius = m_centerRadius,
                PointTypeFilter = m_pointTypeFilter,
                PlaneCenter = m_planeCenter,
                PlaneDirection = m_planeDirection
            };
            overlapTestJob.Schedule().Complete();
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
            // test overlaps non culled space    
            float3 localMin = min - m_planeCenter;
            float3 localMax = max - m_planeCenter;
            float3[] points = new float3[] {
                // localMin side
                new float3(localMin.x, localMin.y, localMin.z),
                new float3(localMax.x, localMin.y, localMin.z),
                new float3(localMin.x, localMax.y, localMin.z),
                new float3(localMin.x, localMin.y, localMax.z),
                // localMax side
                new float3(localMax.x, localMax.y, localMax.z),
                new float3(localMin.x, localMax.y, localMax.z),
                new float3(localMax.x, localMin.y, localMax.z),
                new float3(localMax.x, localMax.y, localMin.z)
            };
            bool isCulled = true;
            foreach (var point in points)
            {
                if(math.dot(point, m_planeDirection) >= 0.0f) 
                {
                    isCulled = false;
                    break;
                }
            }
            if(isCulled) return false;

            // test sphere
            float ex = Mathf.Max(min.x - m_centerRadius.x, 0.0f) + Mathf.Max(m_centerRadius.x - max.x, 0.0f);
            float ey = Mathf.Max(min.y - m_centerRadius.y, 0.0f) + Mathf.Max(m_centerRadius.y - max.y, 0.0f);
            float ez = Mathf.Max(min.z - m_centerRadius.z, 0.0f) + Mathf.Max(m_centerRadius.z - max.z, 0.0f);

            return (ex < m_centerRadius.w) && (ey < m_centerRadius.w) && (ez < m_centerRadius.w) && (ex * ex + ey * ey + ez * ez < m_centerRadius.w * m_centerRadius.w);
        }
    }
}