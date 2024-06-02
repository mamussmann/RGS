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
using Unity.Collections;
using UnityEngine;

namespace RGS.UI
{

    public class UIPlot : MonoBehaviour
    {
        [SerializeField] private GameObject m_typeLabelPrefab;
        [SerializeField] private Transform m_typeLabelContainer;
        [SerializeField] private UIPrlottingShaderProperties m_plotShaderProperties;
        private List<UITypeLabel> m_typeLabelList;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_typeLabelList = new List<UITypeLabel>();
            InitTypeLabels(10);
            m_plotShaderProperties.SetFlipXY(false);
            m_uiMediator.OnRootLengthPlottingChange.AddListener(HandleRootLengthPlottingChange);
        }
        private void HandleRootLengthPlottingChange(NativeArray<float> data, NativeArray<float> timeData, List<Tuple<Color,int>> rootTypeColorList)
        {
            ShowLabels(rootTypeColorList);
            if(timeData.Length == 0) return; 
            m_plotShaderProperties.PlotData(data, rootTypeColorList.Count, data.Length, timeData[timeData.Length -1]);
            for (int i = 0; i < rootTypeColorList.Count; i++)
            {
                m_plotShaderProperties.SetColor(rootTypeColorList[i].Item1, i);
            }
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
            m_uiMediator.OnRootLengthPlottingChange.RemoveListener(HandleRootLengthPlottingChange);
        }

    }

}
