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
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SphereTracingRenderPassFeature : ScriptableRendererFeature
{
    [SerializeField]  private SphereTracingPassSettings m_settings;
    class SphereTracingRenderPass : ScriptableRenderPass
    {
        const string ProfilerTag = "Sphere Tracing Pass";
        RenderTargetIdentifier colorBuffer;
        SphereTracingPassSettings m_settings;
        Material material;
        public int PointCount;
        public bool UseBgColor;
        public bool RenderSegments;
        public Color BgColor;
        public float MinAge;
        public float MaxAge;
        public Vector3 Min;
        public Vector3 Max;
        public bool RenderRootAgeGradient;
        public ComputeBuffer PointBuffer;
        private float[] kernel;
        public SphereTracingRenderPass(SphereTracingPassSettings settings)
        {
            if(material == null) material = CoreUtils.CreateEngineMaterial("RGS/SphereTracing");
            m_settings = settings;
            float s = 1.0f;
            kernel = new float[81] {
                s,-s,s, s,-s,0, s,-s,-s,
                0,-s,s, 0,-s,0, 0,-s,-s,
                -s,-s,s, -s,-s,0, -s,-s,-s,

                s,0,s, s,0,0, s,0,-s,
                0,0,s, 0,0,0, 0,0,-s,
                -s,0,s, -s,0,0, -s,0,-s,

                s,s,s, s,s,0, s,s,-s,
                0,s,s, 0,s,0, 0,s,-s,
                -s,s,s, -s,s,0, -s,s,-s,
            };
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Grab the camera target descriptor. We will use this when creating a temporary render texture.
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            
            // Set the number of depth bits we need for our temporary render texture.
            descriptor.depthBufferBits = 0;
            // Grab the color buffer from the renderer camera color target.
            colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;
            //ConfigureInput(ScriptableRenderPassInput.Color);
if(material == null) return;
            if(RenderSegments)
            {
                material.EnableKeyword("_CAPSULE_SEGMENTS");
            }else {
                material.DisableKeyword("_CAPSULE_SEGMENTS");
            }
            material.SetInt("_PointCount", 0);
            material.SetInt("_MaxSteps", m_settings.MaxSteps);
            material.SetFloat("_MinDistance", m_settings.MinDistance);
            material.SetFloat("_normalAvgOffsetScale", m_settings.NormalAvgOffsetScale);
            material.SetFloat("_maxDistance", m_settings.MaxTraceDistance);

            material.SetInt("_MaxShadowSteps", m_settings.MaxShadowSteps);
            material.SetFloat("_softShadowScale", m_settings.SoftShadowScale);
            material.SetFloat("_ShadowMinDistance", m_settings.ShadowMinDistance);
            material.SetFloat("_maxShadowDistance", m_settings.MaxShadowTraceDistance);
            material.SetFloat("_showAgeGradient", RenderRootAgeGradient ? 1.0f : 0.0f);
            material.SetFloat("_minAge", MinAge);
            material.SetFloat("_maxAge", MaxAge);
            material.SetFloatArray("_normalAvgKernel", kernel);
            material.SetFloat("_ambientLightIntensity", m_settings.AmbientLightIntensity);
            material.SetFloat("_specularIntensity", m_settings.SpecularIntensity);
            material.SetFloat("_minX", Min.x);
            material.SetFloat("_minY", Min.y);
            material.SetFloat("_minZ", Min.z);
            material.SetFloat("_maxX", Max.x);
            material.SetFloat("_maxY", Max.y);
            material.SetFloat("_maxZ", Max.z);
            material.SetFloat("_useBgColor", UseBgColor ? 1.0f : 0.0f);
            material.SetColor("_bgColor", BgColor);
            if(PointBuffer != null)
            {
                material.SetInt("_PointCount", PointCount);
                material.SetBuffer("_PointBuffer", PointBuffer);
            }
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
            {
                Blit(cmd, colorBuffer, colorBuffer, material, 0); // shader pass 0
            }
            
            // Execute the command buffer and release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null) throw new ArgumentNullException("cmd");
        
        }
    }

    [Serializable]
    internal class SphereTracingPassSettings
    {
        [Min(0)] public int MaxSteps;
        [Range(0.0001f, 0.01f)] public float MinDistance;
        [HideInInspector][Range(0.0001f, 0.01f)] public float NormalAvgOffsetScale; // not used at the moment due to performance overhead of calculating the average normal value
        [Range(0.1f, 100.0f)] public float MaxTraceDistance;
        [Header("Shadow")]
        [Min(0)] public int MaxShadowSteps;
        [Range(1.0f, 16.0f)] public float SoftShadowScale;
        [Range(0.0001f, 0.01f)] public float ShadowMinDistance;
        [Range(0.1f, 100.0f)] public float MaxShadowTraceDistance;
        [Range(0.0f, 1.0f)] public float AmbientLightIntensity;
        [Min(0.0f)] public float SpecularIntensity;
    }
    public static SphereTracingRenderPassFeature Instance;
    SphereTracingRenderPass m_ScriptablePass;

    public void SetPointBuffer(ComputeBuffer buffer, int pointCount)
    {
        m_ScriptablePass.PointBuffer = buffer;
        m_ScriptablePass.PointCount = pointCount;
    }
    public void SetMinMaxAge(float minAge, float maxAge)
    {
        m_ScriptablePass.MinAge = minAge;
        m_ScriptablePass.MaxAge = maxAge;
    }
    public void SetBounds(Vector3 min, Vector3 max)
    {
        m_ScriptablePass.Min = min;
        m_ScriptablePass.Max = max;
    }
    public void SetBgColor(Color color, bool useColor)
    {
        m_ScriptablePass.UseBgColor = useColor;
        m_ScriptablePass.BgColor = color;
    }
    public void SetSegmentRenderKeyword(bool renderSegments)
    {
        m_ScriptablePass.RenderSegments = renderSegments;
    }
    public void RenderRootAgeGradent(bool value)
    {
        m_ScriptablePass.RenderRootAgeGradient = value;
    }
    /// <inheritdoc/>
    public override void Create()
    {
        if(Instance == null) {
            Instance = this;
        }
        m_ScriptablePass = new SphereTracingRenderPass(m_settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


