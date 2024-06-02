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
using NaughtyAttributes;
using RGS.Agents;
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace RGS.Models
{
    public enum WaterSourceShape
    {
        CIRCLE, PLANE
    }


    public class WaterSource : MonoBehaviour
    {
        public float TimeInterval;
        public float CountPerArea;
        [SerializeField] private Transform m_rectangleCube;
        [SerializeField] private Transform m_circleCylinder;
        [Header("Shape")]
        public WaterSourceShape WaterSourceShape;
        [SerializeField] [HideIf("IsCircle")]private float m_width;
        [SerializeField] [HideIf("IsCircle")]private float m_depth;
        [SerializeField] [ShowIf("IsCircle")] private float m_radius;
        private int m_count;
        private float m_lastTime;
        private int m_offset;
        private WaterAgentConfig m_waterAgentConfig;
        public bool IsCircle() {return WaterSourceShape == WaterSourceShape.CIRCLE;}
        private NativeArray<float> m_precomputedRandomValues;
        public void Setup(float simulationTime, WaterAgentConfig waterAgentConfig)
        {
            m_lastTime = simulationTime;
            m_waterAgentConfig = waterAgentConfig;
            m_precomputedRandomValues = new NativeArray<float>(1024, Allocator.Persistent);
            for (int i = 0; i < m_precomputedRandomValues.Length; i++)
            {
                m_precomputedRandomValues[i] = UnityEngine.Random.value - 0.5f;
            }
            m_rectangleCube.gameObject.SetActive(false);
            m_circleCylinder.gameObject.SetActive(false);
            switch (WaterSourceShape)
            {
                case WaterSourceShape.CIRCLE:
                    m_count = Mathf.CeilToInt(CountPerArea * math.PI * m_radius * m_radius * 100.0f);
                    m_circleCylinder.localScale = new Vector3(m_radius * 2.0f, 0.01f, m_radius * 2.0f);
                    break;
                case WaterSourceShape.PLANE:
                    m_count = Mathf.CeilToInt(CountPerArea * m_width * m_depth * 100.0f);
                    m_rectangleCube.localScale = new Vector3(m_width, 0.01f, m_depth);
                    break;
            }
        }
        public void UpdateScale(float3 center, float3 border)
        {
            transform.position = center;
            switch (WaterSourceShape)
            {
                case WaterSourceShape.CIRCLE:
                    m_radius = math.distance(center, border);
                    m_count = Mathf.CeilToInt(CountPerArea * math.PI * m_radius * m_radius * 100.0f);
                    m_circleCylinder.localScale = new Vector3(m_radius * 2.0f, 0.01f, m_radius * 2.0f);
                    m_rectangleCube.gameObject.SetActive(false);
                    m_circleCylinder.gameObject.SetActive(true);
                    break;
                case WaterSourceShape.PLANE:
                    m_width = math.abs(center.x - border.x) * 2.0f;
                    m_depth = math.abs(center.z - border.z) * 2.0f;
                    m_count = Mathf.CeilToInt(CountPerArea * m_width * m_depth * 100.0f);
                    m_rectangleCube.localScale = new Vector3(m_width, 0.01f, m_depth);
                    m_circleCylinder.gameObject.SetActive(false);
                    m_rectangleCube.gameObject.SetActive(true);
                    break;
            }
        }
        public void ShowPreview(bool visible)
        {
            m_circleCylinder.gameObject.SetActive(visible && WaterSourceShape == WaterSourceShape.CIRCLE);
            m_rectangleCube.gameObject.SetActive(visible && WaterSourceShape == WaterSourceShape.PLANE);
        }
        public void SetCountPerArea(float value)
        {
            CountPerArea = value;
            switch (WaterSourceShape)
            {
                case WaterSourceShape.CIRCLE:
                    m_count = Mathf.CeilToInt(CountPerArea * math.PI * m_radius * m_radius * 100.0f);
                    break;
                case WaterSourceShape.PLANE:
                    m_count = Mathf.CeilToInt(CountPerArea * m_width * m_depth * 100.0f);
                    break;
            }
        }
        public void UpdateShape()
        {
            switch (WaterSourceShape)
            {
                case WaterSourceShape.CIRCLE:
                    m_circleCylinder.localScale = new Vector3(m_radius * 2.0f, 0.01f, m_radius * 2.0f);
                    m_circleCylinder.gameObject.SetActive(m_rectangleCube.gameObject.activeSelf);
                    m_rectangleCube.gameObject.SetActive(false);
                    break;
                case WaterSourceShape.PLANE:
                    m_rectangleCube.localScale = new Vector3(m_width, 0.01f, m_depth);
                    m_rectangleCube.gameObject.SetActive(m_circleCylinder.gameObject.activeSelf);
                    m_circleCylinder.gameObject.SetActive(false);
                    break;
            }
        }
        public void TimeStepUpdate(NativeList<WaterAgentData> waterAgents, float simulationTime)
        {
            if(simulationTime - m_lastTime > TimeInterval)
            {
                switch (WaterSourceShape)
                {
                    case WaterSourceShape.CIRCLE:
                        CreateWaterAgentsInCircleJob createWaterAgentsInCircleJob = new CreateWaterAgentsInCircleJob()
                        {
                            Count = m_count,
                            PrecomputedRandomValues = m_precomputedRandomValues,
                            Offset = m_offset,
                            Radius = m_radius,
                            Center = transform.position,
                            WaterAgents = waterAgents,
                            InitalWaterValue = m_waterAgentConfig.InitialWaterValue,
                            InitalVelocity = m_waterAgentConfig.InitialVelocity
                        };
                        createWaterAgentsInCircleJob.Schedule().Complete();
                        break;
                    case WaterSourceShape.PLANE:
                        CreateWaterAgentsInPlaneJob createWaterAgentsInPlaneJob = new CreateWaterAgentsInPlaneJob()
                        {
                            Count = m_count,
                            PrecomputedRandomValues = m_precomputedRandomValues,
                            Offset = m_offset,
                            Width = m_width,
                            Depth = m_depth,
                            Center = transform.position,
                            WaterAgents = waterAgents,
                            InitalWaterValue = m_waterAgentConfig.InitialWaterValue,
                            InitalVelocity = m_waterAgentConfig.InitialVelocity
                        };
                        createWaterAgentsInPlaneJob.Schedule().Complete();
                        break;
                }
                m_offset = (m_offset + m_count * 2) % m_precomputedRandomValues.Length;
                m_lastTime = simulationTime;
            }
        }
        private void OnDestroy() {
            if(m_precomputedRandomValues != null && m_precomputedRandomValues.IsCreated) {
                m_precomputedRandomValues.Dispose();
            }
        }

        private void OnDrawGizmosSelected() 
        {
            Gizmos.color = Color.blue;
            switch (WaterSourceShape)
            {
                case WaterSourceShape.CIRCLE:
                    Gizmos.DrawWireCube(transform.position, new Vector3(m_radius * 2.0f, 0.01f, m_radius * 2.0f));
                    break;
                case WaterSourceShape.PLANE:
                    Gizmos.DrawWireCube(transform.position, new Vector3(m_width, 0.01f, m_depth));
                    break;
            }
        }
    }

}
