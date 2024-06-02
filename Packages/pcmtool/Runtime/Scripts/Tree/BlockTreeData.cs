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
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using PCMTool.Tree.Jobs;
using Unity.Jobs;
using System.Runtime.CompilerServices;

namespace PCMTool.Tree
{
    /// <summary>
    /// Struct containing all branch and leaf data related to a block.
    /// </summary>
    public struct BlockTreeData
    {
        /// <summary>
        /// Native array containing the data for each branch.
        /// </summary>
        private NativeArray<BlockTreeDataRow> m_rowData;
        /// <summary>
        /// Native array used to check if branches are occupied. Each byte represents a sequence of eight branches.
        /// </summary>
        private NativeArray<byte> m_occupiedRows;
        /// <summary>
        /// Native array preallocated for leaf copy jobs.
        /// </summary>
        private NativeArray<int> m_copyJobTempData;
        /// <summary>
        /// Reference to the leaf data.
        /// </summary>
        private ILeafDataArray m_leafDataArray;
        /// <summary>
        /// Allocates required memory for a block and sets default values.
        /// </summary>
        public BlockTreeData Initialize()
        {
            m_rowData = new NativeArray<BlockTreeDataRow>(DataConstants.TREE_BLOCK_DATA_SIZE, Allocator.Persistent);
            m_occupiedRows = new NativeArray<byte>(DataConstants.TREE_BLOCK_DATA_SIZE / 8, Allocator.Persistent);
            m_copyJobTempData = new NativeArray<int>(16, Allocator.Persistent);
            m_leafDataArray = new LeafDataArray();
            for (int i = 0; i < m_rowData.Length; i++)
            {
                m_rowData[i] = m_rowData[i].SetDefaultValues();
            }
            return this;
        }
        /// <summary>
        /// Frees memory allocated for the block.
        /// </summary>
        public void Dispose()
        {
            m_rowData.Dispose();
            m_occupiedRows.Dispose();
            m_leafDataArray.Dispose();
            m_copyJobTempData.Dispose();
        }
        /// <summary>
        /// Tries to finds a free sequence of 8 branches within the block.
        /// </summary>
        /// <param name="freeIndex"> Output parameter with the index to the first free sequence of 8 (branch index is freeIndex * 8).</param>
        /// <returns> If the block has a free sequence of 8 branches or not.</returns>
        public bool HasFreeSpot(out int freeIndex)
        {
            for (int i = 0; i < m_occupiedRows.Length; i++)
            {
                if (m_occupiedRows[i] == 0)
                {
                    freeIndex = i;
                    return true;
                }
            }
            freeIndex = -1;
            return false;
        }
        /// <summary>
        /// Adds a new point to an existing leaf or creates a new leaf index.
        /// </summary>
        /// <param name="offset"> Offset to the leaf within the branch.</param>
        public void AddLeaf(int branchIndex, int offset, PointData pointData)
        {
            m_rowData[branchIndex] = m_rowData[branchIndex].AddChild(offset, true);
            int leafIndex = m_rowData[branchIndex].GetLeafIndex(offset);
            if (leafIndex == -1)
            {
                leafIndex = m_leafDataArray.ReserveLeaf();
                m_rowData[branchIndex] = m_rowData[branchIndex].SetLeafIndex(offset, leafIndex);
            }
            m_leafDataArray.AddLeafData(leafIndex, pointData.PosCol, pointData.NormSize, pointData.PointType);
        }
        /// <summary>
        /// Reserves a new branch for the child if no sequence of 8 branches is available -1 is returned.
        /// </summary>
        /// <param name="branchIndex"> Parent branch index.</param>
        /// <param name="offset"> Offset within the branch.</param>
        /// <returns> The index of the new branch.</returns>
        public int AddNewBranch(int branchIndex, int offset)
        {
            int childIndex = m_rowData[branchIndex].ChildIndex;
            int freeIndex = -1;
            if (childIndex == -1)
            {
                if (!HasFreeSpot(out freeIndex)) return freeIndex;
            }
            else
            {
                freeIndex = childIndex / 8;
            }
            OccupyRow(freeIndex, offset);
            return freeIndex * 8 + offset;
        }
        /// <returns> If branch is used.</returns>
        public bool IsOccupied(int branchIndex)
        {
            return (m_occupiedRows[branchIndex / 8] & (0b_0000_0001 << (branchIndex % 8))) != 0;
        }
        /// <summary>
        /// Marks a branch in a sequence of 8 branches as occupied.
        /// </summary>
        /// <param name="index"> Index of the sequence of 8 branches.</param>
        /// <param name="offset"> Local offset in the sequence of 8 branches.</param>
        public void OccupyRow(int index, int offset)
        {
            m_occupiedRows[index] |= (byte)(0b_0000_0001 << offset);
        }
        /// <summary>
        /// Resets the values of the branch.
        /// </summary>
        public void ClearRow(int branchIndex)
        {
            m_rowData[branchIndex] = m_rowData[branchIndex].SetDefaultValues();
            m_occupiedRows[branchIndex / 8] &= (byte)~(0b_0000_0001 << (branchIndex % 8));
        }
        /// <summary>
        /// Sets the far and child index of at the specified branch.
        /// </summary>
        /// <param name="blockIndex"> The block containing the referenced branch.</param>
        /// <param name="blockOffset"> Child branch index in the other block.</param>
        public void AddBlockReference(int branchIndex, int blockIndex, int blockOffset)
        {
            OccupyRow(branchIndex / 8, branchIndex % 8);
            m_rowData[branchIndex] = m_rowData[branchIndex].SetChildIndex(blockOffset, blockIndex);
        }
        public bool HasChild(int branchIndex, int offset)
        {
            return m_rowData[branchIndex].IsChild(offset);
        }
        /// <summary>
        /// A split branch is branch with a child but no leaf.
        /// </summary>
        public bool IsSplitBranch(int branchIndex, int offset)
        {
            return !m_rowData[branchIndex].IsLeaf(offset) && m_rowData[branchIndex].IsChild(offset);
        }
        /// <summary>
        /// Checks if branch refers to another block.
        /// </summary>
        /// <returns> True if the branch references an other block.</returns>
        public bool IsFarChildReference(int branchIndex)
        {
            return m_rowData[branchIndex].IsFar();
        }
        public int GetFarChildBlockReference(int branchIndex)
        {
            return m_rowData[branchIndex].FarIndex;
        }
        public int GetChildBranchIndex(int branchIndex)
        {
            return m_rowData[branchIndex].ChildIndex;
        }
        /// <summary>
        /// Checks if the leaf contains less points then specified by the threshold in DataConstants.
        /// </summary>
        /// <param name="offset"> Offset of the leaf inside the branch.</param>
        /// <returns> If leaf has still space for new points.</returns>
        public bool HasFreeLeafs(int branchIndex, int offset)
        {
            int leafIndex = m_rowData[branchIndex].GetLeafIndex(offset);
            if (leafIndex == -1)
            {
                return true;
            }
            return m_leafDataArray.HasFreeLeafData(leafIndex);
        }
        public bool HasBranchAnyChildren(int branchIndex)
        {
            return m_rowData[branchIndex].ChildMask > 0;
        }
        public bool HasBranchAnyLeafs(int branchIndex)
        {
            return m_rowData[branchIndex].LeafMask > 0;
        }
        public bool HasLeafs(int branchIndex, int offset)
        {
            return m_rowData[branchIndex].IsLeaf(offset);
        }
        public void RemoveChild(int branchIndex, int offset)
        {
            m_rowData[branchIndex] = m_rowData[branchIndex].RemoveChild(offset);
        }
        public void RemoveLeaf(int branchIndex, int offset)
        {
            m_rowData[branchIndex] = m_rowData[branchIndex].RemoveLeaf(offset);
            m_rowData[branchIndex] = m_rowData[branchIndex].SetLeafIndex(offset, -1);
        }
        /// <summary>
        /// Copies data from one branch row to another.
        /// </summary>
        /// <param name="from"> Source branch index.</param>
        /// <param name="to"> Destination branch index.</param>
        public void MoveRow(int from, int to)
        {
            if (from == to) return;
            var fromRowData = m_rowData[from];
            m_rowData[from] = m_rowData[to];
            m_rowData[to] = fromRowData;

            m_occupiedRows[to / 8] |= (byte)(0b_0000_0001 << (to % 8));
            m_occupiedRows[from / 8] &= (byte)~(0b_0000_0001 << (from % 8));
        }
        public void SetChildBit(int branchIndex, int offset)
        {
            m_rowData[branchIndex] = m_rowData[branchIndex].AddChild(offset);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ILeafDataArray GetLeafDataArray()
        {
            return m_leafDataArray;
        }
        /// <summary>
        /// Copies points from one leaf to the eight possible sub leafs based on the bounding box.
        /// </summary>
        /// <param name="to"> Destination leaf data in another block.</param>
        /// <param name="fromLeafIndex"> Parent leaf index.</param>
        /// <param name="subMin"> Bounding box min.</param>
        /// <param name="subMax"> Bounding box max.</param>
        /// <returns> Array of leaf indices.</returns>
        public int[] MoveLeafDataToSubLeaf(ILeafDataArray to, int fromLeafIndex, float3 subMin, float3 subMax)
        {
            to.FillFreeSubLeafsArray(m_copyJobTempData);

            var copyJob = new CopyLeafToSubLeafsJob
            {
                InputLeafIndex = fromLeafIndex,
                InputBodies = m_leafDataArray.ExposeBodyNativeList(),
                Min = subMin,
                Max = subMax,
                FreeLeafIndicesAndHeaders = m_copyJobTempData,
                Bodies = to.ExposeBodyNativeList()
            };
            copyJob.Schedule().Complete();

            for (int i = 0; i < 8; i++)
            {
                if (m_copyJobTempData[i] == -1) continue;
                var header = to.ExposeHeaderSlice(m_copyJobTempData[i]);
                header[0] = new LeafHead() { UsedLength = m_copyJobTempData[8 + i] };
                to.AddModifiedLeafIndex(m_copyJobTempData[i]);
            }
            return m_copyJobTempData.GetSubArray(0, 8).ToArray();
        }
        public int GetLeafIndex(int branchIndex, int offset)
        {
            return m_rowData[branchIndex].GetLeafIndex(offset);
        }
        /// <summary>
        /// Occupies the branch and overrides the leaf indices of the branch.
        /// Additionally leaf and child bits are set if a valid leaf index exists.
        /// </summary>
        public void SetRowLeafIndices(int branchIndex, int[] leafIndices)
        {
            OccupyRow(branchIndex / 8, branchIndex % 8);
            for (int i = 0; i < 8; i++)
            {
                if (leafIndices[i] == -1) continue;
                m_rowData[branchIndex] = m_rowData[branchIndex].SetLeafIndex(i, leafIndices[i]);
                m_rowData[branchIndex] = m_rowData[branchIndex].AddChild(i, true);
            }
        }
        public void DeleteLeaf(int leafIndex)
        {
            m_leafDataArray.DeleteLeaf(leafIndex);
        }
        /// <summary>
        /// Adds a new child branch and copies leaf data to sub leafs if necessary.
        /// </summary>
        /// <param name="offset"> Offset to the child/leaf in the parent branch.</param>
        /// <param name="subMin"> Leaf bounding box min.</param>
        /// <param name="subMax"> Leaf bounding box max.</param>
        public void AddChildBranch(int parentBranchIndex, int branchIndex, int offset, Vector3 subMin, Vector3 subMax)
        {
            if (m_rowData[parentBranchIndex].IsLeaf(offset))
            {
                int leafIndex = m_rowData[parentBranchIndex].GetLeafIndex(offset);
                LocalMoveLeafDataToSubLeaf(leafIndex, new float3(subMin), new float3(subMax), branchIndex);
                RemoveLeaf(parentBranchIndex, offset);
                DeleteLeaf(leafIndex);
            }
            else
            {
                m_rowData[parentBranchIndex] = m_rowData[parentBranchIndex].AddChild(offset);
            }
            m_rowData[parentBranchIndex] = m_rowData[parentBranchIndex].SetChildIndex(branchIndex - offset);
        }
        public void ClearModifiedRowsList()
        {
            m_leafDataArray.ClearModifiedLeafList();
        }
        public NativeArray<BlockTreeDataRow> GetRowData() { return m_rowData; }
        public NativeArray<byte> GetOccupiedRows() { return m_occupiedRows; }
        /// <summary>
        /// Copies points from one leaf to the eight possible sub leafs based on the bounding box.
        /// </summary>
        /// <param name="fromLeafIndex"> Source leaf index.</param>
        /// <param name="subMin"> Bounding box min.</param>
        /// <param name="subMax"> Bounding box max.</param>
        /// <param name="branchIndex"> Source branch index.</param>
        /// <returns> Array of leaf indices.</returns>
        private void LocalMoveLeafDataToSubLeaf(int fromLeafIndex, float3 subMin, float3 subMax, int branchIndex)
        {
            m_leafDataArray.FillFreeSubLeafsArray(m_copyJobTempData);

            var copyJob = new LocalCopyLeafToSubLeafsJob
            {
                InputLeafIndex = fromLeafIndex,
                Min = subMin,
                Max = subMax,
                FreeLeafIndicesAndHeaders = m_copyJobTempData,
                Bodies = m_leafDataArray.ExposeBodyNativeList()
            };
            copyJob.Schedule().Complete();

            for (int i = 0; i < 8; i++)
            {
                if (m_copyJobTempData[i] == -1) continue;
                var header = m_leafDataArray.ExposeHeaderSlice(m_copyJobTempData[i]);
                header[0] = new LeafHead() { UsedLength = m_copyJobTempData[8 + i] };
                m_leafDataArray.AddModifiedLeafIndex(m_copyJobTempData[i]);
                m_rowData[branchIndex] = m_rowData[branchIndex].SetLeafIndex(i, m_copyJobTempData[i]);
                m_rowData[branchIndex] = m_rowData[branchIndex].AddChild(i, true);
            }
        }

    }
    /// <summary>
    /// Struct containing the data for one branch.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BlockTreeDataRow
    {
        /// <summary>
        /// Index to the first branch in a sequence of eight. -1 if no reference is set.
        /// </summary>
        [FieldOffset(0)] public int ChildIndex;
        /// <summary>
        /// Index to another block. -1 if no reference is set.
        /// </summary>
        [FieldOffset(4)] public int FarIndex;
        /// <summary>
        /// Each bit in the byte set to 1 indicates a branch.
        /// </summary>
        [FieldOffset(8)] public byte ChildMask;
        /// <summary>
        /// Each bit in the byte set to 1 indicates a leaf.
        /// </summary>
        [FieldOffset(9)] public byte LeafMask;
        /// <summary>
        /// Index of leaf 0 in the leaf data.
        /// </summary>
        [FieldOffset(10)] private short m_leafIndex0;
        /// <summary>
        /// Index of leaf 1 in the leaf data.
        /// </summary>
        [FieldOffset(12)] private short m_leafIndex1;
        /// <summary>
        /// Index of leaf 2 in the leaf data.
        /// </summary>
        [FieldOffset(14)] private short m_leafIndex2;
        /// <summary>
        /// Index of leaf 3 in the leaf data.
        /// </summary>
        [FieldOffset(16)] private short m_leafIndex3;
        /// <summary>
        /// Index of leaf 4 in the leaf data.
        /// </summary>
        [FieldOffset(18)] private short m_leafIndex4;
        /// <summary>
        /// Index of leaf 5 in the leaf data.
        /// </summary>
        [FieldOffset(20)] private short m_leafIndex5;
        /// <summary>
        /// Index of leaf 6 in the leaf data.
        /// </summary>
        [FieldOffset(22)] private short m_leafIndex6;
        /// <summary>
        /// Index of leaf 7 in the leaf data.
        /// </summary>
        [FieldOffset(24)] private short m_leafIndex7;
        /// <summary>
        /// Checks if a bit in the child mask at index is set to 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsChild(int index) { return (ChildMask & (0b_0000_0001 << index)) != 0; }
        /// <summary>
        /// Checks if a bit in the leaf mask at index is set to 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLeaf(int index) { return (LeafMask & (0b_0000_0001 << index)) != 0; }
        /// <summary>
        /// Initiates data with default values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockTreeDataRow SetDefaultValues()
        {
            ChildIndex = -1;
            FarIndex = -1;
            ChildMask = 0;
            LeafMask = 0;
            m_leafIndex0 = -1;
            m_leafIndex1 = -1;
            m_leafIndex2 = -1;
            m_leafIndex3 = -1;
            m_leafIndex4 = -1;
            m_leafIndex5 = -1;
            m_leafIndex6 = -1;
            m_leafIndex7 = -1;
            return this;
        }
        public BlockTreeDataRow SetLeafIndex(int index, int value)
        {
            switch (index)
            {
                case 0: m_leafIndex0 = (short)value; break;
                case 1: m_leafIndex1 = (short)value; break;
                case 2: m_leafIndex2 = (short)value; break;
                case 3: m_leafIndex3 = (short)value; break;
                case 4: m_leafIndex4 = (short)value; break;
                case 5: m_leafIndex5 = (short)value; break;
                case 6: m_leafIndex6 = (short)value; break;
                case 7: m_leafIndex7 = (short)value; break;
            }
            return this;
        }
        public int GetLeafIndex(int index)
        {
            switch (index)
            {
                case 0: return m_leafIndex0;
                case 1: return m_leafIndex1;
                case 2: return m_leafIndex2;
                case 3: return m_leafIndex3;
                case 4: return m_leafIndex4;
                case 5: return m_leafIndex5;
                case 6: return m_leafIndex6;
                case 7: return m_leafIndex7;
            }
            return m_leafIndex0;
        }

        public BlockTreeDataRow SetChildIndex(int childIndex, int farIndex = -1)
        {
            ChildIndex = childIndex;
            FarIndex = farIndex;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockTreeDataRow AddChild(int index, bool isLeaf = false)
        {
            ChildMask |= (byte)(0b_0000_0001 << index);
            if (!isLeaf) return this;
            LeafMask |= (byte)(0b_0000_0001 << index);
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockTreeDataRow RemoveLeaf(int index)
        {
            LeafMask &= (byte)~(0b_0000_0001 << index);
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockTreeDataRow RemoveChild(int index)
        {
            ChildMask &= (byte)~(0b_0000_0001 << index);
            return this;
        }
        /// <summary>
        /// Checks if far index is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFar() { return FarIndex != -1; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyChildren() { return ChildMask != 0; }
    }
}