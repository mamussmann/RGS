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

using NUnit.Framework;
using UnityEngine;
using PCMTool.Tree;
using Unity.Mathematics;
using PCMTool.Tree.OverlapVolume;
using Unity.Collections;

namespace PCMTool.Testing
{
    public class TreeTest
    {
        [Test]
        public void BlockTreeRowOperationTest()
        {
            BlockTreeDataRow row = new BlockTreeDataRow();
            for (int i = 6; i < 8; i++)
            {
                row.AddChild(i, false);
                Assert.IsTrue(row.IsChild(i), $"IsChild at {i}");
            }
            for (int i = 0; i < 8; i++)
            {
                row.RemoveChild(i);
                Assert.IsFalse(row.IsChild(i), $"IsChild at {i}");
            }
        }

        [Test]
        public void BlockTreeRowOperationAddAndCheckLeafTest()
        {
            BlockTreeDataRow row = new BlockTreeDataRow();

            for (int i = 0; i < 8; i++)
            {
                row.AddChild(i, true);
                Assert.IsTrue(row.IsChild(i), $"IsChild at {i}");
                Assert.IsTrue(row.IsLeaf(i), $"Leaf at {i}");
            }

            row.RemoveLeaf(0);
            Assert.IsFalse(row.IsLeaf(0), $"No Leaf at {0}");
            Assert.IsTrue(row.IsLeaf(1), $"Leaf at {1}");
        }

        [Test]
        public void ConstructEmptyTree()
        {
            PointCloudTree tree = new PointCloudTree();
            var blockList = tree.GetBlockTreeDataList();
            for (int i = 0; i < blockList.Count; i++)
            {
                var rowData = blockList[i].GetRowData();
                for (int j = 0; j < DataConstants.TREE_BLOCK_DATA_SIZE; j++)
                {
                    Assert.IsTrue(rowData[j].ChildMask == 0);
                    Assert.IsTrue(rowData[j].LeafMask == 0);
                }
            }
            tree.Dispose();
        }

        [Test]
        public void ConstructInitialTree()
        {
            PointCloudTree tree = new PointCloudTree();
            tree.AddPoint(new PointData(0.0f, 0.0f, 0.0f));
            Assert.IsTrue(tree.GetBlockTreeDataList()[0].GetRowData()[0].ChildMask == 1);
            Assert.IsTrue(tree.GetBlockTreeDataList()[0].GetRowData()[0].LeafMask == 1);
            tree.Dispose();
        }

        [Test]
        public void ConstructAndFillTreeInside()
        {
            PointCloudTree tree = new PointCloudTree();
            var initPoint = new float3(-0.25f,-0.25f,-0.25f);
            tree.AddPoint(new PointData(initPoint, new float3(1,0,1), 0));
            for (int i = 0; i < 8; i++)
            {
                var testPoint = initPoint + (DataConstants.INITIAL_SIZE * 0.5f) * DataConstants.F_MASK[i];
                tree.AddPoint(new PointData(testPoint, new float3(1,0,1), 0));
            }
            Assert.IsTrue(tree.GetBlockTreeDataList()[0].GetRowData()[0].ChildMask == 255, $"Eight child bits are set");
            Assert.IsTrue(tree.GetBlockTreeDataList()[0].GetRowData()[0].LeafMask == 255, $"Eight leaf bits are set.");
            tree.Dispose();
        }

        [Test]
        public void ConstructAndFillTreeOutside()
        {
            PointCloudTree tree = new PointCloudTree();
            tree.AddPoint(new PointData(new Vector3(0,0,0), new Vector3(1,0,1), 0));
            tree.AddPoint(new PointData(new Vector3(5.0f,1.0f,0.0f), new Vector3(1,0,1), 0));
            var blockList = tree.GetBlockTreeDataList();
            int sum = 0;
            for (int i = 0; i < blockList.Count; i++)
            {
                sum += blockList[i].GetLeafDataArray().CountModifiedLeafsWithFlag(1);
            }
            Assert.IsTrue(sum == 2);
            tree.Dispose();
        }

        [Test]
        public void ConstructAndFillTreeInsideWithRemoveTest()
        {
            PointCloudTree tree = new PointCloudTree();
            float range = 0.5f;
            int count = 500;
            for (int i = 0; i < count; i++)
            {
                float3 point = (new float3(1) * -0.5f) + ( range * DataConstants.F_MASK[i % 8]) + (new float3(1) * (range / 2.0f));
                tree.AddPoint(new PointData(point, point, 0));
                if( (i+1) % 8 == 0){
                    range *= 0.5f;
                    
                }
            }
            
            var blockList = tree.GetBlockTreeDataList();
            int sum = 0;
            for (int i = 0; i < blockList.Count; i++)
            {
                sum += blockList[i].GetLeafDataArray().CountModifiedLeafsWithFlag(1);
            }
            Assert.IsTrue(sum == count);
            NativeArray<int> output = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var modifyList = tree.RemovePoints(new AABBOverlapVolume(new float3(-1.0f), new float3(1.0f), output));
            output.Dispose();

            sum = 0;
            for (int i = 0; i < blockList.Count; i++)
            {
                sum += blockList[i].GetLeafDataArray().CountModifiedLeafsWithFlag(1);
            }
            Assert.IsTrue(sum == 0);
            tree.Dispose();
        }
    }
}