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

public class HighlightRootPointCutRendererFeature : ScriptableRendererFeature
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RootSelectPointData
    {
        public float3 Pos;
        public float2 IDSize;

        public RootSelectPointData(float3 pos, float2 idSize)
        {
            Pos = pos;
            IDSize = idSize;
        }
    }

    class HighlightRootPointCutPass : ScriptableRenderPass
    {
        public int VertexCount;
        private Material _material;
        public HighlightRootPointCutPass(Material material)
        {
            _material = material;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "HightlightRootPointCutPass");
            Camera camera = renderingData.cameraData.camera;
            cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Points, VertexCount, 1);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private HighlightRootPointCutPass _rootSelectPointPass;
    public Material _material;
    private ComputeBuffer _pointBuffer;
    private ComputeBuffer _selectedRootIndexBuffer;
    public static HighlightRootPointCutRendererFeature Instance;
    public override void Create()
    {
        if(Instance == null) {
            Instance = this;
        }
        _rootSelectPointPass = new HighlightRootPointCutPass(_material);
        _rootSelectPointPass.VertexCount = 0;
        _rootSelectPointPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public void SetBuffer(ComputeBuffer buffer, int vertexCount)
    {
        _pointBuffer = buffer;
        _material.SetBuffer("_rootPointBuffer", buffer); 
        _rootSelectPointPass.VertexCount = vertexCount;
    }
    
    public void SetSelectedRootIndexBuffer(ComputeBuffer buffer, int usedBufferSize)
    {
        _material.SetBuffer("_rootPointIndexBuffer", buffer); 
        _material.SetInt("_rootPointIndexBufferSize", usedBufferSize); 
        _selectedRootIndexBuffer = buffer;
    }
    public void SetTime(float time)
    {
        _material.SetFloat("_startTime", time); 
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material != null && _pointBuffer != null && _pointBuffer.IsValid() && _selectedRootIndexBuffer != null && _selectedRootIndexBuffer.IsValid())
        {
            renderer.EnqueuePass(_rootSelectPointPass);
        }
    }
}
