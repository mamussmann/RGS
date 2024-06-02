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
using RGS.Configurations.Root;
using RGS.Interaction;
using RGS.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{

    public class UIAgentInfoPanel : MonoBehaviour
    {
        [SerializeField] private RootSGConfiguration m_rootSGConfiguration;
        [SerializeField] private Button m_closeButton;
        [Header("InfoContent")]
        [SerializeField] private GameObject m_infoContent;
        [SerializeField] private TMP_Text m_displayName;
        [SerializeField] private TMP_Text m_agentSymbol;
        [SerializeField] private TMP_Text m_rootBranchLength;
        [SerializeField] private TMP_Text m_rootBranchAge;
        [SerializeField] private TMP_Text m_scalingFactors;
        [Header("PartialDerivationContent")]
        [SerializeField] private GameObject m_partialDerivationContent;
        [SerializeField] private TMP_Text m_prodAgentSymbol;
        [SerializeField] private TMP_InputField m_prodResultSymbols;
        [SerializeField] private TMP_Dropdown m_TypeSelection;
        [SerializeField] private Button m_AddTypeButton;
        [SerializeField] private Button m_ConvertButton;
        [SerializeField] private Button m_AddButton;
        private int m_agentIndex;
        private long m_selectedAgentId;
        private Vector3 m_selectedAgentPosition;
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_uiMediator.OnPlayEvent.AddListener(HandleSimulationContinue);
            m_uiMediator.OnFastForwardEvent.AddListener(HandleSimulationContinue);
            m_uiMediator.OnAgentSelected.AddListener(HandleAgentSelected);
            m_closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_AddTypeButton.onClick.AddListener(HandleAddTypeClicked);
            m_prodResultSymbols.onValidateInput += HandleValidateInput;
            m_ConvertButton.onClick.AddListener(HandleConvertClicked);
            m_AddButton.onClick.AddListener(HandleAddClicked);
            m_prodResultSymbols.onSelect.AddListener(_ => SessionInfo.IsInputEnabled = false);
            m_prodResultSymbols.onEndEdit.AddListener(_ => SessionInfo.IsInputEnabled = true);
            FillDropdown();
            m_selectedAgentId = -1;
        }

        private void HandleAddClicked()
        {
            if(m_selectedAgentId == -1) return;
            m_interactionMediator.OnApplyPartialDerivation.Invoke(m_selectedAgentId, m_prodAgentSymbol.text + m_prodResultSymbols.text, false);
        }

        private void HandleConvertClicked()
        {
            if(m_selectedAgentId == -1) return;
            m_interactionMediator.OnApplyPartialDerivation.Invoke(m_selectedAgentId, m_prodResultSymbols.text, true);
        }

        private char HandleValidateInput(string text, int charIndex, char addedChar)
        {   
            for (int i = 0; i < m_rootSGConfiguration.RootSGAgents.Length; i++)
            {
                if(m_rootSGConfiguration.RootSGAgents[i].AgentType == addedChar)
                {
                    return addedChar;
                }
            }
            return (char)0;
        }

        private void HandleAddTypeClicked()
        {
            int index = m_TypeSelection.value;
            m_prodResultSymbols.text = m_prodResultSymbols.text + m_rootSGConfiguration.RootSGAgents[index].AgentType;
        }

        private void FillDropdown()
        {
            List<TMP_Dropdown.OptionData> dropdownList = new List<TMP_Dropdown.OptionData>();
            m_TypeSelection.ClearOptions();
            for (int i = 0; i < m_rootSGConfiguration.RootSGAgents.Length; i++)
            {
                string entry = $"{m_rootSGConfiguration.RootSGAgents[i].AgentType} : {m_rootSGConfiguration.RootSGAgents[i].DisplayName}";
                dropdownList.Add(new TMP_Dropdown.OptionData(entry));
            }
            m_TypeSelection.AddOptions(dropdownList);
        }

        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType == ButtonEventType.SHOW_AGENT_INFOCONTENT)
            {
                m_infoContent.SetActive(true);
                m_partialDerivationContent.SetActive(false);
            }
            if(buttonEventType == ButtonEventType.SHOW_AGENT_PARTIAL_DERIAVTION)
            {
                m_infoContent.SetActive(false);
                m_partialDerivationContent.SetActive(true);
            }
        }

        private void Start() {
            gameObject.SetActive(false);
        }
        void Update()
        {
            if(gameObject.activeSelf){
                transform.position = Camera.main.WorldToScreenPoint(m_selectedAgentPosition);
            }
        }
        private void HandleAgentSelected(long uniqueId, Vector3 position, AgentInfoPanelModel model)
        {
            m_selectedAgentId = uniqueId;
            gameObject.SetActive(true);
            m_prodAgentSymbol.text = model.Symbol;
            transform.position = Camera.main.WorldToScreenPoint(position);
            m_selectedAgentPosition = position;
            m_displayName.text = model.DisplayName;
            m_agentSymbol.text = $"Agent Symbol: {model.Symbol}";
            m_rootBranchLength.text = $"Branch Length: {(model.RootLength * SessionInfo.Unit_Length_Scale).ToString("0.0")}{SessionInfo.Unit_Length}";
            m_rootBranchAge.text = $"Branch Age: {model.RootAge.ToString("0.0")}{SessionInfo.Unit_Time}";
            m_scalingFactors.text = $"sl:{model.ScalingFactors.RootLengthScale.ToString("0.0")}, sθ:{model.ScalingFactors.BranchingAngleScale.ToString("0.0")}, sbd:{model.ScalingFactors.BranchingDensityScale.ToString("0.0")}, sbp:{model.ScalingFactors.BranchingProbabilityScale.ToString("0.0")}, se: {model.ScalingFactors.ElongationScale.ToString("0.0")}, sg:{model.ScalingFactors.GravitropismScale.ToString("0.0")}, sgsa:{model.ScalingFactors.GSAScale.ToString("0.0")}";
        }

        private void HandleSimulationContinue()
        {
            gameObject.SetActive(false);
        }
        
        private void OnDestroy() {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_AddTypeButton.onClick.RemoveListener(HandleAddTypeClicked);
            m_ConvertButton.onClick.RemoveListener(HandleConvertClicked);
            m_AddButton.onClick.RemoveListener(HandleConvertClicked);
        }
    }

}
