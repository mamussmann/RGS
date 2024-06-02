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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    public class UIMetricsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject m_metricsSection;
        [SerializeField] private GameObject m_simValueSection;
        [SerializeField] private Button m_panelOpenCloseButton;
        [SerializeField] private Transform m_outsideButtonRefTransform;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private bool m_isVisible = false;
        private void Awake() {
            Vector3 position = transform.position;
            position.x = m_isVisible ? 25: -325.0f;
            transform.position = position;

            position = m_panelOpenCloseButton.transform.position;
            position.x = m_isVisible ? m_outsideButtonRefTransform.position.x: 0.0f;
            m_panelOpenCloseButton.GetComponentInChildren<TMP_Text>().text = m_isVisible ? "<" : ">";
            m_panelOpenCloseButton.transform.position = position;
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_panelOpenCloseButton.onClick.AddListener(HandleOpenCloseButtonClicked);
        }

        private void HandleOpenCloseButtonClicked()
        {
            m_isVisible = !m_isVisible;
            ChangePanelVisibility(m_isVisible);
            m_panelOpenCloseButton.GetComponentInChildren<TMP_Text>().text = m_isVisible ? "<" : ">";
        }
        void Start()
        {
            m_metricsSection.SetActive(true);
            m_simValueSection.SetActive(false);
        }
        private void HandleEventToggleClicked(ToggleEventType toggleEventType, bool value)
        {
            if(toggleEventType == ToggleEventType.TOGGLE_METRICS) 
            {
                m_isVisible = value;
                m_panelOpenCloseButton.GetComponentInChildren<TMP_Text>().text = m_isVisible ? "<" : ">";
                ChangePanelVisibility(value);
            } 
        }

        private void ChangePanelVisibility(bool value)
        {
            LeanTween.cancel(gameObject);
            LeanTween.cancel(m_panelOpenCloseButton.gameObject);
            LeanTween.moveX(gameObject, value ? 25: -325.0f, 0.1f).setEase(value ? LeanTweenType.easeOutQuad : LeanTweenType.easeInQuad);
            LeanTween.moveX(m_panelOpenCloseButton.gameObject, value ? m_outsideButtonRefTransform.position.x: 0.0f, 0.1f).setEase(value ? LeanTweenType.easeOutQuad : LeanTweenType.easeInQuad);
        }

        private void HandleEventButtonClicked(ButtonEventType eventType)
        {
            if(eventType == ButtonEventType.SHOW_METRICS) {
                m_metricsSection.SetActive(true);
                m_simValueSection.SetActive(false);
            } else if(eventType == ButtonEventType.SHOW_SIMVALUES)
            {
                m_simValueSection.SetActive(true);
                m_metricsSection.SetActive(false);
            }
        }

        private void Update() {
            if(!SessionInfo.IsInputEnabled) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                m_uiMediator.OnEventToggleClicked.Invoke(ToggleEventType.TOGGLE_METRICS, !m_isVisible);
            }
        }
        private void OnDestroy() {
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_panelOpenCloseButton.onClick.RemoveListener(HandleOpenCloseButtonClicked);
        }
    }
}
