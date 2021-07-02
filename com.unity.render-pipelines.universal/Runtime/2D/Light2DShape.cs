using System;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {
        [SerializeField] int                m_ShapeLightParametricSides         = 5;
        [SerializeField] float              m_ShapeLightParametricAngleOffset   = 0.0f;
        [SerializeField] float              m_ShapeLightParametricRadius        = 1.0f;
        [SerializeField] float              m_ShapeLightFalloffSize             = 0.50f;
        [SerializeField] Vector2            m_ShapeLightFalloffOffset           = Vector2.zero;
        [SerializeField] Vector3[]          m_ShapePath                         = null;

        // CUSTOM CODE
        [Serializable]
        public class VolumeTexture
        {
            public Texture2D Texture = null;
            public Vector2 Direction = Vector2.right;
            public float Power = 1.0f;
            public float Scale = 1.0f;
            public float AspectRatio = 1.0f;
            public float TimeScale = 1.0f;
            public float AlphaMultiplier = 1.0f;
            public bool IsAdditive = false;
        }

        [SerializeField] VolumeTexture[] m_VolumeTextures = new VolumeTexture[0];
        [SerializeField] bool m_IsDitheringEnabled = false;
        [SerializeField] Texture2D m_DitheringTexture = null;
        //

        float   m_PreviousShapeLightFalloffSize             = -1;
        int     m_PreviousShapeLightParametricSides         = -1;
        float   m_PreviousShapeLightParametricAngleOffset   = -1;
        float   m_PreviousShapeLightParametricRadius        = -1;


        public int              shapeLightParametricSides       => m_ShapeLightParametricSides;
        public float            shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        public float            shapeLightParametricRadius      => m_ShapeLightParametricRadius;
        public float            shapeLightFalloffSize           => m_ShapeLightFalloffSize;
        public Vector2          shapeLightFalloffOffset         => m_ShapeLightFalloffOffset;
        public Vector3[]        shapePath                       => m_ShapePath;
        // CUSTOM CODE
        public VolumeTexture[]  volumeTextures                  => m_VolumeTextures;
        public bool isDitheringEnabled                          => m_IsDitheringEnabled;
        public Texture2D ditheringTexture                       => m_DitheringTexture;
        //
    }
}
