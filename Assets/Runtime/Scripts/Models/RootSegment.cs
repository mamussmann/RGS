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
using RGS.Rendering;
using Unity.Mathematics;

namespace RGS.Models
{
    public struct RootSegment
    {
        public float3 Start;
        public float3 End;
        public float3 Center;
        public float Length;
        public int RootType;
        public long UniqueAgentId;
        public float EmergenceTime;
        public float Radius;
        public float EncodedColor;
        public RootSegment(float3 start, float3 end, int rootType, long uniqueAgentId, float emergenceTime, float radius, float encodedColor)
        {
            Start = start;
            End = end;
            Center = ((end - start) * 0.5f) + start;
            Length = math.distance(start, end);
            RootType = rootType;
            UniqueAgentId = uniqueAgentId;
            EmergenceTime = emergenceTime;
            Radius = radius;
            EncodedColor = encodedColor;
        }

        public ShaderRootSegmentData GetShaderAsData(float3 origin)
        {
            return new ShaderRootSegmentData()
                {
                    Start = new float4(Start + origin, EmergenceTime),
                    End = new float4(End + origin, EmergenceTime),
                    RadiusLengthColor = new float3(Radius, Length, EncodedColor),
                    NormalizedDirection = math.normalize((End + origin) - (Start + origin))
                };
        }
    }

}