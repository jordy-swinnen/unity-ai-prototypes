using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Prototypes.DioramaTurn.Editor
{
    /// <summary>
    /// Creates the DioramaTurn renderer asset (a copy of the PC renderer plus the tilt-shift full-screen pass)
    /// and appends it to the shared PC pipeline's renderer list, through Unity's API only. Idempotent.
    /// This is the one agreed change outside the prototype folder: one list entry in Assets/Settings.
    /// </summary>
    public static class RendererSetup
    {
        public const string RendererPath = "Assets/Prototypes/DioramaTurn/Settings/DioramaTurnRenderer.asset";
        public const string MaterialPath = "Assets/Prototypes/DioramaTurn/Materials/DioramaTurnTiltShift.mat";
        public const string ShaderPath = "Assets/Prototypes/DioramaTurn/Shaders/DioramaTurnTiltShift.shader";
        const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";
        const string SourceRendererPath = "Assets/Settings/PC_Renderer.asset";
        const string FeatureName = "DioramaTurn Tilt-Shift";

        /// <summary>Ensures the renderer exists and is registered, and returns its index in the pipeline's list.</summary>
        public static int Ensure()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null) throw new InvalidOperationException($"missing render pipeline asset at {PipelinePath}");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                if (!AssetDatabase.CopyAsset(SourceRendererPath, RendererPath))
                    throw new InvalidOperationException($"could not copy {SourceRendererPath} to {RendererPath}");
                AssetDatabase.ImportAsset(RendererPath, ImportAssetOptions.ForceSynchronousImport);
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (renderer == null) throw new InvalidOperationException($"copy at {RendererPath} is not a UniversalRendererData");
            }
            if (!HasTiltShift(renderer)) AddTiltShift(renderer);

            return AppendToPipeline(pipeline, renderer);
        }

        static bool HasTiltShift(UniversalRendererData renderer)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(RendererPath))
            {
                if (asset is FullScreenPassRendererFeature feature && feature.passMaterial != null &&
                    feature.passMaterial.shader != null &&
                    feature.passMaterial.shader.name == "Prototypes/DioramaTurn/TiltShift")
                    return true;
            }
            return false;
        }

        static void AddTiltShift(UniversalRendererData renderer)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidOperationException($"missing shader at {ShaderPath}");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "DioramaTurnTiltShift" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.None;

            AssetDatabase.AddObjectToAsset(feature, renderer);

            var serialized = new SerializedObject(renderer);
            var features = serialized.FindProperty("m_RendererFeatures");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            var map = serialized.FindProperty("m_RendererFeatureMap");
            if (map != null)
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = 0;
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
        }

        static int AppendToPipeline(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
        {
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (ReferenceEquals(list.GetArrayElementAtIndex(i).objectReferenceValue, renderer)) return i;
            }
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = renderer;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return list.arraySize - 1;
        }
    }
}
