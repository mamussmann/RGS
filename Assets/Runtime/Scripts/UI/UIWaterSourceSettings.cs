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
using RGS.Models;
using TMPro;
using UnityEngine;

namespace RGS.UI
{

    public class UIWaterSourceSettings : MonoBehaviour
    {
        [SerializeField] private TMP_InputField m_countByArea;
        [SerializeField] private TMP_InputField m_intervalTime;
        [SerializeField] private TMP_Dropdown m_shapeDropdown;
        private WaterSource m_waterSource;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_countByArea.onSubmit.AddListener(HandleAreaSubmit);
            m_intervalTime.onSubmit.AddListener(HandleIntervalSubmit);
            m_countByArea.onEndEdit.AddListener(HandleAreaSubmit);
            m_intervalTime.onEndEdit.AddListener(HandleIntervalSubmit);
            m_shapeDropdown.onValueChanged.AddListener(HandleDropDownChange);
            m_uiMediator.OnPauseEvent.AddListener(HandlePause);
            m_uiMediator.OnPlayEvent.AddListener(HandlePlay);
            m_uiMediator.OnFastForwardEvent.AddListener(HandlePlay);
        }

        private void HandlePause()
        {
            m_countByArea.interactable = true;
            m_intervalTime.interactable = true;
            m_shapeDropdown.interactable = true;
        }

        private void HandlePlay()
        {
            m_countByArea.interactable = false;
            m_intervalTime.interactable = false;
            m_shapeDropdown.interactable = false;
        }

        private void Start() 
        {
            m_waterSource = GameObject.FindAnyObjectByType<WaterSource>();
            if(m_waterSource == null) return;
            m_countByArea.text = ""+m_waterSource.CountPerArea;
            m_intervalTime.text = ""+m_waterSource.TimeInterval;
            m_shapeDropdown.value = (int) m_waterSource.WaterSourceShape;
            m_shapeDropdown.RefreshShownValue();
        }


        private void HandleDropDownChange(int index)
        {
            if(m_waterSource == null) return;
            m_waterSource.WaterSourceShape = (WaterSourceShape)index;
            m_waterSource.UpdateShape();
        }

        private void HandleIntervalSubmit(string text)
        {
            if(m_waterSource == null) return;
            m_waterSource.TimeInterval = float.Parse(text);
        }

        private void HandleAreaSubmit(string text)
        {
            if(m_waterSource == null) return;
            m_waterSource.SetCountPerArea(float.Parse(text));
        }

        private void OnDestroy() 
        {
            m_countByArea.onEndEdit.RemoveListener(HandleAreaSubmit);
            m_intervalTime.onEndEdit.RemoveListener(HandleIntervalSubmit);
            m_countByArea.onSubmit.RemoveListener(HandleAreaSubmit);
            m_intervalTime.onSubmit.RemoveListener(HandleIntervalSubmit);
            m_shapeDropdown.onValueChanged.RemoveListener(HandleDropDownChange);
            m_uiMediator.OnPauseEvent.RemoveListener(HandlePause);
            m_uiMediator.OnPlayEvent.RemoveListener(HandlePlay);
            m_uiMediator.OnFastForwardEvent.RemoveListener(HandlePlay);
        }
    }

}
