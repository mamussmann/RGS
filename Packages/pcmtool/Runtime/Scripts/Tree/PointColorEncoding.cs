
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
using UnityEngine;

namespace PCMTool.Tree
{
    /// <summary>
    /// Static class for color encoding methods.
    /// </summary>
    public static class PointColorEncoding
    {
        /// <summary>
        /// Encodes a vector3 color as float.
        /// </summary>
        /// <param name="color"> color as vector3.</param>
        /// <returns> Color encoded as a single float.</returns>
        public static float EncodeColorFloat(Vector3 color)
        {
            Color c = new Color(
                Mathf.Max(color.x, 0.0f), 
                Mathf.Max(color.y, 0.0f), 
                Mathf.Max(color.z, 0.0f));
            return EncodeColorFloat(c);
        }
        /// <summary>
        /// Encodes a color as float.
        /// </summary>
        /// <param name="color"> color.</param>
        /// <returns> Color encoded as a single float.</returns>
        public static float EncodeColorFloat(Color color)
        {
            const float kMaxBrightness = 16;

            var y = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
            y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

            var rgb = new Vector3(color.r, color.g, color.b);
            rgb *= 255 * 255 / (y * kMaxBrightness);
            uint output = ((uint)rgb.x      ) |
                   ((uint)rgb.y <<  8) |
                   ((uint)rgb.z << 16) |
                   ((uint)y     << 24);
            return BitConverter.ToSingle(BitConverter.GetBytes(output));
        }
        /// <summary>
        /// Decodes a color that has been encoded as single float.
        /// </summary>
        /// <param name="value"> encoded color.</param>
        /// <returns> Color.</returns>
        public static Color DecodeColor(float value)
        {
            int data = BitConverter.ToInt32(BitConverter.GetBytes(value));
            const float kMaxBrightness = 16;
            float r = (data      ) & 0xff;
            float g = (data >>  8) & 0xff;
            float b = (data >> 16) & 0xff;
            float a = (data >> 24) & 0xff;
            return new Color(r, g, b) * a * kMaxBrightness / (255 * 255);
        }
    }
   
}