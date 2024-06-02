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
using System.Linq;
using RGS.Configurations.Root;
using RGS.Models;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace RGS.UI
{

    public class UIHistogramDensityProfile : MonoBehaviour
    {
        [SerializeField] private RootSGConfiguration m_rootSGConfiguration;
        [SerializeField] private TMP_Text m_titleText;
        [SerializeField] private TMP_Text m_minYValueText;
        [SerializeField] private TMP_Text m_maxYValueText;
        [SerializeField] private TMP_Text m_maxXValueText;
        [SerializeField] private GameObject m_typeLabelPrefab;
        [SerializeField] private Transform m_typeLabelContainer;
        [SerializeField] [Min(0.001f)] private float m_binSize;
        [SerializeField] private UIHistogramShaderProperties m_HistogramShaderProperties;
        private List<UITypeLabel> m_typeLabelList;
        private ComputeBuffer m_histogramSamplesBuffer;
        private Color[] m_colors;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_typeLabelList = new List<UITypeLabel>();
            m_histogramSamplesBuffer = new ComputeBuffer(4096, 4, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            m_colors = new Color[32];
            InitTypeLabels(10);
            m_titleText.text = $"Root segments per depth\n(segment length={(m_rootSGConfiguration.SegmentLength * 100.0f).ToString("0.00")} cm \n bin size={(m_binSize * 100.0f).ToString("0.00")}cm)";
            m_uiMediator.OnRootDensityPlottingChange.AddListener(HandleRootDensityPlottingChange);
            m_HistogramShaderProperties.SetBuffer(m_histogramSamplesBuffer);
        }
        private void HandleRootDensityPlottingChange(List<RootSegment> data, List<Tuple<Color,int>> rootTypeColorList, Vector2 minMax)
        {
            ShowLabels(rootTypeColorList);
            if(data.Count <= 1)
            {
                m_HistogramShaderProperties.PlotValues(0, m_colors, 0.0f, rootTypeColorList.Count);
                return;
            }
            float height = minMax.y - minMax.x;
            int binCount = Mathf.CeilToInt(height / m_binSize);
            int totalSampleCount = rootTypeColorList.Count * binCount;
            var histogramSamples = m_histogramSamplesBuffer.BeginWrite<float>(0, totalSampleCount);
            for (int i = 0; i < histogramSamples.Length; i++)
            {
                histogramSamples[i] = 0.0f;
            }
            float maxValue = 0.0f;
            foreach (var segment in data)
            {
                int binIndex = (int)((minMax.y - segment.Center.y) / m_binSize);
                int index = 0;
                for (int i = 0; i < rootTypeColorList.Count; i++)
                {
                    if(rootTypeColorList[i].Item2 == segment.RootType) {
                        index = i;
                        break;
                    }
                }
                histogramSamples[binIndex * rootTypeColorList.Count + index] += 1;
                if(histogramSamples[binIndex * rootTypeColorList.Count + index] > maxValue) 
                {
                    maxValue = histogramSamples[binIndex * rootTypeColorList.Count + index] + 1;
                }
            }
            m_histogramSamplesBuffer.EndWrite<float>(totalSampleCount);
            var colors = rootTypeColorList.Select(x => x.Item1).ToArray();
            for (int i = 0; i < colors.Length; i++)
            {
                m_colors[i] = colors[i];
            }
            m_maxYValueText.text = $"{(minMax.y * SessionInfo.Unit_Length_Scale).ToString("0.0")}{SessionInfo.Unit_Length}";
            m_minYValueText.text = $"{(minMax.x * SessionInfo.Unit_Length_Scale).ToString("0.0")}{SessionInfo.Unit_Length}";
            m_maxXValueText.text = $"{maxValue}";
            m_HistogramShaderProperties.PlotValues(totalSampleCount, m_colors, maxValue, rootTypeColorList.Count);
        }
        private void ShowLabels(List<Tuple<Color,int>> typeLabels)
        {
            for (int i = 0; i < m_typeLabelList.Count; i++)
            {
                m_typeLabelList[i].gameObject.SetActive(i < typeLabels.Count);
            }
            for (int i = 0; i < Mathf.Min(m_typeLabelList.Count, typeLabels.Count); i++)
            {
                m_typeLabelList[i].SetLabel(typeLabels[i].Item1, typeLabels[i].Item2);
            }
        }
        private void InitTypeLabels(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = GameObject.Instantiate(m_typeLabelPrefab, m_typeLabelContainer);
                m_typeLabelList.Add(instance.GetComponent<UITypeLabel>());
                m_typeLabelList[m_typeLabelList.Count-1].SetLabel(UnityEngine.Random.ColorHSV(), i);
            }
        }
        private void OnDestroy() 
        {
            m_histogramSamplesBuffer.Dispose();
            m_uiMediator.OnRootDensityPlottingChange.RemoveListener(HandleRootDensityPlottingChange);
        }

    }

}
