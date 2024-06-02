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
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    [RequireComponent(typeof(Toggle))]
    public class UIEventToggle : MonoBehaviour
    {
        [SerializeField] private ToggleEventType m_toggleEvent;
        private Toggle m_toggle;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_toggle = GetComponent<Toggle>();
            m_toggle.onValueChanged.AddListener((x) => m_uiMediator.OnEventToggleClicked.Invoke(m_toggleEvent, x));
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
        }
        private void HandleEventToggleClicked(ToggleEventType toggleEventType, bool value)
        {
            if(toggleEventType != m_toggleEvent) return;
            m_toggle.isOn = value;
        }
        private void OnDestroy() {
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
        }
    }
}
