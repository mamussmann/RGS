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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    public class UIControlPanel : MonoBehaviour
    {
        [SerializeField] private Transform m_insideRefTransform;
        [SerializeField] private Transform m_outsideRefTransform;
        [SerializeField] private GameObject m_actionsPanel;
        [SerializeField] private GameObject m_controlsPanel;
        [SerializeField] private GameObject m_settingsPanel;
        [SerializeField] private Button m_panelOpenCloseButton;
        [SerializeField] private Transform m_outsideButtonRefTransform;
        private bool m_isVisible = true;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_panelOpenCloseButton.onClick.AddListener(HandleOpenCloseButtonClicked);
        }

        private void HandleOpenCloseButtonClicked()
        {
            m_isVisible = !m_isVisible;
            ChangePanelVisibility(m_isVisible);
            m_panelOpenCloseButton.GetComponentInChildren<TMP_Text>().text = m_isVisible ? ">" : "<";
        }

        private void Start() {
            m_actionsPanel.SetActive(true);
            m_settingsPanel.SetActive(false);
            m_controlsPanel.SetActive(false);
        }

        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType == ButtonEventType.SHOW_ACTIONS)
            {
                m_actionsPanel.SetActive(true);
                m_settingsPanel.SetActive(false);
                m_controlsPanel.SetActive(false);
            } else if(buttonEventType == ButtonEventType.SHOW_CONTROLS)
            {
                m_settingsPanel.SetActive(false);
                m_controlsPanel.SetActive(true);
                m_actionsPanel.SetActive(false);
            } else if(buttonEventType == ButtonEventType.SHOW_SETTINGS)
            {
                m_settingsPanel.SetActive(true);
                m_controlsPanel.SetActive(false);
                m_actionsPanel.SetActive(false);
            }
        }

        private void ChangePanelVisibility(bool value)
        {
            LeanTween.cancel(gameObject);
            LeanTween.cancel(m_panelOpenCloseButton.gameObject);
            LeanTween.moveX(gameObject, value ? m_insideRefTransform.position.x: m_outsideRefTransform.position.x, 0.1f).setEase(value ? LeanTweenType.easeOutQuad : LeanTweenType.easeInQuad);
            LeanTween.moveX(m_panelOpenCloseButton.gameObject, value ? m_outsideButtonRefTransform.position.x: Screen.width, 0.1f).setEase(value ? LeanTweenType.easeOutQuad : LeanTweenType.easeInQuad);
        }
        private void Update() {
            if(!SessionInfo.IsInputEnabled) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                m_isVisible = !m_isVisible;
                m_panelOpenCloseButton.GetComponentInChildren<TMP_Text>().text = m_isVisible ? ">" : "<";
                ChangePanelVisibility(m_isVisible);
            }
        }
        private void OnDestroy() {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_panelOpenCloseButton.onClick.RemoveListener(HandleOpenCloseButtonClicked);
        }
    }
}
