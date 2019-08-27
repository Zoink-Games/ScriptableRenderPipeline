using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Data.Util;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [FormerName("UnityEditor.Experimental.Rendering.LightweightPipeline.LightWeightPBRSubShader")]
    [FormerName("UnityEditor.ShaderGraph.LightWeightPBRSubShader")]
    [FormerName("UnityEditor.Rendering.LWRP.LightWeightPBRSubShader")]
    class UniversalPBRSubShader : IPBRSubShader
    {
#region Passes
        ShaderPass m_ForwardPass = new ShaderPass
        {
            // Definition
            displayName = "Universal Forward",
            referenceName = "FORWARD",
            lightMode = "UniversalForward",
            mainInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/DuplicateIncludes/PBRForwardPass.hlsl",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredVaryings = new List<string>()
            {
                "Varyings.positionWS",
                "Varyings.normalWS",
                "Varyings.tangentWS", //needed for vertex lighting
                "Varyings.bitangentWS",
                "Varyings.viewDirectionWS",
                "Varyings.lightmapUV",
                "Varyings.sh",
                "Varyings.fogFactorAndVertexLight", //fog and vertex lighting, vert input is dependency
                "Varyings.shadowCoord", //shadow coord, vert input is dependency
            },

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_fog",
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                s_LightmapKeyword,
                s_DirectionalLightmapCombinedKeyword,
                s_MainLightShadowsKeyword,
                s_MainLightShadowsCascadeKeyword,
                s_AdditionalLightsKeyword,
                s_AdditionalLightShadowsKeyword,
                s_ShadowsSoftKeyword,
                s_MixedLightingSubtractiveKeyword,
            },

            // Includes = new List<string>()
            // {
            //     "#include \"Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/DuplicateIncludes/PBRForwardPass.hlsl\"",
            // },
            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     pass.ExtraDefines.Clear();
            //     var masterNode = node as PBRMasterNode;
            //     GetSurfaceTagsOptions(masterNode, ref pass);
            //     if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
            //         pass.ExtraDefines.Add("#define _NORMALMAP 1");
            //     if (requirements.requiresDepthTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
            //     if (requirements.requiresCameraOpaqueTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            //     if (masterNode.model == PBRMasterNode.Model.Specular)
            //         pass.ExtraDefines.Add("#define _SPECULAR_SETUP");
            // }
        };

        ShaderPass m_ForwardPassMetallic2D = new ShaderPass
        {
            // Definition
            displayName = "Universal2D",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },
            
            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     var masterNode = node as PBRMasterNode;

            //     if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
            //         pass.ExtraDefines.Add("#define _NORMALMAP 1");
            //     if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
            //         pass.ExtraDefines.Add("#define _AlphaClip 1");
            //     if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
            //         pass.ExtraDefines.Add("#define _ALPHAPREMULTIPLY_ON 1");
            //     if (requirements.requiresDepthTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
            //     if (requirements.requiresCameraOpaqueTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            // }
        };

        ShaderPass m_ForwardPassSpecular2D = new ShaderPass()
        {
            // Definition
            displayName = "Universal2D",
            
            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     var masterNode = node as PBRMasterNode;

            //     pass.ExtraDefines.Add("#define _SPECULAR_SETUP 1");
            //     if (masterNode.IsSlotConnected(PBRMasterNode.NormalSlotId))
            //         pass.ExtraDefines.Add("#define _NORMALMAP 1");
            //     if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId))
            //         pass.ExtraDefines.Add("#define _AlphaClip 1");
            //     if (masterNode.surfaceType == SurfaceType.Transparent && masterNode.alphaMode == AlphaMode.Premultiply)
            //         pass.ExtraDefines.Add("#define _ALPHAPREMULTIPLY_ON 1");
            //     if (requirements.requiresDepthTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
            //     if (requirements.requiresCameraOpaqueTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            // }
        };

        ShaderPass m_DepthOnlyPass = new ShaderPass()
        {
            // Definition
            displayName = "DepthOnly",
            referenceName = "DEPTHONLY",
            lightMode = "DepthOnly",
            mainInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/DuplicateIncludes/DepthOnlyPass.hlsl",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ColorMaskOverride = "ColorMask 0",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
            },
            
            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     pass.ExtraDefines.Clear();
            //     var masterNode = node as PBRMasterNode;
            //     GetSurfaceTagsOptions(masterNode, ref pass);
            //     if (requirements.requiresDepthTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_DEPTH_TEXTURE");
            //     if (requirements.requiresCameraOpaqueTexture)
            //         pass.ExtraDefines.Add("#define REQUIRE_OPAQUE_TEXTURE");
            // }
        };

        ShaderPass m_ShadowCasterPass = new ShaderPass()
        {
            // Definition
            displayName = "ShadowCaster",
            referenceName = "SHADOWCASTER",
            lightMode = "ShadowCaster",
            mainInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/DuplicateIncludes/ShadowCasterPass.hlsl",
            
            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.normalOS",
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ZTestOverride = "ZTest LEqual",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
                "multi_compile_instancing",
            },
            keywords = new KeywordDescriptor[]
            {
                s_SmoothnessChannelKeyword,
            },
            
            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     var masterNode = node as PBRMasterNode;
            //     GetSurfaceTagsOptions(masterNode, ref pass);
            // }
        };
        ShaderPass m_LitMetaPass = new ShaderPass()
        {
            // Definition
            displayName = "Meta",
            referenceName = "META",
            lightMode = "Meta",
            mainInclude = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/DuplicateIncludes/LightingMetaPass.hlsl",

            // Port mask
            vertexPorts = new List<int>()
            {
                PBRMasterNode.PositionSlotId
            },
            pixelPorts = new List<int>()
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId
            },

            // Required fields
            requiredAttributes = new List<string>()
            {
                "Attributes.uv1", //needed for meta vertex position
            },

            // Render State Overrides
            ZWriteOverride = "ZWrite On",
            ZTestOverride = "ZTest LEqual",

            // Pass setup
            includes = new List<string>()
            {
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl",
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl",
            },
            pragmas = new List<string>()
            {
                "prefer_hlslcc gles",
                "exclude_renderers d3d11_9x",
                "target 2.0",
            },
            
            // OnGeneratePassImpl = (IMasterNode node, ref Pass pass, ref ShaderGraphRequirements requirements) =>
            // {
            //     var masterNode = node as PBRMasterNode;
            //     GetSurfaceTagsOptions(masterNode, ref pass);
            // }
        };
#endregion

#region Keywords
        static KeywordDescriptor s_LightmapKeyword = new KeywordDescriptor()
        {
            displayName = "Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_DirectionalLightmapCombinedKeyword = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_SampleGIKeyword = new KeywordDescriptor()
        {
            displayName = "Sample GI",
            referenceName = "_SAMPLE_GI",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_MainLightShadowsKeyword = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows",
            referenceName = "_MAIN_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_MainLightShadowsCascadeKeyword = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows Cascade",
            referenceName = "_MAIN_LIGHT_SHADOWS_CASCADE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_AdditionalLightsKeyword = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "_ADDITIONAL",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
            }
        };

        static KeywordDescriptor s_AdditionalLightShadowsKeyword = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_ShadowsSoftKeyword = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_MixedLightingSubtractiveKeyword = new KeywordDescriptor()
        {
            displayName = "Mixed Lighting Subtractive",
            referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        static KeywordDescriptor s_SmoothnessChannelKeyword = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };
#endregion

        public int GetPreviewPassIndex() { return 0; }

        ActiveFields GetActiveFieldsFromMasterNode(PBRMasterNode masterNode, ShaderPass pass)
        {
            var activeFields = new ActiveFields();
            var baseActiveFields = activeFields.baseInstance;

            if (masterNode.IsSlotConnected(PBRMasterNode.AlphaThresholdSlotId) ||
                masterNode.GetInputSlots<Vector1MaterialSlot>().First(x => x.id == PBRMasterNode.AlphaThresholdSlotId).value > 0.0f)
            {
                baseActiveFields.Add("AlphaClip");
            }

            if(masterNode.IsSlotConnected(PBRMasterNode.PositionSlotId))
            {
                baseActiveFields.Add("features.modifyMesh");
            }

            // Keywords for transparent
            // #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
            if (masterNode.surfaceType != ShaderGraph.SurfaceType.Opaque)
            {
                // transparent-only defines
                baseActiveFields.Add("SurfaceType.Transparent");

                // #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
                if (masterNode.alphaMode == AlphaMode.Alpha)
                {
                    baseActiveFields.Add("BlendMode.Alpha");
                }
                else if (masterNode.alphaMode == AlphaMode.Additive)
                {
                    baseActiveFields.Add("BlendMode.Add");
                }
                else if (masterNode.alphaMode == AlphaMode.Premultiply)
                {
                    baseActiveFields.Add("BlendMode.Premultiply");
                }
            }
            else
            {
                // opaque-only defines
            }

            return activeFields;
        }

        bool GenerateShaderPass(PBRMasterNode masterNode, ShaderPass pass, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            UniversalSubShaderUtilities.SetRenderState(masterNode.surfaceType, masterNode.alphaMode, masterNode.twoSided.isOn, ref pass);

            // apply master node options to active fields
            var activeFields = GetActiveFieldsFromMasterNode(masterNode, pass);

            return UniversalSubShaderUtilities.GenerateShaderPass(masterNode, pass, mode, activeFields, result, sourceAssetDependencyPaths);
        }

        public string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            if (sourceAssetDependencyPaths != null)
            {
                // UniversalPBRSubShader.cs
                sourceAssetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath("ca91dbeb78daa054c9bbe15fef76361c"));
            }

            // Master Node data
            var pbrMasterNode = masterNode as PBRMasterNode;
            var subShader = new ShaderGenerator();

            subShader.AddShaderChunk("SubShader", true);
            subShader.AddShaderChunk("{", true);
            subShader.Indent();
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(pbrMasterNode.surfaceType);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, "UniversalPipeline");
                subShader.AddShaderChunk(tagsBuilder.ToString());
                
                GenerateShaderPass(pbrMasterNode, m_ForwardPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_ShadowCasterPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_DepthOnlyPass, mode, subShader, sourceAssetDependencyPaths);
                GenerateShaderPass(pbrMasterNode, m_LitMetaPass, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", true);

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset)
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        public UniversalPBRSubShader() { }
    }
}
