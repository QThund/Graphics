using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Gaussian blur")]
    public sealed class GaussianBlur : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The separation among samples. The greater the blurrier.")]
        public ClampedFloatParameter BlurSize = new ClampedFloatParameter(0.0f, 0.0f, 0.5f);

        [Tooltip("The amount of times the texture will be sampled.")]
        public FloatParameter Samples = new FloatParameter(0.0f);

        //[Tooltip(".")]
        //public BoolParameter Gauss = new BoolParameter(false);

        //[Tooltip(".")]
        //public ClampedFloatParameter StandardDeviation = new ClampedFloatParameter(0.0f, 0.0f, 0.3f);

        public bool IsActive()
        {
            return active &&
                   (BlurSize.value != 0.0f || BlurSize.value != 0.0f);
        }

        public bool IsTileCompatible() => true;
    }
}
