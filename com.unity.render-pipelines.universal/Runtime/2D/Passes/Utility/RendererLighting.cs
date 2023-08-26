using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class RendererLighting
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Clear Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        private static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";
        private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";
        private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";
        private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";
        // CUSTOM CODE
        private static readonly string k_UseVolumeTextures = "USE_VOLUME_TEXTURES";
        private static readonly string k_UseDithering = "USE_DITHERING";
        //
        private static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";

        private static readonly string[] k_UseBlendStyleKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0", "USE_SHAPE_LIGHT_TYPE_1", "USE_SHAPE_LIGHT_TYPE_2", "USE_SHAPE_LIGHT_TYPE_3"
        };

        private static readonly int[] k_BlendFactorsPropIDs =
        {
            Shader.PropertyToID("_ShapeLightBlendFactors0"),
            Shader.PropertyToID("_ShapeLightBlendFactors1"),
            Shader.PropertyToID("_ShapeLightBlendFactors2"),
            Shader.PropertyToID("_ShapeLightBlendFactors3")
        };

        private static readonly int[] k_MaskFilterPropIDs =
        {
            Shader.PropertyToID("_ShapeLightMaskFilter0"),
            Shader.PropertyToID("_ShapeLightMaskFilter1"),
            Shader.PropertyToID("_ShapeLightMaskFilter2"),
            Shader.PropertyToID("_ShapeLightMaskFilter3")
        };

        private static readonly int[] k_InvertedFilterPropIDs =
        {
            Shader.PropertyToID("_ShapeLightInvertedFilter0"),
            Shader.PropertyToID("_ShapeLightInvertedFilter1"),
            Shader.PropertyToID("_ShapeLightInvertedFilter2"),
            Shader.PropertyToID("_ShapeLightInvertedFilter3")
        };

        private static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;
        private static bool s_HasSetupRenderTextureFormatToUse;

        private static readonly int k_SrcBlendID = Shader.PropertyToID("_SrcBlend");
        private static readonly int k_DstBlendID = Shader.PropertyToID("_DstBlend");
        private static readonly int k_FalloffIntensityID = Shader.PropertyToID("_FalloffIntensity");
        private static readonly int k_FalloffDistanceID = Shader.PropertyToID("_FalloffDistance");
        private static readonly int k_FalloffOffsetID = Shader.PropertyToID("_FalloffOffset");
        private static readonly int k_LightColorID = Shader.PropertyToID("_LightColor");
        private static readonly int k_VolumeOpacityID = Shader.PropertyToID("_VolumeOpacity");
        // CUSTOM CODE
        private const int VOLUME_TEXTURE_COUNT = 4;
        private static readonly int k_VolumeTextureCount = Shader.PropertyToID("_VolumeTextureCount");
        private static readonly int[] k_VolumeTextureIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0"),
                                                                      Shader.PropertyToID("_VolumeTexture1"),
                                                                      Shader.PropertyToID("_VolumeTexture2"),
                                                                      Shader.PropertyToID("_VolumeTexture3")};
        private static readonly int[] k_VolumeTexturePowerIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0Power"),
                                                                           Shader.PropertyToID("_VolumeTexture1Power"),
                                                                           Shader.PropertyToID("_VolumeTexture2Power"),
                                                                           Shader.PropertyToID("_VolumeTexture3Power")};
        private static readonly int[] k_VolumeTextureScaleIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0Scale"),
                                                                           Shader.PropertyToID("_VolumeTexture1Scale"),
                                                                           Shader.PropertyToID("_VolumeTexture2Scale"),
                                                                           Shader.PropertyToID("_VolumeTexture3Scale")};
        private static readonly int[] k_VolumeTextureTimeScaleIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0TimeScale"),
                                                                               Shader.PropertyToID("_VolumeTexture1TimeScale"),
                                                                               Shader.PropertyToID("_VolumeTexture2TimeScale"),
                                                                               Shader.PropertyToID("_VolumeTexture3TimeScale")};
        private static readonly int[] k_VolumeTextureDirectionIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0Direction"),
                                                                               Shader.PropertyToID("_VolumeTexture1Direction"),
                                                                               Shader.PropertyToID("_VolumeTexture2Direction"),
                                                                               Shader.PropertyToID("_VolumeTexture3Direction")};
        private static readonly int[] k_VolumeTextureAlphaMultiplierIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0AlphaMultiplier"),
                                                                                     Shader.PropertyToID("_VolumeTexture1AlphaMultiplier"),
                                                                                     Shader.PropertyToID("_VolumeTexture2AlphaMultiplier"),
                                                                                     Shader.PropertyToID("_VolumeTexture3AlphaMultiplier")};
        private static readonly int[] k_VolumeTextureAspectRatioIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0AspectRatio"),
                                                                                 Shader.PropertyToID("_VolumeTexture1AspectRatio"),
                                                                                 Shader.PropertyToID("_VolumeTexture2AspectRatio"),
                                                                                 Shader.PropertyToID("_VolumeTexture3AspectRatio")};
        private static readonly int[] k_VolumeTextureIsAdditiveIDs = new int[]{ Shader.PropertyToID("_VolumeTexture0IsAdditive"),
                                                                                Shader.PropertyToID("_VolumeTexture1IsAdditive"),
                                                                                Shader.PropertyToID("_VolumeTexture2IsAdditive"),
                                                                                Shader.PropertyToID("_VolumeTexture3IsAdditive")};
        private static readonly int k_IsDitheringEnabledID = Shader.PropertyToID("_IsDitheringEnabled");
        private static readonly int k_DitheringTextureID = Shader.PropertyToID("_DitheringTexture");
        private static readonly int k_CachedLightTextureID = Shader.PropertyToID("_LightTexture");
        private static readonly int k_MaximumColorChannelValuesID = Shader.PropertyToID("_MaximumColorChannelValues");
        private static readonly int k_BlitSourceTexID = Shader.PropertyToID("_SourceTex");
        private static readonly int k_CachedLightTextureColorID = Shader.PropertyToID("_Color");
        //
        private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");
        private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");
        private static readonly int k_LightPositionID = Shader.PropertyToID("_LightPosition");
        private static readonly int k_LightInvMatrixID = Shader.PropertyToID("_LightInvMatrix");
        private static readonly int k_LightNoRotInvMatrixID = Shader.PropertyToID("_LightNoRotInvMatrix");
        private static readonly int k_InnerRadiusMultID = Shader.PropertyToID("_InnerRadiusMult");
        private static readonly int k_OuterAngleID = Shader.PropertyToID("_OuterAngle");
        private static readonly int k_InnerAngleMultID = Shader.PropertyToID("_InnerAngleMult");
        private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");
        private static readonly int k_IsFullSpotlightID = Shader.PropertyToID("_IsFullSpotlight");
        private static readonly int k_LightZDistanceID = Shader.PropertyToID("_LightZDistance");
        private static readonly int k_PointLightCookieTexID = Shader.PropertyToID("_PointLightCookieTex");

        // CUSTOM CODE
        private static Mesh m_quadMesh;
        private static Material m_cachedLightTextureMaterial;
        private static Material m_blitLightTextureMaterial;

        // Gets a mesh made of 6 vertices (position, normal and texture coordinates) and 2 triangles, forming a quad
        private static Mesh GetQuadMesh()
        {
            if(m_quadMesh == null)
            {
                m_quadMesh = new Mesh();

                float width = 1.0f;
                float height = 1.0f;
                Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);

                Vector3[] vertices = new Vector3[4]
                                        {
                                            new Vector3(0, 0, 0) - halfSize,
                                            new Vector3(width, 0, 0) - halfSize,
                                            new Vector3(0, height, 0) - halfSize,
                                            new Vector3(width, height, 0) - halfSize
                                        };
                m_quadMesh.vertices = vertices;

                int[] tris = new int[6]
                                    {
                                        // lower left triangle
                                        0, 2, 1,
                                        // upper right triangle
                                        2, 3, 1
                                    };
                m_quadMesh.triangles = tris;

                Vector3[] normals = new Vector3[4]
                                        {
                                            -Vector3.forward,
                                            -Vector3.forward,
                                            -Vector3.forward,
                                            -Vector3.forward
                                        };
                m_quadMesh.normals = normals;

                Vector2[] uv = new Vector2[4]
                                    {
                                        new Vector2(0, 0),
                                        new Vector2(1, 0),
                                        new Vector2(0, 1),
                                        new Vector2(1, 1)
                                    };
                m_quadMesh.uv = uv;
            }

            return m_quadMesh;
        }

        // Gets the material used when drawing quads to write the contents of the cached light textures on the temporary light texture (quads have position, orientation and size)
        private static Material GetCachedLightTextureMaterial()
        {
            if(m_cachedLightTextureMaterial == null)
            {
                m_cachedLightTextureMaterial = new Material(Shader.Find("2D/S_CachedLightTextureQuad"));
            }

            return m_cachedLightTextureMaterial;
        }

        // Gets the material used when blitting the content of the temporary light texture to the external render texture
        private static Material GetBlitLightTextureMaterial()
        {
            if (m_blitLightTextureMaterial == null)
            {
                m_blitLightTextureMaterial = new Material(Shader.Find("2D/S_BlitLightTextureResultProcessed"));
            }

            return m_blitLightTextureMaterial;
        }
        //

        private static GraphicsFormat GetRenderTextureFormat()
        {
            if (!s_HasSetupRenderTextureFormatToUse)
            {
                if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                    s_RenderTextureFormatToUse = GraphicsFormat.B10G11R11_UFloatPack32;
                else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
                    s_RenderTextureFormatToUse = GraphicsFormat.R16G16B16A16_SFloat;

                s_HasSetupRenderTextureFormatToUse = true;
            }

            return s_RenderTextureFormatToUse;
        }

        public static void CreateNormalMapRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd)
        {
            var descriptor = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);
            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
            descriptor.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(pass.rendererData.normalsRenderTarget.id, descriptor, FilterMode.Bilinear);
        }

        public static void CreateBlendStyleRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int blendStyleIndex)
        {
            var renderTextureScale = Mathf.Clamp(pass.rendererData.lightBlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1.0f);
            var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
            var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

            var descriptor = new RenderTextureDescriptor(width, height);
            descriptor.graphicsFormat = GetRenderTextureFormat();
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            ref var blendStyle = ref pass.rendererData.lightBlendStyles[blendStyleIndex];
            cmd.GetTemporaryRT(blendStyle.renderTargetHandle.id, descriptor, FilterMode.Bilinear);
            blendStyle.hasRenderTarget = true;
            blendStyle.isDirty = true;
        }

        public static void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
        {
            var keyword = k_UseBlendStyleKeywords[blendStyleIndex];

            if (enabled)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        public static void ReleaseRenderTextures(this IRenderPass2D pass, CommandBuffer cmd)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                if (!pass.rendererData.lightBlendStyles[i].hasRenderTarget)
                    continue;

                pass.rendererData.lightBlendStyles[i].hasRenderTarget = false;
                cmd.ReleaseTemporaryRT(pass.rendererData.lightBlendStyles[i].renderTargetHandle.id);
            }

            cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTarget.id);
            cmd.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
        }


        private static bool RenderLightSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, bool rtNeedsClear, Color clearColor, List<Light2D> lights
            // CUSTOM CODE
            , out bool hasRenderedShadows
            //
            )
        {
            // CUSTOM CODE
            hasRenderedShadows = false;
            //
            var renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null &&
                    light.lightType != Light2D.LightType.Global &&
                    light.blendStyleIndex == blendStyleIndex &&
                    light.IsLitLayer(layerToRender))
                {
                    // Render light
                    var lightMaterial = pass.rendererData.GetLightMaterial(light, false);
                    if (lightMaterial == null)
                        continue;

                    var lightMesh = light.lightMesh;
                    if (lightMesh == null)
                        continue;

                    // CUSTOM CODE
                    if(pass.rendererData.Is2DShadowsEnabled)
                    {
                        hasRenderedShadows |=
                    //
                            ShadowRendering.RenderShadows(pass, renderingData, cmd, layerToRender, light, light.shadowIntensity, renderTexture, renderTexture);
                    // CUSTOM CODE
                    }
                    //

                    if (!renderedAnyLight && rtNeedsClear)
                    {
                        cmd.ClearRenderTarget(false, true, clearColor);
                    }

                    renderedAnyLight = true;

                    if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                        cmd.SetGlobalTexture(k_CookieTexID, light.lightCookieSprite.texture);

                    cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
                    cmd.SetGlobalFloat(k_FalloffDistanceID, light.shapeLightFalloffSize);
                    cmd.SetGlobalVector(k_FalloffOffsetID, light.shapeLightFalloffOffset);
                    cmd.SetGlobalColor(k_LightColorID, light.intensity * light.color);
                    cmd.SetGlobalFloat(k_VolumeOpacityID, light.volumeOpacity);

                    if (light.useNormalMap || light.lightType == Light2D.LightType.Point)
                        SetPointLightShaderGlobals(cmd, light);

                    // CUSTOM CODE
                    int volumeTextureCount = Mathf.Min(VOLUME_TEXTURE_COUNT, light.volumeTextures.Length);
                    cmd.SetGlobalFloat(k_VolumeTextureCount, volumeTextureCount);

                    for (int j = 0; j < volumeTextureCount; ++j)
                    {
                        cmd.SetGlobalTexture(k_VolumeTextureIDs[j], light.volumeTextures[j].Texture);
                        cmd.SetGlobalVector(k_VolumeTextureDirectionIDs[j], light.volumeTextures[j].Direction);
                        cmd.SetGlobalFloat(k_VolumeTexturePowerIDs[j], light.volumeTextures[j].Power);
                        cmd.SetGlobalFloat(k_VolumeTextureScaleIDs[j], light.volumeTextures[j].Scale);
                        cmd.SetGlobalFloat(k_VolumeTextureTimeScaleIDs[j], light.volumeTextures[j].TimeScale);
                        cmd.SetGlobalFloat(k_VolumeTextureAspectRatioIDs[j], light.volumeTextures[j].AspectRatio);
                        cmd.SetGlobalFloat(k_VolumeTextureAlphaMultiplierIDs[j], light.volumeTextures[j].AlphaMultiplier);
                        cmd.SetGlobalFloat(k_VolumeTextureIsAdditiveIDs[j], light.volumeTextures[j].IsAdditive ? 1.0f : 0.0f);
                    }
                    //

                    // Light code could be combined...
                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                    {
                        cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightMaterial);
                    }
                    else if (light.lightType == Light2D.LightType.Point)
                    {
                        var scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                        var matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                        cmd.DrawMesh(lightMesh, matrix, lightMaterial);
                    }
                }
            }

            // If no lights were rendered, just clear the RenderTarget if needed
            if (!renderedAnyLight && rtNeedsClear)
            {
                cmd.ClearRenderTarget(false, true, clearColor);
            }

            return renderedAnyLight;
        }

        private static void RenderLightVolumeSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture, List<Light2D> lights)
        {
            if (lights.Count > 0)
            {
                for (var i = 0; i < lights.Count; i++)
                {
                    var light = lights[i];

                    // CUSTOM CODE
                    cmd.SetGlobalFloat(k_IsDitheringEnabledID, light.isDitheringEnabled ? 1.0f : 0.0f);

                    if (light.isDitheringEnabled)
                    {
                        cmd.SetGlobalTexture(k_DitheringTextureID, light.ditheringTexture);
                    }
                    //

                    var topMostLayer = light.GetTopMostLitLayer();
                    if (layerToRender == topMostLayer)
                    {
                        if (light != null && light.lightType != Light2D.LightType.Global && light.volumeOpacity > 0.0f && light.blendStyleIndex == blendStyleIndex && light.IsLitLayer(layerToRender))
                        {
                            var lightVolumeMaterial = pass.rendererData.GetLightMaterial(light, true);
                            if (lightVolumeMaterial != null)
                            {
                                var lightMesh = light.lightMesh;
                                if (lightMesh != null)
                                {
                                    // CUSTOM CODE
                                    if (pass.rendererData.Is2DShadowsEnabled)
                                    {
                                    //
                                        ShadowRendering.RenderShadows(pass, renderingData, cmd, layerToRender, light, light.shadowVolumeIntensity, renderTexture, depthTexture);
                                    // CUSTOM CODE
                                    }
                                    //

                                    if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                                        cmd.SetGlobalTexture(k_CookieTexID, light.lightCookieSprite.texture);

                                    cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
                                    cmd.SetGlobalFloat(k_FalloffDistanceID, light.shapeLightFalloffSize);
                                    cmd.SetGlobalVector(k_FalloffOffsetID, light.shapeLightFalloffOffset);
                                    cmd.SetGlobalColor(k_LightColorID, light.intensity * light.color);
                                    cmd.SetGlobalFloat(k_VolumeOpacityID, light.volumeOpacity);
                                    
                                    // Is this needed
                                    if (light.useNormalMap || light.lightType == Light2D.LightType.Point)
                                        SetPointLightShaderGlobals(cmd, light);

                                    // CUSTOM CODE
                                    int volumeTextureCount = Mathf.Min(VOLUME_TEXTURE_COUNT, light.volumeTextures.Length);
                                    cmd.SetGlobalFloat(k_VolumeTextureCount, volumeTextureCount);

                                    for (int j = 0; j < volumeTextureCount; ++j)
                                    {
                                        cmd.SetGlobalTexture(k_VolumeTextureIDs[j], light.volumeTextures[j].Texture);
                                        cmd.SetGlobalVector(k_VolumeTextureDirectionIDs[j], light.volumeTextures[j].Direction);
                                        cmd.SetGlobalFloat(k_VolumeTexturePowerIDs[j], light.volumeTextures[j].Power);
                                        cmd.SetGlobalFloat(k_VolumeTextureScaleIDs[j], light.volumeTextures[j].Scale);
                                        cmd.SetGlobalFloat(k_VolumeTextureTimeScaleIDs[j], light.volumeTextures[j].TimeScale);
                                        cmd.SetGlobalFloat(k_VolumeTextureAspectRatioIDs[j], light.volumeTextures[j].AspectRatio);
                                        cmd.SetGlobalFloat(k_VolumeTextureAlphaMultiplierIDs[j], light.volumeTextures[j].AlphaMultiplier);
                                        cmd.SetGlobalFloat(k_VolumeTextureIsAdditiveIDs[j], light.volumeTextures[j].IsAdditive ? 1.0f : 0.0f);
                                    }
                                    //

                                    // Could be combined...
                                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                                    {
                                        cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightVolumeMaterial);
                                    }
                                    else if (light.lightType == Light2D.LightType.Point)
                                    {
                                        var scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                                        var matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                        cmd.DrawMesh(lightMesh, matrix, lightVolumeMaterial);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void SetShapeLightShaderGlobals(this IRenderPass2D pass, CommandBuffer cmd)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
            {
                var blendStyle = pass.rendererData.lightBlendStyles[i];
                if (i >= k_BlendFactorsPropIDs.Length)
                    break;

                cmd.SetGlobalVector(k_BlendFactorsPropIDs[i], blendStyle.blendFactors);
                cmd.SetGlobalVector(k_MaskFilterPropIDs[i], blendStyle.maskTextureChannelFilter.mask);
                cmd.SetGlobalVector(k_InvertedFilterPropIDs[i], blendStyle.maskTextureChannelFilter.inverted);
            }

            cmd.SetGlobalTexture(k_FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
        }

        private static float GetNormalizedInnerRadius(Light2D light)
        {
            return light.pointLightInnerRadius / light.pointLightOuterRadius;
        }

        private static float GetNormalizedAngle(float angle)
        {
            return (angle / 360.0f);
        }

        private static void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix, bool includeRotation)
        {
            var outerRadius = light.pointLightOuterRadius;
            var lightScale = Vector3.one;
            var outerRadiusScale = new Vector3(lightScale.x * outerRadius, lightScale.y * outerRadius, lightScale.z * outerRadius);

            var transform = light.transform;
            var rotation = includeRotation ? transform.rotation : Quaternion.identity;

            var scaledLightMat = Matrix4x4.TRS(transform.position, rotation, outerRadiusScale);
            retMatrix = Matrix4x4.Inverse(scaledLightMat);
        }

        private static void SetPointLightShaderGlobals(CommandBuffer cmd, Light2D light)
        {
            // This is used for the lookup texture
            GetScaledLightInvMatrix(light, out var lightInverseMatrix, true);
            GetScaledLightInvMatrix(light, out var lightNoRotInverseMatrix, false);

            var innerRadius = GetNormalizedInnerRadius(light);
            var innerAngle = GetNormalizedAngle(light.pointLightInnerAngle);
            var outerAngle = GetNormalizedAngle(light.pointLightOuterAngle);
            var innerRadiusMult = 1 / (1 - innerRadius);

            cmd.SetGlobalVector(k_LightPositionID, light.transform.position);
            cmd.SetGlobalMatrix(k_LightInvMatrixID, lightInverseMatrix);
            cmd.SetGlobalMatrix(k_LightNoRotInvMatrixID, lightNoRotInverseMatrix);
            cmd.SetGlobalFloat(k_InnerRadiusMultID, innerRadiusMult);
            cmd.SetGlobalFloat(k_OuterAngleID, outerAngle);
            cmd.SetGlobalFloat(k_InnerAngleMultID, 1 / (outerAngle - innerAngle));
            cmd.SetGlobalTexture(k_LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
            cmd.SetGlobalTexture(k_FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
            cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
            cmd.SetGlobalFloat(k_IsFullSpotlightID, innerAngle == 1 ? 1.0f : 0.0f);

            cmd.SetGlobalFloat(k_LightZDistanceID, light.pointLightDistance);

            if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                cmd.SetGlobalTexture(k_PointLightCookieTexID, light.lightCookieSprite.texture);
        }

        public static void ClearDirtyLighting(this IRenderPass2D pass, CommandBuffer cmd, uint blendStylesUsed)
        {
            for (var i = 0; i < pass.rendererData.lightBlendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                if (!pass.rendererData.lightBlendStyles[i].isDirty)
                    continue;

                cmd.SetRenderTarget(pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier());
                cmd.ClearRenderTarget(false, true, Color.black);
                pass.rendererData.lightBlendStyles[i].isDirty = false;
            }
        }

        public static void RenderNormals(this IRenderPass2D pass, ScriptableRenderContext context, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings, RenderTargetIdentifier depthTarget)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.SetRenderTarget(pass.rendererData.normalsRenderTarget.Identifier(), depthTarget);
                cmd.ClearRenderTarget(true, true, k_NormalClearColor);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
            context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }

        public static void RenderLights(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, uint blendStylesUsed
            // CUSTOM CODE
            , out bool hasRenderedShadows
            //
            )
        {
            // CUSTOM CODE
            hasRenderedShadows = false;
            bool hasRenderedShadowsForBlendingStyle = false;
            //
            var blendStyles = pass.rendererData.lightBlendStyles;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                // CUSTOM CODE
#if UNITY_EDITOR

                SortingLayer[] layers = Light2DManager.GetCachedSortingLayer();
                string layerName = layerToRender.ToString();

                for(int l = 0; l < layers.Length; ++l)
                {
                    if(layers[l].id == layerToRender)
                    {
                        layerName = layers[l].name;
                    }
                }

                string sampleName = "BlendStyle:" + blendStyles[i].name + " Layer:" + layerName;
#else
                string sampleName = "BlendStyle:" + blendStyles[i].name + " Layer:" + layerToRender;
#endif

                cmd.BeginSample(sampleName);
                //

                var rtID = pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier();
                cmd.SetRenderTarget(rtID);

                var rtDirty = false;
                if (!Light2DManager.GetGlobalColor(layerToRender, i, out var clearColor))
                    clearColor = Color.black;
                else
                    rtDirty = true;

                // CUSTOM CODE
                // It clears the light texture when changing to a new blend style or sorting layer
                cmd.ClearRenderTarget(false, true, clearColor);
                
                // Light textures caching
                // It draws a quad per cached light texture, according to its transformations at the moment the texture was captured
                bool thereAreCachedLightTextures = false;
                List<Renderer2DData.CachedLightTextureData> cachedLightTextures = pass.rendererData.CachedLightTextures;

                if (pass.rendererData.IsLightTextureCachingEnabled && cachedLightTextures != null)
                {
                    Color debugColor = pass.rendererData.IsLightTextureCachingDebugModeEnabled ? Color.green
                                                                                               : Color.clear;
                    GetCachedLightTextureMaterial().SetColor(k_CachedLightTextureColorID, debugColor);
                    Vector2 cameraViewportSize = new Vector2(renderingData.cameraData.camera.aspect * renderingData.cameraData.camera.orthographicSize * 2.0f, renderingData.cameraData.camera.orthographicSize * 2.0f);
                    Rect cameraRect = new Rect((Vector2)renderingData.cameraData.camera.transform.position - cameraViewportSize * 0.5f, cameraViewportSize);

                    for (int j = 0; j < cachedLightTextures.Count; ++j)
                    {
                        // It only draws the textures that correspond to the current blend style and sorting layer
                        if(cachedLightTextures[j].SortingLayerId == layerToRender &&
                           cachedLightTextures[j].BlendStyle == i &&
                           cameraRect.Overlaps(cachedLightTextures[j].WorldRect))
                        {
                            thereAreCachedLightTextures = true;
                            cmd.SetGlobalTexture(k_CachedLightTextureID, new RenderTargetIdentifier(cachedLightTextures[j].Texture));
                            cmd.SetGlobalFloat(k_MaximumColorChannelValuesID, cachedLightTextures[j].MaximumLightAccumulationPerColorChannel);
                            cmd.DrawMesh(GetQuadMesh(), cachedLightTextures[j].WorldMatrix, GetCachedLightTextureMaterial());
                        }
                    }
                }

                // If there are no cached light textures for the current blend style and sorting layer, then draw the static lights as usual
                if(!thereAreCachedLightTextures)
                {
                    // Renders all the static lights
                    rtDirty |= RenderLightSet(
                        pass, renderingData,
                        i,
                        cmd,
                        layerToRender,
                        rtID,
                        false, // False so the light texture is not cleared
                        clearColor,
                        pass.rendererData.lightCullResult.visibleStaticLights
                        , out hasRenderedShadowsForBlendingStyle
                    );

#if UNITY_EDITOR
                    // Light textures capturing
                    if(pass.rendererData.IsLightTextureCapturingEnabled)
                    {
                        // Caches the resulting texture (before blurring)

                        // Note: It's not possible to just copy the render texture (CopyTexture) because its format is R11G11B10 and format of the potentially stored textures (PNG) uses RGBA32. When several lights overlap, the resulting values of the color channels (RGBA) for each pixel may be greater than 1. When the texture is converted from R11G11B10 to RGBA32 format, color channels are trimmed, leading to darker colors. To avoid that, colors are normalized before they are converted, and denormalized when they are read back. This value establishes the maximum value each color channel may have without being trimmed, the value that will be equivalent to 1 in the normalized texture. Unfortunatelly, the higher the value is, the lower the quality of the texture will be, as we are reducing the range of values that each channel can represent (color banding may appear)..
                        if ((blendStylesUsed & (1u << pass.rendererData.LightTextureBlendStyleToCapture)) != 0 && // Does the set of blend styles of the current iteration include the blend style to be captured?
                            layerToRender == pass.rendererData.LightTextureSortingLayerToCapture)
                        {
                            // Note: Yes, this is a Blit, but cmd.Blit takes a different texture when using rtID, and I have no explanation for that
                            cmd.SetRenderTarget(pass.rendererData.CachedLightsRenderTexture);
                            cmd.SetGlobalTexture(k_BlitSourceTexID, rtID);
                            cmd.SetGlobalFloat(k_MaximumColorChannelValuesID, pass.rendererData.MaximumLightAccumulationPerColorChannel);
                            Camera mainCamera = renderingData.cameraData.camera;
                            Vector2 quadSize = new Vector2(mainCamera.aspect * mainCamera.orthographicSize, mainCamera.orthographicSize) * 2.0f;
                            cmd.DrawMesh(GetQuadMesh(), Matrix4x4.TRS((Vector2)mainCamera.transform.position, mainCamera.transform.rotation, new Vector3(quadSize.x, quadSize.y, 1.0f)), GetBlitLightTextureMaterial());
                            cmd.SetRenderTarget(RenderTexture.active);
                        }
                    }
#endif
                }

                // Renders all the non-static lights
                //

                rtDirty |= RenderLightSet(
                    pass, renderingData,
                    i,
                    cmd,
                    layerToRender,
                    rtID,
                    // CUSTOM CODE
                    false, // False so the light texture is not cleared
                    //
                    clearColor,
                    // CUSTOM CODE
                    pass.rendererData.lightCullResult.visibleNonStaticLights
                    , out hasRenderedShadowsForBlendingStyle
                    //
                );

                pass.rendererData.lightBlendStyles[i].isDirty = rtDirty;

                // CUSTOM CODE
                hasRenderedShadows |= hasRenderedShadowsForBlendingStyle;

                cmd.EndSample(sampleName);
                //
            }
        }

        public static void RenderLightVolumes(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTarget, RenderTargetIdentifier depthTarget, uint blendStylesUsed)
        {
            var blendStyles = pass.rendererData.lightBlendStyles;

            for (var i = 0; i < blendStyles.Length; ++i)
            {
                if ((blendStylesUsed & (uint)(1 << i)) == 0)
                    continue;

                // CUSTOM CODE
#if UNITY_EDITOR

                SortingLayer[] layers = Light2DManager.GetCachedSortingLayer();
                string layerName = layerToRender.ToString();

                for (int l = 0; l < layers.Length; ++l)
                {
                    if (layers[l].id == layerToRender)
                    {
                        layerName = layers[l].name;
                    }
                }

                string sampleName = "VOLUMES=BlendStyle:" + blendStyles[i].name + " Layer:" + layerName;
#else
                string sampleName = "VOLUMES=BlendStyle:" + blendStyles[i].name + " Layer:" + layerToRender;
#endif

                cmd.BeginSample(sampleName);
                //

                RenderLightVolumeSet(
                    pass, renderingData,
                    i,
                    cmd,
                    layerToRender,
                    renderTarget,
                    depthTarget,
                    pass.rendererData.lightCullResult.visibleLights
                );

                // CUSTOM CODE
                cmd.EndSample(sampleName);
                //
            }
        }

        private static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
        {
            material.SetFloat(k_SrcBlendID, (float)src);
            material.SetFloat(k_DstBlendID, (float)dst);
        }

        private static uint GetLightMaterialIndex(Light2D light, bool isVolume)
        {
            var isPoint = light.isPointLight;
            var bitIndex = 0;
            var volumeBit = isVolume ? 1u << bitIndex : 0u;
            bitIndex++;
            var shapeBit = !isPoint ? 1u << bitIndex : 0u;
            bitIndex++;
            var additiveBit = light.alphaBlendOnOverlap ? 0u : 1u << bitIndex;
            bitIndex++;
            var spriteBit = light.lightType == Light2D.LightType.Sprite ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointCookieBit = (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? 1u << bitIndex : 0u;
            bitIndex++;
            var pointFastQualityBit = (isPoint && light.pointLightQuality == Light2D.PointLightQuality.Fast) ? 1u << bitIndex : 0u;
            bitIndex++;
            var useNormalMap = light.useNormalMap ? 1u << bitIndex : 0u;

            // CUSTOM CODE
            bitIndex++;
            var useVolumeTextures = light.volumeTextures.Length > 0 ? 1u << bitIndex : 0u;
            bitIndex++;
            var useDithering = light.isDitheringEnabled ? 1u << bitIndex : 0u;
            //

            return pointFastQualityBit | pointCookieBit | spriteBit | additiveBit | shapeBit | volumeBit | useNormalMap
                // CUSTOM CODE
                | useVolumeTextures | useDithering
                //
                ;
        }

        private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            var isPoint = light.isPointLight;
            Material material;

            if (isVolume)
                material = CoreUtils.CreateEngineMaterial(isPoint ? rendererData.pointLightVolumeShader : rendererData.shapeLightVolumeShader);
            else
            {
                material = CoreUtils.CreateEngineMaterial(isPoint ? rendererData.pointLightShader : rendererData.shapeLightShader);

                if (!light.alphaBlendOnOverlap)
                {
                    SetBlendModes(material, BlendMode.One, BlendMode.One);
                    material.EnableKeyword(k_UseAdditiveBlendingKeyword);
                }
                else
                    SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
            }

            if (light.lightType == Light2D.LightType.Sprite)
                material.EnableKeyword(k_SpriteLightKeyword);

            if (isPoint && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                material.EnableKeyword(k_UsePointLightCookiesKeyword);

            if (isPoint && light.pointLightQuality == Light2D.PointLightQuality.Fast)
                material.EnableKeyword(k_LightQualityFastKeyword);

            if (light.useNormalMap)
                material.EnableKeyword(k_UseNormalMap);

            // CUSTOM CODE
            if (light.volumeTextures.Length > 0)
                material.EnableKeyword(k_UseVolumeTextures);

            if(light.isDitheringEnabled)
                material.EnableKeyword(k_UseDithering);
            //

            return material;
        }

        private static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume)
        {
            var materialIndex = GetLightMaterialIndex(light, isVolume);

            if (!rendererData.lightMaterials.TryGetValue(materialIndex, out var material))
            {
                material = CreateLightMaterial(rendererData, light, isVolume);
                rendererData.lightMaterials[materialIndex] = material;
            }

            return material;
        }
    }
}
