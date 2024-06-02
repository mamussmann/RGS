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
using Unity.Mathematics;
using UnityEngine;

namespace PCMTool.Tree.Debugging
{
    /// <summary>
    /// Provides a debug view of the sparse octree structure using bounding boxes
    /// </summary>
    public class TreeBoundsViewer : MonoBehaviour
    {
        [SerializeField] private LeafRenderer m_leafRenderer;
        [SerializeField] private bool m_showBounds;
        [SerializeField] private Color m_boundsColor = Color.black;
        [SerializeField][Min(0)] private int m_maxDepth;
        private List<Vector3> m_currentBoundsList;

        /// <summary>
        /// Generates bounding boxes based on the referenced leaf renderer.
        /// </summary>
        public void GenerateBoundsList()
        {
            if (m_leafRenderer == null)
            {
                Debug.LogWarning("Failed to generate Bounds! Leaf renderer reference not set.");
                return;
            }
            m_currentBoundsList = new List<Vector3>();
            var tree = m_leafRenderer.PointCloudTree;
            TravelTree(m_currentBoundsList, tree.GetBlockTreeDataList(), tree.RootBlockIndex, tree.RootBranchIndex, tree.RootMin, tree.RootMax, m_maxDepth);
        }
        /// <summary>
        /// Traverses the tree from the given root down to the max depth.
        /// </summary>
        /// <param name="list"> The output list containing bounds as min, max pairs.</param>
        private void TravelTree(List<Vector3> list, List<BlockTreeData> treeStructureData, int blockIndex, int branchIndex, float3 min, float3 max, int maxDepth)
        {
            list.Add(min);
            list.Add(max);
            if (maxDepth <= 0) return;
            float3 half = (max - min) * 0.5f;
            float3 diff = half;
            for (int i = 0; i < 8; i++)
            {
                diff = half;
                diff = half * DataConstants.F_MASK[i];
                float3 subMin = min + diff;
                float3 subMax = min + diff + half;
                if(treeStructureData[blockIndex].HasChild(branchIndex, i) ){
                    if (!treeStructureData[blockIndex].HasLeafs(branchIndex, i))
                    {
                        var newBlockIndex = blockIndex;
                        if (treeStructureData[blockIndex].IsFarChildReference(branchIndex))
                        {
                            newBlockIndex = treeStructureData[blockIndex].GetFarChildBlockReference(branchIndex);
                        }
                        TravelTree(list, treeStructureData, newBlockIndex, treeStructureData[blockIndex].GetChildBranchIndex(branchIndex) + i, subMin, subMax, maxDepth - 1);
                    }
                    else if (!treeStructureData[blockIndex].HasLeafs(branchIndex, i))
                    {
                        list.Add(subMin);
                        list.Add(subMax);
                    }
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (!m_showBounds) return;
            if (m_currentBoundsList == null) return;
            Gizmos.color = m_boundsColor;
            for (int i = 0; i < m_currentBoundsList.Count; i += 2)
            {
                Vector3 min = m_currentBoundsList[i];
                Vector3 max = m_currentBoundsList[i + 1];
                Vector3 diff = max - min;
                Gizmos.DrawWireCube(min + (diff * 0.5f), diff);
            }
        }
    }

}