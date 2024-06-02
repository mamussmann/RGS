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

using Unity.Mathematics;

namespace PCMTool.Tree
{
    /// <summary>
    /// Static class containing all configuration values.
    /// </summary>
    public static class DataConstants
    {
        /// <summary>
        /// The initial amount of reserved leafs in LeafDataArray
        /// </summary>
        public static readonly int TREE_INIT_LEAF_DATA_SIZE = 8;
        /// <summary>
        /// The amount of branches in a block. (Should be a multiple of 8)
        /// </summary>
        public static readonly int TREE_BLOCK_DATA_SIZE = 8 * 16;
        /// <summary>
        /// The amount of points within a leaf.
        /// </summary>
        public static readonly int TREE_BLOCK_LEAF_CAPACITY = 1024;
        /// <summary>
        /// The size of the bounding box around the first point added to the tree.
        /// </summary>
        public static readonly float INITIAL_SIZE = 1.0f;
        /// <summary>
        /// The initial capacity of the list containing indices about modified blocks.
        /// </summary>
        public static readonly int TREE_MODIFIED_BLOCKS_INITIAL_SIZE = 64;
        /// <summary>
        /// 
        /// </summary>
        public static readonly int TREE_RESERVED_EMPTY_BLOCKS = 64;
        public static readonly int REMOVE_DEPTH_INITIAL_SLICE_SIZE = 64; // initial allocated size of NativeLists in remove process
        public static readonly int COMPUTE_BUFFER_SIZE = 20000000;
        public static readonly float3[] F_MASK = new float3[]
            {
                new float3(0,0,0),
                new float3(0,1,0),
                new float3(1,0,0),
                new float3(1,1,0),

                new float3(1,1,1),
                new float3(0,1,1),
                new float3(0,0,1),
                new float3(1,0,1)
            };
    }
}