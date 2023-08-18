// CUSTOM CODE
using System.Collections.Generic;
using System.Linq;
//
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent transparencySortMode = EditorGUIUtility.TrTextContent("Transparency Sort Mode", "Default sorting mode used for transparent objects");
            public static readonly GUIContent transparencySortAxis = EditorGUIUtility.TrTextContent("Transparency Sort Axis", "Axis used for custom axis sorting mode");
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightBlendStyles = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent defaultMaterialType = EditorGUIUtility.TrTextContent("Default Material Type", "Material to use when adding new objects to a scene");
            public static readonly GUIContent defaultCustomMaterial = EditorGUIUtility.TrTextContent("Default Custom Material", "Material to use when adding new objects to a scene");

            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent renderTextureScale = EditorGUIUtility.TrTextContent("Render Texture Scale", "The resolution of the lighting buffer relative to the screen resolution. 1.0 means full screen size.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive = EditorGUIUtility.TrTextContent("Additive");
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Use Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");
            public static readonly GUIContent postProcessData = EditorGUIUtility.TrTextContent("Post-processing Data", "Resources (textures, shaders, etc.) required by post-processing effects.");

            // CUSTOM CODE
            public static readonly GUIContent renderTargets = EditorGUIUtility.TrTextContent("Additional render targets", "A list of render targets to be set when rendering 2D geometry, in the same order. The first texture must be accessed by the index 1 in the shaders, as the index 0 will be occupied by a default color texture.");
            public static readonly GUIContent enable2DShadows = EditorGUIUtility.TrTextContent("Enable 2D shadows", "When enabled, shadows casted by 2D lights will be calculated and rendered.");
            public static readonly GUIContent enable2DShadowBlurring = EditorGUIUtility.TrTextContent("Enable 2D shadow blurring", "When enabled, it applies gaussian blur to the 2D shadows texture calculated for every camera (if shadows are rendered by that camera).");
            public static readonly GUIContent shadowBlurBlitMaterial = EditorGUIUtility.TrTextContent("Shadow blur blit material", "The material to use in the blit operation when shadows are blurred.");
            public static readonly GUIContent enableLightTextureCaching = EditorGUIUtility.TrTextContent("Enable 2D light texture caching", "When enabled, light textures (used by the renderer to store the result of calculating 2D lights and shadows) will use cached textures previously generated.");
            public static readonly GUIContent enableDebugModeForLightTextureCaching = EditorGUIUtility.TrTextContent("Enable Debug mode for 2D light texture caching", "When enabled, light textures will be rendered using a highlight color.");
            public static readonly GUIContent enableLightTextureCapturing = EditorGUIUtility.TrTextContent("Enable 2D light texture capturing", "When enabled, the result of one of the light textures generated during the rendering process will be copied to an external render texture, every frame. This texture will contain only the lights marked as 'static' object, whose blend style and minimum target sorting layer match the arguments provided. Capturing only works in the editor.");
            public static readonly GUIContent lightTextureBlendStyleToCapture = EditorGUIUtility.TrTextContent("Light texture's blend style to capture", "The captured light texture will contain lights with this blend style only.");
            public static readonly GUIContent lightTextureSortingLayerToCapture = EditorGUIUtility.TrTextContent("Light texture's sorting layer to capture", "The captured light texture will contain lights with this sorting layer only.");
            public static readonly GUIContent maximumLightAccumulationPerColorChannel = EditorGUIUtility.TrTextContent("Maximum light accumulation per color channel", "When several lights overlap, the resulting values of the color channels (RGBA) for each pixel may be greater than 1. When the texture is converted from R11G11B10 to RGBA32 format, color channels are trimmed, leading to darker colors. To avoid that, colors are normalized before they are converted, and denormalized when they are read back. This value establishes the maximum value each color channel may have without being trimmed, the value that will be equivalent to 1 in the normalized texture. Unfortunatelly, the higher the value is, the lower the quality of the texture will be, as we are reducing the range of values that each channel can represent (color banding may appear).");
            public static readonly GUIContent cachedLightsRenderTexture = EditorGUIUtility.TrTextContent("Cached lights render texture", "The texture were the result of rendering the static lights will be copied to.");
            //
        }

        struct LightBlendStyleProps
        {
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty renderTextureScale;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }


        SerializedProperty m_TransparencySortMode;
        SerializedProperty m_TransparencySortAxis;
        SerializedProperty m_HDREmulationScale;
        SerializedProperty m_LightBlendStyles;
        LightBlendStyleProps[] m_LightBlendStylePropsArray;
        SerializedProperty m_UseDepthStencilBuffer;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_DefaultMaterialType;
        SerializedProperty m_DefaultCustomMaterial;

        // CUSTOM CODE
        SerializedProperty m_renderTargets;
        SerializedProperty m_enable2DShadows;
        SerializedProperty m_enable2DShadowBlurring;
        SerializedProperty m_shadowBlurBlitMaterial;
        SerializedProperty m_enableLightTextureCaching;
        SerializedProperty m_cachedLightsRenderTexture;
        SerializedProperty m_enableDebugModeForLightTextureCaching;
        SerializedProperty m_enableLightTextureCapturing;
        SerializedProperty m_lightTextureBlendStyleToCapture;
        SerializedProperty m_lightTextureSortingLayerToCapture;
        SerializedProperty m_maximumLightAccumulationPerColorChannel;

        int[] m_BlendStyleIndices;
        GUIContent[] m_BlendStyleNames;

        int[] m_SortingLayerIds;
        GUIContent[] m_SortingLayerNames;
        //

        Analytics.Renderer2DAnalytics m_Analytics = Analytics.Renderer2DAnalytics.instance;
        Renderer2DData m_Renderer2DData;
        bool m_WasModified;

        void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = m_Renderer2DData.GetInstanceID();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = 0;
                modifiedData.blending_modes_used = 0;
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }

        void OnEnable()
        {
            m_WasModified = false;
            m_Renderer2DData = (Renderer2DData)serializedObject.targetObject;

            m_TransparencySortMode = serializedObject.FindProperty("m_TransparencySortMode");
            m_TransparencySortAxis = serializedObject.FindProperty("m_TransparencySortAxis");
            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");

            int numBlendStyles = m_LightBlendStyles.arraySize;
            m_LightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                props.name = blendStyleProp.FindPropertyRelative("name");
                props.maskTextureChannel = blendStyleProp.FindPropertyRelative("maskTextureChannel");
                props.renderTextureScale = blendStyleProp.FindPropertyRelative("renderTextureScale");
                props.blendMode = blendStyleProp.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                    props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.modulate");
                if (props.blendFactorAdditive == null)
                    props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additve");
            }

            m_UseDepthStencilBuffer = serializedObject.FindProperty("m_UseDepthStencilBuffer");
            m_PostProcessData = serializedObject.FindProperty("m_PostProcessData");
            m_DefaultMaterialType = serializedObject.FindProperty("m_DefaultMaterialType");
            m_DefaultCustomMaterial = serializedObject.FindProperty("m_DefaultCustomMaterial");

            // CUSTOM CODE
            m_renderTargets = serializedObject.FindProperty("m_renderTargets");
            m_enable2DShadows = serializedObject.FindProperty("m_Enable2DShadows");
            m_enable2DShadowBlurring = serializedObject.FindProperty("m_Enable2DShadowBlurring");
            m_shadowBlurBlitMaterial = serializedObject.FindProperty("m_shadowBlurBlitMaterial");
            m_enableLightTextureCaching = serializedObject.FindProperty("m_EnableLightTextureCaching");
            m_cachedLightsRenderTexture = serializedObject.FindProperty("m_CachedLightsRenderTexture");
            m_enableDebugModeForLightTextureCaching = serializedObject.FindProperty("m_EnableDebugModeForLightTextureCaching");
            m_enableLightTextureCapturing = serializedObject.FindProperty("m_EnableLightTextureCapturing");
            m_lightTextureBlendStyleToCapture = serializedObject.FindProperty("m_LightTextureBlendStyleToCapture");
            m_lightTextureSortingLayerToCapture = serializedObject.FindProperty("m_LightTextureSortingLayerToCapture");
            m_maximumLightAccumulationPerColorChannel = serializedObject.FindProperty("m_MaximumLightAccumulationPerColorChannel");

            // Blend styles
            // Copied from Light2DEditor
            var blendStyleIndices = new List<int>();
            var blendStyleNames = new List<string>();

            var rendererData = Light2DEditorUtility.GetRenderer2DData();
            if (rendererData != null)
            {
                for (int i = 0; i < rendererData.lightBlendStyles.Length; ++i)
                {
                    blendStyleIndices.Add(i);

                    ref var blendStyle = ref rendererData.lightBlendStyles[i];
                    blendStyleNames.Add(blendStyle.name);
                }
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    blendStyleIndices.Add(i);
                    blendStyleNames.Add("Operation" + i);
                }
            }

            m_BlendStyleIndices = blendStyleIndices.ToArray();
            m_BlendStyleNames = blendStyleNames.Select(x => new GUIContent(x)).ToArray();

            // Sorting layers
            SortingLayer[] allSortingLayers = SortingLayer.layers;
            m_SortingLayerIds = new int[allSortingLayers.Length];
            m_SortingLayerNames = new GUIContent[allSortingLayers.Length];

            for(int i = 0; i < allSortingLayers.Length; ++i)
            {
                m_SortingLayerIds[i] = allSortingLayers[i].id;
                m_SortingLayerNames[i] = new GUIContent(allSortingLayers[i].name);
            }

            //
        }

        private void OnDestroy()
        {
            SendModifiedAnalytics(m_Analytics);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            
            EditorGUILayout.PropertyField(m_TransparencySortMode, Styles.transparencySortMode);
            if(m_TransparencySortMode.intValue == (int)TransparencySortMode.CustomAxis)
                EditorGUILayout.PropertyField(m_TransparencySortAxis, Styles.transparencySortAxis);

            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.LabelField(Styles.lightBlendStyles);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            int numBlendStyles = m_LightBlendStyles.arraySize;
            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];
                
                EditorGUILayout.BeginHorizontal();
                blendStyleProp.isExpanded = EditorGUILayout.Foldout(blendStyleProp.isExpanded, props.name.stringValue, true);
                EditorGUILayout.EndHorizontal();

                if (blendStyleProp.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(props.name, Styles.name);
                    EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                    EditorGUILayout.PropertyField(props.renderTextureScale, Styles.renderTextureScale);
                    EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                    if (props.blendMode.intValue == (int)Light2DBlendStyle.BlendMode.Custom)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(Styles.customBlendFactors, GUILayout.MaxWidth(200.0f));
                        EditorGUI.indentLevel--;

                        int oldIndentLevel = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;

                        EditorGUIUtility.labelWidth = 80.0f;
                        EditorGUILayout.PropertyField(props.blendFactorMultiplicative, Styles.blendFactorMultiplicative, GUILayout.MinWidth(110.0f));

                        GUILayout.Space(10.0f);

                        EditorGUIUtility.labelWidth = 50.0f;
                        EditorGUILayout.PropertyField(props.blendFactorAdditive, Styles.blendFactorAdditive, GUILayout.MinWidth(90.0f));

                        EditorGUIUtility.labelWidth = 0.0f;
                        EditorGUI.indentLevel = oldIndentLevel;
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
            }

            
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(m_UseDepthStencilBuffer, Styles.useDepthStencilBuffer);
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.postProcessData);

            EditorGUILayout.PropertyField(m_DefaultMaterialType, Styles.defaultMaterialType);
            if(m_DefaultMaterialType.intValue == (int)Renderer2DData.Renderer2DDefaultMaterialType.Custom)
                EditorGUILayout.PropertyField(m_DefaultCustomMaterial, Styles.defaultCustomMaterial);

            // CUSTOM CODE
            EditorGUILayout.PropertyField(m_renderTargets, Styles.renderTargets);
            EditorGUILayout.PropertyField(m_enable2DShadows, Styles.enable2DShadows);
            EditorGUILayout.PropertyField(m_enable2DShadowBlurring, Styles.enable2DShadowBlurring);
            EditorGUILayout.PropertyField(m_shadowBlurBlitMaterial, Styles.shadowBlurBlitMaterial);
            EditorGUILayout.PropertyField(m_enableLightTextureCaching, Styles.enableLightTextureCaching);
            EditorGUILayout.PropertyField(m_enableDebugModeForLightTextureCaching, Styles.enableDebugModeForLightTextureCaching);
            EditorGUILayout.PropertyField(m_enableLightTextureCapturing, Styles.enableLightTextureCapturing);

            m_lightTextureBlendStyleToCapture.intValue = EditorGUILayout.IntPopup(Styles.lightTextureBlendStyleToCapture, m_lightTextureBlendStyleToCapture.intValue, m_BlendStyleNames, m_BlendStyleIndices);
            m_lightTextureSortingLayerToCapture.intValue = EditorGUILayout.IntPopup(Styles.lightTextureSortingLayerToCapture, m_lightTextureSortingLayerToCapture.intValue, m_SortingLayerNames, m_SortingLayerIds);

            EditorGUILayout.PropertyField(m_maximumLightAccumulationPerColorChannel, Styles.maximumLightAccumulationPerColorChannel);
            EditorGUILayout.PropertyField(m_cachedLightsRenderTexture, Styles.cachedLightsRenderTexture);
            //

            m_WasModified |= serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
