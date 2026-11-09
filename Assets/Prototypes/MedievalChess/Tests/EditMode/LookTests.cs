using System.Linq;
using NUnit.Framework;
using Prototypes.MedievalChess.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Prototypes.MedievalChess.Tests
{
    public class TorchFlickerTests
    {
        [Test]
        public void The_flicker_stays_within_the_amplitude_and_averages_near_one()
        {
            const float amplitude = 0.2f;
            float sum = 0f;
            int n = 2000;
            for (int i = 0; i < n; i++)
            {
                float f = TorchFlicker.Factor(i * 0.013f, 1.7f, amplitude, 7f);
                Assert.GreaterOrEqual(f, 1f - amplitude - 1e-4f);
                Assert.LessOrEqual(f, 1f + amplitude + 1e-4f);
                sum += f;
            }
            Assert.AreEqual(1f, sum / n, 0.03f);
        }

        [Test]
        public void An_amplitude_of_zero_gives_a_steady_flame()
        {
            for (int i = 0; i < 100; i++) Assert.AreEqual(1f, TorchFlicker.Factor(i * 0.1f, 3f, 0f, 7f));
        }

        [Test]
        public void The_flicker_actually_moves_and_each_torch_has_its_own()
        {
            var values = Enumerable.Range(0, 50).Select(i => TorchFlicker.Factor(i * 0.05f, 1f, 0.2f, 7f)).ToArray();
            Assert.Greater(values.Max() - values.Min(), 0.1f, "it should visibly wander");
            Assert.AreNotEqual(TorchFlicker.Factor(0.7f, 1f, 0.2f, 7f), TorchFlicker.Factor(0.7f, 2f, 0.2f, 7f));
            Assert.AreEqual(TorchFlicker.Factor(0.7f, 1f, 0.2f, 7f), TorchFlicker.Factor(0.7f, 1f, 0.2f, 7f), "the same moment gives the same value");
        }
    }

    public class LookBuilderTests
    {
        const string TempScene = "Assets/MedievalChessLookTest_Temp.unity";
        MedievalChessSettings settings;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            GameSceneBuilder.Build(TempScene);
            settings = AssetDatabase.LoadAssetAtPath<MedievalChessSettings>(DuelSceneBuilder.SettingsPath);
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempScene);
        }

        [Test]
        public void A_global_volume_has_bloom_a_vignette_tonemapping_and_colour_grading_from_the_settings()
        {
            var volume = Object.FindFirstObjectByType<Volume>();
            Assert.IsNotNull(volume);
            Assert.IsTrue(volume.isGlobal);
            var profile = volume.sharedProfile;
            Assert.IsTrue(AssetDatabase.Contains(profile), "the profile is a saved asset");
            Assert.IsTrue(profile.TryGet(out Bloom bloom));
            Assert.AreEqual(settings.look.bloomIntensity, bloom.intensity.value, 1e-4f);
            Assert.AreEqual(settings.look.bloomThreshold, bloom.threshold.value, 1e-4f);
            Assert.IsTrue(profile.TryGet(out Vignette vignette));
            Assert.AreEqual(settings.look.vignetteIntensity, vignette.intensity.value, 1e-4f);
            Assert.IsTrue(profile.TryGet(out Tonemapping tone));
            Assert.AreEqual(settings.look.tonemapping == 1 ? TonemappingMode.ACES : TonemappingMode.Neutral, tone.mode.value);
            Assert.IsTrue(profile.TryGet(out ColorAdjustments grade));
            Assert.AreEqual(settings.look.saturation, grade.saturation.value, 1e-4f);
        }

        [Test]
        public void The_camera_draws_post_processing()
        {
            var data = Camera.main.GetUniversalAdditionalCameraData();
            Assert.IsTrue(data.renderPostProcessing);
        }

        [Test]
        public void Four_torches_stand_round_the_board_each_with_a_flame_and_a_warm_flickering_light()
        {
            var torches = Object.FindObjectsByType<TorchFlicker>(FindObjectsSortMode.None);
            Assert.AreEqual(4, torches.Length);
            Assert.AreEqual(4, torches.Select(t => t.seed).Distinct().Count(), "each torch flickers on its own");
            var look = settings.look;
            foreach (var t in torches)
            {
                Assert.AreEqual(LightType.Point, t.torchLight.type);
                Assert.AreEqual(look.torchLightIntensity, t.torchLight.intensity, 1e-3f);
                Assert.AreEqual(look.torchLightRange, t.torchLight.range, 1e-3f);
                Assert.AreEqual(Mathf.Abs(look.torchX), Mathf.Abs(t.transform.position.x), 1e-3f);
                Assert.AreEqual(Mathf.Abs(look.torchZ), Mathf.Abs(t.transform.position.z), 1e-3f);
                Assert.IsNotNull(t.flame);
                var mat = t.flame.GetComponent<Renderer>().sharedMaterial;
                Assert.IsTrue(mat.IsKeywordEnabled("_EMISSION"));
                Assert.Greater(mat.GetColor("_EmissionColor").maxColorComponent, 1f, "bright enough for Bloom to pick up");
            }
            Assert.AreEqual(2, torches.Count(t => t.transform.position.x > 0));
            Assert.AreEqual(2, torches.Count(t => t.transform.position.z > 0));
        }

        [Test]
        public void The_torch_light_steadies_at_effect_strength_zero_and_wanders_at_one()
        {
            var t = Object.FindFirstObjectByType<TorchFlicker>();
            float old = settings.effectStrength;
            try
            {
                settings.effectStrength = 0f;
                t.Freeze(0.37f);
                float steady = t.torchLight.intensity;
                t.Freeze(1.91f);
                Assert.AreEqual(steady, t.torchLight.intensity, 1e-4f);

                settings.effectStrength = 1f;
                t.Freeze(0.37f);
                float a = t.torchLight.intensity;
                t.Freeze(1.91f);
                Assert.AreNotEqual(a, t.torchLight.intensity);
            }
            finally
            {
                settings.effectStrength = old;
                t.Freeze(null);
            }
        }

        [Test]
        public void The_key_light_is_a_cool_fill_and_the_hall_has_fog()
        {
            var key = GameObject.Find("Key Light").GetComponent<Light>();
            Assert.AreEqual(settings.look.keyLightIntensity, key.intensity, 1e-3f);
            Assert.AreEqual(settings.look.keyLightColor.b, key.color.b, 1e-3f);
            Assert.Greater(key.color.b, key.color.r, "a cool fill, so the torches are the only warm light");
            Assert.IsTrue(RenderSettings.fog);
        }
    }
}
