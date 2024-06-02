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
using System.IO;
using UnityEngine;

namespace RGS
{

    public class SessionInfo : MonoBehaviour
    {
        public static bool IsInputEnabled;
        public static string SessionFolderName;
        public static float Unit_Length_Scale = 100.0f;
        public static string Unit_Length = "cm";
        public static string Unit_Water = "wa";
        public static string Unit_Nutrient = "nu";
        public static string Unit_Time = "simDay";
        private void Awake() {
            IsInputEnabled = false;
            DateTime dateCurrent = DateTime.Now;
            SessionFolderName = dateCurrent.ToString("yy-MM-dd-HH-mm");
        }

        public static string GetSessionFolderPath()
        {
#if UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, SessionInfo.SessionFolderName);
#else
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), SessionInfo.SessionFolderName);
#endif
        }
        public static string GetPlotLengthPyFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "plotRootLength.py");
        }
        public static string GetPlotNutrientPyFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "plotRootNutrients.py");
        }
        public static string GetPlotHistPyFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "plotRootHist.py");
        }
        public static string GetPlotPerformancePyFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "plotPerformance.py");
        }
        public static string GetPlotPerformanceSimModesPyFilePath()
        {
            return Path.Combine(Application.streamingAssetsPath, "plotPerformanceSimModes.py");
        }
    }

}
