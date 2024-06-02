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
using RGS.Agents;
using RGS.Jobs;
using SGR.Configurations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Simulation
{

    public class SimpleBoidsSimulationArea : MonoBehaviour
    {
        [SerializeField] private RuntimeLeafModifier m_leafModifier;
        [SerializeField] private float m_simulationTimeStep;
        [SerializeField] private float m_agentPlaceStepDistance;
        [SerializeField] private Transform m_center;
        [SerializeField] private int m_agentCount;
        [SerializeField] private bool m_LogPosition;
        [SerializeField] private SimpleBoidsConfig m_boidsConfig;
        [SerializeField] private Mesh m_agentMesh;
        [SerializeField] private int m_pointCount;
        [SerializeField] private bool m_shouldPlacePoints = true;
        [SerializeField] private bool m_useCollisionDetection = true;
        private float m_lastTime = 0.1f;
        private NativeArray<SimpleBoidsAgentData> m_agentsReadonly;
        private NativeArray<SimpleBoidsAgentData> m_agentsWriteOnly;
        private NativeArray<float> m_weights;
        private void Start() 
        {
            m_lastTime = Time.time;
            m_agentsReadonly = new NativeArray<SimpleBoidsAgentData>(m_agentCount, Allocator.Persistent);
            m_agentsWriteOnly = new NativeArray<SimpleBoidsAgentData>(m_agentCount, Allocator.Persistent);
            m_weights = new NativeArray<float>(5, Allocator.Persistent);
            UpdateWeights();
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                var pos = UnityEngine.Random.insideUnitSphere * 10.0f;
                var dir = UnityEngine.Random.onUnitSphere;
                float4 direction = new float4(dir.x, dir.y, dir.z, 0.0f);
                float4 position = new float4(pos.x, pos.y, pos.z, 0.0f);
                m_agentsReadonly[i] = new SimpleBoidsAgentData(position, direction, position, math.abs(direction), direction);
            }
        }

        private void TimeStepUpdate()
        {
            UpdateWeights();
            if(m_shouldPlacePoints) {
                PlacePoints();
            }
            SimulateAgents();
            if(m_useCollisionDetection)
            {
                CollisionDetection();
            }
            var tmpAgentList = m_agentsReadonly;
            m_agentsReadonly = m_agentsWriteOnly;
            m_agentsWriteOnly = tmpAgentList;
        }
        private void UpdateWeights()
        {
            m_weights[0] = m_boidsConfig.m_separation;
            m_weights[1] = m_boidsConfig.m_cohesion;
            m_weights[2] = m_boidsConfig.m_alignment;
            m_weights[3] = m_boidsConfig.m_centerWeight;
            m_weights[4] = m_boidsConfig.m_randomWeight;
        }
        private void PlacePoints()
        {
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                if(math.distance(m_agentsReadonly[i].PosCol, m_agentsReadonly[i].LastPosition) > m_agentPlaceStepDistance){
                    m_leafModifier.Add(m_agentsReadonly[i].Position, 0.05f, m_pointCount, m_agentsReadonly[i].Color.xyz, true, i + 1);
                }
            }
        }
        private void SimulateAgents()
        {
            SimpleBoidsJob job = new SimpleBoidsJob(){
                Delta = m_simulationTimeStep,
                AgentsReadOnly = m_agentsReadonly,
                AgentsWriteOnly = m_agentsWriteOnly,
                Center = new float4(m_center.position.x, m_center.position.y, m_center.position.z, 0.0f),
                SeparationDistance = m_boidsConfig.m_separationDistance,
                CohesionDistance = m_boidsConfig.m_cohesionDistance,
                PerceptionAngle = Mathf.Deg2Rad * m_boidsConfig.m_perceptionAngle,
                Weights = m_weights
            };
            job.Schedule(m_agentsReadonly.Length, 128).Complete();
        }
        private void CollisionDetection()
        {
            for (int i = 0; i < m_agentsWriteOnly.Length; i++)
            {
                if (m_leafModifier.OverlapPC(m_agentsWriteOnly[i].Position, 0.1f, 0))
                {
                    m_agentsWriteOnly[i] = m_agentsReadonly[i];
                }
            }
        }
        private void Update()
        {
            if (m_simulationTimeStep < Time.time - m_lastTime)
            {
                TimeStepUpdate();
                m_lastTime = Time.time;
            }
        }

        private void OnDrawGizmos() {
            for (int i = 0; i < m_agentsReadonly.Length; i++)
            {
                if (m_LogPosition) {
                    Debug.Log(m_agentsReadonly[i].Position);
                }
                Gizmos.color = new Color(m_agentsReadonly[i].Color.x, m_agentsReadonly[i].Color.y, m_agentsReadonly[i].Color.z);
                Gizmos.DrawMesh(m_agentMesh, m_agentsReadonly[i].Position, Quaternion.LookRotation(m_agentsReadonly[i].Direction.xyz, Vector3.up));
            }
        }

        private void OnDestroy() {
            m_agentsReadonly.Dispose();
            m_agentsWriteOnly.Dispose();
            m_weights.Dispose();
        }
    }

}
