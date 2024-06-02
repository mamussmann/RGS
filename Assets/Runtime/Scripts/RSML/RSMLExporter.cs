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
using RGS.Models;
using RGS.Simulation;
using RGS.UI;
using UnityEngine;
using RGS.RSML.Model;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Linq;

namespace RGS.RSML
{
    public class RSMLExporter : MonoBehaviour
    {
        private Guid m_currentSelection;
        private int m_exportFileCounter = 0;
        private int m_idCounter;
        private readonly UIMediator m_uiMediator = UIMediator.Get();
        private void Start() {
            m_uiMediator.OnEventButtonClicked.AddListener(HandleEventButtonClicked);
            m_uiMediator.OnSelectionChanged.AddListener(HandleSelectionChanged);
        }

        private void HandleSelectionChanged(Guid identifier)
        {
            m_currentSelection = identifier;
        }
        private PlantSeedModel GetSelectedPlantSeedModel()
        {
            var seedModels = GameObject.FindObjectsOfType<PlantSeedModel>();
            PlantSeedModel model = null;
            foreach (var seed in seedModels)
            {
                if (seed.Identifier.Equals(m_currentSelection))
                {
                    model = seed;
                }
            }
            return model;
        }
        private void HandleEventButtonClicked(ButtonEventType buttonEventType)
        {
            if(buttonEventType != ButtonEventType.SAVE_RSML) return;
            PlantSeedModel model = GetSelectedPlantSeedModel();
            if (model == null) {
                m_uiMediator.OnShowWarningPopup.Invoke("Failed to export RSML: no plant selected!");
                return;
            }
            
            var simArea = GameObject.FindObjectOfType<RootGrowthSimulationArea>();

            string rsmlFilePath = Path.Combine(SessionInfo.GetSessionFolderPath(), $"{m_exportFileCounter++}{model.DisplayName}.rsml");
            Export(model.RootSegments, simArea.RootParentChildRelations, SessionInfo.GetSessionFolderPath(), rsmlFilePath, model.AxiomAgentUniqueIds);
        }

        public void Export(List<RootSegment> rootSegments, List<Tuple<long, long, float>> parentChildRelations, string folderPath, string filePath, List<long> axiomAgentUniqueIds)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            //StreamWriter writer = new StreamWriter(filePath);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
            settings.NewLineChars = Environment.NewLine;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(filePath, settings))
            {
                RSMLRoot root = new RSMLRoot();
                // fill metadata //
                root.Metadata = new Metadata();
                root.Metadata.Version = 1;
                root.Metadata.Unit = SessionInfo.Unit_Length;
                root.Metadata.Resolution = 1;
                root.Metadata.Software = "RGS";
                root.Metadata.LastModified = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss");
                root.Metadata.User = "";
                root.Metadata.FileKey = "";
                root.Metadata.Image = "";
                // ------------- //
                root.Scene = new Scene();
                root.Scene.Plant = new Plant();
                // ------------- //
                root.Scene.Plant.Root = new Root[axiomAgentUniqueIds.Count];
                m_idCounter = 0;
                for (int i = 0; i < axiomAgentUniqueIds.Count; i++)
                {
                    root.Scene.Plant.Root[i] = RecursiveCreateRoot(axiomAgentUniqueIds[i], rootSegments, parentChildRelations);
                }
                XmlSerializer ser = new XmlSerializer(typeof(RSMLRoot));
                ser.Serialize(writer, root);
            }
            
        }
        private Root RecursiveCreateRoot(long uniqueId, List<RootSegment> rootSegments, List<Tuple<long, long, float>> parentChildRelations)
        {
            Root rootInstance = new Root();
            rootInstance.Id = m_idCounter++;
            rootInstance.Geometry = new Geometry();
            rootInstance.Geometry.Polyline = new Polyline();
            var segmentList = rootSegments
                .Where(seg => seg.UniqueAgentId == uniqueId);
            rootInstance.Geometry.Polyline.Points =
            segmentList
                .Select(seg =>
                {
                    return new Point { X = (float)Math.Round(seg.Start.x * 100.0f, 4), Y = (float)Math.Round(seg.Start.y * 100.0f, 4), Z = (float)Math.Round(seg.Start.z * 100.0f, 4) };
                })
                .ToArray();
            if(rootInstance.Geometry.Polyline.Points.Length == 0) return null;
            rootInstance.Properties = null;
            rootInstance.Functions = new Functions();
            rootInstance.Functions.FunctionArray = new Function[]{
                new Function(){Name = "emergence_time", Domain="polyline", Samples =
                    segmentList
                        .Select(seg => {
                            return new Sample{Value = (float)Math.Round(seg.EmergenceTime, 4)};
                        })
                        .ToArray()
                }
            };
            var childIds = parentChildRelations.Where(parentChild => parentChild.Item1 == uniqueId).Select(parentChild => parentChild.Item2).ToList();
            rootInstance.Children = new Root[childIds.Count];
            for (int i = 0; i < childIds.Count; i++)
            {
                if(rootSegments.Where(seg => seg.UniqueAgentId == childIds[i]).Count() >= 2) {
                    rootInstance.Children[i] = RecursiveCreateRoot(childIds[i], rootSegments, parentChildRelations);
                }
            }
            return rootInstance;
        }
        private void OnDestroy() {
            m_uiMediator.OnEventButtonClicked.RemoveListener(HandleEventButtonClicked);
            m_uiMediator.OnSelectionChanged.RemoveListener(HandleSelectionChanged);
        }
    }

}
