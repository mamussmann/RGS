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
using Unity.Collections;

namespace PCMTool.Tree
{
    /// <summary>
    /// Handles all updates related to synchronizing the data in the leafDataArrays and the compute buffer.
    /// </summary>
    public interface ILeafBufferUpdater
    {
        /// <summary>
        /// Counts the amount of points that are flagged as new (flag = 1).
        /// </summary>
        /// <param name="blockData"> List of all blocks in a tree.</param>
        /// <param name="modifiedBlocks"> Index list of all modified blocks.</param>
        /// <returns> Amount of points that have been added.</returns>
        int CalculateAdditionalPoints(List<BlockTreeData> blockData, NativeList<int> modifiedBlocks);
        /// <summary>
        /// Updates the data of all points that are flagged as 2.
        /// </summary>
        /// <param name="info"> Struct containing all relevant information for updating.</param>
        void UpdateBufferDataUpdate(LeafUpdateBufferInfo info);
        void UpdateAllBufferDataUpdate(LeafUpdateBufferInfo info);
        /// <summary>
        /// Adds all points to the compute buffer that are flagged with 1.
        /// </summary>
        /// <param name="info"> Struct containing all relevant information for updating.</param>
        /// <returns> The new amount of total points.</returns>
        int UpdateBufferAddUpdate(LeafUpdateBufferInfo info);
        /// <summary>
        /// Removes all points from the compute buffer that are flagged with 3.
        /// </summary>
        /// <param name="info"> Struct containing all relevant information for updating.</param>
        /// <returns> The new amount of total points.</returns>
        int UpdateBufferRemoveUpdate(LeafUpdateBufferInfo info);
    }
}
