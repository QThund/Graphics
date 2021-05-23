using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Screen border")]
    public sealed class ScreenBorder : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The width, in pixels, of the border.")]
        public FloatParameter BorderWidth = new FloatParameter(0f);

        [Tooltip("The hardness of the transparency gradient.")]
        public ClampedFloatParameter BorderGradientPower = new ClampedFloatParameter(1.0f, 1.0f, 256.0f);

        [Tooltip("The color of the border. The alpha component affects its transparency.")]
        public ColorParameter BorderColor = new ColorParameter(Color.white, true, false, true);

        public bool IsActive()
        {
            return BorderWidth.value != 0f
                || BorderColor.value.a != 0f;
        }

        public bool IsTileCompatible() => true;
    }
}
