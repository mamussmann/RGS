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
using MathNet.Numerics.Distributions;
using Unity.Collections;

namespace RGS
{
    public static class RGSMath
    {
        public static NativeParallelHashMap<FloatStdDevKey, float> DistributionValuesCache;
        /// <summary>
        /// Calculates the insertion direction of an emerging root
        /// </summary>
        /// <param name="parentDirection"> Direction of the parent root.</param>
        /// <param name="insertionAngle"> Insertion angle.</param>
        /// <param name="radialAngle"> Radial angle around parent direction.</param>
        /// <returns> Direction of emerging root branch.</returns>
        public static Vector3 CalculateRootInsertionDirection(Vector3 parentDirection, float insertionAngle, float radialAngle)
        {
            var other = Mathf.Abs(Vector3.Dot(Vector3.up, parentDirection)) < 1.0f ? Vector3.up : Vector3.right;
            Vector3 perpendicularVector = Vector3.Cross(parentDirection, other).normalized;
            var result = Quaternion.AngleAxis(insertionAngle, perpendicularVector) * parentDirection;
            result = Quaternion.AngleAxis(radialAngle, parentDirection) * result;
            return result;
        }
        /// <summary>
        /// Calculate normal distributed random value using Box–Muller transform 
        /// </summary>
        public static float GetRandomNormalDistributedValue(float mean, float stdDev, int seed, FloatStdDevKey key)
        {
            if(!DistributionValuesCache.IsCreated) {
                DistributionValuesCache = new NativeParallelHashMap<FloatStdDevKey, float>(100000, Allocator.Persistent);
            }
            if(DistributionValuesCache.ContainsKey(key)){
                return DistributionValuesCache[key];
            }
            float sample = (float) Normal.WithMeanStdDev(mean, stdDev, new System.Random(seed)).Sample();
            DistributionValuesCache.Add(key, sample);
            return sample;
        }

        public static void Dispose()
        {
            if(DistributionValuesCache.IsCreated) {
                DistributionValuesCache.Dispose();
            }
        }
    }

}