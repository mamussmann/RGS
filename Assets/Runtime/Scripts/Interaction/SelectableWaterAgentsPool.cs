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
using RGS.Agents;
using RGS.Configuration.UI;
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.UI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Interaction
{

    public class SelectableWaterAgentsPool : MonoBehaviour
    {
        [SerializeField] private GameObject m_selectableAgentPrefab;
        [SerializeField] private Transform m_poolContainer;
        private List<SelectableWaterAgent> m_agentInstances = new List<SelectableWaterAgent>();
        private bool m_isInWaterSelectMode;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_uiMediator.OnSetSphereTracingPhase.AddListener(x => gameObject.SetActive(!x));
            m_interactionMediator.OnPieActionSelected.AddListener(HandlePieActionSelected);
        }

        private void HandlePieActionSelected(PieMenuActionType pieMenuActionType, int index)
        {
            if(pieMenuActionType == PieMenuActionType.SELECT_MODE)
            {
                m_isInWaterSelectMode = index == 2;
            }else {
                m_isInWaterSelectMode = false;
            }
            foreach (var selectableAgent in m_agentInstances)
            {
                if(selectableAgent.gameObject.activeSelf)
                {
                    selectableAgent.SetIsSelectable(m_isInWaterSelectMode);
                }
            }
        }

        public void UpdateAndShowAgents(NativeList<WaterAgentData> activeAgents, WaterAgentConfig waterAgentConfig)
        {
            for (int i = 0; i < activeAgents.Length; i++)
            {
                if(i >= m_agentInstances.Count) {
                    var instance = GameObject.Instantiate(m_selectableAgentPrefab, m_poolContainer);
                    m_agentInstances.Add(instance.GetComponent<SelectableWaterAgent>());
                }
                m_agentInstances[i].gameObject.SetActive(true);
                m_agentInstances[i].SetWaterAgentInformation(Color.blue, 
                    activeAgents[i].Position, math.normalize(activeAgents[i].Velocity),
                    new Models.WaterAgentInfoPanelModel()
                    {
                        DisplayName = "Water Agent",
                        AgentRadius = waterAgentConfig.WaterOverlapCheckRadius,
                        WaterContent = activeAgents[i].WaterContent
                    }
                );
                m_agentInstances[i].SetIsSelectable(m_isInWaterSelectMode);
            }
        }
        public void HideSelectableAgents()
        {
            foreach (var selectableAgent in m_agentInstances)
            {
                selectableAgent.gameObject.SetActive(false);
            }
        }
        private void OnDestroy() {
            m_interactionMediator.OnPieActionSelected.RemoveListener(HandlePieActionSelected);
        }
    }

}
