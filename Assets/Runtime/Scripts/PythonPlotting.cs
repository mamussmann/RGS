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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RGS.Configurations.Root;
using RGS.Models;
using RGS.UI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Threading;
using System.Globalization;

namespace RGS.Rendering
{

    public class PythonPlotting : MonoBehaviour
    {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        [DllImport ("PythonCallLibraryLinux")] 
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport ("PythonCallLibraryWindows")]
#endif
        public static extern bool startPython(string test);
        [SerializeField] private FpsTracker m_fpsTracker;
        [SerializeField] private UISettings m_uiSettings;
        [SerializeField] private RootSGConfiguration m_rootSGConfiguration;
        private NativeArray<float> m_lengthData, m_nutrientData, m_timeData;
        private List<RootSegment> m_segmentData;
        private List<Tuple<Color,int>> m_rootTypeColorList;
        private List<string> m_nutrientNameList;
        private bool m_rootDataAvailable;
        private bool m_showPlotWindow = true;
        private int m_plotCounter;
        private int m_rootPlotCounter;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Awake() {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_uiMediator.OnRootLengthPlottingChange.AddListener(HandleRootLengthPlottingChange);
            m_uiMediator.OnRootNutrientPlottingChange.AddListener(HandleRootNutrientPlottingChange);
            m_uiMediator.OnRootDensityPlottingChange.AddListener(HandleRootDensityPlottingChange);
            m_uiMediator.OnEventToggleClicked.AddListener(HandleEventToggleClicked);
        }

        private void HandleRootNutrientPlottingChange(NativeArray<float> data, NativeArray<float> timeData, List<string> nutrientNameList)
        {
            m_nutrientData = data;
            m_timeData = timeData;
            m_nutrientNameList = nutrientNameList;
        }

        private void HandleEventToggleClicked(ToggleEventType toggleEventType, bool value)
        {
            if(toggleEventType != ToggleEventType.TOGGLE_SHOW_PLOT_WINDOWS) return;
            m_showPlotWindow = value;
        }

        private void HandleRootDensityPlottingChange(List<RootSegment> data, List<Tuple<Color,int>> rootTypeColorList, Vector2 minMax)
        {
            m_segmentData = data;
            m_rootTypeColorList = rootTypeColorList;
        }
        private void HandleRootLengthPlottingChange(NativeArray<float> data, NativeArray<float> timeData, List<Tuple<Color,int>> rootTypeColorList)
        {
            m_lengthData = data;
            m_timeData = timeData;
            m_rootTypeColorList = rootTypeColorList;
            m_rootDataAvailable = true;
        }
        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType == ButtonEventType.EXPORT_PLOTS) 
            {
                ExportRootPlots();
            }
            if(buttonEventType == ButtonEventType.EXPORT_PERFORMANCE_PLOTS) 
            {
                ExportPerformancePlots();
            }
        }
        private void ExportPerformancePlots()
        {
            string pythonPath = m_uiSettings.SelectedPythonPath;
            string playfpsDataFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootPlayfpsData.csv");
            string ffFpsDataFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootFFfpsData.csv");
            string waterPlayfpsDataFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpWaterPlayfpsData.csv");
            string waterFFFpsDataFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpWaterFFfpsData.csv");
            string fpsModesDataFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpfpsModesData.csv");
            string outputPlayPerformancePlotPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_plotCounter}playModePerformance.pdf");
            string outputFFPerformancePlotPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_plotCounter}fastForwardModePerformance.pdf");
            string outputPerformanceSimModesPlotPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_plotCounter}simModesPerformance.pdf");
            string outputWaterFFPerformancePlotPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_plotCounter}waterFastForwardModePerformance.pdf");
            string outputWaterPerformanceSimModesPlotPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_plotCounter}waterSimModesPerformance.pdf");
            WriteFPSValuesToTempCSVFile(m_fpsTracker.RootPlayfpsValues, SessionInfo.GetSessionFolderPath(), playfpsDataFilePath);
            WriteFPSValuesToTempCSVFile(m_fpsTracker.RootFastForwardfpsValues, SessionInfo.GetSessionFolderPath(), ffFpsDataFilePath);
            WriteFPSValuesToTempCSVFile(m_fpsTracker.WaterPlayfpsValues, SessionInfo.GetSessionFolderPath(), waterPlayfpsDataFilePath);
            WriteFPSValuesToTempCSVFile(m_fpsTracker.WaterFastForwardfpsValues, SessionInfo.GetSessionFolderPath(), waterFFFpsDataFilePath);
            WriteSimModesFPSValuesToTempCSVFile(m_fpsTracker.PausedFPS, m_fpsTracker.PlayFPS, m_fpsTracker.FastForwardFPS,SessionInfo.GetSessionFolderPath(),fpsModesDataFilePath);
            if(!File.Exists(pythonPath))
            {
                m_uiMediator.OnShowWarningPopup.Invoke("Failed to start python process. Make sure that the python executable path is set.");
            } else if(!File.Exists(SessionInfo.GetPlotPerformancePyFilePath()))
            {
                m_uiMediator.OnShowWarningPopup.Invoke("Failed to start python process. Make sure that the python file is in StreamingAssets folder.");
            } else {
                string showPlottingWindowArg = m_showPlotWindow ? "True" : "False";
                string cmdPlotPlayPerformance = $"{pythonPath} {SessionInfo.GetPlotPerformancePyFilePath()} {playfpsDataFilePath} {outputPlayPerformancePlotPath} {showPlottingWindowArg} root play";
                startPython(cmdPlotPlayPerformance);
                string cmdPlotFFPerformance = $"{pythonPath} {SessionInfo.GetPlotPerformancePyFilePath()} {ffFpsDataFilePath} {outputFFPerformancePlotPath} {showPlottingWindowArg} root fast-forward";
                startPython(cmdPlotFFPerformance);
                string cmdPlotWaterPlayPerformance = $"{pythonPath} {SessionInfo.GetPlotPerformancePyFilePath()} {waterPlayfpsDataFilePath} {outputWaterPerformanceSimModesPlotPath} {showPlottingWindowArg} water play";
                startPython(cmdPlotWaterPlayPerformance);
                string cmdPlotWaterFFPerformance = $"{pythonPath} {SessionInfo.GetPlotPerformancePyFilePath()} {waterFFFpsDataFilePath} {outputWaterFFPerformancePlotPath} {showPlottingWindowArg} water fast-forward";
                startPython(cmdPlotWaterFFPerformance);
                string cmdPlotPerformanceSimModes = $"{pythonPath} {SessionInfo.GetPlotPerformanceSimModesPyFilePath()} {fpsModesDataFilePath} {outputPerformanceSimModesPlotPath} {showPlottingWindowArg}";
                startPython(cmdPlotPerformanceSimModes);
                Debug.Log(cmdPlotPlayPerformance);
            }
            m_plotCounter++;
        }

        private void WriteSimModesFPSValuesToTempCSVFile(List<int> pauseFPS, List<int> playFPS, List<int> fastforwardFPS, string folderPath, string fpsDataFilePath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(fpsDataFilePath);

            writer.WriteLine("pause;play;fast forward;");
            string line = $"{pauseFPS.Count};{playFPS.Count};{fastforwardFPS.Count};";
            writer.WriteLine(line);
            int maxCount = Mathf.Max(Mathf.Max(pauseFPS.Count, playFPS.Count), fastforwardFPS.Count);
            for (int i = 0; i < maxCount; i++)
            {
                string fpsLine = "";
                fpsLine = i < pauseFPS.Count ? $"{fpsLine}{pauseFPS[i]};" : $"{fpsLine};";
                fpsLine = i < playFPS.Count ?  $"{fpsLine}{playFPS[i]};" : $"{fpsLine};";
                fpsLine = i < fastforwardFPS.Count ?  $"{fpsLine}{fastforwardFPS[i]};" : $"{fpsLine};";
                writer.WriteLine(fpsLine);
            }
            writer.Flush();
            writer.Close();
        }
        private void WriteFPSValuesToTempCSVFile(Dictionary<int, List<int>> playfpsValues, string folderPath, string fpsDataFilePath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(fpsDataFilePath);
            foreach (var kvp in playfpsValues.OrderByDescending(e => e.Key)) 
            {
                string line = $"{kvp.Key};;{kvp.Value.Count};";
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    line += kvp.Value[i];
                    line = i < kvp.Value.Count - 1 ? line + ";" : line;
                }
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
        }

        private void ExportRootPlots()
        {
            if(!m_rootDataAvailable) {
                m_uiMediator.OnShowWarningPopup.Invoke("No root data available. Make sure that you have selected a plant.");
                return;
            }
            string pythonPath = m_uiSettings.SelectedPythonPath;
            string csvFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootData.csv");
            string csvNutrientFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootNutrientData.csv");
            string rootTypeFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootTypeData.csv");
            string rootNutrientFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootNutrientTypeData.csv");
            string rootSegmentsFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), "tmpRootSegmentsData.csv");
            string outputRootLengthPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_rootPlotCounter}RootLength.pdf");
            string outputRootSegmentsPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_rootPlotCounter}RootSegments.pdf");
            string outputRootNutrientPath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_rootPlotCounter}RootNutrient.pdf");
            WriteRootSegmentsToTempCSVFile(SessionInfo.GetSessionFolderPath(), rootSegmentsFilePath);
            WriteRootTypeInfoToTempCSVFile(SessionInfo.GetSessionFolderPath(), rootTypeFilePath);
            WriteToTempCSVFile(m_lengthData, SessionInfo.GetSessionFolderPath(), csvFilePath, m_rootTypeColorList.Count);
            WriteNutrientInfoToTempCSVFile(SessionInfo.GetSessionFolderPath(), rootNutrientFilePath);
            WriteToTempCSVFile(m_nutrientData, SessionInfo.GetSessionFolderPath(), csvNutrientFilePath, m_nutrientNameList.Count);
            if(!File.Exists(pythonPath))
            {
                m_uiMediator.OnShowWarningPopup.Invoke("Failed to start python process. Make sure that the python executable path is set.");
            } else if(!File.Exists(SessionInfo.GetPlotLengthPyFilePath()) || !File.Exists(SessionInfo.GetPlotHistPyFilePath()))
            {
                m_uiMediator.OnShowWarningPopup.Invoke("Failed to start python process. Make sure that the python file is in StreamingAssets folder.");
            } else {
                string showPlottingWindowArg = m_showPlotWindow ? "True" : "False";
                string cmdPlotRootLength = $"{pythonPath} {SessionInfo.GetPlotLengthPyFilePath()} {csvFilePath} {outputRootLengthPath} {m_rootTypeColorList.Count} {rootTypeFilePath} {showPlottingWindowArg}";
                startPython(cmdPlotRootLength);
                string cmdPlotRootHist = $"{pythonPath} {SessionInfo.GetPlotHistPyFilePath()} {rootSegmentsFilePath} {outputRootSegmentsPath} {m_rootTypeColorList.Count} {rootTypeFilePath} {showPlottingWindowArg} {(m_rootSGConfiguration.SegmentLength * 100.0f).ToString("0.00")}";
                startPython(cmdPlotRootHist);
                string cmdPlotRootNutrient = $"{pythonPath} {SessionInfo.GetPlotNutrientPyFilePath()} {csvNutrientFilePath} {outputRootNutrientPath} {m_nutrientNameList.Count} {rootNutrientFilePath} {showPlottingWindowArg}";
                startPython(cmdPlotRootNutrient);
                Debug.Log(cmdPlotRootHist);
            }
            m_rootPlotCounter++;
        }

        private void WriteRootSegmentsToTempCSVFile(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(filePath);
            //
            int maxLength = 0;
            Dictionary<int, List<float>> segmentsDict = new Dictionary<int, List<float>>();
            foreach (var seg in m_segmentData)
            {
                if(!segmentsDict.ContainsKey(seg.RootType))
                {
                    segmentsDict.Add(seg.RootType, new List<float>());
                }
                segmentsDict[seg.RootType].Add(seg.Center.y);
                if(segmentsDict[seg.RootType].Count > maxLength)
                {
                    maxLength = segmentsDict[seg.RootType].Count;
                }
            }
            //
            string lineSegCount = "";
            for (int i = 0; i < m_rootTypeColorList.Count; i++)
            {
                if(!segmentsDict.ContainsKey(m_rootTypeColorList[i].Item2)) {
                    lineSegCount += 0;
                }else {
                    lineSegCount += segmentsDict[m_rootTypeColorList[i].Item2].Count;
                }
                lineSegCount = i < m_rootTypeColorList.Count - 1 ? lineSegCount + ";" : lineSegCount;
            }
            writer.WriteLine(lineSegCount);
            for (int i = 0; i < maxLength; ++i) {
                string line = "";
                for (int j = 0; j < m_rootTypeColorList.Count; j++)
                {
                    if(segmentsDict.ContainsKey(m_rootTypeColorList[j].Item2) && i < segmentsDict[m_rootTypeColorList[j].Item2].Count) {
                        line += segmentsDict[m_rootTypeColorList[j].Item2][i] * 100; // scale to 1.0 equals 1 cm
                    }
                    line = j < m_rootTypeColorList.Count - 1 ? line + ";" : line;
                }
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
        }
        private void WriteNutrientInfoToTempCSVFile(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(filePath);
            for (int i = 0; i < m_nutrientNameList.Count; ++i) {
                writer.WriteLine(m_nutrientNameList[i]);
            }
            writer.Flush();
            writer.Close();
        }
        private void WriteRootTypeInfoToTempCSVFile(string folderPath, string filePath)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(filePath);
            for (int i = 0; i < m_rootTypeColorList.Count; ++i) {
                int index = m_rootTypeColorList[i].Item2;
                string line = $"#{ColorUtility.ToHtmlStringRGB(m_rootTypeColorList[i].Item1)};{m_rootSGConfiguration.RootSGAgents[index].DisplayName} ({m_rootSGConfiguration.RootSGAgents[index].AgentType})";
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
        }
        private void WriteToTempCSVFile(NativeArray<float> data, string folderPath, string filePath, int stride)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            StreamWriter writer = new StreamWriter(filePath);
            for (int i = 0; i < m_timeData.Length; ++i) {
                string line = m_timeData[i] + ";";
                for (int j = 0; j < stride; j++)
                {
                    line += data[i * stride + j];
                    line = j < stride - 1 ? line + ";" : line;
                }
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
        }

        private void OnDestroy() {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_uiMediator.OnRootLengthPlottingChange.RemoveListener(HandleRootLengthPlottingChange);
            m_uiMediator.OnRootDensityPlottingChange.RemoveListener(HandleRootDensityPlottingChange);
            m_uiMediator.OnEventToggleClicked.RemoveListener(HandleEventToggleClicked);
        }
    }

}