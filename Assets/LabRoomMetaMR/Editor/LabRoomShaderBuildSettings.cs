// Shader build settings for the Built-in Render Pipeline: strips the URP-only keyword sets at shader import, so a
// build never enumerates URP variants it would throw away anyway.
//
// Meta/Lit (Meta XR Core SDK) is in the build through the Touch controller models of OVRControllerPrefab
// (LabRoom_MetaMR > OVRInteractionComprehensive > OVRControllerVisual). Its URP SubShader borrows the passes of
// "Universal Render Pipeline/Lit" with UsePass. With no URP asset active, nothing filters the multi_compile keywords of
// those passes: ForwardLit alone has about 72 billion variants ("~72477M prepared" in the Editor log, about 100M every
// 2 s) and the build spends half an hour or more listing them before LabRoomStripUnusedSRPShaderPasses can drop them.
// The keyword declaration overrides
// below (Project Settings > Graphics > Shader Build Settings) remove those keywords at import: ForwardLit falls to about
// a thousand variants, still dropped by LabRoomStripUnusedSRPShaderPasses before compilation.
//
// Only URP SubShaders declare these keywords (Meta/Lit, MRUK, EnvironmentDepth/OcclusionLit, Meta Avatars samples), and
// none of them appears in the Built-in shader includes. Keyword sets shared with the Built-in pipeline (LIGHTMAP_ON,
// SHADOWS_SHADOWMASK, DIRLIGHTMAP_COMBINED, fog, instancing, LOD_FADE_CROSSFADE...) are left untouched.
//
// Applied when the editor loads and by Lab Room > Meta MR > 1. Configure Project for Quest; checked by 3. Validate.
// If a URP asset becomes active, the overrides are removed again so URP keeps all its variants.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Shaders;
using UnityEngine;
using UnityEngine.Rendering;
using KeywordDeclarationOverride = UnityEditor.Shaders.ShaderBuildSettings.KeywordDeclarationOverride;
using KeywordOverrideInfo = UnityEditor.Shaders.ShaderBuildSettings.KeywordOverrideInfo;

namespace LabRoom.MetaMR.EditorTools
{
    [InitializeOnLoad]
    public static class LabRoomShaderBuildSettings
    {
        // Keyword sets exactly as URP 17.3 declares them in Lit.shader and its pragma includes ("_" first, kept).
        static readonly string[] UrpOnlyKeywordSets =
        {
            "_ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN",
            "_ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS",
            "_ _ADDITIONAL_LIGHT_SHADOWS",
            "_ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX",
            "_ _REFLECTION_PROBE_BLENDING",
            "_ _REFLECTION_PROBE_BOX_PROJECTION",
            "_ _REFLECTION_PROBE_ATLAS",
            "_ REFLECTION_PROBE_ROTATION",
            "_ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH",
            "_ _SCREEN_SPACE_OCCLUSION",
            "_ _SCREEN_SPACE_IRRADIANCE",
            "_ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3",
            "_ _LIGHT_COOKIES",
            "_ _LIGHT_LAYERS",
            "_ _CLUSTER_LIGHT_LOOP",
            "_ _WRITE_RENDERING_LAYERS",
            "_ _RENDER_PASS_ENABLED",
            "_ _GBUFFER_NORMALS_OCT",
            "_ _CASTING_PUNCTUAL_LIGHT_SHADOW",
            "_ LIGHTMAP_BICUBIC_SAMPLING",
            "_ USE_LEGACY_LIGHTMAPS",
            "_ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2",
            "_ DOTS_INSTANCING_ON",
            "_ DEBUG_DISPLAY",
        };

        static LabRoomShaderBuildSettings()
        {
            EditorApplication.delayCall += () =>
            {
                if (UsesScriptableRenderPipeline()) { if (CountApplied() > 0) Remove(); }
                else if (!IsApplied()) Apply();
            };
        }

        public static bool UsesScriptableRenderPipeline()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return true;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) != null) return true;
            return false;
        }

        public static bool IsApplied() => CountApplied() == UrpOnlyKeywordSets.Length;

        public static int CountApplied()
        {
            var current = EditorGraphicsSettings.GetShaderBuildSettings().GetKeywordDeclarationOverridesCopy();
            return UrpOnlyKeywordSets.Count(set => current.Any(o => Matches(o, set) && IsStripOverride(o)));
        }

        /// <summary>Adds (or corrects) the strip overrides of the URP-only keyword sets. Other overrides are kept.</summary>
        public static void Apply()
        {
            if (UsesScriptableRenderPipeline())
            {
                LabRoomMetaMRSetup.Log("Shader build settings: an SRP asset is active, URP keyword overrides not applied");
                return;
            }
            var settings = EditorGraphicsSettings.GetShaderBuildSettings();
            var overrides = settings.GetKeywordDeclarationOverridesCopy()
                .Where(o => !UrpOnlyKeywordSets.Any(set => Matches(o, set))).ToList();
            overrides.AddRange(UrpOnlyKeywordSets.Select(StripOverride));
            settings.KeywordDeclarationOverrides = overrides.ToArray();
            EditorGraphicsSettings.SetShaderBuildSettings(settings);
            AssetDatabase.SaveAssets();
            LabRoomMetaMRSetup.Log($"Shader build settings: {UrpOnlyKeywordSets.Length} URP-only keyword sets stripped " +
                                   "(Built-in pipeline; avoids enumerating ~72 billion Universal Render Pipeline/Lit variants)");
        }

        /// <summary>Removes the overrides added by <see cref="Apply"/>.</summary>
        public static void Remove()
        {
            var settings = EditorGraphicsSettings.GetShaderBuildSettings();
            settings.KeywordDeclarationOverrides = settings.GetKeywordDeclarationOverridesCopy()
                .Where(o => !UrpOnlyKeywordSets.Any(set => Matches(o, set))).ToArray();
            EditorGraphicsSettings.SetShaderBuildSettings(settings);
            AssetDatabase.SaveAssets();
            LabRoomMetaMRSetup.Log("Shader build settings: URP keyword overrides removed (an SRP asset is active)");
        }

        static string[] Keywords(string set) => set.Split(' ');

        static KeywordDeclarationOverride StripOverride(string set) => new KeywordDeclarationOverride
        {
            keywords = Keywords(set).Select(k => new KeywordOverrideInfo(k, k == "_")).ToArray(),
            variantGenerationMode = ShaderBuildSettings.ShaderVariantGenerationMode.Default,
        };

        static bool Matches(KeywordDeclarationOverride o, string set) =>
            o.keywords != null && new HashSet<string>(o.keywords.Select(k => k.name)).SetEquals(Keywords(set));

        static bool IsStripOverride(KeywordDeclarationOverride o) => o.keywords.All(k => k.keepInBuild == (k.name == "_"));
    }
}
