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
using RGS.Configurations;
using RGS.Configurations.Root;
using RGS.Models;
using TMPro;
using UnityEngine;

namespace RGS.UI
{

    public class UIPlantSimValuesDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_absorbText;
        [SerializeField] private TMP_Text m_agentValuesText;
        [SerializeField] private TMP_Text m_noPlantSelectedText;
        [SerializeField] private TMP_Text m_plantResourceUsageText;
        [SerializeField] private GameObject m_agentResourceUsageTextScrollView;
        [SerializeField] private TMP_Text m_agentResourceUsageText;
        [SerializeField] private RootSGConfiguration m_rootSGConfiguration;
        private PlantSeedModel m_selectedPlant;
        private bool m_plantSelected;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() 
        {
            m_uiMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
            m_uiMediator.OnPlantAgentDataChanged.AddListener(HandlePlantAgentDataChanged);
            HandleUpdatePlantSelection();
            SetAbsorbText();
            SetAgentValuesText();
        }

        private void HandlePlantAgentDataChanged()
        {
            if(m_plantSelected)
            {
                UpdatePlantResourceUsageText();
                UpdateAgentResourceUsageText();
            }
        }

        private void HandleSelectionChanged(Guid plantGuid)
        {
            PlantSeedModel[] plantmodels = GameObject.FindObjectsOfType<PlantSeedModel>();
            m_plantSelected = false;
            m_selectedPlant = null;
            foreach (var plant in plantmodels)
            {
                if(plant.Identifier.Equals(plantGuid))
                {
                    m_selectedPlant = plant;
                    m_plantSelected = true;
                    UpdatePlantResourceUsageText();
                    UpdateAgentResourceUsageText();
                    break;
                }
            }
            HandleUpdatePlantSelection();
        }

        private void UpdatePlantResourceUsageText()
        {
            RGSConfiguration config = RGSConfiguration.Get();
            m_plantResourceUsageText.text = $"Plant resource capacity:\n";
            foreach (var nutrientModel in m_selectedPlant.PlantNutrients)
            {
                int mappingIndex = m_selectedPlant.NutrientIndexMapping[nutrientModel.PointTypeIndex];
                if(mappingIndex != -1) 
                {
                    string pointTypeName = config.PointTypes[nutrientModel.PointTypeIndex].Identifier;
                    float capacity = m_selectedPlant.NutrientCapacity[mappingIndex];
                    m_plantResourceUsageText.text += $"- {pointTypeName} capacity: {capacity.ToString("0.0000")}\n";
                }
            }
            m_plantResourceUsageText.text += $"Additions from shoot:\n";
            foreach (var nutrientModel in m_selectedPlant.PlantConfiguration.ShootNutrientModel)
            {
                string pointTypeName = config.PointTypes[nutrientModel.PointTypeIndex].Identifier;
                if(nutrientModel.ValuePerTimeStep > 0.0f) {
                    m_plantResourceUsageText.text += $"- {pointTypeName} per time = {nutrientModel.ValuePerTimeStep.ToString("0.0000")}\n";
                }
            }
            m_plantResourceUsageText.text += $"Usage of shoot:\n";
            foreach (var nutrientModel in m_selectedPlant.PlantConfiguration.ShootNutrientModel)
            {
                string pointTypeName = config.PointTypes[nutrientModel.PointTypeIndex].Identifier;
                if(nutrientModel.ValuePerTimeStep < 0.0f) {
                    m_plantResourceUsageText.text += $"- {pointTypeName} per time = {nutrientModel.ValuePerTimeStep.ToString("0.0000")}\n";
                }
            }
        }

        private void UpdateAgentResourceUsageText()
        {
            RGSConfiguration config = RGSConfiguration.Get();
            m_agentResourceUsageText.text = $"Production requirements:\n";
            foreach (var colorRootTypeTuple in m_selectedPlant.RootTypeColorList)
            {
                string hexColor = ColorUtility.ToHtmlStringRGB(colorRootTypeTuple.Item1 * 0.5f);
                RootSGAgent rootAgent = m_rootSGConfiguration.RootSGAgents[colorRootTypeTuple.Item2];
                string displayName = rootAgent.DisplayName;
                m_agentResourceUsageText.text += $"{rootAgent.AgentType}: <color=#{hexColor}>{displayName}</color>\n";
                foreach (var requiredResource in rootAgent.RequiredResources)
                {
                    string pointTypeName = config.PointTypes[requiredResource.ResourceType].Identifier;
                    m_agentResourceUsageText.text += $"- {pointTypeName} : {requiredResource.RequiredAmount}\n";
                }
                m_agentResourceUsageText.text += $"\n";
            }
        }

        private void HandleUpdatePlantSelection()
        {
            m_noPlantSelectedText.gameObject.SetActive(!m_plantSelected);
            m_plantResourceUsageText.gameObject.SetActive(m_plantSelected);
            m_agentResourceUsageTextScrollView.gameObject.SetActive(m_plantSelected);
        }

        private void SetAbsorbText()
        {
            RGSConfiguration config = RGSConfiguration.Get();
            m_absorbText.text = $"Root Water Absorb per Time = {config.WaterAbsorbPerTimeStep}\n";
            m_absorbText.text += $"Root Water Usage per Time = {config.WaterUsagePerTimeStep}\n";
            m_absorbText.text += $"Root Nutrient Absorb per Time = {config.NutrientAbsorbPerTimeStep}\n";
            m_absorbText.text += $"Soil Water Evaporation per Time = {config.WaterEvaporationPerTimeStep}";
        }

        private void SetAgentValuesText()
        {
            m_agentValuesText.text = $"Root agent point overlap radius = {(RGSConfiguration.Get().OverlapCheckRadius * SessionInfo.Unit_Length_Scale).ToString("0.00")} {SessionInfo.Unit_Length}\n";
            m_agentValuesText.text += $"Root segement length = {(m_rootSGConfiguration.SegmentLength * SessionInfo.Unit_Length_Scale).ToString("0.00")} {SessionInfo.Unit_Length}\n";
            m_agentValuesText.text += $"Water agent point overlap radius = {(m_rootSGConfiguration.WaterAgentConfig.WaterOverlapCheckRadius * SessionInfo.Unit_Length_Scale).ToString("0.00")} {SessionInfo.Unit_Length}";
        }

        private void OnDestroy() 
        {
            m_uiMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
            m_uiMediator.OnPlantAgentDataChanged.RemoveListener(HandlePlantAgentDataChanged);
        }
    }

}