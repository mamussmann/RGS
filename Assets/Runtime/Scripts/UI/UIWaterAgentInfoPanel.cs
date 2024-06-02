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
using RGS.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{

    public class UIWaterAgentInfoPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_displayName;
        [SerializeField] private TMP_Text m_agentRadius;
        [SerializeField] private TMP_Text m_waterContent;
        [SerializeField] private Button m_closeButton;
        private Vector3 m_selectedAgentPosition;
        private int m_agentIndex;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Start() {
            m_uiMediator.OnPlayEvent.AddListener(HandleSimulationContinue);
            m_uiMediator.OnFastForwardEvent.AddListener(HandleSimulationContinue);
            m_uiMediator.OnWaterAgentSelected.AddListener(HandleWaterAgentSelected);
            gameObject.SetActive(false);
            m_closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void Update() {
            
            if(gameObject.activeSelf){
                transform.position = Camera.main.WorldToScreenPoint(m_selectedAgentPosition);
            }
        }

        private void HandleWaterAgentSelected(Vector3 position, WaterAgentInfoPanelModel model)
        {
            gameObject.SetActive(true);
            transform.position = Camera.main.WorldToScreenPoint(position);
            m_selectedAgentPosition = position;
            m_displayName.text = model.DisplayName;
            m_agentRadius.text = $"Radius: {(model.AgentRadius * SessionInfo.Unit_Length_Scale).ToString("0.0")}{SessionInfo.Unit_Length}";
            m_waterContent.text = $"Water Content: {model.WaterContent.ToString("0.00")}{SessionInfo.Unit_Water}";
        }

        private void HandleSimulationContinue()
        {
            gameObject.SetActive(false);
        }
    }

}
