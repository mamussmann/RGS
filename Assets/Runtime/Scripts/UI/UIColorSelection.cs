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
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{

    public class UIColorSelection : MonoBehaviour
    {
        [SerializeField] private Toggle m_toggle;
        [SerializeField] private Image m_colorPreview;
        [SerializeField] private Slider m_hueSlider;
        [SerializeField] private Slider m_saturationSlider;
        [SerializeField] private Slider m_valueSlider;
        private bool m_useCustomBgColor;
        private readonly UIMediator m_uiButtonMediator = UIMediator.Get();
        private void Awake() {
            m_toggle.onValueChanged.AddListener(HandleUseBgColorToggle);
            m_hueSlider.onValueChanged.AddListener(HandleValueChanged);
            m_saturationSlider.onValueChanged.AddListener(HandleValueChanged);
            m_valueSlider.onValueChanged.AddListener(HandleValueChanged);
        }
        void Start()
        {
            HandleUseBgColorToggle(m_toggle.isOn);
        }
        private void HandleUseBgColorToggle(bool useBgColor)
        {
            m_useCustomBgColor = useBgColor;
            if(useBgColor)
            {
                m_hueSlider.interactable = true;
                m_saturationSlider.interactable = true;
                m_valueSlider.interactable = true;
                UpdateColor();
            } else {
                m_colorPreview.color = Color.gray;
                m_hueSlider.interactable = false;
                m_saturationSlider.interactable = false;
                m_valueSlider.interactable = false;
            }
            m_uiButtonMediator.OnRenderBgColorChange.Invoke(m_colorPreview.color, m_useCustomBgColor);
        }
        private void HandleValueChanged(float value)
        {
            UpdateColor();
            m_uiButtonMediator.OnRenderBgColorChange.Invoke(m_colorPreview.color, m_useCustomBgColor);
        }
        private void UpdateColor()
        {
            m_colorPreview.color = Color.HSVToRGB(m_hueSlider.value, m_saturationSlider.value, m_valueSlider.value);
        }
        private void OnDestroy() {
            m_toggle.onValueChanged.RemoveListener(HandleUseBgColorToggle);
            m_hueSlider.onValueChanged.RemoveListener(HandleValueChanged);
            m_saturationSlider.onValueChanged.RemoveListener(HandleValueChanged);
            m_valueSlider.onValueChanged.RemoveListener(HandleValueChanged);
        }
    }

}
