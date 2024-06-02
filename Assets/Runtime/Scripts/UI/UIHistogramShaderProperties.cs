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
using Unity.Collections;
using UnityEngine;

namespace RGS.UI
{

    public class UIHistogramShaderProperties : MonoBehaviour
    {
        [SerializeField] private Material m_material;
        private const string m_MaxValueShaderName = "_MaxValue";
        private const string m_AgentCountShaderName = "_AgentCount";
        private const string m_ArrayLengthShaderName = "_ArrayLength";
        private const string m_BarValuesShaderName = "_BarValues";
        private const string m_BarColorsShaderName = "_BarColors";
        private void Awake() {
            m_material.SetInt(m_ArrayLengthShaderName, 0);
        }
        public void PlotValues(int length, Color[] colors, float maxValue, int agentCount)
        {
            m_material.SetInt(m_ArrayLengthShaderName, length);
            m_material.SetInt(m_AgentCountShaderName, agentCount);
            m_material.SetFloat(m_MaxValueShaderName, maxValue);
            m_material.SetColorArray(m_BarColorsShaderName, colors);
        }
        public void SetBuffer(ComputeBuffer barValues)
        {
            m_material.SetBuffer(m_BarValuesShaderName, barValues);
        }
    }

}
