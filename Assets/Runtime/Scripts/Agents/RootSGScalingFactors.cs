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
using RGS.Models;

namespace RGS.Agents
{

    [StructLayout(LayoutKind.Sequential)]
    public struct RootSGScalingFactors
    {
        public float RootLengthScale;
        public float BranchingDensityScale;
        public float BranchingAngleScale;
        public float BranchingProbabilityScale;
        public float ElongationScale;
        public float GravitropismScale;
        public float GSAScale;
        public RootSGScalingFactors(float rootLengthScale, float branchingDensityScale, float branchingAngleScale, float branchingProbabilityScale, float elongationScale, float gravitropismScale, float gsaScale)
        {
            RootLengthScale = rootLengthScale;
            BranchingDensityScale = branchingDensityScale;
            BranchingAngleScale = branchingAngleScale;
            BranchingProbabilityScale = branchingProbabilityScale;
            ElongationScale = elongationScale;
            GravitropismScale = gravitropismScale;
            GSAScale = gsaScale;
        }
        public RootSGScalingFactors SetScalingFactor(float value, ScalingFunctionType scalingFunctionType)
        {
            switch (scalingFunctionType)
            {
                case ScalingFunctionType.BRANCH_DENSITY: 
                    this.BranchingDensityScale = value;
                    break;
                case ScalingFunctionType.BRANCHING_PROBABILITY: 
                    this.BranchingProbabilityScale = value;
                    break;
                case ScalingFunctionType.ELONGATION_RATE:
                    this.ElongationScale = value;
                    break;
                case ScalingFunctionType.INSERTION_ANGLE: 
                    this.BranchingAngleScale = value;
                    break;
                case ScalingFunctionType.ROOT_LENGTH: 
                    this.RootLengthScale = value;
                    break;
                case ScalingFunctionType.GRAVITROPISM: 
                    this.GravitropismScale = value;
                    break;
                case ScalingFunctionType.GSA_ANGLE:
                    this.GSAScale = value;
                    break;
            }
            return this;
        }

    }
}
