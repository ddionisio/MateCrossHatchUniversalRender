using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace M8.CrossHatch.Universal.ShaderGUI {
    public struct SimpleLitProperties {
        // Surface Input Props
        public MaterialProperty specColor;
        public MaterialProperty specGlossMap;
        public MaterialProperty specHighlights;
        public MaterialProperty smoothnessMapChannel;
        public MaterialProperty smoothness;
        public MaterialProperty bumpMapProp;

        //Cross-Hatch
        public MaterialProperty crossHatchColor;
        public MaterialProperty crossHatchMap;
        public MaterialProperty crossHatchTriPlanarScale;

        public SimpleLitProperties(MaterialProperty[] properties) {
            // Surface Input Props
            specColor = BaseShaderGUI.FindProperty("_SpecColor", properties);
            specGlossMap = BaseShaderGUI.FindProperty("_SpecGlossMap", properties, false);
            specHighlights = BaseShaderGUI.FindProperty("_SpecularHighlights", properties, false);
            smoothnessMapChannel = BaseShaderGUI.FindProperty("_SmoothnessSource", properties, false);
            smoothness = BaseShaderGUI.FindProperty("_Smoothness", properties, false);
            bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);

            //Cross-Hatch
            crossHatchColor = BaseShaderGUI.FindProperty("_CrossHatchColor", properties);
            crossHatchMap = BaseShaderGUI.FindProperty("_CrossHatchMap", properties, false);
            crossHatchTriPlanarScale = BaseShaderGUI.FindProperty("_CrossHatchMap_TriPlanar_Scale", properties, false);
        }
    }

    public class CrossHatchSimpleLitShader : BaseShaderGUI {
        public enum SpecularSource {
            SpecularTextureAndColor,
            NoSpecular
        }

        public enum SmoothnessMapChannel {
            SpecularAlpha,
            AlbedoAlpha,
        }

        public enum UVSource {
            TexCoord, //use input.texcoord
            TriPlanar //generate crosshatch UV
        }

        public static class StylesExt {
            public static GUIContent specularMapText =
                new GUIContent("Specular Map", "Sets and configures a Specular map and color for your Material.");

            public static GUIContent smoothnessText = new GUIContent("Smoothness",
                "Controls the spread of highlights and reflections on the surface.");

            public static GUIContent smoothnessMapChannelText =
                new GUIContent("Source",
                    "Specifies where to sample a smoothness map from. By default, uses the alpha channel for your map.");

            public static GUIContent highlightsText = new GUIContent("Specular Highlights",
                "When enabled, the Material reflects the shine from direct lighting.");

            //Cross-Hatch
            public static GUIContent crossHatchText = new GUIContent("Cross-Hatch",
                "Sets and configures a Cross-Hatch map and color for your Material.");
        }

        public static void SetMaterialKeywords(Material material) {
            //Specular Source
            var opaque = (SurfaceType)material.GetFloat("_Surface") == SurfaceType.Opaque;

            SpecularSource specSource = (SpecularSource)material.GetFloat("_SpecularHighlights");
            if(specSource == SpecularSource.NoSpecular) {
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", false);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", false);
                CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);
            }
            else {
                var smoothnessSource = (SmoothnessMapChannel)material.GetFloat("_SmoothnessSource");
                bool hasMap = material.GetTexture("_SpecGlossMap");
                CoreUtils.SetKeyword(material, "_SPECGLOSSMAP", hasMap);
                CoreUtils.SetKeyword(material, "_SPECULAR_COLOR", !hasMap);
                if(opaque)
                    CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", smoothnessSource == SmoothnessMapChannel.AlbedoAlpha);
                else
                    CoreUtils.SetKeyword(material, "_GLOSSINESS_FROM_BASE_ALPHA", false);

                string color;
                if(smoothnessSource != SmoothnessMapChannel.AlbedoAlpha || !opaque)
                    color = "_SpecColor";
                else
                    color = "_BaseColor";

                var col = material.GetColor(color);
                col.a = material.GetFloat("_Smoothness");
                material.SetColor(color, col);
            }
            //

            //Cross-Hatch
            var uvSource = (UVSource)material.GetFloat("_CrossHatchUVMode");
            switch(uvSource) {
                case UVSource.TexCoord:
                    CoreUtils.SetKeyword(material, "_CROSSHATCH_UV_TRIPLANAR", false);
                    break;
                case UVSource.TriPlanar:
                    CoreUtils.SetKeyword(material, "_CROSSHATCH_UV_TRIPLANAR", true);
                    break;
            }
        }
                
        // Properties
        private SimpleLitProperties mProperties;

        private bool mCrossHatchFoldout = true;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties) {
            base.FindProperties(properties);
            mProperties = new SimpleLitProperties(properties);
        }

        // material changed check
        public override void MaterialChanged(Material material) {
            if(material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material, SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material) {
            if(material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                base.DrawSurfaceOptions(material);
            }
            if(EditorGUI.EndChangeCheck()) {
                foreach(var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material) {
            base.DrawSurfaceInputs(material);

            //Specular
            SpecularSource specSource = (SpecularSource)mProperties.specHighlights.floatValue;
            EditorGUI.BeginDisabledGroup(specSource == SpecularSource.NoSpecular);
            TextureColorProps(materialEditor, StylesExt.specularMapText, mProperties.specGlossMap, mProperties.specColor, true);

            //Smoothness
            var opaque = ((BaseShaderGUI.SurfaceType)material.GetFloat("_Surface") ==
                          BaseShaderGUI.SurfaceType.Opaque);
            EditorGUI.indentLevel += 2;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mProperties.smoothness.hasMixedValue;
            var smoothnessSource = (int)mProperties.smoothnessMapChannel.floatValue;
            var smoothness = mProperties.smoothness.floatValue;
            smoothness = EditorGUILayout.Slider(StylesExt.smoothnessText, smoothness, 0f, 1f);
            if(EditorGUI.EndChangeCheck()) {
                mProperties.smoothness.floatValue = smoothness;
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!opaque);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mProperties.smoothnessMapChannel.hasMixedValue;
            if(opaque)
                smoothnessSource = EditorGUILayout.Popup(StylesExt.smoothnessMapChannelText, smoothnessSource, Enum.GetNames(typeof(SmoothnessMapChannel)));
            else
                EditorGUILayout.Popup(StylesExt.smoothnessMapChannelText, 0, Enum.GetNames(typeof(SmoothnessMapChannel)));
            if(EditorGUI.EndChangeCheck())
                mProperties.smoothnessMapChannel.floatValue = smoothnessSource;
            EditorGUI.showMixedValue = false;
            EditorGUI.indentLevel -= 3;
            EditorGUI.EndDisabledGroup();
            //

            EditorGUI.EndDisabledGroup();
            //

            DrawNormalArea(materialEditor, mProperties.bumpMapProp);

            DrawEmissionProperties(material, true);
                        
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material) {
            SpecularSource specularSource = (SpecularSource)mProperties.specHighlights.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mProperties.specHighlights.hasMixedValue;
            bool enabled = EditorGUILayout.Toggle(StylesExt.highlightsText, specularSource == SpecularSource.SpecularTextureAndColor);
            if(EditorGUI.EndChangeCheck())
                mProperties.specHighlights.floatValue = enabled ? (float)SpecularSource.SpecularTextureAndColor : (float)SpecularSource.NoSpecular;
            EditorGUI.showMixedValue = false;

            base.DrawAdvancedOptions(material);
        }

        public override void DrawAdditionalFoldouts(Material material) {
            base.DrawAdditionalFoldouts(material);

            //Cross-Hatch
            mCrossHatchFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(mCrossHatchFoldout, "Cross-Hatch");
            if(mCrossHatchFoldout) {
                //texture
                TextureColorProps(materialEditor, StylesExt.crossHatchText, mProperties.crossHatchMap, mProperties.crossHatchColor, false);

                //texture mode
                var uvSource = (UVSource)material.GetFloat("_CrossHatchUVMode");

                var _uvSource = (UVSource)EditorGUILayout.EnumPopup("UV Source", uvSource);
                if(uvSource != _uvSource) {
                    uvSource = _uvSource;
                    material.SetFloat("_CrossHatchUVMode", (float)uvSource);
                }

                switch(uvSource) {
                    case UVSource.TexCoord:
                        DrawTileOffset(materialEditor, mProperties.crossHatchMap);
                        break;

                    case UVSource.TriPlanar:
                        var triPlanarScale = EditorGUILayout.FloatField("Scale", mProperties.crossHatchTriPlanarScale.floatValue);
                        if(mProperties.crossHatchTriPlanarScale.floatValue != triPlanarScale)
                            mProperties.crossHatchTriPlanarScale.floatValue = triPlanarScale;
                        break;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader) {
            if(material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if(material.HasProperty("_Emission")) {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if(oldShader == null || !oldShader.name.Contains("Legacy Shaders/")) {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if(oldShader.name.Contains("/Transparent/Cutout/")) {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if(oldShader.name.Contains("/Transparent/")) {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            MaterialChanged(material);
        }
    }
}