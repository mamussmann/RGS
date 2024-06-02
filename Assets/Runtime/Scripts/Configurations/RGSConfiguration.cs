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
using NaughtyAttributes;
using UnityEngine;

namespace RGS.Configurations
{

    [CreateAssetMenu(fileName = "RGSConfiguration", menuName = "SGR/RGSConfiguration", order = 1)]
    public class RGSConfiguration : ScriptableObject
    {
        public float OverlapCheckRadius => m_overlapCheckRadius;
        [SerializeField] private float m_overlapCheckRadius;
        public float UnitTropismVectorMagnitude => m_unitTropismVectorMagnitude;
        [SerializeField] private float m_unitTropismVectorMagnitude;
        public float SimulationTimestep => m_simulationTimeStep;
        [SerializeField][Min(0.001f)] private float m_simulationTimeStep;
        public int NutrientPointSimStep => m_nutrientPointSimStep;
        [Tooltip("Amount of simulation update steps between point and nutrient update.")]
        [SerializeField][Min(1)] private int m_nutrientPointSimStep;
        public float NutrientOverlapRadius => m_nutrientOverlapRadius;
        [SerializeField] private float m_nutrientOverlapRadius;
        public int PointTypesCount => m_pointTypes.Length;
        public float WaterAbsorbPerTimeStep => m_waterAbsorbPerTimeStep;
        [Label("Water Absorb Per Time")][SerializeField]private float m_waterAbsorbPerTimeStep;
        public float WaterUsagePerTimeStep => m_waterUsagePerTimeStep;
        [Label("Water Usage Per Time")][SerializeField]private float m_waterUsagePerTimeStep;
        public float NutrientAbsorbPerTimeStep => m_nutrientAbsorbPerTimeStep;
        [Label("Nutrient Absorb Per Time")][SerializeField]private float m_nutrientAbsorbPerTimeStep;
        public float WaterEvaporationPerTimeStep => m_waterEvaporationPerTimeStep;
        [Label("Water Evaporation Per Time")][SerializeField]private float m_waterEvaporationPerTimeStep;
        public float RootWaterCapacityPerRadius => m_rootWaterCapacityPerRadius;
        [Label("Root Water Capacity Per Radius")][SerializeField]private float m_rootWaterCapacityPerRadius;
        public PointTypeData[] PointTypes => m_pointTypes;
        [SerializeField] private PointTypeData[] m_pointTypes;
        /////////////////////////////////////////
        public DropdownList<int> GetResourcePointDropdownList()
        {
            var dropDownList = new DropdownList<int>();
            for (int i = 0; i < m_pointTypes.Length; i++)
            {
                if(m_pointTypes[i].IsPlantResource) 
                {
                    dropDownList.Add(m_pointTypes[i].Identifier, i);
                }
            }
            return dropDownList;
        }

        public int GetIndexOfPointType(string pointType)
        {
            for (int i = 0; i < m_pointTypes.Length; i++)
            {
                if(m_pointTypes[i].Identifier.Equals(pointType)) 
                {
                    return i;
                }
            }
            return -1;
        }
        private static RGSConfiguration m_instance;
        public static RGSConfiguration Get()
        {
            if(m_instance == null) 
            {
                m_instance = Resources.Load<RGSConfiguration>("RGSConfiguration");
                return m_instance;
            }
            return m_instance;
        }        
    }
}
