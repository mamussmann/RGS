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
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.UI
{
    public class UIPrlottingShaderProperties : MonoBehaviour
    {
        [SerializeField] private Material m_material;
        [SerializeField] private TMP_Text m_maxYValue;
        [SerializeField] private TMP_Text m_maxXValue;
        [SerializeField] private TMP_Text m_minYValue;
        [SerializeField] private TMP_Text m_minXValue;
        [Header("Format")]
        [SerializeField] private string m_XStringFormat = "0.0";
        [SerializeField] private string m_YStringFormat = "0.0";
        [SerializeField] private string m_XAxisUnit = "s";
        [SerializeField] private string m_YAxisUnit = "cm";
        private const int m_sampleCount = 100;
        private const string m_sampleCountShaderName = "_SampleCount";
        private const string m_sampleArrayShaderName = "_Samples";
        private const string m_sampleTypesShaderName = "_SampleTypes";
        private const string m_ColorSampleShaderName = "_ColorSample";
        private const string m_flipXYShaderName = "_flipXY";
        private float[] m_sampleArray = new float[2 * m_sampleCount];
        private bool m_isFlipped = false;
        public void SetFlipXY(bool isFlipped)
        {
            m_material.SetFloat(m_flipXYShaderName, isFlipped ? 1.0f: 0.0f);
            m_isFlipped = isFlipped;
        }
        public void SetColor(Color color, int index)
        {
            m_material.SetColor(m_ColorSampleShaderName + index, color);
        }
        public void PlotData(NativeArray<float> data, int stride, int dataLength, float maxXValue)
        {
            float largestValue = float.NegativeInfinity;
            float smallestValue = float.PositiveInfinity;
            foreach (var sample in data)
            {
                if (sample > largestValue)
                {
                    largestValue = sample;
                }
                else if (sample < smallestValue)
                {
                    smallestValue = sample;
                }
            }
            m_maxXValue.text = $"{maxXValue.ToString(m_XStringFormat)}{m_XAxisUnit}";
            m_maxYValue.text = $"{largestValue.ToString(m_YStringFormat)}{m_YAxisUnit}";
            m_minXValue.text = $"{0}{m_XAxisUnit}";
            m_minYValue.text = $"{smallestValue}{m_YAxisUnit}";
            if(m_isFlipped){
                string maxXText = m_maxXValue.text;
                m_maxXValue.text = m_maxYValue.text;
                m_maxYValue.text = maxXText;
                string minXText = m_minXValue.text;
                m_minXValue.text = m_minYValue.text;
                m_minYValue.text = minXText;
            }
            for (int i = 0; i < stride; i++)
            {
                PlotArray(data, m_sampleArray, largestValue, i, dataLength / stride, stride);
            }
            m_material.SetInteger(m_sampleTypesShaderName, stride);
        }

        private void PlotArray(NativeArray<float> data, float[] samplesArray, float maxValue, int index, int dataLength, int stride)
        {
            for (int i = 0; i < m_sampleCount; i++)
            {
                float x = i / (float)m_sampleCount;
                samplesArray[i * 2] = x;
                if (i >= dataLength) break;
                int dataIndex = (((int)(x * (float)dataLength)) * stride) + index;
                float value = data[dataIndex] / maxValue;
                samplesArray[i * 2 + 1] = value;
            }
            m_material.SetInteger(m_sampleCountShaderName + index, Mathf.Min(dataLength, m_sampleCount));
            m_material.SetFloatArray(m_sampleArrayShaderName + index, samplesArray);
        }
    }

}
