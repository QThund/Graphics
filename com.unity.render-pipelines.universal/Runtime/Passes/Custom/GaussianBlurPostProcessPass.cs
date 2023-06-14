using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a blurred version of the image with a post-processing effect.
    /// </summary>
    public class GaussianBlurPostProcessPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        private Material m_postProcessMaterial;

        protected Material PostProcessMaterial
        {
            get
            {
                if (m_postProcessMaterial == null)
                {
                    m_postProcessMaterial = new Material(Shader.Find("Game/S_GaussianBlurPostProcess"));
                }

                return m_postProcessMaterial;
            }
        }

        const string k_RenderPostProcessingTag = "Render Gaussian Blur PostProcessing Effect";
        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);

        PostProcessData m_Data;

        Material m_BlitMaterial;

        public GaussianBlurPostProcessPass(RenderPassEvent evt, PostProcessData data, Material blitMaterial)
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
            GaussianBlur gaussianBlur = VolumeManager.instance.stack.GetComponent<GaussianBlur>();

            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTargetIdentifier cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : m_Source.id;

            if (gaussianBlur.IsActive() && !cameraData.isSceneViewCamera)
            {
                cmd.GetTemporaryRT(Shader.PropertyToID("_TempTarget"), GetCompatibleDescriptor(), FilterMode.Bilinear);
                int destination = Shader.PropertyToID("_TempTarget");

                PostProcessMaterial.SetFloat("_BlurSize", gaussianBlur.BlurSize.value);
                PostProcessMaterial.SetFloat("_Samples", gaussianBlur.Samples.value);
                //PostProcessMaterial.SetFloat("_Gauss", gaussianBlur.Gauss.value ? 1.0f : 0.0f);
                //PostProcessMaterial.SetFloat("_StandardDeviation", gaussianBlur.StandardDeviation.value);

                RenderingUtils.Blit(
                            cmd, cameraTarget, destination, PostProcessMaterial, 0, false,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);

                RenderingUtils.Blit(
                            cmd, destination, cameraTarget, PostProcessMaterial, 1, false,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            }
        }
    }
}
