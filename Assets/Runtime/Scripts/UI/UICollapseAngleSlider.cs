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
using RGS.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    public class UICollapseAngleSlider : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_text;
        [SerializeField] private Slider m_slider;
        private SphereTracingPointsRenderer m_renderer;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_renderer = FindObjectOfType<SphereTracingPointsRenderer>();
            m_slider.value = m_renderer.SegmentCollapseAngleDelta / Mathf.PI;
            m_text.text = $"Collapse Angle: {(Mathf.Rad2Deg * m_renderer.SegmentCollapseAngleDelta).ToString("0.0")}";
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
            m_slider.onValueChanged.AddListener(HandleValueChange);
        }

        private void HandleValueChange(float value)
        {
            float angleValue = value * Mathf.PI;
            m_text.text = $"Collapse Angle: {(Mathf.Rad2Deg * angleValue).ToString("0.0")}";
            m_renderer.SegmentCollapseAngleDelta = angleValue;
        }

        private void HandleEventToggleClicked(ToggleEventType type, bool value)
        {
            if(type == ToggleEventType.TOGGLE_CAPSULE_RENDERING)
            {
                m_slider.interactable = value;
            }
        }
        private void OnDestroy() {
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
        }
    }

}
