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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Interaction;
using RGS.Simulation;
using RGS.UI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static RootSelectPointRendererFeature;

namespace RGS.Models
{

    [StructLayout(LayoutKind.Sequential)]
    public struct PCRootPoint
    {
        public float3 Position;
        public long AgentUniqueId;
        public float TimeStamp;
    }

    public class PlantSeedModel : MonoBehaviour
    {
        public const int RenderPointsStride = 6;
        public bool SimulateResourceCost = false;
        public Guid Identifier {get;} = Guid.NewGuid();
        public string Axiom => m_plantConfiguration.Axiom;
        public string DisplayName => m_plantConfiguration.PlantName;
        public PlantConfiguration PlantConfiguration => m_plantConfiguration;
        public List<Tuple<Color,int>> RootTypeColorList => m_rootTypeColorList;
        [SerializeField] private PlantConfiguration m_plantConfiguration;
        public Bounds BoundingBox => m_boundingBox;
        private Bounds m_boundingBox;
        private int m_rootTypeCount;
        private NativeList<float> m_rootLengthDataOverTime;
        private NativeList<float> m_rootNutrientDataOverTime;
        private NativeList<float> m_rootLengthTimeData;
        public NativeList<RootPointData> RootPoints => m_rootPoints;
        private NativeList<RootPointData> m_rootPoints;
        private NativeList<PCRootPoint> m_pcRootPoints;
        public NativeList<NutrientRootPointData> NutrientRootPointDataList => m_nutrientRootPointDataList;
        private NativeList<NutrientRootPointData> m_nutrientRootPointDataList;
        private NativeList<NutrientRootPointData> m_nutrientRootPointsToBeAdded;
        public List<RootSegment> RootSegments => m_segments;
        private List<RootSegment> m_segments;
        private Vector2 m_segmentsMinMax;
        private Dictionary<int, float> m_rootLengthByType;
        public List<NutrientModel> PlantNutrients => m_plantNutrients;
        private List<NutrientModel> m_plantNutrients;
        public NativeList<int> ModifiedBlocksAndLeafs;
        private NativeList<float> m_renderingPoints;
        public NativeArray<float> NutrientBalanceValues => m_nutrientBalanceValues;
        public NativeArray<float> m_nutrientBalanceValues;
        public NativeArray<float> m_nutrientBaseCapacity;
        public NativeArray<float> NutrientCapacity => m_nutrientCapacity;
        private NativeArray<float> m_nutrientCapacity;
        public NativeArray<int> NutrientIndexMapping => m_nutrientIndexMapping;
        public NativeArray<int> m_nutrientIndexMapping;
        public float MinAge => m_minAge;
        private float m_minAge;
        public float MaxAge => m_maxAge;
        private float m_maxAge;
        private bool m_isSelected;
        private float m_currentDepth;
        public float3 Origin => m_origin;
        private Vector3 m_origin;
        private List<Tuple<Color,int>> m_rootTypeColorList;
        private float m_initTime;
        public List<long> AxiomAgentUniqueIds => m_axiomAgentUniqueIds;
        private List<long> m_axiomAgentUniqueIds;
        private float m_nutrientOverlapRadius;
        private ComputeBuffer m_rootSelectPointsBuffer;
        private int m_usedSize;
        private const int MAX_BUFFER_SIZE = 100000;
        private const int INIT_BUFFER_SIZE = 4096;
        private const int INIT_TRACKING_SIZE = 4096;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private bool m_isSetup;
        void Awake()
        {
            m_minAge = float.MaxValue;
            m_maxAge = float.MinValue;
            m_nutrientOverlapRadius = RGSConfiguration.Get().NutrientOverlapRadius;
            if (m_plantConfiguration != null)
            {
                Setup();
            }
            m_segmentsMinMax = new Vector2(float.MaxValue, float.MinValue);
        }
        public void Setup()
        {
            if(m_isSetup) return;
            m_isSetup = true;
            m_rootSelectPointsBuffer = new ComputeBuffer(MAX_BUFFER_SIZE, 24, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            m_usedSize = 0;
            m_initTime = Time.time;
            m_segments = new List<RootSegment>();
            m_axiomAgentUniqueIds = new List<long>();

            m_rootLengthByType = new Dictionary<int, float>();
            m_boundingBox = new Bounds(transform.position, Vector3.zero);
            m_rootPoints = new NativeList<RootPointData>(INIT_BUFFER_SIZE, Allocator.Persistent);
            m_pcRootPoints = new NativeList<PCRootPoint>(MAX_BUFFER_SIZE, Allocator.Persistent);
            m_nutrientRootPointDataList = new NativeList<NutrientRootPointData>(INIT_BUFFER_SIZE, Allocator.Persistent);
            m_nutrientRootPointsToBeAdded = new NativeList<NutrientRootPointData>(INIT_BUFFER_SIZE, Allocator.Persistent);
            m_renderingPoints = new NativeList<float>(INIT_BUFFER_SIZE*RenderPointsStride, Allocator.Persistent);
            ModifiedBlocksAndLeafs = new NativeList<int>(1024, Allocator.Persistent);
            m_uiMediator.OnSelectionChanged.AddListener(HandlePlantSelectionChanged);
            m_origin = transform.position;
            m_plantNutrients = new List<NutrientModel>();
            SeedNutrient[] seedNutrients = m_plantConfiguration.SeedNutrients;
            for (int i = 0; i < m_plantConfiguration.RequiredNutrients.Length; i++)
            {
                float initialValue = 0.0f;
                foreach (var seedNutrient in seedNutrients)
                {
                    if(seedNutrient.PointTypeIndex == m_plantConfiguration.RequiredNutrients[i].PointTypeIndex)
                    {
                        initialValue = seedNutrient.InitialValue;
                    }
                }
                m_plantNutrients.Add(new NutrientModel(m_plantConfiguration.RequiredNutrients[i], initialValue));
            }
            m_nutrientIndexMapping = new NativeArray<int>(RGSConfiguration.Get().PointTypesCount, Allocator.Persistent);
            for (int i = 0; i < m_nutrientIndexMapping.Length; i++)
            {
                m_nutrientIndexMapping[i] = -1;
            }
            m_nutrientBalanceValues = new NativeArray<float>(m_plantNutrients.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_nutrientBaseCapacity = new NativeArray<float>(m_plantNutrients.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_nutrientCapacity = new NativeArray<float>(m_plantNutrients.Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < m_nutrientBalanceValues.Length; i++)
            {
                m_nutrientBalanceValues[i] = m_plantNutrients[i].PlantAvailableValue;
                m_nutrientBaseCapacity[i] = m_plantNutrients[i].PlantAvailableValue;
            }
            for (int i = 0; i < m_plantNutrients.Count; i++)
            {
                m_nutrientIndexMapping[m_plantNutrients[i].PointTypeIndex] = i;
            }
            UpdatePlantCapacity();
        }
        private void UpdatePlantCapacity()
        {
            int segmentCount = m_segments.Count;

            for (int i = 0; i < m_plantNutrients.Count; i++)
            {
                m_nutrientCapacity[i] = m_nutrientBaseCapacity[i] + (segmentCount * m_plantNutrients[i].CapacityPerSegment);
            }
        }
        private void UpdateValues()
        {
            for (int i = 0; i < m_plantNutrients.Count; i++)
            {
                m_plantNutrients[i].PlantAvailableValue = m_nutrientBalanceValues[i];
            }
        }
        public void LogBalance()
        {
            Debug.Log($"Balance Values");
            for (int i = 0; i < m_nutrientBalanceValues.Length; i++)
            {
                Debug.Log($"P:{m_nutrientBalanceValues[i]}");
            }
        }
        public void SetEmergenceTime(float time)
        {
            m_initTime = time;
        }
        public void SetPlantConfiguration(PlantConfiguration plantConfiguration)
        {
            m_plantConfiguration = plantConfiguration;
        }
        public void SetupRootTypes(List<Tuple<Color,int>> rootTypeColorList)
        {
            m_rootTypeCount = rootTypeColorList.Count;
            m_rootLengthDataOverTime = new NativeList<float>(INIT_TRACKING_SIZE * m_rootTypeCount, Allocator.Persistent);
            m_rootNutrientDataOverTime = new NativeList<float>(INIT_TRACKING_SIZE * m_rootTypeCount, Allocator.Persistent);
            m_rootLengthTimeData = new NativeList<float>(INIT_TRACKING_SIZE, Allocator.Persistent);
            m_rootTypeColorList = rootTypeColorList;
        }
        public void AddRootTypes(List<Tuple<Color,int>> rootTypeColorList)
        {
            int lastCount = m_rootTypeColorList.Count;
            int lastDataCount = m_rootLengthDataOverTime.Length / lastCount;
            m_rootTypeColorList.AddRange(rootTypeColorList);
            int newDataCount = lastDataCount * m_rootTypeColorList.Count;
            NativeList<float> nextData = new NativeList<float>(newDataCount, Allocator.Persistent);
            nextData.Length = newDataCount;
            for (int i = 0; i < lastDataCount; i++)
            {
                for (int j = 0; j < m_rootTypeColorList.Count; j++)
                {
                    if(j < lastCount)
                    {
                        nextData[i * m_rootTypeColorList.Count + j] = m_rootLengthDataOverTime[i * lastCount + j];
                    }else {
                        nextData[i * m_rootTypeColorList.Count + j] = 0;
                    }
                }
            }
            m_rootLengthDataOverTime.Dispose();
            m_rootLengthDataOverTime = nextData;
            UpdateTrackingVisuals();
        }
        private void HandlePlantSelectionChanged(Guid selection)
        {
            m_isSelected = selection.Equals(Identifier);
            if(m_isSelected)
            {
                m_uiMediator.OnPlantDepthChange.Invoke(m_currentDepth);
                m_uiMediator.OnRootLengthChange.Invoke(GetRootLengthSum());
                m_uiMediator.OnRootSegmentsChange.Invoke(m_segments.Count);                
            }
        }
        public void UpdateRootPointSelectionBuffer()
        {
            RootSelectPointRendererFeature.Instance.SetBuffer(m_rootSelectPointsBuffer, m_usedSize);
        }
        public void AddRootLength(float value, int index)
        {
            if(m_rootLengthByType.ContainsKey(index))
            {
                m_rootLengthByType[index] += value;
            } else {
                m_rootLengthByType.Add(index, 0.0f);
            }
            if (m_isSelected) m_uiMediator.OnRootLengthChange.Invoke(GetRootLengthSum());
        }
        public void UpdateNutrientRootPoints()
        {
            m_nutrientRootPointDataList.AddRange(m_nutrientRootPointsToBeAdded);
            m_nutrientRootPointsToBeAdded.Clear();
        }

        public void AddPCRootPoint(float3 position, long uniqueAgentId, float m_currentSimulationTime)
        {
            m_pcRootPoints.Add(new PCRootPoint() {
                Position = position,
                AgentUniqueId = uniqueAgentId,
                TimeStamp = m_currentSimulationTime
            });
        }
        public void ExtendBounds(Vector3 point, long agentUniqueId, int rootType, float radius, float encodedColor, float currentSimulationTime)
        {
            int parentIndex = GetLastPointWithUniqueIdIndex(agentUniqueId);
            if (parentIndex != -1) {
                AddNewSegment(m_rootPoints[parentIndex].Position, point, rootType, agentUniqueId, currentSimulationTime, radius, encodedColor);
            }
            float pointAge = currentSimulationTime - m_initTime;
            m_rootPoints.Add(new RootPointData(point, pointAge, parentIndex, rootType, agentUniqueId));
            m_nutrientRootPointsToBeAdded.Add(new NutrientRootPointData(point, radius + m_nutrientOverlapRadius, 0.0f, RGSConfiguration.Get().RootWaterCapacityPerRadius));
            m_renderingPoints.Add(point.x);
            m_renderingPoints.Add(point.y);
            m_renderingPoints.Add(point.z);
            m_renderingPoints.Add(radius);
            m_renderingPoints.Add(encodedColor);
            m_renderingPoints.Add(pointAge);
            AddSelectPoint(point, pointAge, radius);
            m_minAge = m_minAge > pointAge ? pointAge : m_minAge;
            m_maxAge = m_maxAge < pointAge ? pointAge : m_maxAge;
            m_boundingBox.Encapsulate(point);
            float depth = Mathf.Abs(point.y - m_origin.y);
            if (depth > m_currentDepth) {
                m_currentDepth = depth;
            }
        }
        public void PostExtendBoundsPlottingChange()
        {
            if (m_isSelected) {
                m_uiMediator.OnPlantDepthChange.Invoke(m_currentDepth);
                m_uiMediator.OnRootSegmentsChange.Invoke(m_segments.Count);
                m_uiMediator.OnRootDensityPlottingChange.Invoke(m_segments, m_rootTypeColorList, m_segmentsMinMax);
            }
        }
        public void RemoveCutPoints(NativeList<int> cutPoints, float toleranceRadius, List<long> idList, float time)
        {
            cutPoints.Sort();
            m_currentDepth = 0.0f;
            for (int i = 0; i < cutPoints.Length; i++)
            {
                int index = cutPoints[i] - i;
                m_rootPoints.RemoveAt(index);
                m_nutrientRootPointDataList.RemoveAt(index);
                m_renderingPoints.RemoveRange(index * RenderPointsStride, RenderPointsStride);
            }

            m_minAge = float.MaxValue;
            m_maxAge = float.MinValue;
            m_boundingBox = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < m_rootPoints.Length; i++)
            {
                m_boundingBox.Encapsulate(m_rootPoints[i].Position);
                float depth = Mathf.Abs(m_rootPoints[i].Position.y - m_origin.y);
                if (depth > m_currentDepth) {
                    m_currentDepth = depth;
                }
                float pointAge = m_rootPoints[i].TimeStamp;
                m_minAge = m_minAge > pointAge ? pointAge : m_minAge;
                m_maxAge = m_maxAge < pointAge ? pointAge : m_maxAge;
            }
            
            var array = m_rootSelectPointsBuffer.BeginWrite<RootSelectPointData>(0 , m_renderingPoints.Length / RenderPointsStride);

            for (int i = 0; i < m_renderingPoints.Length / RenderPointsStride; i++)
            {
                float3 position = new float3(m_renderingPoints[i * RenderPointsStride], m_renderingPoints[i * RenderPointsStride + 1], m_renderingPoints[i * RenderPointsStride + 2]);
                array[i] = new RootSelectPointData(position, new float3(i + 1, m_renderingPoints[i * RenderPointsStride + 5], m_renderingPoints[i * RenderPointsStride + 3]));
            }
            m_rootSelectPointsBuffer.EndWrite<RootSelectPointRendererFeature.RootSelectPointData>(m_renderingPoints.Length / RenderPointsStride);
            m_usedSize = m_renderingPoints.Length / RenderPointsStride;
            
            for (int i = 0; i < m_segments.Count;)
            {
                if(idList.Contains(m_segments[i].UniqueAgentId) && m_segments[i].EmergenceTime > time ){
                    m_segments.RemoveAtSwapBack(i);
                }else {
                    i++;
                }
            }
            
            UpdatePlantCapacity();
            m_rootLengthByType.Clear();

            for (int i = 0; i < m_segments.Count;i++)
            {
                if(m_rootLengthByType.ContainsKey(m_segments[i].RootType))
                {
                    m_rootLengthByType[m_segments[i].RootType] += m_segments[i].Length;
                } else {
                    m_rootLengthByType.Add(m_segments[i].RootType, m_segments[i].Length);
                }
            }

            m_uiMediator.OnPlantDepthChange.Invoke(m_currentDepth);
            m_uiMediator.OnRootLengthChange.Invoke(GetRootLengthSum());
            m_uiMediator.OnRootSegmentsChange.Invoke(m_segments.Count);
            m_uiMediator.OnRootDensityPlottingChange.Invoke(m_segments, m_rootTypeColorList, m_segmentsMinMax);
        }

        public RootPointData GetCuttingPoint(int cuttingIndex)
        {
            cuttingIndex = Mathf.Clamp(cuttingIndex, 0, m_usedSize-1);
            return m_rootPoints[cuttingIndex];
        }

        public void HighlightPossibleCut(List<long> idList, float time, ComputeBuffer m_selectedRootIndexBuffer, int MAX_BUFFER_SIZE)
        {
            HighlightRootPointCutRendererFeature.Instance.SetBuffer(m_rootSelectPointsBuffer, m_usedSize);
            int selectedRootIndicesCount = 0;
            var buffer = m_selectedRootIndexBuffer.BeginWrite<int>(0, Mathf.Min(m_rootPoints.Length, MAX_BUFFER_SIZE));
            int index = 0;
            for (int i = 0; i < m_rootPoints.Length; i++)
            {
                if(idList.Contains(m_rootPoints[i].AgentUniqueId)){
                    buffer[index++] = i;
                }
            }
            selectedRootIndicesCount = Mathf.Min(index, MAX_BUFFER_SIZE);
            m_selectedRootIndexBuffer.EndWrite<int>(selectedRootIndicesCount);
            HighlightRootPointCutRendererFeature.Instance.SetTime(time);
            HighlightRootPointCutRendererFeature.Instance.SetSelectedRootIndexBuffer(m_selectedRootIndexBuffer, selectedRootIndicesCount);
            HighlightRootPointCutRendererFeature.Instance.SetActive(true);
        }
        public NativeList<float3> GetSelectedRootCutPoints(List<long> idList, float time, out NativeList<int> cutPointIndices, out Bounds bounds)
        {
            NativeList<float3> tmpBuffer = new NativeList<float3>(m_rootPoints.Length, Allocator.TempJob);
            cutPointIndices = new NativeList<int>(m_rootPoints.Length, Allocator.TempJob);
            bounds = new Bounds();
            for (int i = 0; i < m_rootPoints.Length; i++)
            {
                if(idList.Contains(m_rootPoints[i].AgentUniqueId) && m_rootPoints[i].TimeStamp > time ){
                    cutPointIndices.Add(i);
                }
            }

            for (int i = 0; i < m_pcRootPoints.Length; )
            {
                if(idList.Contains(m_pcRootPoints[i].AgentUniqueId) && m_pcRootPoints[i].TimeStamp > time ){
                    tmpBuffer.Add(m_pcRootPoints[i].Position);
                    bounds.Encapsulate(m_pcRootPoints[i].Position);
                    m_pcRootPoints.RemoveAtSwapBack(i);
                } else {
                    i++;
                }
                
            }
            
            return tmpBuffer;
        }
        private void AddSelectPoint(Vector3 position, float pointAge, float radius)
        {
            if(m_usedSize >= MAX_BUFFER_SIZE) return;
            var array = m_rootSelectPointsBuffer.BeginWrite<RootSelectPointData>(m_usedSize,1);
            array[0] = new RootSelectPointData(position, new float3(m_usedSize + 1, pointAge, radius));
            m_usedSize++;
            m_rootSelectPointsBuffer.EndWrite<RootSelectPointRendererFeature.RootSelectPointData>(1);
        }
        public NativeList<float> GetRenderingPointData()
        {
            return m_renderingPoints;
        }
        void Update()
        {
            if (m_isSelected) {
                UpdateValues();
                float maxValue = m_nutrientCapacity[0];
                for (int i = 1; i < m_nutrientCapacity.Length; i++)
                {
                    if(m_nutrientCapacity[i] > maxValue)
                    {
                        maxValue = m_nutrientCapacity[i];
                    }   
                }
                m_uiMediator.OnPlantNutrientChange.Invoke(m_plantNutrients, maxValue);
            }
        }
        public void UpdateDataTracking(float currentSimulationTime)
        {
            if(!m_rootLengthDataOverTime.IsCreated) return;
            int startIndex = m_rootLengthDataOverTime.Length;
            m_rootLengthDataOverTime.ResizeUninitialized(startIndex + m_rootTypeColorList.Count);
            int nutrientStartIndex = m_rootNutrientDataOverTime.Length;
            m_rootNutrientDataOverTime.ResizeUninitialized(nutrientStartIndex + m_plantNutrients.Count);
            float time = currentSimulationTime - m_initTime;
            m_rootLengthTimeData.Add(time);
            for (int i = 0; i < m_plantNutrients.Count; i++)
            {
                m_rootNutrientDataOverTime[nutrientStartIndex + i]  = m_plantNutrients[i].PlantAvailableValue;
            }
            for (int i = 0; i < m_rootTypeColorList.Count; i++)
            {
                int agentTypeIndex = m_rootTypeColorList[i].Item2;
                float lengthValue = m_rootLengthByType.ContainsKey(agentTypeIndex) ? m_rootLengthByType[agentTypeIndex] : 0.0f;
                m_rootLengthDataOverTime[startIndex + i] = lengthValue;
            }
        }
        public void UpdateTrackingVisuals()
        {
            if (m_isSelected) {
                m_uiMediator.OnRootNutrientPlottingChange.Invoke(m_rootNutrientDataOverTime, m_rootLengthTimeData, m_plantNutrients.Select(x => x.NutrientDisplayName).ToList());
                m_uiMediator.OnRootLengthPlottingChange.Invoke(m_rootLengthDataOverTime.AsArray(), m_rootLengthTimeData.AsArray(), m_rootTypeColorList);
                m_uiMediator.OnRootDensityPlottingChange.Invoke(m_segments, m_rootTypeColorList, m_segmentsMinMax);
            }
        }
        private void AddNewSegment(float3 start, float3 end, int rootType, long uniqueAgentId, float currentSimulationTime, float radius, float encodedColor)
        {
            float3 origin = m_origin;
            RootSegment segment = new RootSegment(start - origin, end - origin, rootType, uniqueAgentId, currentSimulationTime - m_initTime, radius, encodedColor);
            if(segment.Center.y < m_segmentsMinMax.x)
            {
                m_segmentsMinMax.x = segment.Center.y;
            }
            if(segment.Center.y > m_segmentsMinMax.y)
            {
                m_segmentsMinMax.y = segment.Center.y;
            }
            m_segments.Add(segment);
            UpdatePlantCapacity();
        }
        private int GetLastPointWithUniqueIdIndex(long uniqueId)
        {
            for (int i = m_rootPoints.Length - 1; i >= 0; i--)
            {
                if(m_rootPoints[i].AgentUniqueId == uniqueId) {
                    return i;
                }
            }
            return -1;
        }
        private float GetRootLengthSum()
        {
            float sum = 0.0f;
            foreach (var length in m_rootLengthByType)
            {
                sum += length.Value;
            }
            return sum;
        }
        private void OnDrawGizmosSelected() 
        {
            Gizmos.DrawWireCube(m_boundingBox.center, m_boundingBox.size);
        }
        private void OnDestroy()
        {
            m_uiMediator.OnSelectionChanged.RemoveListener(HandlePlantSelectionChanged);
            m_rootLengthDataOverTime.Dispose();
            m_rootNutrientDataOverTime.Dispose();
            m_rootLengthTimeData.Dispose();
            m_rootPoints.Dispose();
            m_renderingPoints.Dispose();
            m_nutrientRootPointDataList.Dispose();
            m_nutrientIndexMapping.Dispose();
            m_nutrientBalanceValues.Dispose();
            ModifiedBlocksAndLeafs.Dispose();
            m_nutrientRootPointsToBeAdded.Dispose();
            m_nutrientCapacity.Dispose();
            m_nutrientBaseCapacity.Dispose();
            m_rootSelectPointsBuffer.Dispose();
            m_pcRootPoints.Dispose();
        }

        public void AddResourcesFromShoot(float deltaTime)
        {
            if(!SimulateResourceCost) return;
            Thread.MemoryBarrier();
            for (int i = 0; i < m_plantConfiguration.ShootNutrientModel.Length; i++)
            {
                int index = m_nutrientIndexMapping[m_plantConfiguration.ShootNutrientModel[i].PointTypeIndex];
                if(index != -1)
                {
                    if(m_nutrientBalanceValues[index] >= m_nutrientCapacity[index]) continue;
                    m_nutrientBalanceValues[index] = math.max(m_plantConfiguration.ShootNutrientModel[i].ValuePerTimeStep * deltaTime + m_nutrientBalanceValues[index], 0.0f);
                }
            }
            Thread.MemoryBarrier();
        }

        public bool CheckAndRemoveResources(int agentType, UnevenSequentialDataArray<int2, RootAgentRequiredResource> agentsRequiredResources)
        {
            if(!SimulateResourceCost) return true;
            Thread.MemoryBarrier();
            // check if possible
            int mappingIndex;
            RootAgentRequiredResource rootAgentRequiredResource;
            int startIndex = agentsRequiredResources.DataStartIndexArray[agentType].x;
            for (int i = 0; i < agentsRequiredResources.DataStartIndexArray[agentType].y; i++)
            {
                rootAgentRequiredResource = agentsRequiredResources.SequentialDataArray[startIndex + i];
                mappingIndex = m_nutrientIndexMapping[rootAgentRequiredResource.ResourceType];
                if (mappingIndex == -1) 
                {
                    Debug.LogWarning("Plant cannot map required resource!");
                    Thread.MemoryBarrier();
                    return false;
                }
                if( m_nutrientBalanceValues[mappingIndex] < m_plantNutrients[mappingIndex].PlantMinValue || m_nutrientBalanceValues[mappingIndex] < rootAgentRequiredResource.RequiredAmount)
                {
                    Thread.MemoryBarrier();
                    return false;
                }
            }
            for (int i = 0; i < agentsRequiredResources.DataStartIndexArray[agentType].y; i++)
            {
                rootAgentRequiredResource = agentsRequiredResources.SequentialDataArray[startIndex + i];
                mappingIndex = m_nutrientIndexMapping[rootAgentRequiredResource.ResourceType];
                m_nutrientBalanceValues[mappingIndex] -= rootAgentRequiredResource.RequiredAmount;
            }

            Thread.MemoryBarrier();
            // remove values
            return true;
        }

    }
    
}
