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
using RGS.Configuration;
using UnityEngine;

namespace RGS.Configurations.Generators
{

    [CreateAssetMenu(fileName = "ClayPotWithSoilConfiguration", menuName = "SGR/Configurations/ClayPotWithSoilConfiguration", order = 1)]
    public class ClayPotWithSoilConfiguration : ScriptableObject
    {
        public float BaseHeight => m_baseHeight;
        [SerializeField] private float m_baseHeight;
        public float BaseRadius => m_baseRadius;
        [SerializeField] private float m_baseRadius;
        public float InnerRadiusFraction => m_innerRadiusFraction;
        [SerializeField] private float m_innerRadiusFraction;
        public int HeightDetail => m_heightDetail;
        [SerializeField] private int m_heightDetail;
        public int SoilWidthDetail => m_soilWidthDetail;
        [SerializeField] private int m_soilWidthDetail;
        public int SoilHeightDetail => m_soilHeightDetail;
        [SerializeField] private int m_soilHeightDetail;
        public int CircleDetail => m_circleDetail;
        [SerializeField] private int m_circleDetail;
        public int Depth => m_depth;
        [SerializeField] private int m_depth;
        public float PointSize => m_pointSize;
        [SerializeField] private float m_pointSize;
        public Color ClayColor => m_clayColor;
        [SerializeField] private Color m_clayColor;
        public Color SoilColor => m_soilColor;
        [SerializeField] private Color m_soilColor;
        public AnimationCurve AnimCurve => m_animCurve;
        [SerializeField] private AnimationCurve m_animCurve;
    }
}
