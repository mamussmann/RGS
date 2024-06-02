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
using RGS.Models;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RGS.UI
{
    public class UIMediator
    {
        public UnityEvent<bool> OnSetUIVisibility {get;} = new UnityEvent<bool>();
        public UnityEvent<bool> OnSetSphereTracingPhase {get;} = new UnityEvent<bool>();
        //
        public UnityEvent<int> OnShowHeatmap {get;} = new UnityEvent<int>();
        //
        public UnityEvent<Guid> OnSelectPlant {get;} = new UnityEvent<Guid>();
        public UnityEvent OnPlantAgentDataChanged {get;} = new UnityEvent();
        public UnityEvent<Guid> OnSelectionChanged {get;} = new UnityEvent<Guid>();
        public UnityEvent<float> OnPlantDepthChange {get;} = new UnityEvent<float>();
        public UnityEvent<float> OnRootLengthChange {get;} = new UnityEvent<float>();
        public UnityEvent<int> OnRootSegmentsChange {get;} = new UnityEvent<int>();
        public UnityEvent<string> OnShowWarningPopup {get;} = new UnityEvent<string>();
        // data, rootTypeColorList
        public UnityEvent<NativeArray<float>, NativeArray<float>, List<string>> OnRootNutrientPlottingChange {get;} = new UnityEvent<NativeArray<float>, NativeArray<float>, List<string>>();
        public UnityEvent<NativeArray<float>, NativeArray<float>, List<Tuple<Color,int>>> OnRootLengthPlottingChange {get;} = new UnityEvent<NativeArray<float>, NativeArray<float>, List<Tuple<Color,int>>>();
        public UnityEvent<List<RootSegment>, List<Tuple<Color,int>>, Vector2> OnRootDensityPlottingChange {get;} = new UnityEvent<List<RootSegment>, List<Tuple<Color,int>>, Vector2>();
        public UnityEvent<List<NutrientModel>, float> OnPlantNutrientChange {get;} = new UnityEvent<List<NutrientModel>, float>();
        public UnityEvent<ButtonEventType> OnEventButtonClicked {get;} = new UnityEvent<ButtonEventType>();
        public UnityEvent<ToggleEventType, bool> OnEventToggleClicked {get;} = new UnityEvent<ToggleEventType, bool>();
        //
        public UnityEvent<Vector3, WaterAgentInfoPanelModel> OnWaterAgentSelected {get;} = new UnityEvent<Vector3, WaterAgentInfoPanelModel>(); // unique agent id, position in world space
        public UnityEvent<long, Vector3, AgentInfoPanelModel> OnAgentSelected {get;} = new UnityEvent<long, Vector3, AgentInfoPanelModel>(); // unique agent id, position in world space
        // 
        public UnityEvent OnShowMetrics {get;} = new UnityEvent();
        public UnityEvent OnHideMetrics {get;} = new UnityEvent();
        public UnityEvent<string> OnShowTooltip {get;} = new UnityEvent<string>();
        public UnityEvent OnHideTooltip {get;} = new UnityEvent();
        public UnityEvent<Color, bool> OnRenderBgColorChange {get;} = new UnityEvent<Color, bool>(); // color, use custom bg color
        // Time control
        public UnityEvent OnPauseEvent {get;} = new UnityEvent();
        public UnityEvent OnPlayEvent {get;} = new UnityEvent();
        public UnityEvent OnFastForwardEvent {get;} = new UnityEvent();
        //
        public UnityEvent OnScreenResolutionChanged {get;} = new UnityEvent();

        private static UIMediator m_instance;
        public static UIMediator Get() {
            if(m_instance == null) {
                m_instance = new UIMediator();
            }
            return m_instance;
        }
    }
}
