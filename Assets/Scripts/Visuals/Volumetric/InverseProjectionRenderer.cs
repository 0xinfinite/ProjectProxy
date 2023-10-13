using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InverseProjectionRenderer : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        
        
        private ProfilingSampler m_ProfilingSampler;
        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList = default;//new List<ShaderTagId>();

        private RenderTargetHandle depthTex;
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }
        static class ShaderIDs
        {
            internal static readonly int Opacity = Shader.PropertyToID("_Opacity");
            internal static readonly int InverseView = Shader.PropertyToID("_MatrixHClipToWorld");
        }

        
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler)) {
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            cmd.BeginSample("Inverse Projection");

            //var sheet = context.propertySheets.Get(Shader.Find("Hidden/Test/InverseProjection"));
            //sheet.properties.SetFloat(ShaderIDs.Opacity, settings.opacity);
            cmd.SetGlobalMatrix(ShaderIDs.InverseView, renderingData.cameraData.camera.cameraToWorldMatrix);

            //var pass = (int)settings.target.value;
            //cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, pass);

            cmd.EndSample("Inverse Projection");

            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            //var cmd = buffer;
            
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


