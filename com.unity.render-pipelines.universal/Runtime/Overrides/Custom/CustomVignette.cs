using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Custom vignette")]
    public sealed class CustomVignette : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The radius, in pixels, of the inner area of the vignette.")]
        public FloatParameter VignetteRadius = new FloatParameter(0.0f);

        [Tooltip("The hardness of the transparency gradient.")]
        public ClampedFloatParameter VignetteGradientPower = new ClampedFloatParameter(1.0f, 1.0f, 128.0f);

        [Tooltip("The color of the vignette. The alpha component affects its transparency.")]
        public ColorParameter VignetteColor = new ColorParameter(Color.white, false, true, true);

        [Tooltip("The color of the vignette. The alpha component affects its transparency.")]
        public TextureParameter VignetteTexture = new TextureParameter(null);

        [Tooltip("The minimum alpha value of the texture required for the vignette to be rendered.")]
        public ClampedFloatParameter TextureAlphaClipThreshold = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public bool IsActive()
        {
            return active &&
                   VignetteRadius.value != 0.0f &&
                   VignetteColor.value.a != 0.0f &&
                   TextureAlphaClipThreshold.value != 1.0f;
        }

        public bool IsTileCompatible() => true;
    }
}
