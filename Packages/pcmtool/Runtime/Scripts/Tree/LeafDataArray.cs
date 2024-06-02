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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace PCMTool.Tree
{
    public class LeafDataArray : ILeafDataArray
    {
        private NativeList<LeafHead> m_leafHeadList;
        private NativeList<LeafBody> m_leafBodyList;
        private NativeList<int> m_modifiedLeafs;
        
        public LeafDataArray()
        {
            int initialLeafSize = DataConstants.TREE_INIT_LEAF_DATA_SIZE;
            m_modifiedLeafs = new NativeList<int>(initialLeafSize, Allocator.Persistent);
            m_leafHeadList = new NativeList<LeafHead>(initialLeafSize, Allocator.Persistent);
            m_leafBodyList = new NativeList<LeafBody>(initialLeafSize * DataConstants.TREE_BLOCK_LEAF_CAPACITY, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_modifiedLeafs.Dispose();
            m_leafHeadList.Dispose();
            m_leafBodyList.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<int> GetModifiedLeafsList()
        {
            return m_modifiedLeafs;
        }

        public void AddLeafData(int leafIndex, float4 posCol, float3 normSize, int pointType)
        {
            int bodyIndex = (int)m_leafHeadList[leafIndex].UsedLength;
            m_leafHeadList[leafIndex] = m_leafHeadList[leafIndex].IncrementLength();
            bodyIndex = leafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + bodyIndex;
            m_leafBodyList[bodyIndex] = new LeafBody()
            {
                PosCol = posCol,
                NormSize = normSize,
                PointType = pointType,
                BufferIndex = -1,
                ModifyFlag = 1
            };
            AddModifiedLeafIndex(leafIndex);
        }

        public int ReserveLeaf()
        {
            int freeLeafIndex = -1;
            for (int i = 0; i < m_leafHeadList.Length; i++)
            {
                if (m_leafHeadList[i].IsFree() && !ModifiedLeafsContains(i))
                {
                    freeLeafIndex = i;
                    break;
                }
            }
            if (freeLeafIndex == -1)
            {
                if (m_leafHeadList.Length == m_leafHeadList.Capacity)
                {
                    freeLeafIndex = m_leafHeadList.Length;
                    m_modifiedLeafs.SetCapacity(freeLeafIndex + 1);
                    m_leafHeadList.Resize(freeLeafIndex + 1, NativeArrayOptions.UninitializedMemory);
                    m_leafBodyList.Resize(freeLeafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + DataConstants.TREE_BLOCK_LEAF_CAPACITY, NativeArrayOptions.UninitializedMemory);
                }
                else
                {
                    freeLeafIndex = m_leafHeadList.Length;
                    m_leafHeadList.Length = freeLeafIndex + 1;
                    m_leafBodyList.Length = freeLeafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + DataConstants.TREE_BLOCK_LEAF_CAPACITY;
                }
            }
            m_leafHeadList[freeLeafIndex] = new LeafHead() { UsedLength = 0 };
            return freeLeafIndex;
        }
        public bool HasFreeLeafData(int leafIndex)
        {
            return m_leafHeadList[leafIndex].UsedLength < DataConstants.TREE_BLOCK_LEAF_CAPACITY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<LeafBody> ExposeBodySlice(int leafIndex)
        {
            return new NativeSlice<LeafBody>(m_leafBodyList, leafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY, DataConstants.TREE_BLOCK_LEAF_CAPACITY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<LeafHead> ExposeHeaderSlice(int leafIndex)
        {
            return new NativeSlice<LeafHead>(m_leafHeadList, leafIndex, 1);
        }

        public void ClearModifiedLeafList()
        {
            m_modifiedLeafs.Clear();
        }

        public void DeleteLeaf(int leafIndex)
        {
            for (int i = 0; i < m_leafHeadList[leafIndex].UsedLength; i++)
            {
                int index = leafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + i;
                m_leafBodyList[index] = m_leafBodyList[index].SetModifyFlag(3);
            }
            AddModifiedLeafIndex(leafIndex);
        }
        public void FillFreeSubLeafsArray(NativeArray<int> array)
        {
            ReserveLeafRange(array.GetSubArray(0, 8));
        }

        public int CountModifiedLeafsWithFlag(byte flag)
        {
            int count = 0;
            for (int i = 0; i < m_modifiedLeafs.Length; i++)
            {
                int leafIndex = m_modifiedLeafs[i];
                for (int j = 0; j < m_leafHeadList[leafIndex].UsedLength; j++)
                {
                    int index = leafIndex * DataConstants.TREE_BLOCK_LEAF_CAPACITY + j;
                    count = m_leafBodyList[index].ModifyFlag == flag ? count + 1 : count;
                }
            }
            return count;
        }
        private bool ModifiedLeafsContains(int leafIndex)
        {
            for (int i = 0; i < m_modifiedLeafs.Length; i++)
            {
                if (m_modifiedLeafs[i] == leafIndex) return true;
            }
            return false;
        }
        public void AddModifiedLeafIndex(int leafIndex)
        {
            for (int i = 0; i < m_modifiedLeafs.Length; i++)
            {
                if (m_modifiedLeafs[i] == leafIndex)
                {
                    return;
                }
            }
            m_modifiedLeafs.AddNoResize(leafIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<LeafBody> ExposeBodyNativeList()
        {
            return m_leafBodyList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<LeafHead> ExposeHeaderNativeList()
        {
            return m_leafHeadList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPointCountAtLeaf(int leafIndex)
        {
            return m_leafHeadList[leafIndex].UsedLength;
        }

        private void ReserveLeafRange(NativeArray<int> freeLeafIndices)
        {
            int reservedLeafsCount = 0;
            for (int i = 0; i < m_leafHeadList.Length; i++)
            {
                if (m_leafHeadList[i].IsFree() && !ModifiedLeafsContains(i))
                {
                    freeLeafIndices[reservedLeafsCount++] = i;
                    if (reservedLeafsCount >= freeLeafIndices.Length) break;
                }
            }
            int leafsToAllocate = freeLeafIndices.Length - reservedLeafsCount;
            if (leafsToAllocate > 0)
            {
                int startIndex = m_leafHeadList.Length;
                if (m_leafHeadList.Length + leafsToAllocate > m_leafHeadList.Capacity)
                {
                    int newLength = m_leafHeadList.Length + leafsToAllocate;
                    m_modifiedLeafs.SetCapacity(newLength);
                    m_leafHeadList.Resize(newLength, NativeArrayOptions.UninitializedMemory);
                    m_leafBodyList.Resize(newLength * DataConstants.TREE_BLOCK_LEAF_CAPACITY, NativeArrayOptions.UninitializedMemory);
                }
                else
                {
                    int newLength = m_leafHeadList.Length + leafsToAllocate;
                    m_leafHeadList.Length = newLength;
                    m_leafBodyList.Length = newLength * DataConstants.TREE_BLOCK_LEAF_CAPACITY;
                }
                for (int i = startIndex; i < startIndex + leafsToAllocate; i++)
                {
                    freeLeafIndices[reservedLeafsCount++] = i;
                }
            }
            for (int i = 0; i < freeLeafIndices.Length; i++)
            {
                m_leafHeadList[freeLeafIndices[i]] = new LeafHead() { UsedLength = 0 };
            }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LeafHead
    {
        public int UsedLength;
        public bool IsFree() { return UsedLength == 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafHead IncrementLength() { UsedLength = UsedLength + 1; return this; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafHead DecrementLength() { UsedLength = UsedLength - 1; return this; }
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct LeafBody
    {
        [FieldOffset(0)] public float4 PosCol; //xyz = position, w = color
        [FieldOffset(16)] public float3 NormSize; //x = normal, y = point Size, z = not used
        [FieldOffset(28)] public int PointType;
        [FieldOffset(32)] public int BufferIndex;
        [FieldOffset(36)] public byte ModifyFlag;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafBody SetModifyFlag(byte value) { ModifyFlag = value; return this; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LeafBody SetBufferIndex(int value) { BufferIndex = value; return this; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LeafBody SetValue(float value)
        {
            NormSize.z = value;
            ModifyFlag = 2;
            return this;
        }
    }
}