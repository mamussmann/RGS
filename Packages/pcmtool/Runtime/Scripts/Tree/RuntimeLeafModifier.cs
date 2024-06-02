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
using UnityEngine;
using PCMTool.Tree.Files;
using PCMTool.Tree.QueryJobScheduler;
using Unity.Mathematics;
using Unity.Collections;
using PCMTool.Tree.Query;
using PCMTool.Tree.OverlapVolume;
using System;

namespace PCMTool.Tree
{
    /// <summary>
    /// Class that encapsulates complex operations that can be performed on the tree.
    /// </summary>
    [RequireComponent(typeof(LeafRenderer))]
    public class RuntimeLeafModifier : MonoBehaviour
    {
        /// <summary>
        /// Optional custom seed.
        /// </summary>
        [SerializeField] private int m_customSeed;
        /// <summary>
        /// If true custom seed is used.
        /// </summary>
        [SerializeField] private bool m_useCustomSeed;
        /// <summary>
        /// Offset applied to imported ply points.
        /// </summary>
        [SerializeField] private Vector3 m_importOffset;
        /// <summary>
        /// The maximum distance of the sphere cast.
        /// </summary>
        [SerializeField] private float m_sphereCastDistance = 10.0f;
        /// <summary>
        /// The maximum amount of sphere overlap checks that are performed in a sphere cast.
        /// </summary>
        [SerializeField] [Min(2)] private int m_sphereDetail = 50;
        public int SphereDetail => m_sphereDetail;
        public float SphereCastDistance => m_sphereCastDistance;
        public PointCloudTree Tree => m_tree;
        private PointCloudTree m_tree;
        private LeafRenderer m_renderer;
        private void Awake() {
            m_renderer = GetComponent<LeafRenderer>();
            m_tree = new PointCloudTree();
            m_renderer.PointCloudTree = m_tree;
            m_renderer.LeafBufferUpdater = new LeafBufferUpdater();
            if(m_useCustomSeed)
            {
                UnityEngine.Random.InitState(m_customSeed);
            }
        }
        public void CreateWithGenerator(Action<PointCloudTree> genAction) 
        {
            genAction(m_tree);
            m_renderer.OnPointsAdded.Invoke();
        }
        public void PlacePoints(NativeArray<PointData> points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                m_tree.AddPoint(points[i]);   
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        public void PointUpdated()
        {
            m_renderer.OnPointsAllUpdate.Invoke();
        }
        public void PointRemoveUpdated()
        {
            m_renderer.OnPointsRemoved.Invoke();
        }
        /// <summary>
        /// Loads the points from the given ply file.
        /// </summary>
        /// <param name="detail"> Detail level (0-2).</param>
        public void LoadPlyFile(string filePath, int detail)
        {
            PlyImporterExporter.Import(filePath, m_tree, 1.0f, detail, m_importOffset);
            m_renderer.OnPointsAdded.Invoke();
        }
        /// <summary>
        /// Saves the current point cloud data to the given file.
        /// </summary>
        public void SaveToPlyFile(string filePath)
        {
            m_renderer.SavePointsAsPly(filePath);
        }
        /// <summary>
        /// Adds random points that are distributed in a cube shape.
        /// </summary>
        public void AddCube(Vector3 origin, Vector3 dir1, Vector3 dir2, Vector3 dir3, int count, Color pointColor, bool useColor = true, int pointType = 0, float pointScale = 0.1f, float pointAmount = 0.0f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 point = origin + (dir1 * UnityEngine.Random.value) + (dir2 * UnityEngine.Random.value)+ (dir3 * UnityEngine.Random.value);
                Vector3 color = useColor ? new Vector3(pointColor.r, pointColor.g, pointColor.b) : point;
                m_tree.AddPoint(new PointData(point, color, pointScale, pointType, pointAmount));
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        /// <summary>
        /// Adds random points that are distributed in a cube shape.
        /// </summary>
        public void AddCube(Vector3 origin, Vector3 dir1, Vector3 dir2, Vector3 dir3, int count, Vector3 colorMin, Vector3 colorMax, int pointType = 0, float pointScale = 0.1f, float pointAmount = 0.0f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 point = origin + (dir1 * UnityEngine.Random.value) + (dir2 * UnityEngine.Random.value)+ (dir3 * UnityEngine.Random.value);
                Vector3 color = Vector3.Lerp(colorMin, colorMax, UnityEngine.Random.value);
                m_tree.AddPoint(new PointData(point, color, pointScale, pointType, pointAmount));
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        /// <summary>
        /// Removes all points within the given cube shape.
        /// </summary>
        public void RemoveCube(Vector3 origin, Vector3 dir1, Vector3 dir2, Vector3 dir3)
        {
            Vector3 v1 = origin;
            Vector3 v2 = origin + dir1 + dir2 + dir3;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            min.x = v1.x < v2.x ? v1.x : v2.x;
            min.y = v1.y < v2.y ? v1.y : v2.y;
            min.z = v1.z < v2.z ? v1.z : v2.z;
            max.x = v1.x >= v2.x ? v1.x : v2.x;
            max.y = v1.y >= v2.y ? v1.y : v2.y;
            max.z = v1.z >= v2.z ? v1.z : v2.z;
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var modifyList = m_tree.RemovePoints(new AABBOverlapVolume(min, max, output));
            output.Dispose();
            m_tree.ClearEmptyBranches(modifyList);
            m_renderer.OnPointsRemoved.Invoke();
            m_tree.ClearEmptyBlocks();
        }
        /// <summary>
        /// Removes all points within the given sphere shape.
        /// </summary>
        public void Carve(Vector3 position, float radius)
        {
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var modifyList = m_tree.RemovePoints(new SphereOveralapVolume(position, radius, output));
            output.Dispose();
            m_tree.ClearEmptyBranches(modifyList);
            m_renderer.OnPointsRemoved.Invoke();
            m_tree.ClearEmptyBlocks();
        }
        public void AddPoint(Vector3 position, Vector3 customColor = new Vector3(), bool useColor = false, float pointScale = 1.0f, int pointType = 0)
        {
            Vector3 color = useColor ? customColor : position;
            m_tree.AddPoint(new PointData(position, color, pointScale, pointType));
            m_renderer.OnPointsAdded.Invoke();
        }
        public void AddSphere(Vector3 position, float radius, int pointCount, Vector3 colorMin, Vector3 colorMax, int pointType = 0, float pointScale = 0.1f)
        {
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value) + position;
                Vector3 color = Vector3.Lerp(colorMin, colorMax, UnityEngine.Random.value);
                m_tree.AddPoint(new PointData(point, color, pointScale, pointType, 0.5f));
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        public void AddSphere(Vector3 position, float radius, int pointCount, Vector3 colorMin, Vector3 colorMax, int pointType = 0, float pointScale = 0.1f, float pointAmountValue = 1.0f)
        {
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value) + position;
                Vector3 color = Vector3.Lerp(colorMin, colorMax, UnityEngine.Random.value);
                m_tree.AddPoint(new PointData(point, color, pointScale, pointType, pointAmountValue));
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        /// <summary>
        /// Adds random points that are distributed in a sphere shape.
        /// </summary>
        public void Add(Vector3 position, float radius, int pointCount, Vector3 customColor = new Vector3(), bool useColor = false, int pointType = 0, float pointScale = 0.1f)
        {
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value) + position;
                Vector3 color = useColor ? customColor : point;
                m_tree.AddPoint(new PointData(point, color, pointScale, pointType, 0.5f));
            }
            m_renderer.OnPointsAdded.Invoke();
        }
        /// <summary>
        /// Modifies the point size of all points within a sphere shape.
        /// </summary>
        public void SetPointScale(Vector3 position, float radius, float deltaSize)
        {
            m_tree.QueryOverlapPoints(new SphereQueryOveralapVolume(position, radius));

            PointSizeJobScheduler job = new PointSizeJobScheduler(){
                Tree = m_tree,
                DeltaSize = deltaSize,
                OriginRadius = new float4(position, radius)
            };
            job.ScheduleJobs();
            m_renderer.OnPointsUpdated.Invoke();
        }
        /// <summary>
        /// Modifies the point color of all points within a sphere shape.
        /// </summary>
        public void SetColor(Vector3 position, float radius, Color color)
        {
            float encodedColor = PointColorEncoding.EncodeColorFloat(color);
            m_tree.QueryOverlapPoints(new SphereQueryOveralapVolume(position, radius));
            ColorJobScheduler job = new ColorJobScheduler(){
                Tree = m_tree,
                TargetEncodedColor = encodedColor,
                OriginRadius = new float4(position, radius)
            };
            job.ScheduleJobs();
            m_renderer.OnPointsUpdated.Invoke();
        }
        /// <summary>
        /// Performs a sphere cast on the current point cloud tree.
        /// </summary>
        public bool HitPC(Vector3 start, Vector3 direction, float radius, out Vector3 position, bool useRadiusOffset = true)
        {
            position = Vector3.zero;
            float hitDistance;
            if(m_tree.SphereOverlapCast(start, direction, m_sphereCastDistance, m_sphereDetail, radius, out hitDistance))
            {
                if(useRadiusOffset) {
                    hitDistance += radius;
                }
                position = start + (direction * hitDistance);
                return true;
            }
            return false;
        }
        public bool OverlapPC(Vector3 position, float radius, int pointTypeFilter)
        {
            bool hasHit = false;
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            if (m_tree.OverlapTestPoints(new SphereOveralapVolume(position, radius, output, 0)))
            {
                hasHit = true;
            }
            output.Dispose();
            return hasHit;
        }
        private void OnDestroy() {
            m_tree.Dispose();
        }

    }
}