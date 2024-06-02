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
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace RGS.Models
{
    // root point data used for nutrient absoption
    
    [StructLayout(LayoutKind.Sequential)]
    public struct NutrientRootPointData 
    {
        public float3 Position;
        public float SquareRadius;
        public float MaxWater;
        public float WaterContent;
        public NutrientRootPointData(float3 position, float radius, float waterContent, float rootWaterCapacityPerRadius)
        {
            Position = position;
            SquareRadius = radius * radius;
            WaterContent = waterContent;
            MaxWater = radius * rootWaterCapacityPerRadius;
        }
        public NutrientRootPointData AddWater(float water)
        {
            this.WaterContent = math.min(WaterContent + water, MaxWater);
            return this;
        }
        public NutrientRootPointData RemoveWater(float water)
        {
            this.WaterContent = math.max(WaterContent - water, 0.0f);
            return this;
        }
    }

}
