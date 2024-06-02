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
using PCMTool.Tree.OverlapVolume;
using PCMTool.Tree.Query;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace PCMTool.Tree
{
    /// <summary>
    /// Class that that functions as the root of the tree that handles most tree operations.
    /// </summary>
    public class PointCloudTree : IPointCloudTree
    {
        public NativeList<int> ModifiedBlocks => m_modifiedBlocks;
        public float3 RootMin => m_min;
        public float3 RootMax => m_max;
        public int RootBlockIndex => m_headBlockIndex;
        public int RootBranchIndex => m_headIndex;
        private float3 m_min;
        private float3 m_max;
        private float m_size;
        private int m_headIndex;
        private int m_headBlockIndex;
        private List<BlockTreeData> m_treeStructureData = new List<BlockTreeData>();
        private NativeList<int> m_modifiedBlocks;
        private NativeList<int> m_reservedEmptyBlocks;
        private bool m_isTreeInitialized = false;
        public PointCloudTree()
        {
            m_modifiedBlocks = new NativeList<int>(DataConstants.TREE_MODIFIED_BLOCKS_INITIAL_SIZE, Allocator.Persistent);
            m_reservedEmptyBlocks = new NativeList<int>(DataConstants.TREE_RESERVED_EMPTY_BLOCKS, Allocator.Persistent);
            int startBlockIndex = CreateNewBlock();
            m_headBlockIndex = startBlockIndex;
        }
        public void Dispose()
        {
            for (int i = 0; i < m_treeStructureData.Count; i++)
            {
                m_treeStructureData[i].Dispose();
            }
            m_modifiedBlocks.Dispose();
            m_reservedEmptyBlocks.Dispose();
        }
        /// <summary>
        /// Removes all empty blocks that are in the list after the current root block.
        /// Empty block in front of the root block need to remain to maintain validity of the branch indices.
        /// </summary>
        public void ClearEmptyBlocks()
        {
            NativeList<byte> blockUsedFlags = new NativeList<byte>(m_treeStructureData.Count, Allocator.Temp);
            blockUsedFlags.Length = m_treeStructureData.Count;
            for (int i = 0; i < blockUsedFlags.Length; i++)
            {
                blockUsedFlags[i] = 0;
            }
            blockUsedFlags[m_headBlockIndex] = 1;
            foreach (BlockTreeData block in m_treeStructureData)
            {
                for (int i = 0; i < DataConstants.TREE_BLOCK_DATA_SIZE; i++)
                {
                    int farChildBlockIndex = block.GetFarChildBlockReference(i);
                    if (farChildBlockIndex != -1)
                    {
                        blockUsedFlags[farChildBlockIndex] = 1;
                    }
                }
            }
            int lastRemove = blockUsedFlags.Length;
            for (int i = blockUsedFlags.Length - 1; i >= 0; i--)
            {
                int indexValue = blockUsedFlags[i];
                if (indexValue == 1) continue;
                if (i + 1 == lastRemove)
                {
                    m_treeStructureData[i].Dispose();
                    m_treeStructureData.RemoveAt(i);
                    for (int j = 0; j < m_reservedEmptyBlocks.Length; j++)
                    {
                        if (m_reservedEmptyBlocks[j] == i)
                        {
                            m_reservedEmptyBlocks.RemoveAtSwapBack(j);
                            break;
                        }
                    }
                    lastRemove--;
                }
                else if (!m_reservedEmptyBlocks.Contains(i))
                {
                    m_reservedEmptyBlocks.Add(i);
                }
            }
            blockUsedFlags.Dispose();
        }
        /// <summary>
        /// Method for adding the first point in the tree. The first point determines the initial Bounding box.
        /// </summary>
        /// <param name="pointData"> Point data of the first point.</param>
        private void AddInitialPoint(PointData pointData)
        {
            m_isTreeInitialized = true;
            m_headIndex = m_treeStructureData[m_headBlockIndex].AddNewBranch(0, 0);
            m_treeStructureData[m_headBlockIndex].AddLeaf(m_headIndex, 0, pointData);
            m_size = DataConstants.INITIAL_SIZE;
            m_min = pointData.Position - (0.25f * new float3(m_size));
            m_max = pointData.Position + (0.75f * new float3(m_size));
            AddModifiedBlock(m_headBlockIndex);
        }
        private void AddPointInSubBounds(int blockIndex, int branchIndex, int subIndex, PointData pointData, float3 subMin, float3 subMax)
        {
            // check if branch is split
            if (m_treeStructureData[blockIndex].IsSplitBranch(branchIndex, subIndex))
            {
                var childBranchIndex = m_treeStructureData[blockIndex].GetChildBranchIndex(branchIndex) + subIndex;
                if (m_treeStructureData[blockIndex].IsFarChildReference(branchIndex))
                {
                    blockIndex = m_treeStructureData[blockIndex].GetFarChildBlockReference(branchIndex);
                }
                IterateSubBounds(blockIndex, childBranchIndex, pointData, subMin, subMax);
                return;
            }
            if (m_treeStructureData[blockIndex].HasFreeLeafs(branchIndex, subIndex))
            {
                m_treeStructureData[blockIndex].AddLeaf(branchIndex, subIndex, pointData);
                AddModifiedBlock(blockIndex);
            }
            else
            {
                int nextBranchIndex = m_treeStructureData[blockIndex].AddNewBranch(branchIndex, subIndex);
                if (nextBranchIndex == -1 || m_treeStructureData[blockIndex].IsFarChildReference(branchIndex))
                {
                    int newBlockIndex;
                    int childIndex;
                    if (m_treeStructureData[blockIndex].IsFarChildReference(branchIndex))
                    {
                        newBlockIndex = m_treeStructureData[blockIndex].GetFarChildBlockReference(branchIndex);
                        childIndex = m_treeStructureData[blockIndex].GetChildBranchIndex(branchIndex);
                    }
                    else
                    {
                        nextBranchIndex = subIndex;
                        childIndex = 0;
                        newBlockIndex = CreateNewBlock();
                        m_treeStructureData[blockIndex].AddBlockReference(branchIndex, newBlockIndex, childIndex);
                    }

                    int leafIndex = m_treeStructureData[blockIndex].GetLeafIndex(branchIndex, subIndex);
                    int[] result = m_treeStructureData[blockIndex].MoveLeafDataToSubLeaf(m_treeStructureData[newBlockIndex].GetLeafDataArray(), leafIndex, subMin, subMax);
                    m_treeStructureData[newBlockIndex].SetRowLeafIndices(childIndex + (nextBranchIndex % 8), result);
                    m_treeStructureData[blockIndex].DeleteLeaf(leafIndex);
                    m_treeStructureData[blockIndex].RemoveLeaf(branchIndex, subIndex);
                    AddModifiedBlock(newBlockIndex);
                    IterateSubBounds(newBlockIndex, nextBranchIndex, pointData, subMin, subMax);
                }
                else
                {
                    m_treeStructureData[blockIndex].AddChildBranch(branchIndex, nextBranchIndex, subIndex, subMin, subMax);
                    AddModifiedBlock(blockIndex);
                    IterateSubBounds(blockIndex, nextBranchIndex, pointData, subMin, subMax);
                }
            }
        }

        /// <summary>
        /// Flags all points as removed and removes the leafs that overlap the given volume.
        /// </summary>
        /// <param name="aABBOverlapVolume"> Overlap volume used to perform overlap testing with bounds as well as points.</param>
        /// <returns> List of native arrays sorted by tree depth.</returns>
        public List<NativeList<int>> RemovePoints(IOverlapVolume aABBOverlapVolume)
        {
            List<NativeList<int>> modifyList = new List<NativeList<int>>();
            if (aABBOverlapVolume.TestAABBOverlap(m_min, m_max))
            {
                IterateRemoveInSubBounds(m_headBlockIndex, m_headIndex, aABBOverlapVolume, m_min, m_max, 0, modifyList);
            }
            return modifyList;
        }
        /// <summary>
        /// Performs a sphere cast based on the given parameters.
        /// </summary>
        /// <returns> True if any points have been hit.</returns>
        public bool SphereOverlapCast(Vector3 origin, Vector3 direction, float distance, int steps, float radius, out float hitDistance)
        {
            steps = Mathf.Max(steps, 2);
            hitDistance = -1.0f;
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < steps; i++)
            {
                float d = distance * (i / ((float)steps - 1));
                if (OverlapTestPoints(new SphereOveralapVolume(origin + (direction * d), radius, output)))
                {
                    hitDistance = d;
                    output.Dispose();
                    return true;
                }
            }
            output.Dispose();
            return false;
        }
        /// <summary>
        /// Tests if the given overlap volume intersects any points.
        /// </summary>
        /// <returns> True if any points intersect with the volume.</returns>
        public bool OverlapTestPoints(IOverlapVolume overlapVolume)
        {
            OverlapHit overlapHit = new OverlapHit();
            if (overlapVolume.TestAABBOverlap(m_min, m_max))
            {
                IterateOverlapTestSubBounds(m_headBlockIndex, m_headIndex, overlapVolume, m_min, m_max, overlapHit);
            }
            return overlapHit.IsOverlap;
        }

        private void IterateOverlapTestSubBounds(int blockIndex, int branchIndex, IOverlapVolume overlapVolume, float3 min, float3 max, OverlapHit overlapHit)
        {
            float3 half = (max - min) * 0.5f;
            float3 diff = half;
            for (int i = 0; i < 8; i++)
            {
                diff = half * DataConstants.F_MASK[i];
                float3 subMin = min + diff;
                float3 subMax = min + diff + half;
                if (overlapVolume.TestAABBOverlap(subMin, subMax))
                {
                    OverlapTestPointsInSubBounds(blockIndex, branchIndex, i, overlapVolume, subMin, subMax, overlapHit);
                }
            }
        }

        private void OverlapTestPointsInSubBounds(int blockIndex, int branchIndex, int subIndex, IOverlapVolume overlapVolume, float3 subMin, float3 subMax, OverlapHit overlapHit)
        {
            BlockTreeData localBlock = m_treeStructureData[blockIndex];
            if (localBlock.IsSplitBranch(branchIndex, subIndex))
            {
                int childBranchIndex = localBlock.GetChildBranchIndex(branchIndex) + subIndex;
                if (localBlock.IsFarChildReference(branchIndex))
                {
                    blockIndex = localBlock.GetFarChildBlockReference(branchIndex);
                }
                IterateOverlapTestSubBounds(blockIndex, childBranchIndex, overlapVolume, subMin, subMax, overlapHit);
            }
            else if (localBlock.HasLeafs(branchIndex, subIndex))
            {
                int leafIndex = localBlock.GetLeafIndex(branchIndex, subIndex);
                int leafCount = localBlock.GetLeafDataArray().GetPointCountAtLeaf(leafIndex);
                NativeSlice<LeafBody> leafBodies = localBlock.GetLeafDataArray().ExposeBodySlice(leafIndex);
                if (overlapVolume.ExecuteOverlapTestJob(leafCount, leafBodies))
                {
                    overlapHit.IsOverlap = true;
                }
            }
        }
        /// <summary>
        /// Performs a point query operation with the given query volume.
        /// </summary>
        public void QueryOverlapPoints(IQueryOverlapVolume overlapVolume)
        {
            if (overlapVolume.TestAABBOverlap(m_min, m_max))
            {
                IterateQueryOverlapPointsSubBounds(m_headBlockIndex, m_headIndex, overlapVolume, m_min, m_max, 0);
            }
        }

        private void IterateQueryOverlapPointsSubBounds(int blockIndex, int branchIndex, IQueryOverlapVolume overlapVolume, float3 min, float3 max, int depth)
        {
            float3 half = (max - min) * 0.5f;
            float3 diff;
            float3 subMin; 
            float3 subMax;
            for (int i = 0; i < 8; i++)
            {
                diff = half * DataConstants.F_MASK[i];
                subMin = min + diff;
                subMax = min + diff + half;
                if (overlapVolume.TestAABBOverlap(subMin, subMax))
                {
                    QueryOverlapPointsInSubBounds(blockIndex, branchIndex, i, overlapVolume, subMin, subMax, depth);
                }
            }
        }

        private void QueryOverlapPointsInSubBounds(int blockIndex, int branchIndex, int subIndex, IQueryOverlapVolume overlapVolume, float3 subMin, float3 subMax, int depth)
        {
            BlockTreeData localBlock = m_treeStructureData[blockIndex];
            if (localBlock.IsSplitBranch(branchIndex, subIndex))
            {
                var childBranchIndex = localBlock.GetChildBranchIndex(branchIndex) + subIndex;
                if (localBlock.IsFarChildReference(branchIndex))
                {
                    blockIndex = localBlock.GetFarChildBlockReference(branchIndex);
                }
                IterateQueryOverlapPointsSubBounds(blockIndex, childBranchIndex, overlapVolume, subMin, subMax, depth + 1);
            }
            else if (localBlock.HasLeafs(branchIndex, subIndex))
            {
                AddModifiedBlock(blockIndex);
                int leafIndex = localBlock.GetLeafIndex(branchIndex, subIndex);
                localBlock.GetLeafDataArray().AddModifiedLeafIndex(leafIndex);
            }
        }

        /// <summary>
        /// Clears empty branch data based on the modify list created through a remove operation.
        /// </summary>
        public void ClearEmptyBranches(List<NativeList<int>> modifyList)
        {
            for (int i = modifyList.Count - 1; i >= 0; i--)
            {
                NativeList<int> modifiedRows = modifyList[i];
                for (int j = 0; j < modifiedRows.Length; j += 2)
                {
                    int blockIndex = modifiedRows[j];
                    int branchIndex = modifiedRows[j + 1];
                    if (
                        !m_treeStructureData[blockIndex].HasBranchAnyLeafs(branchIndex) &&
                        m_treeStructureData[blockIndex].GetChildBranchIndex(branchIndex) == -1)
                    {
                        m_treeStructureData[blockIndex].ClearRow(branchIndex);
                        continue;
                    }


                    if (!m_treeStructureData[blockIndex].HasBranchAnyChildren(branchIndex))
                    {
                        m_treeStructureData[blockIndex].ClearRow(branchIndex);
                    }
                    else
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            if (!m_treeStructureData[blockIndex].HasChild(branchIndex, k)) continue;

                            var childBranchIndex = m_treeStructureData[blockIndex].GetChildBranchIndex(branchIndex);
                            if (childBranchIndex == -1)
                            {
                                break;
                            }
                            childBranchIndex += k;
                            int childBlockIndex = blockIndex;
                            if (m_treeStructureData[blockIndex].IsFarChildReference(branchIndex))
                            {
                                childBlockIndex = m_treeStructureData[blockIndex].GetFarChildBlockReference(branchIndex);
                            }

                            if (!m_treeStructureData[childBlockIndex].HasBranchAnyChildren(childBranchIndex))
                            {
                                m_treeStructureData[childBlockIndex].ClearRow(childBranchIndex);
                                if (!m_treeStructureData[blockIndex].HasLeafs(branchIndex, k))
                                {
                                    m_treeStructureData[blockIndex].RemoveChild(branchIndex, k);
                                }
                            }
                        }
                    }
                }
            }
            modifyList.ForEach(nativeList =>
            {
                nativeList.Dispose();
            });
        }

        private void IterateRemoveInSubBounds(int blockIndex, int branchIndex, IOverlapVolume aABBOverlapVolume, float3 min, float3 max, int depth, List<NativeList<int>> modifyList)
        {
            float3 half = (max - min) * 0.5f;
            float3 diff = half;
            bool isRemoveOrIterate = false;
            for (int i = 0; i < 8; i++)
            {
                diff = half * DataConstants.F_MASK[i];
                float3 subMin = min + diff;
                float3 subMax = min + diff + half;
                if (aABBOverlapVolume.TestAABBOverlap(subMin, subMax))
                {
                    if (modifyList.Count <= depth)
                    {
                        modifyList.Add(new NativeList<int>(DataConstants.REMOVE_DEPTH_INITIAL_SLICE_SIZE, Allocator.Temp));
                    }
                    if (RemovePointsInSubBounds(blockIndex, branchIndex, i, aABBOverlapVolume, subMin, subMax, depth, modifyList))
                    {
                        isRemoveOrIterate = true;
                    }
                }
            }
            if (isRemoveOrIterate)
            {
                modifyList[depth].Add(blockIndex);
                modifyList[depth].Add(branchIndex);
            }
        }

        private bool RemovePointsInSubBounds(int blockIndex, int branchIndex, int subIndex, IOverlapVolume aABBOverlapVolume, float3 subMin, float3 subMax, int depth, List<NativeList<int>> modifyList)
        {
            var localBlock = m_treeStructureData[blockIndex];
            if (localBlock.IsSplitBranch(branchIndex, subIndex))
            {
                var childBranchIndex = localBlock.GetChildBranchIndex(branchIndex) + subIndex;
                if (localBlock.IsFarChildReference(branchIndex))
                {
                    blockIndex = localBlock.GetFarChildBlockReference(branchIndex);
                }
                IterateRemoveInSubBounds(blockIndex, childBranchIndex, aABBOverlapVolume, subMin, subMax, depth + 1, modifyList);
                return true;
            }
            else if (localBlock.HasLeafs(branchIndex, subIndex))
            {
                int leafIndex = localBlock.GetLeafIndex(branchIndex, subIndex);
                int leafCount = localBlock.GetLeafDataArray().GetPointCountAtLeaf(leafIndex);
                NativeSlice<LeafBody> leafBodies = localBlock.GetLeafDataArray().ExposeBodySlice(leafIndex);
                int outputLeafCount = aABBOverlapVolume.ExecuteOverlapRemoveJob(leafCount, leafBodies);
                if (outputLeafCount < leafCount)
                {
                    AddModifiedBlock(blockIndex);
                    localBlock.GetLeafDataArray().AddModifiedLeafIndex(leafIndex);
                }
                if (outputLeafCount == 0)
                {
                    localBlock.RemoveChild(branchIndex, subIndex);
                    localBlock.RemoveLeaf(branchIndex, subIndex);
                    return true;
                }
            }
            return false;
        }

        private void IterateSubBounds(int blockIndex, int branchIndex, PointData pointData, float3 min, float3 max)
        {
            float3 half = (max - min) * 0.5f;
            float3 diff = half;
            for (int i = 0; i < 8; i++)
            {
                diff = half * DataConstants.F_MASK[i];
                float3 subMin = min + diff;
                float3 subMax = min + diff + half;
                if (IsInBounds(pointData.Position, subMin, subMax))
                {
                    AddPointInSubBounds(blockIndex, branchIndex, i, pointData, subMin, subMax);
                }
            }
        }
        /// <summary>
        /// Adds a point to the tree.
        /// </summary>
        public void AddPoint(PointData pointData)
        {
            if (!m_isTreeInitialized)
            {
                AddInitialPoint(pointData);
            }
            else
            {
                if (IsInBounds(pointData.Position, m_min, m_max))
                {
                    IterateSubBounds(m_headBlockIndex, m_headIndex, pointData, m_min, m_max);
                }
                else
                {
                    float3 sideToExtend = new float3(0, 0, 0);
                    sideToExtend.x = pointData.Position.x > m_min.x ? 1.0f : 0.0f;
                    sideToExtend.y = pointData.Position.y > m_min.y ? 1.0f : 0.0f;
                    sideToExtend.z = pointData.Position.z > m_min.z ? 1.0f : 0.0f;
                    int nextHeadIndex = -1;
                    float3 inverseSideToExtend = sideToExtend - new float3(1, 1, 1);
                    inverseSideToExtend.x = Mathf.Abs(inverseSideToExtend.x);
                    inverseSideToExtend.y = Mathf.Abs(inverseSideToExtend.y);
                    inverseSideToExtend.z = Mathf.Abs(inverseSideToExtend.z);
                    int extendIndex = -1;
                    int childBitOffset = 0;
                    for (int i = 0; i < DataConstants.F_MASK.Length; i++)
                    {
                        if (DataConstants.F_MASK[i].Equals(sideToExtend))
                        {
                            nextHeadIndex = i;
                            extendIndex = i;
                        }
                        if (DataConstants.F_MASK[i].Equals(inverseSideToExtend))
                        {
                            childBitOffset = i;
                        }
                    }

                    int freeIndex = -1;
                    int nextHeadBlockIndex = m_headBlockIndex;
                    if (m_treeStructureData[m_headBlockIndex].HasFreeSpot(out freeIndex))
                    {
                        nextHeadIndex = freeIndex * 8 + nextHeadIndex;
                        m_treeStructureData[nextHeadBlockIndex].AddBlockReference(nextHeadIndex, -1, (m_headIndex / 8) * 8);
                    }
                    else
                    {
                        nextHeadBlockIndex = CreateNewBlock();
                        m_treeStructureData[nextHeadBlockIndex].AddBlockReference(nextHeadIndex, m_headBlockIndex, (m_headIndex / 8) * 8);
                    }
                    float3 diff = m_max - m_min;
                    m_treeStructureData[nextHeadBlockIndex].SetChildBit(nextHeadIndex, childBitOffset);
                    m_treeStructureData[m_headBlockIndex].MoveRow(m_headIndex, (m_headIndex / 8) * 8 + (childBitOffset));
                    m_max = (diff * DataConstants.F_MASK[extendIndex]) + m_max;
                    m_min = m_max - (2.0f * diff);

                    m_headBlockIndex = nextHeadBlockIndex;
                    m_headIndex = nextHeadIndex;
                    AddPoint(pointData);
                }
            }
        }
        /// <summary>
        /// Creates a new block by either reserving new memory or using an empty block that has not yet been removed.
        /// </summary>
        private int CreateNewBlock()
        {
            int index = m_treeStructureData.Count;
            if (m_reservedEmptyBlocks.Length > 0)
            {
                index = m_reservedEmptyBlocks[m_reservedEmptyBlocks.Length - 1];
                m_reservedEmptyBlocks.Length--;
            }
            else
            {
                m_treeStructureData.Add(new BlockTreeData());
                m_treeStructureData[index] = m_treeStructureData[index].Initialize();
            }
            return index;
        }
        /// <summary>
        /// Adds an index to the modified block list.
        /// </summary>
        public void AddModifiedBlock(int blockIndex)
        {
            for (int i = 0; i < m_modifiedBlocks.Length; i++)
            {
                if (m_modifiedBlocks[i] == blockIndex)
                {
                    return;
                }
            }
            m_modifiedBlocks.Add(blockIndex);
        }
        /// <summary>
        /// Clears the modified block list as well as all modified row lists in the leaf data arrays.
        /// </summary>
        public void ClearModifiedBlockList()
        {
            for (int i = 0; i < m_modifiedBlocks.Length; i++)
            {
                m_treeStructureData[m_modifiedBlocks[i]].ClearModifiedRowsList();
            }
            m_modifiedBlocks.Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsInBounds(float3 point, float3 min, float3 max)
        {
            return point.x > min.x && point.x <= max.x &&
                point.y > min.y && point.y <= max.y &&
                point.z > min.z && point.z <= max.z;
        }
        public List<BlockTreeData> GetBlockTreeDataList()
        {
            return m_treeStructureData;
        }
    }
}