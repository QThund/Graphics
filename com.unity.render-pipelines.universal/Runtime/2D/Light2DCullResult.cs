using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LightStats
    {
        public int totalLights;
        public int totalNormalMapUsage;
        public int totalVolumetricUsage;
        public uint blendStylesUsed;
    }

    internal interface ILight2DCullResult
    {
        List<Light2D> visibleLights { get; }
        // CUSTOM CODE
        List<Light2D> visibleStaticLights { get; }
        List<Light2D> visibleNonStaticLights { get; }
        //
        LightStats GetLightStatsByLayer(int layer);
        bool IsSceneLit();
    }

    internal class Light2DCullResult : ILight2DCullResult
    {
        private List<Light2D> m_VisibleLights = new List<Light2D>();
        // CUSTOM CODE
        private List<Light2D> m_StaticVisibleLights = new List<Light2D>();
        private List<Light2D> m_NonStaticVisibleLights = new List<Light2D>();
        //
        public List<Light2D> visibleLights => m_VisibleLights;
        // CUSTOM CODE
        public List<Light2D> visibleStaticLights => m_StaticVisibleLights;
        public List<Light2D> visibleNonStaticLights => m_NonStaticVisibleLights;
        //

        public bool IsSceneLit()
        {
            if (visibleLights.Count > 0)
                return true;

            foreach (var light in Light2DManager.lights)
            {
                if (light.lightType == Light2D.LightType.Global)
                    return true;
            }

            return false;
        }
        public LightStats GetLightStatsByLayer(int layer)
        {
            var returnStats = new LightStats();
            foreach (var light in visibleLights)
            {
                if (!light.IsLitLayer(layer))
                    continue;

                returnStats.totalLights++;
                if (light.useNormalMap)
                    returnStats.totalNormalMapUsage++;
                if (light.volumeOpacity > 0)
                    returnStats.totalVolumetricUsage++;

                returnStats.blendStylesUsed |= (uint)(1 << light.blendStyleIndex);
            }

            return returnStats;
        }

        public void SetupCulling(ref ScriptableCullingParameters cullingParameters, Camera camera)
        {
            Profiler.BeginSample("Cull 2D Lights");
            m_VisibleLights.Clear();
            // CUSTOM CODE
            m_StaticVisibleLights.Clear();
            m_NonStaticVisibleLights.Clear();
            //
            foreach (var light in Light2DManager.lights)
            {
                if ((camera.cullingMask & (1 << light.gameObject.layer)) == 0)
                    continue;

#if UNITY_EDITOR
                if (!UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(light.gameObject, camera))
                    continue;
#endif

                if (light.lightType == Light2D.LightType.Global)
                {
                    m_VisibleLights.Add(light);
                    continue;
                }
                // CUSTOM CODE
                else if (light.intensity <= 0.0f)
                {
                    continue;
                }
                //

                Profiler.BeginSample("Test Planes");
                var position = light.boundingSphere.position;
                var culled = false;
                for (var i = 0; i < cullingParameters.cullingPlaneCount; ++i)
                {
                    var plane = cullingParameters.GetCullingPlane(i);
                    // most of the time is spent getting world position
                    var distance = math.dot(position, plane.normal) + plane.distance;
                    if (distance < -light.boundingSphere.radius)
                    {
                        culled = true;
                        break;
                    }
                }
                Profiler.EndSample();
                if (culled)
                    continue;

                m_VisibleLights.Add(light);
            }

            // must be sorted here because light order could change
            m_VisibleLights.Sort((l1, l2) => l1.lightOrder - l2.lightOrder);

            // CUSTOM CODE
            // Splitting after sorting makes sure these other lists are sorted too
            for (int i = 0; i < m_VisibleLights.Count; ++i)
            {
                if (m_VisibleLights[i].gameObject.isStatic)
                {
                    m_StaticVisibleLights.Add(m_VisibleLights[i]);
                }
                else
                {
                    m_NonStaticVisibleLights.Add(m_VisibleLights[i]);
                }
            }
            //

            Profiler.EndSample();
        }
    }
}
