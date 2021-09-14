using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Glitch distortion")]
    public sealed class GlitchDistortion : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The amount of horizontal displacement of every line, in screen pixels.")]
        public FloatParameter DisplacementLength = new FloatParameter(0.0f);

        [Header("Noise texture generation")]

        [Tooltip("The amount of horizontal lines to be displaced. This cannot be changed in runtime.")]
        public IntParameter NoiseResolution = new IntParameter(1080, false);

        [Tooltip("Point filter (0) will show horizontal lines as blocks, whereas bilinear (1) or trilinear (2) filter result in near lines to be displaced similarily, which looks more like waves.")]
        public ClampedIntParameter NoiseTextureFilterMode = new ClampedIntParameter(0, 0, 2);

        /// <summary>
        /// Gets the 1D texture used for calculating the noise values.
        /// </summary>
        public Texture2D NoiseTexture
        {
            get
            {
                return sm_noiseTexture;
            }
        }

        static private Texture2D sm_noiseTexture;

        public bool IsActive()
        {
            return active &&
                   DisplacementLength.value != 0f;
        }

        public bool IsTileCompatible() => true;

        protected override void OnEnable()
        {
            base.OnEnable();

            if(sm_noiseTexture == null)
            {
                sm_noiseTexture = new Texture2D(NoiseResolution.value, 1, TextureFormat.R8, false);
                sm_noiseTexture.filterMode = (FilterMode)NoiseTextureFilterMode.value;

                Color[] noiseTextureValues = new Color[sm_noiseTexture.width];

                for (int i = 0; i < noiseTextureValues.Length; ++i)
                {
                    noiseTextureValues[i].r = Random.value;
                }

                sm_noiseTexture.SetPixels(noiseTextureValues);
                sm_noiseTexture.Apply();
            }
        }
    }
}
