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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PCMTool.Tree;
using RGS.Models;
using RGS.Simulation;
using RGS.UI;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Rendering
{

    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderRootSegmentData
    {
        public float4 Start;
        public float4 End;
        public float3 RadiusLengthColor;
        public float3 NormalizedDirection;
        public ShaderRootSegmentData ExtendToSegment(ShaderRootSegmentData other)
        {
            this.End = other.End;
            this.NormalizedDirection = math.normalize(this.End.xyz - this.Start.xyz);
            this.RadiusLengthColor.y = math.length(this.End.xyz - this.Start.xyz);
            return this;
        }
    }
    public class SphereTracingPointsRenderer : MonoBehaviour
    {
        public float SegmentCollapseAngleDelta = 0.15f;
        [SerializeField] private Camera m_rootRendererCamera;
        [SerializeField] private int rootPointCount;
        [SerializeField] private int m_rootPointRestrictionLimit;
        [SerializeField] private bool m_renderSegments;
        private readonly UIMediator m_uiButtonMediator = UIMediator.Get();
        private ComputeBuffer m_rootComputeBuffer;
        private PlantSeedModel m_activeSeedModel;
        private int m_counter;
        private bool m_restrictRootsPoints = true;
        private bool m_useBgColor;
        private Color m_bgColor;
        private Texture2D m_targetImage;
        private void Awake() {
            m_targetImage = new Texture2D(m_rootRendererCamera.targetTexture.width, m_rootRendererCamera.targetTexture.height, TextureFormat.RGB24, false);
            m_uiButtonMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_uiButtonMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
            m_uiButtonMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
            m_uiButtonMediator.OnRenderBgColorChange.AddListener(HandleBgColorChange);
            m_counter = 0;
        }
        void Start()
        {
            m_uiButtonMediator.OnEventToggleClicked.Invoke(ToggleEventType.TOGGLE_CAPSULE_RENDERING, m_renderSegments);
        }
        private void HandleBgColorChange(Color color, bool value)
        {
            m_useBgColor = value;
            m_bgColor = color;
        }

        private void HandleEventToggleClicked(ToggleEventType type, bool value)
        {
            if(type == ToggleEventType.TOGGLE_AGE_RENDERING)
            {
                SphereTracingRenderPassFeature.Instance.RenderRootAgeGradent(value);
            }
            if(type == ToggleEventType.TOGGLE_RESTRICT_RENDERING)
            {
                m_restrictRootsPoints = value;
            }
            if(type == ToggleEventType.TOGGLE_CAPSULE_RENDERING)
            {
                m_renderSegments = value;
            }
        }

        private void HandleSelectionChanged(Guid guid)
        {
            PlantSeedModel[] seedModels = GameObject.FindObjectsOfType<PlantSeedModel>();
            foreach (var seedModel in seedModels)
            {
                if(seedModel.Identifier.Equals(guid))
                {
                    m_activeSeedModel = seedModel;
                }
            }
        }

        private ShaderRootSegmentData[] GetCollapsedSegments(float angleDelta)
        {
            float3 origin = m_activeSeedModel.Origin;
            List<ShaderRootSegmentData> result = new List<ShaderRootSegmentData>();
            var idList = m_activeSeedModel.RootSegments.Select( segment => segment.UniqueAgentId).Distinct().ToList();
            foreach (var rootId in idList)
            {
                var segmentsByTime = m_activeSeedModel.RootSegments.Where(segment => segment.UniqueAgentId == rootId).OrderBy(segment => segment.EmergenceTime).ToList();
                if(segmentsByTime.Count <= 1){
                    if(segmentsByTime.Count > 0)
                    {
                        result.Add(segmentsByTime[0].GetShaderAsData(origin));
                    }
                }else {
                    ShaderRootSegmentData start = segmentsByTime[0].GetShaderAsData(origin);
                    float3 refDirection = start.NormalizedDirection;
                    for (int i = 1; i < segmentsByTime.Count; i++)
                    {
                        var data = segmentsByTime[i].GetShaderAsData(origin);
                        float angle = math.acos(math.dot(refDirection, data.NormalizedDirection));
                        if(angle <= angleDelta)
                        {
                            start = start.ExtendToSegment(data);
                        }else {
                            result.Add(start);
                            start = data;
                            refDirection = data.NormalizedDirection;
                        }
                    }
                    result.Add(start);
                }
            }
            Debug.Log($"Collapsed {m_activeSeedModel.RootSegments.Count} to {result.Count}");
            return result.ToArray();
        }

        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType != ButtonEventType.RENDER_ROOTS) return;
            if(m_activeSeedModel == null) return;
            if(m_activeSeedModel.GetRenderingPointData().Length == 0) return;
            if(m_renderSegments)
            {
                ShaderRootSegmentData[] segmentData = GetCollapsedSegments(SegmentCollapseAngleDelta);
                m_rootComputeBuffer = new ComputeBuffer(segmentData.Length, 14 * 4, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                rootPointCount = segmentData.Length;
                m_rootComputeBuffer.SetData(segmentData);
            } else {
                rootPointCount = m_activeSeedModel.GetRenderingPointData().Length / PlantSeedModel.RenderPointsStride;
                m_rootComputeBuffer = new ComputeBuffer(rootPointCount, PlantSeedModel.RenderPointsStride * 4, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
                m_rootComputeBuffer.SetData(m_activeSeedModel.GetRenderingPointData().ToArray());
            }
            SphereTracingRenderPassFeature.Instance.SetSegmentRenderKeyword(m_renderSegments);
            SphereTracingRenderPassFeature.Instance.SetBgColor(m_bgColor, m_useBgColor);
            SphereTracingRenderPassFeature.Instance.SetMinMaxAge(m_activeSeedModel.MinAge, m_activeSeedModel.MaxAge);
            SphereTracingRenderPassFeature.Instance.SetBounds(m_activeSeedModel.BoundingBox.min - (Vector3.one * 0.01f), m_activeSeedModel.BoundingBox.max + (Vector3.one * 0.001f));
            Render();
        }

        public void Render()
        {
            var currentRT = RenderTexture.active;
            RenderTexture.active = m_rootRendererCamera.targetTexture;
            UpdatePointBuffer(m_restrictRootsPoints ? Mathf.Min(rootPointCount, m_rootPointRestrictionLimit) : rootPointCount);
            m_rootRendererCamera.Render();

            m_targetImage.ReadPixels(new Rect(0, 0, m_rootRendererCamera.targetTexture.width, m_rootRendererCamera.targetTexture.height), 0, 0);
            m_targetImage.Apply();

            RenderTexture.active = currentRT;

            byte[] bytes = m_targetImage.EncodeToPNG();
            try
            {
                using (var fs = new FileStream(GetRenderImagePath(), FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            catch
            {
                Debug.LogWarning("Failed to write image!");
            }

            m_rootComputeBuffer.Dispose();
            Application.OpenURL(SessionInfo.GetSessionFolderPath());
        }
        
        private void UpdatePointBuffer(int count)
        {
            SphereTracingRenderPassFeature.Instance.SetPointBuffer(m_rootComputeBuffer, count);
        }

        private string GetRenderImagePath()
        {
            string fileName = $"{m_counter++}{m_activeSeedModel.DisplayName.Replace(' ', '-')}.png";
            string folderPath = SessionInfo.GetSessionFolderPath();
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(SessionInfo.GetSessionFolderPath(),fileName);
        }

        private void OnDestroy() {
            m_uiButtonMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_uiButtonMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
            m_uiButtonMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
            m_uiButtonMediator.OnRenderBgColorChange.RemoveListener(HandleBgColorChange);
            if(m_rootComputeBuffer != null && m_rootComputeBuffer.IsValid())
            {
                m_rootComputeBuffer.Dispose();
            }
        }
    }

}