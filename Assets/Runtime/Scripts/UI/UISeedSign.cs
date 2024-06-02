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
using UnityEngine.UI;

namespace RGS.UI
{
    [RequireComponent(typeof(Button))]
    public class UISeedSign : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_text;
        private Button m_button;
        private PlantSeedModel m_model;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        public void Setup(PlantSeedModel model)
        {
            m_button = GetComponent<Button>();
            m_model = model;
            m_text.text = model.DisplayName;
            m_button.onClick.AddListener(HandleButtonClicked);
            UpdateScreenPosition();
        }
        public void UpdateScreenPosition()
        {
            transform.position = Camera.main.WorldToScreenPoint(m_model.transform.position);
            Vector3 cameraRelative = Camera.main.transform.InverseTransformPoint(m_model.transform.position);
            gameObject.SetActive(cameraRelative.z > 0.0f);
        }

        private void HandleButtonClicked()
        {
            m_uiMediator.OnSelectPlant.Invoke(m_model.Identifier);
        }

        private void OnDestroy() {
            m_button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

}
