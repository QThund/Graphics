using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DRendererData_overview.html")]
    public partial class Renderer2DData : ScriptableRendererData
    {
        // CUSTOM CODE
        [Serializable]
        public class RenderTargetData
        {
            public string Name;
            
            [Tooltip("Enable random access write into this render texture on Shader Model 5.0 level shaders. See Also: RenderTexture.enableRandomWrite.")]
            public bool EnableRandomWrite;
            
            [Tooltip("Mipmap levels are generated automatically when this flag is set.")]
            public bool AutoGenerateMips;

            [Tooltip("Render texture has mipmaps when this flag is set. See Also: RenderTexture.useMipMap.")]
            public bool UseMipMap;

            [Tooltip("The render texture memoryless mode property.")]
            public RenderTextureMemoryless Memoryless;

            [Tooltip("If this RenderTexture is a VR eye texture used in stereoscopic rendering, this property decides what special rendering occurs, if any. Instead of setting this manually, use the value returned by XR.XRSettings.eyeTextureDesc|eyeTextureDesc or other VR functions returning a RenderTextureDescriptor.")]
            public VRTextureUsage VrUsage;
            
            [Tooltip("Determines how the RenderTexture is sampled if it is used as a shadow map. See also: ShadowSamplingMode for more details.")]
            public ShadowSamplingMode ShadowSamplingMode;

            [Tooltip("Dimensionality (type) of the render texture. See Also: RenderTexture.dimension.")]
            public TextureDimension Dimension;

            [Tooltip("The precision of the render texture's depth buffer in bits (0, 16, 24/32 are supported). See Also: RenderTexture.depth.")]
            public int DepthBufferBits;
            
            [Tooltip("The format of the stencil data that you can encapsulate within a RenderTexture. Specifying this property creates a stencil element for the RenderTexture and sets its format. This allows for stencil data to be bound as a Texture to all shader types for the platforms that support it. This property does not specify the format of the stencil buffer, which is constrained by the depth buffer format specified in RenderTexture.depth. Currently, most platforms only support R8_UInt (DirectX11, DirectX12), while PS4 also supports R8_UNorm.")]
            public GraphicsFormat StencilFormat;

            public RenderTextureFormat ColorFormat { get; set; }

            [Tooltip("If true and msaaSamples is greater than 1, the render texture will not be resolved by default. Use this if the render texture needs to be bound as a multisampled texture in a shader.")]
            public bool BindMS;

            [Tooltip("The color format for the RenderTexture.")]
            public GraphicsFormat GraphicsFormat;

            [Tooltip("User-defined mipmap count.")]
            public int MipCount;

            [Tooltip("Volume extent of a 3D render texture.")]
            public int VolumeDepth;

            [Tooltip("The multisample antialiasing level for the RenderTexture. See Also: RenderTexture.antiAliasing.")]
            [Min(1)]
            public int MsaaSamples;

            [Tooltip("This flag causes the render texture uses sRGB read/write conversions.")]
            public bool sRGB;

            [Tooltip("Set to true to enable dynamic resolution scaling on this render texture. See also: RenderTexture.useDynamicScale")]
            public bool UseDynamicScale;

            public FilterMode TextureFilter;
        }
        //

        public enum Renderer2DDefaultMaterialType
        {
            Lit,
            Unlit,
            Custom
        }

        [SerializeField]
        TransparencySortMode m_TransparencySortMode = TransparencySortMode.Default;

        [SerializeField]
        Vector3 m_TransparencySortAxis = Vector3.up;

        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField, FormerlySerializedAs("m_LightOperations")]
        Light2DBlendStyle[] m_LightBlendStyles = null;

        [SerializeField]
        bool m_UseDepthStencilBuffer = true;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape.shader")]
        Shader m_ShapeLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape-Volumetric.shader")]
        Shader m_ShapeLightVolumeShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point.shader")]
        Shader m_PointLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point-Volumetric.shader")]
        Shader m_PointLightVolumeShader = null;

        [SerializeField, Reload("Shaders/Utils/Blit.shader")]
        Shader m_BlitShader = null;

        [SerializeField, Reload("Shaders/2D/ShadowGroup2D.shader")]
        Shader m_ShadowGroupShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2DRemoveSelf.shader")]
        Shader m_RemoveSelfShadowShader = null;

        [SerializeField, Reload("Runtime/Data/PostProcessData.asset")]
        PostProcessData m_PostProcessData = null;

        // CUSTOM CODE
        [SerializeField]
        List<RenderTargetData> m_renderTargets = new List<RenderTargetData>();

        public List<RenderTargetData> AdditionalRenderTargets
        {
            get
            {
                return m_renderTargets;
            }
        }

        public int GetIndexOfRenderTarget(string renderTargetName)
        {
            int index = -1;

            for(int i = 0; i < m_renderTargets.Count; ++i)
            {
                if(m_renderTargets[i].Name == renderTargetName)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        public Material ShadowBlurBlitMaterial
        {
            get
            {
                return m_shadowBlurBlitMaterial;
            }
        }

        [SerializeField]
        Material m_shadowBlurBlitMaterial;
        //

        public float hdrEmulationScale => m_HDREmulationScale;
        public Light2DBlendStyle[] lightBlendStyles => m_LightBlendStyles;
        internal bool useDepthStencilBuffer => m_UseDepthStencilBuffer;

        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;
        internal Shader shadowGroupShader => m_ShadowGroupShader;
        internal Shader removeSelfShadowShader => m_RemoveSelfShadowShader;
        internal PostProcessData postProcessData => m_PostProcessData;
        internal TransparencySortMode transparencySortMode => m_TransparencySortMode;
        internal Vector3 transparencySortAxis => m_TransparencySortAxis;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
                ResourceReloader.TryReloadAllNullIn(m_PostProcessData, UniversalRenderPipelineAsset.packagePath);
            }
#endif
            return new Renderer2D(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            OnEnableInEditor();
#endif

            for (var i = 0; i < m_LightBlendStyles.Length; ++i)
            {
                m_LightBlendStyles[i].renderTargetHandle.Init($"_ShapeLightTexture{i}");
            }

            normalsRenderTarget.Init("_NormalMap");
            shadowsRenderTarget.Init("_ShadowTex");

            const int totalMaterials = 256;
            if(shadowMaterials == null || shadowMaterials.Length == 0)
                shadowMaterials = new Material[totalMaterials];
            if(removeSelfShadowMaterials == null || removeSelfShadowMaterials.Length == 0)
                removeSelfShadowMaterials = new Material[totalMaterials];
        }

        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Material[] shadowMaterials { get; private set; }
        internal Material[] removeSelfShadowMaterials { get; private set; }

        internal RenderTargetHandle normalsRenderTarget;
        internal RenderTargetHandle shadowsRenderTarget;

        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }
    }
}
