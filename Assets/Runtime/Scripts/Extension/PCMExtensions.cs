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
using PCMTool.Tree.OverlapVolume;
using Unity.Collections;
using UnityEngine;

namespace RGS.Extension
{
    public static class PCMExtensions
    {
        public static bool HitPCWithCullingPlane(this RuntimeLeafModifier modifier, Vector3 start, Vector3 direction, float radius, out Vector3 position, Vector3 cullingPlanePosition, Vector3 cullingDirection, bool useRadiusOffset = true)
        {
            position = Vector3.zero;
            float hitDistance;
            if(modifier.Tree.SphereOverlapCastWithCullingPlane(start, direction, modifier.SphereCastDistance, modifier.SphereDetail, radius, out hitDistance, cullingPlanePosition, cullingDirection))
            {
                if(useRadiusOffset) {
                    hitDistance += radius;
                }
                position = start + (direction * hitDistance);
                return true;
            }
            return false;
        }
        public static bool SphereOverlapCastWithCullingPlane(this PointCloudTree tree, Vector3 origin, Vector3 direction, float distance, int steps, float radius, out float hitDistance, Vector3 cullingPlanePosition, Vector3 cullingDirection)
        {
            steps = Mathf.Max(steps, 2);
            hitDistance = -1.0f;
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < steps; i++)
            {
                float d = distance * (i / ((float)steps - 1));
                if (tree.OverlapTestPoints(new SphereWithCullingPlaneOveralapVolume(origin + (direction * d), radius, output, cullingPlanePosition, cullingDirection)))
                {
                    hitDistance = d;
                    output.Dispose();
                    return true;
                }
            }
            output.Dispose();
            return false;
        }
    }

}
