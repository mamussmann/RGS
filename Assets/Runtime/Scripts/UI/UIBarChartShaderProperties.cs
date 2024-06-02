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
using UnityEngine;

namespace RGS.UI
{
    public class UIBarChartShaderProperties : MonoBehaviour
    {
        public const int MAX_BARS = 32;
        [SerializeField] private Material m_material;
        [SerializeField] [Range(0.0f, 0.5f)] private float m_barWidth;
        private BarInfo[] m_barInfoList = new BarInfo[MAX_BARS];
        private const string m_barWidthShaderName = "_BarWidthFactor";
        private const string m_barCountShaderName = "_BarCount";
        private const string m_barValuesShaderName = "_BarValues";
        private const string m_barMinValuesShaderName = "_BarMinValues";
        private const string m_barMinColorShaderName = "_BarMinColor";
        private const string m_BarColorShaderName = "_BarColor";
        private float[] m_barValues = new float[MAX_BARS];
        private float[] m_barMinValues = new float[MAX_BARS];
        public void SetBarColors(Color valueColor, Color minColor)
        {
            m_material.SetColor(m_BarColorShaderName, valueColor);
            m_material.SetColor(m_barMinColorShaderName, minColor);
        }
        public void SetBarValue(int index, float value, float minValue)  
        {
            m_barInfoList[index].CurrentValue = value;
            m_barInfoList[index].MinValue = minValue;
        }
        public void PlotBarChart(int count, float maxValue) {
            count = Mathf.Min(MAX_BARS, count);
            for (int i = 0; i < count; i++)
            {
                m_barValues[i] = m_barInfoList[i].CurrentValue / maxValue;
                m_barMinValues[i] = Mathf.Min(m_barInfoList[i].MinValue, m_barInfoList[i].CurrentValue) / maxValue;
            }
            m_material.SetFloat(m_barWidthShaderName, m_barWidth);
            m_material.SetInteger(m_barCountShaderName, count);
            m_material.SetFloatArray(m_barValuesShaderName, m_barValues);
            m_material.SetFloatArray(m_barMinValuesShaderName, m_barMinValues);
        }
        [Serializable]
        private struct BarInfo
        {
            public float CurrentValue;
            public float MinValue;
        }
    }
}
