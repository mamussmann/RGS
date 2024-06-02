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
using RGS.Interaction;
using RGS.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TMPro.TMP_Dropdown;

namespace RGS.UI
{
    [RequireComponent(typeof(Canvas))]
    public class UIRootEvaluationCanvas : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown m_plantSelection;
        [SerializeField] private Toggle m_toggleSimResource;
        private List<PlantSeedModel> m_seedModels;
        private List<OptionData> m_plantOptionData;
        private List<Guid> m_plantGuids;
        private int m_currentSelectionIndex;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private readonly InteractionMediator m_interactionMediator = InteractionMediator.Get();
        private Canvas m_canvas;
        void Awake()
        {
            m_currentSelectionIndex = -1;
            m_canvas = GetComponent<Canvas>();
            m_plantOptionData = new List<OptionData>();
            m_seedModels = new List<PlantSeedModel>();
            m_plantGuids = new List<Guid>();
            var modelsInScene = GameObject.FindObjectsOfType<PlantSeedModel>();
            for (int i = 0; i < modelsInScene.Length; i++)
            {
                AddPlant(modelsInScene[i], i);
            }
            m_plantSelection.options = m_plantOptionData;
            m_plantSelection.onValueChanged.AddListener(HandlePlantSelectionChanged);
            m_uiMediator.OnSelectPlant.AddListener(HandleSelectPlant);
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_toggleSimResource.onValueChanged.AddListener(HandleSimResourceCostToggle);
            m_interactionMediator.OnNewSeedAdded.AddListener(HandleNewSeedAdded);
            m_uiMediator.OnSetUIVisibility.AddListener(HandleSetUIVisibility);
        }
        private void Update() {
            if(Input.GetKeyUp(KeyCode.F9))
            {
                m_canvas.enabled = !m_canvas.enabled;
            }
        }
        private void OnRectTransformDimensionsChange()
        {
            m_uiMediator.OnScreenResolutionChanged.Invoke();
        }
 
        private void HandleSimResourceCostToggle(bool value)
        {
            m_seedModels[m_currentSelectionIndex].SimulateResourceCost = value;
        }

        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType != ButtonEventType.OPEN_EXPORT_FOLDER) return;
            Application.OpenURL(SessionInfo.GetSessionFolderPath());
        }

        private void HandleEventToggleClicked(ToggleEventType type, bool value)
        {
            if(type == ToggleEventType.TOGGLE_METRICS)
            {
                HandleToggleRootMetrics(value);
            }
        }

        private void Start() {
            
            if(m_seedModels.Count > 0){
                m_uiMediator.OnSelectPlant.Invoke(m_plantGuids[0]);
            }
            m_toggleSimResource.interactable = m_currentSelectionIndex != -1;
        }

        private void HandleSetUIVisibility(bool isVisible)
        {
            m_canvas.enabled = isVisible;
        }

        private void AddPlant(PlantSeedModel model, int index) 
        {
            m_seedModels.Add(model);
            m_plantOptionData.Add(new OptionData($"Plant {index}"));
            m_plantGuids.Add(model.Identifier);
        }
        private void HandleToggleRootMetrics(bool value)
        {
            if(value) {
                m_uiMediator.OnShowMetrics.Invoke();
            }else {
                m_uiMediator.OnHideMetrics.Invoke();
            }
        }
        private void HandleNewSeedAdded(PlantSeedModel model)
        {
            AddPlant(model, m_seedModels.Count);
            if(m_seedModels.Count == 1){
                m_uiMediator.OnSelectPlant.Invoke(model.Identifier);
            }
        }

        private void HandleSelectPlant(Guid identifier)
        {
            HandlePlantSelectionChanged(m_plantGuids.IndexOf(identifier));
        }

        private void HandlePlantSelectionChanged(int index)
        {
            m_currentSelectionIndex = index;
            m_uiMediator.OnSelectionChanged.Invoke(m_plantGuids[index]);
            m_seedModels[index].UpdateTrackingVisuals();
            m_toggleSimResource.isOn = m_seedModels[index].SimulateResourceCost;
            m_toggleSimResource.interactable = m_currentSelectionIndex != -1;
            m_plantSelection.value = index;
            m_plantSelection.RefreshShownValue();
        }

        private void OnDestroy() {
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
            m_interactionMediator.OnNewSeedAdded.RemoveListener(HandleNewSeedAdded);
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_uiMediator.OnSelectPlant.RemoveListener(HandleSelectPlant);
        }
    }

}
