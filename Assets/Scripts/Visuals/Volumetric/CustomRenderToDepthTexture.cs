using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/* 
- Renders DepthOnly pass to _CameraDepthTexture, filtered by LayerMask
- If the depth texture is generated via a Depth Prepass, URP uses the Opaque Layer Mask at the top of the Forward/Universal Renderer asset
  to determine which objects should be rendered to the depth texture. This feature can be used to render objects on *other layers* into the depth texture as well.
- The Frame Debugger window can be used to check if a project is using a Depth Prepass vs Copy Depth pass, though unsure if that is the same for final build.
- Also see :
  https://github.com/Unity-Technologies/Graphics/blob/d48795e93b2e62d936ca6fca61364922f8c91285/com.unity.render-pipelines.universal/Runtime/UniversalRenderer.cs#L436
  https://github.com/Unity-Technologies/Graphics/blob/d48795e93b2e62d936ca6fca61364922f8c91285/com.unity.render-pipelines.universal/Runtime/UniversalRenderer.cs#L1174
Example : 
- This RenderObjects example from the URP docs allows you to see the character through walls :
  https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/renderer-features/how-to-custom-effect-render-objects.html
- But the character will no longer appear in the depth texture if using a Depth Prepass (but still appears if using Copy Depth, just to be confusing! woo~)
- Any shaders that rely on the depth texture (aka Scene Depth node) now won't include the character. e.g. Water Foam
- Using this feature can fix that, by allowing us to add the Character layer to the depth texture
- Note : Probably shouldn't be used with SSAO Depth Normals mode since that makes depth texture generation use DepthNormals instead of DepthOnly passes 
  and might be weird to have objects appear in the depth texture but not the normals texture.
*/
public class CustomRenderToDepthTexture : ScriptableRendererFeature {

	public LayerMask layerMask;

	public RenderPassEvent _event = RenderPassEvent.BeforeRenderingSkybox;
	/* 
	- Set to at least AfterRenderingPrePasses if depth texture is generated via a Depth Prepass,
		or it will just be overriden
	- Set to at least BeforeRenderingSkybox if depth texture is generated via a Copy Depth pass,
	  	Anything before this is already included in the texture! (Though not for Scene View as that always uses a prepass)
	*/

	class RenderToDepthTexturePass : ScriptableRenderPass {

		private ProfilingSampler m_ProfilingSampler;
		private FilteringSettings m_FilteringSettings;
		private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

		private int targetCameraID;
		[SerializeField]private /*RenderTargetHandle*/RenderTexture targetDepthTex;

		public RenderToDepthTexturePass(LayerMask layerMask) {
			m_ProfilingSampler = new ProfilingSampler("RenderToDepthTexture");
			m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
			m_ShaderTagIdList.Add(new ShaderTagId("DepthOnly")); // Only render DepthOnly pass
			//targetDepthTex.Init("_CameraDepthTexture");
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
			//ConfigureTarget(targetDepthTex.Identifier());
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			Camera camera = renderingData.cameraData.camera;
			if (camera.GetInstanceID() != targetCameraID)
				return;
			
			camera.Render();
			
			SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
			DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

			CommandBuffer cmd = CommandBufferPool.Get();
			using (new ProfilingScope(cmd, m_ProfilingSampler)) {
				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void OnCameraCleanup(CommandBuffer cmd) { }
	}

	RenderToDepthTexturePass m_ScriptablePass;

	public override void Create() {
		m_ScriptablePass = new RenderToDepthTexturePass(layerMask);
		m_ScriptablePass.renderPassEvent = _event;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		if (!renderingData.cameraData.requiresDepthTexture) return;
		renderer.EnqueuePass(m_ScriptablePass);
	}
}