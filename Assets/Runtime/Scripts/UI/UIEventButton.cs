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
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    [RequireComponent(typeof(Button))]
    public class UIEventButton : MonoBehaviour
    {
        [SerializeField] private ButtonEventType m_buttonEvent;
        [SerializeField] private bool m_toggleInteractableOnEvent;
        [SerializeField] [EnableIf("m_toggleInteractableOnEvent")] private ButtonEventType m_enableEvent;
        [SerializeField] [EnableIf("m_toggleInteractableOnEvent")] private ButtonEventType m_disableEvent;
        private Button m_button;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_button = GetComponent<Button>();
            m_button.onClick.AddListener(() => m_uiMediator.OnEventButtonClicked.Invoke(m_buttonEvent));
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
        }

        private void HandleEventButtonClicked(ButtonEventType eventType)
        {
            if(!m_toggleInteractableOnEvent) return;
            if(m_enableEvent == eventType) {
                m_button.interactable = true;
            } else if(m_disableEvent == eventType)
            {
                m_button.interactable = false;
            }
        }

        private void OnDestroy() 
        {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
        }
    }
}
