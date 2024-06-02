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
using PCMTool.Tree;
using RGS.Configurations;
using RGS.Configurations.Generators;
using RGS.Models;
using Unity.Mathematics;
using UnityEngine;

namespace RGS.Generator
{

    public class SoilEnvironmentGenerator : MonoBehaviour
    {
        [SerializeField] private EnvironmentType m_environmentType;
        [SerializeField] private Sprite m_heightmap;
        [SerializeField] private Sprite m_stoneTopMap;
        [SerializeField] private Sprite m_naclTopMap;
        [SerializeField] private float m_fieldScale;
        [SerializeField] private int m_pointDensity;
        [SerializeField] private int m_pointDepthDensity;
        [SerializeField] private Color m_minSoilColor;
        [SerializeField] private Color m_maxSoilColor;
        [SerializeField] private Color m_stoneColor;
        [SerializeField] private Color m_naclColor;
        [SerializeField] private float m_minHeight;
        [SerializeField] private float m_maxHeight;
        [SerializeField] private float m_yOffset;
        private const string CLAY_POINT_TYPE_NAME = "Obstacle";
        private const string SOIL_POINT_TYPE_NAME = "Soil";
        private const string ROOT_POINT_TYPE_NAME = "Root";
        private const string NACL_POINT_TYPE_NAME = "NaCl";
        private int m_rootPointType;
        private ClayPotModel[] m_clayPotModels;
        public void Awake()
        {
            m_rootPointType = RGSConfiguration.Get().GetIndexOfPointType(ROOT_POINT_TYPE_NAME);
            m_clayPotModels = GameObject.FindObjectsOfType<ClayPotModel>();
        }
        public void CreateEnvironment(RuntimeLeafModifier runtimeLeafModifier)
        {
            switch (m_environmentType)
            {
                case EnvironmentType.FIELD:
                    runtimeLeafModifier.CreateWithGenerator((tree) =>
                    {
                        CreateTerrain(tree);
                    });
                    break;
                case EnvironmentType.CLAYPOT:
                    foreach (var model in m_clayPotModels)
                    {
                        runtimeLeafModifier.CreateWithGenerator((tree) =>
                        {
                            CreateClayPotWithSoil(tree, model.Config, model.transform.position);
                        });
                    }
                    break;
                case EnvironmentType.SHOWCASE:
                    break;
            }
        }
        public void CreateTerrain(PointCloudTree tree)
        {
            int soilPointType = RGSConfiguration.Get().GetIndexOfPointType(SOIL_POINT_TYPE_NAME);
            int phosphorusPointType = RGSConfiguration.Get().GetIndexOfPointType("Phosphorus");
            int ironPointType = RGSConfiguration.Get().GetIndexOfPointType("Iron");
            int clayPointType = RGSConfiguration.Get().GetIndexOfPointType(CLAY_POINT_TYPE_NAME);
            int naclPointType = RGSConfiguration.Get().GetIndexOfPointType(NACL_POINT_TYPE_NAME);
            int size = (int) (m_fieldScale * m_pointDensity);
            float halfFieldScale = m_fieldScale * 0.5f;
            for (int x = 0; x < size; x++)
            {
                float normX = x / (float)size;
                for (int z = 0; z < size; z++)
                {
                    float normZ = z / (float)size;
                    float textureValue = m_heightmap.texture.GetPixelBilinear(normX, normZ).r;
                    float stoneTopValue = m_stoneTopMap.texture.GetPixelBilinear(normX, normZ).r;
                    float naclTopValue = m_naclTopMap.texture.GetPixelBilinear(normX, normZ).r;
                    float height = Mathf.Lerp(m_minHeight, m_maxHeight, textureValue);
                    float3 position = new float3((normX * m_fieldScale) - halfFieldScale, height, (normZ * m_fieldScale) - halfFieldScale);
                    int heightScale = (int) (height * m_pointDepthDensity);
                    for (int y = 0; y < heightScale; y++)
                    {
                        float normY = y / (float)heightScale;
                        float textureXYValue = m_heightmap.texture.GetPixelBilinear(normX, normY).r;
                        float val = textureValue * textureXYValue;
                        Color color = Color.Lerp(m_minSoilColor, m_maxSoilColor, UnityEngine.Random.value) * normY;
                        position.y = normY * height + m_yOffset;
                        tree.AddPoint(new PointData(position + RandomOffset(0.02f), new float3(color.r, color.g, color.b), 0.01f, soilPointType, UnityEngine.Random.value));

                        if(val > 0.15f)
                        {
                            tree.AddPoint(new PointData(position + RandomOffset(), new float3(color.r, color.g, color.b), 0.01f, phosphorusPointType, Mathf.Clamp01(val*2.0f)));
                        }
                        float textureIron = m_heightmap.texture.GetPixelBilinear(normY, normX).r;
                        float ironValue = textureValue * textureIron;
                        if(ironValue > 0.2f)
                        {
                            tree.AddPoint(new PointData(position + RandomOffset(), new float3(color.r, color.g, color.b), 0.01f, ironPointType, Mathf.Clamp01(ironValue*2.0f)));
                        }
                        float stoneLayer = m_stoneTopMap.texture.GetPixelBilinear(normX, normY).r * (1.0f - normY);
                        float stoneValue = stoneTopValue * stoneLayer;
                        if(stoneValue > 0.15f)
                        {
                            //tree.AddPoint(new PointData(position + RandomOffset(), new float3(m_stoneColor.r, m_stoneColor.g, m_stoneColor.b), 0.01f, clayPointType, 1.0f));
                        }
                        float naclValue = naclTopValue * textureXYValue;
                        if(naclValue > 0.25f)
                        {
                            tree.AddPoint(new PointData(position + RandomOffset(), new float3(m_naclColor.r, m_naclColor.g, m_naclColor.b), 0.01f, naclPointType, Mathf.Clamp01(naclValue*2.0f)));
                        }
                    }
                }
            }
        }
        private float3 RandomOffset(float scale = 0.01f)
        {
            return new float3(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f)) * scale;
        }
        public void PlaceRootPoint(PointCloudTree tree, float3 position, float3 color, float radius)
        {
            tree.AddPoint(new PointData(position, color, radius, m_rootPointType));
        }
        public void CreateClayPotWithSoil(PointCloudTree tree, ClayPotWithSoilConfiguration clayPotConfig, Vector3 position)
        {
            float startTime = Time.realtimeSinceStartup;
            Vector3[] positions = new Vector3[clayPotConfig.HeightDetail * clayPotConfig.CircleDetail * clayPotConfig.Depth];
            int c = 0;
            for (int i = 0; i < clayPotConfig.HeightDetail; i++)
            {
                float heightFactor = i / (float)clayPotConfig.HeightDetail;
                Vector3 direction = Vector3.forward * clayPotConfig.AnimCurve.Evaluate(heightFactor) * clayPotConfig.BaseRadius;
                for (int j = 0; j < clayPotConfig.CircleDetail; j++)
                {
                    var pos = Quaternion.Euler(0.0f, (j / (float)clayPotConfig.CircleDetail) * 360.0f, 0.0f) * direction;
                    pos += Vector3.up * heightFactor * clayPotConfig.BaseHeight;
                    for (int k = 0; k < clayPotConfig.Depth; k++)
                    {
                        var xy = new Vector3(pos.x, 0.0f, pos.z) * (1.0f - ((k / (float)clayPotConfig.Depth) * clayPotConfig.InnerRadiusFraction));
                        xy.y = pos.y;
                        positions[c] = xy;
                        c++;
                    }
                }
            }
            //Debug.Log($"Point calculations took: {Time.realtimeSinceStartup - startTime}");

            int clayPointType = RGSConfiguration.Get().GetIndexOfPointType(CLAY_POINT_TYPE_NAME);
            int soilPointType = RGSConfiguration.Get().GetIndexOfPointType(SOIL_POINT_TYPE_NAME);
            int naclPointType = RGSConfiguration.Get().GetIndexOfPointType(NACL_POINT_TYPE_NAME);
            startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < positions.Length; i++)
            {
                tree.AddPoint(new PointData(position + positions[i], clayPotConfig.ClayColor * (1.0f - (UnityEngine.Random.value * 0.1f)), clayPotConfig.PointSize, clayPointType));
            }
            int width = clayPotConfig.SoilWidthDetail;
            float diameter = clayPotConfig.BaseRadius * 2.0f;
            for (int x = 0; x <= width; x++)
            {
                float nx = x/(float)width;

                for (int z = 0; z <= width; z++)
                {
                    float nz = z /(float)width;
                    for (int y = 0; y < clayPotConfig.SoilHeightDetail; y++)
                    {
                        float ny = y/(float)clayPotConfig.SoilHeightDetail;
                        ny *= clayPotConfig.SoilHeightDetail / (float) clayPotConfig.HeightDetail;
                        Color color = clayPotConfig.SoilColor * (1.0f - (UnityEngine.Random.value * 0.1f));
                        
                        float3 point = new float3((nx * diameter) - clayPotConfig.BaseRadius, ny * clayPotConfig.BaseHeight, (nz * diameter) - clayPotConfig.BaseRadius);
                        point += RandomOffset(clayPotConfig.PointSize);
                        float curveValue = clayPotConfig.AnimCurve.Evaluate(ny) * (clayPotConfig.BaseRadius -  clayPotConfig.PointSize);
                        if(new Vector2(point.x, point.z).magnitude < curveValue)
                        {
                            tree.AddPoint(new PointData(position + new Vector3(point.x, point.y, point.z), color, clayPotConfig.PointSize, soilPointType, 1.0f));
                        }

                    }
                }
            }
            //Debug.Log($"Point addition to the tree took: {Time.realtimeSinceStartup - startTime}");
        }
    }

}
