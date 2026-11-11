using System.Linq;
using NUnit.Framework;
using Prototypes.DioramaTurn.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Prototypes.DioramaTurn.Tests
{
    public class RendererSetupTests
    {
        const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";
        const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        [Test]
        public void The_renderer_asset_has_the_tilt_shift_full_screen_feature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererSetup.RendererPath);
            Assert.IsNotNull(renderer, $"no renderer at {RendererSetup.RendererPath}");

            var features = AssetDatabase.LoadAllAssetsAtPath(RendererSetup.RendererPath)
                .OfType<FullScreenPassRendererFeature>()
                .ToArray();
            Assert.AreEqual(1, features.Length, "exactly one full-screen pass");
            Assert.IsNotNull(features[0].passMaterial);
            Assert.IsNotNull(features[0].passMaterial.shader);
            Assert.AreEqual("Prototypes/DioramaTurn/TiltShift", features[0].passMaterial.shader.name);
            Assert.AreEqual(FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing, features[0].injectionPoint);
            Assert.IsTrue(features[0].fetchColorBuffer);
        }

        [Test]
        public void The_pipeline_lists_the_diorama_renderer_after_the_untouched_pc_renderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var pc = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            var ours = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererSetup.RendererPath);

            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            Assert.AreEqual(2, list.arraySize, "the PC pipeline must hold exactly the PC renderer and ours");
            Assert.AreSame(pc, list.GetArrayElementAtIndex(0).objectReferenceValue, "the PC renderer keeps index 0");
            Assert.AreSame(ours, list.GetArrayElementAtIndex(1).objectReferenceValue);
        }

        [Test]
        public void Running_the_setup_again_does_not_duplicate_anything()
        {
            int first = RendererSetup.Ensure();
            int second = RendererSetup.Ensure();
            Assert.AreEqual(first, second);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var serialized = new SerializedObject(pipeline);
            Assert.AreEqual(2, serialized.FindProperty("m_RendererDataList").arraySize);

            var features = AssetDatabase.LoadAllAssetsAtPath(RendererSetup.RendererPath)
                .OfType<FullScreenPassRendererFeature>()
                .ToArray();
            Assert.AreEqual(1, features.Length);
        }
    }
}
