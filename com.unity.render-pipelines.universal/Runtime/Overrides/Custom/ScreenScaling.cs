using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Screen scaling")]
    public sealed class ScreenScaling : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The scale of the screen.")]
        public Vector2Parameter Scale = new Vector2Parameter(Vector2.one);

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
                   (Scale.value.x != 1.0f || Scale.value.y != 1.0f);
        }

        public bool IsTileCompatible() => true;
    }
}
