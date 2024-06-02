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
using PCMTool.Tree;
using RGS.Jobs;
using RGS.Agents;
using RGS.Configurations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace RGS.Simulation
{

    public class SimpleGrammarSimulationArea : MonoBehaviour
    {
        [SerializeField] private RuntimeLeafModifier m_leafModifier;
        [SerializeField] private float m_simulationTimeStep;
        [SerializeField] private float m_placementTravelDistance;
        [SerializeField] private int m_pointCount;
        [SerializeField] private float m_artifactRadius;
        [SerializeField] private Transform m_center;
        [SerializeField] private SimpleGrammarConfiguration m_grammarConfiguration;
        [SerializeField] private Mesh m_agentMesh;
        [SerializeField] private Transform[] m_plantSeeds;
        private NativeList<SimpleSGAgent> m_agentsReadonly;
        private NativeList<SimpleSGAgent> m_agentsWriteOnly;
        private NativeArray<float> m_agentsWeights;
        private NativeArray<float3> m_agentColors;
        private NativeArray<ProbabilityOutputIndex> m_ruleProbabilities;
        private NativeList<int> m_ruleOutputs;
        private float m_lastTime;
        private int m_counter;
        private void Start() 
        {
            m_agentsReadonly = new NativeList<SimpleSGAgent>(100,Allocator.Persistent);
            m_agentsWriteOnly = new NativeList<SimpleSGAgent>(100,Allocator.Persistent);
            UpdateGrammarRules();
            UpdateGrammarWeights();
            
            for (int i = 0; i < m_plantSeeds.Length; i++)
            {
                float4 position = new float4(m_plantSeeds[i].position, 0.0f);
                m_agentsReadonly.Add(new SimpleSGAgent(position, float4.zero, position, new float4(1,0,0,0), 0, 0.0f));
            }
            m_agentsWriteOnly.Resize(m_agentsReadonly.Length, NativeArrayOptions.UninitializedMemory);
        }

        private void TimeStepUpdate()
        {
            m_counter++;
            PlacePoints();
            SimulateAgents();
            // calculate production rules
            if(m_counter % 10 == 0){
                CalculateProductionRules();
            }
            // apply rules
            // swap read and write buffer and match size
            if(m_agentsReadonly.Length != m_agentsWriteOnly.Length) {
                //m_agentsReadonly.SetCapacity(m_agentsWriteOnly.Capacity);
                m_agentsReadonly.Resize(m_agentsWriteOnly.Length, NativeArrayOptions.UninitializedMemory);
            }
            var tmpAgentList = m_agentsReadonly;
            m_agentsReadonly = m_agentsWriteOnly;
            m_agentsWriteOnly = tmpAgentList;
        }
        private void CalculateProductionRules()
        {
            NativeList<SimpleSGAgent> newAgents = new NativeList<SimpleSGAgent>(100,Allocator.Persistent);
            for (int i = 0; i < m_agentsWriteOnly.Length; i++)
            {
                float rndValue = UnityEngine.Random.value;
                if(rndValue < m_ruleProbabilities[m_agentsWriteOnly[i].AgentType].Probability) {
                    int outputIndex = m_ruleProbabilities[m_agentsWriteOnly[i].AgentType].OutputIndex;
                    if(outputIndex == -1) continue;
                    int len = m_ruleOutputs[outputIndex];
                    for (int j = outputIndex + 1; j <= outputIndex + len; j++)
                    {
                        float3 rndDir = UnityEngine.Random.insideUnitSphere;
                        rndDir = math.normalize(rndDir);
                        newAgents.Add(new SimpleSGAgent(
                            m_agentsWriteOnly[i].Position,
                            m_agentsWriteOnly[i].Direction,
                            m_agentsWriteOnly[i].Position,
                            new float4(rndDir, 0.0f),
                            m_ruleOutputs[j],
                            0.0f
                        ));
                    }
                }else{
                    newAgents.Add(new SimpleSGAgent(
                        m_agentsWriteOnly[i].Position,
                        m_agentsWriteOnly[i].Direction,
                        m_agentsWriteOnly[i].Position,
                        m_agentsWriteOnly[i].RandomDirection,
                        m_agentsWriteOnly[i].AgentType,
                        0.0f
                    ));
                }
            }
            //Debug.Log(newAgents.Length);
            m_agentsWriteOnly.Dispose();
            m_agentsWriteOnly = newAgents;
        }

        private void SimulateAgents()
        {
            SimpleSGJob job = new SimpleSGJob()
            {
                Center = new float4(m_center.position, 0.0f),
                Delta = m_simulationTimeStep,
                AgentsReadOnly = m_agentsReadonly,
                AgentsWriteOnly = m_agentsWriteOnly,
                Weights = m_agentsWeights,
                SeparationDistance = 0.0f,
                CohesionDistance = 0.0f
            };
            job.Schedule().Complete();
        }

        private void UpdateGrammarRules()
        {
            m_ruleProbabilities = new NativeArray<ProbabilityOutputIndex>(m_grammarConfiguration.m_productionRules.Length, Allocator.Persistent);
            for (int i = 0; i < m_ruleProbabilities.Length; i++)
            {
                int index = GetAgentTypeOfChar(m_grammarConfiguration.m_productionRules[i].Input);
                if (index != -1)
                {
                    m_ruleProbabilities[index] = new ProbabilityOutputIndex()
                    {
                        Probability = m_grammarConfiguration.m_productionRules[i].Probability,
                        OutputIndex = -1
                    };
                }
            }
            m_ruleOutputs = new NativeList<int>(m_grammarConfiguration.m_productionRules.Length * 2, Allocator.Persistent);
            int ruleOutputIndex = 0;
            for (int i = 0; i < m_ruleProbabilities.Length; i++)
            {
                int index = GetAgentTypeOfChar(m_grammarConfiguration.m_productionRules[i].Input);
                if (index != -1)
                {
                    string outputString = m_grammarConfiguration.m_productionRules[i].Output;
                    if (outputString.Length > 0)
                    {
                        m_ruleOutputs.Add(outputString.Length);
                    }
                    for (int j = 0; j < outputString.Length; j++)
                    {
                        int outputCharIndex = GetAgentTypeOfChar(m_grammarConfiguration.m_productionRules[i].Output[j]);
                        m_ruleOutputs.Add(outputCharIndex);
                    }
                    m_ruleProbabilities[index] = new ProbabilityOutputIndex()
                    {
                        Probability = m_ruleProbabilities[index].Probability,
                        OutputIndex = outputString.Length == 0 ? -1 : ruleOutputIndex
                    };
                    if (outputString.Length > 0)
                    {
                        ruleOutputIndex += outputString.Length + 1;
                    }
                }
            }
        }
        private int GetAgentTypeOfChar(char character)
        {
            for (int i = 0; i < m_grammarConfiguration.m_agents.Length; i++)
            {
                if(m_grammarConfiguration.m_agents[i].AgentType == character)
                {
                    return i;
                }
            }
            return -1;
        }
        private void UpdateGrammarWeights()
        {
            int agentTypesCount = m_grammarConfiguration.m_agents.Length;
            m_agentsWeights = new NativeArray<float>(agentTypesCount * 3, Allocator.Persistent);
            for (int i = 0; i < agentTypesCount; i++)
            {
                m_agentsWeights[i * 3] = m_grammarConfiguration.m_agents[i].PhototropismWeight;
                m_agentsWeights[i * 3 + 1] = m_grammarConfiguration.m_agents[i].GravityWeight;
                m_agentsWeights[i * 3 + 2] = m_grammarConfiguration.m_agents[i].RandomWeight;
            }
            m_agentColors = new NativeArray<float3>(agentTypesCount, Allocator.Persistent);
            for (int i = 0; i < agentTypesCount; i++)
            {
                m_agentColors[i] = m_grammarConfiguration.m_agents[i].Color;
            }
        }

        private void PlacePoints()
        {
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                m_agentsReadonly[i] = m_agentsReadonly[i].SetTravelDistance(m_agentsReadonly[i].TravelDistance + math.distance(m_agentsReadonly[i].Position, m_agentsReadonly[i].LastPosition));
                if (m_agentsReadonly[i].TravelDistance > m_placementTravelDistance)
                {
                    m_leafModifier.Add(m_agentsReadonly[i].Position.xyz, m_artifactRadius, m_pointCount, m_agentColors[m_agentsReadonly[i].AgentType], true, m_agentsReadonly[i].AgentType);
                }
            }
        }

        private void Update() {
            if (m_simulationTimeStep < Time.time - m_lastTime)
            {
                TimeStepUpdate();
                m_lastTime = Time.time;
            }
        }

        private void OnDrawGizmosSelected() {
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                float3 color = m_agentColors[m_agentsReadonly[i].AgentType];
                Gizmos.color = new Color(color.x, color.y, color.z);
                Gizmos.DrawMesh(m_agentMesh, m_agentsReadonly[i].Position.xyz, Quaternion.LookRotation(m_agentsReadonly[i].Direction.xyz, Vector3.up));
            }
        }

        private void OnDestroy() {
            m_agentsReadonly.Dispose();
            m_agentsWriteOnly.Dispose();
            m_agentsWeights.Dispose();
            m_agentColors.Dispose();
            m_ruleProbabilities.Dispose();
            m_ruleOutputs.Dispose();
        }

        [Serializable]
        private struct ProbabilityOutputIndex
        {
            public float Probability;
            public int OutputIndex;
        }
    }

}