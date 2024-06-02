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
using System.Collections.Generic;
using RGS.Simulation;
using RGS.UI;
using UnityEngine;

namespace RGS
{

    public class FpsTracker : MonoBehaviour
    {
        private const int MAX_SAMPLES = 1000000;
        public Dictionary<int, List<int>> RootPlayfpsValues = new Dictionary<int, List<int>>();
        public Dictionary<int, List<int>> RootFastForwardfpsValues = new Dictionary<int, List<int>>();
        public Dictionary<int, List<int>> WaterPlayfpsValues = new Dictionary<int, List<int>>();
        public Dictionary<int, List<int>> WaterFastForwardfpsValues = new Dictionary<int, List<int>>();
        public List<int> PausedFPS = new List<int>();
        public List<int> PlayFPS = new List<int>();
        public List<int> FastForwardFPS = new List<int>();
        [SerializeField] private RootGrowthSimulationArea m_rootGrowthSimulationArea;
        private Dictionary<int, List<int>> m_activeRootDict;
        private Dictionary<int, List<int>> m_activeWaterDict;
        private List<int> m_activeFPSList;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            m_uiMediator.OnPauseEvent.AddListener(HandlePauseSimulation);
            m_uiMediator.OnPlayEvent.AddListener(HandlePlaySimulation);
            m_uiMediator.OnFastForwardEvent.AddListener(HandleFastForward);
            HandlePauseSimulation();
        }

        private void HandlePauseSimulation()
        {
            m_activeRootDict = null;
            m_activeWaterDict = null;
            m_activeFPSList = PausedFPS;
        }

        private void HandlePlaySimulation()
        {
            m_activeWaterDict = WaterPlayfpsValues;
            m_activeRootDict = RootPlayfpsValues;
            m_activeFPSList = PlayFPS;
        }

        private void HandleFastForward()
        {
            m_activeWaterDict = WaterFastForwardfpsValues;
            m_activeRootDict = RootFastForwardfpsValues;
            m_activeFPSList = FastForwardFPS;
        }

        void Update()
        {
            int currentFPS = Mathf.Min((int) (1.0f/ Time.smoothDeltaTime), 500);
            if(currentFPS < 0) return;
            if(m_activeWaterDict != null)
            {
                int agentsWaterCount = m_rootGrowthSimulationArea.WaterAgentCount;
                if(!m_activeWaterDict.ContainsKey(agentsWaterCount))
                {
                    m_activeWaterDict.Add(agentsWaterCount, new List<int>());
                }
                if(m_activeWaterDict[agentsWaterCount].Count >= MAX_SAMPLES) {
                    m_activeWaterDict[agentsWaterCount].RemoveAt(0);
                }
                m_activeWaterDict[agentsWaterCount].Add(currentFPS);
            }
            if(m_activeRootDict != null)
            {
                int agentsRootCount = m_rootGrowthSimulationArea.RootAgentCount;
                if(!m_activeRootDict.ContainsKey(agentsRootCount))
                {
                    m_activeRootDict.Add(agentsRootCount, new List<int>());
                }
                if(m_activeRootDict[agentsRootCount].Count >= MAX_SAMPLES) {
                    m_activeRootDict[agentsRootCount].RemoveAt(0);
                }
                m_activeRootDict[agentsRootCount].Add(currentFPS);
            }
            if(m_activeFPSList != null)
            {
                if (m_activeFPSList.Count >= MAX_SAMPLES) {
                    m_activeFPSList.RemoveAt(0);
                }
                m_activeFPSList.Add(currentFPS);
            }
        }
        private void OnDestroy() 
        {
            m_uiMediator.OnPauseEvent.RemoveListener(HandlePauseSimulation);
            m_uiMediator.OnPlayEvent.RemoveListener(HandlePlaySimulation);
            m_uiMediator.OnFastForwardEvent.RemoveListener(HandleFastForward);
        }
    }
    
}
