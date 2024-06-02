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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace PCMTool.Tree
{
    /// <summary>
    /// Struct used for wrapping relevant data to the compute buffer methods.
    /// </summary>
    public struct LeafUpdateBufferInfo 
    {
        /// <summary>
        /// List of all blocks in the current tree.
        /// </summary>
        public List<BlockTreeData> BlockData; 
        /// <summary>
        /// Native list of blocks that are flag as modified.
        /// </summary>
        public NativeList<int> ModifiedBlocks;
        /// <summary>
        /// Exposed part of the compute buffer.
        /// </summary>
        public NativeArray<PointData> ComputeBuffer;
        /// <summary>
        /// Native array containing a pair of block index and index of the point in leaf data for each point the the compute buffer.
        /// </summary>
        public NativeArray<int> RowIndexBuffer;
        /// <summary>
        /// Current amount of points within a compute buffer.
        /// </summary>
        public int UsedBufferSize;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafUpdateBufferInfo(List<BlockTreeData> blockData, NativeList<int> modifiedBlocks, NativeArray<PointData> computeBuffer, NativeArray<int> rowIndexBuffer, int usedBufferSize)
        {
            BlockData = blockData;
            ModifiedBlocks = modifiedBlocks;
            ComputeBuffer = computeBuffer;
            RowIndexBuffer = rowIndexBuffer;
            UsedBufferSize = usedBufferSize;
        }
    }
}