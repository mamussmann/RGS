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
using System.Collections;
using System.Collections.Generic;
using RGS.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RGS.UI
{
    public class UIBarChart : MonoBehaviour
    {
        [SerializeField] private GameObject m_parentContainer;
        [SerializeField] private GameObject m_labelPrefab;
        [SerializeField] private Transform m_labelContainer;
        [SerializeField] private Image m_valueImage;
        [SerializeField] private Image m_minValueImage;
        [SerializeField] private TMP_Text m_maxYValueText;
        [SerializeField] private UIBarChartShaderProperties m_barChartShaderProperties;
        [SerializeField] private RectTransform m_barChartRect;
        [SerializeField] private Color m_barColor;
        [SerializeField] private Color m_minBarColor;
        private List<GameObject> m_nutrientLabels;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_valueImage.color = m_barColor;
            m_minValueImage.color = m_minBarColor;
            InitTypeLabels(UIBarChartShaderProperties.MAX_BARS);
            m_barChartShaderProperties.SetBarColors(m_barColor, m_minBarColor);
            m_uiMediator.OnPlantNutrientChange.AddListener(HandlePlantNutrientChange);
        }
        private void InitTypeLabels(int count)
        {
            m_nutrientLabels = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var instance = GameObject.Instantiate(m_labelPrefab, m_labelContainer);
                instance.SetActive(false);
                m_nutrientLabels.Add(instance);
            }
        }
        private void HandlePlantNutrientChange(List<NutrientModel> plantNutrients, float maxValue)
        {
            m_maxYValueText.text = maxValue.ToString("0.00");
            for (int i = 0; i < m_nutrientLabels.Count; i++)
            {
                m_nutrientLabels[i].SetActive(i < plantNutrients.Count);
            }
            for (int i = 0; i < plantNutrients.Count; i++)
            {
                m_barChartShaderProperties.SetBarValue(i, plantNutrients[i].PlantAvailableValue, plantNutrients[i].PlantMinValue);
                m_nutrientLabels[i].GetComponentInChildren<TMP_Text>().text = plantNutrients[i].NutrientDisplayName;
            }
            m_barChartShaderProperties.PlotBarChart(plantNutrients.Count, maxValue);
            if(gameObject.activeSelf && m_parentContainer.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(m_barChartRect, Input.mousePosition) && RectTransformUtility.ScreenPointToLocalPointInRectangle(m_barChartRect, Input.mousePosition, Camera.current, out var localPosition))
            {
                Vector2 normalizedPosition = Rect.PointToNormalized(m_barChartRect.rect, localPosition);
                int index = (int) (normalizedPosition.x / (1.0f / plantNutrients.Count));
                index = Mathf.Clamp(index, 0, plantNutrients.Count-1);
                m_uiMediator.OnShowTooltip.Invoke(plantNutrients[index].PlantAvailableValue.ToString("0.000"));
            }
        }

        private void Update() {
            if(!gameObject.activeSelf || !RectTransformUtility.RectangleContainsScreenPoint(m_barChartRect, Input.mousePosition) || !m_parentContainer.activeSelf)
            {
                m_uiMediator.OnHideTooltip.Invoke();
            }
            
        }

        private void OnDestroy() 
        {
            m_uiMediator.OnPlantNutrientChange.RemoveListener(HandlePlantNutrientChange);
        }
    }
}