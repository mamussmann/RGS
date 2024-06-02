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
using Unity.Collections;
using Unity.Mathematics;

namespace PCMTool.Tree
{
    /// <summary>
    /// Manages data for all leafs in a block.
    /// </summary>
    public interface ILeafDataArray
    {
        /// <summary>
        /// Frees all reserved memory
        /// </summary>
        void Dispose();
        /// <summary>
        /// Reserves data for a new leaf.
        /// </summary>
        /// <returns> An index to free leaf data. </returns>
        int ReserveLeaf();
        /// <summary>
        /// Flags all leafs at the index as to be removed.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        void DeleteLeaf(int leafIndex);
        /// <summary>
        /// Checks if the amount of points in the leaf is above the threshold 
        /// specified in DataConstants.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        /// <returns> True if leaf has free space for additional points.</returns>
        bool HasFreeLeafData(int leafIndex);
        /// <summary>
        /// Calculates the amount of points that are marked as modified and 
        /// have the same flag as specified by the parameter.
        /// </summary>
        /// <param name="flag"> Flag used to compare to.</param>
        /// <returns> Amount of matching points.</returns>
        int CountModifiedLeafsWithFlag(byte flag);
        /// <summary>
        /// Adds the given point to the leaf specified with the index.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        /// <param name="posCol"> Position and encoded color.</param>
        /// <param name="normSize"> Encoded normal and point size.</param>
        void AddLeafData(int leafIndex, float4 posCol, float3 normSize, int pointType);
        /// <summary>
        /// Counts the points a leaf.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        /// <returns> Amount points at a leaf.</returns>
        int GetPointCountAtLeaf(int leafIndex);
        /// <summary>
        /// Exposes the leaf header containing the point count.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        /// <returns> Native slice pointing to the leaf head.</returns>
        NativeSlice<LeafHead> ExposeHeaderSlice(int leafIndex);
        /// <summary>
        /// Exposes the internal leaf header list.
        /// </summary>
        /// <returns> Native list containing leaf heads.</returns>
        NativeList<LeafHead> ExposeHeaderNativeList();
        /// <summary>
        /// Exposes the leaf body containing the point data.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        /// <returns> Native slice pointing to the leaf body.</returns>
        NativeSlice<LeafBody> ExposeBodySlice(int leafIndex);
        /// <summary>
        /// Exposes the internal leaf body list.
        /// </summary>
        /// <returns> Native list containing leaf bodies.</returns>
        NativeList<LeafBody> ExposeBodyNativeList();
        /// <summary>
        /// Getter for the internal modified leafs list.
        /// </summary>
        /// <returns> List of indices of modified leafs.</returns>
        NativeList<int> GetModifiedLeafsList();
        /// <summary>
        /// Clears the modified leafs list.
        /// </summary>
        void ClearModifiedLeafList();
        /// <summary>
        /// Adds the index to the modified leafs list.
        /// </summary>
        /// <param name="leafIndex"> Index of the leaf data.</param>
        void AddModifiedLeafIndex(int leafIndex);
        /// <summary>
        /// Fills the given array with eight free leaf indices.
        /// </summary>
        /// <param name="array"> Array pointing to index data.</param>
        void FillFreeSubLeafsArray(NativeArray<int> array);
    }

}
