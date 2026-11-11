using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Prototypes.PixelInvaders.Tests
{
    public class PixelInvadersPlayModeTests
    {
        [UnityTest]
        public IEnumerator Simulation_steps_deterministically_in_play_mode()
        {
            var settings = ScriptableObject.CreateInstance<PixelInvadersSettings>();
            var sim = new PixelInvadersSimulation(settings, seed: 7);
            sim.Step(0.02f, new PixelInvadersInput { moveAxis = 1f, firePressed = true });
            yield return null;
            Assert.Greater(sim.CannonX, 0f);
            Assert.AreEqual(1, sim.PlayerShots.Count);
            Object.Destroy(settings);
        }
    }
}
