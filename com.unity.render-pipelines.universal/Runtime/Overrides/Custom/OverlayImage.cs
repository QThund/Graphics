using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Overlay image")]
    public sealed class OverlayImage : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The power of the alpha component of the image.")]
        public FloatParameter AlphaPower = new FloatParameter(1.0f);

        [Tooltip("The offset to add to the alpha component of the image.")]
        public FloatParameter AlphaOffset = new FloatParameter(1.0f);

        [Tooltip("When enabled, the alpha component of the image is inverted after reading it.")]
        public BoolParameter InvertAlpha = new BoolParameter(false);

        [Tooltip("The color to add to the image.")]
        public ColorParameter ImageColor = new ColorParameter(Color.white, false, true, true);

        [Tooltip("The image.")]
        public TextureParameter Image = new TextureParameter(null);

        [Tooltip("The minimum alpha value of the image required for it to be rendered.")]
        public ClampedFloatParameter TextureAlphaMinimumClipThreshold = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        [Tooltip("The maximum alpha value of the image required for it to be rendered.")]
        public ClampedFloatParameter TextureAlphaMaximumClipThreshold = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public bool IsActive()
        {
            return active &&
                   ImageColor.value.a != 0.0f &&
                   TextureAlphaMinimumClipThreshold.value != 1.0f &&
                   TextureAlphaMaximumClipThreshold.value != 0.0f;
        }

        public bool IsTileCompatible() => true;
    }
}
