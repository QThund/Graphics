using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Sun shafts")]
    public sealed class SunShafts : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The radius at which the effect begins.")]
        public FloatParameter RadialBlurRadius = new FloatParameter(0.1f);

        [Tooltip("The amount of times the texture will be sampled.")]
        public IntParameter RadialBlurSamples = new IntParameter(16);

        [Tooltip("The intensity of the effect.")]
        public FloatParameter RadialBlurAmount = new FloatParameter(1.0f);

        [Tooltip("The center of the effect, from which the shafts are casted.")]
        public Vector2Parameter RadialBlurCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("A multiplier applied to the opacity of the shafts.")]
        public FloatParameter ShaftsAlphaMultiplier = new FloatParameter(1.0f);

        [Tooltip("The separation among samples. The greater the blurrier.")]
        public FloatParameter GaussianBlurSize = new FloatParameter(0.005f);

        [Tooltip("The amount of times the texture will be sampled after radial blur.")]
        public FloatParameter GaussianBlurSamples = new FloatParameter(30.0f);

        [Tooltip(".")]
        public FloatParameter GaussianBlurStandardDeviation = new FloatParameter(0.032f);

        public bool IsActive()
        {
            return active &&
                   RadialBlurSamples.value > 0 &&
                   GaussianBlurSamples.value > 0 &&
                   ShaftsAlphaMultiplier.value > 0.0f;
        }

        public bool IsTileCompatible() => true;
    }
}
