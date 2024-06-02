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
using System.Runtime.CompilerServices;
using PCMTool.Tree.Files;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PCMTool.Tree
{
    /// <summary>
    /// Class handling the rendering of a point cloud tree.
    /// </summary>
    public class LeafRenderer : MonoBehaviour
    {

#if UNITY_EDITOR
        public static bool RenderGUI = true;
#endif
        /// <summary>
        /// Shader used for rendering the points in the point cloud.
        /// </summary>
        [SerializeField] private Shader m_leafShader;
        /// <summary>
        /// If true points are rendered as quad geometry.
        /// </summary>
        [SerializeField] private bool m_renderQuads;
        /// <summary>
        /// The type of points rendered
        /// </summary>
        [SerializeField] private int m_pointType;
        /// <summary>
        /// If all point types should be rendered
        /// </summary>
        public bool ShowAllPoints = true;
        /// <summary>
        /// If culling plane should be used
        /// </summary>
        public bool EnableCullingPlane = false;
        public Vector3 PlaneDirection;
        public Vector3 PlanePosition;
        public UnityEvent OnPointsAdded {get;} = new UnityEvent();
        public UnityEvent OnPointsRemoved {get;} = new UnityEvent();
        public UnityEvent OnPointsUpdated {get;} = new UnityEvent();
        public UnityEvent OnPointsAllUpdate {get;} = new UnityEvent();
        public IPointCloudTree PointCloudTree;
        public ILeafBufferUpdater LeafBufferUpdater;
        /// <summary>
        /// Compute buffer used for rendering the points.
        /// </summary>
        private ComputeBuffer m_buffer;
        /// <summary>
        /// Native array containing a pair of block index and index of the point in leaf data for each point the the compute buffer.
        /// </summary>
        private NativeArray<int> m_rowIndexBuffer;
        /// <summary>
        /// The amount of points in the compute buffer.
        /// </summary>
        private int m_pointSize = 0;
        [SerializeField] private int m_heatmapPointType = -1;
        [SerializeField] [Range(0.0f, 1.0f)] private float m_heatmapThreshold = 0.0f;
        [SerializeField] private Material m_leafMaterial;
        private bool m_showHeatmapGradient;
        private const string m_shaderPointBufferName = "_PointBuffer";
        private const string m_shaderPointSizeName = "_PointSize";
        private const string m_shaderPointTypeName = "_PointType";
        private const string m_shaderHeatmapOverlayPointTypeName = "_heatmapOverlayPointType";
        private const string m_shaderHeatmapOverlayThresholdName = "_heatmapOverlayThreshold";
        private const string m_shaderShowHeatmapGradientThresholdName = "_showHeatmapGradient";
        private const string m_shaderQuadGeoKeyword = "_QUAD_GEOMETRY";
        private const string m_shaderShowAllPointsName = "_ShowAllPoints";
        private const string m_shaderPlaneDirectionName = "_PlaneDirection";
        private const string m_shaderPlanePositionName = "_PlanePosition";
        private const string m_shaderEnableCullingPlane = "_EnableCullingPlane";
        private void Awake() {
            m_buffer = new ComputeBuffer(DataConstants.COMPUTE_BUFFER_SIZE, 32, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            m_rowIndexBuffer = new NativeArray<int>(DataConstants.COMPUTE_BUFFER_SIZE * 2, Allocator.Persistent);
            OnPointsAdded.AddListener(HandlePointsAdded);
            OnPointsRemoved.AddListener(HandlePointsRemoved);
            OnPointsUpdated.AddListener(HandlePointsUpdated);
            OnPointsAllUpdate.AddListener(HandlePointsAllUpdated);
            InitRenderer();
        }

        void OnDestroy()
        {
            OnPointsAdded.RemoveListener(HandlePointsAdded);
            OnPointsRemoved.RemoveListener(HandlePointsRemoved);
            OnPointsUpdated.RemoveListener(HandlePointsUpdated);
            OnPointsAllUpdate.RemoveListener(HandlePointsAllUpdated);
            m_buffer.Release();
            m_rowIndexBuffer.Dispose();
        }
        public void ShowHeatmapGradient(bool isVisible)
        {
            m_showHeatmapGradient = isVisible;
        }
        public void SetHeatmapPointType(int pointType)
        {
            m_heatmapPointType = pointType;
        }
        public void SetHeatMapThreshold(float value)
        {
            m_heatmapThreshold = Mathf.Clamp01(value);
        }
        /// <summary>
        /// Creates the material and sets shader parameters.
        /// </summary>
        void InitRenderer()
        {
            if(m_leafMaterial != null) return;
            m_leafMaterial = new Material(m_leafShader);
            m_leafMaterial.hideFlags = HideFlags.DontSave;
            if(m_renderQuads){
                m_leafMaterial.EnableKeyword(m_shaderQuadGeoKeyword);
            }
        }
        /// <summary>
        /// Sets point size to 0.
        /// </summary>
        public void Clear()
        {
            m_pointSize = 0;
        }

        public ComputeBuffer GetPointBuffer()
        {
            return m_buffer;
        }

        public int GetPointCount()
        {
            return m_pointSize;
        }

        private void HandlePointsAdded()
        {
            int deltaPointCount = LeafBufferUpdater.CalculateAdditionalPoints(PointCloudTree.GetBlockTreeDataList(), PointCloudTree.ModifiedBlocks);
            int newPointSize = m_pointSize + deltaPointCount;
            if(newPointSize == 0){
                m_pointSize = newPointSize;
                return;
            }
            UpdateBuffer(newPointSize);
        }

        private void HandlePointsUpdated()
        {
            UpdateBufferOnlyUpdate();
        }
        private void HandlePointsAllUpdated()
        {
            UpdateAllBufferOnlyUpdate();
        }

        private void HandlePointsRemoved()
        {
            UpdateBuffer(m_pointSize);
        }

        private void UpdateBuffer(int newPointSize)
        {
            var data = m_buffer.BeginWrite<PointData>(0, newPointSize);
            var info = new LeafUpdateBufferInfo(PointCloudTree.GetBlockTreeDataList(), PointCloudTree.ModifiedBlocks, data, m_rowIndexBuffer, m_pointSize);
            LeafBufferUpdater.UpdateBufferDataUpdate(info);
            info.UsedBufferSize = LeafBufferUpdater.UpdateBufferRemoveUpdate(info);
            m_pointSize = LeafBufferUpdater.UpdateBufferAddUpdate(info);
            m_buffer.EndWrite<PointData>(newPointSize);
            PointCloudTree.ClearModifiedBlockList();
            m_leafMaterial.SetBuffer(m_shaderPointBufferName, m_buffer);
            
        }

        public void SavePointsAsPly(string filePath)
        {
            var data = m_buffer.BeginWrite<PointData>(0, m_pointSize);
            PlyImporterExporter.Export(filePath, data, m_pointSize);
            m_buffer.EndWrite<PointData>(m_pointSize);
        }

        private void UpdateBufferOnlyUpdate()
        {
            var data = m_buffer.BeginWrite<PointData>(0, m_pointSize);
            var info = new LeafUpdateBufferInfo(PointCloudTree.GetBlockTreeDataList(), PointCloudTree.ModifiedBlocks, data, m_rowIndexBuffer, m_pointSize);
            LeafBufferUpdater.UpdateBufferDataUpdate(info);
            m_buffer.EndWrite<PointData>(m_pointSize);
            PointCloudTree.ClearModifiedBlockList();
            m_leafMaterial.SetBuffer(m_shaderPointBufferName, m_buffer);
        }
        private void UpdateAllBufferOnlyUpdate()
        {
            var data = m_buffer.BeginWrite<PointData>(0, m_pointSize);
            var info = new LeafUpdateBufferInfo(PointCloudTree.GetBlockTreeDataList(), PointCloudTree.ModifiedBlocks, data, m_rowIndexBuffer, m_pointSize);
            LeafBufferUpdater.UpdateAllBufferDataUpdate(info);
            m_buffer.EndWrite<PointData>(m_pointSize);
            m_leafMaterial.SetBuffer(m_shaderPointBufferName, m_buffer);
        }
        void Update() {
            if(transform.hasChanged) {
                m_leafMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
                transform.hasChanged = false;
            }
            m_leafMaterial.SetInt(m_shaderPointTypeName, m_pointType);
            m_leafMaterial.SetFloat(m_shaderShowAllPointsName, ShowAllPoints ? 1.0f : 0.0f);
            m_leafMaterial.SetVector(m_shaderPlaneDirectionName, PlaneDirection);
            m_leafMaterial.SetVector(m_shaderPlanePositionName, PlanePosition);
            m_leafMaterial.SetFloat(m_shaderEnableCullingPlane, EnableCullingPlane ? 1.0f : 0.0f);
            m_leafMaterial.SetInt(m_shaderHeatmapOverlayPointTypeName, m_heatmapPointType);
            m_leafMaterial.SetFloat(m_shaderHeatmapOverlayThresholdName, m_heatmapThreshold);
            m_leafMaterial.SetFloat(m_shaderShowHeatmapGradientThresholdName, m_showHeatmapGradient ? 1.0f : 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCameraInValid()
        {
            var camera = Camera.current;
            if(camera == null) return false;
            return (camera.cullingMask & (1 << gameObject.layer)) == 0 || camera.name == "Preview Scene Camera";
        }

#if UNITY_EDITOR
        void OnGUI() {
            if(!RenderGUI) return;
            GUILayout.BeginArea(new Rect(0,15,Screen.width, Screen.height - 100));
            GUILayout.Label($"PointSize: {m_pointSize}");
            if(PointCloudTree != null) {
                GUILayout.Label($"Block Count: {PointCloudTree.GetBlockTreeDataList().Count}");
            }
            GUILayout.EndArea();
        }
#endif
    }
}