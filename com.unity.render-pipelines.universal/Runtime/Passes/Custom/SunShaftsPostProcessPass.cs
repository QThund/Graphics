using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a custom post-processing effect.
    /// </summary>
    public class SunShaftsPostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        RenderTargetHandle m_sunShaftsTexture;
        private Material m_radialBlurMaterial;
        private Material m_gaussianBlurMaterial;
        private Material m_postProcessMaterial;

        const string k_RenderPostProcessingTag = "Sun Shafts PostProcessing Effect";
        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);

        PostProcessData m_Data;

        Material m_BlitMaterial;

        public SunShaftsPostProcessPass(RenderPassEvent evt, PostProcessData data, Material blitMaterial)
        {
            m_radialBlurMaterial = new Material(Shader.Find("Game/S_RadialBlurPostProcess"));
            m_gaussianBlurMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/BlurFx"));
            m_postProcessMaterial = new Material(Shader.Find("Game/S_SunShaftsPostProcess"));
            base.profilingSampler = new ProfilingSampler(nameof(PostProcessPass));
            renderPassEvent = evt;
            m_Data = data;
            m_BlitMaterial = blitMaterial;
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle source, in RenderTargetHandle sunShaftsTexture)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Source = source;
            m_sunShaftsTexture = sunShaftsTexture;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingRenderPostProcessing))
            {
                Render(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void DrawFullscreenMesh(CommandBuffer cmd, Material material, int passIndex)
        {
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, passIndex);
        }

        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat, m_Descriptor.depthBufferBits);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            var desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            SunShafts sunShafts = VolumeManager.instance.stack.GetComponent<SunShafts>();

            if(!sunShafts.active)
            {
                return;
            }

            cmd.GetTemporaryRT(Shader.PropertyToID("_TempTarget"), GetCompatibleDescriptor(), FilterMode.Bilinear);
            int blurredSunShaftsTempTexture = Shader.PropertyToID("_TempTarget");
            cmd.GetTemporaryRT(Shader.PropertyToID("_TempTarget2"), GetCompatibleDescriptor(), FilterMode.Bilinear);
            int gaussianBlurredTempTexture = Shader.PropertyToID("_TempTarget2");
            cmd.GetTemporaryRT(Shader.PropertyToID("_TempTarget3"), GetCompatibleDescriptor(), FilterMode.Bilinear);
            int addedToBackbufferTempTexture = Shader.PropertyToID("_TempTarget3");
            cmd.SetGlobalTexture("_BackbufferTex", m_Source.Identifier());

            m_radialBlurMaterial.SetFloat("_EffectAmount", sunShafts.RadialBlurAmount.value);
            m_radialBlurMaterial.SetFloat("_Radius", sunShafts.RadialBlurRadius.value);
            m_radialBlurMaterial.SetFloat("_CenterX", sunShafts.RadialBlurCenter.value.x);
            m_radialBlurMaterial.SetFloat("_CenterY", sunShafts.RadialBlurCenter.value.y);
            m_radialBlurMaterial.SetFloat("_Samples", sunShafts.RadialBlurSamples.value);

            RenderingUtils.Blit(
                        cmd, m_sunShaftsTexture.id, blurredSunShaftsTempTexture, m_radialBlurMaterial, 0, false,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            m_gaussianBlurMaterial.SetFloat("_BlurSize", sunShafts.GaussianBlurSize.value);
            m_gaussianBlurMaterial.SetFloat("_Samples", sunShafts.GaussianBlurSamples.value);
            m_gaussianBlurMaterial.SetFloat("_StandardDeviation", sunShafts.GaussianBlurStandardDeviation.value);

            RenderingUtils.Blit(
                        cmd, blurredSunShaftsTempTexture, gaussianBlurredTempTexture, m_gaussianBlurMaterial, 0, false,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            m_postProcessMaterial.SetFloat("_SunShaftsAlphaMultiplier", sunShafts.ShaftsAlphaMultiplier.value);

            RenderingUtils.Blit(
                        cmd, gaussianBlurredTempTexture, addedToBackbufferTempTexture, m_postProcessMaterial, 0, false,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

            RenderingUtils.Blit(cmd, addedToBackbufferTempTexture, m_Source.id, m_BlitMaterial);
        }
    }
}
