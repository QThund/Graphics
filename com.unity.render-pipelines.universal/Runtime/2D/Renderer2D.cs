using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Renderer2D : ScriptableRenderer
    {
        ColorGradingLutPass m_ColorGradingLutPass;
        Render2DLightingPass m_Render2DLightingPass;
        PostProcessPass m_PostProcessPass;
        PixelPerfectBackgroundPass m_PixelPerfectBackgroundPass;
        FinalBlitPass m_FinalBlitPass;
        PostProcessPass m_FinalPostProcessPass;
        Light2DCullResult m_LightCullResult;
        CustomPostProcessPass m_CustomPostProcessPass;

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Create Camera Textures");

        bool m_UseDepthStencilBuffer = true;
        bool m_CreateColorTexture;
        bool m_CreateDepthTexture;

        readonly RenderTargetHandle k_ColorTextureHandle;
        readonly RenderTargetHandle k_BloomColorTextureHandle;
        readonly RenderTargetHandle k_DepthTextureHandle;
        readonly RenderTargetHandle k_AfterPostProcessColorHandle;
        readonly RenderTargetHandle k_ColorGradingLutHandle;

        Material m_BlitMaterial;

        Renderer2DData m_Renderer2DData;

        internal bool createColorTexture => m_CreateColorTexture;
        internal bool createDepthTexture => m_CreateDepthTexture;

        public Renderer2D(Renderer2DData data) : base(data)
        {
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);

            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingOpaques, data.postProcessData);
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_PixelPerfectBackgroundPass = new PixelPerfectBackgroundPass(RenderPassEvent.AfterRendering + 1);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);
            m_CustomPostProcessPass = new CustomPostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data.postProcessData, m_BlitMaterial);

            m_UseDepthStencilBuffer = data.useDepthStencilBuffer;

            // We probably should declare these names in the base class,
            // as they must be the same across all ScriptableRenderer types for camera stacking to work.
            k_ColorTextureHandle.Init("_CameraColorTexture");
            k_BloomColorTextureHandle.Init("_BloomCameraColorTexture");
            k_DepthTextureHandle.Init("_CameraDepthAttachment");
            k_AfterPostProcessColorHandle.Init("_AfterPostProcessTexture");
            k_ColorGradingLutHandle.Init("_InternalGradingLut");

            m_Renderer2DData = data;

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };

            m_LightCullResult = new Light2DCullResult();
            m_Renderer2DData.lightCullResult = m_LightCullResult;
        }

        protected override void Dispose(bool disposing)
        {
            // always dispose unmanaged resources
            m_PostProcessPass.Cleanup();
            m_FinalPostProcessPass.Cleanup();
            m_ColorGradingLutPass.Cleanup();
            
            CoreUtils.Destroy(m_BlitMaterial);
        }

        public Renderer2DData GetRenderer2DData()
        {
            return m_Renderer2DData;
        }

        void CreateRenderTextures(
            ref CameraData cameraData,
            bool forceCreateColorTexture,
            FilterMode colorTextureFilterMode,
            CommandBuffer cmd,
            out RenderTargetHandle colorTargetHandle,
            out RenderTargetHandle bloomColorTargetHandle,
            out RenderTargetHandle depthTargetHandle)
        {
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            if (cameraData.renderType == CameraRenderType.Base)
            {
                m_CreateColorTexture = forceCreateColorTexture
                    || cameraData.postProcessEnabled
                    || cameraData.isHdrEnabled
                    || cameraData.isSceneViewCamera
                    || !cameraData.isDefaultViewport
                    || !m_UseDepthStencilBuffer
                    || !cameraData.resolveFinalTarget
                    || !Mathf.Approximately(cameraData.renderScale, 1.0f);

                m_CreateDepthTexture = !cameraData.resolveFinalTarget && m_UseDepthStencilBuffer;

                colorTargetHandle = m_CreateColorTexture ? k_ColorTextureHandle : RenderTargetHandle.CameraTarget;
                depthTargetHandle = m_CreateDepthTexture ? k_DepthTextureHandle : colorTargetHandle;

                bloomColorTargetHandle = k_BloomColorTextureHandle;

                if (m_CreateColorTexture)
                {
                    var colorDescriptor = cameraTargetDescriptor;
                    colorDescriptor.depthBufferBits = m_CreateDepthTexture || !m_UseDepthStencilBuffer ? 0 : 32;
                    cmd.GetTemporaryRT(k_ColorTextureHandle.id, colorDescriptor, colorTextureFilterMode);

                    var bloomColorDescriptor = cameraTargetDescriptor;
                    bloomColorDescriptor.depthBufferBits = 0;
                    cmd.GetTemporaryRT(k_BloomColorTextureHandle.id, bloomColorDescriptor, FilterMode.Point);
                }

                if (m_CreateDepthTexture)
                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                    cmd.GetTemporaryRT(k_DepthTextureHandle.id, depthDescriptor, FilterMode.Point);
                }
            }
            else    // Overlay camera
            {
                // These render textures are created by the base camera, but it's the responsibility of the last overlay camera's ScriptableRenderer
                // to release the textures in its FinishRendering().
                m_CreateColorTexture = true;
                m_CreateDepthTexture = true;

                colorTargetHandle = k_ColorTextureHandle;
                bloomColorTargetHandle = k_BloomColorTextureHandle;
                depthTargetHandle = k_DepthTextureHandle;
            }
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            bool stackHasPostProcess = renderingData.postProcessingEnabled;
            bool lastCameraInStack = cameraData.resolveFinalTarget;
            var colorTextureFilterMode = FilterMode.Bilinear;

            PixelPerfectCamera ppc = null;
            bool ppcUsesOffscreenRT = false;
            bool ppcUpscaleRT = false;

#if UNITY_EDITOR
            // The scene view camera cannot be uninitialized or skybox when using the 2D renderer.
            if (cameraData.cameraType == CameraType.SceneView)
            {
                renderingData.cameraData.camera.clearFlags = CameraClearFlags.SolidColor;
            }
#endif

            // Pixel Perfect Camera doesn't support camera stacking.
            if (cameraData.renderType == CameraRenderType.Base && lastCameraInStack)
            {
                cameraData.camera.TryGetComponent(out ppc);
                if (ppc != null)
                {
                    if (ppc.offscreenRTSize != Vector2Int.zero)
                    {
                        ppcUsesOffscreenRT = true;

                        // Pixel Perfect Camera may request a different RT size than camera VP size.
                        // In that case we need to modify cameraTargetDescriptor here so that all the passes would use the same size.
                        cameraTargetDescriptor.width = ppc.offscreenRTSize.x;
                        cameraTargetDescriptor.height = ppc.offscreenRTSize.y;
                    }

                    colorTextureFilterMode = ppc.finalBlitFilterMode;
                    ppcUpscaleRT = ppc.upscaleRT && ppc.isRunning;
                }
            }

            RenderTargetHandle colorTargetHandle;
            RenderTargetHandle bloomColorTargetHandle;
            RenderTargetHandle depthTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CreateRenderTextures(ref cameraData, ppcUsesOffscreenRT, colorTextureFilterMode, cmd,
                    out colorTargetHandle, out bloomColorTargetHandle, out depthTargetHandle);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(colorTargetHandle.Identifier()/*, color2TargetHandle.Identifier()*/, depthTargetHandle.Identifier());

            // We generate color LUT in the base camera only. This allows us to not break render pass execution for overlay cameras.
            if (stackHasPostProcess && cameraData.renderType == CameraRenderType.Base)
            {
                m_ColorGradingLutPass.Setup(k_ColorGradingLutHandle);
                EnqueuePass(m_ColorGradingLutPass);
            }

            m_Render2DLightingPass.ConfigureTarget(new RenderTargetIdentifier[]{ colorTargetHandle.Identifier(), bloomColorTargetHandle.Identifier() }, depthTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            // When using Upscale Render Texture on a Pixel Perfect Camera, we want all post-processing effects done with a low-res RT,
            // and only upscale the low-res RT to fullscreen when blitting it to camera target. Also, final post processing pass is not run in this case,
            // so FXAA is not supported (you don't want to apply FXAA when everything is intentionally pixelated).
            bool requireFinalPostProcessPass =
                lastCameraInStack && !ppcUpscaleRT && stackHasPostProcess && cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;

            if (cameraData.postProcessEnabled)
            {
                RenderTargetHandle postProcessDestHandle =
                    lastCameraInStack && !ppcUpscaleRT && !requireFinalPostProcessPass ? RenderTargetHandle.CameraTarget : k_AfterPostProcessColorHandle;

                m_PostProcessPass.Setup(
                    cameraTargetDescriptor,
                    colorTargetHandle,
                    bloomColorTargetHandle,
                    postProcessDestHandle,
                    depthTargetHandle,
                    k_ColorGradingLutHandle,
                    requireFinalPostProcessPass,
                    postProcessDestHandle == RenderTargetHandle.CameraTarget);

                EnqueuePass(m_PostProcessPass);
                colorTargetHandle = postProcessDestHandle;
            }

            if (ppc != null && ppc.isRunning && (ppc.cropFrameX || ppc.cropFrameY))
                EnqueuePass(m_PixelPerfectBackgroundPass);

            m_CustomPostProcessPass.Setup(cameraTargetDescriptor, colorTargetHandle);
            EnqueuePass(m_CustomPostProcessPass);

            if (requireFinalPostProcessPass)
            {
                m_FinalPostProcessPass.SetupFinalPass(colorTargetHandle);
                EnqueuePass(m_FinalPostProcessPass);
            }
            else if (lastCameraInStack && colorTargetHandle != RenderTargetHandle.CameraTarget)
            {
                m_FinalBlitPass.Setup(cameraTargetDescriptor, colorTargetHandle);
                EnqueuePass(m_FinalBlitPass);
            }
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
            m_LightCullResult.SetupCulling(ref cullingParameters, cameraData.camera);
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_CreateColorTexture)
            {
                cmd.ReleaseTemporaryRT(k_ColorTextureHandle.id);
                cmd.ReleaseTemporaryRT(k_BloomColorTextureHandle.id);
            }
            
            if (m_CreateDepthTexture)
                cmd.ReleaseTemporaryRT(k_DepthTextureHandle.id);
        }
    }
}
