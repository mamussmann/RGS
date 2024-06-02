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
using PCMTool.Tree.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace PCMTool.Tree
{
    public class LeafBufferUpdater : ILeafBufferUpdater
    {
        public int CalculateAdditionalPoints(List<BlockTreeData> blockData, NativeList<int> modifiedBlocks)
        {
            int count = 0;
            for (int i = 0; i < modifiedBlocks.Length; i++)
            {
                int blockIndex = modifiedBlocks[i];
                var leafDataArray = blockData[blockIndex].GetLeafDataArray();
                count += leafDataArray.CountModifiedLeafsWithFlag(1);
            }
            return count;
        }
        public void UpdateBufferDataUpdate(LeafUpdateBufferInfo info)
        {
            for (int i = 0; i < info.ModifiedBlocks.Length; i++)
            {
                int blockIndex = info.ModifiedBlocks[i];
                var leafDataArray = info.BlockData[blockIndex].GetLeafDataArray();
                var modifiedList = leafDataArray.GetModifiedLeafsList();
                for (int j = 0; j < modifiedList.Length; j++)
                {
                    var head = leafDataArray.ExposeHeaderSlice(modifiedList[j]);
                    var body = leafDataArray.ExposeBodySlice(modifiedList[j]);
                    for (int k = 0; k < head[0].UsedLength; k++)
                    {
                        if(body[k].BufferIndex != -1 && body[k].ModifyFlag == 2)
                        {
                            int bufferIndex = body[k].BufferIndex;
                            info.ComputeBuffer[bufferIndex] = new PointData(){
                                PosCol = body[k].PosCol,
                                NormSize = body[k].NormSize,
                                PointType = body[k].PointType
                            };
                            body[k] = body[k].SetModifyFlag(0);
                        }
                    }
                }
            }
        }
        public void UpdateAllBufferDataUpdate(LeafUpdateBufferInfo info)
        {
            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(128, Allocator.Temp);
            for (int i = 0; i < info.BlockData.Count; i++)
            {
                var leafDataArray = info.BlockData[i].GetLeafDataArray();

                var job = new PointBufferUpdateJob()
                {
                    ComputeBuffer = info.ComputeBuffer,
                    LeafHeaders = leafDataArray.ExposeHeaderNativeList(),
                    LeafBodies = leafDataArray.ExposeBodyNativeList()
                };
                jobHandles.Add(job.Schedule(leafDataArray.ExposeHeaderNativeList().Length, 16));
            }
            JobHandle.CompleteAll(jobHandles);
            jobHandles.Dispose();
        }
        public int UpdateBufferAddUpdate(LeafUpdateBufferInfo info)
        {
            int usedBufferSize = info.UsedBufferSize;
            for (int i = 0; i < info.ModifiedBlocks.Length; i++)
            {
                int blockIndex = info.ModifiedBlocks[i];
                var leafDataArray = info.BlockData[blockIndex].GetLeafDataArray();
                var modifiedList = leafDataArray.GetModifiedLeafsList();
                for (int j = 0; j < modifiedList.Length; j++)
                {
                    var head = leafDataArray.ExposeHeaderSlice(modifiedList[j]);
                    var body = leafDataArray.ExposeBodySlice(modifiedList[j]);
                    for (int k = 0; k < head[0].UsedLength; k++)
                    {
                        if(body[k].BufferIndex == -1 && body[k].ModifyFlag == 1)
                        {
                            info.ComputeBuffer[usedBufferSize] = new PointData(body[k].PosCol, body[k].NormSize, body[k].PointType);
                            body[k] = body[k].SetBufferIndex(usedBufferSize);
                            body[k] = body[k].SetModifyFlag(0);
                            info.RowIndexBuffer[usedBufferSize * 2] = blockIndex;
                            info.RowIndexBuffer[usedBufferSize * 2+1] = modifiedList[j] * DataConstants.TREE_BLOCK_LEAF_CAPACITY + k;
                            usedBufferSize += 1;
                        }
                    }
                }
            }
            return usedBufferSize;
        }

        public int UpdateBufferRemoveUpdate(LeafUpdateBufferInfo info)
        {
            int usedBufferSize = info.UsedBufferSize;
            for (int i = 0; i < info.ModifiedBlocks.Length; i++)
            {
                int blockIndex = info.ModifiedBlocks[i];
                var leafDataArray = info.BlockData[blockIndex].GetLeafDataArray();
                var modifiedList = leafDataArray.GetModifiedLeafsList();
                for (int j = 0; j < modifiedList.Length; j++)
                {
                    var head = leafDataArray.ExposeHeaderSlice(modifiedList[j]);
                    var body = leafDataArray.ExposeBodySlice(modifiedList[j]);
                    for (int k = head[0].UsedLength - 1; k >= 0; k--)
                    {
                        int bufferIndex = body[k].BufferIndex;
                        if(bufferIndex != -1 && body[k].ModifyFlag == 3)
                        {
                            if(body[k].BufferIndex != usedBufferSize-1){
                                info.ComputeBuffer[bufferIndex] = info.ComputeBuffer[(usedBufferSize-1)];
                                var leafBodies = info.BlockData[info.RowIndexBuffer[(usedBufferSize-1) * 2]].GetLeafDataArray().ExposeBodyNativeList();
                                int bodyDataIndex = info.RowIndexBuffer[(usedBufferSize-1) * 2 + 1];
                                leafBodies[bodyDataIndex] = leafBodies[bodyDataIndex].SetBufferIndex(bufferIndex);

                                info.RowIndexBuffer[bufferIndex * 2] = info.RowIndexBuffer[(usedBufferSize-1) * 2];
                                info.RowIndexBuffer[bufferIndex * 2+1] = info.RowIndexBuffer[(usedBufferSize-1) * 2 + 1];
                            }
                            body[k] = new LeafBody()
                            {
                                BufferIndex = -1,
                                ModifyFlag = 0
                            };
                            usedBufferSize -= 1;
                            
                            if(k != head[0].UsedLength-1) {
                                body[k] = body[head[0].UsedLength-1];
                                bufferIndex = body[k].BufferIndex;
                                info.RowIndexBuffer[bufferIndex * 2+1] = modifiedList[j] * DataConstants.TREE_BLOCK_LEAF_CAPACITY + k;
                            }
                            head[0] = head[0].DecrementLength();
                        }else if(bufferIndex == -1 && body[k].ModifyFlag == 3)
                        {
                            body[k] = body[k].SetModifyFlag(0);

                            if(k != head[0].UsedLength-1) {
                                body[k] = body[head[0].UsedLength-1];
                                bufferIndex = body[k].BufferIndex;
                                info.RowIndexBuffer[bufferIndex * 2+1] = modifiedList[j] * DataConstants.TREE_BLOCK_LEAF_CAPACITY + k;
                            }
                            head[0] = head[0].DecrementLength();
                        }
                    }
                }
            }

            return usedBufferSize;
        }
    }
}