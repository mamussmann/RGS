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
using System.Collections.Generic;
using RGS.Agents;
using RGS.Configuration.UI;
using RGS.Configurations.Root;
using RGS.UI;
using Unity.Collections;
using UnityEngine;

namespace RGS.Interaction
{

    public class SelectableAgentsPool : MonoBehaviour
    {
        [SerializeField] private GameObject m_queuedAgentPrefab;
        [SerializeField] private GameObject m_pausedAgentPrefab;
        [SerializeField] private GameObject m_selectableAgentPrefab;
        [SerializeField] private Transform m_poolContainer;
        private List<GameObject> m_queuedAgentInstances = new List<GameObject>();
        private List<GameObject> m_pausedAgentInstances = new List<GameObject>();
        private List<SelectableAgent> m_agentInstances = new List<SelectableAgent>();
        private bool m_isInRootAgentSelectMode;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiButtonMediator = UIMediator.Get();
        private void Awake() {
            m_uiButtonMediator.OnSetSphereTracingPhase.AddListener(x => gameObject.SetActive(!x));
            m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
        }

        private void HandlePieActionSelected(PieMenuActionType pieMenuActionType, int index)
        {
            if(pieMenuActionType == PieMenuActionType.SELECT_MODE)
            {
                m_isInRootAgentSelectMode = index == 1;
            }else {
                m_isInRootAgentSelectMode = false;
            }
            gameObject.SetActive(m_isInRootAgentSelectMode);
        }

        public void ShowQueuedAgents(NativeArray<QueuedRootSGAgentData> queuedAgents, RootSGConfiguration configuration)
        {
            for (int i = 0; i < queuedAgents.Length; i++)
            {
                if(i >= m_queuedAgentInstances.Count) {
                    var instance = GameObject.Instantiate(m_queuedAgentPrefab, m_poolContainer);
                    m_queuedAgentInstances.Add(instance);
                }
                Color color = configuration.RootSGAgents[queuedAgents[i].AgentData.AgentType].UnityColor;
                m_queuedAgentInstances[i].transform.position = queuedAgents[i].AgentData.Position;
                m_queuedAgentInstances[i].gameObject.SetActive(true);
                m_queuedAgentInstances[i].GetComponent<MeshRenderer>().material.color = color;
            }
        }
        public void ShowPausedAgents(NativeArray<PausedRootSGAgentData> pausedAgents, RootSGConfiguration configuration)
        {
            for (int i = 0; i < pausedAgents.Length; i++)
            {
                if(i >= m_pausedAgentInstances.Count) {
                    var instance = GameObject.Instantiate(m_pausedAgentPrefab, m_poolContainer);
                    m_pausedAgentInstances.Add(instance);
                }
                Color color = configuration.RootSGAgents[pausedAgents[i].AgentData.AgentType].UnityColor;
                m_pausedAgentInstances[i].transform.position = pausedAgents[i].AgentData.Position;
                m_pausedAgentInstances[i].gameObject.SetActive(true);
                m_pausedAgentInstances[i].GetComponent<MeshRenderer>().material.color = color;
            }
        }
        public void UpdateAndShowAgents(NativeList<RootSGAgentData> activeAgents, RootSGConfiguration configuration, NativeList<RootSGScalingFactors> agentsScalingFactors, float currentSimulationTime)
        {
            for (int i = 0; i < activeAgents.Length; i++)
            {
                if(i >= m_agentInstances.Count) {
                    var instance = GameObject.Instantiate(m_selectableAgentPrefab, m_poolContainer);
                    m_agentInstances.Add(instance.GetComponent<SelectableAgent>());
                }
                m_agentInstances[i].gameObject.SetActive(true);
                m_agentInstances[i].SetAgentInformation(
                    activeAgents[i].UniqueAgentId, configuration.RootSGAgents[activeAgents[i].AgentType].UnityColor, 
                    activeAgents[i].Position, activeAgents[i].LookDirection,
                    new Models.AgentInfoPanelModel()
                    {
                        DisplayName = configuration.RootSGAgents[activeAgents[i].AgentType].DisplayName,
                        Symbol = ""+configuration.RootSGAgents[activeAgents[i].AgentType].AgentType,
                        RootLength = activeAgents[i].TravelDistance,
                        RootAge =  currentSimulationTime - activeAgents[i].EmergenceTime,
                        ScalingFactors = agentsScalingFactors[i]
                    }
                );
            }
            gameObject.SetActive(m_isInRootAgentSelectMode);
        }
        public void HideSelectableAgents()
        {
            foreach (var selectableAgent in m_agentInstances)
            {
                selectableAgent.gameObject.SetActive(false);
            }
            foreach (var queuedAgent in m_queuedAgentInstances)
            {
                queuedAgent.SetActive(false);
            }
            foreach (var pausedAgent in m_pausedAgentInstances)
            {
                pausedAgent.SetActive(false);
            }
        }
        private void OnDestroy() {
            m_interactionMediator.OnPieActionSelected.RemoveListener(HandlePieActionSelected);
        }
    }

}
