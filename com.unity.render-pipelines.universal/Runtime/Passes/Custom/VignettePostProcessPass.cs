using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a border around the screen with a post-processing effect.
    /// </summary>
    public class VignettePostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        private Material m_postProcessMaterial;

        protected Material PostProcessMaterial
        {
            get
            {
                if(m_postProcessMaterial == null)
                {
                    m_postProcessMaterial = new Material(Shader.Find("Game/S_VignettePostProcess"));
                }

                return m_postProcessMaterial;
            }
        }

        const string k_RenderPostProcessingTag = "Render Vignette PostProcessing Effect";
        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);

        PostProcessData m_Data;

        Material m_BlitMaterial;

        public VignettePostProcessPass(RenderPassEvent evt, PostProcessData data, Material blitMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(PostProcessPass));
            renderPassEvent = evt;
            m_Data = data;
            m_BlitMaterial = blitMaterial;
        }

        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle source)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Source = source;
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
            CustomVignette customVignette = VolumeManager.instance.stack.GetComponent<CustomVignette>();

            if(customVignette.IsActive() && !renderingData.cameraData.isSceneViewCamera)
            {
                cmd.GetTemporaryRT(Shader.PropertyToID("_TempTarget"), GetCompatibleDescriptor(), FilterMode.Bilinear);
                int destination = Shader.PropertyToID("_TempTarget");

                PostProcessMaterial.SetFloat("_VignetteRadius", customVignette.VignetteRadius.value);
                PostProcessMaterial.SetFloat("_VignetteGradientPower", customVignette.VignetteGradientPower.value);
                PostProcessMaterial.SetColor("_VignetteColor", customVignette.VignetteColor.value);
                PostProcessMaterial.SetTexture("_VignetteTexture", customVignette.VignetteTexture.value);
                PostProcessMaterial.SetFloat("_TextureAlphaClipThreshold", customVignette.TextureAlphaClipThreshold.value);

                RenderingUtils.Blit(
                            cmd, m_Source.id, destination, PostProcessMaterial, 0, false,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

                RenderingUtils.Blit(cmd, destination, m_Source.id, m_BlitMaterial);
            }
        }
    }
}
