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
using TMPro;
using UnityEngine;

namespace RGS.UI
{

    public class UIRootMetrics : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_rootLengthText;
        [SerializeField] private TMP_Text m_rootDepthText;
        [SerializeField] private TMP_Text m_rootSegmentsText;
        private readonly UIMediator m_uiMeditor = UIMediator.Get();
        private void Start() {
            m_uiMeditor.OnPlantDepthChange.AddListener(HandlePlantDepthChange);
            m_uiMeditor.OnRootLengthChange.AddListener(HandleRootLengthChange);
            m_uiMeditor.OnRootSegmentsChange.AddListener(HandleRootSegmentsChange);
        }

        private void HandlePlantDepthChange(float depth)
        {
            depth *= SessionInfo.Unit_Length_Scale;
            m_rootDepthText.text = $"Maximal depth: {depth.ToString("0.0")} {SessionInfo.Unit_Length}";
        }
        private void HandleRootLengthChange(float length)
        {
            length *= SessionInfo.Unit_Length_Scale;
            m_rootLengthText.text = $"Total root length: {length.ToString("0.0")} {SessionInfo.Unit_Length}";
        }
        private void HandleRootSegmentsChange(int segmentCount)
        {
            m_rootSegmentsText.text = $"Root segments: {segmentCount} ";
        }
        private void OnDestroy() {
            m_uiMeditor.OnPlantDepthChange.RemoveListener(HandlePlantDepthChange);
            m_uiMeditor.OnRootLengthChange.RemoveListener(HandleRootLengthChange);
            m_uiMeditor.OnRootSegmentsChange.RemoveListener(HandleRootSegmentsChange);
        }
    }

}
