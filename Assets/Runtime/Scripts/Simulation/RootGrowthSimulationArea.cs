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
using PCMTool.Tree;
using PCMTool.Tree.Query;
using RGS.Agents;
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Generator;
using RGS.Interaction;
using RGS.Jobs;
using RGS.Models;
using RGS.OverlapVolume;
using RGS.QueryJobScheduler;
using RGS.UI;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#if UNITY_EDITOR
using Unity.Profiling;
using UnityEditor;
#endif
using UnityEngine;

namespace RGS.Simulation
{

    /// <summary>
    /// Manages the simulation of root growth in the scene using swarm grammar.
    /// </summary>
    public class RootGrowthSimulationArea : MonoBehaviour
    {
#if UNITY_EDITOR
        public static bool RenderGUI = true;
#endif
        const int PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT = 64;
        const int PARALLELFOR_WATER_MOVEMENT_INNERLOOPBATCHCOUNT = 64;

#if UNITY_EDITOR 
#region ProfilingMarker
        static readonly ProfilerMarker s_NutrientAbsorbPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.NutrientAbsorb");
        static readonly ProfilerMarker s_SimulateAgentsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents");
        static readonly ProfilerMarker s_SimulateAgentsWaterPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Water");
        static readonly ProfilerMarker s_QueryPointsInWaterAgentsRadiusPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Water.PointsQuery");
        static readonly ProfilerMarker s_CalculateWaterAgentsDirectionsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Water.Direction");
        static readonly ProfilerMarker s_SimulateWaterAgentsMovementPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Water.Movement");
        static readonly ProfilerMarker s_QueryPointsInAgentsRadiusPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.PointsQuery");
        static readonly ProfilerMarker s_QueryPointsInAgentsRadiusOverlapPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.PointsQuery.Overlap");
        static readonly ProfilerMarker s_CalculateRootScalingFactorsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.RootScalingFactors");
        static readonly ProfilerMarker s_CalculateAgentsDirectionsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Direction");
        static readonly ProfilerMarker s_CalculateAgentsAccelerationPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Acceleration");
        static readonly ProfilerMarker s_SimulateAgentsMovementPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.SimulateAgents.Movement");
        static readonly ProfilerMarker s_TrackDistancesPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.TrackDistances");
        static readonly ProfilerMarker s_PlacePointsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.PlacePoints");
        static readonly ProfilerMarker s_PlacePointsExtendPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.PlacePoints.Extend");
        static readonly ProfilerMarker s_PlacePointsLeafModifyPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.PlacePoints.LeafModify");
        static readonly ProfilerMarker s_WaterEvaporationPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.WaterEvaporation");
        static readonly ProfilerMarker s_ApplyProductionRulesPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.ApplyProductionRules");
        static readonly ProfilerMarker s_ApplyProductionRulesPart1PerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.ApplyProductionRules.CalculateNewBranching");
        static readonly ProfilerMarker s_ApplyProductionRulesPart2PerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.ApplyProductionRules.CalculateBranching");
        static readonly ProfilerMarker s_ApplyProductionRulesPart3PerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.ApplyProductionRules.AddEmergingBranches");
        static readonly ProfilerMarker s_ApplyProductionRulesResizePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.ApplyProductionRules.Resize");
        static readonly ProfilerMarker s_NutrietntAbsorbRulesPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "RGS.NutrietntAbsorbtion");
#endregion
#endif
        public List<Tuple<long, long, float>> RootParentChildRelations => m_rootParentChildRelations;
        public int RootAgentCount {get; private set;}
        public int WaterAgentCount {get; private set;}
        private static long AgentUniqueIdCounter = 0;
        [SerializeField] private WaterSource m_waterSource;
        [SerializeField] private float m_waterAgentsKillHeight;
        [SerializeField] private RuntimeLeafModifier m_leafModifier;
        [SerializeField] private SoilEnvironmentGenerator m_soilEnvGenerator;
        private const int m_initialAgentAllocationCapacity = 128;
        private const int m_initialWaterAgentAllocationCapacity = 128;
        [SerializeField] private GizmosRenderOptions m_gizmosRenderOptions;
        [SerializeField] private RootSGConfiguration m_rootSGConfiguration;
        [SerializeField] private SelectableAgentsPool m_selectableAgentsPool;
        [SerializeField] private SelectableWaterAgentsPool m_selectableWaterAgentsPool;
        [SerializeField] private SimpleMagneticFieldModel[] m_magneticFieldModels;
        [Header("Debug display")]
        [SerializeField] private Mesh m_agentMesh;
        private List<PlantSeedModel> m_seedModels;
        private NativeList<RootSGScalingFactors> m_agentsScalingFactors;
        private NativeList<RootSGAgentData> m_agentsReadonly;
        private NativeList<RootSGAgentData> m_agentsWriteOnly;
        private NativeList<PointData> m_pointToBeAdded;
        private NativeList<int> m_agentsPastSegmentDistance;
        private NativeList<int> m_agentsWithNewPointsDistance;
        private NativeList<int> m_chemotropismPointTypesWithSign;
        private NativeList<float3> m_magnetotropismBounds;
        private List<Tuple<long, long, float>> m_rootParentChildRelations;
        private RootSGAgentTypeSOA m_rootSGAgentTypeSOA;
        private AgentScalingParametersSOA m_agentScalingParametersSOA;
        private UnevenSequentialDataList<int, float3> m_agentsDirectionTries; 
        private UnevenSequentialDataList<int2, LeafBody> m_agentsPointsInRadius;
        private NativeArray<float> m_precomputedRandomValues;
        private PointPlantAbsorbScheduler m_pointPlantAbsorbScheduler;
        private NativeList<QueuedRootSGAgentData> m_queuedRootBranches;
        private NativeList<PausedRootSGAgentData> m_pausedAgentsList;
        // Water //
        private PointWaterScheduler m_pointWaterScheduler;
        private NativeList<WaterAgentData> m_waterAgentsReadonly;
        private NativeList<WaterAgentData> m_waterAgentsWriteOnly;
        private UnevenSequentialDataList<int2, LeafBody> m_waterAgentsPointsInRadius;
        private NativeList<float3> m_waterAgentsDirectionTries; 
        private AgentTypePerceptionData m_waterAgentPerceptionData;
        // ----- //
        private int m_simSteps = 1;
        private int m_rootPointId;
        private int m_collisionPointId;
        private int m_halotropismPointId;
        private int m_soilPointId;
        private int m_harmfullMetalsPointId;
        // Time
        private float m_lastTime;
        private float m_currentSimulationTime;
        private float m_simulationTimeStep;
        private bool m_playSimulation;
        private long m_simUpdateCounter;
        private long m_nutrientPointSimStep;
        // rendering buffer
        private const int MAX_BUFFER_SIZE = 10000;
        private ComputeBuffer m_selectedRootIndexBuffer;
        // Events
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake()
        {
            m_selectedRootIndexBuffer = new ComputeBuffer(MAX_BUFFER_SIZE, 4, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            m_seedModels = new List<PlantSeedModel>();
            m_rootSGAgentTypeSOA = m_rootSGAgentTypeSOA.LoadData(m_rootSGConfiguration);
            m_agentScalingParametersSOA = m_agentScalingParametersSOA.LoadData(m_rootSGConfiguration);
            m_rootParentChildRelations = new List<Tuple<long, long, float>>();
            m_queuedRootBranches = new NativeList<QueuedRootSGAgentData>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            // root agents
            m_agentsReadonly = new NativeList<RootSGAgentData>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_agentsWriteOnly = new NativeList<RootSGAgentData>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_agentsScalingFactors = new NativeList<RootSGScalingFactors>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_pointToBeAdded = new NativeList<PointData>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_agentsPastSegmentDistance = new NativeList<int>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_agentsWithNewPointsDistance = new NativeList<int>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            m_agentsDirectionTries = new UnevenSequentialDataList<int, float3>();
            m_agentsPointsInRadius = new UnevenSequentialDataList<int2, LeafBody>();
            m_pausedAgentsList = new NativeList<PausedRootSGAgentData>(m_initialAgentAllocationCapacity, Allocator.Persistent);
            // water agents
            m_waterAgentsReadonly = new NativeList<WaterAgentData>(m_initialWaterAgentAllocationCapacity, Allocator.Persistent);
            m_waterAgentsWriteOnly = new NativeList<WaterAgentData>(m_initialWaterAgentAllocationCapacity, Allocator.Persistent);
            m_waterAgentsPointsInRadius = new UnevenSequentialDataList<int2, LeafBody>();
            m_waterAgentsDirectionTries = new NativeList<float3>(m_initialWaterAgentAllocationCapacity * m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries, Allocator.Persistent);
            //
            m_rootPointId = RGSConfiguration.Get().GetIndexOfPointType("Root");
            m_collisionPointId = RGSConfiguration.Get().GetIndexOfPointType("Obstacle");
            m_halotropismPointId = RGSConfiguration.Get().GetIndexOfPointType("NaCl");
            m_soilPointId = RGSConfiguration.Get().GetIndexOfPointType("Soil");
            m_harmfullMetalsPointId = RGSConfiguration.Get().GetIndexOfPointType("HarmfulMetals");
            m_precomputedRandomValues = new NativeArray<float>(1024, Allocator.Persistent);
            List<int> excludedPoints = new List<int>();
            for (int i = 0; i < RGSConfiguration.Get().PointTypes.Length; i++)
            {
                if(!RGSConfiguration.Get().PointTypes[i].IsAbsorbableByPlant)
                {
                    excludedPoints.Add(i);
                }
            }
            m_pointPlantAbsorbScheduler = new PointPlantAbsorbScheduler(m_leafModifier.Tree, m_soilPointId, excludedPoints.ToArray());
            m_pointWaterScheduler = new PointWaterScheduler(m_leafModifier.Tree, m_soilPointId, m_rootSGConfiguration.WaterAgentConfig);
            for (int i = 0; i < m_precomputedRandomValues.Length; i++)
            {
                m_precomputedRandomValues[i] = UnityEngine.Random.value;
            }
            m_simulationTimeStep = RGSConfiguration.Get().SimulationTimestep;
            m_nutrientPointSimStep = RGSConfiguration.Get().NutrientPointSimStep;
            m_currentSimulationTime = 0.0f;
            if(m_waterSource != null)
            {
                m_waterSource.Setup(m_currentSimulationTime, m_rootSGConfiguration.WaterAgentConfig);
            }
            m_chemotropismPointTypesWithSign = new NativeList<int>(RGSConfiguration.Get().PointTypesCount, Allocator.Persistent);
            foreach (var pointType in RGSConfiguration.Get().PointTypes)
            {
                if(pointType.CausePositiveChemotropism) {
                    m_chemotropismPointTypesWithSign.Add(RGSConfiguration.Get().GetIndexOfPointType(pointType.Identifier));
                }else if(pointType.CauseNegativeChemotropism)
                {
                    m_chemotropismPointTypesWithSign.Add(-RGSConfiguration.Get().GetIndexOfPointType(pointType.Identifier));
                }
            }
            m_waterAgentPerceptionData = new AgentTypePerceptionData(
                        math.radians(m_rootSGConfiguration.WaterAgentConfig.PerceptionAngle), 
                        math.radians(m_rootSGConfiguration.WaterAgentConfig.DeltaPerceptionAngleDeg), 
                        m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries,
                        math.PI * 0.75f);

            m_interactionMediator.OnApplyPartialDerivation.AddListener(HandlePartialDerivation);
            m_interactionMediator.OnCutRootAt.AddListener(HandleCutRootAt);
            m_interactionMediator.OnPreviewCutRootAt.AddListener(HandlePreviewCuttingAt);
            m_interactionMediator.OnMagneticFieldUpdate.AddListener(HandleMagnetUpdate);
        }

        void Start()
        {
            m_soilEnvGenerator.CreateEnvironment(m_leafModifier);
            m_magnetotropismBounds = new NativeList<float3>(m_magneticFieldModels.Length * 3, Allocator.Persistent);
            for (int i = 0; i < m_magneticFieldModels.Length; i++)
            {
                if(!m_magneticFieldModels[i].isActiveAndEnabled) continue;
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].GetBounds().min);
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].GetBounds().max);
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].FieldDirection);
            }
            
            SubscribeToEvents();

#if UNITY_EDITOR

        EditorApplication.playModeStateChanged += EndJobsOnPlayModeChange;
#endif
        }
#if UNITY_EDITOR
        private void EndJobsOnPlayModeChange(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.ExitingPlayMode)
            {
                EndWaterEvaporation();
                PlaceRootPointsInTree();
            }
        }
#endif
        
        /// <summary>
        /// Iterates through all magnetic field objects and updates bounds data in native list accordingly.
        /// </summary>
        private void HandleMagnetUpdate()
        {
            m_magnetotropismBounds.Clear();
            for (int i = 0; i < m_magneticFieldModels.Length; i++)
            {
                if(!m_magneticFieldModels[i].isActiveAndEnabled) continue;
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].GetBounds().min);
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].GetBounds().max);
                m_magnetotropismBounds.Add(m_magneticFieldModels[i].FieldDirection);
            }
        }

        /// <summary>
        /// Creates and sets up a new plant game object based on the given parameters.
        /// </summary>
        /// <param name="configuration"> plant configuration asset.</param>
        /// <param name="position"> planting position.</param>
        /// <returns> PlantSeedModel.</returns>
        public PlantSeedModel PlaceSeed(PlantConfiguration configuration, Vector3 position)
        {
            var instance = new GameObject($"Seed-{configuration.PlantName}");
            instance.transform.position = position;
            var plantSeedModelComponent = instance.AddComponent<PlantSeedModel>();
            plantSeedModelComponent.SetPlantConfiguration(configuration);
            plantSeedModelComponent.Setup();
            plantSeedModelComponent.SetEmergenceTime(m_currentSimulationTime);
            AddSeedModelAgents(plantSeedModelComponent);
            plantSeedModelComponent.SetupRootTypes(m_rootSGConfiguration.GetPossibleRootTypesForAxiom(plantSeedModelComponent.Axiom));
            return plantSeedModelComponent;
        }
        private void SubscribeToEvents()
        {
            m_uiMediator.OnPauseEvent.AddListener(HandlePauseSimulation);
            m_uiMediator.OnPlayEvent.AddListener(HandlePlaySimulation);
            m_uiMediator.OnFastForwardEvent.AddListener(HandleFastForward);
        }
        private void HandlePauseSimulation()
        {
            m_playSimulation = false;
            m_simSteps = 1;
            EndWaterEvaporation();
            PlaceRootPointsInTree();
            UpdateSelectableRootAgents();
            m_selectableWaterAgentsPool.UpdateAndShowAgents(m_waterAgentsReadonly, m_rootSGConfiguration.WaterAgentConfig);
        }
        private void UpdateSelectableRootAgents()
        {
            m_selectableAgentsPool.UpdateAndShowAgents(m_agentsReadonly, m_rootSGConfiguration, m_agentsScalingFactors, m_currentSimulationTime);
            m_selectableAgentsPool.ShowQueuedAgents(m_queuedRootBranches, m_rootSGConfiguration);
            m_selectableAgentsPool.ShowPausedAgents(m_pausedAgentsList, m_rootSGConfiguration);
        }
        private void HandlePlaySimulation()
        {
            m_playSimulation = true; 
            m_simUpdateCounter = 0;
            m_simSteps = 1;
            m_selectableAgentsPool.HideSelectableAgents();
            m_selectableWaterAgentsPool.HideSelectableAgents();
        }
        private void HandleFastForward()
        {
            m_playSimulation = true;
            m_simUpdateCounter = 0;
            m_simSteps = 10;
            m_selectableAgentsPool.HideSelectableAgents();
            m_selectableWaterAgentsPool.HideSelectableAgents();
        }

        /// <summary>
        /// Adds the plant seed model axiom agents with directions based on insertion angle.
        /// </summary>
        /// <param name="plantSeedModel"> plant configuration asset.</param>
        private void AddSeedModelAgents(PlantSeedModel plantSeedModel)
        {
            int plantId = m_seedModels.Count;
            m_seedModels.Add(plantSeedModel);
            float3 position = plantSeedModel.transform.position;
            for (int j = 0; j < plantSeedModel.Axiom.Length; j++)
            {
                int agentType = m_rootSGConfiguration.GetAgentTypeOfChar(plantSeedModel.Axiom[j]);
                //Debug.Log("Create agent:"+m_seedModels[i].Axiom[j] + " is index:"+agentType);
                if(agentType == -1) {
                    Debug.LogWarning($"Failed to create Agent: Axiom {plantSeedModel.Axiom[j]} does not exist in the rules!");
                    continue;
                }
                long nextUniqueId = AgentUniqueIdCounter++;
                plantSeedModel.AxiomAgentUniqueIds.Add(nextUniqueId);
                float3 startDirection = RGSMath.CalculateRootInsertionDirection(new float3(0.0f, -1.0f, 0.0f), 
                    Mathf.Clamp(m_rootSGConfiguration.RootSGAgents[agentType].RootInsertionAngle.GetValue(nextUniqueId, FloatStdDevType.ROOT_INSERTION_ANGLE), 0.0f, 180.0f), UnityEngine.Random.value * 360.0f);
                m_agentsReadonly.Add(RootSGAgentData.NewAgent(position, math.normalize(startDirection) * m_rootSGConfiguration.RootSGAgents[agentType].InitialVelocity, agentType, plantId, nextUniqueId, m_currentSimulationTime));
            }
            m_agentsWriteOnly.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            m_agentsScalingFactors.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            m_agentsWithNewPointsDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            m_agentsPastSegmentDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            RootAgentCount = m_agentsReadonly.Length;
        }

        /// <summary>
        /// Second part of simulation update. Ends nutrient absorbtion, places points in the pointcloud and updates it.
        /// If simulation is in play or fast-forward mode nutrient absorbtion and water evaporation jobs are started again.
        /// </summary>
        private void PlaceRootPointsInTree()
        {
#if UNITY_EDITOR
            using(s_NutrietntAbsorbRulesPerfMarker.Auto())
            {
#endif
                EndNutrientAbsorption();
#if UNITY_EDITOR
            }
#endif

            PointCloudDataUpdate();
            if(m_pointToBeAdded.Length != 0) {

                for (int i = 0; i < m_seedModels.Count; i++)
                {
                    m_seedModels[i].UpdateNutrientRootPoints();
                }
    #if UNITY_EDITOR
                using(s_PlacePointsLeafModifyPerfMarker.Auto())
                {
    #endif
                    m_leafModifier.PlacePoints(m_pointToBeAdded);
    #if UNITY_EDITOR
                }
    #endif
                m_pointToBeAdded.Clear();
            }

            if(!m_playSimulation) return;
            StartNutrientAbsorption();
            StartWaterEvaporation();
        }
        /// <summary>
        /// First part of the simulation update loop without without any profiling.
        /// </summary>
        private void TimeStepUpdate()
        {
            if(!m_playSimulation) return;
            EndWaterEvaporation();
            m_simUpdateCounter++;
            SimulateWaterAgents();
            SimulateRootAgents();
            //
            UpdateTracking();
            CalculatePlacePoints();
            //
            if(m_simUpdateCounter % m_nutrientPointSimStep != 0)
            {
                StartWaterEvaporation();
            }
            ApplyProductionRules();
            m_currentSimulationTime += m_simulationTimeStep;
        }
#if UNITY_EDITOR
        /// <summary>
        /// First part of the simulation update loop without with unity editor profiling.
        /// </summary>
        private void TimeStepUpdateProfiling()
        {
            using(s_WaterEvaporationPerfMarker.Auto())
            {
                EndWaterEvaporation();
            }
            if(!m_playSimulation) return;
            m_simUpdateCounter++;
            using(s_SimulateAgentsWaterPerfMarker.Auto())
            {
                SimulateWaterAgents();
            }
            using(s_SimulateAgentsPerfMarker.Auto())
            {
                SimulateRootAgents();
            }
            //
            using(s_TrackDistancesPerfMarker.Auto())
            {
                UpdateTracking();
            }
            using(s_PlacePointsPerfMarker.Auto())
            {
                CalculatePlacePoints();
            }
            //
            if(m_simUpdateCounter % m_nutrientPointSimStep != 0)
            {
                StartWaterEvaporation();
            }
            using(s_ApplyProductionRulesPerfMarker.Auto())
            {
                ApplyProductionRules();
            }
            m_currentSimulationTime += m_simulationTimeStep;
        }
#endif
        /// <summary>
        /// Starts nutrient absorption jobs.
        /// </summary>
        private void StartNutrientAbsorption()
        {
            m_pointPlantAbsorbScheduler.SchedulePlantsNutrientUpdateJobs(m_seedModels);
        }

        /// <summary>
        /// Waits for nutrient absorption jobs to complete.
        /// </summary>
        private void EndNutrientAbsorption()
        {
            m_pointPlantAbsorbScheduler.Complete();
        }

        /// <summary>
        /// Starts water evaporation jobs, that update soil water values.
        /// </summary>
        private void StartWaterEvaporation()
        {
            m_pointWaterScheduler.SchedulePointWaterUpdateJobs(m_waterAgentsReadonly.AsArray());
        }

        /// <summary>
        /// Waits for water evaporation completion.
        /// </summary>
        private void EndWaterEvaporation()
        {
            m_pointWaterScheduler.Complete();
        }

        /// <summary>
        /// Updates all point values in the point cloud using parallel running jobs.
        /// </summary>
        private void PointCloudDataUpdate()
        {
            m_leafModifier.PointUpdated();
        }

        /// <summary>
        /// Updates trackging related data for all plants and UI visuals for selected plant.
        /// </summary>
        private void UpdateTracking()
        {
            for (int i = 0; i < m_agentsWriteOnly.Length; i++)
            {
                if(m_agentsWriteOnly[i].PlantId >= m_seedModels.Count) {
                    Debug.LogWarning("PlantId out of bounds in tracking distances!");
                    continue;
                }
                m_seedModels[m_agentsWriteOnly[i].PlantId].AddRootLength(m_agentsWriteOnly[i].DeltaTravelDistance, m_agentsWriteOnly[i].AgentType);
            }

            for (int i = 0; i < m_seedModels.Count; i++)
            {
                m_seedModels[i].UpdateDataTracking(m_currentSimulationTime);
                m_seedModels[i].UpdateTrackingVisuals();
            }
        }

        /// <summary>
        /// Selects a production rule based on frequency using a random value from 0 to sum of all frequencies.
        /// </summary>
        /// <param name="possibleRules"> array of all possible production rules.</param>
        /// <returns> RootSGProductionRule.</returns>
        private RootSGProductionRule SelectProductionRuleByFrequency(RootSGProductionRule[] possibleRules)
        {
            RootSGProductionRule selectedRule = null;
            int prodRulesSum = 0;
            foreach (var rule in possibleRules)
            {
                prodRulesSum += rule.ApplicationFrequency;
            }
            if (prodRulesSum == 0) return null;
            int selectedRuleDistributionIndex = UnityEngine.Random.Range(0, prodRulesSum);
            int counter = 0;
            foreach (var rule in possibleRules)
            {
                if (selectedRuleDistributionIndex >= counter &&
                    selectedRuleDistributionIndex < counter + rule.ApplicationFrequency)
                {
                    selectedRule = rule;
                    break;
                }
                counter += rule.ApplicationFrequency;
            }
            return selectedRule;
        }

        /// <summary>
        /// Selects production rule and applies this rule by adding new agents.
        /// </summary>
        /// <param name="parentType"> parent root type index.</param>
        /// <param name="parentUniqueId"> parent root unique id.</param>
        /// <param name="parentDistance"> parent current tracel distance.</param>
        /// <param name="i"> index of parent agent.</param>
        /// <param name="insertionAngleScale"> agengt insertion angle scaling value.</param>
        private void CalculateBranching(int parentType, long parentUniqueId, float parentDistance, int i, float insertionAngleScale)
        {

        #if UNITY_EDITOR
            using(s_ApplyProductionRulesPart2PerfMarker.Auto())
            {
        #endif
            RootSGProductionRule[] productionRules = m_rootSGConfiguration.RootSGAgents[m_agentsWriteOnly[i].AgentType].ProductionRules;
            RootSGProductionRule selectedRule = SelectProductionRuleByFrequency(productionRules);
            if (selectedRule == null) return;
            foreach (var agentTypeChar in selectedRule.Output)
            {
                int type = m_rootSGConfiguration.GetAgentTypeOfChar(agentTypeChar);
                long nextUniqueId = parentType != type ? AgentUniqueIdCounter++ : m_agentsWriteOnly[i].UniqueAgentId;
                if (parentType != type)
                {
                    float3 insertionDirection = RGSMath.CalculateRootInsertionDirection(
                        math.normalize(m_agentsWriteOnly[i].Velocity), 
                        Mathf.Clamp(m_rootSGConfiguration.RootSGAgents[type].RootInsertionAngle.GetValue(nextUniqueId, FloatStdDevType.ROOT_INSERTION_ANGLE) * insertionAngleScale, 0.0f, 180.0f),
                        UnityEngine.Random.value * 360.0f);
                    m_queuedRootBranches.Add(new QueuedRootSGAgentData()
                        {
                            ParentId = parentUniqueId,
                            AgentData = m_agentsWriteOnly[i].ApplyRule(type, math.normalize(insertionDirection) * m_rootSGConfiguration.RootSGAgents[type].InitialVelocity, nextUniqueId, parentDistance),
                            QueueTime = m_currentSimulationTime
                        });
#if UNITY_EDITOR
                    if (m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.RootBranchInsertionAngle))
                    {
                        var pos = m_agentsWriteOnly[i].Position;
                        Debug.DrawLine(pos, pos + math.normalize(m_agentsWriteOnly[i].Velocity) * 0.01f, Color.black, 5.0f);
                        Debug.DrawLine(pos, pos + math.normalize(insertionDirection) * 0.02f, Color.yellow, 5.0f);
                    }
#endif
                }
            }

#if UNITY_EDITOR
            }
#endif
        }

        /// <summary>
        /// Checks if any queued branch can emerge, if so the agent is removed from the queue and added to the read only list.
        /// </summary>
        /// <param name="idIndexMapping"> maps unique ids to agent's index in the write only list.</param>
        private void AddEmergingBranches(NativeParallelHashMap<long, int> idIndexMapping)
        {
            for (int i = 0; i < m_queuedRootBranches.Length;)
            {
                if(idIndexMapping.TryGetValue(m_queuedRootBranches[i].ParentId, out int parentIndex))
                {
                    RootSGAgentData parentRootAgent = m_agentsWriteOnly[parentIndex];
                    float apicalZoneLength = m_rootSGConfiguration.RootSGAgents[parentRootAgent.AgentType].RootApicalZoneLength.GetValue(parentRootAgent.UniqueAgentId, FloatStdDevType.ROOT_APICAL_ZONE);

                    if (parentRootAgent.TravelDistance - m_queuedRootBranches[i].AgentData.ParentEmergenceDistance > apicalZoneLength)
                    {
                        bool isPossible = m_seedModels[parentRootAgent.PlantId].CheckAndRemoveResources(m_queuedRootBranches[i].AgentData.AgentType, m_rootSGAgentTypeSOA.AgentsRequiredResources);
                        m_rootParentChildRelations.Add(new Tuple<long, long, float>(parentRootAgent.UniqueAgentId, m_queuedRootBranches[i].AgentData.UniqueAgentId, m_queuedRootBranches[i].QueueTime));
                        if(isPossible)
                        {
                            m_agentsReadonly.Add(m_queuedRootBranches[i].AgentData.SetEmerganceTime(m_currentSimulationTime));
                        }else {
                            m_pausedAgentsList.Add(new PausedRootSGAgentData() {
                                ResourceAmount = m_rootSGAgentTypeSOA.AgentsSumOfRequiredResources[m_queuedRootBranches[i].AgentData.AgentType], 
                                AgentData = m_queuedRootBranches[i].AgentData.SetEmerganceTime(m_currentSimulationTime)
                            });
                        }
                        if(i < m_queuedRootBranches.Length - 1)
                        {
                            m_queuedRootBranches[i] = m_queuedRootBranches[m_queuedRootBranches.Length-1];
                        }else {
                            i++;
                        }
                        m_queuedRootBranches.Length--;
                    }else {
                        i++;
                    }
                }else {
                    i++;
                }
            }

        }

        /// <summary>
        /// Calculated partial derivation on a given agent.
        /// </summary>
        /// <param name="uniqueAgentId"> unique id of the agent partial derivation is applied on.</param>
        /// <param name="symbols"> resulting agent types.</param>
        /// <param name="replace"> should replace agent.</param>
        private void HandlePartialDerivation(long uniqueAgentId, string symbols, bool replace)
        {
            m_agentsWriteOnly.Clear();
            float parentTravelDistance = 0.0f;
            int indexOfParent = -1;
            int plantId = -1;
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                if(m_agentsReadonly[i].UniqueAgentId == uniqueAgentId)
                {
                    plantId = m_agentsReadonly[i].PlantId;
                    indexOfParent = i;
                    parentTravelDistance = m_agentsReadonly[i].TravelDistance;
                    char agentSymbol = m_rootSGConfiguration.RootSGAgents[m_agentsReadonly[i].AgentType].AgentType;
                    if(symbols.Contains(agentSymbol) && !replace) 
                    {
                        symbols = symbols.Remove(symbols.IndexOf(agentSymbol), 1);
                    }else {
                        continue;
                    }
                }
                m_agentsWriteOnly.Add(m_agentsReadonly[i]);
            }
            if(replace)
            {
                for (int i = 0; i < symbols.Length; i++)
                {
                    int agentType = m_rootSGConfiguration.GetAgentTypeOfChar(symbols[i]);
                    long nextUniqueId = AgentUniqueIdCounter++;
                    float3 startDirection = RGSMath.CalculateRootInsertionDirection(
                        m_agentsReadonly[indexOfParent].LookDirection, 
                        Mathf.Clamp(m_rootSGConfiguration.RootSGAgents[agentType].RootInsertionAngle.GetValue(nextUniqueId, FloatStdDevType.ROOT_INSERTION_ANGLE), 0.0f, 180.0f), 
                        UnityEngine.Random.value * 360.0f);
                    m_rootParentChildRelations.Add(new Tuple<long, long, float>(uniqueAgentId, nextUniqueId, m_currentSimulationTime));
                    m_agentsWriteOnly.Add(RootSGAgentData.NewAgent(m_agentsReadonly[indexOfParent].Position, math.normalize(startDirection) * m_rootSGConfiguration.RootSGAgents[agentType].InitialVelocity, agentType, m_agentsReadonly[indexOfParent].PlantId, nextUniqueId, m_currentSimulationTime));
                }
            } else {
                for (int i = 0; i < symbols.Length; i++)
                {
                    int type = m_rootSGConfiguration.GetAgentTypeOfChar(symbols[i]);
                    float3 direction = m_agentsReadonly[indexOfParent].Velocity;
                    long nextUniqueId = AgentUniqueIdCounter++;
                    direction = RGSMath.CalculateRootInsertionDirection(
                            math.normalize(direction), Mathf.Clamp(m_rootSGConfiguration.RootSGAgents[type].RootInsertionAngle.GetValue(nextUniqueId, FloatStdDevType.ROOT_INSERTION_ANGLE), 0.0f, 180.0f),
                            UnityEngine.Random.value * 360.0f);
                        m_queuedRootBranches.Add(new QueuedRootSGAgentData()
                            {
                                ParentId = uniqueAgentId,
                                AgentData = m_agentsReadonly[indexOfParent].ApplyRule(type, math.normalize(direction) * m_rootSGConfiguration.RootSGAgents[type].InitialVelocity, nextUniqueId, parentTravelDistance),
                                QueueTime = m_currentSimulationTime
                            });
                }
            }

            // add types to plant model
            List<Tuple<Color,int>> AdditionalTypes = new List<Tuple<Color,int>>();
            symbols = new string(symbols.ToCharArray().Distinct().ToArray());
            string symbolsNotInList = "";
            for (int i = 0; i < symbols.Length; i++)
            {
                int agentType = m_rootSGConfiguration.GetAgentTypeOfChar(symbols[i]);
                if(!m_seedModels[plantId].RootTypeColorList.Any(x => x.Item2 == agentType))
                {
                    symbolsNotInList += symbols[i];
                }
            }
            AdditionalTypes.AddRange(m_rootSGConfiguration.GetPossibleRootTypesForAxiom(symbolsNotInList));

            m_seedModels[plantId].AddRootTypes(AdditionalTypes);

            if (m_agentsReadonly.Length != m_agentsWriteOnly.Length)
            {
                m_agentsReadonly.Resize(m_agentsWriteOnly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsScalingFactors.Resize(m_agentsWriteOnly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsWithNewPointsDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsPastSegmentDistance.Resize(m_agentsWriteOnly.Length, NativeArrayOptions.UninitializedMemory);
            }
            m_agentsReadonly.CopyFrom(m_agentsWriteOnly);
            RootAgentCount = m_agentsReadonly.Length;

            m_uiMediator.OnPlantAgentDataChanged.Invoke();
        }

        private void ApplyProductionRules()
        {
            m_agentsReadonly.Clear();
            #if UNITY_EDITOR
                using(s_ApplyProductionRulesPart1PerfMarker.Auto())
                {
            #endif
            NativeParallelHashMap<long, int> idIndexMapping = new NativeParallelHashMap<long, int>(m_agentsWriteOnly.Length, Allocator.Temp);
            for (int i = 0; i < m_agentsWriteOnly.Length; i++)
            {
                long uniqueAgentId = m_agentsWriteOnly[i].UniqueAgentId;
                int parentType = m_agentsWriteOnly[i].AgentType;
                // Remove agent if max root length is reached
                float maxRootLength = math.max(0.0f, m_rootSGConfiguration.RootSGAgents[parentType].MaximumRootLength.GetValue(uniqueAgentId, FloatStdDevType.ROOT_MAX_LEN));
                idIndexMapping.Add(m_agentsWriteOnly[i].UniqueAgentId, i);
                if (m_rootSGConfiguration.RootSGAgents[parentType].UseMaxLength && m_agentsWriteOnly[i].TravelDistance >= maxRootLength * m_agentsScalingFactors[i].RootLengthScale) {
                    continue;
                }// Remove agent if max root life time is reached
                if (m_currentSimulationTime - m_agentsWriteOnly[i].EmergenceTime >= m_rootSGConfiguration.RootSGAgents[parentType].RootLifeTime.GetValue(uniqueAgentId, FloatStdDevType.ROOT_LIFE_TIME)) {
                    continue;
                }
                // check if resources 
                bool isPossible = m_seedModels[m_agentsWriteOnly[i].PlantId].CheckAndRemoveResources(m_agentsWriteOnly[i].AgentType, m_rootSGAgentTypeSOA.AgentsRequiredResources);
                if(isPossible)
                {
                    // Check if root is long enough to produce possible branching points
                    if (m_agentsWriteOnly[i].TravelDistance < m_rootSGConfiguration.RootSGAgents[parentType].RootBasalZoneLength.GetValue(uniqueAgentId, FloatStdDevType.ROOT_BASAL_ZONE)) 
                    {
                        m_agentsReadonly.Add(m_agentsWriteOnly[i]);
                    } else {
                        float interbranchingDistance = m_rootSGConfiguration.RootSGAgents[parentType].RootInterBranchingDistance.GetValue(uniqueAgentId, FloatStdDevType.ROOT_INTER_BRANCHING_DISTANCE) * m_agentsScalingFactors[i].BranchingDensityScale;
                        if (
                            m_agentsWriteOnly[i].TravelDistance - m_agentsWriteOnly[i].LastBranchingDistance >= interbranchingDistance &&
                            m_rootSGConfiguration.RootSGAgents[m_agentsWriteOnly[i].AgentType].ProductionRules.Length > 0)
                        {
                            m_agentsReadonly.Add(m_agentsWriteOnly[i].SetLastBranchingDistance());
                            if (UnityEngine.Random.value <= m_rootSGConfiguration.RootSGAgents[m_agentsWriteOnly[i].AgentType].BranchingProbability * m_agentsScalingFactors[i].BranchingProbabilityScale) 
                            {
                                CalculateBranching(parentType, m_agentsWriteOnly[i].UniqueAgentId, m_agentsWriteOnly[i].TravelDistance, i, m_agentsScalingFactors[i].BranchingAngleScale);
                            }
                        }
                        else{
                            m_agentsReadonly.Add(m_agentsWriteOnly[i]);
                        }
                    }
                }
                else {
                    m_pausedAgentsList.Add(new PausedRootSGAgentData() {
                        ResourceAmount = m_rootSGAgentTypeSOA.AgentsSumOfRequiredResources[m_agentsWriteOnly[i].AgentType], 
                        AgentData = m_agentsWriteOnly[i]
                    });
                }
            }
#if UNITY_EDITOR
                using (s_ApplyProductionRulesPart3PerfMarker.Auto())
                {
#endif
                    AddEmergingBranches(idIndexMapping);
#if UNITY_EDITOR
                }
#endif
                idIndexMapping.Dispose();
#if UNITY_EDITOR
            }
#endif
            PlantSeedModel plantModel;
            for (int i = 0; i < m_pausedAgentsList.Length;)
            {
                plantModel = m_seedModels[m_pausedAgentsList[i].AgentData.PlantId];
                bool isPossible = plantModel.CheckAndRemoveResources(m_pausedAgentsList[i].AgentData.AgentType, m_rootSGAgentTypeSOA.AgentsRequiredResources);
                if(!isPossible) 
                {
                    i++;
                    continue;
                }
                m_agentsReadonly.Add(m_pausedAgentsList[i].AgentData);
                m_pausedAgentsList.RemoveAtSwapBack(i);

            }

            foreach (var plant in m_seedModels)
            {
                plant.AddResourcesFromShoot(m_simulationTimeStep);
            }
            if (m_agentsReadonly.Length != m_agentsWriteOnly.Length)
            {
                m_agentsWriteOnly.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsScalingFactors.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsWithNewPointsDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsPastSegmentDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            }
            RootAgentCount = m_agentsReadonly.Length;
        }
        private void DrawDebugVelocityAccelerationVectors()
        {
            if(!m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentMovementVectors)) return;
            foreach (var agent in m_agentsReadonly)
            {
                Debug.DrawLine(agent.Position, agent.Position + (math.normalize(agent.Velocity) * 0.01f), Color.green, 2.0f);
                Debug.DrawLine(agent.Position, agent.Position + (math.normalize(agent.Acceleration) * 0.01f), Color.red, 2.0f);
            }
        }
        private void DrawDebugPossibleDirectionsVectors()
        {
            if(!m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentsPossibleDirections)) return;
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                int agentTypeIndex = m_agentsReadonly[i].AgentType;
                int start = m_agentsDirectionTries.DataStartIndexList[i];
                for (int j = 0; j < m_rootSGAgentTypeSOA.AgentsNumberOfTries[agentTypeIndex]; j++)
                {
                    float3 direction = m_agentsDirectionTries.SequentialDataList[start + j];
                    Debug.DrawLine(m_agentsReadonly[i].Position, m_agentsReadonly[i].Position + (math.normalize(direction) * 0.01f), Color.magenta, 0.01f);
                }
            }
        }
        private void DrawDebugWaterVelocityAccelerationVectors()
        {
            if(!m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentMovementVectors)) return;
            foreach (var agent in m_waterAgentsReadonly)
            {
                Debug.DrawLine(agent.Position.xyz, agent.Position + (math.normalize(agent.Velocity.xyz) * 0.01f), Color.green, 2.0f);
                Debug.DrawLine(agent.Position.xyz, agent.Position + (math.normalize(agent.Acceleration.xyz) * 0.01f), Color.red, 2.0f);
            }
        }
        private void DrawDebugWaterPossibleDirectionsVectors()
        {
            if(!m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentsPossibleDirections)) return;
            for (int i = 0; i < m_waterAgentsReadonly.Length; i++)
            {
                for (int j = 0; j < m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries; j++)
                {
                    float3 direction = m_waterAgentsDirectionTries[i* m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries + j];
                    Debug.DrawLine(m_waterAgentsReadonly[i].Position, m_waterAgentsReadonly[i].Position + (math.normalize(direction) * 0.01f), Color.magenta, 0.01f);
                }
            }
        }

        /// <summary>
        /// Simulates all steps involved in water agent update.
        /// </summary>
        private void SimulateWaterAgents()
        {

#if UNITY_EDITOR
            using(s_QueryPointsInWaterAgentsRadiusPerfMarker.Auto())
            {
#endif
                m_waterAgentsPointsInRadius.DataStartIndexList.Clear();
                m_waterAgentsPointsInRadius.SequentialDataList.Clear();
                for (int i = 0; i < m_waterAgentsReadonly.Length; i++)
                {
                    float overlapCheckRadius = m_rootSGConfiguration.WaterAgentConfig.WaterOverlapCheckRadius;
                    m_leafModifier.Tree.ClearModifiedBlockList();
                    m_leafModifier.Tree.QueryOverlapPoints(new SphereQueryOveralapVolume(m_waterAgentsReadonly[i].Position.xyz, overlapCheckRadius));
                    PointQuerySchedulerNew schedulePointQueryJobs = new PointQuerySchedulerNew()
                    {
                        Tree = m_leafModifier.Tree,
                        OriginSquareRadius = new float4(m_waterAgentsReadonly[i].Position, overlapCheckRadius * overlapCheckRadius),
                        AgentPointCounter = m_waterAgentsPointsInRadius.DataStartIndexList,
                        OutputPoints = m_waterAgentsPointsInRadius.SequentialDataList
                    };
                    schedulePointQueryJobs.SchedulePointQueryJobs();
                }
#if UNITY_EDITOR
            }
            using(s_CalculateWaterAgentsDirectionsPerfMarker.Auto())
            {
#else 
            JobHandle lastHandle = default;
#endif
                // calculate possible directions
                // down is view direction
                // choose closest to gravity
                m_waterAgentsDirectionTries.Clear();
                m_waterAgentsDirectionTries.Resize(m_waterAgentsReadonly.Length * m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries, NativeArrayOptions.UninitializedMemory);
                CalculateWaterAgentsDirectionsJob calculateWaterAgentsDirectionsJob = new CalculateWaterAgentsDirectionsJob()
                {
                    MaxVelocity = m_rootSGConfiguration.WaterAgentConfig.MaxVelocity,
                    AgentRadius = m_rootSGConfiguration.WaterAgentConfig.WaterOverlapCheckRadius,
                    PerceptionData = m_waterAgentPerceptionData,
                    RandomValues = m_precomputedRandomValues,
                    RandomSeed = UnityEngine.Random.Range(0, m_precomputedRandomValues.Length),
                    CollisionPointId = m_collisionPointId,
                    AgentsReadOnly = m_waterAgentsReadonly,
                    PointsDataStartLength = m_waterAgentsPointsInRadius.DataStartIndexList,
                    PointsInRadius = m_waterAgentsPointsInRadius.SequentialDataList,
                    AgentsPossibleDirections = m_waterAgentsDirectionTries
                };
#if UNITY_EDITOR
                var handle = calculateWaterAgentsDirectionsJob.Schedule(m_waterAgentsReadonly.Length, PARALLELFOR_WATER_MOVEMENT_INNERLOOPBATCHCOUNT);
                handle.Complete();
#else
                lastHandle = calculateWaterAgentsDirectionsJob.Schedule(m_waterAgentsReadonly.Length, PARALLELFOR_WATER_MOVEMENT_INNERLOOPBATCHCOUNT, lastHandle);
#endif
#if UNITY_EDITOR
            }
        DrawDebugWaterPossibleDirectionsVectors();
            using(s_SimulateWaterAgentsMovementPerfMarker.Auto())
            {
#endif
                // apply new acceleration and apply collision speed damping
                SimulateWaterAgentsJob simulateWaterAgentsJob = new SimulateWaterAgentsJob()
                {
                    AgentRadius = m_rootSGConfiguration.WaterAgentConfig.WaterOverlapCheckRadius,
                    DeltaTime = m_simulationTimeStep,
                    Acceleration = m_rootSGConfiguration.WaterAgentConfig.Acceleration,
                    MaxVelocity = m_rootSGConfiguration.WaterAgentConfig.MaxVelocity,
                    NumberOfTries = m_rootSGConfiguration.WaterAgentConfig.WaterAgentNumberOfTries,
                    CollisionPointId = m_collisionPointId,
                    AgentsReadOnly = m_waterAgentsReadonly,
                    AgentsWriteOnly = m_waterAgentsWriteOnly,
                    AgentsPossibleDirections = m_waterAgentsDirectionTries,
                    PointsDataStartLength = m_waterAgentsPointsInRadius.DataStartIndexList,
                    PointsInRadius = m_waterAgentsPointsInRadius.SequentialDataList
                };

#if UNITY_EDITOR
                var handle = simulateWaterAgentsJob.Schedule(m_waterAgentsReadonly.Length, PARALLELFOR_WATER_MOVEMENT_INNERLOOPBATCHCOUNT);
                handle.Complete();
#else
                simulateWaterAgentsJob.Schedule(m_waterAgentsReadonly.Length, PARALLELFOR_WATER_MOVEMENT_INNERLOOPBATCHCOUNT, lastHandle).Complete();
#endif
    
#if UNITY_EDITOR
            }
        DrawDebugWaterVelocityAccelerationVectors();
#endif
            // remove if under kill height or no water
            // then swap and resize
            m_waterAgentsReadonly.Clear();
            for (int i = 0; i < m_waterAgentsWriteOnly.Length; i++)
            {
                if(m_waterAgentsWriteOnly[i].WaterContent <= 0) continue;
                if(m_waterAgentsWriteOnly[i].Position.y <= m_waterAgentsKillHeight) continue; 
                m_waterAgentsReadonly.Add(m_waterAgentsWriteOnly[i]);
            }

            if(m_waterSource != null)
            {
                m_waterSource.TimeStepUpdate(m_waterAgentsReadonly, m_currentSimulationTime);
            }
            if (m_waterAgentsReadonly.Length != m_waterAgentsWriteOnly.Length)
            {
                m_waterAgentsWriteOnly.Resize(m_waterAgentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            }
            WaterAgentCount = m_waterAgentsReadonly.Length;
        }

        /// <summary>
        /// Simulates all steps involved in root agent update.
        /// </summary>
        private void SimulateRootAgents()
        {
#if UNITY_EDITOR
            using(s_QueryPointsInAgentsRadiusPerfMarker.Auto())
            {
#endif
                m_agentsPointsInRadius.DataStartIndexList.Clear();
                m_agentsPointsInRadius.SequentialDataList.Clear();
                for (int i = 0; i < m_agentsReadonly.Length; i++)
                {
                    float overlapCheckRadius = m_rootSGAgentTypeSOA.AgentsRadius[m_agentsReadonly[i].AgentType] + RGSConfiguration.Get().OverlapCheckRadius;
#if UNITY_EDITOR
                    using(s_QueryPointsInAgentsRadiusOverlapPerfMarker.Auto()) {
#endif
                        m_leafModifier.Tree.ClearModifiedBlockList();
                        m_leafModifier.Tree.QueryOverlapPoints(new SphereQueryOveralapVolume(m_agentsReadonly[i].Position, overlapCheckRadius));
#if UNITY_EDITOR
                    }
#endif
                    PointQuerySchedulerNew schedulePointQueryJobs = new PointQuerySchedulerNew()
                    {
                        Tree = m_leafModifier.Tree,
                        OriginSquareRadius = new float4(m_agentsReadonly[i].Position, overlapCheckRadius * overlapCheckRadius),
                        AgentPointCounter = m_agentsPointsInRadius.DataStartIndexList,
                        OutputPoints = m_agentsPointsInRadius.SequentialDataList
                    };
                    schedulePointQueryJobs.SchedulePointQueryJobs();
                }
#if UNITY_EDITOR
            }
            // compute scaling factors for len, insertion angle, elongation rate, gravitropism
            using(s_CalculateRootScalingFactorsPerfMarker.Auto())
            {
#endif
                JobHandle handle = default;
                for (int i = 0; i < Enum.GetNames(typeof(ScalingFunctionType)).Length; i++)
                {
                    CalculateRootParameterScalingJob calculateRootParameterScalingJob = new CalculateRootParameterScalingJob()
                    {
                        AgentsReadOnly = m_agentsReadonly.AsArray(),
                        ScalingFunctionParameterStartLength = m_agentScalingParametersSOA.GetAgentsFactorsArrayByType((ScalingFunctionType)i).DataStartIndexArray,
                        ScalingFunctionParameterDataLength = m_agentScalingParametersSOA.GetAgentsFactorsArrayByType((ScalingFunctionType)i).SequentialDataArray,
                        PointsDataStartLength = m_agentsPointsInRadius.DataStartIndexList.AsArray(),
                        PointsInRadius = m_agentsPointsInRadius.SequentialDataList.AsArray(),
                        AgentsRootScalingFactors  = m_agentsScalingFactors.AsArray(),
                        ScalingFunctionType = (ScalingFunctionType)i
                    };
                    handle = calculateRootParameterScalingJob.Schedule(m_agentsReadonly.Length, PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT, handle);
                }
                CalculateRootGSAByLengthScalingJob calculateRootGSAByLengthScalingJob = new CalculateRootGSAByLengthScalingJob()
                {
                    AgentsReadOnly = m_agentsReadonly.AsArray(),
                    AgentsRootScalingFactors = m_agentsScalingFactors.AsArray(),
                    GSAByLengthScalingFactors = m_agentScalingParametersSOA.GSAByLengthScalingFactors
                };
                handle = calculateRootGSAByLengthScalingJob.Schedule(m_agentsReadonly.Length, PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT, handle);
                handle.Complete();
#if UNITY_EDITOR
            }
            using(s_CalculateAgentsDirectionsPerfMarker.Auto())
            {
#endif
                m_agentsDirectionTries.DataStartIndexList.Clear();
                m_agentsDirectionTries.SequentialDataList.Clear();
                FillAgentsTriesListJob fillAgentsTriesListJob = new FillAgentsTriesListJob()
                {
                    AgentsNumberOfTries = m_rootSGAgentTypeSOA.AgentsNumberOfTries,
                    AgentsReadOnly = m_agentsReadonly.AsArray(),
                    PossibleDirectionsDataStartIndex = m_agentsDirectionTries.DataStartIndexList,
                    AgentsPossibleDirections = m_agentsDirectionTries.SequentialDataList,
                };
                fillAgentsTriesListJob.Schedule().Complete();
                CalculateAgentsDirectionsJob calculateAgentsDirectionsJob = new CalculateAgentsDirectionsJob()
                {
                    RandomValues = m_precomputedRandomValues,
                    RandomSeed = UnityEngine.Random.Range(0, m_precomputedRandomValues.Length),
                    CollisionPointId = m_collisionPointId,
                    DeltaTime = m_simulationTimeStep,
                    AgentsPerceptionData = m_rootSGAgentTypeSOA.AgentsPerceptionData,
                    AgentsReadOnly = m_agentsReadonly.AsArray(),
                    PossibleDirectionsDataStartIndex = m_agentsDirectionTries.DataStartIndexList.AsArray(),
                    AgentsPossibleDirections = m_agentsDirectionTries.SequentialDataList.AsArray(),
                    PointsDataStartLength = m_agentsPointsInRadius.DataStartIndexList.AsArray(),
                    PointsInRadius = m_agentsPointsInRadius.SequentialDataList.AsArray(),
                    AgentsVelocityLimits = m_rootSGAgentTypeSOA.AgentsVelocityLimits,
                    AgentsRadius = m_rootSGAgentTypeSOA.AgentsRadius
                };
                calculateAgentsDirectionsJob.Schedule(m_agentsReadonly.Length, PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT).Complete();
#if UNITY_EDITOR
            }
            #endif
#if UNITY_EDITOR
            DrawDebugPossibleDirectionsVectors();
            using(s_CalculateAgentsAccelerationPerfMarker.Auto())
            {
#endif      
                CalculateAgentsAccelerationJob calculateAgentsAccelerationJob = new CalculateAgentsAccelerationJob()
                {
                    UnitTropismVectorMagnitude = RGSConfiguration.Get().UnitTropismVectorMagnitude,
                    RootPointType = m_rootPointId,
                    AgentsGSA = m_rootSGAgentTypeSOA.AgentsGSA,
                    AgentsSoundDetectionRadius = m_rootSGAgentTypeSOA.AgentsSoundDetectRadius,
                    SoilPointType = m_soilPointId,
                    MagnetotropismBounds = m_magnetotropismBounds.AsArray(),
                    ChemotropismPointTypesWithSign = m_chemotropismPointTypesWithSign.AsArray(),
                    NaClPointType = m_halotropismPointId,
                    AgentsNumberOfTries = m_rootSGAgentTypeSOA.AgentsNumberOfTries,
                    AgentsAccelerationLimits = m_rootSGAgentTypeSOA.AgentsAccelerationLimits,
                    AgentsRadius = m_rootSGAgentTypeSOA.AgentsRadius,
                    AgentsWeights = m_rootSGAgentTypeSOA.AgentsWeights,
                    AgentsReadOnly = m_agentsReadonly.AsArray(),
                    PossibleDirectionsDataStartIndex = m_agentsDirectionTries.DataStartIndexList.AsArray(),
                    AgentsPossibleDirections = m_agentsDirectionTries.SequentialDataList.AsArray(),
                    AgentsRootScalingFactors  = m_agentsScalingFactors,
                    PointsDataStartLength = m_agentsPointsInRadius.DataStartIndexList.AsArray(),
                    PointsInRadius = m_agentsPointsInRadius.SequentialDataList.AsArray(),
                    AgentsVelocityLimits = m_rootSGAgentTypeSOA.AgentsVelocityLimits,
                    WaterAgentsReadonly = m_waterAgentsReadonly.AsArray()
                };
                calculateAgentsAccelerationJob.Schedule(m_agentsReadonly.Length, PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT).Complete();
#if UNITY_EDITOR
            }
            DrawDebugVelocityAccelerationVectors();
            using(s_SimulateAgentsMovementPerfMarker.Auto())
            {
#endif      
                SimulateRootAgentsJob simulateRootAgentsJob = new SimulateRootAgentsJob()
                {
                    CollisionPointId = m_collisionPointId,
                    PointsDataStartLength = m_agentsPointsInRadius.DataStartIndexList.AsArray(),
                    PointsInRadius = m_agentsPointsInRadius.SequentialDataList.AsArray(),
                    DeltaTime = m_simulationTimeStep,
                    AgentsVelocityLimits = m_rootSGAgentTypeSOA.AgentsVelocityLimits,
                    AgentsReadOnly = m_agentsReadonly.AsArray(),
                    AgentsWriteOnly = m_agentsWriteOnly.AsArray(),
                    AgentsRootScalingFactors  = m_agentsScalingFactors.AsArray(),
                    AgentsRadius = m_rootSGAgentTypeSOA.AgentsRadius
                };
                simulateRootAgentsJob.Schedule(m_agentsReadonly.Length, PARALLELFOR_MOVEMENT_INNERLOOPBATCHCOUNT).Complete();
#if UNITY_EDITOR
            }
#endif
        }

        /// <summary>
        /// Recursive traversal of root system. Where all root ids emerged after given time are added to the idList.
        /// </summary>
        /// <param name="uniqueId"> unique id of current root agent.</param>
        /// <param name="idList"> list of connected root unique ids emerged after time.</param>
        /// <param name="time"> root emergence time threshold.</param>
        private void RecursiveConnectedRootSearch(long uniqueId, List<long> idList, float time)
        {
            foreach (var ids in m_rootParentChildRelations)
            {
                if(ids.Item1 == uniqueId && ids.Item3 > time)
                {
                    idList.Add(ids.Item2);
                    RecursiveConnectedRootSearch(ids.Item2, idList, time);
                }
            }
        }

        /// <summary>
        /// Updates root highlight renderer to show all point in possible root cut.
        /// </summary>
        /// <param name="uniqueRootId"> unique id of selected root branch.</param>
        /// <param name="time"> root emergence time threshold.</param>
        /// <param name="selectedPlant"> selected plant.</param>
        private void HandlePreviewCuttingAt(long uniqueRootId, float time, PlantSeedModel selectedPlant)
        {
            List<long> idList = new List<long>();
            idList.Add(uniqueRootId);
            RecursiveConnectedRootSearch(uniqueRootId, idList, time);
            selectedPlant.HighlightPossibleCut(idList, time, m_selectedRootIndexBuffer, MAX_BUFFER_SIZE);
        }

        /// <summary>
        /// Performes root cut and removes all root points, that emerged after the given time.
        /// </summary>
        /// <param name="uniqueRootId"> unique id of selected root branch.</param>
        /// <param name="time"> root emergence time threshold.</param>
        /// <param name="selectedPlant"> selected plant.</param>
        private void HandleCutRootAt(long uniqueRootId, float time, PlantSeedModel selectedPlant)
        {
            // create list of child unique ids
            List<long> idList = new List<long>();
            idList.Add(uniqueRootId);
            RecursiveConnectedRootSearch(uniqueRootId, idList, time);
            // remove agents with unique ids
            var tmpRef = m_agentsWriteOnly;
            m_agentsWriteOnly = m_agentsReadonly;
            m_agentsReadonly = tmpRef;
            m_agentsReadonly.Clear();
            for (int i = 0; i < m_agentsWriteOnly.Length; i++)
            {
                if(!idList.Contains(m_agentsWriteOnly[i].UniqueAgentId))
                {
                    m_agentsReadonly.Add(m_agentsWriteOnly[i]);
                }
            }

            if (m_agentsReadonly.Length != m_agentsWriteOnly.Length)
            {
                m_agentsWriteOnly.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsScalingFactors.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsWithNewPointsDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
                m_agentsPastSegmentDistance.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
            }
            RootAgentCount = m_agentsReadonly.Length;
            // remove paused agents
            for (int i = 0; i < m_pausedAgentsList.Length; )
            {

                if(idList.Contains(m_pausedAgentsList[i].AgentData.UniqueAgentId))
                {
                    m_pausedAgentsList.RemoveAtSwapBack(i);
                }else {
                    i++;
                }
            }
            // remove queued agents
            for (int i = 0; i < m_queuedRootBranches.Length; )
            {
                if(idList.Contains(m_queuedRootBranches[i].ParentId) )
                {
                    m_queuedRootBranches.RemoveAtSwapBack(i);
                }else {
                    i++;
                }
            }

            m_selectableAgentsPool.HideSelectableAgents();
            UpdateSelectableRootAgents();

            // retrieve list of rendering points
            Bounds bounds;
            NativeList<int> cutPointIndices;
            NativeList<float3> cutPoints = selectedPlant.GetSelectedRootCutPoints(idList, time, out cutPointIndices, out bounds);
            // remove all points with age >= time and root id contained in list
            float toleranceValue = 0.0001f;
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var modifyList = m_leafModifier.Tree.RemovePoints(new AABBPointsOverlapVolume(bounds.min - (Vector3.one * 0.1f), bounds.max + (Vector3.one * 0.1f), cutPoints, toleranceValue,m_rootPointId,output));
            output.Dispose();
            m_leafModifier.Tree.ClearEmptyBranches(modifyList);
            m_leafModifier.PointRemoveUpdated();
            m_leafModifier.Tree.ClearEmptyBlocks();
            // update segments and tracking data
            selectedPlant.RemoveCutPoints(cutPointIndices, toleranceValue, idList, time);

            cutPoints.Dispose();
            cutPointIndices.Dispose();

            // updat buffer for select render texture
            selectedPlant.UpdateRootPointSelectionBuffer();
        }

        /// <summary>
        /// Iterates through each agent and places the agent specific artifact points in the world.
        /// Artifact points are only placed if the agent reached its' placement travel distance.
        /// </summary>
        private void CalculatePlacePoints()
        {
            m_agentsPastSegmentDistance.Clear();
            m_agentsWithNewPointsDistance.Clear();

            CalculateNewRootPointsJob calculateNewRootPointsJob = new CalculateNewRootPointsJob()
            {
                SegmentLength = m_rootSGConfiguration.SegmentLength,
                Agents = m_agentsWriteOnly,
                AgentsEncodedColors = m_rootSGAgentTypeSOA.AgentsEncodedColors,
                AgentsRadius = m_rootSGAgentTypeSOA.AgentsRadius,
                PointToBeAdded = m_pointToBeAdded,
                AgentsPastSegmentDistance = m_agentsPastSegmentDistance,
                AgentsWithNewPointsDistance = m_agentsWithNewPointsDistance

            };
            calculateNewRootPointsJob.Schedule().Complete();
#if UNITY_EDITOR
            using(s_PlacePointsExtendPerfMarker.Auto())
            {
#endif
                for (int i = 0; i < m_agentsPastSegmentDistance.Length; i++)
                {
                    var agent = m_agentsWriteOnly[m_agentsPastSegmentDistance[i]];
                    m_seedModels[agent.PlantId].ExtendBounds(
                        agent.Position, agent.UniqueAgentId,
                        agent.AgentType, m_rootSGAgentTypeSOA.AgentsRadius[agent.AgentType], 
                        m_rootSGAgentTypeSOA.AgentsEncodedColors[agent.AgentType], m_currentSimulationTime);
                }
                for (int i = 0; i < m_seedModels.Count; i++)
                {
                    m_seedModels[i].PostExtendBoundsPlottingChange();
                }
#if UNITY_EDITOR
            }
#endif
            for (int i = 0; i < m_agentsWithNewPointsDistance.Length; i++)
            {
                var agent = m_agentsWriteOnly[m_agentsWithNewPointsDistance[i]];
                m_seedModels[agent.PlantId].AddPCRootPoint(agent.Position, agent.UniqueAgentId, m_currentSimulationTime);
            }
        }
        /// <summary>
        /// Resizes read only buffer to match write only buffer and swaps read only with write only buffer.
        /// </summary>
        private void ResizeAndSwapBuffers()
        {
            if (m_agentsReadonly.Length != m_agentsWriteOnly.Length)
            {
                m_agentsReadonly.Resize(m_agentsWriteOnly.Length, NativeArrayOptions.UninitializedMemory);
            }
            var tmpAgentList = m_agentsReadonly;
            m_agentsReadonly = m_agentsWriteOnly;
            m_agentsWriteOnly = tmpAgentList;
        }
        private void Update()
        {
                for (int i = 0; i < m_simSteps; i++)
                {
#if UNITY_EDITOR
                    TimeStepUpdateProfiling();
#else
                    TimeStepUpdate();
#endif
                }
                if(m_simUpdateCounter % m_nutrientPointSimStep == 0)
                {
                    PlaceRootPointsInTree();
                }
                m_lastTime = Time.time;
        }

        private void OnDrawGizmos()
        {
            if (!m_agentsReadonly.IsCreated) return;
            if (m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentsGizmosMesh))
            { 
                for (int i = 0; i < m_agentsReadonly.Length; i++)
                {
                    float3 color = m_rootSGAgentTypeSOA.AgentColors[m_agentsReadonly[i].AgentType];
                    Gizmos.color = new Color(color.x, color.y, color.z);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.matrix = Matrix4x4.Translate(m_agentsReadonly[i].Position) * Matrix4x4.Scale(Vector3.one * 0.01f);
                    Gizmos.DrawMesh(m_agentMesh, Vector3.zero, Quaternion.LookRotation(m_agentsReadonly[i].Velocity, Vector3.up), Vector3.one * 10.0f);
                }
            }
            if (m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.AgentRadiusSphere))
            {
                for (int i = 0; i < m_agentsReadonly.Length; i++)
                {
                    float3 color = m_rootSGAgentTypeSOA.AgentColors[m_agentsReadonly[i].AgentType];
                    Gizmos.color = new Color(color.x, color.y, color.z);
                    Gizmos.matrix = Matrix4x4.identity;
                    float overlapCheckRadius = m_rootSGAgentTypeSOA.AgentsRadius[m_agentsReadonly[i].AgentType] + RGSConfiguration.Get().OverlapCheckRadius;
                    Gizmos.DrawWireSphere(m_agentsReadonly[i].Position, overlapCheckRadius);
                    Gizmos.matrix = Matrix4x4.Translate(m_agentsReadonly[i].Position.xyz) * Matrix4x4.Scale(Vector3.one * 0.01f);
                    Gizmos.DrawMesh(m_agentMesh, Vector3.zero, Quaternion.LookRotation(m_agentsReadonly[i].Velocity, Vector3.up));
                }
            }
            if(m_gizmosRenderOptions.HasFlag(GizmosRenderOptions.WaterAgentsGizmosMesh))
            {
                for (int i = 0; i < m_waterAgentsReadonly.Length; i++)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.matrix = Matrix4x4.Translate(m_waterAgentsReadonly[i].Position) * Matrix4x4.Scale(Vector3.one * 0.01f);
                    Gizmos.DrawMesh(m_agentMesh, Vector3.zero, Quaternion.LookRotation(m_waterAgentsReadonly[i].Velocity.xyz, Vector3.up), Vector3.one * 10.0f);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.DrawWireSphere(m_waterAgentsReadonly[i].Position, m_rootSGConfiguration.WaterAgentConfig.WaterOverlapCheckRadius);
                }
            }
        }
#if UNITY_EDITOR
        private void OnGUI() {
            if(!RenderGUI) return;
            GUILayout.Label($"Agent Count: {RootAgentCount}");
            GUILayout.Label($"Water Agent Count: {WaterAgentCount}");
            GUILayout.Label($"Simulation time: {m_currentSimulationTime.ToString("0.00")}");
            
        }
#endif
        void OnDisable()
        {
            m_pointPlantAbsorbScheduler.Complete();
            m_pointWaterScheduler.Complete();
        }
        private void OnDestroy()
        {
            RGSMath.Dispose();
            m_interactionMediator.OnMagneticFieldUpdate.RemoveListener(HandleMagnetUpdate);
            m_interactionMediator.OnApplyPartialDerivation.RemoveListener(HandlePartialDerivation);
            m_interactionMediator.OnCutRootAt.RemoveListener(HandleCutRootAt);
            m_interactionMediator.OnPreviewCutRootAt.RemoveListener(HandlePreviewCuttingAt);
            m_selectedRootIndexBuffer.Dispose();
            m_queuedRootBranches.Dispose();
            m_pointWaterScheduler.Dispose();
            m_pointPlantAbsorbScheduler.Dispose();
            m_agentsReadonly.Dispose();
            m_agentsWriteOnly.Dispose();
            m_rootSGAgentTypeSOA.Dispose();
            m_agentScalingParametersSOA.Dispose();
            m_agentsDirectionTries.Dispose();
            m_agentsPointsInRadius.Dispose();
            m_precomputedRandomValues.Dispose();
            m_agentsScalingFactors.Dispose();
            m_pointToBeAdded.Dispose();
            m_agentsPastSegmentDistance.Dispose();
            m_agentsWithNewPointsDistance.Dispose();
            m_chemotropismPointTypesWithSign.Dispose();
            m_magnetotropismBounds.Dispose();
            m_pausedAgentsList.Dispose();
            // Water
            m_waterAgentsReadonly.Dispose();
            m_waterAgentsWriteOnly.Dispose();
            m_waterAgentsPointsInRadius.Dispose();
            m_waterAgentsDirectionTries.Dispose();
            //
            m_uiMediator.OnPauseEvent.RemoveListener(HandlePauseSimulation);
            m_uiMediator.OnPlayEvent.RemoveListener(HandlePlaySimulation);
            m_uiMediator.OnFastForwardEvent.RemoveListener(HandleFastForward);
        }
    }

}