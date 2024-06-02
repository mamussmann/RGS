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
using PCMTool.Tree;
using RGS.Configurations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.TMP_Dropdown;

namespace RGS.UI
{
    public class UIHeatmapSettings : MonoBehaviour
    {
        [SerializeField] private LeafRenderer m_leafRenderer;
        [SerializeField] private TMP_Dropdown m_dropdown;
        [SerializeField] private Toggle m_heatmapGradient;
        private int[] m_dropdownIndexMapping;
        [SerializeField] private Slider m_slider;
        [SerializeField] private TMP_Text m_sliderText;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_uiMediator.OnShowHeatmap.AddListener(HandleShowHeatMap);
            m_slider.onValueChanged.AddListener(HandleThresholdValueChanged);
            m_dropdown.onValueChanged.AddListener(HandleHeatmapPointTypeChanged);
            m_dropdown.ClearOptions();
            List<OptionData> options = new List<OptionData>();
            List<int> indexMapping = new List<int>();
            options.Add(new OptionData("Nothing"));
            indexMapping.Add(-1);
            for (int i = 0; i < RGSConfiguration.Get().PointTypes.Length; i++)
            {
                if(RGSConfiguration.Get().PointTypes[i].ShowAsHeatmap)
                {
                    string name = RGSConfiguration.Get().PointTypes[i].Identifier;
                    if(name.Equals("Soil"))
                    {
                        name = "Water";
                    }
                    options.Add(new OptionData(name));
                    indexMapping.Add(i);
                }
            }
            m_dropdownIndexMapping = indexMapping.ToArray();
            m_dropdown.AddOptions(options);
            m_heatmapGradient.onValueChanged.AddListener(HandleValueChange);
            m_leafRenderer.ShowHeatmapGradient(m_heatmapGradient.isOn);
        }

        private void HandleValueChange(bool isVisible)
        {
            m_leafRenderer.ShowHeatmapGradient(isVisible);
        }

        private void HandleShowHeatMap(int pointIndex)
        {
            for (int i = 0; i < m_dropdownIndexMapping.Length; i++)
            {
                if(m_dropdownIndexMapping[i] == pointIndex) 
                {
                    m_dropdown.value = i;
                    m_dropdown.RefreshShownValue();
                    HandleHeatmapPointTypeChanged(i);
                    m_slider.value = 0.0f;
                    HandleThresholdValueChanged(0);
                    return;
                }
            }
        }

        private void HandleHeatmapPointTypeChanged(int dropdownIndex)
        {
            m_leafRenderer.SetHeatmapPointType(m_dropdownIndexMapping[dropdownIndex]);
        }

        private void HandleThresholdValueChanged(float value)
        {
            m_sliderText.text = $"Threshold:{value.ToString("0.00")}";
            m_leafRenderer.SetHeatMapThreshold(value);
        }

        private void OnDestroy() {
            m_slider.onValueChanged.RemoveListener(HandleThresholdValueChanged);
            m_dropdown.onValueChanged.RemoveListener(HandleHeatmapPointTypeChanged);
            m_uiMediator.OnShowHeatmap.RemoveListener(HandleShowHeatMap);
            m_heatmapGradient.onValueChanged.AddListener(HandleValueChange);
        }
    }

}
