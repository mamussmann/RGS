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
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LeafPointRendererFeature : ScriptableRendererFeature
{

    class LeafPointRenderPass : ScriptableRenderPass
    {
        public int VertexCount;
        private Material _material;
        public LeafPointRenderPass(Material material)
        {
            _material = material;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "LeafPointRenderPass");
            Camera camera = renderingData.cameraData.camera;
            cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Points, VertexCount, 1);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private LeafPointRenderPass _rootSelectPointPass;
    public Material _material;
    private ComputeBuffer _buffer;
    public static LeafPointRendererFeature Instance;
    public override void Create()
    {
        if(Instance == null) {
            Instance = this;
        }
        _rootSelectPointPass = new LeafPointRenderPass(_material);
        _rootSelectPointPass.VertexCount = 0;
        _rootSelectPointPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public void SetBuffer(ComputeBuffer buffer, int vertexCount)
    {
        _buffer = buffer;
        _material.SetBuffer("_rootPointBuffer", buffer); 
        _rootSelectPointPass.VertexCount = vertexCount;
    }
    public void SetVertexCount(int vertexCount)
    {
        _rootSelectPointPass.VertexCount = vertexCount;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material != null)
        {
            renderer.EnqueuePass(_rootSelectPointPass);
        }
    }
}
