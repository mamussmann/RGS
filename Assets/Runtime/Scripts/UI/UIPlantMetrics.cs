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
using UnityEngine;

namespace RGS.UI
{

    public class UIPlantMetrics : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_disableOnValidSelection;
        [SerializeField] private GameObject[] m_enableOnValidSelection;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_uiMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
        }
        private void Start() {
            HandleValidPlantSelection(false);
        }
        private void HandleSelectionChanged(Guid plantGuid)
        {
            PlantSeedModel[] plantmodels = GameObject.FindObjectsOfType<PlantSeedModel>();
            foreach (var plant in plantmodels)
            {
                if(plant.Identifier.Equals(plantGuid))
                {
                    HandleValidPlantSelection(true);
                    return;
                }
            }
            HandleValidPlantSelection(false);
        }
        
        private void HandleValidPlantSelection(bool isValid)
        {
            foreach (var go in m_disableOnValidSelection)
            {
                go.SetActive(!isValid);
            }
            foreach (var go in m_enableOnValidSelection)
            {
                go.SetActive(isValid);
            }
        }

        private void OnDestroy() {
            m_uiMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
        }
    }

}
