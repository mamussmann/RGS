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
using UnityEngine;
using PCMTool.Tree;
using System;
using System.IO;
using PCMTool.Tree.Tracking;
using System.Collections.Generic;
using Unity.Collections;
using PCMTool.Tree.OverlapVolume;
using Unity.Mathematics;
using PCMTool.Tree.QueryJobScheduler;
using PCMTool.Tree.Query;
using UnityEngine.UI;
using PCMTool.Tree.Files;

namespace PCMTool.Benchmark
{
    [RequireComponent(typeof(LeafRenderer))]
    public class Benchmark : MonoBehaviour
    {
        [SerializeField] private int m_sampleCount = 100;
        [SerializeField] private int m_pointCount = 10000;
        [SerializeField] private float m_radius = 0.5f;
        [SerializeField] private InputField m_sampleCountField;
        [SerializeField] private InputField m_pointCountField;
        [SerializeField] private InputField m_radiusField;
        [SerializeField] private InputField m_relativePathFiled;
        [SerializeField] private Text m_textResult;
        private LeafRenderer m_renderer;
        private Report m_report;
        private void Awake() {
            m_renderer = GetComponent<LeafRenderer>();
            m_sampleCountField.text = $"{m_sampleCount}";
            m_pointCountField.text = $"{m_pointCount}";
            m_radiusField.text = $"{m_radius}";
        }
        public void StartMeasurement()
        {
            m_sampleCount = int.Parse(m_sampleCountField.text);
            m_pointCount = int.Parse(m_pointCountField.text);
            m_radius = float.Parse(m_radiusField.text);
            m_report = new Report();
            m_report.OperatingSystem = SystemInfo.operatingSystem;
            m_report.Graphics = SystemInfo.graphicsDeviceType.ToString();
            m_report.ConstantsConfiguration = new ConstantsConfiguration(
                DataConstants.TREE_BLOCK_DATA_SIZE, DataConstants.TREE_BLOCK_LEAF_CAPACITY, 
                DataConstants.COMPUTE_BUFFER_SIZE, DataConstants.INITIAL_SIZE, 
                DataConstants.TREE_INIT_LEAF_DATA_SIZE
            );
            m_report.SampleCount = m_sampleCount;
            MeasureAddSphere(m_pointCount, m_radius);
            MeasureRemoveSphere(m_pointCount, m_radius);
            MeasureAddCube(m_pointCount, m_radius *2.0f);
            MeasureRemoveCube(m_pointCount, m_radius * 2.0f);
            MeasureColorSphere(m_pointCount, m_radius);
            if(m_relativePathFiled.text != "")
            {
                MeasureLoadPly(m_relativePathFiled.text);
            }
            string jsonString = JsonUtility.ToJson(m_report, true);
            m_textResult.text = jsonString;
#if UNITY_EDITOR
            Debug.Log(jsonString);
#else
            var stream = File.Create($"{Application.dataPath}/report-{DateTime.Now.ToString("dd-mm-yyyy-H-mm")}.json");
            var writer = new StreamWriter(stream);
            writer.Write(jsonString);
            writer.Dispose();
            stream.Close();
#endif
        }
        private void MeasureColorSphere(int pointCount, float radius)
        {
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value);
                points[i] = point;
            }
            MeasureAction(m_sampleCount, "Color Sphere", pointCount, (tree, updater) => {
                for (int i = 0; i < points.Length; i++)
                {
                    tree.AddPoint(new PointData(points[i], points[i], 0));
                }
                m_renderer.OnPointsAdded.Invoke();},
                (tree, updater) => {
                    float encodedColor = PointColorEncoding.EncodeColorFloat(Color.green);
                    tree.QueryOverlapPoints(new SphereQueryOveralapVolume(Vector3.zero, radius));
                    ColorJobScheduler job = new ColorJobScheduler(){
                        Tree = tree,
                        TargetEncodedColor = encodedColor,
                        OriginRadius = new float4(Vector3.zero, radius)
                    };
                    job.ScheduleJobs();
                    m_renderer.OnPointsUpdated.Invoke();
                });
        }
        private void MeasureRemoveSphere(int pointCount, float radius)
        {
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value);
                points[i] = point;
            }
            MeasureAction(m_sampleCount, "Remove Sphere", pointCount, (tree, updater) => {
                for (int i = 0; i < points.Length; i++)
                {
                    tree.AddPoint(new PointData(points[i], points[i], 0));
                }
                m_renderer.OnPointsAdded.Invoke();},
                (tree, updater) => {
                    NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    var modifyList = tree.RemovePoints(new SphereOveralapVolume(Vector3.zero, radius, output));
                    output.Dispose();
                    tree.ClearEmptyBranches(modifyList);
                    m_renderer.OnPointsRemoved.Invoke();
                    tree.ClearEmptyBlocks();
                });
        }
        private void MeasureRemoveCube(int pointCount, float scale)
        {   
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = new Vector3(-0.5f, -0.5f, -0.5f) + (Vector3.right * UnityEngine.Random.value) + (Vector3.up * UnityEngine.Random.value)+ (Vector3.forward * UnityEngine.Random.value);
                points[i] = point * scale;
            }
            MeasureAction(m_sampleCount, "Remove Cube", pointCount, (tree, updater) => {
                for (int i = 0; i < points.Length; i++)
                {
                    tree.AddPoint(new PointData(points[i], points[i], 0));
                }
                m_renderer.OnPointsAdded.Invoke();
            }, (tree, updater) => {
                NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var modifyList = tree.RemovePoints(new AABBOverlapVolume(new float3(-scale/2.0f), new float3(scale/2.0f), output));
                output.Dispose();
                tree.ClearEmptyBranches(modifyList);
                m_renderer.OnPointsRemoved.Invoke();
                tree.ClearEmptyBlocks();
            });
        }
        private void MeasureAddCube(int pointCount, float scale)
        {   
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 point = new Vector3(-0.5f, -0.5f, -0.5f) + (Vector3.right * UnityEngine.Random.value) + (Vector3.up * UnityEngine.Random.value)+ (Vector3.forward * UnityEngine.Random.value);
                points[i] = point * scale;
            }
            MeasureAction(m_sampleCount, "Add Cube", pointCount, (a,b) => {}, (tree, updater) => {
                for (int i = 0; i < points.Length; i++)
                {
                    tree.AddPoint(new PointData(points[i], points[i], 0));
                }
                m_renderer.OnPointsAdded.Invoke();
            });
        }
        private void MeasureLoadPly(string path)
        {   
            MeasureAction(m_sampleCount, $"Load ply ({path})", 0, (a,b) => {}, (tree, updater) => {
                PlyImporterExporter.Import($"{Application.dataPath}/{path}", tree, 1.0f, 0, Vector3.zero);
                m_renderer.OnPointsAdded.Invoke();
            });
        }
        private void MeasureAddSphere(int pointCount, float radius)
        {   
            Vector3[] points = new Vector3[pointCount];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 randomDir = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f).normalized;
                Vector3 point = (randomDir * radius * UnityEngine.Random.value);
                points[i] = point;
            }
            MeasureAction(m_sampleCount, "Add Sphere", pointCount, (a,b) => {}, (tree, updater) => {
                for (int i = 0; i < points.Length; i++)
                {
                    tree.AddPoint(new PointData(points[i], points[i], 0));
                }
                m_renderer.OnPointsAdded.Invoke();
            });
        }
        private void MeasureAction(int measureCount, string name, int pointCount, Action<PointCloudTree, ILeafBufferUpdater> prepareAction, Action<PointCloudTree, ILeafBufferUpdater> treeAction)
        {
            AverageValueTracker avgTimeTracker = new AverageValueTracker(measureCount);
            for (int i = 0; i < measureCount; i++)
            {
                PerformActionInNewTree((tree, updater) => {
                    prepareAction(tree, updater);
                    float startTime = Time.realtimeSinceStartup;
                    treeAction(tree, updater);
                    avgTimeTracker.AddTime(Time.realtimeSinceStartup - startTime);
                });
            }
            m_report.Measurements.Add(new Measurement(name, avgTimeTracker.GetAverageTime(), avgTimeTracker.GetMedian(), pointCount));
        }

        private void PerformActionInNewTree(Action<PointCloudTree, ILeafBufferUpdater> treeAction)
        {
            
            PointCloudTree tree = new PointCloudTree();
            m_renderer.PointCloudTree = tree;
            m_renderer.LeafBufferUpdater = new LeafBufferUpdater();
            treeAction(tree, m_renderer.LeafBufferUpdater);
            tree.Dispose();
            m_renderer.Clear();
        }
        [Serializable]
        internal class Measurement
        {
            public string Action;
            public float AverageTime;
            public float MedianTime;
            public int PointCount;
            public Measurement(string action, float averageTime, float medianTime, int pointCount)
            {
                Action = action;
                AverageTime = averageTime;
                MedianTime = medianTime;
                PointCount = pointCount;
            }
        }
        [Serializable]
        internal class Report
        {
            public string OperatingSystem;
            public string Graphics;
            public ConstantsConfiguration ConstantsConfiguration;
            public int SampleCount;
            public List<Measurement> Measurements = new List<Measurement>();
        }
        [Serializable]
        internal class ConstantsConfiguration
        {
            public int BranchesPerBlock;
            public int PointsPerLeaf;
            public int ComputeBufferSize;
            public float InitialBoundsSize;
            public int InitialLeafCapacity;
            public ConstantsConfiguration(int branchesPerBlock, int pointsPerLeaf, int computeBufferSize, float initialBoundsSize, int initialLeafCapacity)
            {
                BranchesPerBlock = branchesPerBlock;
                PointsPerLeaf = pointsPerLeaf;
                ComputeBufferSize = computeBufferSize;
                InitialBoundsSize = initialBoundsSize;
                InitialLeafCapacity = initialLeafCapacity;
            }
        }
    }
}