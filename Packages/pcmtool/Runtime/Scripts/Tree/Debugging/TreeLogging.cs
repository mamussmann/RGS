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
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PCMTool.Tree.Debugging
{
    /// <summary>
    /// Static class with methods for logging the octree for debugging.
    /// </summary>
    public static class TreeLogging
    {
        /// <summary>
        /// Prints the current octree structure to the console.
        /// </summary>
        /// <param name="treeStructureData"> List of tree data blocks.</param>
        public static void LogTreeData(List<BlockTreeData> treeStructureData)
        {
            string outputLog = "";
            for (int i = 0; i < treeStructureData.Count; i++)
            {
                outputLog += $"Tree Block {i}\n";
                outputLog += LogBlockData(treeStructureData[i].GetRowData(), treeStructureData[i].GetOccupiedRows());
            }
            Debug.Log(outputLog);
        }
        /// <summary>
        /// Creates a string containing branch information about Index, ChildIndex, isFar, ChildMask, LeafMask
        /// and occupy status.
        /// </summary>
        /// <param name="rowData"> Array of branch data in a block.</param>
        /// <param name="occupiedRows"> Array of bytes describing if an branch is used or empty.</param>
        /// <returns>A string containing information the branch rows in a block.</returns>
        public static string LogBlockData(NativeArray<BlockTreeDataRow> rowData, NativeArray<byte> occupiedRows)
        {
            string output = "Index | ChildIndex | isFar | ChildMask | LeafMask \n";
            for (int i = 0; i < rowData.Length; i++)
            {
                if (i % 8 == 0)
                {
                    output += $" occupied rows - [{Convert.ToString(occupiedRows[i / 8], 2)}] \n";
                }
                output += (i % 8 == 0 && i != 0) ? "\n" : "";
                output += $"[{i}]   {rowData[i].ChildIndex},{rowData[i].FarIndex},{Convert.ToString(rowData[i].ChildMask, 2).PadLeft(8, '0')},{Convert.ToString(rowData[i].LeafMask, 2).PadLeft(8, '0')}";

                output += "\n";
            }
            return output;
        }
    }

}